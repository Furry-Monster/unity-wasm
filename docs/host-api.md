# Host API Reference

## editor_core

| Import | Signature | Description |
|--------|-----------|-------------|
| `log` | `(i32 level, i32 ptr, i32 len)` | Console log. level: 0=info, 1=warning, 2=error |
| `log_error` | `(i32 ptr, i32 len)` | Console error |
| `get_editor_time` | `() -> f64` | `EditorApplication.timeSinceStartup` |
| `show_progress` | `(title, info, f32 progress)` | Progress bar (strings via memory) |
| `clear_progress` | `()` | Clear progress bar |

## editor_selection

| Import | Signature | Description |
|--------|-----------|-------------|
| `get_active_object` | `() -> u64` | Handle to primary selection (0 = none) |
| `get_active_objects_count` | `() -> i32` | Multi-selection count |
| `get_active_object_at` | `(i32 index) -> u64` | Nth selected object handle |
| `get_active_asset_path` | `(out ptr, max len) -> i32` | Bytes written |
| `get_object_name` | `(handle, out ptr, max len) -> i32` | Object name bytes written |

## editor_assets

| Import | Signature | Description |
|--------|-----------|-------------|
| `asset_exists` | `(path ptr, len) -> i32` | 1 if asset exists |
| `find_assets_count` | `(filter, paths) -> i32` | Match count |
| `find_asset_at` | `(filter, paths, index, out) -> i32` | Path bytes written |
| `load_text_asset` | `(path, out) -> i32` | Text bytes written |
| `write_bulk_payload` | `(offset, payload, len, type) -> i32` | FMBO bulk header write |

## Bulk Memory Protocol

Header at offset (12 bytes, little-endian):

- `u32` magic `0x464D4F42` ("FMBO")
- `u16` version `1`
- `u16` type id
- `u32` payload length

Payload follows immediately after the header.

## editor_scene (Tier 3)

| Import | Signature | Description |
|--------|-----------|-------------|
| `get_object_path` | `(handle, out ptr, max len) -> i32` | Hierarchy path bytes written |
| `get_serialized_property` | `(handle, path ptr, path len, out ptr, max len) -> i32` | Serialized property value bytes |
| `get_component_count` | `(handle) -> i32` | Component count on GameObject |
| `get_component_type_at` | `(handle, index, out ptr, max len) -> i32` | Full type name bytes written |

Semantic documentation: `wit/editor-api/scene.wit`. Lowered signatures: `schemas/host-imports.v1.json`.

## Manifest & Codegen

- **Truth source**: `schemas/host-imports.v1.json` — used by Host registry, schema export, and contract verification
- **WIT files**: `wit/editor-api/*.wit` — human-readable API tiers (not enforced at load time)
- **Rust imports**: `python3 scripts/gen-rust-imports.py src/imports.rs` — generates `extern "C"` blocks for all manifest imports
