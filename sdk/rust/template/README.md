# Rust WASM Tool Template

Copy this directory to start a new Unity Editor WASM tool.

## Prerequisites

- Rust stable + `wasm32-unknown-unknown` target:

```bash
rustup target add wasm32-unknown-unknown
```

## Quick Start

1. Copy this folder:

```bash
cp -r sdk/rust/template examples/my-tool
cd examples/my-tool
```

2. Edit `tool.json` — change `id`, `name`, and `menu`.

3. Edit `Cargo.toml` — change the `name` field (hyphens become underscores in the `.wasm` filename).

4. Build:

```bash
chmod +x build.sh
./build.sh
```

5. In Unity: **Tools → Wasm Editor → Refresh Tools → Run Tool...**

## Important Notes

- **`CARGO_TARGET_DIR=target`** — `build.sh` sets this so output stays inside the tool directory. Without it, Cargo may write to a global target dir outside the repo.
- **Wasm output name** — crate `hello-tool` produces `hello_tool.wasm`. Update `build.sh` if you rename the crate.
- **Import module names** — must match Host API: `editor_core`, `editor_selection`, `editor_assets`. See [docs/host-api.md](../../docs/host-api.md).
- **Exports** — `on_init`, `on_shutdown`, `on_menu_click` are required for menu-driven tools.

## Hot Reload

With Unity Editor open, rebuild and the host reloads within ~300ms:

```bash
./build.sh
```

Optional: use `cargo-watch` for automatic rebuilds:

```bash
cargo install cargo-watch
CARGO_TARGET_DIR=target cargo watch -x 'build --target wasm32-unknown-unknown --release' -s './build.sh'
```

## Tool Placement

| Location | Use case |
|----------|----------|
| `examples/<name>/` | Repo examples |
| `Assets/Editor/Tools/<name>/` | Project-specific tools |
| `~/UnityEditorTools/<name>/` | Personal global tools |

Each tool directory needs `tool.json` + `bin/tool.wasm`.
