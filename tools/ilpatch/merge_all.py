import json, os, sys, shutil, glob, re

DLL = sys.argv[1] if len(sys.argv) > 1 else "ArcenAIW2Core"
FORCE = "--force" in sys.argv
HERE = os.path.dirname(os.path.abspath(__file__))
# Adjust for working directory
if not os.path.exists(os.path.join(HERE, "tools/ilpatch")):
    HERE = os.getcwd()

SKELETON = os.path.join(HERE, "tools/ilpatch", f"{DLL}.extract.json")
OUTPUT = os.path.join(HERE, "tools/ilpatch", f"{DLL}.merged.json")

with open(SKELETON, "r", encoding="utf-8") as f:
    skeleton = json.load(f)

# Extract translations from all part files
part_files = sorted(glob.glob(os.path.join(HERE, "tools/ilpatch", f"{DLL}.part*.json")))
translations = {}
for pf in part_files:
    with open(pf, "r", encoding="utf-8") as f:
        content = f.read()
    pattern = re.compile(r'^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*,?\s*$', re.MULTILINE)
    for m in pattern.finditer(content):
        key = m.group(1)
        val = m.group(2)
        while '\\\\' in key:
            key = key.replace('\\\\', '\\')
        key = key.replace('\\n', '\n').replace('\\t', '\t').replace('\\r', '\r')
        if val.strip():
            while '\\\\' in val:
                val = val.replace('\\\\', '\\')
            val = val.replace('\\n', '\n').replace('\\t', '\t').replace('\\r', '\r')
            translations[key] = val

injected = 0
for key, val in translations.items():
    if key in skeleton:
        skeleton[key] = val
        injected += 1
    else:
        print("Warning: key not found in skeleton: " + repr(key[:60]))

# Regression check
OLD_MERGED = OUTPUT + ".bak"
if os.path.exists(OLD_MERGED):
    with open(OLD_MERGED, "r", encoding="utf-8") as f:
        old = json.load(f)
    new_translated = [k for k, v in skeleton.items() if v.strip()]
    old_translated = set(k for k, v in old.items() if v.strip())
    lost = old_translated - set(new_translated)
    old_keys = set(old.keys())
    new_keys = set(skeleton.keys())
    overlap = len(old_keys & new_keys) / max(len(old_keys), len(new_keys)) if old_keys else 1.0
    version_changed = overlap < 0.9
    if lost:
        print(f"\n*** REGRESSION [{DLL}]: {len(lost)} previously translated keys went empty! ***")
        for k in sorted(lost)[:10]:
            print(f"  LOST: {k[:60]}")
        if version_changed:
            print("  -> Skeleton changed (game version update), continuing.")
        elif FORCE:
            print("  -> FORCE mode: recovering lost keys from .bak and continuing.")
            for k in lost:
                if k in old and old[k] and old[k].strip():
                    skeleton[k] = old[k]
                    print(f"  RECOVERED: {k[:60]}")
        else:
            print("  -> Skeleton unchanged. Use --force to recover from .bak and continue.")
            sys.exit(1)

# Write
with open(OUTPUT, "w", encoding="utf-8") as f:
    json.dump(skeleton, f, ensure_ascii=False, indent=2)
shutil.copy2(OUTPUT, OLD_MERGED)

total = len(skeleton)
translated = sum(1 for v in skeleton.values() if v.strip())
print(f"\n[{DLL}] Total: {total}, Translated: {translated}, Remaining: {total - translated}")
print(f"Output: {OUTPUT}")
