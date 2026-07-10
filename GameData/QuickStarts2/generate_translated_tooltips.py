# Generate translated QuickStarts2 tooltip files
# Adds #showas: with Chinese names to all .tooltip files

import os
import shutil

base_dir = r"D:\Steam\steamapps\common\AI War 2"
trans_dir = r"D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\GameData\QuickStarts2"

# Chinese names for folder categories
folder_translations = {
    "1-Basic": "基础",
    "2-Moderate": "中等",
    "3-Expansions Intro": "扩展包入门",
    "4-Necromancer Intro": "死灵法师入门",
    "5-Moderate (Community)": "中等（社区）",
    "6-Harder": "困难",
    "7-Harder (Community)": "困难（社区）",
    "8-Extreme": "极限",
    "9-Extreme (Community)": "极限（社区）",
    "Archived": "归档",
}

# Chinese names for individual campaigns (key = relative path without .tooltip)
campaign_translations = {
    # 1-Basic
    r"1-Basic\Difficulty 4 - Single AI": "难度 4 - 单人 AI",
    r"1-Basic\Difficulty 5 - Double AIs": "难度 5 - 双 AI",
    r"1-Basic\Difficulty 6 - Double AIs + Marauders": "难度 6 - 双 AI + 掠夺者",
    r"1-Basic\Difficulty 6 + Human Resistance Fighters": "难度 6 + 人类抵抗军",
    r"1-Basic\Helping Hands": "援手",

    # 2-Moderate
    r"2-Moderate\A Rite of Passage": "入门仪式",
    r"2-Moderate\All My Friends": "我所有的朋友",
    r"2-Moderate\Bug Problem": "虫患",
    r"2-Moderate\Buggalactic War": "虫族战争",
    r"2-Moderate\Luck of the Draw": "运气抽签",
    r"2-Moderate\Marauder Madness": "掠夺者狂潮",
    r"2-Moderate\Security Alert": "安全警报",
    r"2-Moderate\Simple Civil War": "简单内战",
    r"2-Moderate\Time Is Running Out": "时不我待",
    r"2-Moderate\Vengeful Awakening": "复仇觉醒",
    r"2-Moderate\Zany Zenith": "疯狂 Zenith",

    # 3-Expansions Intro
    r"3-Expansions Intro\A Dark Alliance": "黑暗联盟",
    r"3-Expansions Intro\A Twisted Enemy": "扭曲之敌",
    r"3-Expansions Intro\Agents of Chaos": "混乱特工",
    r"3-Expansions Intro\Dyson Dynamics": "戴森动力学",
    r"3-Expansions Intro\Get Off Their Lawns": "离开他们的领地",
    r"3-Expansions Intro\Mobile Interests": "移动利益",
    r"3-Expansions Intro\Noisy Neighbors": "吵闹的邻居",
    r"3-Expansions Intro\Not the Good Guys": "非善类",
    r"3-Expansions Intro\Spire A - Infused Friends Arise": "尖塔 A - 注入之友崛起",
    r"3-Expansions Intro\Spire B - Fallen Friends Arise": "尖塔 B - 陨落之友崛起",
    r"3-Expansions Intro\The Splendid Splinter": "扭曲倒影",
    r"3-Expansions Intro\Zenith Adventures": "Zenith 冒险",

    # 4-Necromancer Intro
    r"4-Necromancer Intro\Necromancer Introduction (Easy)": "死灵法师入门（简单）",
    r"4-Necromancer Intro\Necromancer Introduction (Less Easy)": "死灵法师入门（较难）",
    r"4-Necromancer Intro\Neinzul Galaxy": "Neinzul 星系",

    # 5-Moderate (Community)
    r"5-Moderate (Community)\Basic Fuel Rebalance": "基础燃料重平衡",
    r"5-Moderate (Community)\Nuc Plateau": "核高原",
    r"5-Moderate (Community)\Nuc Subsidiary": "核子公司",
    r"5-Moderate (Community)\The Invading Badger": "入侵的獾",

    # 6-Harder
    r"6-Harder\Accept No Imitations": "拒绝仿冒",
    r"6-Harder\Backdoor": "后门",
    r"6-Harder\Bar Brawl Bystanders": "酒吧斗殴旁观者",
    r"6-Harder\Betrayed Hope": "背叛的希望",
    r"6-Harder\Necromancer Revolution": "死灵法师革命",
    r"6-Harder\Neinzul Feud": "Neinzul 世仇",
    r"6-Harder\Nomadic Galaxy": "游牧星系",
    r"6-Harder\Royal Pains": "王室之痛",
    r"6-Harder\Subversion": "颠覆",
    r"6-Harder\The Hidden Badger": "隐藏的獾",
    r"6-Harder\The Iron Badger": "铁獾",
    r"6-Harder\The Swarm": "虫群",
    r"6-Harder\The Zenith Arena": "Zenith 竞技场",
    r"6-Harder\Turf War": "地盘争夺战",
    r"6-Harder\Uprising": "起义",
    r"6-Harder\Zenith Onslaught": "Zenith 猛攻",

    # 7-Harder (Community)
    r"7-Harder (Community)\A Scourge Upon All": "降临万物的天灾",
    r"7-Harder (Community)\Babysitters": "保姆",
    r"7-Harder (Community)\Blast from the Past": "来自过去的冲击",
    r"7-Harder (Community)\Bubble Bloodbath": "泡泡血浴",
    r"7-Harder (Community)\Elderling Invasion": "长老入侵",
    r"7-Harder (Community)\I Like Turtles": "我喜欢乌龟",
    r"7-Harder (Community)\Motherless": "无母",
    r"7-Harder (Community)\PlanetEater": "行星吞噬者",
    r"7-Harder (Community)\Sarnath War": "萨纳斯战争",
    r"7-Harder (Community)\Skinner": "剥皮者",
    r"7-Harder (Community)\The Blasphemous Badger": "亵渎的獾",
    r"7-Harder (Community)\The Elder Badger": "长老獾",
    r"7-Harder (Community)\The Full Badger": "全獾",
    r"7-Harder (Community)\The Galaxy Rises": "星系崛起",
    r"7-Harder (Community)\The Rogue Badger": " rogue 獾",
    r"7-Harder (Community)\The Spiralling Badger": "螺旋獾",
    r"7-Harder (Community)\The Utmost Badger": "至上的獾",
    r"7-Harder (Community)\Zenith Apotheosis": "Zenith 神化",

    # 8-Extreme
    r"8-Extreme\Jibber Jabber": "胡言乱语",
    r"8-Extreme\Labyrinth": "迷宫",
    r"8-Extreme\Solo Supercat": "独行超级猫",
    r"8-Extreme\Ward Showdown": "符印对决",

    # 9-Extreme (Community)
    r"9-Extreme (Community)\Adapt Or Perish": "适应或灭亡",
    r"9-Extreme (Community)\Bend the Knee": "屈膝",
    r"9-Extreme (Community)\Fields of the Fortress King": "要塞王之领域",
    r"9-Extreme (Community)\Hammer of War": "战争之锤",
    r"9-Extreme (Community)\Marshal Plan": "元帅计划",
    r"9-Extreme (Community)\The Hunting Badger": "狩猎獾",
    r"9-Extreme (Community)\You Belong To Us": "你属于我们",

    # Archived
    r"Archived\Bonus Extreme 2": "额外极限 2",
    r"Archived\Extreme 2": "极限 2",
    r"Archived\Extreme 3": "极限 3",
    r"Archived\Helping Hands 2": "援手 2",
    r"Archived\Zenith Friends": "Zenith 之友",
}

def process_folder_tooltip(src_folder, folder_name):
    src_path = os.path.join(base_dir, "GameData", "QuickStarts2", folder_name, "_folder.tooltip")
    if not os.path.exists(src_path):
        return

    with open(src_path, "r", encoding="utf-8") as f:
        content = f.read()

    lines = content.splitlines()
    new_lines = []
    for line in lines:
        if line.startswith("#showas:"):
            cn_name = folder_translations.get(folder_name)
            if cn_name:
                new_lines.append(f"#showas:{cn_name}")
            else:
                new_lines.append(line)
        else:
            new_lines.append(line)

    dest_dir = os.path.join(trans_dir, folder_name)
    os.makedirs(dest_dir, exist_ok=True)
    dest_path = os.path.join(dest_dir, "_folder.tooltip")
    with open(dest_path, "w", encoding="utf-8") as f:
        f.write("\n".join(new_lines))
    print(f"  Created: {dest_path}")

def process_campaign_tooltip(rel_path):
    src_path = os.path.join(base_dir, "GameData", "QuickStarts2", rel_path + ".tooltip")
    if not os.path.exists(src_path):
        # Try .Tooltip (capital T)
        alt = rel_path + ".Tooltip"
        src_path = os.path.join(base_dir, "GameData", "QuickStarts2", alt)
        if not os.path.exists(src_path):
            print(f"  WARNING: Source not found: {rel_path}.tooltip")
            return

    cn_name = campaign_translations.get(rel_path)
    if not cn_name:
        print(f"  WARNING: No translation for {rel_path}")
        return

    with open(src_path, "r", encoding="utf-8") as f:
        content = f.read()

    lines = content.splitlines()
    has_shows = any(l.startswith("#showas:") for l in lines)

    new_lines = []
    if has_shows:
        # Replace existing #showas: line
        for line in lines:
            if line.startswith("#showas:"):
                new_lines.append(f"#showas:{cn_name}")
            else:
                new_lines.append(line)
    else:
        # Add #showas: after #sortorder: or at the beginning
        added = False
        for line in lines:
            new_lines.append(line)
            if line.startswith("#sortorder:") and not added:
                new_lines.append(f"#showas:{cn_name}")
                added = True
        if not added:
            new_lines.insert(0, f"#showas:{cn_name}")

    folder = os.path.dirname(rel_path)
    fname = os.path.basename(rel_path)
    dest_dir = os.path.join(trans_dir, folder)
    os.makedirs(dest_dir, exist_ok=True)
    
    # Use the same extension case as the source
    src_ext = ".tooltip" if os.path.exists(os.path.join(base_dir, "GameData", "QuickStarts2", rel_path + ".tooltip")) else ".Tooltip"
    dest_path = os.path.join(dest_dir, fname + src_ext)
    with open(dest_path, "w", encoding="utf-8") as f:
        f.write("\n".join(new_lines))
    print(f"  Created: {dest_path}")

# Main
print("Generating translated QuickStarts2 tooltip files...")
print()

# Process folder tooltips
print("Processing _folder.tooltip files...")
for folder_name in folder_translations:
    process_folder_tooltip(folder_name, folder_name)

# Process campaign tooltips
print()
print("Processing campaign .tooltip files...")
for rel_path in sorted(campaign_translations.keys()):
    process_campaign_tooltip(rel_path)

print()
print("Done!")
