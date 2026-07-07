# Task 3: Greater/Lesser Category 标签

**上下文：** AI War 2 汉化项目。翻译 `Window_PrototypeInGameHoverEntityInfo.cs` 中 Greater Category 标题标签。

## 修改的文件（1个文件，8处修改）

文件路径：`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_PrototypeInGameHoverEntityInfo.cs`

### 第 9331 行
原文：`Buffer.Add( "Vulnerabilities: " );`
改为：`Buffer.Add( "弱点：" );`

### 第 9333 行
原文：`Buffer.Add( "Vul: " );`
改为：`Buffer.Add( "弱：" );`

### 第 9339 行
原文：`Buffer.Add( "Resistances: " );`
改为：`Buffer.Add( "抗性：" );`

### 第 9341 行
原文：`Buffer.Add( "Res: " );`
改为：`Buffer.Add( "抗：" );`

### 第 9347 行
原文：`Buffer.Add( "Immunities: " );`
改为：`Buffer.Add( "免疫：" );`

### 第 9349 行
原文：`Buffer.Add( "Imu: " );`
改为：`Buffer.Add( "免：" );`

### 第 9355 行
原文：`Buffer.Add( "Other: " );`
改为：`Buffer.Add( "其他：" );`

### 第 9357 行
原文：`Buffer.Add( "Oth: " );`
改为：`Buffer.Add( "他：" );`

## 规则
- 只改 `"..."` 内文本，不碰引号外代码
- 使用 Edit 工具逐字符串替换

## 验证
- 运行 `.\build.ps1`，确认 0 错误

## 全局约束
- 禁止中文引号 `""`，用 `''` 替代
