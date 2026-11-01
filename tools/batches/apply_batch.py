# -*- coding: utf-8 -*-
"""把某一批的译文追加进 manual_zh.tsv（按批内序号对应）。

批文件由 dump_ui_batch.py 生成，每行一条，换行已转义为 \\n。
"""
import os
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BATCH_DIR = os.path.join(PROJECT, 'tools', 'batches')
MANUAL = os.path.join(PROJECT, 'tools', 'ilpatch', 'manual_zh.tsv')

# 序号从 1 开始；没有列出的即视为日志行，跳过不翻
ZH = {
    2: r"\n已对该派系投入的入侵点数：<color=#3DE799>",
    3: "游戏中的派系已经太多，无法再添加。",
    6: r"\n\n<color=#888888>点击前往其所在星球。</color>",
    7: r"\n\n<i>每新增一个特门都会增加收获，旅程越长带回的资源越好。</i>",
    8: r"\n\n<i>来自神龛的低阶朝圣者只携带金属、入侵点和科学点。</i>",
    9: r"\n\n<size=85%><color=#999999>点击此图标可查看你所有入侵的详细历史。</color></size>",
    10: r"\n\n<size=85%><color=#999999>左键点击此图标可查看交互式金属流向明细；右键点击则显示传统纯文本摘要。</color></size>",
}


def main():
    batch = sys.argv[1] if len(sys.argv) > 1 else 'code_001'
    path = os.path.join(BATCH_DIR, batch + '.txt')
    lines = [l.rstrip('\n') for l in open(path, encoding='utf-8')]
    added = 0
    with open(MANUAL, 'a', encoding='utf-8', newline='\n') as f:
        for i, raw in enumerate(lines, 1):
            if i not in ZH:
                continue
            f.write(f'{raw}\t{ZH[i]}\n')
            added += 1
    print(f'{batch}: 写入 {added} 条（批内共 {len(lines)} 条，其余按日志跳过）')


if __name__ == '__main__':
    main()
