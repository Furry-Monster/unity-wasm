#!/usr/bin/env python3
"""Compare host-imports manifest against generated HostImportRegistry.g.cs."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    manifest_path = root / "schemas" / "host-imports.v1.json"
    registry_path = (
        root
        / "packages/com.fumo.editor-wasm/Editor/Generated/HostImportRegistry.g.cs"
    )

    if not manifest_path.exists():
        print(f"MISSING {manifest_path}")
        return 1
    if not registry_path.exists():
        print(f"MISSING {registry_path}")
        return 1

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest_keys = {f"{i['module']}.{i['name']}" for i in manifest["imports"]}

    text = registry_path.read_text(encoding="utf-8")
    registry_keys = set(re.findall(r'new\("([^"]+)", "([^"]+)", \d+\)', text))
    registry_keys = {f"{m}.{n}" for m, n in registry_keys}

    failed = False
    missing_in_registry = sorted(manifest_keys - registry_keys)
    extra_in_registry = sorted(registry_keys - manifest_keys)

    if missing_in_registry:
        print(f"FAIL registry missing manifest imports: {missing_in_registry}")
        failed = True
    if extra_in_registry:
        print(f"FAIL registry has extra imports: {extra_in_registry}")
        failed = True

    manifest_exports = set(manifest.get("exports") or [])
    export_match = re.search(
        r"static readonly string\[\] _exports\s*=\s*\{([^}]*)\};", text, re.S
    )
    registry_exports: set[str] = set()
    if export_match:
        registry_exports = set(re.findall(r'"([^"]+)"', export_match.group(1)))

    if manifest_exports != registry_exports:
        print(f"FAIL export mismatch manifest={sorted(manifest_exports)} registry={sorted(registry_exports)}")
        failed = True

    if not failed:
        print(
            f"OK   manifest/registry parity ({len(registry_keys)} imports, "
            f"{len(registry_exports)} exports)"
        )

    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
