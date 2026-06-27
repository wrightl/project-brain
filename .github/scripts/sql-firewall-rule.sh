#!/usr/bin/env bash
# Manage a temporary Azure SQL firewall rule for GitHub Actions runners.
# Usage:
#   bash sql-firewall-rule.sh add
#   bash sql-firewall-rule.sh remove
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=azure-sql-lib.sh
source "${SCRIPT_DIR}/azure-sql-lib.sh"

ACTION="${1:?Usage: sql-firewall-rule.sh add|remove}"
RULE_NAME="github-actions-migrate"

log() {
  azure_sql_log "$*"
}

fail() {
  azure_sql_fail "$*"
}

on_error() {
  local exit_code=$?
  echo "::error::sql-firewall-rule.sh failed at line ${BASH_LINENO[0]} (exit ${exit_code}) while running: ${BASH_COMMAND}" >&2
  exit "$exit_code"
}
trap on_error ERR

add_rule() {
  local runner_ip resource_group sql_server

  runner_ip="$(curl -fsS --retry 3 --retry-delay 2 https://api.ipify.org)"
  log "Runner IP: ${runner_ip}"

  read -r resource_group sql_server <<<"$(azure_sql_discover_server)"
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

  if ! read -r resource_group sql_server <<<"$(azure_sql_discover_server 2>/dev/null)"; then
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

azure_sql_require_env
azure_sql_ensure_az_context

case "$ACTION" in
  add) add_rule ;;
  remove) remove_rule ;;
  *)
    fail "Unknown action: ${ACTION} (expected add or remove)"
    ;;
esac
