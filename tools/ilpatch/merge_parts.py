import json, re, sys, os

SKELETON = os.path.join(os.path.dirname(__file__), "ArcenAIW2Core.extract.json")
PARTS_DIR = os.path.dirname(__file__)
OUTPUT = os.path.join(os.path.dirname(__file__), "ArcenAIW2Core.merged.json")

# Load clean skeleton
with open(SKELETON, "r", encoding="utf-8") as f:
    skeleton = json.load(f)

# Regex: extract all "key": "non-empty value" from part files
part_pattern = re.compile(r'^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*,?\s*$', re.MULTILINE)

translations = {}
for i in range(1, 10):
    part_path = os.path.join(PARTS_DIR, f"ArcenAIW2Core.part{i}.json")
    if not os.path.exists(part_path):
        print(f"Warning: {part_path} not found, skipping")
        continue
    with open(part_path, "r", encoding="utf-8") as f:
        content = f.read()
    count = 0
    for m in part_pattern.finditer(content):
        key = m.group(1)
        val = m.group(2)
        # Part files encode control chars as \n/\t (JSON escape).
        # Convert to actual control chars to match skeleton.
        while '\\\\' in key:
            key = key.replace('\\\\', '\\')
        key = key.replace('\\n', '\n').replace('\\t', '\t').replace('\\r', '\r')
        if val.strip():
            translations[key] = val
            count += 1
    print(f"Part{i}: extracted {count} translations")

# Inject into skeleton (also fix value format)
for key, val in translations.items():
    if key in skeleton:
        while '\\\\' in val:
            val = val.replace('\\\\', '\\')
        val = val.replace('\\n', '\n').replace('\\t', '\t').replace('\\r', '\r')
        skeleton[key] = val
    else:
        print(f"Warning: key not found in skeleton: {key[:80]}...")

# --- Regression check: detect previously translated keys that went silent ---
OLD_MERGED = OUTPUT + ".bak"
if os.path.exists(OLD_MERGED):
    with open(OLD_MERGED, "r", encoding="utf-8") as f:
        old = json.load(f)
    new_translated = [k for k, v in skeleton.items() if v.strip()]
    old_translated = set(k for k, v in old.items() if v.strip())
    lost = old_translated - set(new_translated)

    # Detect whether skeleton was re-extracted (game version change):
    # if old merged's key set differs from current skeleton by >10%, it's a new version.
    old_keys = set(old.keys())
    new_keys = set(skeleton.keys())
    overlap = len(old_keys & new_keys) / max(len(old_keys), len(new_keys)) if old_keys else 1.0
    version_changed = overlap < 0.9

    if lost:
        print(f"\n*** REGRESSION: {len(lost)} previously translated keys went empty! ***")
        for k in sorted(lost)[:20]:
            print(f"  LOST: {k[:80]}")
        if len(lost) > 20:
            print(f"  ... and {len(lost) - 20} more")
        if version_changed:
            print(f"  → Skeleton changed (game version update), continuing.")
        else:
            print(f"  → Skeleton unchanged — {len(lost)} translation(s) lost due to format mismatch!")
            sys.exit(1)

# Write output without BOM.
# Python's json.dump already escapes control chars (\n→\\n, \t→\\t) regardless of ensure_ascii.
with open(OUTPUT, "w", encoding="utf-8") as f:
    json.dump(skeleton, f, ensure_ascii=False, indent=2)

# Also update .bak for next run
import shutil
shutil.copy2(OUTPUT, OLD_MERGED)

# Count
total = len(skeleton)
translated = sum(1 for v in skeleton.values() if v.strip())
print(f"\nTotal: {total}, Translated: {translated}, Remaining: {total - translated}")
print(f"Output: {OUTPUT}")
