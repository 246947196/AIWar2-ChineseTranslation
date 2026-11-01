# -*- coding: utf-8 -*-
"""按「≥6 个英文单词」口径，分批导出外部 DLL 里未翻译的句子。

用法：
    python tools/dump_ui_batch.py --asm code --batch 1 --size 50
输出：tools/batches/<asm>_<batch>.txt（原文，一行一条，含富文本标签）
"""
import argparse
import json
import os
import re
import subprocess
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.dirname(PROJECT)
ILPATCH = os.path.join(PROJECT, 'tools', 'ilpatch', 'bin', 'ilpatch.exe')
DICT_DIR = os.path.join(PROJECT, 'tools', 'ilpatch')
OUT_DIR = os.path.join(PROJECT, 'tools', 'batches')

DLLS = {
    'code': 'GameData/ModdableLogicDLLs/AIWarExternalCode.dll',
    'deep': 'GameData/ModdableLogicDLLs/AIWarExternalDeepProcessingCode.dll',
    'viz': 'GameData/ModdableLogicDLLs/AIWarExternalVisualizationCode.dll',
}

CJK = re.compile(r'[\u4e00-\u9fff]')
TAG = re.compile(r'<[^>]+>')
WORDS = re.compile(r"[A-Za-z][A-Za-z'\-]+")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--asm', default='code', choices=list(DLLS))
    ap.add_argument('--batch', type=int, default=1)
    ap.add_argument('--size', type=int, default=50)
    ap.add_argument('--min-words', type=int, default=6)
    args = ap.parse_args()

    dll = os.path.join(GAME, DLLS[args.asm])
    tmpp = os.path.join(DICT_DIR, '_dump.json')
    subprocess.run([ILPATCH, 'dump-ldstr', dll, tmpp], capture_output=True, check=True)
    allstr = set(json.load(open(tmpp, encoding='utf-8')))
    os.remove(tmpp)

    dict_path = os.path.join(DICT_DIR, f'AIWarExternal{ {"code":"Code","deep":"DeepProcessingCode","viz":"VisualizationCode"}[args.asm] }.external.json')
    known = set()
    if os.path.exists(dict_path):
        known = set(json.load(open(dict_path, encoding='utf-8')))
    manual = os.path.join(DICT_DIR, 'manual_zh.tsv')
    if os.path.exists(manual):
        for line in open(manual, encoding='utf-8'):
            if '\t' in line and not line.startswith('#'):
                k = line.split('\t', 1)[0]
                known.add(k.replace('\\n', '\n').replace('\\t', '\t'))

    def wc(s):
        return len(WORDS.findall(TAG.sub(' ', s)))

    def sentence_like(s):
        """日志行 vs 界面句子的区分。

        日志行通常以制表符/多空格开头、没有句末标点、且常含代码味的关键词。
        界面句子则以大写字母或富文本标签开头，通常带标点。"""
        t = s.strip()
        if not t:
            return False
        if s[:1] in ('\t', ' ') and not s.lstrip()[:1].isupper():
            return False
        if not (t[0].isupper() or t[0] == '<'):
            return False
        if re.search(r'(SKIP|AIP|debugStage|Path [AB]|TID\d|\.cs\b|==|!=|->)', s):
            return False
        # 内部日志/报错的一贯开头，玩家永远看不到
        if re.match(r'^(Bug|Cannot|Could not|Couldn|Unknown|Invalid|Missing|Expected|Failed|'
                    r'Warning|Error|Calling|Returning|Skipping|Adding|Removing|Set |Get|Trying|'
                    r'Found |No |Not |Unable|Null|Empty|Index|Assert|Check)', t):
            return False
        # 界面文本通常是"对玩家说话"的：含 you/your/this/that，或以祈使句开头
        if not re.search(r'\b(you|your|You|Your|this|This|that|That)\b', t) \
           and not re.match(r'^(Click|Press|Hold|Select|Choose|If |When |Once |The |Each )', t):
            return False
        if not re.search(r'[.!?:]$|[.!?:]\s|<', t):
            # 没有标点也不是富文本，多半是日志
            return False
        return True

    cand = sorted(s for s in allstr
                  if s not in known and not CJK.search(s)
                  and wc(s) >= args.min_words and sentence_like(s))
    start = (args.batch - 1) * args.size
    chunk = cand[start:start + args.size]
    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, f'{args.asm}_{args.batch:03d}.txt')
    with open(out, 'w', encoding='utf-8', newline='\n') as f:
        for s in chunk:
            f.write(s.replace('\n', '\\n') + '\n')
    print(f'{args.asm}: 候选 {len(cand)} 条，第 {args.batch} 批 {len(chunk)} 条 -> {os.path.relpath(out, PROJECT)}')
    for i, s in enumerate(chunk, 1):
        print(f'--- {i}')
        print(s)


if __name__ == '__main__':
    sys.exit(main())
