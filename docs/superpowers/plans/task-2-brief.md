# Task 2: 翻译 Window_InGameHoverEntityInfo.cs

**上下文：** AI War 2 汉化项目。该文件 9185 行，仅有末尾 2 处 `Buffer.Add` 为英文调试文本，其余均已翻译或为代码逻辑。

## 修改的文件（1个文件，2处修改）

文件路径：`DLLSource/AIWarExternalCode/src/UIs/InGamePassiveDisplay/Window_InGameHoverEntityInfo.cs`

### 1. 第 9161 行
原文：`Buffer.Add( "Hey, I'm talking to you from the sidebar or the build menu, probably!  Not hovering over a specific unit." );`
改为：`Buffer.Add( "嘿，这是侧边栏或建造菜单的提示！当前没有悬停于具体单位。" );`

### 2. 第 9163 行
原文：`Buffer.Add( "Hey, I'm hovering over a specific unit at location: " ).Add( RelatedEntityOrNull.WorldLocation.X ).Add( "," ).Add( RelatedEntityOrNull.WorldLocation.Y );`
改为：`Buffer.Add( "嘿，当前悬停于位置：" ).Add( RelatedEntityOrNull.WorldLocation.X ).Add( "," ).Add( RelatedEntityOrNull.WorldLocation.Y );`

## 规则
- 只改 `"..."` 内文本，不碰引号外代码
- 使用 Edit 工具逐字符串替换

## 验证
- 运行 `.\build.ps1`，确认 0 错误

## 全局约束
- 禁止中文引号 `""`，用 `''` 替代
