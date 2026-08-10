#!/usr/bin/env bash
set -euo pipefail

# Apply minReplicas to staging container apps.
# Required env: RESOURCE_GROUP, CONTAINER_APPS, TARGET

if [[ -z "${RESOURCE_GROUP:-}" || -z "${CONTAINER_APPS:-}" || -z "${TARGET:-}" ]]; then
  echo "RESOURCE_GROUP, CONTAINER_APPS, and TARGET are required" >&2
  exit 1
fi

for app in $CONTAINER_APPS; do
  current=$(az containerapp show \
    --name "$app" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.template.scale.minReplicas" \
    -o tsv)

  echo "$app: current=$current target=$TARGET"

  if [[ "$current" == "$TARGET" ]]; then
    echo "$app already at target minReplicas=$TARGET, skipping"
    continue
  fi

  az containerapp update \
    --name "$app" \
    --resource-group "$RESOURCE_GROUP" \
    --min-replicas "$TARGET"

  echo "$app updated to minReplicas=$TARGET"
done
