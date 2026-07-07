# Task 9: 翻译 Window_InGameHoverEntityInfo.cs 武器/战斗系统 (lines 5604-7421)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

## 翻译原则
- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>` 、 `</color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- Debug 日志、内部标识符、错误码不翻译
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件

## 翻译对照表

### 目标优先级/错误调试 (lines ~5604-5661) — 保持英文
Do NOT translate:
- `"targetPriorityList is empty!"`
- `"targetPriorityList.Count: "`
- `"Squad Faction ReasonCode: "`
- `"BUG!  OutOfRange!"`
- Any `"CODE "` + variable debug strings

### 状态原因文本 (lines ~5910-6064)
```
buffer.Add("Non-functional - its owning faction must control this planet!")
→ buffer.Add("失效 - 所属阵营必须控制此星球！")

buffer.Add("In Unexplored Space - unable to function without scouts having ever been sent here!")
→ buffer.Add("在未探索空间 - 没有侦察兵到过此地则无法运作！")

buffer.Add("Not yet fully claimed, and also paused so that it will NOT be claimed.")
→ buffer.Add("尚未完全占领，且已暂停，因此不会被占领。")

buffer.Add("Not yet fully claimed.")
→ buffer.Add("尚未完全占领。")

buffer.Add("In 'hold fire' mode.")
→ buffer.Add("停火模式。")

buffer.Add("In 'pause function' mode.")
→ buffer.Add("功能暂停模式。")

buffer.Add("Still under construction (")
→ buffer.Add("仍在建造中（")

buffer.Add("Faction does not control this planet.")
→ buffer.Add("阵营未控制此星球。")

buffer.Add("Faction does not have enough energy.")
→ buffer.Add("阵营能量不足。")

buffer.Add("Not enough ")
→ buffer.Add("不足")

buffer.Add(" at this planet.")
→ buffer.Add("，在此星球。")

buffer.Add("Stored metal is zero.")
→ buffer.Add("存储金属为零。")

buffer.Add("Is not remains!")
→ buffer.Add("不是残骸！")

buffer.Add("Must wait another ")
→ buffer.Add("必须等待 ")

buffer.Add("Must only wait another ")
→ buffer.Add("只需再等待 ")

buffer.Add("Command Stations cannot be rebuilt on enemy planets.")
→ buffer.Add("指挥站不能在敌方星球重建。")

buffer.Add("Rebuilding would put you into negative energy.")
→ buffer.Add("重建会导致能量为负。")
```

### 棕色断电/力场 (lines ~6081-6084)
```
buffer.Add("Brownout: Bubble forcefields down for another ")
→ buffer.Add("电力不足：气泡力场将在 ")

buffer.Add("Brownout: Had negative energy balance!  Your bubble forcefields won't be able to project their protective field for another ")
→ buffer.Add("电力不足：能量平衡为负！你的气泡力场将在 ")
```

### 异常 (line ~6227)
Keep "Exception during generation of tooltip!" in English (debug)

### 武器统计描述 (lines ~6298-7000)
```
buffer.Add("Launches ") → buffer.Add("发射 ")

buffer.Add("Reload Speed of ") → buffer.Add("装弹速度 ")

buffer.Add("Reload ") → buffer.Add("装弹 ")

buffer.Add("BURST FIRE") → buffer.Add("爆发射击")

buffer.Add("Will deal an additional ") → buffer.Add("将额外造成 ")

buffer.Add(" corrosive damage. ") → buffer.Add(" 腐蚀伤害。")

buffer.Add("All damage applied is corrosive damage. ") → buffer.Add("所有伤害均为腐蚀伤害。")

buffer.Add("Range of ") → buffer.Add("范围 ")

buffer.Add("When striking the hull of a targets with no personal shields left that also use less than ")
→ buffer.Add("当击中无个人护盾且使用少于 ")

buffer.Add("When striking the hull of a target with no personal shields left, causes them to change to ")
→ buffer.Add("当击中无个人护盾的目标船体时，使其改变为 ")

buffer.Add("When striking any target that uses less than ")
→ buffer.Add("当击中任何使用少于 ")

buffer.Add("When striking the any target, causes it to change to ")
→ buffer.Add("当击中任何目标时，使其改变为 ")
```

#### AOE 相关
```
buffer.Add("DMG split between targets, ") → buffer.Add("伤害在目标间分摊，")

buffer.Add("all targets in range") → buffer.Add("范围内所有目标")

buffer.Add("spreading its damage among ") → buffer.Add("在其间分摊伤害 ")

buffer.Add("at most <color=#ffdf72>") → buffer.Add("最多 <color=#ffdf72>")

buffer.Add("</color> targets") → buffer.Add("</color> 个目标")

buffer.Add("doing its full damage to the primary target, and ")
→ buffer.Add("对主要目标造成全额伤害，并")

buffer.Add("doing their full damage to ") → buffer.Add("对其造成全额伤害")

buffer.Add("spreading their damage among ") → buffer.Add("在其间分摊伤害")
```

#### 光束/链式闪电
Read exact lines to match the precise strings. Key translations:
```
"hitting the main intended target for full damage, then hits <color=#ffdf72>"
→ "击中主要目标造成全额伤害，然后击中 <color=#ffdf72>"

" targets with a second copy of damage, divided evenly among all those hit, max damage per beam <color=#ffdf72>"
→ " 个目标，造成第二份伤害并在所有目标间平均分摊，每束最大伤害 <color=#ffdf72>"

"hitting the main intended target for full damage, then hits everything else with a second copy of damage, divided evenly among all those hit max damage per beam <color=#ffdf72>"
→ "击中主要目标造成全额伤害，然后对所有其他目标造成第二份伤害，平均分摊，每束最大伤害 <color=#ffdf72>"

"Note that because of multiple beams potentially hitting a single target, closer or larger targets tend to take more damage.  "
→ "注意：由于多束光束可能击中同一目标，更近或更大的目标会承受更多伤害。"
```

#### Chain lightning
```
"up to <color=#ffdf72>" → "最多 <color=#ffdf72>"
"</color> overall targets " → "</color> 个总目标 "
"any number of overall targets " → "任意数量的总目标 "
"with a chain lightning attack that jumps <color=#ffdf72>" → "链式闪电攻击，跳跃 <color=#ffdf72>"
"</color> times " → "</color> 次 "
"with chain range <color=#ffdf72>" → "链式范围 <color=#ffdf72>"
"Each time the lightning jumps, it can strike " → "每次闪电跳跃可击中 "
"any number of targets in the next cycle.  " → "任意数量目标在下一周期。"
"Each target after the primary takes " → "主要目标后的每个目标承受 "
"Each target after the primary takes full damage.  " → "主要目标后的每个目标承受全额伤害。"
```

#### Beam penetration
```
"hitting <color=#ffdf72>" → "击中 <color=#ffdf72>"
"</color> targets" → "</color> 个目标"
"hitting all targets intersected by the beam" → "击中光束路径上的所有目标"
```

#### Shot limits
```
"Only " → "仅 "
" can be fired per target and stack. " → " 可对每个目标和堆叠发射。"
"All can be aimed at the same target. " → "均可瞄准同一目标。"
"A single target may be damaged by multiple intersecting beams.  Each beam does the full damage listed above.  " → "单个目标可能被多束相交光束击中。每束光束造成全额伤害。"
"Each beam does the full damage listed above.  " → "每束光束造成全额伤害。"
```

#### Mirror weapons
```
"This mirror weapon fires a projectile back with " → "此镜面武器反射一枚射弹，"

"Fires a projectile back with power based on shot impact to parent unit.  "
→ "基于击中母舰的冲击力反射一枚射弹。"
```

#### Engine stun
```
"s</color> if target engine < <color=#ffdf72>" → "秒</color> 如果目标引擎 < <color=#ffdf72>"
"s</color> if the target has an engine power less than <color=#ffdf72>" → "秒</color> 如果目标引擎动力低于 <color=#ffdf72>"
"The target can be slowed up to a full " → "目标可被减速最多 "
"The more stun-seconds accumlated on a target, the slower it goes. 4s = 50% move speed, 7s+ = immobilized.  " → "目标累积的眩晕秒数越多，速度越慢。4秒 = 50%移动速度，7秒以上 = 无法移动。"
"s</color> if target mass < <color=#ffdf72>" → "秒</color> 如果目标质量 < <color=#ffdf72>"
"s</color> if the target has a mass less than <color=#ffdf72>" → "秒</color> 如果目标质量低于 <color=#ffdf72>"
```

#### Armor pierce
```
"s</color> if armor < <color=#ffdf72>" → "秒</color> 如果护甲 < <color=#ffdf72>"
"mm</color>, max " → "毫米</color>，最大 "
"s.  " → "秒。"
"s</color> if the target has an armor thickness of less than <color=#ffdf72>" → "秒</color> 如果目标护甲厚度低于 <color=#ffdf72>"
"mm</color>.  The total amount of extra reload time per target that can be applied is " → "毫米</color>。每个目标可施加的额外装弹时间总量为 "
```

#### Push/Pull
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

## 执行步骤
1. Read the file around lines 5604-7421 to find exact string patterns
2. For each English string in the translation table, use Edit tool to replace it
3. Run build.ps1 to verify
4. Report back
