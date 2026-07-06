# Unity Editor WASM Platform

WebAssembly editor extension platform for **Unity 2022 LTS**. Run Rust/AssemblyScript tools inside a Wasmtime sandbox with crash isolation, hot reload, and a curated Editor Host API.

## Features

- **Wasmtime .NET** runtime (Editor-only, JIT enabled)
- **Curated host API** — Selection, AssetDatabase, logging, progress
- **Hot reload** — rebuild `.wasm` without Domain Reload
- **Structured trap reports** — AI-friendly JSON diagnostics
- **WIT contracts** — `wit/editor-api/` defines the ABI
- **Example tools** — `selection-logger`, `asset-scanner`, `prefab-inspector-lite`
- **FFI manifest** — `schemas/host-imports.v1.json` is the enforceable host contract

## Quick Start

### 1. Open sample project

Open `sample-project/` in Unity **2022.3 LTS**.

### 2. Build example tools

```bash
./scripts/build-all-examples.sh
```

Or build individually:

```bash
cd examples/selection-logger && ./build.sh
cd examples/asset-scanner && ./build.sh
cd examples/prefab-inspector-lite && ./build.sh
```

Verify host/guest contract locally:

```bash
./scripts/verify-contracts.sh
```

### 3. Run in Unity

1. **Tools → Wasm Editor → Refresh Tools**
2. **Tools → Wasm Editor → Run Tool...** — pick a tool from the context menu
3. Optional: **Tools → Wasm Editor → Open Tool Shell** for logs, trap JSON, and reload times

### 4. Hot reload

With Unity open, edit `examples/*/src/lib.rs` and run `./build.sh`. The host reloads within ~300ms.

## Tool Development

See **[docs/getting-started-tool-dev.md](docs/getting-started-tool-dev.md)** for the full guide (template, `tool.json`, Host imports, traps, placement paths).

Copy `sdk/rust/template/` to start a new tool.

## Project Layout

```
packages/com.fumo.editor-wasm/   # UPM host package
packages/com.fumo.wasmtime*/     # Wasmtime native + .NET bindings
wit/editor-api/                  # WIT interface definitions
examples/                        # Rust example tools
sdk/rust/template/               # Copyable tool template
sample-project/                  # Unity sample project
scripts/build-all-examples.sh    # Local batch build
docs/                            # Architecture & guides
schemas/                         # Exported JSON Schema for AI agents
```

## Install in Your Project

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.fumo.editor-wasm": "file:/path/to/unity-wasm/packages/com.fumo.editor-wasm",
    "com.fumo.wasmtime-dotnet": "file:/path/to/unity-wasm/packages/com.fumo.wasmtime-dotnet",
    "com.fumo.wasmtime": "file:/path/to/unity-wasm/packages/com.fumo.wasmtime"
  }
}
```

Place tools under `Assets/Editor/Tools/<your-tool>/tool.json` + `bin/tool.wasm`.

## Tool Manifest

```json
{
  "id": "com.example.my-tool",
  "name": "My Tool",
  "abi": "editor-api/1",
  "entry": "bin/tool.wasm",
  "exports": {
    "on_init": "on_init",
    "on_shutdown": "on_shutdown",
    "on_menu_click": "on_menu_click"
  }
}
```

## Menu Commands

| Menu | Action |
|------|--------|
| Tools → Wasm Editor → Refresh Tools | Rescan tool.json manifests |
| Tools → Wasm Editor → Run Tool... | Context menu listing all discovered tools (dynamic) |
| Tools → Wasm Editor → Open Tool Shell | Launcher, logs, trap panel |
| Tools → Wasm Editor → Export API Schema | Write `schemas/editor-api.schema.json` |
| Tools → Wasm Editor → Generate Host Bindings | Generate registry + API schema from manifest |
| Tools → Wasm Editor → Verbose Host Import Log | Toggle per-import Console logging |

## License

MIT (Wasmtime components under Apache-2.0 — see package LICENSE files).
