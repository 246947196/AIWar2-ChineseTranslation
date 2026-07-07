# Task 14a: Window_PrototypeInGameHoverEntityInfo.cs 早期遗漏 (lines ~1300-2900)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

## 规则
- 只改 `"..."` 内文本；保留 `<color>`/`</color>`/`{变量}`
- 禁止中文引号 `""`，用 `''` 替代
- Debug 字符串保持英文；跳过 `//` 注释行
- 使用 Edit 工具逐字符串替换

## 翻译对照表

### 黑客/武器/目标 (lines ~1356-1941)
```
"Hacking: " → "黑客入侵："
"some weapons" → "某些武器"
"Target is " → "目标为 "
```
Debug 保持: `"fireteamId "`, `"ComputeDisabledReason: "`, `"null relatedEntityData"`, `"relatedEntityData not a ship"`

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
Translate various forms of "Could provide" / "Provides":
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

### Bug 提示/牺牲/捕获/复制/警示 (lines ~2452-2669)
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

### 巢穴/引擎 (lines ~2669-2694)
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

## 执行步骤
1. Read file sections lines 1300-2900
2. Use Edit tool for each replacement
3. Run build.ps1
4. Report to task-14a-report.md
