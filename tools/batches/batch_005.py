# -*- coding: utf-8 -*-
"""第 5 批（code_001.txt 重导版）——以标点/标签开头的界面片段。"""
import os
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BATCH_DIR = os.path.join(PROJECT, 'tools', 'batches')
MANUAL = os.path.join(PROJECT, 'tools', 'ilpatch', 'manual_zh.tsv')

ZH = {
    4: "\t银河中一颗未知星球将在……后进入芬布尔之冬 ",
    18: r"\n\n每座天灾建筑与每名战士都会获得 ",
    19: r"\n\n他们的世界已浮出水面；入口将在……后开启 ",
    22: r"\n一具新的躯体正在它的金字塔等待（",
    29: r"\n这次入侵将由……执行 ",
    33: "  （纵深打击的报复（若适用）要等到它被……之后才会发生 ",
    34: " （但它们的计算伤害为零）？",
    35: r" （ODSS）被入侵后，可为你所有星球、战斗站与要塞解锁额外的炮台、雷场与防御。\n\n",
    38: r" （无需占领星球本身），使 AI 无法再经它派出波次。\n\n",
    39: " <size=60%>（点击「开始游戏」，或在派系页签悬停派系查看详情）</size>",
    42: " 因当前 AI 难度，存在一个倍率 ",
    44: " 需要一名使用金属的人类玩家",
    48: " 它们因为你超过……而对你恼火 ",
    50: r" 活动，看看新的经济建筑正在何处落地。\n\n",
}


def main():
    path = os.path.join(BATCH_DIR, 'code_001.txt')
    lines = [l.rstrip('\n') for l in open(path, encoding='utf-8')]
    added = 0
    with open(MANUAL, 'a', encoding='utf-8', newline='\n') as f:
        for i, raw in enumerate(lines, 1):
            if i not in ZH:
                continue
            zh = ZH[i].replace('\n', '\\n').replace('\t', '\\t')
            f.write(f'{raw}\t{zh}\n')
            added += 1
    print(f'写入 {added} 条（批内 {len(lines)} 条）')


if __name__ == '__main__':
    main()
