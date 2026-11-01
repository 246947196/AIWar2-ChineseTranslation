# -*- coding: utf-8 -*-
"""跑完游戏后检查日志，抓出「翻译引起的」异常。

我这次就栽在这里：只 grep 了 ArcenDebugLog 的 error/exception 关键词，漏掉了
`Could not find '名称'` —— 它不带 error/exception 字样，但正是翻译把 C# 用的
查找键译掉导致的。所以判定规则必须包含这类"查表失败"。

用法：
    python tools/check_run.py            # 只看最后一次运行
    python tools/check_run.py --all      # 看整个日志

注意：游戏**每次启动都会截断** PlayerData/ArcenDebugLog.txt（只保留本次运行），
所以要在退出游戏之后立刻跑，别等下一次启动。
"""
import argparse
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.dirname(PROJECT)
LOG = os.path.join(GAME, 'PlayerData', 'ArcenDebugLog.txt')
ERR = os.path.join(GAME, 'PlayerData', 'ErrorsReportedByEngine.txt')

# (正则, 说明, 是否致命)
RULES = [
    (re.compile(r"Could not find '(.*)'"), '按名查找失败（多半是译文把查找键翻掉了）', True),
    (re.compile(r'XmlException'), 'XML 解析失败（文件被跳过，内容全丢）', True),
    (re.compile(r'NullReferenceException'), '空引用', True),
    (re.compile(r'MissingMethodException|TypeLoadException'), '程序集/版本不匹配', True),
    (re.compile(r'ArgumentException: Mesh'), 'UI 顶点超限（长文本一次性渲染）', False),
    (re.compile(r'\bERROR\b'), '游戏自报错误', False),
    (re.compile(r'^System\.\w*Exception:'), '表/行解析异常（多半也是译文吃了枚举值）', True),
]

# 已知无害：游戏自己的随机派系资格检查
BENIGN = [re.compile(r'Could not find a faction with name <.*> that is eligible to be random')]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--all', action='store_true')
    args = ap.parse_args()

    if not os.path.exists(LOG):
        print(f'找不到日志: {LOG}')
        return 2
    lines = open(LOG, encoding='utf-8', errors='ignore').read().split('\n')

    # 不再按时间戳过滤：异常体是多行的，续行不带时间戳，一过滤就漏（上次就是这么漏的）

    findings = []
    for l in lines:
        if any(b.search(l) for b in BENIGN):
            continue
        for rx, desc, fatal in RULES:
            m = rx.search(l)
            if m:
                findings.append((desc, fatal, l.strip()[:160]))
                break

    if not findings:
        print('未发现可疑日志。')
        return 0
    print(f'发现 {len(findings)} 条可疑：')
    seen = set()
    for desc, fatal, line in findings:
        key = (desc, line.split('\t')[-1][:60])
        if key in seen:
            continue
        seen.add(key)
        flag = '致命' if fatal else '注意'
        print(f'  [{flag}] {desc}\n         {line}')
    fatal_n = sum(1 for _, f, _ in findings if f)
    print(f'\n合计：致命 {fatal_n} 条 / 共 {len(findings)} 条')
    return 1 if fatal_n else 0


if __name__ == '__main__':
    sys.exit(main())
