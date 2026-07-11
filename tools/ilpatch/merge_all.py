import json, os, sys

DLL = sys.argv[1] if len(sys.argv) > 1 else "ArcenAIW2Core"
PART_COUNT = 99  # auto-detect: scan up to 99
HERE = os.path.dirname(os.path.abspath(__file__))

SKELETON = os.path.join(HERE, f"{DLL}.extract.json")
OUTPUT = os.path.join(HERE, f"{DLL}.merged.json")

with open(SKELETON, "r", encoding="utf-8") as f:
    skeleton = json.load(f)

translations = {}
for i in range(1, PART_COUNT + 1):
    part_path = os.path.join(HERE, f"{DLL}.part{i}.json")
    if not os.path.exists(part_path):
        continue
    with open(part_path, "r", encoding="utf-8-sig") as f:
        content = f.read().strip()
    # Wrap fragment in braces to make valid JSON
    if content.startswith("{") and not content.endswith("}"):
        content = content.rstrip(",") + "}"
    elif not content.startswith("{") and not content.endswith("}"):
        content = "{" + content.rstrip(",") + "}"
    elif not content.startswith("{") and content.endswith("}"):
        content = "{" + content
    try:
        part = json.loads(content)
    except json.JSONDecodeError as e:
        print(f"Warning: could not parse {part_path}: {e}")
        continue
    count = 0
    for key, val in part.items():
        if not val.strip():
            continue
        while '\\\\' in key:
            key = key.replace('\\\\', '\\')
        key = key.replace('\\n', '\n').replace('\\t', '\t').replace('\\r', '\r')
        while '\\\\' in val:
            val = val.replace('\\\\', '\\')
        val = val.replace('\\n', '\n').replace('\\t', '\t').replace('\\r', '\r')
        translations[key] = val
        count += 1
    print(f"Part{i}: extracted {count} translations")

injected = 0
for key, val in translations.items():
    if key in skeleton:
        skeleton[key] = val
        injected += 1
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
    old_keys = set(old.keys())
    new_keys = set(skeleton.keys())
    overlap = len(old_keys & new_keys) / max(len(old_keys), len(new_keys)) if old_keys else 1.0
    version_changed = overlap < 0.9
    if lost:
        print(f"\n*** REGRESSION [{DLL}]: {len(lost)} previously translated keys went empty! ***")
        for k in sorted(lost)[:20]:
            print(f"  LOST: {k[:80]}")
        if len(lost) > 20:
            print(f"  ... and {len(lost) - 20} more")
        if version_changed:
            print(f"  → Skeleton changed (game version update), continuing.")
        else:
            print(f"  → Skeleton unchanged — {len(lost)} translation(s) lost due to format mismatch!")
            sys.exit(1)

with open(OUTPUT, "w", encoding="utf-8") as f:
    json.dump(skeleton, f, ensure_ascii=False, indent=2)

import shutil
shutil.copy2(OUTPUT, OLD_MERGED)

total = len(skeleton)
translated = sum(1 for v in skeleton.values() if v.strip())
print(f"\n[{DLL}] Total: {total}, Translated: {translated}, Remaining: {total - translated}")
print(f"Output: {OUTPUT}")
