# Task 7: Buff/辅助/能量文本

**上下文：** 翻译 `Window_PrototypeInGameHoverEntityInfo.cs` 中 Buff Limits、Supercharging 及其他辅助文本。

## 修改的文件（1个文件，25处修改）

文件路径：`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

### Buff Limits 标题（第 9726 行）
原文：`Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( "Buff Limits:" ).EndColor()...`
改为：`Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( "增益上限：" ).EndColor()...`

### Cannot be buffed（第 9804 行）
原文：`Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " Cannot be buffed. " ).EndColor();`
改为：`Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " 无法获得增益。 " ).EndColor();`

### Buff Limit 标签（第 9767-9791 行）

第 9767 行：
原文：`Buffer.Add( " Damage: " );`
改为：`Buffer.Add( " 伤害：" );`

第 9769 行：
原文：`Buffer.Add( " Dmg " );`
改为：`Buffer.Add( " 伤 " );`

第 9773 行：
原文：`Buffer.Add( " Hull: " );`
改为：`Buffer.Add( " 船体：" );`

第 9775 行：
原文：`Buffer.Add( " Hull " );`
改为：`Buffer.Add( " 船体 " );`

第 9780 行：
原文：`Buffer.Add( " Shield: " );`
改为：`Buffer.Add( " 护盾：" );`

第 9782 行：
原文：`Buffer.Add( " Shd " );`
改为：`Buffer.Add( " 护盾 " );`

第 9787 行：
原文：`Buffer.Add( " Speed: " );`
改为：`Buffer.Add( " 速度：" );`

第 9789 行：
原文：`Buffer.Add( " Spd " );`
改为：`Buffer.Add( " 速度 " );`

### Supercharging 文本（第 9747 行、第 9832-9849 行）

第 9747 行：
原文：`Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " Cannot Supercharge:" ).EndColor()...`
改为：`Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " 无法超载：" ).EndColor()...`

第 9832 行：
原文：`Buffer.Add( " Damage " );`
改为：`Buffer.Add( " 伤害 " );`

第 9837 行：
原文：`Buffer.Add( " Hull " );`
改为：`Buffer.Add( " 船体 " );`

第 9842 行：
原文：`Buffer.Add( " Shield " );`
改为：`Buffer.Add( " 护盾 " );`

第 9848 行：
原文：`Buffer.Add( " Speed " );`
改为：`Buffer.Add( " 速度 " );`

### ModifierData 单位后缀（第 9456-9465 行）

第 9456 行：
原文：`Buffer.Add( " duration " );`
改为：`Buffer.Add( " 持续时间 " );`

第 9458 行：
原文：`Buffer.Add( " damage " );`
改为：`Buffer.Add( " 伤害 " );`

第 9463 行：
原文：`Buffer.Add( " dur " );`
改为：`Buffer.Add( " 持续 " );`

第 9465 行：
原文：`Buffer.Add( " dmg " );`
改为：`Buffer.Add( " 伤 " );`

### "x" 和 "s"（第 9471、9481 行）

第 9471 行：
原文：`Buffer.Add( "x" );`
改为：`Buffer.Add( "倍" );`

第 9481 行：
原文：`Buffer.Add( "s" );`
改为：`Buffer.Add( "秒" );`

**注意：** "s" 作为秒数后缀，需确认上下文是否确实是时间单位后缀（在 `if (DataType == ShipClassData_ModifiedUnit.TimeBased)` 分支内）。

### 辅助文本（第 9920、10096、10098 行）

第 9920 行：
原文：`Buffer.Add( "Within " ).WrapRangeMoreReadable( ...`
改为：`Buffer.Add( "范围内 " ).WrapRangeMoreReadable( ...`

第 10096 行：
原文：`Buffer.Add( "its owners and allies with " );`
改为：`Buffer.Add( "其拥有者和盟友 " );`

第 10098 行：
原文：`Buffer.Add( "its enemies with " );`
改为：`Buffer.Add( "其敌人 " );`

## 规则
- 只改 `"..."` 内文本
- 使用 Edit 工具逐字符串替换

## 验证
- 运行 `.\build.ps1`，确认 0 错误
