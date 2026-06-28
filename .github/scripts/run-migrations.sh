#!/usr/bin/env bash

echo "=== Database migration job runner ==="

set -euo pipefail

if [[ "${MIGRATION_DEBUG:-false}" == "true" ]]; then
  set -x
fi

on_err() {
  local exit_code=$?
  echo "ERROR: command failed on line ${BASH_LINENO[0]} with exit code ${exit_code}" >&2
  exit "$exit_code"
}
trap on_err ERR

JOB_NAME="${MIGRATION_JOB_NAME:-migrations}"
POLL_INTERVAL_SECONDS="${MIGRATION_POLL_INTERVAL_SECONDS:-15}"
TIMEOUT_SECONDS="${MIGRATION_TIMEOUT_SECONDS:-1800}"
CONTAINER_NAME="${MIGRATION_CONTAINER_NAME:-$JOB_NAME}"

resolve_resource_group() {
  if [[ -n "${AZURE_RESOURCE_GROUP:-}" ]]; then
    echo "Using AZURE_RESOURCE_GROUP from environment: ${AZURE_RESOURCE_GROUP}"
    RESOURCE_GROUP="$AZURE_RESOURCE_GROUP"
    return 0
  fi

  if [[ -n "${AZURE_ENV_NAME:-}" ]]; then
    echo "AZURE_ENV_NAME=${AZURE_ENV_NAME}"
    echo "Selecting azd environment: ${AZURE_ENV_NAME}"
    azd env select "${AZURE_ENV_NAME}"

    echo "Trying azd env get-value AZURE_RESOURCE_GROUP..."
    if candidate="$(azd env get-value AZURE_RESOURCE_GROUP 2>/dev/null)" && [[ -n "$candidate" ]]; then
      echo "Resolved resource group from azd env: ${candidate}"
      RESOURCE_GROUP="$candidate"
      return 0
    fi
    echo "azd env get-value AZURE_RESOURCE_GROUP did not return a value"
  fi

  echo "Trying azd show -o json..."
  if command -v jq >/dev/null 2>&1; then
    if candidate="$(azd show -o json 2>/dev/null | jq -r '.resourceGroup // empty' 2>/dev/null)" && [[ -n "$candidate" ]]; then
      echo "Resolved resource group from azd show: ${candidate}"
      RESOURCE_GROUP="$candidate"
      return 0
    fi
  fi

  if [[ -n "${AZURE_ENV_NAME:-}" ]]; then
    RESOURCE_GROUP="rg-${AZURE_ENV_NAME}"
    echo "Using fallback resource group naming: ${RESOURCE_GROUP}"
    return 0
  fi

  echo "ERROR: Could not resolve AZURE_RESOURCE_GROUP" >&2
  echo "Set AZURE_RESOURCE_GROUP, ensure AZURE_ENV_NAME is configured, or run azd provision first." >&2
  exit 1
}

verify_job_exists() {
  echo "Verifying Container App Job '${JOB_NAME}' exists in '${RESOURCE_GROUP}'..."
  if az containerapp job show --name "$JOB_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    echo "Found migration job: ${JOB_NAME}"
    return 0
  fi

  echo "ERROR: Container App Job '${JOB_NAME}' not found in resource group '${RESOURCE_GROUP}'" >&2
  echo "Available jobs in resource group:" >&2
  az containerapp job list --resource-group "$RESOURCE_GROUP" -o table >&2 || true
  exit 1
}

print_failure_diagnostics() {
  local execution="$1"

  echo "Recent migration job executions:"
  az containerapp job execution list \
    --name "$JOB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output table \
    --query '[].{Status: properties.status, Name: name, StartTime: properties.startTime}'

  echo "Migration job logs:"
  if ! az containerapp job logs show \
    --name "$JOB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --container "$CONTAINER_NAME" \
    --execution "$execution" \
    --tail 100; then
    echo "Log streaming unavailable (Log Analytics may have ingest lag). Try:"
    echo "  az monitor log-analytics query --workspace <workspace-id> \\"
    echo "    --analytics-query \"ContainerAppConsoleLogs_CL | where ContainerGroupName_s startswith '${execution}' | order by _timestamp_d asc\""
  fi
}

resolve_resource_group
verify_job_exists

echo "Starting migration job '${JOB_NAME}' in resource group '${RESOURCE_GROUP}'"

EXECUTION="$(az containerapp job start \
  --name "$JOB_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query name \
  -o tsv)"

if [[ -z "$EXECUTION" ]]; then
  echo "ERROR: az containerapp job start did not return an execution name" >&2
  exit 1
fi

echo "Migration execution started: ${EXECUTION}"

deadline=$((SECONDS + TIMEOUT_SECONDS))

while true; do
  if (( SECONDS >= deadline )); then
    echo "Migration job timed out after ${TIMEOUT_SECONDS}s"
    print_failure_diagnostics "$EXECUTION"
    exit 1
  fi

  STATUS="$(az containerapp job execution show \
    --name "$JOB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --job-execution-name "$EXECUTION" \
    --query "properties.status" \
    -o tsv)"

  echo "Execution ${EXECUTION} status: ${STATUS:-unknown}"

  case "${STATUS:-}" in
    Succeeded)
      echo "Database migrations completed successfully"
      exit 0
      ;;
    Failed)
      echo "Database migrations failed"
      ;;
    Stopped|Cancelled|Canceled|Degraded)
      echo "Database migrations ended with status: ${STATUS}"
      ;;
    Running|Processing|Pending|""|null)
      sleep "$POLL_INTERVAL_SECONDS"
      continue
      ;;
    *)
      echo "Unexpected migration status: ${STATUS}"
      sleep "$POLL_INTERVAL_SECONDS"
      continue
      ;;
  esac

  print_failure_diagnostics "$EXECUTION"
  exit 1
done
