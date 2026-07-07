# Task 4: Lesser Category 子分类标签

**上下文：** 翻译 `Window_PrototypeInGameHoverEntityInfo.cs` 中 Lesser Category 子分类标签。

## 修改的文件（1个文件，6处修改）

文件路径：`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

范围：第 9407-9425 行

### 第 9407 行
原文：`Buffer.Add( "Debuffs: " );`
改为：`Buffer.Add( "减益：" );`

### 第 9410 行
原文：`Buffer.Add( "Death Effects: " );`
改为：`Buffer.Add( "死亡效果：" );`

### 第 9413 行
原文：`Buffer.Add( "Ammo Types: " );`
改为：`Buffer.Add( "弹药类型：" );`

### 第 9416 行
原文：`Buffer.Add( "Exotic Damage: " );`
改为：`Buffer.Add( "异种伤害：" );`

### 第 9419 行
原文：`Buffer.Add( "General Damage: " );`
改为：`Buffer.Add( "通用伤害：" );`

### 第 9425 行
原文：`Buffer.Add( "Special Mechanic: " );`
改为：`Buffer.Add( "特殊机制：" );`

## 规则
- 只改 `"..."` 内文本
- 使用 Edit 工具逐字符串替换

## 验证
- 运行 `.\build.ps1`，确认 0 错误
