# Architecture

## Layers

| Layer | Responsibility |
|-------|----------------|
| `tool.wasm` | Tool business logic (Rust, AssemblyScript, …) |
| `EditorHostBridge` | WIT-aligned host import implementations |
| `WasmEditorHost` | Wasmtime load / call / trap / fuel |
| `HotReloadService` | FileSystemWatcher + debounced reload |
| `WasmEditorRuntime` | Discovery orchestration, menu/shell invoke, lifecycle |
| `ToolWindowShell` | UIElements launcher, logs, trap panel, manifest metadata |

## Data Flow

```
tool.json → ToolDiscoveryService → WasmEditorRuntime → HotReloadService → WasmEditorHost
                                                                          ↓
                                                               Linker.DefineFunction imports
                                                                          ↓
                                                               EditorHostBridge → UnityEditor APIs
                                                                          ↓
                                                               ToolWindowShell (status / log / trap / progress)
```

## Safety Boundaries

- Guest memory traps are caught; the Editor process continues.
- Fuel limits interrupt infinite loops (`TrapCode.OutOfFuel`).
- Store memory limit defaults to 256 MiB.
- Host import exceptions are logged; stale handles return errors instead of NullRef.

## ABI

Host imports use module names `editor_core`, `editor_selection`, `editor_assets`.
Guest exports: `on_init`, `on_shutdown`, `on_menu_click`.

See `wit/editor-api/` for the canonical WIT definitions.
