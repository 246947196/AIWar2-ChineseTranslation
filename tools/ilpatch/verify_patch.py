# Verify which translations in merged.json are already applied to a DLL.
# Usage: python verify_patch.py <dll> <merged.json>
#
# 3-layer verification:
# 1. ilpatch extract (LooksTranslatable filter) -> visible English/Chinese keys
# 2. ilpatch inspect raw bytes (system encoding) -> cross-check English key NOT in DLL
# 3. UTF-16LE binary search in DLL -> detect Chinese values invisible to extract

import json, subprocess, sys, os, tempfile

def main():
    if len(sys.argv) < 3:
        print("Usage: python verify_patch.py <dll> <merged.json>")
        sys.exit(1)

    dll = sys.argv[1]
    merged_path = sys.argv[2]

    for path in [dll, merged_path]:
        if not os.path.exists(path):
            print(f"ERROR: not found: {path}")
            sys.exit(1)

    with open(merged_path, "r", encoding="utf-8") as f:
        merged = json.load(f)

    with open(dll, "rb") as f:
        dll_bytes = f.read()

    ilpatch = os.path.join(os.path.dirname(__file__), "bin", "Release", "net8.0", "ilpatch.exe")

    # Layer 1: fresh extract (LooksTranslatable filter)
    tmp = os.path.join(tempfile.gettempdir(), "verify_fresh_extract.json")
    result = subprocess.run([ilpatch, "extract", dll, tmp], capture_output=True)
    if result.returncode != 0:
        print(f"ERROR: ilpatch extract failed: {result.stderr.decode('utf-8', errors='replace')}")
        sys.exit(1)
    with open(tmp, "r", encoding="utf-8") as f:
        fresh = json.load(f)
    fresh_keys = set(fresh.keys())

    # Layer 2: raw inspect bytes (search for English keys in system-encoded output)
    r = subprocess.run([ilpatch, "inspect", dll], capture_output=True)
    inspect_bytes = r.stdout

    pending = []
    applied_visible = []
    applied_hidden = []
    missing = []

    for key, val in merged.items():
        if not val.strip():
            continue
        if key in fresh_keys:
            pending.append(key)
            continue
        if val in fresh_keys:
            applied_visible.append(key)
            continue

        # Key not in extract -> either patched or removed.
        # Cross-check: is the English key still in inspect output (raw bytes)?
        # inspect outputs in system encoding (GBK on Chinese Windows).
        key_bytes = key.encode("utf-8")  # utf-8 because key is English ASCII
        if key_bytes in inspect_bytes:
            # English key still in DLL as ldstr -> should have been patched but wasn't
            missing.append(key)
            continue

        # English not in DLL -> value should be there. Check via UTF-16LE binary search.
        val_bytes = val.encode("utf-16-le")
        if val_bytes in dll_bytes:
            applied_hidden.append(key)
        else:
            missing.append(key)

    total = len(pending) + len(applied_visible) + len(applied_hidden) + len(missing)

    print(f"DLL: {os.path.basename(dll)} ({len(dll_bytes)} bytes)")
    print(f"  Fresh extract: {len(fresh)} keys")
    print(f"  Merged dict:   {len(merged)} keys, {total} translated")
    print()

    print("=" * 60)
    print(f"  {'Status':<30} {'Count':>6}")
    print("=" * 60)
    print(f"  {'pending (English in extract)':<30} {len(pending):>6}")
    print(f"  {'applied (Chinese in extract)':<30} {len(applied_visible):>6}")
    print(f"  {'applied (Chinese in DLL binary)':<30} {len(applied_hidden):>6}")
    print(f"  {'missing':<30} {len(missing):>6}")
    print("=" * 60)
    print(f"  {'total':<30} {total:>6}")
    print()

    if pending:
        print(f"PENDING ({len(pending)}):")
        for k in sorted(pending)[:5]:
            print(f"  {k[:80]!r}")
        if len(pending) > 5:
            print(f"  ... and {len(pending) - 5} more")
        print()

    if applied_hidden:
        print(f"APPLIED hidden ({len(applied_hidden)} — in DLL but filtered by extract):")
        for k in sorted(applied_hidden)[:3]:
            print(f"  {k[:80]!r}")
        if len(applied_hidden) > 3:
            print(f"  ... and {len(applied_hidden) - 3} more")
        print()

    if missing:
        print(f"MISSING ({len(missing)}):")
        for k in sorted(missing)[:5]:
            print(f"  {k[:80]!r}")
        if len(missing) > 5:
            print(f"  ... and {len(missing) - 5} more")
        print()

    s = "OK" if not pending and not missing else "PARTIAL" if not missing else "ISSUES"
    print(f"{s}: {len(applied_visible)+len(applied_hidden)} applied, {len(pending)} pending, {len(missing)} missing.")

if __name__ == "__main__":
    main()
