#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
for dir in "$root"/examples/*/; do
  if [ -f "$dir/build.sh" ]; then
    echo "==> Building $(basename "$dir")"
    (cd "$dir" && chmod +x build.sh && ./build.sh)
  fi
done
echo "All examples built."
