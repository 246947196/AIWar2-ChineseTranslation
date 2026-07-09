# AI War 2 汉化项目上下文

详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

XML 文件整体替换 + DLL 源码编译替换 + AssetBundle 拦截重定向

## 工作流程 (日常翻译)

1. 编辑 `GameData/Configuration/` 中 XML、`DLLSource/` 中 C# 源码、或 `arcenui_translations.json`
2. 运行 `python patch_arcenui.py extract` 更新翻译模板
3. 编辑 `arcenui_translations.json` 填入翻译
4. 运行 `build.ps1` 编译 DLL + 自动 patch arcenui bundle
5. 运行 `deploy.ps1` 部署（含版本检查：基线版本必须匹配游戏版本）
6. 启动游戏验证
7. 提交 Git

## 游戏更新检测

工具：`check_update.ps1` — 替代旧的 `check_translation.ps1`

| 命令 | 用途 |
|------|------|
| `check_update.ps1 -snapshot` | 建立基线快照（必须在英文状态下运行） |
| `check_update.ps1 -snapshot -force` | 强制覆盖已有快照 |
| `check_update.ps1` | 检测游戏更新，生成报告到 `translation_update_report_*.txt` |

**游戏更新后流程：**
1. Steam 更新游戏（文件恢复为英文）
2. `check_update.ps1` → 生成变更报告
3. `check_update.ps1 -snapshot -force` → 更新基线
4. 按报告逐条翻译修改
5. `build.ps1` + `deploy.ps1`
6. 提交 git

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

## 控制菜单分类翻译

Control Bindings 菜单左侧分类按钮显示的是 `InputAction` XML 文件中 `category` 属性值。

2026-07-07 翻译了 `GameData/Configuration/InputAction/` 下所有 XML 文件的 category 值：

| 英文 | 中文 | 涉及文件 |
|------|------|----------|
| Critical | 关键 | Central.xml, UIAliases.xml |
| Common | 通用 | Central.xml, UIAliases.xml, UtilityActions.xml |
| Selection | 选择 | Central.xml, UIAliases.xml |
| Tooltips/Details | 提示信息/详情 | Central.xml |
| Camera | 视角 | Camera.xml |
| Sidebar | 侧边栏 | UIAliases.xml |
| Ship Controls | 舰船控制 | UIAliases.xml |
| Other UI | 其他界面 | UIAliases.xml |
| Fleet Groups | 舰队组 | ControlGroups.xml |
| Overlays | 覆盖层 | UtilityActions.xml |
| Multiplayer | 多人 | UIAliases.xml |
| Utility | 实用工具 | UtilityActions.xml |

注意：`Hidden` 和 `Unused` 保留英文，因为 `Window_ControlBindingsMenu.cs` 的 C# 代码按精确字符串 `"Unused"`/`"Hidden"` 过滤隐藏条目。

## DLL 项目一览

| 项目 | 源码 | 编译 | 翻译 |
|------|------|------|------|
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | ✅ | ✅ 完成 |
| ArcenUIAssetRedirect（BepInEx 插件） | DLLSource/ArcenUIAssetRedirect/src/ | ✅ | ✅ 完成 |
| ArcenUniversal（IL 汉化） | —（无源码，见 SPEC 8.15） | — | ✅ 已部署（151 条 ldstr） |
| ArcenAIW2Core（IL 汉化） | —（无源码，见 SPEC 8.15） | — | ✅ 已部署（889 条 ldstr） |
| ArcenAIW2Visualization（IL 汉化） | —（无源码，见 SPEC 8.15） | — | ✅ 已完成 |

## deploy.ps1 行为说明

- **版本检查**：用正则 `(?s)<game_version\s[^>]*?minor_version="(\d+)"[^>]*?>` 从 `KDL_GameVersions.xml` 提取最新版本号（支持跨行属性、兼容无 `major_version` 的旧条目）
- **`$ErrorActionPreference = "Stop"`**：任何源文件缺失时立即终止，不静默继续
- **字体部署**：`sarasa_gothic` 目录使用 `-Recurse` 递归复制，确保字体文件完整
- **DLL 部署**：自动 `New-Item` 创建 `GameData/ModdableLogicDLLs/` 目标目录

## 编译

```powershell
.\build.ps1
```

编译器：Roslyn 4.12.0（`C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472\csc.exe`）
MSBuild：`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`
目标框架：
- 外部代码项目：.NET Framework 4.7.1
- BepInEx 插件 (ArcenUIAssetRedirect)：.NET Framework 4.7.2
引用：`..\..\..\ReliableDLLStorage\`（插件额外引用 `..\..\..\BepInEx\core\`）

> 核心 DLL（ArcenUniversal / ArcenAIW2Core / ArcenAIW2Visualization）不走编译路线，改用 `ilpatch`（dnlib）做 IL 字面量替换，详见 SPEC 8.15。

## 翻译规则

1. **Edit 工具**逐字符串替换，禁止 Write 覆写整个文件
2. 只改 `"..."` 内文本，不碰引号外代码
3. 保留 `{变量}` 和 `<color>` 标签
4. 每翻译完一个**外部代码** DLL 编译验证，0 错误继续（核心 DLL 走 IL 汉化，不编译，用 `ilpatch inspect` 回读验证）
5. 禁止中文引号 `""`，用 `''` 替代
6. Debug 日志、内部标识符不翻译
7. 核心 DLL（IL 汉化）：用 `tools/ilpatch` 按 JSON 字典替换 `ldstr` 字面量，不碰任何代码结构（详见 SPEC 8.15）
8. **JSON 字典编码**：合并/重写 ilpatch 字典必须用**无 BOM 的 UTF-8**（PowerShell `Set-Content -Encoding UTF8` 会加 BOM，导致 ilpatch/Python 解析失败）。优先用 Python `open(path,'w',encoding='utf-8')` 或 .NET `UTF8Encoding(false)`
9. **大 DLL 并行翻译**：候选 >1000 条时按行切分为 `*.partN.json` 分片交多代理并行；合并时切忌直接拼接分片文件（子代理易破坏 JSON 结构），应重新 `extract` 干净骨架后用正则提取各分片 value 注入（详见 SPEC 8.15.9）
