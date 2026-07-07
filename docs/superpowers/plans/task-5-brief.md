# Task 5: Buff/ShipClass 通用文本

**上下文：** 翻译 `Window_PrototypeInGameHoverEntityInfo.cs` 中的通用标签和 Debuff 修饰符名称。

## 修改的文件（1个文件，18处修改）

文件路径：`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

### 第 9496 行
原文：`Buffer.Add( "All Debuffs" );`
改为：`Buffer.Add( "全部减益" );`

### 第 9498 行
原文：`Buffer.Add( "Debuffs" );`
改为：`Buffer.Add( "减益" );`

### 第 9556 行
原文：`Buffer.Add( "All Death Effects" );`
改为：`Buffer.Add( "全部死亡效果" );`

### 第 9558 行
原文：`Buffer.Add( "Death Effects" );`
改为：`Buffer.Add( "死亡效果" );`

### 第 9569 行（注释内，不活跃代码，但仍翻译以保持一致性）
原文：`Buffer.Add( "All Zombifying Types" );`
改为：`Buffer.Add( "全部僵尸化类型" );`

### 第 9571 行（注释内）
原文：`Buffer.Add( "Zombifying Types" );`
改为：`Buffer.Add( "僵尸化类型" );`

### 第 9602 行
原文：`Buffer.Add( "Immune to all damage" );`
改为：`Buffer.Add( "免疫所有伤害" );`

### 第 9604 行
原文：`Buffer.Add( "Invulnerable" );`
改为：`Buffer.Add( "无敌" );`

### WriteShipClass_ModifierData 调用中的修饰符名称（第 9504-9549 行）

第 9504 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Engine Slow", "E Slow", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "引擎减速", "引减", ...`

第 9510 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Weapon Slow", "W Slow", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "武器减速", "武减", ...`

第 9516 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Paralysis", "Stun", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "瘫痪", "晕眩", ...`

第 9522 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Acid", "Acid", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "腐蚀", "腐蚀", ...`

第 9528 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Weapon Phasing", "Phase", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "武器相位", "相位", ...`

第 9534 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Knockback", "Knock", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "击退", "击退", ...`

第 9540 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Tachyon Beams", "Tach", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "超光速粒子束", "粒子", ...`

第 9546 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Gravitic Cores", "Grav", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "引力核心", "引力", ...`

**注意：** WriteShipClass_ModifierData 调用中，第 3 个参数是长名（ModifierNameLong），第 4 个参数是短名（ModifierNameShort）。

## 规则
- 只改 `"..."` 内文本
- 使用 Edit 工具逐字符串替换

## 验证
- 运行 `.\build.ps1`，确认 0 错误
