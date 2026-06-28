#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=azure-sql-lib.sh
source "${SCRIPT_DIR}/azure-sql-lib.sh"

MIGRATION_LOG="$(mktemp)"
trap 'rm -f "$MIGRATION_LOG"' EXIT

on_error() {
  local exit_code=$?
  echo "::error::apply-database-migrations.sh failed at line ${BASH_LINENO[0]} (exit ${exit_code}) while running: ${BASH_COMMAND}" >&2
  exit "$exit_code"
}
trap on_error ERR

export PATH="${HOME}/.dotnet/tools:${PATH}"

azure_sql_require_env
azure_sql_ensure_az_context

if ! command -v dotnet-ef >/dev/null 2>&1; then
  azure_sql_fail "dotnet-ef is not on PATH. Install it with 'dotnet tool install --global dotnet-ef' and ensure ~/.dotnet/tools is on PATH."
fi

azure_sql_log "dotnet-ef version: $(dotnet-ef --version)"

read -r resource_group sql_server <<<"$(azure_sql_discover_server)"
database_name="$(azure_sql_resolve_database_name "$resource_group" "$sql_server")"
azure_sql_ensure_deploy_principal_sql_access "$resource_group" "$sql_server"
azure_sql_wait_for_database_online "$resource_group" "$sql_server" "$database_name"

run_migrations() {
  local connection_string="$1"
  export ConnectionStrings__projectbraindb="$connection_string"
  export EF_CONNECTION_STRING="$connection_string"

  : >"$MIGRATION_LOG"
  dotnet ef database update \
    --project ProjectBrain.Database/ProjectBrain.Database.csproj \
    --startup-project ProjectBrain.Api/ProjectBrain.Api.csproj \
    --configuration Release \
    --connection "$connection_string" \
    --verbose 2>&1 | tee "$MIGRATION_LOG"
  return "${PIPESTATUS[0]}"
}

run_migrations_with_retries() {
  local connection_string="$1"
  local auth_label="$2"
  local attempt max_attempts=3
  local wait_seconds=45

  for attempt in $(seq 1 "$max_attempts"); do
    azure_sql_log "Applying EF Core migrations (${auth_label}), attempt ${attempt}/${max_attempts}..."
    if run_migrations "$connection_string"; then
      azure_sql_log "Database migrations applied successfully (${auth_label})."
      return 0
    fi

    if azure_sql_migration_log_is_pause_error "$MIGRATION_LOG" && [ "$attempt" -lt "$max_attempts" ]; then
      azure_sql_log "Database may still be resuming from auto-pause; waiting ${wait_seconds}s before retry..."
      sleep "$wait_seconds"
      azure_sql_wait_for_database_online "$resource_group" "$sql_server" "$database_name"
      continue
    fi

    return 1
  done

  return 1
}

handle_migration_failure() {
  local auth_label="$1"
  local allow_auth_fallback="${2:-false}"

  if azure_sql_migration_log_is_pause_error "$MIGRATION_LOG"; then
    azure_sql_fail "${auth_label} migration failed: Azure SQL database is paused or still resuming (error 40613). Wait for the database to reach Online status in Azure Portal, then re-run the workflow."
  fi

  if azure_sql_migration_log_is_auth_error "$MIGRATION_LOG"; then
    if [ "$allow_auth_fallback" = "true" ]; then
      return 0
    fi

    azure_sql_fail "${auth_label} migration failed due to authentication or authorization. Ensure the GitHub Actions service principal (${AZURE_CLIENT_ID:-unknown}) is the SQL Entra admin or has db_owner on the database."
  fi

  azure_sql_fail "${auth_label} migration failed. See the dotnet ef output above for details."
}

azure_sql_log "Restoring and building projects for EF design-time services..."
dotnet restore ProjectBrain.Api/ProjectBrain.Api.csproj
dotnet build ProjectBrain.Api/ProjectBrain.Api.csproj --configuration Release --no-restore

entra_connection_string="$(azure_sql_build_entra_connection_string "$resource_group" "$sql_server")"
if run_migrations_with_retries "$entra_connection_string" "Entra ID"; then
  exit 0
fi

if handle_migration_failure "Entra ID" "true"; then
  azure_sql_log "Entra ID migration failed with an authentication error; checking whether SQL authentication is available..."

  if azure_sql_is_entra_only_server "$resource_group" "$sql_server"; then
    azure_sql_fail "Entra ID migration failed and the SQL server is Entra-only (SQL authentication is disabled). Ensure the GitHub Actions service principal (${AZURE_CLIENT_ID:-unknown}) is the SQL Entra admin or has db_owner on the database."
  fi

  if [ -z "${AZURE_PROJECTBRAIN_PASSWORD:-}" ]; then
    azure_sql_fail "Entra ID migration failed and AZURE_PROJECTBRAIN_PASSWORD is not set for SQL auth fallback."
  fi

  sql_connection_string="$(azure_sql_build_sql_auth_connection_string "$resource_group" "$sql_server")"
  if run_migrations_with_retries "$sql_connection_string" "SQL authentication"; then
    exit 0
  fi

  handle_migration_failure "SQL authentication" "false"
fi
