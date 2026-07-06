# Getting Started — WASM Tool Development

Guide for tool authors building Unity Editor extensions with `com.fumo.editor-wasm`.

For a quick demo, see the [README](../README.md). This document covers day-to-day tool development.

---

## 1. Five-Minute Hello Tool

### Prerequisites

- Unity **2022.3 LTS** with `com.fumo.editor-wasm` installed (see README)
- Rust + `wasm32-unknown-unknown`:

```bash
rustup target add wasm32-unknown-unknown
```

### Steps

1. Copy the template:

```bash
cp -r sdk/rust/template examples/my-hello
cd examples/my-hello
```

2. Edit `tool.json` — set unique `id` and `name`:

```json
{
  "id": "com.myteam.my-hello",
  "name": "My Hello",
  "abi": "editor-api/1",
  "entry": "bin/tool.wasm"
}
```

3. Edit `Cargo.toml` — change `name = "my-hello"` and update `build.sh` to copy `my_hello.wasm`.

4. Build:

```bash
chmod +x build.sh
./build.sh
```

5. In Unity:

   - **Tools → Wasm Editor → Refresh Tools**
   - **Tools → Wasm Editor → Run Tool...** — opens a tool picker popup
   - Optional: **Tools → Wasm Editor → Open Tool Shell** for logs, manifest metadata, and trap JSON

   Running a tool does **not** auto-open Tool Shell (logs still appear in the Console).

You should see `Hello Tool: running` (or your customized message) in the Console / Tool Shell.

---

## 2. `tool.json` Reference

Fields map to [ToolManifest.cs](../packages/com.fumo.editor-wasm/Editor/ToolManifest.cs):

| Field | Required | Description |
|-------|----------|-------------|
| `id` | yes | Unique tool ID, e.g. `com.fumo.asset-scanner` |
| `name` | yes | Display name in Run Tool menu and Tool Shell |
| `version` | no | Semver string (default `1.0.0`) |
| `abi` | yes | Host API version, currently `editor-api/1` |
| `entry` | yes | Path to wasm relative to tool root, usually `bin/tool.wasm` |
| `menu` | no | **Not used for Run in M1.** Documentation + `Export Tool Registry` only; launch uses **Run Tool...** picker window |
| `shortcut` | no | Reserved for future shortcut support |
| `exports.on_init` | yes | Wasm export called on load |
| `exports.on_shutdown` | no | Called on unload |
| `exports.on_menu_click` | yes | Called when tool is Run |
| `exports.on_selection_changed` | no | Reserved |

### Directory layout

```
my-tool/
├── tool.json
├── bin/
│   └── tool.wasm
├── Cargo.toml
├── build.sh
└── src/
    └── lib.rs
```

---

## 3. Host Import Quick Reference

Full reference: [host-api.md](host-api.md)

### Rust extern declarations

Generate the full import surface from the repo manifest (recommended):

```bash
python3 scripts/gen-rust-imports.py src/imports.rs
```

Then `mod imports;` and call `imports::log(...)`, etc. Module names **must** match `schemas/host-imports.v1.json`:

```rust
mod imports;

unsafe {
    imports::log(0, msg.as_ptr() as i32, msg.len() as i32);
    let handle = imports::get_active_object();
}
```

Manual `#[link(wasm_import_module = "...")]` blocks are fine for minimal tools; keep names in sync with [host-api.md](host-api.md).

### Required wasm exports

```rust
#[no_mangle]
pub extern "C" fn on_init() -> i32 { 0 }

#[no_mangle]
pub extern "C" fn on_shutdown() {}

#[no_mangle]
pub extern "C" fn on_menu_click() { /* ... */ }
```

Strings are passed as `(ptr, len)` into guest linear memory. Search paths for asset APIs use `\0` separation, e.g. `b"Assets\0"`.

---

## 4. Hot Reload Workflow

The host watches `bin/tool.wasm` with a **300ms debounce**. Rebuild while Unity is open — no Domain Reload needed.

```bash
./build.sh
```

### Optional: auto-rebuild with cargo-watch

```bash
cargo install cargo-watch
CARGO_TARGET_DIR=target cargo watch -x 'build --target wasm32-unknown-unknown --release' -s './build.sh'
```

### Build all repo examples

```bash
./scripts/build-all-examples.sh
```

---

## 5. Trap Debugging

If guest code traps (panic, invalid import, out of fuel):

1. Editor stays alive — the trap is isolated to the WASM module
2. Open **Tool Shell** → **Last Trap** section shows JSON diagnostics
3. Click **Copy JSON** to paste into an issue or AI assistant
4. Fix Rust code → `./build.sh` → auto reload

See also [debugging.md](debugging.md).

---

## 6. Tool Placement & Discovery

[ToolDiscoveryService](../packages/com.fumo.editor-wasm/Editor/ToolDiscoveryService.cs) scans these roots:

| Path | Purpose |
|------|---------|
| `<project>/Packages/**/tool.json` | UPM packages |
| `Assets/Editor/Tools/**/tool.json` | Project-local tools |
| `~/UnityEditorTools/**/tool.json` | User-global tools |
| `<repo>/examples/**/tool.json` | Repo examples (sample-project) |

### Add a custom search root (advanced)

From Editor C# (platform code only):

```csharp
ToolDiscoveryService.AddSearchRoot("/path/to/tools");
WasmEditorRuntime.RefreshTools();
```

Tool authors normally place tools in one of the default paths above.

---

## 7. Common Errors

| Symptom | Cause | Fix |
|---------|-------|-----|
| Tool not in Run menu | Missing wasm or bad `tool.json` | Run `./build.sh`; check Console for skip warnings |
| `WASM not found` | `entry` path wrong | Ensure `bin/tool.wasm` exists relative to `tool.json` |
| Instantiate / import error | Wrong `#[link(wasm_import_module = "...")]` | Match [host-api.md](host-api.md) module names |
| Trap on Run | Guest panic or bad memory access | Tool Shell → Copy JSON; fix Rust |
| Hot reload not firing | Wrong output path | `build.sh` must copy to `bin/tool.wasm` |
| Duplicate tool ID | Two manifests share `id` | Use unique `id` per tool |
| Tool skipped on load | Wrong `abi` | Must be `editor-api/1` (enforced in M2) |
| Unknown import at load | Guest imports not in manifest | Run `./scripts/verify-contracts.sh`; align with host-imports.v1.json |

**ABI field:** `tool.json.abi` must be `editor-api/1`. Other values are rejected at load time.

---

## 8. Examples

| Tool | Path | Demonstrates |
|------|------|--------------|
| Selection Logger | `examples/selection-logger` | Tier 1 selection API + generated imports |
| Asset Scanner | `examples/asset-scanner` | Tier 2 assets + progress bar |
| Prefab Inspector Lite | `examples/prefab-inspector-lite` | Tier 3 scene / component introspection |
| Hello Tool (template) | `sdk/rust/template` | Minimal starting point |

> **Performance note (M1):** Asset Scanner walks assets on the Editor main thread and may freeze briefly on large projects. This is expected for M1; batched scanning is planned for M3 ([roadmap](roadmap.md)).

---

## Next Steps

- [architecture.md](architecture.md) — platform overview
- [roadmap.md](roadmap.md) — M2 WIT/codegen, M3.5 UI platform
- [ai-agent-integration.md](ai-agent-integration.md) — Schema export for AI tools
