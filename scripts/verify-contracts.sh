#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

echo "==> Building examples"
if [ -f "$root/scripts/build-all-examples.sh" ]; then
  "$root/scripts/build-all-examples.sh"
fi

echo "==> Building contract probes"
for dir in "$root"/tests/contract/*/; do
  if [ -f "$dir/build.sh" ]; then
    echo "    $(basename "$dir")"
    (cd "$dir" && chmod +x build.sh && ./build.sh)
  fi
done

echo "==> Verifying wasm imports against host-imports.v1.json"
python3 "$root/scripts/verify_wasm_imports.py"
echo "==> Verifying generated HostImportRegistry vs manifest"
python3 "$root/scripts/verify_host_registry.py"
echo "Contract verification passed."
