# AI Agent Integration

## API Schema

Export via **Tools → Wasm Editor → Export API Schema** or use the committed file:

`schemas/editor-api.schema.json`

Each entry describes a host import as a tool definition for LLM function calling.

## Trap-Driven Fix Loop

1. Tool traps during `on_menu_click`
2. Host captures `TrapReport.ToJson()`
3. `SelfHealingLoop.BuildFixRequest()` packages context for an agent
4. Agent patches guest source → `build.sh` → hot reload

```csharp
var request = SelfHealingLoop.BuildFixRequest(manifest, host.LastTrapReport);
SelfHealingLoop.WriteFixRequest(request);
```

## Tool Registry

After **Refresh Tools**, discovered manifests are loaded into `WasmEditorRuntime.Tools`.
Agents should read each tool's `id`, `abi`, and export map before invoking menu actions.

## Prompt Hint

> You control Unity Editor through WASM host imports listed in editor-api.schema.json.
> Guest code must export on_init / on_menu_click and import editor_core / editor_selection.
> Never assume direct UnityEditor C# access from guest code.
