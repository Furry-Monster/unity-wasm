# Unity Editor WASM Platform

WebAssembly editor extension platform for **Unity 2022 LTS**. Run Rust/AssemblyScript tools inside a Wasmtime sandbox with crash isolation, hot reload, and a curated Editor Host API.

## Features

- **Wasmtime .NET** runtime (Editor-only, JIT enabled)
- **Curated host API** — Selection, AssetDatabase, logging, progress
- **Hot reload** — rebuild `.wasm` without Domain Reload
- **Structured trap reports** — AI-friendly JSON diagnostics
- **WIT contracts** — `wit/editor-api/` defines the ABI
- **Example tool** — `examples/selection-logger`

## Quick Start

### 1. Open sample project

Open `sample-project/` in Unity **2022.3 LTS**.

### 2. Build example tool

```bash
cd examples/selection-logger
./build.sh
```

### 3. Run in Unity

1. **Tools → Wasm Editor → Refresh Tools**
2. Select any asset or GameObject in the Hierarchy
3. **Tools → Wasm Editor → Run → Selection Logger**
4. Open **Window → Wasm Editor → Tool Shell** for logs

### 4. Hot reload

With Unity open, edit `examples/selection-logger/src/lib.rs` and run `./build.sh` again. The host reloads within ~300ms.

## Project Layout

```
packages/com.fumo.editor-wasm/   # UPM host package
packages/com.fumo.wasmtime*/     # Wasmtime native + .NET bindings
wit/editor-api/                  # WIT interface definitions
examples/selection-logger/       # Rust example tool
sample-project/                  # Unity sample project
sdk/                             # Tool templates
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
  "menu": "Tools/My Tool",
  "exports": {
    "on_menu_click": "on_menu_click"
  }
}
```

## Menu Commands

| Menu | Action |
|------|--------|
| Tools → Wasm Editor → Refresh Tools | Rescan tool.json manifests |
| Tools → Wasm Editor → Open Tool Shell | Log / trap panel |
| Tools → Wasm Editor → Export API Schema | Write `schemas/editor-api.schema.json` |
| Tools → Wasm Editor → Generate Host Bindings | Generate API registry |

## License

MIT (Wasmtime components under Apache-2.0 — see package LICENSE files).
