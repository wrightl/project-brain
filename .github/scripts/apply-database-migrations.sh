#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=azure-sql-lib.sh
source "${SCRIPT_DIR}/azure-sql-lib.sh"

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

run_migrations() {
  local connection_string="$1"
  export ConnectionStrings__projectbraindb="$connection_string"
  export EF_CONNECTION_STRING="$connection_string"

  dotnet ef database update \
    --project ProjectBrain.Database/ProjectBrain.Database.csproj \
    --startup-project ProjectBrain.Api/ProjectBrain.Api.csproj \
    --configuration Release \
    --connection "$connection_string" \
    --verbose
}

azure_sql_log "Restoring and building projects for EF design-time services..."
dotnet restore ProjectBrain.Api/ProjectBrain.Api.csproj
dotnet build ProjectBrain.Api/ProjectBrain.Api.csproj --configuration Release --no-restore

entra_connection_string="$(azure_sql_build_entra_connection_string "$resource_group" "$sql_server")"
azure_sql_log "Applying EF Core migrations with Entra ID authentication..."
if run_migrations "$entra_connection_string"; then
  azure_sql_log "Database migrations applied successfully (Entra ID)."
  exit 0
fi

azure_sql_log "Entra ID migration failed; retrying with SQL authentication..."

if [ -z "${AZURE_PROJECTBRAIN_PASSWORD:-}" ]; then
  azure_sql_fail "Entra ID migration failed and AZURE_PROJECTBRAIN_PASSWORD is not set for SQL auth fallback."
fi

sql_connection_string="$(azure_sql_build_sql_auth_connection_string "$resource_group" "$sql_server")"
run_migrations "$sql_connection_string"

azure_sql_log "Database migrations applied successfully (SQL authentication)."
