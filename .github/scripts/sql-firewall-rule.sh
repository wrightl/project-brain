#!/usr/bin/env bash
# Manage a temporary Azure SQL firewall rule for GitHub Actions runners.
# Usage:
#   sql-firewall-rule.sh add
#   sql-firewall-rule.sh remove
set -euo pipefail

ACTION="${1:?Usage: sql-firewall-rule.sh add|remove}"
RULE_NAME="github-actions-migrate"

require_env() {
  : "${AZURE_ENV_NAME:?AZURE_ENV_NAME is required}"
}

select_azd_env() {
  azd env select "$AZURE_ENV_NAME" --no-prompt
}

get_connection_string() {
  azd env get-value ConnectionStrings__projectbraindb --environment "$AZURE_ENV_NAME"
}

parse_sql_server_name() {
  local connection_string="$1"
  local server=""

  if [[ "$connection_string" =~ Server=tcp:([a-zA-Z0-9-]+)\.database\.windows\.net ]]; then
    server="${BASH_REMATCH[1]}"
  elif [[ "$connection_string" =~ Server=([^;,]+) ]]; then
    server="${BASH_REMATCH[1]}"
    server="${server#tcp:}"
    server="${server%%.*}"
  fi

  if [ -z "$server" ]; then
    echo "Could not parse SQL server name from connection string." >&2
    exit 1
  fi

  echo "$server"
}

resolve_resource_group() {
  local sql_server="$1"
  local resource_group=""

  resource_group="$(azd env get-value AZURE_RESOURCE_GROUP --environment "$AZURE_ENV_NAME" 2>/dev/null || true)"
  if [ -n "$resource_group" ]; then
    echo "$resource_group"
    return 0
  fi

  if [ -n "${AZURE_DEPLOY_ENV:-}" ]; then
    resource_group="rg-projectbrain-${AZURE_DEPLOY_ENV}"
    if az group show --name "$resource_group" >/dev/null 2>&1; then
      echo "$resource_group"
      return 0
    fi
  fi

  resource_group="$(az resource list \
    --name "$sql_server" \
    --resource-type "Microsoft.Sql/servers" \
    --query "[0].resourceGroup" \
    -o tsv 2>/dev/null || true)"

  if [ -n "$resource_group" ] && [ "$resource_group" != "null" ]; then
    echo "$resource_group"
    return 0
  fi

  echo "Could not resolve SQL server resource group for '$sql_server'." >&2
  exit 1
}

add_rule() {
  local runner_ip sql_server resource_group

  runner_ip="$(curl -fsS https://api.ipify.org)"
  echo "Runner IP: $runner_ip"

  local connection_string
  connection_string="$(get_connection_string)"
  sql_server="$(parse_sql_server_name "$connection_string")"
  resource_group="$(resolve_resource_group "$sql_server")"

  echo "SQL server: $sql_server"
  echo "Resource group: $resource_group"

  az sql server firewall-rule delete \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --name "$RULE_NAME" \
    2>/dev/null || true

  az sql server firewall-rule create \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --name "$RULE_NAME" \
    --start-ip-address "$runner_ip" \
    --end-ip-address "$runner_ip"
}

remove_rule() {
  local sql_server resource_group

  local connection_string
  if ! connection_string="$(get_connection_string 2>/dev/null)"; then
    echo "Connection string unavailable; skipping firewall rule removal."
    return 0
  fi

  sql_server="$(parse_sql_server_name "$connection_string")"
  resource_group="$(resolve_resource_group "$sql_server")"

  echo "Removing firewall rule from SQL server: $sql_server (resource group: $resource_group)"

  az sql server firewall-rule delete \
    --resource-group "$resource_group" \
    --server "$sql_server" \
    --name "$RULE_NAME" \
    2>/dev/null || true
}

require_env
select_azd_env

case "$ACTION" in
  add) add_rule ;;
  remove) remove_rule ;;
  *)
    echo "Unknown action: $ACTION (expected add or remove)" >&2
    exit 1
    ;;
esac
