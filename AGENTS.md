# AI War 2 汉化项目规范

本文件是 AI 助手的项目上下文。详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

XML 文件整体替换 + DLL 源码编译替换

## 工作流程

1. 编辑翻译文件夹里的 XML 或 DLLSource/ 中的 C# 源码
2. 运行 `deploy.ps1` 部署（含 XML + DLL）
3. 启动游戏验证
4. 提交到 Git

## 关键约定

- Git 分支统一使用 `main`
- 翻译和使用分开，文件结构一致

## DLL 汉化（已完成）

三个外部 DLL 项目已汉化并编译：

| 项目 | 源码位置 | 编译产物 |
|------|---------|---------|
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/ | DLLBin/AIWarExternalCode.dll |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | DLLBin/AIWarExternalDeepProcessingCode.dll |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | DLLBin/AIWarExternalVisualizationCode.dll |

### 编译命令

```powershell
.\build.ps1
```

编译器：Roslyn 4.12.0（`C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472\csc.exe`）

### 翻译规则

1. **使用 Edit 工具**：逐字符串替换，禁止用 Write 覆写整个文件
2. **只改引号内内容**：只能替换 `"..."` 内的文本
3. **保留插值和标签**：`$"{variable}"` 和 `<color>` 标签保持不变
4. **编译验证**：每翻译完一个文件编译验证，0 错误再继续
5. **UTF-8 BOM 编码**：所有含中文的 .cs 文件必须为 UTF-8 with BOM，否则游戏中文显示为方框
6. **禁止中文引号**：C# 字符串中不能使用 `""`（中文双引号），会被编译器误判。引用按钮名称等必须用 `''`（单引号）

### 关键警告

- **编译器版本**：必须使用 Roslyn 4.12.0 / C# 13.0，低版本会导致运行时错误
- **文件编码**：翻译后必须转换为 UTF-8 with BOM，否则中文显示为方框
- **中文引号**：`""` 会破坏 C# 语法，必须用 `''` 替代
