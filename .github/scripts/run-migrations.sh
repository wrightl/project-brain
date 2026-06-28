#!/usr/bin/env bash
set -euo pipefail

# Starts the manual Azure Container App Job that applies EF migrations and seeds data,
# then waits for the execution to reach a terminal state.

JOB_NAME="${MIGRATION_JOB_NAME:-migrations}"
POLL_INTERVAL_SECONDS="${MIGRATION_POLL_INTERVAL_SECONDS:-15}"
TIMEOUT_SECONDS="${MIGRATION_TIMEOUT_SECONDS:-1800}"

RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-}"
if [[ -z "$RESOURCE_GROUP" ]]; then
  RESOURCE_GROUP="$(azd env get-value AZURE_RESOURCE_GROUP)"
fi

echo "Starting migration job '$JOB_NAME' in resource group '$RESOURCE_GROUP'"

EXECUTION="$(az containerapp job start \
  --name "$JOB_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query name \
  -o tsv)"

echo "Migration execution started: $EXECUTION"

deadline=$((SECONDS + TIMEOUT_SECONDS))

while true; do
  if (( SECONDS >= deadline )); then
    echo "Migration job timed out after ${TIMEOUT_SECONDS}s"
    exit 1
  fi

  STATUS="$(az containerapp job execution show \
    --name "$JOB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --job-execution-name "$EXECUTION" \
    --query "properties.status" \
    -o tsv)"

  echo "Execution $EXECUTION status: ${STATUS:-unknown}"

  case "${STATUS:-}" in
    Succeeded)
      echo "Database migrations completed successfully"
      exit 0
      ;;
    Failed)
      echo "Database migrations failed"
      ;;
    Stopped|Cancelled|Canceled|Degraded)
      echo "Database migrations ended with status: $STATUS"
      ;;
    Running|Processing|Pending|""|null)
      sleep "$POLL_INTERVAL_SECONDS"
      continue
      ;;
    *)
      echo "Unexpected migration status: $STATUS"
      sleep "$POLL_INTERVAL_SECONDS"
      continue
      ;;
  esac

  echo "Recent migration job executions:"
  az containerapp job execution list \
    --name "$JOB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --output table \
    --query '[].{Status: properties.status, Name: name, StartTime: properties.startTime}' \
    || true

  echo "Migration job logs (if available):"
  az containerapp job logs show \
    --name "$JOB_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --execution "$EXECUTION" \
    --tail 100 \
    || true

  exit 1
done
