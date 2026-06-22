# Debugging WASM Editor Tools

## Debug Builds

In `Cargo.toml`:

```toml
[profile.dev]
debug = true
```

`WasmEditorHost` enables `Config.WithDebugInfo(true)` by default.

## Trap Reports

When a guest traps, `WasmEditorHost.LastTrapReport` contains JSON:

- `trap_message`, `trap_code`, WASM stack frames
- `fuel_remaining`
- `host_call_trace` — recent host imports

View in **Tools → Wasm Editor → Open Tool Shell** or Unity Console.

## Source-Level Debugging

Attach VS Code / LLDB to Wasmtime when running with debug info enabled.
You cannot step from WASM into C# host imports — use dual debuggers.

## WebView UI (Phase 3)

When using `WebViewBridge`, use browser DevTools for HTML/CSS/TS debugging separately from WASM.
