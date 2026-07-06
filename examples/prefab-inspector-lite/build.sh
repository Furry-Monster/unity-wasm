#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
root="$(cd ../.. && pwd)"
python3 "$root/scripts/gen-rust-imports.py" "src/imports.rs"
CARGO_TARGET_DIR=target cargo build --target wasm32-unknown-unknown --release
mkdir -p bin
cp target/wasm32-unknown-unknown/release/prefab_inspector_lite.wasm bin/tool.wasm
echo "Built bin/tool.wasm ($(wc -c < bin/tool.wasm) bytes)"
