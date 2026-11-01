# -*- coding: utf-8 -*-
"""把外部代码 DLL 的译文以 IL 字面量方式打到**当前版本**的 DLL 上。

为什么不用源码重编译：`DLLSource/` 是 7 月的源码分叉，重编译出来的 DLL 会把游戏
7 月之后的代码改动一起退回去。IL 补丁只替换字符串字面量，代码结构、数值、新逻辑
全部保持当前版本。

做法：
  1. `DLLBin/_base/<asm>.dll` 保存**未打补丁**的当前版本 DLL（基线）
  2. 每次从基线复制 → ilpatch patch → 写回 `DLLBin/<asm>.dll`
  3. 顺手校验中文条数，并记录基线哈希，供 deploy.ps1 判断是否过期

用法：
    python tools/patch_external_dlls.py                # 打补丁
    python tools/patch_external_dlls.py --refresh-base # 游戏更新后重新取基线
"""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.dirname(PROJECT)
ILPATCH = os.path.join(PROJECT, 'tools', 'ilpatch', 'bin', 'ilpatch.exe')
DICT_DIR = os.path.join(PROJECT, 'tools', 'ilpatch')
BIN_DIR = os.path.join(PROJECT, 'DLLBin')
BASE_DIR = os.path.join(BIN_DIR, '_base')
GAME_DLL_DIR = os.path.join(GAME, 'GameData', 'ModdableLogicDLLs')
BASE_INFO = os.path.join(DICT_DIR, 'external_base.json')

ASMS = ('AIWarExternalCode', 'AIWarExternalDeepProcessingCode',
        'AIWarExternalVisualizationCode')

CJK = r'\u4e00-\u9fff'


def sha(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def cjk_count(dll):
    tmp = os.path.join(PROJECT, 'tools', 'ilpatch', '_tmp_ldstr.json')
    subprocess.run([ILPATCH, 'dump-ldstr', dll, tmp], capture_output=True, check=True)
    try:
        data = json.load(open(tmp, encoding='utf-8'))
    finally:
        os.path.exists(tmp) and os.remove(tmp)
    items = data if isinstance(data, list) else list(data)
    return sum(1 for s in items if any('\u4e00' <= c <= '\u9fff' for c in s)), len(items)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--refresh-base', action='store_true',
                    help='游戏更新后，从游戏目录重新取未打补丁的基线')
    args = ap.parse_args()

    os.makedirs(BASE_DIR, exist_ok=True)
    info = {}
    if os.path.exists(BASE_INFO):
        info = json.load(open(BASE_INFO, encoding='utf-8'))

    for asm in ASMS:
        base = os.path.join(BASE_DIR, f'{asm}.dll')
        game_dll = os.path.join(GAME_DLL_DIR, f'{asm}.dll')
        if args.refresh_base or not os.path.exists(base):
            if not os.path.exists(game_dll):
                print(f'  !! 找不到游戏 DLL: {game_dll}')
                continue
            shutil.copy2(game_dll, base)
            print(f'  取基线: {asm} <- {os.path.relpath(game_dll, GAME)}')
        # 游戏当前 DLL 要么是未打补丁的原版（== 基线），要么是我们上一轮部署的产物
        # （== 记录的 dll_sha256）。两者都不是才说明游戏更新过。
        if os.path.exists(game_dll):
            cur = sha(game_dll)
            known = {sha(base)}
            if asm in info and info[asm].get('dll_sha256'):
                known.add(info[asm]['dll_sha256'])
            if cur not in known:
                print(f'  !! {asm} 的基线与游戏当前 DLL 不一致（游戏更新过？）')
                print(f'     先跑: python tools\\patch_external_dlls.py --refresh-base')
                return 1

        out = os.path.join(BIN_DIR, f'{asm}.dll')
        dct = os.path.join(DICT_DIR, f'{asm}.external.json')
        if not os.path.exists(dct):
            print(f'  !! 缺少字典 {os.path.relpath(dct, PROJECT)}，先跑 harvest_cs_tm.py --write')
            return 1
        # 合并人工译文覆盖层（重抽字典不会冲掉这些）
        manual = os.path.join(DICT_DIR, 'manual_zh.tsv')
        if os.path.exists(manual):
            try:
                d = json.load(open(dct, encoding='utf-8'))
                n = 0
                for line in open(manual, encoding='utf-8'):
                    line = line.rstrip('\n')
                    if not line or line.startswith('#') or '\t' not in line:
                        continue
                    en, zh = line.split('\t', 1)
                    if en and zh and d.get(en) != zh:
                        d[en] = zh
                        n += 1
                if n:
                    dct = os.path.join(DICT_DIR, f'_{asm}.merged.tmp.json')
                    json.dump(d, open(dct, 'w', encoding='utf-8', newline='\n'),
                              ensure_ascii=False, indent=1, sort_keys=True)
                    print(f'    合并人工译文 {n} 条')
            except Exception as e:
                print(f'    !! 人工译文合并失败: {e}')
        shutil.copy2(base, out)
        bak = out + '.bak'
        if os.path.exists(bak):
            os.remove(bak)
        r = subprocess.run([ILPATCH, 'patch', out, dct], capture_output=True, text=True)
        line = [l for l in r.stderr.splitlines() if 'replaced' in l or 'locked' in l]
        print(f'  {asm}: ' + (' / '.join(line) if line else r.stderr.strip()[:120]))
        new = out[:-4] + '.new.dll'
        if os.path.exists(new):
            shutil.move(new, out)
            print(f'    （侧旁写入，已覆盖目标）')
        zh, total = cjk_count(out)
        print(f'    含中文 ldstr {zh} / {total}')
        info[asm] = {'base_sha256': sha(base), 'dll_sha256': sha(out), 'cjk': zh}

    with open(BASE_INFO, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(info, f, ensure_ascii=False, indent=2, sort_keys=True)
    print(f'已记录基线信息: {os.path.relpath(BASE_INFO, PROJECT)}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
