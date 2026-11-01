#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""XML 守卫：校验 / 自动修复汉化项目里的 XML。

背景：游戏用 .NET XmlDocument 解析配置，属性值里出现裸 `<` `>` 或半角 `"`
会直接抛 XmlException，整个文件被跳过 —— 该模组/实体条目全部丢失，
表现为"文本变回英文、单位消失"。这类破损在翻译过程中极易产生（中文引号
被写成半角、富文本标签没转义），而且游戏只在日志里留一行，肉眼看不见。

用法：
    python tools/xml_guard.py validate                # 只校验，失败返回码 1
    python tools/xml_guard.py validate --path GameData
    python tools/xml_guard.py diff                    # 对照游戏原件查结构损伤
    python tools/xml_guard.py fix --dry-run           # 预览要改什么（默认）
    python tools/xml_guard.py fix --apply             # 真正写入

修复范围（仅针对本来就解析失败的文件，已合法的文件一个字都不动）：
    1. 属性值内的裸 `<` `>`      -> &lt; &gt;
    2. 属性值内的裸半角 `"`      -> &quot;   （成对出现时按对替换）
不改动标签名、属性名、属性顺序，也不改任何已经合法的文件。

注意：重复属性（duplicate attribute）与丢字段属于结构损坏，不做自动修复，
只报告，需人工对照游戏原件恢复。
"""
import argparse
import os
import re
import sys
import xml.etree.ElementTree as ET

# 属性结束后允许紧跟的字符：空白、`>`、`/>`
_ATTR_START = re.compile(r'[A-Za-z_:][\w:.\-]*\s*=')


def parses(path):
    try:
        ET.parse(path)
        return True, ""
    except ET.ParseError as e:
        return False, str(e)
    except OSError as e:
        return False, f"IO: {e}"


def iter_xml(root):
    for dp, _, fn in os.walk(root):
        for f in fn:
            if f.lower().endswith('.xml'):
                yield os.path.join(dp, f)


def scan(text):
    """把文本切成 (区段, 类型) 并找出属性值内的非法字符位置。

    返回 (新文本, 统计 dict)。区段类型：outside / tag / value / comment。
    """
    out = []
    n = len(text)
    i = 0
    state = 'outside'
    stat = {'lt': 0, 'gt': 0, 'quote': 0}

    while i < n:
        ch = text[i]

        if state == 'outside':
            if text.startswith('<!--', i):
                state = 'comment'
                out.append('<!--')
                i += 4
                continue
            if text.startswith('<![CDATA[', i):
                end = text.find(']]>', i)
                if end == -1:
                    end = n - 3
                out.append(text[i:end + 3])
                i = end + 3
                continue
            if ch == '<':
                state = 'tag'
            out.append(ch)
            i += 1
            continue

        if state == 'comment':
            if text.startswith('-->', i):
                out.append('-->')
                i += 3
                state = 'outside'
            else:
                out.append(ch)
                i += 1
            continue

        if state == 'tag':
            if ch == '>':
                out.append('>')
                i += 1
                state = 'outside'
                continue
            # 属性值起点：="  或者 =  " （允许等号后有空格）
            if ch == '=':
                out.append(ch)
                i += 1
                while i < n and text[i] in ' \t\r\n':
                    out.append(text[i])
                    i += 1
                if i < n and text[i] == '"':
                    out.append('"')
                    i += 1
                    state = 'value'
                continue
            out.append(ch)
            i += 1
            continue

        # state == 'value'
        if ch == '"':
            j = i + 1
            while j < n and text[j] in ' \t\r\n':
                j += 1
            closes = False
            if j >= n:
                closes = True
            elif text[j] in '>/':
                closes = True
            elif text.startswith('?>', j):      # <?xml ... ?> 声明结尾
                closes = True
            elif _ATTR_START.match(text, j):
                closes = True
            if closes:
                out.append('"')
                i += 1
                state = 'tag'
            else:
                out.append('&quot;')
                stat['quote'] += 1
                i += 1
            continue
        if ch == '<':
            out.append('&lt;')
            stat['lt'] += 1
            i += 1
            continue
        if ch == '>':
            out.append('&gt;')
            stat['gt'] += 1
            i += 1
            continue
        out.append(ch)
        i += 1

    return ''.join(out), stat


def cmd_validate(args):
    bad = []
    total = 0
    for root in args.path:
        for p in iter_xml(root):
            total += 1
            ok, err = parses(p)
            if not ok:
                bad.append((p, err))
    print(f"校验 {total} 个 XML，失败 {len(bad)}")
    for p, err in bad:
        print(f"  FAIL {os.path.relpath(p, args.base)}  |  {err}")
    return 1 if bad else 0


def cmd_fix(args):
    changed = []
    unfixable = []
    for root in args.path:
        for p in iter_xml(root):
            ok, err = parses(p)
            if ok:
                continue
            text = open(p, encoding='utf-8', newline='').read()
            new, stat = scan(text)
            ok2, err2 = parses_text(new)
            if ok2:
                changed.append((p, stat, err))
            else:
                unfixable.append((p, err, err2))

    print(f"需要修复且可自动修复: {len(changed)}，需人工: {len(unfixable)}")
    for p, stat, err in changed:
        rel = os.path.relpath(p, args.base)
        print(f"  FIX  {rel}  轻量转义: <{stat['lt']} >{stat['gt']} \"{stat['quote']}   (原错误: {err[:52]})")
    for p, err, err2 in unfixable:
        rel = os.path.relpath(p, args.base)
        print(f"  MANUAL {rel}  自动修复无效: {err2[:60]}")

    if not args.apply:
        print("\n[dry-run] 未写入。加 --apply 执行。")
        return 0

    for p, stat, _ in changed:
        text = open(p, encoding='utf-8', newline='').read()
        new, _ = scan(text)
        with open(p, 'w', encoding='utf-8', newline='') as f:
            f.write(new)
    print(f"\n已写入 {len(changed)} 个文件。")
    return 0


def parses_text(text):
    # 用 bytes 解析：文件带 BOM 与 encoding 声明，直接喂 str 会被 ElementTree 拒绝
    try:
        ET.fromstring(text.encode('utf-8'))
        return True, ""
    except ET.ParseError as e:
        return False, str(e)
    except ValueError as e:
        return False, f"Value: {e}"


def _key(el):
    for a in ('name', 'id', 'key', 'internal_name'):
        v = el.get(a)
        if v:
            return (el.tag, a, v)
    return None


def _index(root):
    """按「祖先链 + 自身 tag/name」建索引，避免 W1/W2 这类重名武器跨单位撞车。"""
    d = {}
    dup = 0

    def ident(el):
        for a in ('name', 'id', 'key', 'internal_name'):
            v = el.get(a)
            if v:
                return f"{el.tag}#{v}"
        return el.tag

    def walk(el, path):
        nonlocal dup
        me = ident(el)
        here = path + (me,)
        if el.get('name') or el.get('id') or el.get('key') or el.get('internal_name'):
            if here in d:
                dup += 1
            else:
                d[here] = el
        for ch in el:
            walk(ch, here)

    walk(root, ())
    return d, dup


def resolve_original(proj_path, base, game_root):
    """"把项目内的相对路径映射到游戏原件路径；映射不到返回 None。"""
    rel = os.path.relpath(proj_path, base).replace('\\', '/')
    if rel.startswith('GameData/') or rel.startswith('XMLMods/'):
        cand = os.path.join(game_root, rel.replace('/', os.sep))
        return cand if os.path.exists(cand) else None
    return None


def cmd_diff(args):
    rows = []
    unmapped = 0
    for root in args.path:
        for p in iter_xml(root):
            orig = resolve_original(p, args.base, args.game)
            if not orig:
                unmapped += 1
                continue
            ok, err = parses(p)
            if not ok:
                rows.append((p, None, err))
                continue
            try:
                cur, dup = _index(ET.parse(p).getroot())
                ref, _ = _index(ET.parse(orig).getroot())
            except ET.ParseError as e:
                rows.append((p, None, str(e)))
                continue
            missing_el = [k for k in ref if k not in cur]
            extra_el = [k for k in cur if k not in ref]
            missing_attr = []
            for k in ref:
                if k in cur:
                    lost = set(ref[k].attrib) - set(cur[k].attrib)
                    if lost:
                        missing_attr.append((k, sorted(lost)))
            if missing_el or extra_el or missing_attr or dup:
                rows.append((p, dict(missing_el=missing_el, extra_el=extra_el,
                                     missing_attr=missing_attr, dup=dup), err))

    rows.sort(key=lambda r: (-(len((r[1] or {}).get('missing_attr') or []) +
                               len((r[1] or {}).get('missing_el') or [])), r[0]))
    print(f"对照游戏原件：{len(rows)} 个文件有结构差异，{unmapped} 个文件无原件可比（扩展/DLC 目录）")
    for p, info, err in rows:
        rel = os.path.relpath(p, args.base)
        if info is None:
            print(f"  ERR    {rel}  {err}")
            continue
        bits = []
        if info['missing_attr']:
            bits.append(f"丢属性 {len(info['missing_attr'])} 处")
        if info['missing_el']:
            bits.append(f"丢元素 {len(info['missing_el'])} 个")
        if info['extra_el']:
            bits.append(f"多元素 {len(info['extra_el'])} 个")
        if info['dup']:
            bits.append(f"重复键 {info['dup']} 个")
        print(f"  DIFF   {rel}  {' / '.join(bits)}")
        for k, lost in info['missing_attr'][:4]:
            print(f"           {k[-1]}: 缺 {lost}")
        for k in info['missing_el'][:3]:
            print(f"           缺元素 {k[-1]}")
    return 1 if rows else 0


def main():
    ap = argparse.ArgumentParser(description="汉化项目 XML 守卫")
    ap.add_argument('action', choices=['validate', 'fix', 'diff'])
    ap.add_argument('--path', nargs='+', default=None, help='要扫描的目录（默认 XMLMods GameData）')
    ap.add_argument('--base', default='.', help='输出相对路径的基准目录')
    ap.add_argument('--game', default='..', help='游戏根目录（diff 模式对照原件用，默认 ..）')
    ap.add_argument('--apply', action='store_true', help='fix 时真正写入')
    ap.add_argument('--dry-run', action='store_true', help='fix 时只预览（默认行为）')
    args = ap.parse_args()
    if args.path is None:
        args.path = [p for p in ('XMLMods', 'GameData') if os.path.isdir(p)]
    if not args.path:
        print("没有可扫描的目录（请在项目根目录运行）")
        return 2
    if args.action == 'validate':
        return cmd_validate(args)
    if args.action == 'diff':
        return cmd_diff(args)
    return cmd_fix(args)


if __name__ == '__main__':
    sys.exit(main())
