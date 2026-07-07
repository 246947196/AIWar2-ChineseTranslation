# Task 14: 翻译 Window_PrototypeInGameHoverEntityInfo.cs — 早期区域遗漏 (lines 1-4900)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

## 规则
- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>`、`</color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- Debug 日志、内部标识符、错误码不翻译
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件

## 翻译对照表

### 黑客/武器/目标 (lines ~1356-1978)
```
"Hacking: " → "黑客入侵："
"some weapons" → "某些武器"
"Target is " → "目标为 "
```
Note: "fireteamId " (line 1912), "ComputeDisabledReason: " (line 1978), and "null relatedEntityData"/"relatedEntityData not a ship" (lines 47-52) keep English (debug). Commented-out lines 177-182 skip.

### 资源生产 (lines ~2181-2322)
```
"Produces " → "生产 "
"Generates " → "生成 "
"Gathers " → "采集 "
"Stores " → "存储 "
"Increases asteroid powerplant production: " → "增加小行星发电站产量："
"Provides a <color=#ffdf72>" → "为所有 "
"x</color> boost to all " → " 提供 <color=#ffdf72>倍</color> 加成 "
"Metal/s produced" → "金属/秒"
"Energy generated" → "能量生成"
"Hacking gathered" → "黑客采集"
"Science gathered" → "科学采集"
"Argon produced" → "氩气生产"
"Radon produced" → "氡气生产"
"Xenon produced" → "氙气生产"
```

### 资源倍率 (lines ~2361-2391)
```
"Could provide a <color=#ffdf72>" → "可能提供 <color=#ffdf72>"
"Provides a <color=#ffdf72>" → "提供 <color=#ffdf72>"
"Could provide a " → "可能提供 "
"Provides a" → "提供"
"could provide a " → "可能提供 "
"provides a" → "提供"
```

### 行星停留 (lines ~2407-2414)
```
"since it has been at this planet and non-crippled for more than " → "因已在此星球且未受损超过 "
"once it has been at this planet and non-crippled for at least " → "一旦在此星球且未受损至少 "
"when it has been at a planet and non-crippled for more than " → "当已在某星球且未受损超过 "
```

### Bug 提示/牺牲/捕获/充能 (lines ~2452-2669)
```
"No ships are granted from this one for some reason!  (This is a bug, please report it with a savegame.)  "
→ "由于某种原因此单位未提供任何舰船！（这是一个 BUG，请附上存档报告。）  "

"When allied units on this planet would die, this ship instead reduces its own health to regenerate them. The efficiency is one hull point from this unit per <color=#ffdf72>"
→ "当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>"

"Cannot be captured by other factions. " → "不能被其他阵营捕获。"
"Cannot be supercharged. " → "不能超载充能。"
"copies of itself" → "自身的复制体"

"Once those targets have been alerted and have released their guards to fight, those targets can then be attacked by this ship.  "
→ "一旦这些目标被警示并解除护卫状态，此舰船即可攻击它们。  "
```

### 巢穴/引擎/突袭 (lines ~2669-2694)
```
"LAIR:</color> " → "巢穴：</color> "
"EXO / RAID ENGINE:</color> Spawns waves and exo strikes" → "EXO / 突袭引擎：</color> 生成波次和外银河打击"
"EXO ENGINE:</color> Spawns exo strikes" → "EXO 引擎：</color> 生成外银河打击"
"RAID ENGINE:</color> Spawns waves" → "突袭引擎：</color> 生成波次"
```

### 风筝/轨道 (lines ~2765-2843)
```
"Never allowed to kite.  " → "不允许风筝。  "
"Orbits " → "绕轨道运行 "
"the gravity well" → "重力井"
"its ancestor" → "其祖先"
"the flagship" → "旗舰"
"deg/s" → "度/秒"
```

### 力场 (lines ~2875-2922)
```
"HARDENED " → "强化 "
"GREAT-FORCEFIELD" → "巨力场"
"Hardened forcefields do not shrink as their shield health goes down.  " → "强化力场不会随着护盾生命值下降而缩小。  "
"Great-forcefields do not cause allies firing out from under the shield to have any damage penalty.  " → "巨力场不会使护盾下的友方射击受到伤害惩罚。  "
"Any allies firing out from under the shield only do half damage.  " → "任何从护盾下向外射击的友方单位只造成一半伤害。  "
"This electrotoxic forcefield deals " → "此电毒力场造成 "
"The electrotoxic hull on this unit deals " → "此单位的电毒船体造成 "
"Returns damage dealt to it.  " → "返还所受伤害。  "
```

### 隐形点数 (line ~2969)
```
"Current Cloaking Points: " → "当前隐形点数："
```

### 所需/仅 (lines ~3210-3215)
```
"The required " → "所需 "
"Only " → "仅 "
```

### AIP 文本 (lines ~3266-3322)
Same as those in the other file:
```
"AI Progress (AIP) will <color=#ffdf72>rise by " → "AI 进程 (AIP) 将<color=#ffdf72>上升 "
"</color> if this dies.  " → "</color> 如果此单位死亡。  "
"Since you have not already paid the AI Progress (AIP) price for taking this planet, AIP will <color=#ffdf72>rise by " → "由于你尚未为此星球支付 AI 进程 (AIP) 代价，AIP 将<color=#ffdf72>上升 "
"AI Progress (AIP) will <color=#ffdf72>be reduced by " → "AI 进程 (AIP) 将<color=#ffdf72>减少 "

"player kills this unit, they get " → "击杀此单位，获得 "
" metal. </color>" → " 金属。</color>"
" science. </color>" → " 科学。</color>"
" hacking points. </color>" → " 黑客点数。</color>"
" science </color> and " → " 科学</color> 和 "

"AI Progress (AIP) will <color=#ffdf72>rise by " → "AI 进程 (AIP) 将<color=#ffdf72>上升 "
"AI Progress (AIP) will <color=#ffdf72>be reduced by " → "AI 进程 (AIP) 将<color=#ffdf72>减少 "
"</color> if all remaining <color=#ffdf72>" → "</color> 如果所有剩余的 <color=#ffdf72>"
```

### 科技/祖先/后代 (lines ~3354-3537) — 同另一文件
```
"Starts at " → "起始等级 "
"Upgraded by Tech: " → "科技升级："
"Techs: " → "科技："
"Ancestor Unit: " → "祖先单位："
"Normally will have an ancestor unit, and dies if that ancestor dies.  " → "通常有祖先单位，如果祖先死亡则此单位也会死亡。  "
"Builds up to " → "最多建造 "
"Error!  No descendants available to build!" → "错误！没有可建造的后代！"
"Descendants are: " → "后代为："
"Descendants are a mix of: " → "后代混合了："
```

### 特殊属性 (lines ~3587-3714) — 同另一文件
```
"Elite: Only one elite ship line can be added to any fleet.  " → "精英：每支舰队只能添加一条精英舰船线。  "
"Allows AI ships to warp in here.  " → "允许 AI 舰船跃迁至此。  "
"Cannot traverse wormholes.  " → "无法穿越虫洞。  "
"Self-destructs if command station is destroyed.  " → "如果指挥站被摧毁则自毁。  "
"Expires after " → "在 "
"Loses <color=#ffdf72>" → "每秒损失 <color=#ffdf72>"
"Immune to all damage.  " → "免疫所有伤害。  "
"Cannot be repaired -- whatever this thing is, we don't know how to fix it.  " → "无法修复 -- 我们不知道这东西怎么修。  "
"Cannot be repaired.  " → "无法修复。  "
"Scrapping this unit gives no metal.  " → "拆解此单位不获得金属。  "
"Scrapping this unit on a friendly planet refunds " → "在友方星球拆解此单位返还 "
"Cannot be protected by forcefields, due to its strange interaction with the fabric of reality.  " → "由于与现实结构产生奇怪交互，无法被力场保护。  "
"Cannot be protected by forcefields.  " → "无法被力场保护。  "
"Immune to enemy weapon system bonus damage.  " → "免疫敌方武器系统加成伤害。  "
"Strange interactions with the very fabric of spacetime cause this to exist in the normal plane of existence only part of the time" → "与时空结构的奇怪交互导致此单位仅部分时间存在于正常位面"
"Phases to " → "相位切换至 "
" every " → " 每 "
```

### 无敌/武器点 (lines ~3751-3778)
```
"Immune to all damage for " → "免疫所有伤害，持续 "
"Currently has <color=#ffdf72>" → "当前拥有 <color=#ffdf72>"
"Produces " → "生产 "
```

### 重创/弹射/死亡 (lines ~3852-3862) — 同另一文件
```
"Cannot die, but rather becomes crippled at 1 HP.  " → "不会死亡，而是在 1 HP 时变为重创状态。  "
"When crippled, will use bail-out function to a friendly planet.  " → "重创时将使用弹射功能前往友方星球。  "
"When crippled in deepstrike territory, will use bail-out function to a friendly planet.  " → "在深袭区域重创时将使用弹射功能前往友方星球。  "
"When controlled by a human, dies to remains that can be rebuilt.  " → "由人类控制时，死亡变为可重建的残骸。  "
"Reverts to neutral status on death, rather than truly dying.  " → "死亡时恢复为中立状态，而非真正死亡。  "
```

### 行为/命令 (lines ~3895-3948) — 同另一文件
```
"Behaviour: " → "行为："
". " → "。"
"No queued orders\n" → "无排队命令\n"
"This unit has " → "此单位有 "
" queued orders, " → " 个排队命令，"
" queued orders, the first of which is " → " 个排队命令，第一个为 "
"Threat " → "威胁 "
"Waiting against " → "正在等待对抗 "
```

### 威胁/外银河/守卫 (lines ~3952-3997) — 同 Task 8
```
"This type of ship will stay threatfleet rather than joining the Hunter. " → "此类舰船将留在威胁舰队中，而不会加入猎手。"
"This ship will stay threatfleet, since it is not targeting humans. " → "此舰船将留在威胁舰队中，因为它不针对人类。"
"This ship will stay threatfleet, since it s not linked to the Sentinels hivemind for some reason. " → "此舰船将留在威胁舰队中，因其未链接到哨兵蜂巢思维。"
"This ship will leave the threatfleet and join the Hunters after " → "此舰船将在 "
"s of waiting around to attack, or after " → " 秒的待机攻击时间后离开威胁舰队加入猎手，或在经过 "
"s more seconds of just existing. " → " 秒的存在时间后离开。"
"Part of an Exostrike heading for " → "属于外银河打击部队，目标为 "
" on " → " 位于 "
" but without a target. " → " 但无明确目标。"
"Guarding " → "守卫 "
"This entity will despawn in " → "此实体将在 "
" seconds. " → " 秒后消失。"
```

### 舰队/旗舰 (lines ~4065-4125) — 同 Task 8
```
"Is " → "是 "
"centerpiece" → "旗舰"
" of fleet " → " 的舰队 "
"This flagship has been crippled " → "此旗舰已被重创 "
" times.  " → " 次。  "
"Ships unloaded have " → "卸载的舰船有 "
" seconds delay before firing.  " → " 秒的开火延迟。"
"Ships unloaded from this flagship have a delay of " → "从此旗舰卸载的舰船有 "
" seconds before they can shoot.  " → " 秒后才能开火。"
"This fleet is on friendly planets, and can rebuild its ships quicker.  " → "此舰队位于友方星球，可以更快重建舰船。"
"In transport-ready mode.  " → "运输就绪模式。"
"My fleet is in transport-ready mode, where all ships try to get into my bays.  " → "我的舰队处于运输就绪模式，所有舰船正试图进入舱位。"
```

### 黑客/隐形/外银河打击 (lines ~4153-4272)
```
"This unit is currently hacking; it is diverting engine power to the Hacking Matrix, so it is slowed and cannot leave the planet. " → "此单位正在黑客入侵；它将引擎能量转移到黑客矩阵，因此速度降低且无法离开星球。"
"Hacking disables a ships cloaking system. " → "黑客入侵会禁用舰船的隐形系统。"
"The AI will generate Exostrikes (Exogalactic Strikeforces) against you if you capture this structure. " → "如果你占领此建筑，AI 将派出外银河打击部队对付你。"
"This unit only stays cloaked if its faction owns its planet. " → "此单位仅在其阵营控制该星球时保持隐形。"
```

### 增援/工厂 (lines ~4296-4500)
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
```

### 建造等级/堆叠/旗舰模式 (lines ~4567-4631)
```
"Must be at mark level " → "必须达到标记等级 "
" to build this structure. " → " 才能建造此建筑。"

"This stack takes damage like normal, but shoots " → "此堆叠正常承受伤害，但发射 "
"x</color> the normal amount of shots.  " → "倍</color> 的正常射击量。"

"Hold " → "按住 "
"You can find out more about this, and change its mode, on the Fleets sidebar tab.  Find this flagship's fleet and click it.  " → "你可以在舰队侧边栏页签中了解更多信息并更改其模式。找到此旗舰的舰队并点击它。"
```

### 未探索空间 (line ~4893)
```
"In Unexplored Space - unable to function without scouts having ever been sent here!"
→ "在未探索空间 - 没有侦察兵到过此地则无法运作！"
```

### Damage modifier debug strings (lines ~6918-7663)
Keep English:
- `"Unknown DamageModifierBasedOn."` — debug
- `"Unknown DamageModifierAppliesTo"` / `"Unknown DamageModifierAppliesTo."` — debug
- `"multiples of"` — internal identifier, keep
- `"mm"` — unit identifier in damage modifier context, keep

### 剩余遗漏 (lines ~5600-9200)
```
"damage" → "伤害"  (at line ~5664)

"None" → "无"  (at lines 6545, 6569, 8116, 8188, 8291 — in display context)

"unknown planet" → "未知星球"  (at line ~9092)
```

### 保持英文的调试字符串
- `"null relatedEntityData"` (line 47)
- `"relatedEntityData not a ship"` (line 52)
- Commented-out lines 177-182
- `"fireteamId "` (line 1912)
- `"ComputeDisabledReason: "` (line 1978)
- `"BUG!  OutOfRange!"` (line 4654)
- `"Current ship coordinates: "` (line 4836)
- `"ERROR_WRITE_NICE_TEXT: "` (lines 4943, 4988)
- `"Exception during generation of tooltip!"` (line 5145, already translated)
- `"Unknown DamageModifierBasedOn."` (lines 6918, 6997, 7149, 7228, 7426, 7622)
- `"Unknown DamageModifierAppliesTo"` / `"Unknown DamageModifierAppliesTo."` (lines 7045, 7276, 7467, 7663)
- `"multiples of"` (line 6937)
- `"mm"` (lines 6977, 7208)
- All damage abort codes (lines 7690-7723)
- `"OnlyWriteIfWeapon"` etc (lines 6578, 6621 — commented out)

## 执行步骤
1. Read the file in sections (lines 1300-3000, 3000-4000, 4000-4900, 5500-9200)
2. For each string in the translation table, use Edit tool
3. Run build.ps1
4. Report to task-14-report.md
