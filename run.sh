#!/usr/bin/env bash
set -euo pipefail

cleanup_stale_aspire() {
  echo "Cleaning up stale Aspire processes..."

  # Only one dotnet watch instance should run this AppHost at a time.
  existing_watch=$(pgrep -f 'dotnet watch run --project ./projectbrain.apphost' 2>/dev/null || true)
  if [ -n "$existing_watch" ]; then
    echo "Stopping existing dotnet watch: $existing_watch"
    kill -9 $existing_watch 2>/dev/null || true
  fi

  # Orphaned AppHost / DCP children from interrupted dotnet watch runs.
  pkill -9 -f 'projectbrain.apphost/bin/Debug' 2>/dev/null || true
  pkill -9 -f 'Aspire.Hosting.Dcp' 2>/dev/null || true

  # Legacy fixed ports from older launchSettings (harmless if unused).
  for port in 22259 21127 17201 15251; do
    pids=$(lsof -ti tcp:"$port" -sTCP:LISTEN 2>/dev/null || true)
    if [ -n "$pids" ]; then
      echo "Stopping stale listener on port $port: $pids"
      kill -9 $pids 2>/dev/null || true
    fi
  done
}

cleanup_stale_aspire
sleep 0.5

dotnet watch run --project ./projectbrain.apphost/projectbrain.apphost.csproj --launch-profile https
