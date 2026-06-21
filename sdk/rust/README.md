# Rust Tool Template

Copy this folder to `Assets/Editor/Tools/my-tool/` or keep under `examples/`.

## Setup

```bash
rustup target add wasm32-unknown-unknown
```

## Build

```bash
CARGO_TARGET_DIR=target cargo build --target wasm32-unknown-unknown --release
cp target/wasm32-unknown-unknown/release/my_tool.wasm bin/tool.wasm
```

## Imports

Link against host modules documented in `docs/host-api.md`:

- `editor_core` — logging, time, progress
- `editor_selection` — Selection handles
- `editor_assets` — AssetDatabase queries

## Exports

Your module must export:

- `on_init() -> i32` — 0 on success
- `on_shutdown()`
- `on_menu_click()`

See `examples/selection-logger` for a complete reference.
