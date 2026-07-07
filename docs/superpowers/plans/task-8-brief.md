# Task 8: 翻译 Window_InGameHoverEntityInfo.cs 游戏机制/状态描述 (lines 4784-5604)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

## 翻译原则
- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>` 、 `</color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- 处理 `buffer.Add(`（小写 b）和 `Buffer.Add(`（大写 B）两种形式
- Debug 日志、内部标识符、错误码不翻译
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件

## 翻译对照表

### 舰队状态描述 (lines ~4784-4830)
```
Buffer.Add( "This type of ship will stay threatfleet rather than joining the Hunter. " )
→ Buffer.Add( "此类舰船将留在威胁舰队中，而不会加入猎手。" )

buffer.Add( "This ship will stay threatfleet, since it is not targeting humans. " )
→ buffer.Add( "此舰船将留在威胁舰队中，因为它不针对人类。" )

buffer.Add( "This ship will stay threatfleet, since it s not linked to the Sentinels hivemind for some reason. " )
→ buffer.Add( "此舰船将留在威胁舰队中，因其未链接到哨兵蜂巢思维。" )

buffer.Add( "This ship will leave the threatfleet and join the Hunters after " )
→ buffer.Add( "此舰船将在 " )

buffer.Add( "s of waiting around to attack, or after " )
→ buffer.Add( " 秒的待机攻击时间后离开威胁舰队加入猎手，或在经过 " )

buffer.Add( "s more seconds of just existing. " )
→ buffer.Add( " 秒的存在时间后离开。" )

buffer.Add("Part of an Exostrike heading for " + target.TypeData.GetDisplayName() + " on " + target.GetPlanetName_Safe() + ". ")
→ buffer.Add("属于外银河打击部队，目标为 " + target.TypeData.GetDisplayName() + " 位于 " + target.GetPlanetName_Safe() + "。")

buffer.Add("Part of and Exostrike heading for " + World_AIW2.Instance.GetPlanetByIndex(...).Name + " but without a target. ")
→ buffer.Add("属于外银河打击部队，目标为 " + World_AIW2.Instance.GetPlanetByIndex(...).Name + " 但无明确目标。")

buffer.Add("Guarding " + guarded.TypeData.InternalName)
→ buffer.Add("守卫 " + guarded.TypeData.InternalName)

buffer.Add("This entity will despawn in " + relatedSquadOrNull.DespawnsInXSeconds + " seconds. ", "7486d1")
→ buffer.Add("此实体将在 " + relatedSquadOrNull.DespawnsInXSeconds + " 秒后消失。", "7486d1")
```

### 舰队旗舰描述 (lines ~4880-4990)
Note: Read exact context around "Is " / "centerpiece" / "I am the " / " of fleet " to determine the precise strings. These are likely within the Fleet Centerpiece Things region.

```
"Is " → "是 "

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

### 舰船上限/数据 (lines ~4980-5080)
```
"     Galaxy-Wide Cap: " → "     全银河上限："

" per " → " 每 "

" player-owned planets. " → " 个玩家拥有的星球。"

" Player planets Owned)" → " 玩家星球)"
```
Note: The "Player planets Owned)" line is within a concatenation - check exact context.

### 黑客/隐形/特殊状态 (lines ~5086-5230)
Read exact lines to find the exact strings. Key translations:
```
"This unit is currently hacking; it is diverting engine power to the Hacking Matrix, so it is slowed and cannot leave the planet. "
→ "此单位正在黑客入侵；它将引擎能量转移到黑客矩阵，因此速度降低且无法离开星球。"

"Hacking disables a ships cloaking system. "
→ "黑客入侵会禁用舰船的隐形系统。"

"The AI will generate Exostrikes (Exogalactic Strikeforces) against you if you capture this structure. "
→ "如果你占领此建筑，AI 将派出外银河打击部队对付你。"

"This unit only stays cloaked if its faction owns its planet. "
→ "此单位仅在其阵营控制该星球时保持隐形。"
```

### 工厂/增援/建造描述 (lines ~5230-5512)
```
"This AI Reinforcement Point contains " → "此 AI 增援点包含 "

"Contains " → "包含 "

"Reinforcement Debug Reason Codes: " → "增援调试原因代码："

"Crippled factories are unable to spend metal until they are repaired. "
→ "受损工厂在修复前无法消耗金属。"

"Non-functional factories are unable to spend metal until their functionality is restored. "
→ "失效工厂在功能恢复前无法消耗金属。"

"Disabled factories are unable to spend metal until they are re-enabled. "
→ "已禁用的工厂在重新启用前无法消耗金属。"

"One or more fleets of relevance are in range of this factory, but they already have full ship caps and so there's nothing to do.  "
→ "一个或多个相关舰队在此工厂范围内，但已达舰船上限，无需行动。"

"You must unpause the game briefly before you can see what supporting factories are.  "
→ "你必须短暂取消暂停才能查看支持工厂的信息。"

"No (non-crippled) " → "没有（未受损的）"

" fleets of my faction are on this or adjacent planets, so I can't build anything for anyone.  "
→ " 类型的我方舰队在此星球或邻近星球上，因此无法为任何人建造。"

"No (non-crippled) mobile fleets of my faction are on this or adjacent planets, so I can't build anything for anyone.  "
→ "没有（未受损的）我方机动舰队在此或邻近星球上，因此无法为任何人建造。"

"Debug Construction Blocked: " → "调试：建造被阻止："

"I am the centerpiece of what would become a fleet with the following items:  "
→ "我是将成为以下内容舰队的旗舰："
```

### 额外插槽/标记等级 (lines ~5497-5512)
```
"Grants " → "提供 "

" additional build slots if you own this." → " 个额外建造插槽（如果你拥有此建筑）。"

"Must be at mark level " → "必须达到标记等级 "

" to build this structure. " → " 才能建造此建筑。"
```

### 堆叠/旗舰模式 (lines ~5557-5604)
```
"This stack takes damage like normal, but shoots " → "此堆叠正常承受伤害，但发射 "

"x</color> the normal amount of shots.  " → "倍</color> 的正常射击量。"

"Hold " → "按住 "

"You can find out more about this, and change its mode, on the Fleets sidebar tab.  Find this flagship's fleet and click it.  "
→ "你可以在舰队侧边栏页签中了解更多信息并更改其模式。找到此旗舰的舰队并点击它。"
```

## 调试字符串不翻译
Lines containing:
- "BUG!  OutOfRange!" → 保持原文
- "targetPriorityList is empty!" → 保持原文
- "CODE " → 保持原文
- Any `debugStage` related strings → 保持原文

## 编译验证
完成后运行：`.\build.ps1`
Expected: 0 errors across all 4 projects

## 执行步骤
1. Read the file around lines 4784-5604 to find exact string patterns
2. For each English string string in the translation table, use Edit tool to replace it
3. Process all strings in order from top to bottom
4. Run build.ps1 to verify
5. Report back with status and any issues
