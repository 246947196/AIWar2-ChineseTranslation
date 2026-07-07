"""
arcenui AssetBundle 汉化补丁工具

用法:
  python patch_arcenui.py info      查看 bundle 内文本统计
  python patch_arcenui.py extract   从原始 bundle 提取文本到 JSON
  python patch_arcenui.py patch     根据 JSON 翻译替换文本，输出汉化版 bundle
"""

import UnityPy
import json
import sys
import os
import re
import time

GAME_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUNDLE_SRC = os.path.join(GAME_DIR, "AssetBundles_Win", "arcenui")
BUNDLE_OUT = os.path.join(GAME_DIR, "BepInEx", "plugins", "ChineseTranslation", "AssetBundles_Win", "arcenui")
JSON_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "arcenui_translations.json")


def _safe(text, maxlen=120):
    s = text.replace("\n", "\\n")[:maxlen]
    return s.encode("ascii", errors="replace").decode("ascii")


def get_all_objects(env):
    """Get all objects from all serialized files in the bundle."""
    all_objs = []
    for name, bf in env.files.items():
        if hasattr(bf, "files"):
            for sname, sf in bf.files.items():
                if hasattr(sf, "objects"):
                    for pid, obj in sf.objects.items():
                        all_objs.append(obj)
    return all_objs


def get_text_objects(objs):
    """Get all MonoBehaviours with non-empty m_text."""
    results = []
    for obj in objs:
        if obj.type.name == "MonoBehaviour":
            try:
                data = obj.read()
                if hasattr(data, "m_text") and isinstance(data.m_text, str):
                    results.append((obj, data))
            except Exception:
                pass
    return results


def load_env(path=None):
    if path is None:
        path = BUNDLE_SRC
    if not os.path.exists(path):
        print(f"[ERROR] Bundle not found: {path}")
        sys.exit(1)
    print(f"Loading: {path} ({os.path.getsize(path)/1024/1024:.0f} MB)")
    t0 = time.time()
    env = UnityPy.load(path)
    print(f"  done in {time.time()-t0:.1f}s")
    return env


def cmd_info():
    env = load_env()
    objs = get_all_objects(env)
    text_objs = get_text_objects(objs)
    print(f"\nText components: {len(text_objs)}")

    texts = {}
    for obj, data in text_objs:
        txt = data.m_text
        if txt not in texts:
            texts[txt] = 0
        texts[txt] += 1

    print(f"Unique strings: {len(texts)}")
    print(f"\n{'Count':>5} | String")
    print("-" * 60)
    for txt, count in sorted(texts.items(), key=lambda x: (-x[1], x[0])):
        print(f"{count:>5} | {_safe(txt, 100)}")


def cmd_extract():
    env = load_env()
    objs = get_all_objects(env)
    text_objs = get_text_objects(objs)

    texts = {}
    for obj, data in text_objs:
        txt = data.m_text
        if txt not in texts:
            texts[txt] = 0
        texts[txt] += 1

    existing = {}
    if os.path.exists(JSON_PATH):
        with open(JSON_PATH, encoding="utf-8") as f:
            existing = json.load(f)

    translations = existing.get("translations", {})
    no_translate = set(existing.get("no_translate", []))

    for k in list(translations.keys()):
        if translations[k] and k in no_translate:
            no_translate.discard(k)

    for txt in sorted(texts.keys(), key=lambda x: (len(x), x)):
        txt_norm = txt.replace('\r\n', '\n').replace('\r', '\n')
        if txt_norm in translations or txt_norm in no_translate:
            continue
        if _should_skip(txt):
            no_translate.add(txt_norm)
        else:
            translations[txt_norm] = ""

    output = {
        "translations": dict(sorted(translations.items(), key=lambda x: (len(x[0]), x[0]))),
        "no_translate": sorted(no_translate, key=lambda x: (len(x), x))
    }

    with open(JSON_PATH, "w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    filled = sum(1 for v in translations.values() if v)
    print(f"\nWritten {JSON_PATH}")
    print(f"  translations: {len(translations)} ({filled} filled)")
    print(f"  no_translate: {len(no_translate)}")

    empty = [k for k, v in translations.items() if not v]
    if empty:
        print(f"\nUntranslated ({len(empty)}):")
        for t in empty:
            print(f"  {_safe(t, 80)}")


def _should_skip(txt):
    if not txt or len(txt) <= 1:
        return True
    if txt.startswith("\n"):
        return True
    if re.match(r'^[\d,%\-.+:k/s ]+$', txt.strip()):
        return True
    if re.match(r'^<color=.*', txt):
        return True
    return False


def cmd_patch():
    if not os.path.exists(JSON_PATH):
        print(f"[ERROR] Translation file not found: {JSON_PATH}")
        print("  Run 'extract' first.")
        sys.exit(1)

    with open(JSON_PATH, encoding="utf-8") as f:
        data = json.load(f)

    translations = {}
    for k, v in data.get("translations", {}).items():
        translations[k.replace('\r\n', '\n').replace('\r', '\n')] = v
    no_translate = set()
    for k in data.get("no_translate", []):
        no_translate.add(k.replace('\r\n', '\n').replace('\r', '\n'))

    env = load_env()
    objs = get_all_objects(env)
    text_objs = get_text_objects(objs)

    patched = 0
    skipped = 0
    not_found = set()

    for obj, obj_data in text_objs:
        txt = obj_data.m_text
        # Normalize newlines for lookup, but keep original for patching
        txt_norm = txt.replace('\r\n', '\n').replace('\r', '\n')
        
        if txt_norm in translations and translations[txt_norm]:
            obj_data.m_text = translations[txt_norm]
            obj.patch(obj_data)
            patched += 1
        elif txt_norm in no_translate:
            skipped += 1
        elif txt_norm in translations and not translations[txt_norm]:
            not_found.add(txt_norm)
        else:
            not_found.add(txt_norm)

    if not_found:
        print(f"\nWARNING: {len(not_found)} untranslated strings:")
        for t in sorted(not_found, key=lambda x: (len(x), x)):
            status = " (empty in JSON)" if (t in translations and not translations[t]) else ""
            print(f"  {_safe(t, 100)}{status}")

    out_dir = os.path.dirname(BUNDLE_OUT)
    os.makedirs(out_dir, exist_ok=True)

    print(f"\nSaving to: {BUNDLE_OUT}")
    t0 = time.time()

    out_dir = os.path.dirname(BUNDLE_OUT)
    for name, bf in env.files.items():
        bf.mark_changed()
    env.save(pack="none", out_path=out_dir)

    elapsed = time.time() - t0
    if os.path.exists(BUNDLE_OUT):
        size = os.path.getsize(BUNDLE_OUT)
        print(f"Done! {elapsed:.0f}s, {size/1024/1024:.0f} MB")
        print(f"Patched: {patched}, Skipped: {skipped}")
    else:
        print("FAILED - output not created")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "info":
        cmd_info()
    elif cmd == "extract":
        cmd_extract()
    elif cmd == "patch":
        cmd_patch()
    else:
        print(f"Unknown: {cmd}")
        print(__doc__)
