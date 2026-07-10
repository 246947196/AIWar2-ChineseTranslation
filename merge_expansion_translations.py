#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
merge_expansion_translations.py

For every expansion translation override under:
  AIWar2_ChineseTranslation\GameData\Configuration\Expansions\<exp>\GameData\Configuration\...
produce a COMPLETE Chinese file by overlaying the translated entries onto the
game's English source file (matched by element tag + name attribute), and write
the result back into the real expansion config directory:
  <gameDir>\Expansions\<exp>\GameData\Configuration\...

This guarantees:
  * no game content is lost (the English source is the authoritative complete file)
  * translated text wins (the file replaces the English one at the same path)
  * untranslated entries keep their English text (still functional)

Only translatable attributes / text are overlaid; everything else (gameplay
values, dll_name, type_name, etc.) is preserved from the English source.
"""
import os
import sys
import xml.etree.ElementTree as ET

GAME_DIR = "D:/Steam/steamapps/common/AI War 2"
MOD_DIR = os.path.join(GAME_DIR, "AIWar2_ChineseTranslation")
EXP_OVERRIDE_ROOT = os.path.join(MOD_DIR, "GameData", "Configuration", "Expansions")

TRANSLATABLE_ATTRS = {"display_name", "description", "tooltip", "default_display_name",
                      "plural_display_name", "short_display_name", "custom_NameForLobby"}


def is_translatable_attr(name):
    if name in TRANSLATABLE_ATTRS:
        return True
    low = name.lower()
    return low.endswith("display_name") or low == "description" or low == "tooltip"


def strip_text(s):
    return s.strip() if s else ""


def overlay_element(target_parent_root, oe):
    """Overlay a single override entry (oe) onto the matching target element."""
    oe_name = oe.get("name")
    tag = oe.tag
    match = None
    for se in target_parent_root:
        if se.tag != tag:
            continue
        if oe_name is not None:
            if se.get("name") == oe_name:
                match = se
                break
        else:
            # match by full attribute set
            if dict(se.attrib) == dict(oe.attrib):
                match = se
                break
    if match is None:
        # new entry not present in English source: append as-is
        target_parent_root.append(oe)
        return

    # overlay translatable attributes
    for k, v in oe.attrib.items():
        if is_translatable_attr(k) and strip_text(v):
            match.set(k, v)

    # overlay inner text (e.g. journal/tip bodies)
    oe_text = strip_text(oe.text)
    if oe_text:
        match.text = oe.text

    # overlay child elements, matching by tag + name attribute when available,
    # falling back to tag-only with index tracking (avoids all overrides landing
    # on the first child when multiple share the same tag, e.g. multiple <system>).
    oe_children = list(oe)
    if oe_children:
        se_children = list(match)
        se_by_tag = {}
        se_by_tag_name = {}
        se_index_used = {}
        for c in se_children:
            se_by_tag.setdefault(c.tag, []).append(c)
            cname = c.get("name")
            if cname is not None:
                key = (c.tag, cname)
                se_by_tag_name.setdefault(key, []).append(c)
        for oc in oe_children:
            oc_name = oc.get("name")
            target_se = None
            named = []
            if oc_name is not None:
                key = (oc.tag, oc_name)
                named = se_by_tag_name.get(key, [])
                if named:
                    target_se = named.pop(0)
            if target_se is None:
                # fall back to tag-only, using next unused index
                tag_list = se_by_tag.get(oc.tag, [])
                idx = se_index_used.get(oc.tag, 0)
                while idx < len(tag_list) and tag_list[idx] in named:
                    idx += 1
                if idx < len(tag_list):
                    target_se = tag_list[idx]
                    se_index_used[oc.tag] = idx + 1
                elif tag_list:
                    target_se = tag_list[0]
            if target_se is not None:
                # only overlay translatable attributes (not name, category, gameplay values)
                for ck, cv in oc.attrib.items():
                    if is_translatable_attr(ck) and strip_text(cv):
                        target_se.set(ck, cv)
                # overlay text content
                oc_text_val = strip_text(oc.text)
                if oc_text_val:
                    target_se.text = oc.text
                # overlay nested children recursively (one level is enough for journals)
                oc_grand = list(oc)
                if oc_grand:
                    cand_grand = list(target_se)
                    g_by_tag = {}
                    for g in cand_grand:
                        g_by_tag.setdefault(g.tag, []).append(g)
                    for og in oc_grand:
                        if og.tag in g_by_tag:
                            g_by_tag[og.tag][0].text = og.text
                        else:
                            target_se.append(og)
            else:
                match.append(oc)


def main():
    if not os.path.isdir(EXP_OVERRIDE_ROOT):
        print("No expansion overrides found, nothing to do.")
        return

    count = 0
    for dirpath, _dirnames, filenames in os.walk(EXP_OVERRIDE_ROOT):
        for fn in filenames:
            if not fn.lower().endswith(".xml"):
                continue
            ov_path = os.path.join(dirpath, fn)
            rel = os.path.relpath(ov_path, EXP_OVERRIDE_ROOT)
            # rel looks like: 3_The_Neinzul_Abyss\GameData\Configuration\AIWar2GalaxySetting\NA_GalaxySettings.xml
            target_path = os.path.join(GAME_DIR, "Expansions", rel)

            try:
                oe_tree = ET.parse(ov_path)
            except Exception as e:
                print(f"  SKIP (override parse error) {rel}: {e}")
                continue
            oe_root = oe_tree.getroot()

            if not os.path.exists(target_path):
                # brand new content: just copy the override into place
                os.makedirs(os.path.dirname(target_path), exist_ok=True)
                import shutil
                shutil.copy2(ov_path, target_path)
                count += 1
                print(f"  NEW  {rel}")
                continue

            try:
                se_tree = ET.parse(target_path)
            except Exception as e:
                print(f"  SKIP (source parse error) {rel}: {e}")
                continue
            se_root = se_tree.getroot()

            for oe in list(oe_root):
                if oe.tag is not ET.Comment and oe.tag is not ET.PI:
                    overlay_element(se_root, oe)

            os.makedirs(os.path.dirname(target_path), exist_ok=True)
            # write UTF-8 with BOM to match the game's other config files
            with open(target_path, "wb") as f:
                f.write(b"\xef\xbb\xbf")
                f.write(b'<?xml version="1.0" encoding="utf-8"?>\n')
                f.write(ET.tostring(se_root, encoding="utf-8"))
            count += 1
            print(f"  MERGED {rel}")

    print(f"\nExpansion translation merge complete: {count} file(s) processed.")


if __name__ == "__main__":
    main()
