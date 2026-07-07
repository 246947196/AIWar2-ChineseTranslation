# Task 6: 特殊机制和伤害类型文本

**上下文：** 翻译 `Window_PrototypeInGameHoverEntityInfo.cs` 中 Exotic Damage 修饰符和 Special Mechanics 文本。

## 修改的文件（1个文件，15处修改）

文件路径：`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

### Exotic Damage 修饰符名称（第 9630-9659 行）

第 9630 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "All Exotic Damage", "Exotic Damage", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "全部异种伤害", "异种伤害", ...`

第 9638 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Attrition", "Attr", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "磨损", "磨损", ...`

第 9644 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Electrotoxicity", "ETox", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "电毒性", "电毒", ...`

第 9650 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Revenge Shots", "Veng", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "复仇射击", "复仇", ...`

第 9656 行：
原文：`WriteShipClass_ModifierData( Buffer, ShipClass, "Ion Cannon", "Ion", ...`
改为：`WriteShipClass_ModifierData( Buffer, ShipClass, "离子炮", "离子", ...`

**注意：** WriteShipClass_ModifierData 调用中，第 3 个参数是长名，第 4 个参数是短名。

### Special Mechanics 文本（第 9669-9706 行）

第 9669 行：
原文：`Buffer.Add( "Tractor Beams  Black Hole Machines  Getting Devoured  Getting Infested" );`
改为：`Buffer.Add( "牵引光束  黑洞机器  被吞噬  被感染" );`

第 9671 行：
原文：`Buffer.Add( "All Special Mechanics" );`
改为：`Buffer.Add( "全部特殊机制" );`

第 9682 行：
原文：`Buffer.Add( "Tractor Beams " );`
改为：`Buffer.Add( "牵引光束 " );`

第 9684 行：
原文：`Buffer.Add( "Tractors " );`
改为：`Buffer.Add( "牵引 " );`

第 9691 行：
原文：`Buffer.Add( "Black Hole Machines " );`
改为：`Buffer.Add( "黑洞机器 " );`

第 9693 行：
原文：`Buffer.Add( "Black Holes " );`
改为：`Buffer.Add( "黑洞 " );`

第 9699 行：
原文：`Buffer.Add( "Getting Devoured " );`
改为：`Buffer.Add( "被吞噬 " );`

第 9701 行：
原文：`Buffer.Add( "Devouring " );`
改为：`Buffer.Add( "吞噬 " );`

第 9704 行：
原文：`Buffer.Add( "Getting Infested " );`
改为：`Buffer.Add( "被感染 " );`

第 9706 行：
原文：`Buffer.Add( "Infestation " );`
改为：`Buffer.Add( "感染 " );`

## 规则
- 只改 `"..."` 内文本
- 使用 Edit 工具逐字符串替换

## 验证
- 运行 `.\build.ps1`，确认 0 错误
