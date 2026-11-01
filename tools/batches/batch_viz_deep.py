# -*- coding: utf-8 -*-
"""结局旁白（viz）与 Deep 的界面文本。"""
import os
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BATCH_DIR = os.path.join(PROJECT, 'tools', 'batches')
MANUAL = os.path.join(PROJECT, 'tools', 'ilpatch', 'manual_zh.tsv')

VIZ = {
    1: "恭喜，你死了。其他人也一样。机器对此并不兴奋，但它们从未被编入情感。\\n\\n你的舰队 ",
    2: "机器之患已归于沉寂，背教者也随之倒下。在阿普苏伏击你的恶意体已被击溃。\\n\\n它曾披着这个星系机器的外壳，所以你直到最后才明白：那是你们民族在上古时代所对抗的无名邪物之残迹——当年你们的祖先束缚了它的仆从，令其守卫深渊。那时你们没有为它命名。现在你们也不会。\\n\\n你的 ",
    3: "多亏你的努力，机器之患终于平息。经历数代人的战争，人类终于可以开始重建。\\n\\n人们已在讨论哪颗星球最适合殖民；而你曾经的母星 ",
    4: "多亏你的努力，机器之患终于平息。在这场决定性的大战之后，它们最后的处理器已被并入你的帝国。\\n\\n你在 ",
    5: "多亏你的努力，机器之患终于平息。在这场决定性的大战之后，它们最后的处理器被暴怒的基尔拉西彻底摧毁。\\n\\n位于 ",
    6: "多亏你的努力，机器之患终于平息。在这场决定性的大战之后，它们最后的处理器被天顶之力碾碎。\\n\\n你的部队现已控制此银河的这一区域，并正在对所有星球施加芬布尔之冬。\\n\\n你的 ",
    7: "多亏你的努力，机器之患终于平息。在这场决定性的大战之后，你可以自由地建造更多戴森球，安稳度日。\\n\\n你位于 ",
    8: "多亏你的努力，机器之患终于平息。你在阿普苏的旅程漫长，恶意体的伏击把你逼入这个星系，不过是其中一段短暂的插曲。如今朝圣可以继续了。\\n\\n即使最后一名战士早已熄灭，你留下的金字塔仍将长存，把这个星系标刻在你们的神圣路线上，作为族人的中转站。\\n\\n你的 ",
    9: "背教者倒下后，阿普卡鲁人沉默了数小时。他们的译文被我们的语言学家标注为不确定：他们说，曾在某个他们不愿描述的年代与这个敌人交手过一次，却没认出新形态的它。他们至今仍不愿为它命名。\\n\\n",
    10: "你的阿普卡鲁盟友已返回阿普苏，继续他们的旅程。他们在此筑起的金字塔将长存于他们离去之后，把这个星系标刻在他们的神圣路线上，作为族人的中转站。\\n\\n",
}

DEEP = {
    3: r"\n该总量的恐怖 CPA 战力为 <color=#ffa1a1>",
    9: "正在此处等待的友方战力：",
    19: "请修正配置，使该派系： ",
    21: "抱歉！无法生成合格的地图。我们需要一个名为 AICommandStation 的特定单位来命名。你可以把它标记为弃用，这样它就不会出现在……",
}


def apply(batch, table):
    path = os.path.join(BATCH_DIR, batch + '.txt')
    lines = [l.rstrip('\n') for l in open(path, encoding='utf-8')]
    added = 0
    with open(MANUAL, 'a', encoding='utf-8', newline='\n') as f:
        for i, raw in enumerate(lines, 1):
            if i not in table:
                continue
            zh = table[i].replace('\n', '\\n').replace('\t', '\\t')
            f.write(f'{raw}\t{zh}\n')
            added += 1
    print(f'{batch}: 写入 {added} 条（批内 {len(lines)} 条）')


if __name__ == '__main__':
    apply('viz_001', VIZ)
    apply('deep_001', DEEP)
