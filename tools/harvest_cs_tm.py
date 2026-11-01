# -*- coding: utf-8 -*-
"""把 DLLSource 里的中文译文抽成 IL 补丁字典（按行 diff 版）。

背景：`DLLBin/AIWarExternal*.dll` 是 7 月「源码分叉」编译的，装到新版本游戏上
等于把代码逻辑退回 7 月。正确做法是只把**字符串**搬到当前 DLL 上 —— 和核心
DLL 一样做 IL 字面量替换。

做法（比按整文件字面量序列对齐更准）：
1. 两边同名 .cs 按行做 SequenceMatcher
2. 只处理 `replace` 行块 —— 这些正是被翻译改过的行，其余行原封不动
3. 在改动块内各抽字符串字面量，数量相等才 1:1 配对（数量不等视为代码有改动，跳过）
4. 再按「中文侧含 CJK、游戏侧不含、长度比例合理」过滤

用法：
    python tools/harvest_cs_tm.py            # 只统计
    python tools/harvest_cs_tm.py --write    # 写出 tools/ilpatch/<asm>.external.json
"""
import argparse
import collections
import difflib
import json
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.dirname(PROJECT)
SRC_FORK = os.path.join(PROJECT, 'DLLSource')
SRC_GAME = os.path.join(GAME, 'CodeExternal')

ASMS = ('AIWarExternalCode', 'AIWarExternalDeepProcessingCode',
        'AIWarExternalVisualizationCode')

CJK = re.compile(r'[\u4e00-\u9fff]')
STR = re.compile(r'"((?:[^"\\\n]|\\.)*)"')


def literals(text):
    return [m.group(1) for m in STR.finditer(text)]


def unescape(s):
    return (s.replace('\\"', '"').replace('\\n', '\n').replace('\\t', '\t')
             .replace('\\\\', '\\'))


def walk_cs(root):
    for dp, _, fn in os.walk(root):
        for f in fn:
            if f.endswith('.cs'):
                p = os.path.join(dp, f)
                yield os.path.relpath(p, root).replace('\\', '/'), p


def read_lines(p):
    try:
        return open(p, encoding='utf-8-sig', errors='ignore').read().split('\n')
    except OSError:
        return []


def plausible(en, zh):
    """长度比例合理才算真译文，挡掉对齐错位产生的垃圾对。"""
    if not en or not zh:
        return False
    if not CJK.search(zh) or CJK.search(en):
        return False
    if len(en.strip()) < 2:
        return False
    r = len(zh) / len(en)
    return 0.15 <= r <= 1.4


NEVER_FILE = os.path.join(PROJECT, 'tools', 'ilpatch', 'never_translate.txt')


def load_never():
    """被 C# 当作查找键用的字面量，翻译会让查表失败，必须保留英文。"""
    words = set()
    if os.path.exists(NEVER_FILE):
        for line in open(NEVER_FILE, encoding='utf-8'):
            line = line.strip()
            if line and not line.startswith('#'):
                words.add(line)
    return words


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--write', action='store_true')
    args = ap.parse_args()

    never = load_never()
    game_files = dict(walk_cs(SRC_GAME))
    pairs = collections.defaultdict(dict)
    stat = collections.Counter()
    multi = collections.defaultdict(set)

    for rel, fork_path in walk_cs(SRC_FORK):
        asm = rel.split('/')[0]
        if asm not in ASMS:
            continue
        cand = rel if rel in game_files else rel.replace(f'{asm}/src/', f'{asm}/')
        if cand not in game_files:
            stat['no_counterpart'] += 1
            continue
        a = read_lines(os.path.join(SRC_GAME, game_files[cand]))
        b = read_lines(fork_path)
        sm = difflib.SequenceMatcher(None, a, b, autojunk=False)
        for tag, i1, i2, j1, j2 in sm.get_opcodes():
            if tag != 'replace':
                continue
            en_lits = literals('\n'.join(a[i1:i2]))
            zh_lits = literals('\n'.join(b[j1:j2]))
            if len(en_lits) != len(zh_lits):
                stat['skip_count_mismatch'] += 1
                continue
            prio = 0 if len(en_lits) == 1 else 1   # 单字面量改动块最可信
            for raw_en, raw_zh in zip(en_lits, zh_lits):
                en, zh = unescape(raw_en), unescape(raw_zh)
                if en in never:
                    stat['skip_never'] += 1
                    continue
                if not plausible(en, zh):
                    continue
                prev = pairs[asm].get(en)
                if prev is None:
                    pairs[asm][en] = (zh, prio)
                else:
                    if prev[0] != zh:
                        multi[en].add(prev[0])
                        multi[en].add(zh)
                        stat['conflict'] += 1
                    if prio < prev[1]:
                        pairs[asm][en] = (zh, prio)
                stat['pair'] += 1

    uniq = len({k for d in pairs.values() for k in d})
    print(f'抽出译文对 {stat["pair"]} 条，唯一英文 {uniq}，同词多译 {len(multi)}')
    print(f'  跳过的改动块（字面量数不等，代码有变动）: {stat["skip_count_mismatch"]}')
    print(f'  跳过文件（游戏侧无同名）: {stat["no_counterpart"]}')
    if stat['skip_never']:
        print(f'  按 never_translate.txt 跳过: {stat["skip_never"]}')
    for asm, d in sorted(pairs.items()):
        hi = sum(1 for v in d.values() if v[1] == 0)
        print(f'  {asm}: {len(d)} 条（高置信 {hi}）')
    for en, zs in list(multi.items())[:6]:
        print(f'  多译: {en[:40]!r} -> {sorted(zs)[:3]}')

    if not args.write:
        print('\n[dry] 未写文件。加 --write 生成 tools/ilpatch/<asm>.external.json')
        return 0

    out_dir = os.path.join(PROJECT, 'tools', 'ilpatch')
    for asm, d in pairs.items():
        path = os.path.join(out_dir, f'{asm}.external.json')
        flat = {k: v[0] for k, v in d.items()}
        with open(path, 'w', encoding='utf-8', newline='\n') as f:
            json.dump(flat, f, ensure_ascii=False, indent=1, sort_keys=True)
        print(f'  写入 {os.path.relpath(path, PROJECT)}（{len(d)} 条）')
    return 0


if __name__ == '__main__':
    sys.exit(main())
