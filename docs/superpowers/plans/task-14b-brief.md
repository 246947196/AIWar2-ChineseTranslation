# Task 14b: Window_PrototypeInGameHoverEntityInfo.cs 早期遗漏 (lines ~2900-4900 + stragglers)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

## 规则
- 只改 `"..."` 内文本；保留 `<color>`/`</color>`/`{变量}`
- 禁止中文引号 `""`，用 `''` 替代
- Debug 字符串保持英文
- 使用 Edit 工具逐字符串替换

## 翻译对照表

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
"</color> if all remaining <color=#ffdf72>" → "</color> 如果所有剩余的 <color=#ffdf72>"
```

### 科技/祖先/后代 (lines ~3354-3537)
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

### 特殊属性 (lines ~3587-3714)
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

### 重创/弹射/死亡 (lines ~3852-3862)
```
"Cannot die, but rather becomes crippled at 1 HP.  " → "不会死亡，而是在 1 HP 时变为重创状态。  "
"When crippled, will use bail-out function to a friendly planet.  " → "重创时将使用弹射功能前往友方星球。  "
"When crippled in deepstrike territory, will use bail-out function to a friendly planet.  " → "在深袭区域重创时将使用弹射功能前往友方星球。  "
"When controlled by a human, dies to remains that can be rebuilt.  " → "由人类控制时，死亡变为可重建的残骸。  "
"Reverts to neutral status on death, rather than truly dying.  " → "死亡时恢复为中立状态，而非真正死亡。  "
```

### 行为/命令 (lines ~3895-3948)
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

### 威胁/外银河/守卫 (lines ~3952-3997)
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

### 舰队/旗舰 (lines ~4065-4125)
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

### 黑客/隐形/外银河 (lines ~4153-4272)
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

### 建造等级/堆叠/旗舰 (lines ~4567-4631)
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

### 遗漏字符串 (lines ~5600-9200)
```
"damage" → "伤害"  (at line ~5664)
"None" → "无"  (at lines 6545, 6569, 8116, 8188, 8291 — display context)
"unknown planet" → "未知星球"  (at line ~9092)
```

## 保持英文的调试字符串
- `"BUG!  OutOfRange!"` (line 4654)
- `"Current ship coordinates: "` (line 4836)
- `"ERROR_WRITE_NICE_TEXT: "` (lines 4943, 4988)
- `"Unknown DamageModifierBasedOn."` (lines 6918, 6997, 7149, 7228, 7426, 7622)
- `"Unknown DamageModifierAppliesTo"` / `"Unknown DamageModifierAppliesTo."` (lines 7045, 7276, 7467, 7663)
- `"multiples of"` (line 6937)
- `"mm"` (lines 6977, 7208)
- All damage abort codes (lines 7690-7723)

## 执行步骤
1. Read file sections lines 2900-4900, then lines 5600-9200
2. Use Edit tool for each replacement
3. Run build.ps1
4. Report to task-14b-report.md
