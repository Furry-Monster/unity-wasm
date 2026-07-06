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

## Verbose Host Import Log

**Tools → Wasm Editor → Verbose Host Import Log** toggles per-call logging of host imports to the Unity Console. Each line includes module, function, and key arguments (when available).

Use this when diagnosing unexpected host behavior or verifying which imports a tool invokes during a Run.

## Module Inspect (Tool Shell)

In **Tools → Wasm Editor → Open Tool Shell**, click **Inspect** on a tool row to expand:

- **Imports** — wasm import module/name pairs from the loaded module
- **Exports** — guest export names
- **Recent host trace** — last few host import calls recorded for that tool instance

The tool must be loaded at least once (Run or Refresh Tools) before inspect data appears.

## Contract Verification (local)

From the repo root:

```bash
./scripts/verify-contracts.sh
```

This builds examples and contract probes, then parses each `tool.wasm` import section against `schemas/host-imports.v1.json`. No CI required — run before publishing host or guest changes.

## ABI Mismatch

If `tool.json` declares an unsupported `abi` (not `editor-api/1`), `WasmEditorHost` refuses to load the module and logs a clear error. Fix the manifest or upgrade the host package.
