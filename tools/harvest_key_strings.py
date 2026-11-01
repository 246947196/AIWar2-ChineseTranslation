# -*- coding: utf-8 -*-
"""把 C# 里「参与字符串比较的字面量」抽出来，供 XML 生成时避让。

这个游戏把显示字符串当代码用：代码里到处是
    row.DisplayName == "Build Harvester"
    if ( something.DisplayName == displayName )
    switch ( x ) { case "Metal": ... }
一旦把 XML 的 display_name 翻成中文，这些比较就全部失配 —— 轻则表项丢失，
重则空列表崩溃。所以这些字面量对应的 XML 值必须保留英文。

用法：
    python tools/harvest_key_strings.py          # 输出统计
    python tools/harvest_key_strings.py --write  # 写 tools/key_strings.txt
"""
import argparse
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.dirname(PROJECT)
SRC = os.path.join(GAME, 'CodeExternal')
OUT = os.path.join(PROJECT, 'tools', 'key_strings.txt')

# 只抓"比较"语境："..." 出现在 ==、!=、.Equals(、case 里
PATTERNS = [
    re.compile(r'==\s*"((?:[^"\\\n]|\\.)*)"'),
    re.compile(r'!=\s*"((?:[^"\\\n]|\\.)*)"'),
    re.compile(r'\.Equals\(\s*"((?:[^"\\\n]|\\.)*)"'),
    re.compile(r'\bcase\s+"((?:[^"\\\n]|\\.)*)"\s*:'),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--write', action='store_true')
    args = ap.parse_args()

    found = {}
    files = 0
    for dp, _, fn in os.walk(SRC):
        for f in fn:
            if not f.endswith('.cs'):
                continue
            files += 1
            p = os.path.join(dp, f)
            try:
                text = open(p, encoding='utf-8-sig', errors='ignore').read()
            except OSError:
                continue
            for line in text.split('\n'):
                for rx in PATTERNS:
                    for m in rx.finditer(line):
                        lit = m.group(1)
                        if len(lit) < 2:
                            continue
                        # 只保留"像人话/像名字"的：含空格或以大写开头，纯技术串不要
                        if not (re.search(r'[A-Za-z]', lit)):
                            continue
                        found.setdefault(lit, os.path.relpath(p, SRC))

    print(f'扫描 {files} 个 .cs，抽出参与比较的字面量 {len(found)} 条')
    for k in list(found)[:8]:
        print(f'   {k!r}  ({found[k]})')

    if not args.write:
        print('\n[dry] 加 --write 写 tools/key_strings.txt')
        return 0
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('# 由 harvest_key_strings.py 生成：C# 里参与 ==/!=/Equals/case 比较的字面量\n')
        f.write('# XML 里这些值必须保留英文，否则代码里的比较会失配。\n')
        for k in sorted(found):
            f.write(k.replace('\n', '\\n') + '\n')
    print(f'写入 {os.path.relpath(OUT, PROJECT)}（{len(found)} 条）')
    return 0


if __name__ == '__main__':
    sys.exit(main())
