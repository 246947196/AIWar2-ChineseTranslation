# Task 13: 翻译 Window_InGameHoverEntityInfo.cs — 早期区域遗漏 (lines 1-4780)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

## 规则
- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>`、`</color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- Debug 日志、内部标识符、错误码不翻译
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件

## 翻译对照表

### 资源/武器/目标标签 (lines ~1000-2500)
```
"Metal: " → "金属："
"some weapons" → "某些武器"

"Target is " → "目标为 "
```
Note: "PrimaryKeyID " and "FireteamId " are debug, keep English.
Also skip `"null relatedEntityData"` and `"relatedEntityData not a ship"` (debug, lines 496-501).

### 行星停留时间 (lines ~3074-3077)
```
"since it has been at this planet and non-crippled for more than "
→ "因已在此星球且未受损超过 "

"once it has been at that planet and non-crippled for at least " → "一旦在此星球且未受损至少 "
" more.  " → " 后。  "
```

### 牺牲修复 (line ~3275)
```
"When allied units on this planet would die, this ship instead reduces its own health to regenerate them. The efficiency is one hull point from this unit per <color=#ffdf72>"
→ "当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>"
```

### 轨道描述 (lines ~3495-3511)
```
"Orbits Gravity Well at " → "绕重力井轨道 "
"Orbits ancestor unit at " → "绕祖先单位轨道 "
"Orbits flagship at " → "绕旗舰轨道 "
"/s" → "/秒"
```

### 当前隐形点数 (line ~3741)
```
"Current Cloaking Points: " → "当前隐形点数："
```

### 无敌需求 (line ~4008)
```
"To have invulnerability, it requires at least "
→ "获得无敌需要至少 "
```

### AIP 文本 (lines ~4044-4105)
```
"AI Progress (AIP) will <color=#ffdf72>rise by " → "AI 进程 (AIP) 将<color=#ffdf72>上升 "
"</color> if this dies.  " → "</color> 如果此单位死亡。  "
"Since you have not already paid the AI Progress (AIP) price for taking this planet, AIP will <color=#ffdf72>rise by " → "由于你尚未为此星球支付 AI 进程 (AIP) 代价，AIP 将<color=#ffdf72>上升 "
"AI Progress (AIP) will <color=#ffdf72>be reduced by " → "AI 进程 (AIP) 将<color=#ffdf72>减少 "

For the metal/science/hacking reward texts:
"player kills this unit, they get " → "击杀此单位，获得 "
" metal. </color>" → " 金属。</color>"
" science. </color>" → " 科学。</color>"
" hacking points. </color>" → " 黑客点数。</color>"
" science </color> and " → " 科学</color> 和 "

For the "all remaining" variant:
"AI Progress (AIP) will <color=#ffdf72>rise by " → "AI 进程 (AIP) 将<color=#ffdf72>上升 "
"AI Progress (AIP) will <color=#ffdf72>be reduced by " → "AI 进程 (AIP) 将<color=#ffdf72>减少 "
"</color> if all remaining <color=#ffdf72>" → "</color> 如果所有剩余的 <color=#ffdf72>"
```

### 起始等级/科技 (lines ~4144-4189)
```
"Starts at " → "起始等级 "
"Upgraded by Tech: " → "科技升级："
"Techs: " → "科技："
```

### 祖先/后代 (lines ~4287-4325)
```
"Ancestor Unit: " → "祖先单位："
"Normally will have an ancestor unit, and dies if that ancestor dies.  " → "通常有祖先单位，如果祖先死亡则此单位也会死亡。  "
"Builds up to " → "最多建造 "
"Error!  No descendants available to build!" → "错误！没有可建造的后代！"
"Descendants are: " → "后代为："
"Descendants are a mix of: " → "后代混合了："
```

### 特殊属性 (lines ~4376-4496)
```
"Elite: Only one elite ship line can be added to any fleet.  " → "精英：每支舰队只能添加一条精英舰船线。  "
"Allows AI ships to warp in here.  " → "允许 AI 舰船跃迁至此。  "
"Cannot traverse wormholes.  " → "无法穿越虫洞。  "
"Self-destructs if command station is destroyed.  " → "如果指挥站被摧毁则自毁。  "
"Loses <color=#ffdf72>" → "每秒损失 <color=#ffdf72>"
"Immune to all damage.  " → "免疫所有伤害。  "
"Cannot be repaired -- whatever this thing is, we don't know how to fix it.  " → "无法修复 -- 我们不知道这东西怎么修。  "
"Cannot be repaired.  " → "无法修复。  "
"Scrapping this unit gives no metal.  " → "拆解此单位不获得金属。  "
"Scrapping this unit on a friendly planet refunds " → "在友方星球拆解此单位返还 "
" Cannot be protected by forcefields, due to its strange interaction with the fabric of reality.  " → "由于与现实结构产生奇怪交互，无法被力场保护。  "
"Cannot be protected by forcefields.  " → "无法被力场保护。  "
"Immune to enemy weapon system bonus damage.  " → "免疫敌方武器系统加成伤害。  "
"Strange interactions with the very fabric of spacetime cause this to exist in the normal plane of existence only part of the time" → "与时空结构的奇怪交互导致此单位仅部分时间存在于正常位面"
"Phases to " → "相位切换至 "
" every " → " 每 "
```

### 无敌/构建点 (lines ~4534-4554)
```
"Immune to all damage for " → "免疫所有伤害，持续 "
"Currently has <color=#ffdf72>" → "当前拥有 <color=#ffdf72>"
```

### 重创/弹射/死亡 (lines ~4629-4640)
```
"Cannot die, but rather becomes crippled at 1 HP.  " → "不会死亡，而是在 1 HP 时变为重创状态。  "
"When crippled, will use bail-out function to a friendly planet.  " → "重创时将使用弹射功能前往友方星球。  "
"When crippled in deepstrike territory, will use bail-out function to a friendly planet.  " → "在深袭区域重创时将使用弹射功能前往友方星球。  "
"When controlled by a human, dies to remains that can be rebuilt.  " → "由人类控制时，死亡变为可重建的残骸。  "
"Reverts to neutral status on death, rather than truly dying.  " → "死亡时恢复为中立状态，而非真正死亡。  "
```

### "none" 显示文本 (lines ~4663-4673)
```
"none" → "无"
```
Note: Only in display context (dropdown values), NOT in null object checks.
Keep "null" as English (code logic context, lines 4688, 4698).

### 行为/命令 (lines ~4725-4780)
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

### 武器上下文 "damage" (line ~6691)
```
"damage" → "伤害"
```

## 保持英文的调试字符串
Do NOT translate:
- `"null relatedEntityData"` (line 496)
- `"relatedEntityData not a ship"` (line 501)
- `"PrimaryKeyID "` (line 2437)
- `"FireteamId "` (line 2439)
- `"BUG!  OutOfRange!"` (line 5604)
- `"targetPriorityList is empty!"` (line 5631)
- `"targetPriorityList.Count: "` (line 5635)
- `"Squad Faction ReasonCode: "` (line 5653)
- `"Current ship coordinates: "` (line 5910)
- `"ERROR_WRITE_NICE_TEXT: "` (lines 6018, 6064)
- `"Exception during generation of tooltip!"` (line 6227)
- All damage abort codes (lines 7817-7880)
- All commented-out lines (prefixed with //)

## 执行步骤
1. Read file sections (lines 1000-2500, 3074-3511, 3741-4780, 6691)
2. Use Edit tool for each replacement
3. Run build.ps1
4. Report to task-13-report.md
