# AssemblyScript Tool Template

Lightweight tools can use AssemblyScript targeting `wasm32-unknown-unknown` with the same import module names as Rust tools.

## Recommended Imports

```typescript
@external("editor_core", "log")
declare function log(level: i32, ptr: i32, len: i32): void;
```

Use `docs/host-api.md` for the full import catalog.

## Notes

- Export `on_init`, `on_shutdown`, `on_menu_click` as `@external("env", ...)` or bare exports depending on your AS config.
- Prefer Rust for heavy asset scanning; AssemblyScript suits naming checks and small validators.
