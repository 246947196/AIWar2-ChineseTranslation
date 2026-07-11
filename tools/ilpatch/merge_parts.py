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
for i in range(1, 9):
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
        # Part files encode control chars as \\n/\\t (double escape)
        # due to regex JSON unescaping. Convert to actual control chars to match skeleton.
        key = key.replace('\\\\n', '\n').replace('\\\\t', '\t').replace('\\\\r', '\r')
        if val.strip():
            translations[key] = val
            count += 1
    print(f"Part{i}: extracted {count} translations")

# Inject into skeleton
for key, val in translations.items():
    if key in skeleton:
        skeleton[key] = val
    else:
        print(f"Warning: key not found in skeleton: {key[:80]}...")

# Write output without BOM
with open(OUTPUT, "w", encoding="utf-8") as f:
    json.dump(skeleton, f, ensure_ascii=False, indent=2)

# Count
total = len(skeleton)
translated = sum(1 for v in skeleton.values() if v.strip())
print(f"\nTotal: {total}, Translated: {translated}, Remaining: {total - translated}")
print(f"Output: {OUTPUT}")
