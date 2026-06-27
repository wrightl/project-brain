#!/usr/bin/env bash
# Manage a temporary Azure SQL firewall rule for GitHub Actions runners.
# Usage:
#   bash sql-firewall-rule.sh add
#   bash sql-firewall-rule.sh remove
set -euo pipefail

ACTION="${1:?Usage: sql-firewall-rule.sh add|remove}"
RULE_NAME="github-actions-migrate"

log() {
  echo "[sql-firewall] $*" >&2
}

fail() {
  echo "::error::$*" >&2
  exit 1
}

on_error() {
  local exit_code=$?
  echo "::error::sql-firewall-rule.sh failed at line ${BASH_LINENO[0]} (exit ${exit_code}) while running: ${BASH_COMMAND}" >&2
  exit "$exit_code"
}
trap on_error ERR

require_env() {
  : "${AZURE_ENV_NAME:?AZURE_ENV_NAME is required}"
  command -v az >/dev/null 2>&1 || fail "Azure CLI (az) is not available on PATH."
}

ensure_az_context() {
  if [ -n "${AZURE_SUBSCRIPTION_ID:-}" ]; then
    log "Setting subscription to ${AZURE_SUBSCRIPTION_ID}"
    az account set --subscription "$AZURE_SUBSCRIPTION_ID"
  fi

  local account
  account="$(az account show --query "{name:name, id:id}" -o tsv 2>/dev/null || true)"
  if [ -z "$account" ]; then
    fail "Azure CLI is not logged in. Ensure azure/login runs before this step."
  fi
  log "Azure account: ${account}"
}

discover_sql_server() {
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

  log "Candidate resource groups: ${candidate_groups[*]:-(none)}"

  for resource_group in "${candidate_groups[@]}"; do
    [ -n "$resource_group" ] || continue
    if ! az group show --name "$resource_group" >/dev/null 2>&1; then
      log "Resource group not found: ${resource_group}"
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

    log "No SQL servers in resource group: ${resource_group}"
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

  fail "Could not find an Azure SQL server. Checked groups: ${candidate_groups[*]:-(none)}."
}

add_rule() {
  local runner_ip resource_group sql_server

  runner_ip="$(curl -fsS --retry 3 --retry-delay 2 https://api.ipify.org)"
  log "Runner IP: ${runner_ip}"

  read -r resource_group sql_server <<<"$(discover_sql_server)"
  log "SQL server: ${sql_server}"
  log "Resource group: ${resource_group}"

  az sql server firewall-rule delete \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --name "$RULE_NAME" \
    2>/dev/null || true

  log "Creating firewall rule ${RULE_NAME}..."
  if ! az sql server firewall-rule create \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --name "$RULE_NAME" \
    --start-ip-address "$runner_ip" \
    --end-ip-address "$runner_ip" \
    --only-show-errors; then
    fail "az sql server firewall-rule create failed. Ensure the GitHub federated identity has SQL Server Contributor or Contributor on '${resource_group}'."
  fi

  log "Firewall rule created successfully."
}

remove_rule() {
  local resource_group sql_server

  if ! read -r resource_group sql_server <<<"$(discover_sql_server 2>/dev/null)"; then
    log "Could not resolve SQL server; skipping firewall rule removal."
    return 0
  fi

  log "Removing firewall rule from SQL server: ${sql_server} (resource group: ${resource_group})"

  az sql server firewall-rule delete \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --name "$RULE_NAME" \
    2>/dev/null || true
}

require_env
ensure_az_context

case "$ACTION" in
  add) add_rule ;;
  remove) remove_rule ;;
  *)
    fail "Unknown action: ${ACTION} (expected add or remove)"
    ;;
esac
