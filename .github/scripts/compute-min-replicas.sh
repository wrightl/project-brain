#!/usr/bin/env bash
set -euo pipefail

# Peak hours in Europe/London:
#   Weekdays (Mon-Fri): 18:00-21:59 -> minReplicas 1
#   Weekends (Sat-Sun): 09:00-21:59 -> minReplicas 1
#   All other times:    minReplicas 0

export TZ="Europe/London"

HOUR=$(date +%-H)
DOW=$(date +%u)

TARGET=0

if [[ "$DOW" -ge 6 ]]; then
  if (( HOUR >= 9 && HOUR < 22 )); then
    TARGET=1
  fi
elif (( HOUR >= 18 && HOUR < 22 )); then
  TARGET=1
fi

echo "London time: $(date '+%A %H:%M %Z') (dow=$DOW hour=$HOUR) -> target_min_replicas=$TARGET"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  echo "target_min_replicas=$TARGET" >> "$GITHUB_OUTPUT"
else
  echo "$TARGET"
fi
