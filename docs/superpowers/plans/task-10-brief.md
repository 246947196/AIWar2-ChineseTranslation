# Task 10: 翻译 Window_InGameHoverEntityInfo.cs 模块/隐形/数据文本 (lines 7576-9185)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

## 翻译原则
- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>` 、 `</color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- Debug 日志、内部标识符、错误码不翻译
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件

## 翻译对照表

### 模块属性增强 (lines ~7576-7619)
Read exact lines to find the precise strings. Expected translations:
```
"Hull Health Multiplied By " → "船体生命值倍率 "
"x" → "倍"  (ONLY in the specific context of these module stat display lines, not all "x" in the file)
"Shield Health Multiplied By " → "护盾生命值倍率 "
"Bubble Forcefield added with " → "气泡力场添加，"
"x normal personal shield rating." → "倍正常个人护盾评级。"
"Bubble Forcefield replaces personal shield; no shield strength change." → "气泡力场替换个人护盾；护盾强度不变。"
```

### 隐形系统 (lines ~7682-7695)
```
"Max Cloaking Points: <color=#ffdf72>" → "最大隐形点数：<color=#ffdf72>"
"Cloaking" → "隐形"
": This ship has <color=#ffdf72>" → "：此舰船有 <color=#ffdf72>"
"Every time this ship fires, it will expend " → "此舰船每次开火将消耗 "
"After " → "在 "
" seconds of not losing any cloaking points, this ship will regain all of the lost cloaking points.  " → " 秒未损失隐形点数后，此舰船将恢复所有损失的点数。"
```

### 牵引/范围 (lines ~7723-7779)
```
"They can still move freely, pulling this unit with them, but they can't leave the current planet.  " → "它们仍可自由移动，拖着此单位，但无法离开当前星球。"
"infinite range" → "无限范围"
"range " → "范围 "
"x</color>, only target engines < <color=#ffdf72>" → "倍</color>，仅目标引擎 < <color=#ffdf72>"
"All enemy squads on-planet" → "星球上所有敌方小队"
"All enemy squads within range " → "范围内所有敌方小队"
"x</color> their normal speed if they have an engine power less than <color=#ffdf72>" → "倍</color> 正常速度，如果引擎动力低于 <color=#ffdf72>"
```

### 伤害中止代码 (lines ~7817-7880) — 保持英文
DO NOT translate (debug strings):
- `"Immune to All Damage"`
- `"Newly-Created Immunity To Damage"`
- `"External Invulnerability"`
- `"foundProtectorButCouldNotHitDueToFiniteHitCountAOE"`
- `"Debug_IgnoresDamage"`
- `"HonorFiniteHitCountAOE and not in list"`
- `"Calculated Zero Damage!"`
- `"Damage-Drop-During-Hit"`
- `"Health-Of-Target-Zero"`
- `"Overdrives-Shields-No-Shields"`
- `"Shooting Dead Target"`
- `"Only Fires On Death"`
- `"Maintain Cloak When No Direct Target"`
- `"Empty Target List"`
- `"All Targets Out Of Range"`
- `"No Viable Targets"`

### 重创状态 (lines ~7894-7899)
```
"Crippled - will not die, but needs to be repaired (at " → "重创 - 不会死亡，但需要修复（"
"x normal cost) to full health to function again!" → "倍正常费用）至满血才能恢复功能！"
"Crippled - will not die, but needs to be repaired to full health to function again!" → "重创 - 不会死亡，但需要修复至满血才能恢复功能！"
```

### 物质状态/生成组 (lines ~8125-8653)
Read exact lines. Expected strings:
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

### 转换计时器/属性标签 (lines ~8888-9105)
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

## 执行步骤
1. Read the file around lines 7576-9185 to find exact string patterns
2. For each English string in the translation table, use Edit tool to replace it
3. Run build.ps1 to verify
4. Write report to D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\docs\superpowers\plans\task-10-report.md
