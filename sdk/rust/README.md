# Rust Tool SDK

Copy `template/` to `Assets/Editor/Tools/my-tool/` or keep under `examples/`.

## Prerequisites

```bash
rustup target add wasm32-unknown-unknown
```

## Build

```bash
chmod +x build.sh
./build.sh
```

The template `build.sh` generates `src/imports.rs` from `schemas/host-imports.v1.json` before compiling. You call only the imports you need; unused functions are fine (dead_code warnings are expected).

## Host imports

| Wasm module | Tier | Reference |
|-------------|------|-----------|
| `editor_core` | 0 | logging, time, progress |
| `editor_selection` | 1 | Selection handles |
| `editor_assets` | 2 | AssetDatabase queries |
| `editor_scene` | 3 | hierarchy path, components |

Full lowered signatures: [docs/host-api.md](../../docs/host-api.md).  
Semantic WIT: [wit/editor-api/](../../wit/editor-api/).

## Codegen workflow

```bash
# From repo root — regenerate Rust extern blocks for any tool
python3 scripts/gen-rust-imports.py path/to/src/imports.rs
```

In Unity (after host manifest changes):

**Tools → Wasm Editor → Generate Host Bindings** — updates C# registry, schema, and related generated files.

## Required exports

Your module must export:

- `on_init() -> i32` — return 0 on success
- `on_shutdown()`
- `on_menu_click()`

Set `tool.json` `abi` to `editor-api/1`.

## Examples

| Tool | Path |
|------|------|
| Minimal template | `sdk/rust/template` |
| Selection API | `examples/selection-logger` |
| Asset scan | `examples/asset-scanner` |
| Scene / components | `examples/prefab-inspector-lite` |

## Contract check

After building:

```bash
./scripts/verify-contracts.sh
```

Verifies guest wasm imports against the host manifest (local, no CI).
