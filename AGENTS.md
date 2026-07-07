# AI War 2 汉化项目上下文

详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

XML 文件整体替换 + DLL 源码编译替换 + AssetBundle 拦截重定向

## 工作流程

1. 编辑 `GameData/Configuration/` 中 XML、`DLLSource/` 中 C# 源码、或 `arcenui_translations.json`
2. 运行 `python patch_arcenui.py extract` 更新翻译模板
3. 编辑 `arcenui_translations.json` 填入翻译
4. 运行 `build.ps1` 编译 DLL + 自动 patch arcenui bundle
5. 运行 `deploy.ps1` 部署
6. 启动游戏验证
7. 提交 Git

## arcenui AssetBundle 汉化

arcenui bundle 中的 UI 文本存储在 Unity 预制体的 `m_text` 字段中。

- `patch_arcenui.py` — 主工具（info / extract / patch）
- `arcenui_translations.json` — 翻译对照表（英→中）
- 原理：用 UnityPy 加载 bundle → 替换所有 MonoBehaviours 的 `m_text` → 输出到 `BepInEx/plugins/ChineseTranslation/AssetBundles_Win/arcenui`
- `ArcenUIAssetRedirect.dll` (BepInEx 插件) 拦截游戏加载，重定向到汉化版 bundle
- 翻译范围：目前 150 个 UI 字符串已翻译（按钮标签、窗口标题、背景故事等），120 个占位/数字/人名已标记不翻译

## 主菜单按钮文本来源

主菜单按钮文本来自两个来源：

1. **AssetBundle（prefab m_text）**：`patch_arcenui.py` 提取并替换，适用于无 `GetTextToShowFromVolatile` 覆盖的按钮
2. **C# 代码**：`WindowTogglingButtonController` 构造函数参数（`TextWhenClosed`）和 `GetTextToShowFromVolatile` 覆盖方法

2026-07-07 修复了以下 C# 按钮标签（在 `Window_MainMenu.cs` 中）：
- `bSettings`："Settings" → "设置"
- `bControls`："Settings" → "控制"
- `bViewCredits`："Staff Credits" → "开发人员致谢"
- `bViewCreditsKickstarter`："Kickstarter Credits" → "众筹致谢"
- `bViewBackgroundStory`："Background Story" → "背景故事"

## DLL 项目一览

| 项目 | 源码 | 编译 | 翻译 |
|------|------|------|------|
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | ✅ | ✅ 完成 |
| ArcenUIAssetRedirect（BepInEx 插件） | DLLSource/ArcenUIAssetRedirect/src/ | ✅ | ✅ 完成 |
| ArcenUniversal（反编译） | DLLSource/ArcenUniversal/ | ✅ 0 错误 | ⏳ |
| ArcenAIW2Core（反编译） | DLLSource/ArcenAIW2Core/ | ❌ | ⏳ |
| ArcenAIW2Visualization（反编译） | DLLSource/ArcenAIW2Visualization/ | ❌ | ⏳ |

## 编译

```powershell
.\build.ps1
```

编译器：Roslyn 4.12.0（`C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472\csc.exe`）
MSBuild：`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`
目标框架：
- 外部代码/反编译项目：.NET Framework 4.7.1
- BepInEx 插件 (ArcenUIAssetRedirect)：.NET Framework 4.7.2
引用：`..\..\..\ReliableDLLStorage\`（插件额外引用 `..\..\..\BepInEx\core\`）

## 翻译规则

1. **Edit 工具**逐字符串替换，禁止 Write 覆写整个文件
2. 只改 `"..."` 内文本，不碰引号外代码
3. 保留 `{变量}` 和 `<color>` 标签
4. 每翻译完一个 DLL 编译验证，0 错误继续
5. 禁止中文引号 `""`，用 `''` 替代
6. Debug 日志、内部标识符不翻译
7. **反编译项目**：只改 `"..."` 内字符串，不改 csproj 配置、GlobalUsings.cs、编译修复代码
