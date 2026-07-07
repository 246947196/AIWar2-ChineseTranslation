# C# 源码汉化补完计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复已翻译 C# 文件中残留的中英混杂字符串，并翻译两个大型悬浮提示文件中的未翻译文本。

**Architecture:** 逐文件逐字符串替换，遵循现有翻译规范：只改 `"..."` 内文本，保留 `{变量}` 和 `<color>` 标签，用 `''` 替代中文引号。

**Tech Stack:** C# (.NET Framework 4.7.1/4.7.2), Roslyn 4.12.0

## Global Constraints

- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- 每翻译完一个 Task 后运行 `.\build.ps1` 编译验证，0 错误再继续
- **Debug 日志、内部标识符、错误码不翻译** — 如 "BUG!", "CODE ", "DEBUG_", "ERROR_", "OutOfRange" 等
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件
- **简洁优先**：工具提示文本应简洁明了，避免冗长翻译
- **格式保留**：保留空格、换行符 `\n`、HTML 标签和 Unity 富文本标签

---

### Task 1: 修正 TODO/BUG 中英混杂字符串（4 个文件）

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/DescriptionAppenders/DLC2/ZenithMinersDescriptionAppender.cs:39`
- Modify: `DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/HarvesterDescriptionAppender.cs:21`
- Modify: `DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/SporeDescriptionAppender.cs:21`
- Modify: `DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/TeliumDescriptionAppender.cs:21`
- Modify: `DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/TeliumDescriptionAppender.cs:27`

- [ ] **Step 1: 修复 ZenithMinersDescriptionAppender.cs 的 "TODO"**

将 `"TODO: 为此效果定义附加数据 "` 改为 `"待办：为此效果定义附加数据 "`

- [ ] **Step 2: 修复 HarvesterDescriptionAppender.cs 的 "BUG"**

将 `"无法在此处找到MacrophageFactionBaseInfo。这是一个BUG"` 改为 `"无法在此处找到MacrophageFactionBaseInfo。这是一个错误"`

- [ ] **Step 3: 修复 SporeDescriptionAppender.cs 的 "BUG"**

将 `"无法在此处找到MacrophageFactionBaseInfo。这是一个BUG"` 改为 `"无法在此处找到MacrophageFactionBaseInfo。这是一个错误"`

- [ ] **Step 4: 修复 TeliumDescriptionAppender.cs 的两处 "BUG"**

将第 21 行 `"无法在此处找到MacrophageFactionBaseInfo。这是一个BUG"` 改为 `"无法在此处找到MacrophageFactionBaseInfo。这是一个错误"`

将第 27 行 `"此泰利姆的 tData 为空。这是一个BUG。"` 改为 `"此泰利姆的 tData 为空。这是一个错误。"`

- [ ] **Step 5: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 2: 翻译 Window_InGameHoverEntityInfo.cs

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs:9161-9163`

该文件 9185 行，仅有 2 处 `Buffer.Add` 为英文调试文本（位于末尾 `AnyUnitExampleAppender` 类），其余 `Buffer.Add` 调用或是代码逻辑（HTML 标签、颜色代码）或已在其他 appender 中处理。

- [ ] **Step 1: 翻译第 9161 行的调试文本**

将 `"Hey, I'm talking to you from the sidebar or the build menu, probably!  Not hovering over a specific unit."` 改为 `"嘿，这是侧边栏或建造菜单的提示！当前没有悬停于具体单位。"`

- [ ] **Step 2: 翻译第 9163 行的调试文本**

将 `"Hey, I'm hovering over a specific unit at location: "` 改为 `"嘿，当前悬停于位置："`

- [ ] **Step 3: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 3: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — 分类标签 Section 1（Greater/Lesser Category 标签）

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

**范围：** 第 9331-9357 行，Greater Category 标签（详细/中等两种模式）

- [ ] **Step 1: 翻译 Vulnerabilities 标签（第 9331, 9333 行）**

```
"Vulnerabilities: " → "弱点："
"Vul: " → "弱："
```

- [ ] **Step 2: 翻译 Resistances 标签（第 9339, 9341 行）**

```
"Resistances: " → "抗性："
"Res: " → "抗："
```

- [ ] **Step 3: 翻译 Immunities 标签（第 9347, 9349 行）**

```
"Immunities: " → "免疫："
"Imu: " → "免："
```

- [ ] **Step 4: 翻译 Other 标签（第 9355, 9357 行）**

```
"Other: " → "其他："
"Oth: " → "他："
```

- [ ] **Step 5: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 4: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — Lesser Category 标签

**范围：** 第 9407-9425 行，Lesser Category 子分类标签

- [ ] **Step 1: 翻译 6 个子分类标签**

```
"Debuffs: " → "减益："
"Death Effects: " → "死亡效果："
"Ammo Types: " → "弹药类型："
"Exotic Damage: " → "异种伤害："
"General Damage: " → "通用伤害："
"Special Mechanic: " → "特殊机制："
```

- [ ] **Step 2: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 5: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — Buff/ShipClass 通用文本

**范围：** 第 9496-9604 行的通用标签文本

- [ ] **Step 1: 翻译 Debuff 相关（第 9496, 9498 行）**

```
"All Debuffs" → "全部减益"
"Debuffs" → "减益"
```

- [ ] **Step 2: 翻译死亡效果相关（第 9556, 9558 行）**

```
"All Death Effects" → "全部死亡效果"
"Death Effects" → "死亡效果"
```

- [ ] **Step 3: 翻译僵尸化相关（第 9569, 9571 行，虽注释掉但仍保留翻译）**

```
"All Zombifying Types" → "全部僵尸化类型"
"Zombifying Types" → "僵尸化类型"
```

- [ ] **Step 4: 翻译完全免疫相关（第 9602, 9604 行）**

```
"Immune to all damage" → "免疫所有伤害"
"Invulnerable" → "无敌"
```

- [ ] **Step 5: 翻译 Debuff modifier 名称（第 9504-9549 行）**

`WriteShipClass_ModifierData` 调用中的名称参数（长名和短名）：

```
"Engine Slow", "E Slow" → "引擎减速", "引减"
"Weapon Slow", "W Slow" → "武器减速", "武减"
"Paralysis", "Stun" → "瘫痪", "晕眩"
"Acid", "Acid" → "腐蚀", "腐蚀"
"Weapon Phasing", "Phase" → "武器相位", "相位"
"Knockback", "Knock" → "击退", "击退"
"Tachyon Beams", "Tach" → "超光速粒子束", "粒子"
"Gravitic Cores", "Grav" → "引力核心", "引力"
```

- [ ] **Step 6: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 6: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — 特殊机制和伤害类型文本

**范围：** 第 9630-9706 行的 Exotic Damage 和 Special Mechanics 文本

- [ ] **Step 1: 翻译 Exotic Damage 修饰符名称（第 9630-9659 行）**

```
"All Exotic Damage", "Exotic Damage" → "全部异种伤害", "异种伤害"
"Attrition", "Attr" → "磨损", "磨损"
"Electrotoxicity", "ETox" → "电毒性", "电毒"
"Revenge Shots", "Veng" → "复仇射击", "复仇"
"Ion Cannon", "Ion" → "离子炮", "离子"
```

- [ ] **Step 2: 翻译特殊机制文本（第 9669-9706 行）**

```
"Tractor Beams  Black Hole Machines  Getting Devoured  Getting Infested" → "牵引光束  黑洞机器  被吞噬  被感染"
"All Special Mechanics" → "全部特殊机制"
"Tractor Beams " → "牵引光束 "
"Tractors " → "牵引 "
"Black Hole Machines " → "黑洞机器 "
"Black Holes " → "黑洞 "
"Getting Devoured " → "被吞噬 "
"Devouring " → "吞噬 "
"Getting Infested " → "被感染 "
"Infestation " → "感染 "
```

- [ ] **Step 3: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 7: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — Buff/辅助/能量文本

**范围：** 第 9726-10098 行剩余英文文本

- [ ] **Step 1: 翻译 Buff Limits 相关（第 9726, 9767-9791 行）**

```
"Buff Limits:" → "增益上限："
" Cannot be buffed. " → " 无法获得增益。 "
" Damage: " → " 伤害："
" Dmg " → " 伤 "
" Hull: " → " 船体："
" Hull " → " 船体 "
" Shield: " → " 护盾："
" Shd " → " 护盾 "
" Speed: " → " 速度："
" Spd " → " 速度 "
```

- [ ] **Step 2: 翻译 Supercharging 相关（第 9747, 9832-9849 行）**

```
" Cannot Supercharge:" → " 无法超载："
" Damage " → " 伤害 "
" Hull " → " 船体 "
" Shield " → " 护盾 "
" Speed " → " 速度 "
```

- [ ] **Step 3: 翻译 ModifierData 单位后缀（第 9456, 9458, 9463, 9465 行）**

```
" duration " → " 持续时间 "
" damage " → " 伤害 "
" dur " → " 持续 "
" dmg " → " 伤 "
"x" → "倍"
"s" → "秒"
```

- [ ] **Step 4: 翻译辅助文本（第 9920, 10096, 10098 行）**

```
"Within " → "范围内 "
"its owners and allies with " → "其拥有者和盟友 "
"its enemies with " → "其敌人 "
```

- [ ] **Step 5: 编译验证**

Run: `.\build.ps1`
Expected: 所有项目编译 0 错误

---

### Task 8: 翻译 Window_InGameHoverEntityInfo.cs — 游戏机制/状态描述文本

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

**范围：** 第 4784-5604 行，涵盖舰队状态、威胁舰队、外银河打击、工厂/建造、黑客、指挥站等玩家可见的描述文本。

翻译原则：
- 以下内部/调试标识符保持英文：`"BUG! "`, `"CODE "`, `"ERROR_WRITE_NICE_TEXT: "`, `"Debug_"`, 变量名
- 其余玩家可见描述文本全部翻译

关键字符串对照表（按行号范围分组）：

#### 舰队状态描述（第 4784-4960 行）
```
"This type of ship will stay threatfleet rather than joining the Hunter. " → "此类舰船将留在威胁舰队中，而不会加入猎手。"
"This ship will stay threatfleet, since it is not targeting humans. " → "此舰船将留在威胁舰队中，因为它不针对人类。"
"This ship will stay threatfleet, since it s not linked to the Sentinels hivemind for some reason. " → "此舰船将留在威胁舰队中，因其未链接到哨兵蜂巢思维。"
"This ship will leave the threatfleet and join the Hunters after " → "此舰船将在 " 后离开威胁舰队加入猎手
"Part of an Exostrike heading for " → "属于外银河打击部队，目标为 "
"Part of and Exostrike heading for " → "属于外银河打击部队，目标为 "
"Guarding " → "守卫 "
"This entity will despawn in " → "此实体将在 " 后消失
"Is " → "是 " (舰队旗舰描述)
"centerpiece" → "旗舰"
"I am the " → "我是 "
" of fleet " → " 的舰队 "
"This flagship has been crippled " → "此旗舰已被重创 "
"Ships unloaded have " → "卸载的舰船有 "
" seconds delay before firing.  " → " 秒的开火延迟。"
"Ships unloaded from this flagship have a delay of " → "从此旗舰卸载的舰船有 "
" seconds before they can shoot.  " → " 秒后才能开火。"
"This fleet is on friendly planets, and can rebuild its ships quicker.  " → "此舰队位于友方星球，可以更快重建舰船。"
"In transport-ready mode.  " → "运输就绪模式。"
"My fleet is in transport-ready mode, where all ships try to get into my bays.  " → "我的舰队处于运输就绪模式，所有舰船正试图进入舱位。"
```

#### 黑客/隐形/特殊状态（第 5086-5230 行）
```
"This unit is currently hacking; it is diverting engine power to the Hacking Matrix, so it is slowed and cannot leave the planet. " → "此单位正在黑客入侵；它将引擎能量转移到黑客矩阵，因此速度降低且无法离开星球。"
"Hacking disables a ships cloaking system. " → "黑客入侵会禁用舰船的隐形系统。"
"The AI will generate Exostrikes (Exogalactic Strikeforces) against you if you capture this structure. " → "如果你占领此建筑，AI 将派出外银河打击部队对付你。"
"This unit only stays cloaked if its faction owns its planet. " → "此单位仅在其阵营控制该星球时保持隐形。"
```

#### 工厂/增援/建造描述（第 5230-5604 行）
```
"This AI Reinforcement Point contains " → "此 AI 增援点包含 "
"Contains " → "包含 "
"Reinforcement Debug Reason Codes: " → "增援调试原因代码："
"Crippled factories are unable to spend metal until they are repaired. " → "受损工厂在修复前无法消耗金属。"
"Non-functional factories are unable to spend metal until their functionality is restored. " → "失效工厂在功能恢复前无法消耗金属。"
"Disabled factories are unable to spend metal until they are re-enabled. " → "已禁用的工厂在重新启用前无法消耗金属。"
"One or more fleets of relevance are in range of this factory, but they already have full ship caps and so there's nothing to do.  " → "一个或多个相关舰队在此工厂范围内，但已达舰船上限，无需行动。"
"You must unpause the game briefly before you can see what supporting factories are.  " → "你必须短暂取消暂停才能查看支持工厂的信息。"
"No (non-crippled) " → "没有（未受损的）"
" fleets of my faction are on this or adjacent planets, so I can't build anything for anyone.  " → " 类型的我方舰队在此星球或邻近星球上，因此无法为任何人建造。"
"No (non-crippled) mobile fleets of my faction are on this or adjacent planets, so I can't build anything for anyone.  " → "没有（未受损的）我方机动舰队在此或邻近星球上，因此无法为任何人建造。"
"Debug Construction Blocked: " → "调试：建造被阻止："
"I am the centerpiece of what would become a fleet with the following items:  " → "我是将成为以下内容舰队的旗舰："
```

#### 额外插槽/标记等级（第 5497-5512 行）
```
"Grants " → "提供 "
" additional build slots if you own this." → " 个额外建造插槽（如果你拥有此建筑）。"
"Must be at mark level " → "必须达到标记等级 "
" to build this structure. " → " 才能建造此建筑。"
```

#### 堆叠/旗舰模式（第 5557-5604 行）
```
"This stack takes damage like normal, but shoots " → "此堆叠正常承受伤害，但发射 "
"x</color> the normal amount of shots.  " → "倍</color> 的正常射击量。"
"Hold " → "按住 "
"You can find out more about this, and change its mode, on the Fleets sidebar tab.  Find this flagship's fleet and click it.  " → "你可以在舰队侧边栏页签中了解更多信息并更改其模式。找到此旗舰的舰队并点击它。"
```

**注意：** 第 5604 行的 `"BUG!  OutOfRange!"` 为调试字符串，不翻译。

- [ ] **Step 1: 翻译舰队状态描述（第 4784-4960 行）**
- [ ] **Step 2: 翻译黑客/隐形/特殊状态（第 5086-5230 行）**
- [ ] **Step 3: 翻译工厂/增援/建造描述（第 5230-5512 行）**
- [ ] **Step 4: 翻译堆叠/旗舰模式（第 5557-5604 行）**
- [ ] **Step 5: 编译验证**

Run: `.\build.ps1`
Expected: 0 错误

---

### Task 9: 翻译 Window_InGameHoverEntityInfo.cs — 武器/战斗系统描述

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

**范围：** 第 5604-7000 行，涵盖武器系统描述（AOE、光束、链式闪电、镜面武器等）

因字符串数量大且模式重复，需逐行翻译。关键模式对照：

#### 目标优先级/错误调试（第 5604-5661 行，保留原文）
```
"targetPriorityList is empty!" → 保持（调试）
"targetPriorityList.Count: " → 保持（调试）
"Squad Faction ReasonCode: " → 保持（调试）
```

#### 坐标/状态原因（第 5910-6064 行）
```
"Current ship coordinates: " → 保持（调试）
"Non-functional - its owning faction must control this planet!" → "失效 - 所属阵营必须控制此星球！"
"In Unexplored Space - unable to function without scouts having ever been sent here!" → "在未探索空间 - 没有侦察兵到过此地则无法运作！"
"Not yet fully claimed, and also paused so that it will NOT be claimed." → "尚未完全占领，且已暂停，因此不会被占领。"
"Not yet fully claimed." → "尚未完全占领。"
"In 'hold fire' mode." → "停火模式。"
"In 'pause function' mode." → "功能暂停模式。"
"Still under construction (" → "仍在建造中（"
"Faction does not control this planet." → "阵营未控制此星球。"
"Faction does not have enough energy." → "阵营能量不足。"
"Not enough " → "不足"
" at this planet." → "，在此星球。"
"Stored metal is zero." → "存储金属为零。"
"Is not remains!" → "不是残骸！"
"Must wait another " → "必须等待 "
"Must only wait another " → "只需再等待 "
"Command Stations cannot be rebuilt on enemy planets." → "指挥站不能在敌方星球重建。"
"Rebuilding would put you into negative energy." → "重建会导致能量为负。"
```

#### 棕色断电/力场描述（第 6081-6084 行）
```
"Brownout: Bubble forcefields down for another " → "电力不足：气泡力场将在 "
"Brownout: Had negative energy balance!  Your bubble forcefields won't be able to project their protective field for another " → "电力不足：能量平衡为负！你的气泡力场将在 "
```

#### 错误/异常（第 6227 行）
```
"Exception during generation of tooltip!" → "生成工具提示时出现异常！"
```

#### 武器统计描述（第 6298-6900 行）

常见的武器系统描述，如：
```
"Launches " → "发射 "
"Reload Speed of " → "装弹速度 "
"Reload " → "装弹 "
"BURST FIRE" → "爆发射击"
"Will deal an additional " → "将额外造成 "
" corrosive damage. " → " 腐蚀伤害。"
"All damage applied is corrosive damage. " → "所有伤害均为腐蚀伤害。"
"Range of " → "范围 "
"When striking the hull of a targets with no personal shields left that also use less than " → "当击中无个人护盾且使用少于 "
"When striking the hull of a target with no personal shields left, causes them to change to " → "当击中无个人护盾的目标船体时，使其改变为 "
"When striking any target that uses less than " → "当击中任何使用少于 "
"When striking the any target, causes it to change to " → "当击中任何目标时，使其改变为 "
```

AOE 相关：
```
"DMG split between targets, " → "伤害在目标间分摊，"
"all targets in range" → "范围内所有目标"
"spreading its damage among " → "在其间分摊伤害 "
"at most <color=#ffdf72>" → "最多 <color=#ffdf72>"
"</color> targets" → "</color> 个目标"
"doing its full damage to the primary target, and " → "对主要目标造成全额伤害，并"
"doing their full damage to " → "对其造成全额伤害"
"spreading their damage among " → "在其间分摊伤害"
```

光束/链式闪电：
```
"hitting the main intended target for full damage, then hits <color=#ffdf72>" → "击中主要目标造成全额伤害，然后击中 <color=#ffdf72>"
" targets with a second copy of damage, divided evenly among all those hit, max damage per beam <color=#ffdf72>" → " 个目标，造成第二份伤害并在所有目标间平均分摊，每束最大伤害 <color=#ffdf72>"
"hitting the main intended target for full damage, then hits everything else with a second copy of damage, divided evenly among all those hit max damage per beam <color=#ffdf72>" → "击中主要目标造成全额伤害，然后对所有其他目标造成第二份伤害，平均分摊，每束最大伤害 <color=#ffdf72>"
"Note that because of multiple beams potentially hitting a single target, closer or larger targets tend to take more damage.  " → "注意：由于多束光束可能击中同一目标，更近或更大的目标会承受更多伤害。"
"up to <color=#ffdf72>" → "最多 <color=#ffdf72>"
"</color> overall targets " → "</color> 个总目标"
"any number of overall targets " → "任意数量的总目标"
"with a chain lightning attack that jumps <color=#ffdf72>" → "链式闪电攻击，跳跃 <color=#ffdf72>"
"</color> times " → "</color> 次"
"with chain range <color=#ffdf72>" → "链式范围 <color=#ffdf72>"
"Each time the lightning jumps, it can strike " → "每次闪电跳跃可击中 "
"up to <color=#ffdf72>" → "最多 <color=#ffdf72>"
"</color> targets in the next cycle.  " → "</color> 个目标在下一周期。"
"any number of targets in the next cycle.  " → "任意数量目标在下一周期。"
"Each target after the primary takes " → "主要目标后的每个目标承受 "
"Each target after the primary takes full damage.  " → "主要目标后的每个目标承受全额伤害。"
```

光束穿透：
```
"hitting <color=#ffdf72>" → "击中 <color=#ffdf72>"
"</color> targets" → "</color> 个目标"
"hitting all targets intersected by the beam" → "击中光束路径上的所有目标"
```

射击限制：
```
"Only " → "仅 "
" can be fired per target and stack. " → " 可对每个目标和堆叠发射。"
"All can be aimed at the same target. " → "均可瞄准同一目标。"
"A single target may be damaged by multiple intersecting beams.  Each beam does the full damage listed above.  " → "单个目标可能被多束相交光束击中。每束光束造成全额伤害。"
"Each beam does the full damage listed above.  " → "每束光束造成全额伤害。"
" can be fired per target and stack" → " 可对每个目标和堆叠发射"
"All can be aimed at the same target" → "均可瞄准同一目标"
```

镜面武器：
```
"This mirror weapon fires a projectile back with " → "此镜面武器反射一枚射弹，"
"Fires a projectile back with power based on shot impact to parent unit.  " → "基于击中母舰的冲击力反射一枚射弹。"
```

引擎减速/击晕：
```
"s</color> if target engine < <color=#ffdf72>" → "秒</color> 如果目标引擎 < <color=#ffdf72>"
"s</color> if the target has an engine power less than <color=#ffdf72>" → "秒</color> 如果目标引擎动力低于 <color=#ffdf72>"
"The target can be slowed up to a full " → "目标可被减速最多 "
"The more stun-seconds accumlated on a target, the slower it goes. 4s = 50% move speed, 7s+ = immobilized.  " → "目标累积的眩晕秒数越多，速度越慢。4秒 = 50%移动速度，7秒以上 = 无法移动。"
"s</color> if target mass < <color=#ffdf72>" → "秒</color> 如果目标质量 < <color=#ffdf72>"
"s</color> if the target has a mass less than <color=#ffdf72>" → "秒</color> 如果目标质量低于 <color=#ffdf72>"
```

装甲穿透：
```
"s</color> if armor < <color=#ffdf72>" → "秒</color> 如果护甲 < <color=#ffdf72>"
"mm</color>, max " → "毫米</color>，最大 "
"s.  " → "秒。"
"s</color> if the target has an armor thickness of less than <color=#ffdf72>" → "秒</color> 如果目标护甲厚度低于 <color=#ffdf72>"
"mm</color>.  The total amount of extra reload time per target that can be applied is " → "毫米</color>。每个目标可施加的额外装弹时间总量为 "
```

推/拉效果：
```
"Targets pushed <color=#ffdf72>" → "目标被推开 <color=#ffdf72>"
"Targets hit by the AoE pushed <color=#ffdf72>" → "被AOE击中的目标被推开 <color=#ffdf72>"
"Targets pulled <color=#ffdf72>" → "目标被拉向 <color=#ffdf72>"
"Targets hit by the AoE pulled <color=#ffdf72>" → "被AOE击中的目标被拉向 <color=#ffdf72>"
"pushed away from " → "被推开远离 "
"pulled towards " → "被拉向 "
"this ship. " → "此舰船。"
"the center of the AoE of this ship's shots. " → "此舰船射击的AOE中心。"
"The maximum distance a ship can be pushed is <color=#ffdf72>" → "舰船可被推开的距离上限为 <color=#ffdf72>"
"The maximum distance a ship can be pulled is <color=#ffdf72>" → "舰船可被拉近的距离上限为 <color=#ffdf72>"
"tX</color>.  " → "倍</color>。"
```

- [ ] **Step 1: 翻译状态原因和棕色断电文本（第 5910-6084 行）**
- [ ] **Step 2: 翻译 AOE 和光束描述（第 6298-6900 行）**
- [ ] **Step 3: 翻译链式闪电和镜面武器（第 6900-7000 行）**
- [ ] **Step 4: 翻译引擎减速/装甲/推拉效果（第 7000-7421 行）**
- [ ] **Step 5: 编译验证**

---

### Task 10: 翻译 Window_InGameHoverEntityInfo.cs — 模块/隐形/数据文本

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

**范围：** 第 7576-9185 行，涵盖模块属性增强、隐形、牵引、伤害中止代码、重创状态、生成组等

#### 模块属性增强（第 7576-7619 行）
```
"Hull Health Multiplied By " → "船体生命值倍率 "
"x" → "倍"
"Shield Health Multiplied By " → "护盾生命值倍率 "
"Bubble Forcefield added with " → "气泡力场添加，"
"x normal personal shield rating." → "倍正常个人护盾评级。"
"Bubble Forcefield replaces personal shield; no shield strength change." → "气泡力场替换个人护盾；护盾强度不变。"
```

#### 隐形系统（第 7682-7695 行）
```
"Max Cloaking Points: <color=#ffdf72>" → "最大隐形点数：<color=#ffdf72>"
"Cloaking" → "隐形"
": This ship has <color=#ffdf72>" → "：此舰船有 <color=#ffdf72>"
"Every time this ship fires, it will expend " → "此舰船每次开火将消耗 "
"After " → "在 "
" seconds of not losing any cloaking points, this ship will regain all of the lost cloaking points.  " → " 秒未损失隐形点数后，此舰船将恢复所有损失的点数。"
```

#### 牵引/范围（第 7723-7779 行）
```
"They can still move freely, pulling this unit with them, but they can't leave the current planet.  " → "它们仍可自由移动，拖着此单位，但无法离开当前星球。"
"infinite range" → "无限范围"
"range " → "范围 "
"x</color>, only target engines < <color=#ffdf72>" → "倍</color>，仅目标引擎 < <color=#ffdf72>"
"All enemy squads on-planet" → "星球上所有敌方小队"
"All enemy squads within range " → "范围内所有敌方小队"
"x</color> their normal speed if they have an engine power less than <color=#ffdf72>" → "倍</color> 正常速度，如果引擎动力低于 <color=#ffdf72>"
```

#### 伤害中止代码（第 7817-7880 行，保留调试标识符）
以下为调试字符串，保持英文：
`"Immune to All Damage"`, `"Newly-Created Immunity To Damage"`, `"External Invulnerability"`, `"foundProtectorButCouldNotHitDueToFiniteHitCountAOE"`, `"Debug_IgnoresDamage"`, `"HonorFiniteHitCountAOE and not in list"`, `"Calculated Zero Damage!"`, `"Damage-Drop-During-Hit"`, `"Health-Of-Target-Zero"`, `"Overdrives-Shields-No-Shields"`, `"Shooting Dead Target"`, `"Only Fires On Death"`, `"Maintain Cloak When No Direct Target"`, `"Empty Target List"`, `"All Targets Out Of Range"`, `"No Viable Targets"`

#### 重创状态（第 7894-7899 行）
```
"Crippled - will not die, but needs to be repaired (at " → "重创 - 不会死亡，但需要修复（"
"x normal cost) to full health to function again!" → "倍正常费用）至满血才能恢复功能！"
"Crippled - will not die, but needs to be repaired to full health to function again!" → "重创 - 不会死亡，但需要修复至满血才能恢复功能！"
```

#### 物质状态/生成组（第 8125-8653 行）
```
"Must be " → "必须为 "
" state of matter to function.  " → " 物质状态才能运作。"
"None" → "无"
"Primary: " → "主要："
"Secondary: " → "次要："
"Tertiary: " → "第三："
"Quaternary: " → "第四："
"Quinary: " → "第五："
"Senary: " → "第六："
"Septenary: " → "第七："
"Octonary: " → "第八："
"Nonary: " → "第九："
"Denary: " → "第十："
"Group " → "组 "
"Between " → "介于 "
" and " → " 和 "
"x" → "倍"（仅在生成值上下文）
```

#### 转换计时器/属性标签（第 8888-9105 行）
```
"This will expire after " → "此将在 "
"This will expire in " → "此将在 "
"Could not find a " → "找不到 "
"Strength: " → "战力："
"Hull: " → "船体："
"Shield: " → "护盾："
"Metal: " → "金属："
"Energy: " → "能量："
"Argon: " → "氩气："
"Radon: " → "氡气："
"Xenon: " → "氙气："
```

- [ ] **Step 1: 翻译模块增强和隐形系统（第 7576-7695 行）**
- [ ] **Step 2: 翻译牵引/范围（第 7723-7779 行）**
- [ ] **Step 3: 翻译重创和物质状态（第 7894-8279 行）**
- [ ] **Step 4: 翻译生成组和属性标签（第 8594-9105 行）**
- [ ] **Step 5: 编译验证**

---

### Task 11: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — 状态/武器描述

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

**范围：** 第 4900-6500 行，涵盖建造拒绝原因、武器系统描述（AOE、光束、链式闪电等）

此文件中的大量武器描述与 Task 9 中的模式相同，可直接参照翻译。

关键差异字符串：

#### 建造拒绝原因（第 4908-5008 行）
```
"In 'pause function' mode." → "功能暂停模式。"
"Still under construction (" → "仍在建造中（"
"Was destroyed and not yet rebuilt." → "已被摧毁，尚未重建。"
"Faction does not control this planet." → "阵营未控制此星球。"
"Faction does not have enough energy." → "阵营能量不足。"
"Not enough " → "不足"
" at this planet." → "，在此星球。"
"Stored metal is zero." → "存储金属为零。"
"Is not remains!" → "不是残骸！"
"Must wait another " → "必须等待 "
"Must only wait another " → "只需再等待 "
"Command Stations cannot be rebuilt on enemy planets." → "指挥站不能在敌方星球重建。"
"Rebuilding would put you into negative energy." → "重建会导致能量为负。"
"Brownout: Bubble forcefields down for another " → "电力不足：气泡力场将在 "
"Brownout: Had negative energy balance!  Your bubble forcefields won't be able to project their protective field for another " → "电力不足：能量平衡为负！你的气泡力场将在 "
```

**注意：** 武器系统描述（BURST FIRE、AOE、光束、链式闪电、引擎减速、装甲、推拉等）与 Task 9 几乎完全一致，参照 Task 9 的翻译表逐行替换即可。

- [ ] **Step 1: 翻译建造拒绝原因（第 4908-5008 行）**
- [ ] **Step 2: 翻译 BURST FIRE 和弹药系统（第 5354-5600 行）**
- [ ] **Step 3: 翻译 AOE/光束描述（第 5600-5900 行）**
- [ ] **Step 4: 翻译链式闪电/镜面武器（第 5900-5960 行）**
- [ ] **Step 5: 翻译引擎减速/装甲（第 5959-6250 行）**
- [ ] **Step 6: 编译验证**

---

### Task 12: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — 伤害修饰符/命令/统计文本

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

**范围：** 第 6250-10672 行，涵盖推拉效果、伤害修饰符条件、模块/隐形、命令文本、统计数据

#### 推拉效果（第 6292-6348 行）
与 Task 9 中对应字符串翻译一致。

#### 模块属性增强（第 6545-6621 行）
与 Task 10 中对应字符串翻译一致。

#### 隐形系统（第 6684-6725 行）
与 Task 10 中对应字符串翻译一致。

#### 伤害修饰符标签（第 6855-7242 行）
```
"max hull health" → "最大船体生命值"
"current hull health" → "当前船体生命值"
"max personal shields" → "最大个人护盾"
"current missing personal shields" → "当前缺失的个人护盾"
"current missing hull health" → "当前缺失的船体生命值"
"max bubble forcefield strength" → "最大气泡力场强度"
"current personal shields" → "当前个人护盾"
"is" → "是"
"current speed" → "当前速度"
"armor" → "护甲"
"energy usage" → "能量消耗"
"albedo" → "反照率"
"mass" → "质量"
"engine power" → "引擎动力"
"has been here for" → "已在此停留"
"have been here for" → "已在此停留"
"type" → "类型"
```

#### 伤害修饰符条件（第 7156-7242 行）
```
"less than" → "小于"
"more than" → "大于"
"at most" → "最多"
"at least" → "至少"
"multiples of" → "倍数"
"mm" → "毫米"
"x</color> damage to target" → "倍</color> 伤害对目标"
"x</color> damage to this" → "倍</color> 伤害对此"
"the above weapon does <color=#ffdf72>" → "上述武器造成 <color=#ffdf72>"
"the attacker does <color=#ffdf72>" → "攻击者造成 <color=#ffdf72>"
"x</color> damage to the target" → "倍</color> 伤害对目标"
"x</color> damage to this ship" → "倍</color> 伤害对此舰船"
"mm</color> of target armor" → "毫米</color> 目标护甲"
"mm</color> of armor this has" → "毫米</color> 此舰船护甲"
"x</color> extra damage to target" → "倍</color> 额外伤害对目标"
"x</color> damage to target" → "倍</color> 伤害对目标"
"x</color> extra damage to this ship" → "倍</color> 额外伤害对此舰船"
"x</color> damage to this ship" → "倍</color> 伤害对此舰船"
"mm</color> of armor the target has" → "毫米</color> 目标护甲"
"mm</color> of armor this ship has" → "毫米</color> 此舰船护甲"
```

#### damage abort codes（第 7690-7723 行，保持调试原文）
参考 Task 10 中的伤害中止代码列表，保持英文。

#### 重创状态（第 7737-7741 行）
与 Task 10 中对应字符串翻译一致。

#### 物质状态/生成组（第 7962-8494 行）
```
"Must be " → "必须为 "
" state of matter to function.  " → " 物质状态才能运作。"
"None" → "无"
"Primary: " → "主要："
"Secondary: " → "次要："
"Tertiary: " → "第三："
"Quaternary: " → "第四："
"Quinary: " → "第五："
"Senary: " → "第六："
"Septenary: " → "第七："
"Octonary: " → "第八："
"Nonary: " → "第九："
"Denary: " → "第十："
"Group " → "组 "
"Between " → "介于 "
" and " → " 和 "
```

#### 命令文本（第 9035-9115 行）
```
"Assist " → "协助 "
"Attack " → "攻击 "
"Load into " → "装载入 "
"Unload" → "卸载"
"Go Attack All" → "全部攻击"
"Go Attack Move" → "攻击移动"
"Stop Moving" → "停止移动"
"Move On" → "继续移动"
"Attack Targets" → "攻击目标"
"Go to " → "前往 "
"unknown planet" → "未知星球"
"Go via " → "经由 "
"Decollide" → "解除碰撞"
"Move to " → "移动到 "
"Unknown Target" → "未知目标"
```

#### 建造/修理吞吐量（第 9949-10042 行）
```
"Construct fleet units (Speed <color=#ffdf72>" → "建造舰队单位（速度 <color=#ffdf72>"
"Assist construction (Speed <color=#ffdf72>" → "协助建造（速度 <color=#ffdf72>"
"Boost factory (Speed <color=#ffdf72>" → "加速工厂（速度 <color=#ffdf72>"
"Claim neutral units (Speed <color=#ffdf72>" → "占领中立单位（速度 <color=#ffdf72>"
"Rebuild remains (Speed <color=#ffdf72>" → "重建残骸（速度 <color=#ffdf72>"
"Repairs allied " → "修理友方 "
"Hull / Shield / Engines (Speed <color=#ffdf72>" → "船体 / 护盾 / 引擎（速度 <color=#ffdf72>"
"Hull (Speed <color=#ffdf72>" → "船体（速度 <color=#ffdf72>"
"Shield (Speed <color=#ffdf72>" → "护盾（速度 <color=#ffdf72>"
"Engines (Speed <color=#ffdf72>" → "引擎（速度 <color=#ffdf72>"
"</color>).  " → "</color>）。"
"</color>)" → "</color>）"
```

- [ ] **Step 1: 翻译推拉效果和模块增强（第 6292-6660 行）**
- [ ] **Step 2: 翻译伤害修饰符标签和条件（第 6855-7242 行）**
- [ ] **Step 3: 翻译命令文本（第 9035-9115 行）**
- [ ] **Step 4: 翻译建造/修理吞吐量（第 9949-10042 行）**
- [ ] **Step 5: 编译验证**

---

### Task 13: 翻译 Window_InGameHoverEntityInfo.cs — 早期区域遗漏文本 (lines 1-4780)

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

**范围：** lines 1-4780 的遗漏字符串，以及 lines 5600-9200 的少量遗漏

此任务需逐行读文件找 `buffer.Add(` 和 `Buffer.Add(` 中的英文文本翻译。

**关键字符串对照表：**

#### 资料/金属标签 (lines ~1000-1200)
```
"Metal: " → "金属："
```

但注意：line 1118 的 `buffer.Add( "Metal: " );` 可能在已有翻译的区域里，需要先确认是否已翻译。

#### 武器标签 (lines ~2080-2090)
```
buffer.Add( "some weapons" ) → buffer.Add( "某些武器" )
```

#### 目标状态 (lines ~2470-2480)
```
buffer.Add("Target is " + team.Target.ToStringWithPlanetAndOwner() ).Add("\n")
→ buffer.Add("目标为 " + team.Target.ToStringWithPlanetAndOwner() ).Add("\n")
```

#### 行星停留时间 (lines ~3074-3077)
```
buffer.Add( "since it has been at this planet and non-crippled for more than " )
→ buffer.Add( "因已在此星球且未受损超过 " )

buffer.Add( "once it has been at that planet and non-crippled for at least " ).AddHoursAndMinutes( timeRemaining ).Add( " more.  " )
→ buffer.Add( "一旦在此星球且未受损至少 " ).AddHoursAndMinutes( timeRemaining ).Add( " 后。  " )
```

#### 牺牲修复 (line ~3275)
```
buffer.Add( "When allied units on this planet would die, this ship instead reduces its own health to regenerate them. The efficiency is one hull point from this unit per <color=#ffdf72>" )
→ buffer.Add( "当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>" )
```

#### 轨道描述 (lines ~3495-3511)
```
buffer.Add( "Orbits Gravity Well at " ) → buffer.Add( "绕重力井轨道 " )
buffer.Add( "Orbits ancestor unit at " ) → buffer.Add( "绕祖先单位轨道 " )
buffer.Add( "Orbits flagship at " ) → buffer.Add( "绕旗舰轨道 " )
```

#### 当前隐形点数 (line ~3741)
```
buffer.Add( "Current Cloaking Points: " ) → buffer.Add( "当前隐形点数：" )
```

#### 无敌需求 (line ~4008)
```
buffer.Add( "To have invulnerability, it requires at least " )
→ buffer.Add( "获得无敌需要至少 " )
```

#### AIP 文本 (lines ~4044-4105)
```
buffer.Add( "AI Progress (AIP) will <color=#ffdf72>rise by " )
→ buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " )

buffer.Add( "</color> if this dies.  " )
→ buffer.Add( "</color> 如果此单位死亡。  " )

buffer.Add( "Since you have not already paid the AI Progress (AIP) price for taking this planet, AIP will <color=#ffdf72>rise by " )
→ buffer.Add( "由于你尚未为此星球支付 AI 进程 (AIP) 代价，AIP 将<color=#ffdf72>上升 " )

buffer.Add( "AI Progress (AIP) will <color=#ffdf72>be reduced by " )
→ buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>减少 " )

buffer.Add( "If a")...Add("player kills this unit, they get <color=#ffdf72>" )...Add(" metal. </color>")
→ buffer.Add( "如果玩家")...保持原文 .Add("击杀此单位，获得 <color=#ffdf72>" )...Add(" 金属。</color>")

Same pattern for science and hacking - translate the wrapper text:
"player kills this unit, they get " → "击杀此单位，获得 "
" science. </color>" → " 科学。</color>"
" hacking points. </color>" → " 黑客点数。</color>"
" science </color> and " → " 科学</color> 和 "

buffer.Add( "AI Progress (AIP) will <color=#ffdf72>rise by " )...Add( "</color> if all remaining <color=#ffdf72>" )
→ buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " )...Add( "</color> 如果所有剩余的 <color=#ffdf72>" )

buffer.Add( "AI Progress (AIP) will <color=#ffdf72>be reduced by " )...Add( "</color> if all remaining <color=#ffdf72>" )
→ buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>减少 " )...Add( "</color> 如果所有剩余的 <color=#ffdf72>" )
```

#### 起始等级 (line ~4144)
```
buffer.Add( "Starts at " ) → buffer.Add( "起始等级 " )
```

#### 科技升级 (lines ~4174-4189)
```
buffer.Add( "Upgraded by Tech: " ) → buffer.Add( "科技升级：" )
buffer.Add( "Techs: " ) → buffer.Add( "科技：" )
```

#### 祖先/后代 (lines ~4287-4325)
```
buffer.Add( "Ancestor Unit: " ) → buffer.Add( "祖先单位：" )
buffer.Add( "Normally will have an ancestor unit, and dies if that ancestor dies.  " ) → buffer.Add( "通常有祖先单位，如果祖先死亡则此单位也会死亡。  " )
buffer.Add( "Builds up to " ) → buffer.Add( "最多建造 " )
buffer.Add( "Error!  No descendants available to build!" ) → buffer.Add( "错误！没有可建造的后代！" )
buffer.Add( "Descendants are: " ) → buffer.Add( "后代为：" )
buffer.Add( "Descendants are a mix of: " ) → buffer.Add( "后代混合了：" )
```

#### 特殊属性 (lines ~4376-4496)
```
buffer.Add( "Elite: Only one elite ship line can be added to any fleet.  " )
→ buffer.Add( "精英：每支舰队只能添加一条精英舰船线。  " )

buffer.Add( "Allows AI ships to warp in here.  " )
→ buffer.Add( "允许 AI 舰船跃迁至此。  " )

buffer.Add( "Cannot traverse wormholes.  " )
→ buffer.Add( "无法穿越虫洞。  " )

buffer.Add( "Self-destructs if command station is destroyed.  " )
→ buffer.Add( "如果指挥站被摧毁则自毁。  " )

buffer.Add( "Loses <color=#ffdf72>" )
→ buffer.Add( "每秒损失 <color=#ffdf72>" )

buffer.Add( "Immune to all damage.  " )
→ buffer.Add( "免疫所有伤害。  " )

buffer.Add( "Cannot be repaired -- whatever this thing is, we don't know how to fix it.  " )
→ buffer.Add( "无法修复 -- 我们不知道这东西怎么修。  " )

buffer.Add( "Cannot be repaired.  " )
→ buffer.Add( "无法修复。  " )

buffer.Add( "Scrapping this unit gives no metal.  " )
→ buffer.Add( "拆解此单位不获得金属。  " )

buffer.Add( "Scrapping this unit on a friendly planet refunds ")
→ buffer.Add( "在友方星球拆解此单位返还 " )

buffer.Add( "Cannot be protected by forcefields, due to its strange interaction with the fabric of reality.  " )
→ buffer.Add( "由于与现实结构产生奇怪交互，无法被力场保护。  " )

buffer.Add( "Cannot be protected by forcefields.  " )
→ buffer.Add( "无法被力场保护。  " )

buffer.Add( "Immune to enemy weapon system bonus damage.  " )
→ buffer.Add( "免疫敌方武器系统加成伤害。  " )

buffer.Add( "Strange interactions with the very fabric of spacetime cause this to exist in the normal plane of existence only part of the time" )
→ buffer.Add( "与时空结构的奇怪交互导致此单位仅部分时间存在于正常位面" )

buffer.Add( "Phases to " ) → buffer.Add( "相位切换至 " )
```

#### 无敌/构建点/武器点 (lines ~4534-4554)
```
buffer.Add( "Immune to all damage for " ) → buffer.Add( "免疫所有伤害，持续 " )
buffer.Add( "Currently has <color=#ffdf72>" ) → buffer.Add( "当前拥有 <color=#ffdf72>" )
```

#### 重创/弹射/死亡 (lines ~4629-4640)
```
buffer.Add( "Cannot die, but rather becomes crippled at 1 HP.  " ) → buffer.Add( "不会死亡，而是在 1 HP 时变为重创状态。  " )
buffer.Add( "When crippled, will use bail-out function to a friendly planet.  " ) → buffer.Add( "重创时将使用弹射功能前往友方星球。  " )
buffer.Add( "When crippled in deepstrike territory, will use bail-out function to a friendly planet.  " ) → buffer.Add( "在深袭区域重创时将使用弹射功能前往友方星球。  " )
buffer.Add( "When controlled by a human, dies to remains that can be rebuilt.  " ) → buffer.Add( "由人类控制时，死亡变为可重建的残骸。  " )
buffer.Add( "Reverts to neutral status on death, rather than truly dying.  " ) → buffer.Add( "死亡时恢复为中立状态，而非真正死亡。  " )
```

#### "none" / "null" (lines ~4663-4698)
Translate `"none"` → `"无"` (in display context for dropdown values)
Keep `"null"` → 保持（代码逻辑）

#### 行为/命令文本 (lines ~4725-4780)
```
buffer.Add("Behaviour: " + orders.Behavior + ". ")
→ buffer.Add("行为：" + orders.Behavior + "。")

buffer.Add("No queued orders\n") → buffer.Add("无排队命令\n")

buffer.Add("This unit has " + orders.GetQueuedOrderCount() + " queued orders, " + firstOrder.TypeData.Type + ". " + World_AIW2.Instance.GetPlanetByIndex( firstOrder.RelatedPlanetIndex).Name)
→ buffer.Add("此单位有 " + orders.GetQueuedOrderCount() + " 个排队命令，" + firstOrder.TypeData.Type + "。" + World_AIW2.Instance.GetPlanetByIndex( firstOrder.RelatedPlanetIndex).Name)

buffer.Add("This unit has " + orders.GetQueuedOrderCount() + " queued orders, the first of which is " + firstOrder.TypeData.Type + ". ")
→ buffer.Add("此单位有 " + orders.GetQueuedOrderCount() + " 个排队命令，第一个为 " + firstOrder.TypeData.Type + "。")

buffer.Add("Threat " ) → buffer.Add("威胁 " )

buffer.Add("Waiting against " + World_AIW2.Instance.GetPlanetByIndex(relatedSquadOrNull.WaitingAgainstPlanetIndex).Name + 
→ buffer.Add("正在等待对抗 " + World_AIW2.Instance.GetPlanetByIndex(relatedSquadOrNull.WaitingAgainstPlanetIndex).Name + 
```

#### 剩余遗漏 (lines ~5600-9200)
Line 6691:
```
buffer.Add( "damage" ) → buffer.Add( "伤害" )
```

- [ ] **Step 1: 翻译资源/武器/目标文本 (lines 1000-2500)**
- [ ] **Step 2: 翻译停留时间/牺牲/轨道/隐形 (lines 3074-4008)**
- [ ] **Step 3: 翻译 AIP/科技/祖先/后代 (lines 4044-4325)**
- [ ] **Step 4: 翻译特殊属性/重创/命令 (lines 4376-4780)**
- [ ] **Step 5: 翻译遗漏的 "damage" (lines 5600-9200)**
- [ ] **Step 6: 编译验证**

---

### Task 14: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — 早期区域遗漏文本 (lines 1-4900)

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

**范围：** lines 1-4900 的遗漏字符串，以及 lines 5600-9100 的少量遗漏

#### 黑客 (line ~1356)
```
buffer.Add( "Hacking: " ) → buffer.Add( "黑客入侵：" )
```

#### 武器标签 (line ~1573)
```
buffer.Add( "some weapons" ) → buffer.Add( "某些武器" )
```

#### 目标状态 (line ~1941)
```
buffer.Add( "Target is " ) → buffer.Add( "目标为 " )
```

#### 资源生产 (lines ~2181-2322)
```
buffer.Add( "Produces " ) → buffer.Add( "生产 " )
buffer.Add( "Generates " ) → buffer.Add( "生成 " )
buffer.Add( "Gathers " ) → buffer.Add( "采集 " )
buffer.Add( "Stores " ) → buffer.Add( "存储 " )
buffer.Add( "Increases asteroid powerplant production: " ) → buffer.Add( "增加小行星发电站产量：" )
buffer.Add( "Provides a <color=#ffdf72>" )...Add( "x</color> boost to all " )... → buffer.Add( "为所有 " )...Add( " 提供 <color=#ffdf72>" )...Add( "倍</color> 加成 " )
buffer.Add( "Metal/s produced" ) → buffer.Add( "金属/秒" )
buffer.Add( "Energy generated" ) → buffer.Add( "能量生成" )
buffer.Add( "Hacking gathered" ) → buffer.Add( "黑客采集" )
buffer.Add( "Science gathered" ) → buffer.Add( "科学采集" )
buffer.Add( "Argon produced" ) → buffer.Add( "氩气生产" )
buffer.Add( "Radon produced" ) → buffer.Add( "氡气生产" )
buffer.Add( "Xenon produced" ) → buffer.Add( "氙气生产" )
```

#### 资源倍率 (lines ~2361-2391)
```
buffer.Add( "Could provide a <color=#ffdf72>" ) → buffer.Add( "可能提供 <color=#ffdf72>" )
buffer.Add( "Provides a <color=#ffdf72>" ) → buffer.Add( "提供 <color=#ffdf72>" )
buffer.Add( "Could provide a " ) → buffer.Add( "可能提供 " )
buffer.Add( "Provides a" ) → buffer.Add( "提供" )
buffer.Add( "could provide a " ) → buffer.Add( "可能提供 " )
buffer.Add( "provides a" ) → buffer.Add( "提供" )
buffer.Add( "Could provide a " ) → buffer.Add( "可能提供 " )
buffer.Add( "Provides a" ) → buffer.Add( "提供" )
```

#### 行星停留 (lines ~2407-2414)
```
buffer.Add( "since it has been at this planet and non-crippled for more than " )
→ buffer.Add( "因已在此星球且未受损超过 " )

buffer.Add( "once it has been at this planet and non-crippled for at least " )
→ buffer.Add( "一旦在此星球且未受损至少 " )

buffer.Add( "when it has been at a planet and non-crippled for more than " )
→ buffer.Add( "当已在某星球且未受损超过 " )
```

#### 错误提示 (lines ~2452-2526)
```
buffer.Add( "No ships are granted from this one for some reason!  (This is a bug, please report it with a savegame.)  " )
→ buffer.Add( "由于某种原因此单位未提供任何舰船！（这是一个 BUG，请附上存档报告。）  " )
```

#### 牺牲修复 (line ~2598)
```
buffer.Add( "When allied units on this planet would die, this ship instead reduces its own health to regenerate them. The efficiency is one hull point from this unit per <color=#ffdf72>" )
→ buffer.Add( "当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>" )
```

#### 特殊属性 (lines ~2615-2694)
```
buffer.Add( "Cannot be captured by other factions. ", "999999" ) → buffer.Add( "不能被其他阵营捕获。", "999999" )
buffer.Add( "Cannot be supercharged. ", "999999" ) → buffer.Add( "不能超载充能。", "999999" )
buffer.Add( "copies of itself" ) → buffer.Add( "自身的复制体" )
buffer.Add( "Once those targets have been alerted and have released their guards to fight, those targets can then be attacked by this ship.  " )
→ buffer.Add( "一旦这些目标被警示并解除护卫状态，此舰船即可攻击它们。  " )
buffer.Add( "LAIR:</color> " ) → buffer.Add( "巢穴：</color> " )
buffer.Add( "EXO / RAID ENGINE:</color> Spawns waves and exo strikes" ) → buffer.Add( "EXO / 突袭引擎：</color> 生成波次和外银河打击" )
buffer.Add( "EXO ENGINE:</color> Spawns exo strikes" ) → buffer.Add( "EXO 引擎：</color> 生成外银河打击" )
buffer.Add( "RAID ENGINE:</color> Spawns waves" ) → buffer.Add( "突袭引擎：</color> 生成波次" )
```

#### 风筝/轨道 (lines ~2765-2843)
```
buffer.Add( "Never allowed to kite.  " ) → buffer.Add( "不允许风筝。  " )
buffer.Add( "Orbits " ) → buffer.Add( "绕轨道运行 " )
buffer.Add( "the gravity well" ) → buffer.Add( "重力井" )
buffer.Add( "its ancestor" ) → buffer.Add( "其祖先" )
buffer.Add( "the flagship" ) → buffer.Add( "旗舰" )
buffer.Add( "deg/s" ) → buffer.Add( "度/秒" )
```

#### 力场类型 (lines ~2875-2922)
```
buffer.Add( "HARDENED " ) → buffer.Add( "强化 " )
buffer.Add( "GREAT-FORCEFIELD" ) → buffer.Add( "巨力场" )
buffer.Add( "Hardened forcefields do not shrink as their shield health goes down.  " ) → buffer.Add( "强化力场不会随着护盾生命值下降而缩小。  " )
buffer.Add( "Great-forcefields do not cause allies firing out from under the shield to have any damage penalty.  " ) → buffer.Add( "巨力场不会使护盾下的友方射击受到伤害惩罚。  " )
buffer.Add( "Any allies firing out from under the shield only do half damage.  " ) → buffer.Add( "任何从护盾下向外射击的友方单位只造成一半伤害。  " )
buffer.Add( "This electrotoxic forcefield deals " ) → buffer.Add( "此电毒力场造成 " )
buffer.Add( "The electrotoxic hull on this unit deals " ) → buffer.Add( "此单位的电毒船体造成 " )
buffer.Add( "Returns damage dealt to it.  " ) → buffer.Add( "返还所受伤害。  " )
```

#### 当前隐形点数 (line ~2969)
```
buffer.Add( "Current Cloaking Points: " ) → buffer.Add( "当前隐形点数：" )
```

#### 所需/仅 (lines ~3210-3215)
```
buffer.Add( "The required " ) → buffer.Add( "所需 " )
buffer.Add( "Only " ) → buffer.Add( "仅 " )
```

#### AIP 文本 (lines ~3266-3322) — 与 Task 13 中对应字符串翻译一致
参照 Task 13 的 AIP 翻译表。

#### 起始等级/科技/祖先/后代 (lines ~3354-3537) — 与 Task 13 一致
参照 Task 13 的对应翻译。

#### 特殊属性 (lines ~3587-3667) — 与 Task 13 一致
参照 Task 13 的对应翻译。额外：
```
buffer.Add("Expires after ").AddMinutesAndSeconds(...).Add(".") → buffer.Add("在 ").AddMinutesAndSeconds(...).Add(" 后过期。")
```

#### 相位/武器点 (lines ~3707-3759)
参照 Task 13 的对应翻译。额外：
```
buffer.Add("Produces ") → buffer.Add("生产 ")
```

#### 重创/弹射/命令 (lines ~3852-3913) — 与 Task 13 一致

#### 威胁/外银河打击/守卫 (lines ~3926-4125) — 与 Task 8 一致
参照 Task 8 的对应翻译。

#### 黑客/隐形/外银河 (lines ~4153-4272) — 与 Task 8 一致

#### 增援/工厂/建筑 (lines ~4296-4567) — 与 Task 8 一致

#### 堆叠/旗舰 (lines ~4607-4631) — 与 Task 8 一致

#### 未探索空间 (line ~4893)
```
buffer.Add( "In Unexplored Space - unable to function without scouts having ever been sent here!" )
→ buffer.Add( "在未探索空间 - 没有侦察兵到过此地则无法运作！" )
```

#### 遗落字符串 (lines ~5600-9100)
```
buffer.Add( "damage" ) → buffer.Add( "伤害" )

buffer.Add( "None" ) → buffer.Add( "无" )
(lines 6545, 6569, 8116, 8188, 8291 — multiple "None" in display context)

buffer.Add( "unknown planet" ) → buffer.Add( "未知星球" )
```

#### 保持英文的调试字符串
跳过以下（保持原文）：
- `"Unknown DamageModifierBasedOn."` / `"Unknown DamageModifierAppliesTo"` / `"Unknown DamageModifierAppliesTo."`
- `"multiples of"` (这是内部标识符)
- `"mm"` (在 DamageModifier 上下文中是单位标识符)
- Damage abort codes (lines 7690-7723)
- `"ComputeDisabledReason: "` (line 1978)
- `"fireteamId "` (line 1912)
- `"ERROR_WRITE_NICE_TEXT: "` (lines 4943, 4988)

- [ ] **Step 1: 翻译黑客/武器/目标/资源文本 (lines 1300-2400)**
- [ ] **Step 2: 翻译停留/提示/牺牲/特殊属性/力场 (lines 2407-2969)**
- [ ] **Step 3: 翻译 AIP/科技/祖先/重创/命令 (lines 3210-3913)**
- [ ] **Step 4: 翻译威胁/守卫/增援/工厂 (lines 3926-4900)**
- [ ] **Step 5: 翻译遗漏 None/damage/unknown planet (lines 5000-9100)**
- [ ] **Step 6: 编译验证**

---

### Task 15: 最终验证

- [ ] **Step 1: 完整编译**

Run: `.\build.ps1`
Expected: 所有 4 个项目（AIWarExternalCode、AIWarExternalDeepProcessingCode、AIWarExternalVisualizationCode、ArcenUIAssetRedirect）编译 0 错误

- [ ] **Step 2: 确认无遗漏英文**

```bash
Select-String -Pattern 'Buffer\.Add\(\s*"[A-Za-z]' -Path "DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs"
```
Expected: 仅剩的匹配应为代码逻辑中的 HTML/Unity 标签字符串（如 `"| "`, `"- "`, `"  "`, `"\n"` 等非用户可见文本）
