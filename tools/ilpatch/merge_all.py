import json, re, os, sys

DLL = sys.argv[1] if len(sys.argv) > 1 else "ArcenAIW2Core"
PART_COUNT = 8 if DLL == "ArcenAIW2Core" else 6
HERE = os.path.dirname(os.path.abspath(__file__))

SKELETON = os.path.join(HERE, f"{DLL}.extract.json")
OUTPUT = os.path.join(HERE, f"{DLL}.merged.json")

with open(SKELETON, "r", encoding="utf-8") as f:
    skeleton = json.load(f)

part_pattern = re.compile(r'^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*,?\s*$', re.MULTILINE)

translations = {}
for i in range(1, PART_COUNT + 1):
    part_path = os.path.join(HERE, f"{DLL}.part{i}.json")
    if not os.path.exists(part_path):
        print(f"Warning: {part_path} not found, skipping")
        continue
    with open(part_path, "r", encoding="utf-8") as f:
        content = f.read()
    count = 0
    for m in part_pattern.finditer(content):
        key = m.group(1)
        val = m.group(2)
        if val.strip():
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

with open(OUTPUT, "w", encoding="utf-8") as f:
    json.dump(skeleton, f, ensure_ascii=False, indent=2)

total = len(skeleton)
translated = sum(1 for v in skeleton.values() if v.strip())
print(f"\n[{DLL}] Total: {total}, Translated: {translated}, Remaining: {total - translated}")
print(f"Output: {OUTPUT}")
