# Task 1: 修正 TODO/BUG 中英混杂字符串

**上下文：** AI War 2 汉化项目。已翻译的 C# 文件中存在"TODO"和"BUG"等英文词未翻译的中英混杂字符串，需要修正。

## 修改的文件（4个文件，5处修改）

### 1. ZenithMinersDescriptionAppender.cs
路径：`DLLSource/AIWarExternalCode/src/DescriptionAppenders/DLC2/ZenithMinersDescriptionAppender.cs`
位置：第 39 行
原文：`Buffer.Add( "TODO: 为此效果定义附加数据 " + data.Effect );`
改为：`Buffer.Add( "待办：为此效果定义附加数据 " + data.Effect );`

### 2. HarvesterDescriptionAppender.cs
路径：`DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/HarvesterDescriptionAppender.cs`
位置：第 21 行
原文：`Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个BUG" );`
改为：`Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个错误" );`

### 3. SporeDescriptionAppender.cs
路径：`DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/SporeDescriptionAppender.cs`
位置：第 21 行
原文：`Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个BUG" );`
改为：`Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个错误" );`

### 4. TeliumDescriptionAppender.cs
路径：`DLLSource/AIWarExternalCode/src/DescriptionAppenders/BaseGame/TeliumDescriptionAppender.cs`
位置：第 21 行
原文：`Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个BUG" );`
改为：`Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个错误" );`
位置：第 27 行
原文：`Buffer.Add( "此泰利姆的 tData 为空。这是一个BUG。" );`
改为：`Buffer.Add( "此泰利姆的 tData 为空。这是一个错误。" );`

## 规则
- 只改 `"..."` 内文本，不碰引号外代码
- 使用 Edit 工具逐字符串替换

## 验证
- 在 `.\build.ps1` 中运行编译，确认 0 错误

## 全局约束
- 禁止中文引号 `""`，用 `''` 替代
- Debug 日志、内部标识符不翻译
