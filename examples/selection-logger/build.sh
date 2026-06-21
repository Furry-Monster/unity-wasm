#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
CARGO_TARGET_DIR=target cargo build --target wasm32-unknown-unknown --release
mkdir -p bin
cp target/wasm32-unknown-unknown/release/selection_logger.wasm bin/tool.wasm
echo "Built bin/tool.wasm ($(wc -c < bin/tool.wasm) bytes)"
