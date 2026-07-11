# Verify which translations in merged.json are already applied to a DLL.
# Usage: python verify_patch.py <dll> <merged.json>
#
# Uses ilpatch dump-ldstr to get ALL unique ldstr as clean JSON
# (no Console.WriteLine fragmentation, no encoding issues, no LooksTranslatable filter).

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

    ilpatch = os.path.join(os.path.dirname(__file__), "bin", "Release", "net8.0", "ilpatch.exe")

    # Get ALL unique ldstr from DLL via dump-ldstr (clean JSON, no fragmentation)
    tmp = os.path.join(tempfile.gettempdir(), "verify_all_ldstr.json")
    result = subprocess.run([ilpatch, "dump-ldstr", dll, tmp], capture_output=True)
    if result.returncode != 0:
        print(f"ERROR: ilpatch dump-ldstr failed: {result.stderr.decode('utf-8', errors='replace')}")
        sys.exit(1)

    with open(tmp, "r", encoding="utf-8") as f:
        all_ldstr = set(json.load(f))

    with open(merged_path, "r", encoding="utf-8") as f:
        merged = json.load(f)

    pending = []
    applied = []
    missing = []

    for key, val in merged.items():
        if not val.strip():
            continue
        if key in all_ldstr:
            pending.append(key)
        elif val in all_ldstr:
            applied.append(key)
        else:
            missing.append(key)

    total = len(pending) + len(applied) + len(missing)

    print(f"DLL: {os.path.basename(dll)} ({os.path.getsize(dll)} bytes)")
    print(f"  Unique ldstr: {len(all_ldstr)}")
    print(f"  Merged dict:  {len(merged)} keys, {total} translated")
    print()

    print("=" * 60)
    print(f"  {'Status':<20} {'Count':>6}")
    print("=" * 60)
    print(f"  {'pending':<20} {len(pending):>6}  — English key still in DLL")
    print(f"  {'applied':<20} {len(applied):>6}  — Chinese value in DLL")
    print(f"  {'missing':<20} {len(missing):>6}  — key/value not found in DLL")
    print("=" * 60)
    print(f"  {'total':<20} {total:>6}")
    print()

    if pending:
        print(f"PENDING ({len(pending)}):")
        for k in sorted(pending)[:5]:
            print(f"  {k[:80]!r}")
        if len(pending) > 5:
            print(f"  ... and {len(pending) - 5} more")
        print()

    if missing:
        print(f"MISSING ({len(missing)}):")
        for k in sorted(missing)[:5]:
            v = merged[k]
            print(f"  key={k[:80]!r}")
            print(f"  val={v[:80]!r}")
        if len(missing) > 5:
            print(f"  ... and {len(missing) - 5} more")
        print()

    s = "OK" if not pending and not missing else "PARTIAL" if not pending else "ISSUES"
    print(f"{s}: {len(applied)} applied, {len(pending)} pending, {len(missing)} missing.")

if __name__ == "__main__":
    main()
