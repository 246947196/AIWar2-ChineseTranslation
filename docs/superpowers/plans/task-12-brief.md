# Task 12: 翻译 Window_PrototypeInGameHoverEntityInfo.cs 伤害修饰符/命令/统计 (lines 6250-10672)

## 文件
`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

## 翻译原则
- 只改 `"..."` 内文本，不碰引号外代码
- 保留 `{变量}` 和 `<color>` 、 `</color>` 标签
- 禁止中文引号 `""`，用 `''` 替代
- Debug 日志、内部标识符、错误码不翻译
- 使用 Edit 工具逐字符串替换，禁止 Write 覆写整个文件

## 翻译对照表

### 伤害修饰符标签 (lines ~6855-7242)
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

### 伤害修饰符条件 (lines ~7156-7242)
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

### damage abort codes (lines ~7690-7723) — 保持英文
DO NOT translate:
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

### 重创状态 (lines ~7737-7741)
```
"Crippled - will not die, but needs to be repaired (at " → "重创 - 不会死亡，但需要修复（"
"x normal cost) to full health to function again!" → "倍正常费用）至满血才能恢复功能！"
"Crippled - will not die, but needs to be repaired to full health to function again!" → "重创 - 不会死亡，但需要修复至满血才能恢复功能！"
```

### 模块属性增强 (lines ~6545-6621)
```
"Hull Health Multiplied By " → "船体生命值倍率 "
"Shield Health Multiplied By " → "护盾生命值倍率 "
"Bubble Forcefield added with " → "气泡力场添加，"
"Bubble Forcefield replaces personal shield; no shield strength change." → "气泡力场替换个人护盾；护盾强度不变。"
"Max Cloaking Points: <color=#ffdf72>" → "最大隐形点数：<color=#ffdf72>"
"Cloaking" → "隐形"
": This ship has <color=#ffdf72>" → "：此舰船有 <color=#ffdf72>"
"Every time this ship fires, it will expend " → "此舰船每次开火将消耗 "
"After " → "在 "
" seconds of not losing any cloaking points, this ship will regain all of the lost cloaking points.  " → " 秒未损失隐形点数后，此舰船将恢复所有损失的点数。"
"They can still move freely, pulling this unit with them, but they can't leave the current planet.  " → "它们仍可自由移动，拖着此单位，但无法离开当前星球。"
"infinite range" → "无限范围"
```

### 物质状态/生成组 (lines ~7962-8494)
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

### 命令文本 (lines ~9035-9115)
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

### 建造/修理吞吐量 (lines ~9949-10042)
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

### 属性标签 (lines ~10150-10672)
Also search for and translate these at the END of the file:
```
"Strength: " → "战力："
"Hull: " → "船体："
"Shield: " → "护盾："
"Metal: " → "金属："
"Energy: " → "能量："
"Argon: " → "氩气："
"Radon: " → "氡气："
"Xenon: " → "氙气："
"This will expire after " → "此将在 "
"This will expire in " → "此将在 "
"Could not find a " → "找不到 "
```

## 执行步骤
1. Read the file in sections (lines 6250-7500, 7500-8500, 8500-9500, 9500-10672) to find exact strings
2. For each English string in the translation table, use Edit tool to replace it
3. Run build.ps1 to verify
4. Write report to D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\docs\superpowers\plans\task-12-report.md
