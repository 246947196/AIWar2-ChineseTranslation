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
                      "plural_display_name", "short_display_name", "custom_NameForLobby",
                      "arbitrary_options", "default_option",
                      "sidebar_text", "chat_text", "full_text", "short_name",
                      "chat_text_2", "full_text_2"}


def is_translatable_attr(name):
    if name in TRANSLATABLE_ATTRS:
        return True
    low = name.lower()
    return "display_name" in low or low == "description" or low == "tooltip"


def strip_text(s):
    return s.strip() if s else ""


def overlay_children(target_parent, oe_parent):
    """Recursively overlay the children of oe_parent onto target_parent.

    Children are matched by (tag, name) when available, then by (tag, id)
    (e.g. <choice_value> has no name but carries an id), and finally by tag
    positionally as a fallback. Only translatable attributes + text are
    overlaid; gameplay attributes (related_int_value, dll_name, ...) are kept
    from the English source. Matching children recurse so arbitrary nesting
    (map_type > map_option > choice_value) is handled correctly.
    """
    se_by_name = {}
    se_by_id = {}
    se_by_tag = {}
    se_index_used = {}
    for c in list(target_parent):
        if not isinstance(c.tag, str):
            continue
        nm = c.get("name")
        if nm is not None:
            se_by_name.setdefault((c.tag, nm), []).append(c)
        cid = c.get("id")
        if cid is not None:
            se_by_id.setdefault((c.tag, cid), []).append(c)
        se_by_tag.setdefault(c.tag, []).append(c)

    for oc in list(oe_parent):
        if not isinstance(oc.tag, str):
            continue
        target_se = None
        nm = oc.get("name")
        cid = oc.get("id")
        if nm is not None:
            lst = se_by_name.get((oc.tag, nm))
            if lst:
                target_se = lst.pop(0)
        if target_se is None and cid is not None:
            lst = se_by_id.get((oc.tag, cid))
            if lst:
                target_se = lst.pop(0)
        if target_se is None:
            # fall back to tag-only positional matching
            tag_list = se_by_tag.get(oc.tag, [])
            idx = se_index_used.get(oc.tag, 0)
            if idx < len(tag_list):
                target_se = tag_list[idx]
                se_index_used[oc.tag] = idx + 1
        if target_se is None:
            # override entry not present in the English source: keep as-is
            target_parent.append(oc)
            continue

        # overlay translatable attributes (display_name / description / tooltip ...)
        for k, v in oc.attrib.items():
            if is_translatable_attr(k) and strip_text(v):
                target_se.set(k, v)
        # overlay text content (only when the override actually provides text)
        oc_text = strip_text(oc.text)
        if oc_text:
            target_se.text = oc.text
        # recurse into deeper levels
        if list(oc):
            overlay_children(target_se, oc)


def overlay_element(target_parent_root, oe):
    """Overlay a single override entry (oe) onto the matching top-level target element."""
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

    # overlay child elements recursively (handles map_option > choice_value, etc.)
    if list(oe):
        overlay_children(match, oe)


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
