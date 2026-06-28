#!/usr/bin/env bash

azure_sql_log() {
  echo "[azure-sql] $*" >&2
}

azure_sql_fail() {
  echo "::error::$*" >&2
  exit 1
}

azure_sql_require_env() {
  : "${AZURE_ENV_NAME:?AZURE_ENV_NAME is required}"
  command -v az >/dev/null 2>&1 || azure_sql_fail "Azure CLI (az) is not available on PATH."
}

azure_sql_ensure_az_context() {
  if [ -n "${AZURE_SUBSCRIPTION_ID:-}" ]; then
    azure_sql_log "Setting subscription to ${AZURE_SUBSCRIPTION_ID}"
    az account set --subscription "$AZURE_SUBSCRIPTION_ID"
  fi

  local account
  account="$(az account show --query "{name:name, id:id}" -o tsv 2>/dev/null || true)"
  if [ -z "$account" ]; then
    azure_sql_fail "Azure CLI is not logged in. Ensure azure/login runs before this step."
  fi
  azure_sql_log "Azure account: ${account}"
}

azure_sql_discover_server() {
  local resource_group sql_server candidate_groups=()

  if [ -n "${AZURE_DEPLOY_ENV:-}" ]; then
    candidate_groups+=("rg-projectbrain-${AZURE_DEPLOY_ENV}")
  fi

  if command -v azd >/dev/null 2>&1; then
    local azd_group
    azd_group="$(azd env get-value AZURE_RESOURCE_GROUP --environment "$AZURE_ENV_NAME" 2>/dev/null || true)"
    if [ -n "$azd_group" ]; then
      candidate_groups+=("$azd_group")
    fi
  fi

  azure_sql_log "Candidate resource groups: ${candidate_groups[*]:-(none)}"

  for resource_group in "${candidate_groups[@]}"; do
    [ -n "$resource_group" ] || continue
    if ! az group show --name "$resource_group" >/dev/null 2>&1; then
      azure_sql_log "Resource group not found: ${resource_group}"
      continue
    fi

    sql_server="$(az sql server list \
      --resource-group "$resource_group" \
      --query "[?contains(name, 'projectbrain')].name | [0]" \
      -o tsv 2>/dev/null || true)"

    if [ -z "$sql_server" ] || [ "$sql_server" = "null" ]; then
      sql_server="$(az sql server list \
        --resource-group "$resource_group" \
        --query "[0].name" \
        -o tsv 2>/dev/null || true)"
    fi

    if [ -n "$sql_server" ] && [ "$sql_server" != "null" ]; then
      echo "${resource_group} ${sql_server}"
      return 0
    fi

    azure_sql_log "No SQL servers in resource group: ${resource_group}"
  done

  local discovered_rg discovered_server
  discovered_rg="$(az resource list \
    --resource-type "Microsoft.Sql/servers" \
    --query "[?contains(name, 'projectbrain')].resourceGroup | [0]" \
    -o tsv 2>/dev/null || true)"
  discovered_server="$(az resource list \
    --resource-type "Microsoft.Sql/servers" \
    --query "[?contains(name, 'projectbrain')].name | [0]" \
    -o tsv 2>/dev/null || true)"

  if [ -n "$discovered_rg" ] && [ -n "$discovered_server" ] \
    && [ "$discovered_rg" != "null" ] && [ "$discovered_server" != "null" ]; then
    echo "${discovered_rg} ${discovered_server}"
    return 0
  fi

  azure_sql_fail "Could not find an Azure SQL server. Checked groups: ${candidate_groups[*]:-(none)}."
}

azure_sql_is_valid_database_name() {
  local name="$1"
  [ -n "$name" ] \
    && [ "${#name}" -le 128 ] \
    && [[ "$name" =~ ^[a-zA-Z0-9_-]+$ ]]
}

azure_sql_resolve_database_name() {
  local resource_group="$1"
  local sql_server="$2"
  local database_name=""

  database_name="$(az sql db list \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --query "[?contains(name, 'projectbrain')].name | [0]" \
    -o tsv 2>/dev/null || true)"

  if ! azure_sql_is_valid_database_name "$database_name"; then
    database_name=""
  fi

  if [ -z "$database_name" ]; then
    database_name="projectbraindb"
  fi

  echo "$database_name"
}

azure_sql_wait_for_database_online() {
  local resource_group="$1"
  local sql_server="$2"
  local database_name="$3"
  local timeout_seconds="${4:-300}"
  local poll_interval_seconds="${5:-20}"
  local elapsed=0
  local status=""

  azure_sql_log "Waiting for database '${database_name}' to become Online (timeout: ${timeout_seconds}s)..."

  while [ "$elapsed" -lt "$timeout_seconds" ]; do
    status="$(az sql db show \
      --resource-group "$resource_group" \
      --server "$sql_server" \
      --name "$database_name" \
      --query status \
      -o tsv 2>/dev/null || true)"

    if [ "$status" = "Online" ]; then
      azure_sql_log "Database '${database_name}' is Online."
      return 0
    fi

    azure_sql_log "Database status: ${status:-unknown}; retrying in ${poll_interval_seconds}s (${elapsed}s elapsed)..."
    sleep "$poll_interval_seconds"
    elapsed=$((elapsed + poll_interval_seconds))
  done

  azure_sql_fail "Database '${database_name}' did not become Online within ${timeout_seconds}s (last status: ${status:-unknown})."
}

azure_sql_migration_log_is_pause_error() {
  local log_file="$1"
  grep -qiE '40613|not currently available|transient failure' "$log_file" 2>/dev/null
}

azure_sql_migration_log_is_auth_error() {
  local log_file="$1"
  grep -qiE 'login failed|authentication failed|not authorized|permission denied|cannot open database.*login|18456|18452|18470' "$log_file" 2>/dev/null
}

azure_sql_resolve_admin_password() {
  if [ -n "${AZURE_PROJECTBRAIN_PASSWORD:-}" ]; then
    echo "$AZURE_PROJECTBRAIN_PASSWORD"
    return 0
  fi

  if command -v azd >/dev/null 2>&1; then
    local azd_password=""
    if azd_password="$(azd env get-value projectbrain-password --environment "$AZURE_ENV_NAME" 2>/dev/null)"; then
      if [ -n "$azd_password" ]; then
        echo "$azd_password"
        return 0
      fi
    fi
  fi

  azure_sql_fail "SQL admin password is not available. Set AZURE_PROJECTBRAIN_PASSWORD (mapped from SQL_PASSWORD secret) in the workflow."
}

azure_sql_is_entra_only_server() {
  local resource_group="$1"
  local sql_server="$2"
  local aad_only

  aad_only="$(az sql server show \
    --resource-group "$resource_group" \
    --name "$sql_server" \
    --query "administrators.azureAdOnlyAuthentication" \
    -o tsv 2>/dev/null || true)"

  [ "$aad_only" = "true" ] || [ "$aad_only" = "True" ]
}

azure_sql_ensure_deploy_principal_sql_access() {
  local resource_group="$1"
  local sql_server="$2"

  if [ -z "${AZURE_CLIENT_ID:-}" ]; then
    azure_sql_log "AZURE_CLIENT_ID is not set; skipping deploy principal SQL access check."
    return 0
  fi

  local deployer_oid deployer_name current_oid
  deployer_oid="$(az ad sp show --id "$AZURE_CLIENT_ID" --query id -o tsv)"
  deployer_name="$(az ad sp show --id "$AZURE_CLIENT_ID" --query displayName -o tsv)"
  current_oid="$(az sql server ad-admin list \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --query "[0].sid" \
    -o tsv 2>/dev/null || true)"

  if [ "$current_oid" = "$deployer_oid" ]; then
    azure_sql_log "Deploy service principal is SQL Entra admin."
    return 0
  fi

  if [ -z "$current_oid" ] || [ "$current_oid" = "null" ]; then
    azure_sql_log "No SQL Entra admin configured; assigning deploy service principal (${deployer_name})."
    az sql server ad-admin create \
      --resource-group "$resource_group" \
      --server "$sql_server" \
      --display-name "$deployer_name" \
      --object-id "$deployer_oid"
    return 0
  fi

  azure_sql_log "SQL Entra admin is a different identity (${current_oid}). The GitHub Actions service principal (${AZURE_CLIENT_ID}) must be that admin, belong to that admin group, or have db_owner on the target database."
}

azure_sql_escape_connection_value() {
  local val="$1"
  if [[ "$val" == *';'* || "$val" == *'='* || "$val" == *'"'* || "$val" == *$'\''* ]]; then
    val="${val//\"/\"\"}"
    printf '"%s"' "$val"
  else
    printf '%s' "$val"
  fi
}

azure_sql_build_entra_connection_string() {
  local resource_group="$1"
  local sql_server="$2"
  local fqdn database_name

  fqdn="$(az sql server show \
    --resource-group "$resource_group" \
    --name "$sql_server" \
    --query fullyQualifiedDomainName \
    -o tsv)"
  database_name="$(azure_sql_resolve_database_name "$resource_group" "$sql_server")"

  if [ -z "$fqdn" ]; then
    azure_sql_fail "Could not resolve SQL server FQDN for '${sql_server}'."
  fi

  azure_sql_log "Migration target (Entra ID): ${fqdn}/${database_name}"

  printf 'Server=tcp:%s,1433;Initial Catalog=%s;Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;Authentication=Active Directory Default;' \
    "$fqdn" "$database_name"
}

azure_sql_build_sql_auth_connection_string() {
  local resource_group="$1"
  local sql_server="$2"
  local fqdn admin_user database_name password escaped_password

  fqdn="$(az sql server show \
    --resource-group "$resource_group" \
    --name "$sql_server" \
    --query fullyQualifiedDomainName \
    -o tsv)"
  admin_user="$(az sql server show \
    --resource-group "$resource_group" \
    --name "$sql_server" \
    --query administratorLogin \
    -o tsv)"
  database_name="$(azure_sql_resolve_database_name "$resource_group" "$sql_server")"
  password="$(azure_sql_resolve_admin_password)"

  if [ -z "$fqdn" ] || [ -z "$admin_user" ]; then
    azure_sql_fail "Could not resolve SQL server FQDN or administrator login for '${sql_server}'."
  fi

  escaped_password="$(azure_sql_escape_connection_value "$password")"
  azure_sql_log "Migration target (SQL auth): ${fqdn}/${database_name} (user: ${admin_user})"

  printf 'Server=tcp:%s,1433;Initial Catalog=%s;User ID=%s;Password=%s;Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;' \
    "$fqdn" "$database_name" "$admin_user" "$escaped_password"
}

# Aspire Azure SQL defaults to Entra ID; try that first in CI after azure/login.
azure_sql_build_migration_connection_string() {
  azure_sql_build_entra_connection_string "$@"
}
