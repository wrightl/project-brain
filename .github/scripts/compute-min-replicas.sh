#!/usr/bin/env bash
set -euo pipefail

# Staging stays at minReplicas=0 by default to allow Azure SQL serverless auto-pause.
# Use the Wake staging / Sleep staging workflows for on-demand demos.

TARGET=0

echo "Staging scale target is always minReplicas=$TARGET (on-demand wake only)"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  echo "target_min_replicas=$TARGET" >> "$GITHUB_OUTPUT"
else
  echo "$TARGET"
fi
