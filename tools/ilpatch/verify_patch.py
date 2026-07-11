# Verify which translations in merged.json are already applied to a DLL.
# Usage: python verify_patch.py <dll> <merged.json>
#
# Uses binary search on the DLL file to detect Chinese values that
# ilpatch extract/inspect miss due to LooksTranslatable or Console.WriteLine issues.

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

    # Read DLL raw bytes for binary search
    with open(dll, "rb") as f:
        dll_bytes = f.read()

    # Get fresh extract (filtered by LooksTranslatable) for comparison
    ilpatch = os.path.join(os.path.dirname(__file__), "bin", "Release", "net8.0", "ilpatch.exe")
    tmp = os.path.join(tempfile.gettempdir(), "verify_fresh_extract.json")
    result = subprocess.run([ilpatch, "extract", dll, tmp], capture_output=True)
    if result.returncode != 0:
        print(f"ERROR: ilpatch extract failed: {result.stderr.decode('utf-8', errors='replace')}")
        sys.exit(1)

    with open(tmp, "r", encoding="utf-8") as f:
        fresh = json.load(f)
    fresh_keys = set(fresh.keys())

    pending = []
    applied = []
    applied_hidden = []  # Chinese value found by binary search, not by extract
    missing = []

    for key, val in merged.items():
        if not val.strip():
            continue
        if key in fresh_keys:
            pending.append(key)
        elif val in fresh_keys:
            applied.append(key)
        else:
            # Binary search for the Chinese value in the DLL (#US heap uses UTF-16LE)
            val_bytes = val.encode("utf-16-le")
            if val_bytes in dll_bytes:
                applied_hidden.append(key)
            else:
                missing.append(key)

    total = len(pending) + len(applied) + len(applied_hidden) + len(missing)

    print(f"DLL: {os.path.basename(dll)} ({len(dll_bytes)} bytes)")
    print(f"  Fresh extract (filtered): {len(fresh)} keys")
    print(f"  Merged dict:              {len(merged)} keys, {total} translated")
    print()

    print("=" * 60)
    print(f"  {'Status':<30} {'Count':>6}")
    print("=" * 60)
    print(f"  {'pending (English key in extract)':<30} {len(pending):>6}")
    print(f"  {'applied (Chinese in extract)':<30} {len(applied):>6}")
    print(f"  {'applied (Chinese in DLL binary)':<30} {len(applied_hidden):>6}")
    print(f"  {'missing (not found in DLL)':<30} {len(missing):>6}")
    print("=" * 60)
    print(f"  {'total':<30} {total:>6}")
    print()

    if pending:
        print(f"PENDING ({len(pending)} need patch):")
        for k in sorted(pending)[:5]:
            print(f"  {k[:80]!r}")
        if len(pending) > 5:
            print(f"  ... and {len(pending) - 5} more")
        print()

    if applied_hidden:
        print(f"APPLIED (hidden from extract, {len(applied_hidden)}):")
        for k in sorted(applied_hidden)[:5]:
            v = merged[k]
            # Show why it was hidden
            has_ascii_letter = any(c.isascii() and c.isalpha() for c in v)
            has_space = ' ' in v
            print(f"  val={v[:60]!r}  (letter={has_ascii_letter}, space={has_space})")
        if len(applied_hidden) > 5:
            print(f"  ... and {len(applied_hidden) - 5} more")
        print()

    if missing:
        print(f"MISSING ({len(missing)}):")
        for k in sorted(missing)[:10]:
            v = merged[k]
            print(f"  key={k[:80]!r}")
            print(f"  val={v[:80]!r}")
        if len(missing) > 10:
            print(f"  ... and {len(missing) - 10} more")
        print()

    status = "OK" if not pending and not missing else "PARTIAL" if not missing else "ISSUES"
    print(f"{status}: {len(applied) + len(applied_hidden)} applied, {len(pending)} pending, {len(missing)} missing.")

if __name__ == "__main__":
    main()
