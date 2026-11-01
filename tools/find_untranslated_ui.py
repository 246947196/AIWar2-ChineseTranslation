# -*- coding: utf-8 -*-
"""静态扫描：找出 UI 相关源码里「还没翻译」的英文字面量。

比运行时采集更全 —— 不用玩也能列出候选，代价是可能混入日志/内部串，
所以按「文件是否属于 UI 目录」+ 若干启发式规则过滤。

用法：
    python tools/find_untranslated_ui.py            # 打印统计与前若干条
    python tools/find_untranslated_ui.py --write    # 写 tools/untranslated_ui.tsv
"""
import argparse
import collections
import json
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.dirname(PROJECT)
SRC = os.path.join(GAME, 'CodeExternal')
DICT_DIR = os.path.join(PROJECT, 'tools', 'ilpatch')
OUT = os.path.join(PROJECT, 'tools', 'untranslated_ui.tsv')

CJK = re.compile(r'[\u4e00-\u9fff]')
LIT = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
LETTERS = re.compile(r'[A-Za-z]')
WORDS = re.compile(r'[A-Za-z]{2,}')

# UI 相关目录（游戏里肉眼可见的文本基本出自这些）
UI_DIRS = ('/UIs/', '/EntityText/', '/Hacking/', '/GameCommand/', '/Scenarios/')

SKIP = re.compile(
    r'(^[A-Za-z0-9_]+$)'          # 纯标识符
    r'|\.(cs|dll|png|xml|json)$'
    r'|^(https?|file):'
    r'|^\{?\d'                     # 起始为数字
    r'|\\n\\n$'
)

LOGY = re.compile(r'(Error|Exception|debug|Debug|TID|==|!=|%s|\{0\}|\{1\}|<\w|Snapshot|Parse)', re.I)


def load_known():
    """已被翻译或明确决定保留英文的字面量。"""
    known = set()
    for name in os.listdir(DICT_DIR):
        if not name.endswith('.json') or 'external' not in name and 'merged' not in name:
            continue
        try:
            d = json.load(open(os.path.join(DICT_DIR, name), encoding='utf-8'))
        except Exception:
            continue
        if isinstance(d, dict):
            known |= set(d.keys())
    for fn in ('never_translate.txt',):
        p = os.path.join(DICT_DIR, fn)
        if os.path.exists(p):
            for line in open(p, encoding='utf-8'):
                line = line.strip()
                if line and not line.startswith('#'):
                    known.add(line)
    return known


def plausible(s):
    if not s or len(s) < 3 or len(s) > 400:
        return False
    if CJK.search(s) or not LETTERS.search(s):
        return False
    if len(WORDS.findall(s)) < 1:
        return False
    if SKIP.search(s) or LOGY.search(s):
        return False
    # 全是格式占位符/符号
    if not re.search(r'[A-Za-z]{2,}\s*[A-Za-z]{0,}', s):
        return False
    return True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--write', action='store_true')
    args = ap.parse_args()

    known = load_known()
    hits = {}
    for dp, _, fn in os.walk(SRC):
        for f in fn:
            if not f.endswith('.cs'):
                continue
            path = os.path.join(dp, f)
            rel = path.replace('\\', '/')
            if not any(d in rel for d in UI_DIRS):
                continue
            try:
                text = open(path, encoding='utf-8-sig', errors='ignore').read()
            except OSError:
                continue
            for m in LIT.finditer(text):
                s = m.group(1)
                s = s.replace('\\"', '"').replace('\\n', '\n').replace('\\t', '\t')
                if s in known or not plausible(s):
                    continue
                key = s
                if key not in hits:
                    hits[key] = {'file': f, 'count': 0}
                hits[key]['count'] += 1

    items = sorted(hits.items(), key=lambda kv: (-kv[1]['count'], kv[0]))
    print(f'UI 相关源码里未翻译的候选: {len(items)} 条')
    for s, meta in items[:20]:
        one = s.replace('\n', ' ')[:70]
        print(f"  x{meta['count']:<2} {meta['file'][:34]:36s} {one}")

    if args.write:
        with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
            f.write('# UI 源码里未翻译的字面量（text\tcount\tfile）\n')
            for s, meta in items:
                f.write(f"{s.replace(chr(9), ' ')}\t{meta['count']}\t{meta['file']}\n")
        print(f'已写 {os.path.relpath(OUT, PROJECT)}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
