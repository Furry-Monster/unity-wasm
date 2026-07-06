#!/usr/bin/env python3
"""Parse wasm import section and compare against host-imports manifest."""

from __future__ import annotations

import json
import sys
from pathlib import Path


def read_leb(data: bytes, index: int) -> tuple[int, int]:
    result = 0
    shift = 0
    while True:
        byte = data[index]
        index += 1
        result |= (byte & 0x7F) << shift
        if not (byte & 0x80):
            break
        shift += 7
    return result, index


def skip_limits(data: bytes, index: int) -> int:
    flags = data[index]
    index += 1
    _, index = read_leb(data, index)
    if flags == 1:
        _, index = read_leb(data, index)
    return index


def skip_table_type(data: bytes, index: int) -> int:
    index += 1
    return skip_limits(data, index)


def skip_global_type(data: bytes, index: int) -> int:
    index += 2
    return index


def skip_import_desc(data: bytes, index: int) -> int:
    kind = data[index]
    index += 1
    if kind == 0:
        _, index = read_leb(data, index)
    elif kind == 1:
        index = skip_table_type(data, index)
    elif kind == 2:
        index = skip_limits(data, index)
    elif kind == 3:
        index = skip_global_type(data, index)
    else:
        raise ValueError(f"unsupported import kind {kind}")
    return index


def parse_imports(wasm: bytes) -> list[tuple[str, str]]:
    if wasm[:4] != b"\x00asm":
        raise ValueError("not a wasm module")

    index = 8
    imports: list[tuple[str, str]] = []
    while index < len(wasm):
        section_id = wasm[index]
        index += 1
        size, index = read_leb(wasm, index)
        section_start = index
        if section_id == 2:
            count, index = read_leb(wasm, index)
            for _ in range(count):
                mod_len, index = read_leb(wasm, index)
                module = wasm[index : index + mod_len].decode("utf-8")
                index += mod_len
                name_len, index = read_leb(wasm, index)
                name = wasm[index : index + name_len].decode("utf-8")
                index += name_len
                index = skip_import_desc(wasm, index)
                imports.append((module, name))
        index = section_start + size
    return imports


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    manifest_path = root / "schemas" / "host-imports.v1.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    host_keys = {f"{i['module']}.{i['name']}" for i in manifest["imports"]}

    wasm_paths = [Path(p) for p in sys.argv[1:]] if len(sys.argv) > 1 else []
    if not wasm_paths:
        for path in sorted(root.glob("examples/*/bin/tool.wasm")):
            wasm_paths.append(path)
        for path in sorted(root.glob("tests/contract/*/bin/tool.wasm")):
            wasm_paths.append(path)

    failed = False
    for wasm_path in wasm_paths:
        if not wasm_path.exists():
            print(f"MISSING {wasm_path}")
            failed = True
            continue

        guest = {f"{m}.{n}" for m, n in parse_imports(wasm_path.read_bytes())}
        unknown = sorted(guest - host_keys)
        if unknown:
            print(f"FAIL {wasm_path}: unknown guest imports {unknown}")
            failed = True
        else:
            print(f"OK   {wasm_path} ({len(guest)} imports)")

    missing_on_host = sorted(host_keys)
    print(f"Host manifest defines {len(missing_on_host)} imports.")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
