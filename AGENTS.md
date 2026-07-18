# AI War 2 汉化项目上下文

详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

XML 文件整体替换 + DLL 源码编译替换 + AssetBundle 拦截重定向

## 工作流程 (日常翻译)

1. 编辑 `GameData/Configuration/` 中 XML、`DLLSource/` 中 C# 源码、`arcenui_translations.json`、或 `XMLMods/` 下的文件
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

**分层对比策略：**

| 层 | 对比方式 | 说明 |
|---|---------|------|
| XML | 文件哈希 + 字符串级 | `display_name`/`description` 等稳定属性名作 ID |
| C# 源码 (`CodeExternal/`) | **仅文件哈希** | 有 `DLLSource/` 翻译副本，文件变后用 diff 工具对比 |
| 核心 DLL | 文件哈希 | IL 汉化需重新 `ilpatch` |
| arcenui | 文件哈希 + 字符串级 | 英文原文作 key |

**游戏更新后流程：**
1. Steam 更新游戏（文件恢复为英文）
2. `check_update.ps1` → 生成变更报告
3. `check_update.ps1 -snapshot -force` → 更新基线
4. 按报告翻译修改（C# 源码用 diff 工具对比 `CodeExternal/` 与 `DLLSource/`）
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
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/ | ✅ | ✅ 完成（~2000+ 条字符串，含 2 个大文件全量补译） |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | ✅ | ✅ 完成 |
| ArcenUIAssetRedirect（BepInEx 插件） | DLLSource/ArcenUIAssetRedirect/src/ | ✅ | ✅ 完成 |
| WorldTMPFontPatch（BepInEx 插件） | DLLSource/WorldTMPFontPatch/src/ | ✅ | ✅ 完成 |
| ArcenUniversal（IL 汉化） | —（无源码，见 SPEC 8.15） | — | ✅ 已部署（151 条 ldstr） |
| ArcenAIW2Core（IL 汉化） | —（无源码，见 SPEC 8.15） | — | ✅ 已部署（889 条 ldstr） |
| ArcenAIW2Visualization（IL 汉化） | —（无源码，见 SPEC 8.15） | — | ✅ 已完成 |

## deploy.ps1 行为说明

- **版本检查**：用正则 `(?s)<game_version\s[^>]*?minor_version="(\d+)"[^>]*?>` 从 `KDL_GameVersions.xml` 提取最新版本号（支持跨行属性、兼容无 `major_version` 的旧条目）
- **`$ErrorActionPreference = "Stop"`**：任何源文件缺失时立即终止，不静默继续
- **字体部署**：`sarasa_gothic` 目录使用 `-Recurse` 递归复制，确保字体文件完整
- **DLL 部署**：自动 `New-Item` 创建 `GameData/ModdableLogicDLLs/` 目标目录
- **`.new.dll` 残留清理**（2026-07-18 修复）：`Get-ChildItem` 加 `-Recurse` 递归扫描 `AIWar2_Data/Managed/`、`PatchedAssemblies/`、汉化项目 `PatchedAssemblies/` 三个目录及其子目录（含 `patched/` 子目录）。ilpatch 运行时若 DLL 被游戏锁定会生成 `.new.dll` fallback，这些文件会"静默覆盖"真正的 patch DLL 导致翻译回退，必须递归清理干净

## 编译

```powershell
.\build.ps1
```

编译器：Roslyn 4.12.0（`C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472\csc.exe`）
MSBuild：`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`
目标框架：
- 外部代码项目：.NET Framework 4.7.1
- BepInEx 插件 (ArcenUIAssetRedirect, WorldTMPFontPatch)：.NET Framework 4.7.2
引用：`..\..\..\ReliableDLLStorage\`（插件额外引用 `..\..\..\BepInEx\core\` 和 `..\..\..\ReliableDLLStorage\Unity.TextMeshPro.dll`）

> 核心 DLL（ArcenUniversal / ArcenAIW2Core / ArcenAIW2Visualization）不走编译路线，改用 `ilpatch`（dnlib）做 IL 字面量替换，详见 SPEC 8.15。

## World TMP Font Patch 插件

**新建于 2026-07-11**，独立 BepInEx 插件，用于修复世界空间 `TextMeshPro` 组件的字体替换问题，**2026-07-11 追加** `TextMeshProUGUI.InternalUpdate` 后备 patch 修复加载界面方框（首次启动时序问题）。

### 背景

`I18NFont4UnityGame` 插件只 patch 了 `TMPro.TextMeshProUGUI`（画布 UI 文字）。银河地图行星名、实体标签等使用 `TMPro.TextMeshPro`（世界空间 3D 文字），两者是不同的 Unity 组件，I18NFont4UnityGame 未覆盖。

### 原理

- Harmony postfix patch `TMPro.TextMeshPro.OnEnable` + `InternalUpdate`
- 后备 patch `TMPro.TextMeshProUGUI.InternalUpdate`（补 I18NFont4UnityGame 首次启动时序缺口）
- 自动读取 `xiaoye97.I18NFont4UnityGame.cfg` 中的 `FontName` 配置，复用同一字体 AssetBundle
- 从 Bundle 中加载 `TMP_FontAsset`（按 `{FontName} SDF` 名称匹配），懒加载注入（首次调用 `GetFont()` 时加载字体）
- 部署位置：`BepInEx/plugins/ChineseTranslation/WorldTMPFontPatch.dll`

### 源码

```
DLLSource/WorldTMPFontPatch/
├── src/
│   ├── WorldTMPFontPatchPlugin.cs
│   └── PatchCharMapping.cs        ← 转义编码序列化修复（2026-07-18）
└── WorldTMPFontPatch.csproj
```

### 字体类型对比

| 组件类型 | 用途 | 处理插件 |
|---------|------|---------|
| `TextMeshProUGUI` | 画布 UI 文字（菜单、面板、提示框） | I18NFont4UnityGame + WorldTMPFontPatch（后备） |
| `TextMeshPro` | 世界空间 3D 文字（行星名、实体标签） | WorldTMPFontPatch |

### 已知问题：聊天/消息日志中文方框（无法修复）

**2026-07-13 记录**。聊天日志（ChatLog）的中文字符显示为 `_` / `__`，而非正确的中文。其他 UI 中文显示为 `□`（方框）。

**根因**：游戏使用了高度修改的 TextMeshPro，其 `FontEngine` 在运行时无法加载字体数据，且标准 SDF 图集格式与修改版 TMP 渲染管线不兼容。

**修改版 TMP 现状**：
- 原位于 `AIW2ModdingAndGUI/Assets/com.unity.textmeshpro@2.0.1/` 的修改版 TMP 源码**已丢失**（目录为空）
- `AIWar2_Data/Managed/Unity.TextMeshPro.dll` 比标准版小 16KB（372KB vs 389KB），`ReliableDLLStorage/` 有备份 DLL 但无源码
- 修改内容涉及自定义精灵支持和富文本标签变更（见 `ReliableDLLStorage/Unity.TextMeshPro Info.txt`）

**2026-07-13 尝试的方案**（使用 `AIW2ModdingAndGUI` 项目生成）：

| 方案 | 结果 |
|------|------|
| 嵌入游戏修改版 `Unity.TextMeshPro.dll` 生成 SDFAA Dynamic 字体 | 渲染全空白 |
| MSDFA（多通道 SDF，20,997 字形）| 渲染全空白 |
| RASTER 位图模式（非 SDF）| 渲染全空白 |
| RASTER + `Unlit/Texture` 着色器（绕过 TMP shader）| 渲染全空白 |
| BepInEx 插件：用 GDI+（System.Drawing）从 mi_sans.ttf 直接渲染字形，创建 TMP_FontAsset 绕过 FontEngine | 无效 |
| 标准 Unity TMP 生成的 AssetBundle | 格式与游戏修改版 TMP 不兼容，渲染全空白 |
| UnityPy 注入标准字体图集到 bundle | 8192x8192 图集格式不兼容，渲染全空白 |

之前尝试过的方案：

| 方案 | 结果 |
|------|------|
| I18NFont4UnityGame 自带 `mi_sans` bundle | TMP_FontAsset 图集为空（0 字形） |
| `TMP_FontAsset.CreateFontAsset(Font)` | 运行时 `FontEngine.LoadFontFace()` 失败 |
| 系统字体 `Microsoft YaHei` 等 | 同上，FontEngine 无法加载 |

**结论**：无源码无法修复 TMP 渲染层。但**聊天消息内容损坏**（中文变 `_`）的问题根源不在 TMP 渲染，而在**反序列化层**。

---

### 聊天中文 `_` 问题（序列化损坏，已根治）

**2026-07-18 最终方案**。聊天窗口（ChatLog）中文显示为 `_` 的完整链路已定位并**根治**。

#### 根因链路（2026-07-18 dnlib IL 分析确认）

```
输入框中文 → AddString_Condensed → InternalAdd → AddRaw → WriteBits_Char(c, fullUnicode:false)
  → GetCharIndexFromMapping(c)  ← 中文 U+4E00+ 超出 CharMapping 表范围（max U+2205）
  → 返回兜底索引 4 → 按 7bit 写入索引 4
  → ReadString_Condensed 读出索引 4 → SupportedCharList[4] == '_'
```

**关键发现**：数据在**写入存档那一刻就已损坏**，读端无法恢复。索引 4 是单向兜底，原文信息丢失。位宽硬编码 7bit，格式无自描述标记。

**勘误（2026-07-14 修正）：** `ReadString_Condensed`、`FillString_Condensed`、`ReadChar_Condensed` 在 `ArcenDeserializationBufferModern` 上均为**普通 IL 方法**（各有 700B/11B/149B IL 体），**不是 InternalCall**，Harmony 可直接 Patch。

#### 双轨方案：转义编码（序列化根治）+ Legacy Text 覆盖层（渲染兜底）

**序列化层**（`PatchCharMapping.cs`，2026-07-18 新增）：
- `AddString_Condensed` Prefix：写入前将表外字符编码为 `~uXXXX`
- `ReadString_Condensed` Postfix：读出后解码还原

**渲染层**（`WorldTMPFontPatchPlugin.cs`，2026-07-14 原有，保留作为 TMP 渲染兜底）：

| 组件 | 位置 | 作用 |
|------|------|------|
| **捕获** | `ArcenSerializationBuffer.AddString_Condensed` Prefix | 捕获所有 `FieldNameForErrors=="RelatedString"` 且含非 ASCII 的原文，存入线程安全列表 |
| **渲染恢复** | `TMP_Text.set_text` Postfix | 解析 ChatLog 文本，逐条 `IsContentMatch()` 匹配后替换 overlay text |
| **字体覆盖层** | `TextMeshProUGUI.OnEnable` Postfix | 在 ChatLog TMP 下创建子 `Text`（Legacy），`Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14)` 渲染中文 |

**关键实现细节**（当前插件 `WorldTMPFontPatchPlugin.cs`）:

1. **`IsContentMatch` 匹配逻辑**：长度相等 + 非 `_` 位置一一对应 + 任意 `_` 位置原文为非 ASCII（不要求 exact char match——实测 `——` → `??`）
2. **不消耗列表**：从末尾搜索最新匹配，每次 render 重新搜，无 `consumedCount` 偏移
3. **捕获过滤**：`!hasNonAscii`，不依赖 `<color>` 或 `：` 格式假设
4. **ChatLog 时间格式**：`<u>Xs</u>`（纯秒数如 `1s`、`13s`），不是 `HH:MM`。正则用 `[^<]+` 匹配
5. **Overlay**：TMP 子级（跟着 ScrollRect 滚动），`raycastTarget=false`，字号 `TMP.fontSize * 0.65`，左缩进 0，`lineSpacing=0.9664`
6. **Harmony 兼容**：只用 `Harmony.CreateAndPatchAll(typeof(WorldTMPFontPatchPlugin))`，不引用被 AssemblyRedirector 替换的程序集（ArcenAIW2Core）。引用 `ArcenUniversal.dll` 仅用于 `ArcenSerializationBuffer`——该类型不受 redirector 影响

**补丁列表**：`WorldTMPFontPatchPlugin.cs` 共 5 个 Harmony 补丁，覆盖 ChatLog 和 BasicText 两个 TMP 组件。`PatchCharMapping.cs` 额外提供 2 个序列化层补丁：

| 方法 | 类型 | 作用 |
|------|------|------|
| `ArcenSerializationBuffer.AddString_Condensed` | Prefix | 捕获序列化原文（`RelatedString` 含非 ASCII） |
| `ArcenSerializationBuffer.AddString_Condensed` | Prefix (Priority.Low) | **转义编码**表外字符（PatchCharMapping） |
| `ArcenDeserializationBufferModern.ReadString_Condensed` | Postfix | **解码**还原 `~uXXXX`（PatchCharMapping） |
| `Text.OnEnable` | Postfix | 替换所有 Legacy Text 字体为 Microsoft YaHei |
| `TextMeshProUGUI.OnEnable` | Postfix | 创建 ChatLog + BasicText 中文覆盖层 |
| `TMP_Text.set_text` | Postfix | 恢复中文文本到覆盖层（按组件名分发两种正则） |
| `TextMeshProUGUI.InternalUpdate` | Postfix | 后备字体替换（补 I18NFont4UnityGame 时序缺口） |

**调试日志**：`BepInEx/ChatLogRestore.txt`，每次启动覆写。记录 CAPTURE、FULL_TEXT、MATCH_OK/FAILED、FINAL_LEGACY_TEXT 和 overlay rect 位置。

**实测效果**：所有消息类型（JOURNAL/TIP/PLAYER_CHAT/WARDEN）均能正确恢复中文。内容匹配幂等，滚动条正常，鼠标不拦截。

#### 已知限制：Overlay 文字像素化

Legacy Text（位图渲染）放在有 CanvasScaler 的 Canvas 下时，Canvas 缩放会拉伸位图导致文字模糊/像素化。TMP（SDF 渲染）无此问题，但修改版 TMP 无法渲染中文。

**尝试过的方案**：
- 字号乘 0.65/0.8/1.0 均无效（像素化与字号大小无关）
- `Font.CreateDynamicFontFromOSFont` vs `new Font(path)` TTF 直接加载 → `new Font()` 在某些 Unity 版本不可用，导致崩溃
- 独立 Canvas 无 CanvasScaler（ScreenSpaceOverlay, sortingOrder+10）→ 渲染于屏幕原生分辨率，理论上文字最清晰，但多次实现均出现异常（不显示文字、Canvas 定位失效等问题）

**结论**：像素化属于 Legacy Text + CanvasScaler 的固有限制，目前不可彻底解决。TMP 可渲染中文前只能接受此折衷。

**可行方向（如果将来要修）**：用 `Graphics.DrawMesh` 或 `CommandBuffer` 绕过 Canvas 系统直接渲染文字到屏幕，但复杂度极高。

#### 已知限制：聊天链接点击区域偏移

ChatLog 条目中包含 `<link=N>` 标签用于可点击交互（如点击跳转日志条目）。overlay Legacy Text 不支持链接点击，所以点击事件由隐藏的 TMP 处理。但由于 overlay 中文文本（全角宽）和 TMP 文本（`_`，半角窄）的字符宽度不同，`<link>` 的 hitbox 位置按 TMP 的 `_` 宽度计算，与用户看到的中文位置存在偏移。用户需要点击中文稍左侧的位置才能触发链接。

**尝试过的方案**：
- overlay `raycastTarget=true` + 自实现链接点击 → Legacy Text 无原生支持，需完整的事件路由，复杂度高
- 源码修改 `GetTextToShowFromVolatile` 把正确中文传给 TMP（期望 TMP 缺字 fallback `□` 是全角，宽度匹配）→ TMP 对缺失字形实际显示 `_`（半角）而非 `□`，宽度不匹配问题未解决

**结论**：点击偏移属于可接受范围（聊天框链接点击不频繁），不做进一步修复。

#### 右上角瞬时消息框（BasicText）中文恢复（2026-07-15 修复）

**问题**：右上角动态消息框（`Window_OngoingMessageDisplay`）中走序列化管道的内容（`GameCommand.RelatedString`）中文显示为 `_`。

**调查发现**：
- 消息框有三类文字：`OnUpdate()` 硬编码中文（不走序列化，正常）、教程 XML（不走序列化，正常）、瞬时日志 `LocalMomentaryDisplayLog`（走序列化，损坏）
- 现有捕获层（`OnAddStringCondensed` Prefix）已经在抓所有 `RelatedString` 中文原文——只差恢复层
- 消息框的 TMP 组件名为 **`BasicText`**（不是 AssetBundle 中的 prefab 名 `BasicTextUnderlay`，Unity 实例化后真实名字是 `BasicText`）
- 瞬时日志的富文本格式是 `<link=N>content</link>`，与 ChatLog 的 `<link=N><u>HH:MM</u> content</link>` 不同

**修复**（`WorldTMPFontPatchPlugin.cs`）：
1. 新增 `NeedsChineseOverlay()` 辅助方法，同时匹配 `ChatLog` 和 `BasicText`
2. 覆盖层创建从 OnEnable 改为 `set_text` 时机按需创建（`EnsureOverlay()`），不依赖时序
3. `OnTMPTextSetText` 按组件名分发两种恢复器：
   - ChatLog → `RestoreChatLogText()`（正则 `<link=\d+><u>[^<]+</u>\s+(?<content>.*?)</link>`）
   - BasicText → `RestoreOngoingText()`（正则 `<link=\d+>(?<content>.*?)</link>`）
4. 两种恢复器共用同一套 `TryRestoreContent` + `IsContentMatch` 匹配引擎

**实测效果**：瞬时日志中文正确恢复（如星系视图中点击无法分配起始行星的提示）。Overlay 与 ChatLog 共享同样的像素化限制。

**踩坑记录**：
- 初版用 `StartsWith("BasicTextUnderlay")` 匹配，永远匹配不到——prefab 名和运行时 GameObject 名不同
- 为排查名字匹配问题，临时加了兜底路径（任何含 `_` 的组件都拦截），虽确认了 `BasicText` 名字，但也误伤了 `SubjectSummaryText`、`Cost Text` 等组件，已移除
- 编译缓存问题：`strings` 命令在 Git Bash 对 DLL 二进制不生效，需用 `grep -a` 验证 DLL 内容

#### 尝试过但失败的全部方案

| 方案 | 失败原因 |
|------|---------|
| 替换 `TMP_FontAsset` 为 mi_sans SDF | 图集为空（0 字形），显示 `_` |
| `TMP_FontAsset.CreateFontAsset()` 运行时创建 | `FontEngine.LoadFontFace()` 失败 |
| 系统字体（微软雅黑等）作为 TMP source font | FontEngine 无法加载 |
| `TryAddCharacters` 注入字形 | FontEngine 失败，返回 false |
| `Text.OnEnable` 替换所有 Legacy Text 字体 | I18NFont4UnityGame.FontPatch 冲突 NRE |
| 子对象 LegacyText 覆盖层（AddComponent<Text>） | I18NFont4UnityGame 的 Text.OnEnable 补丁 NRE |
| try-catch 包裹 AddComponent | Unity 在 AddComponent 内部吞异常，try-catch 接不到 |
| 同级对象 LegacyText（非子对象） | 定位问题，渲染时不在正确位置 |
| 禁用 TMP + 子 Text | TMP 禁用后游戏对 ReferenceText 的引用崩溃 |
| TMP alpha=0 + 子 Text | 位置对了但 TMP 透明渲染仍遮挡 |
| 子 Text 尺寸锚点拉伸 | 尺寸正确但 I18NFont4UnityGame NRE 阻止创建 |
| 反射绕过 AddComponent | 无法绕过 Unity 生命周期 |
| 序列化 CharMapping 加 CJK | WRITE 端有效（中文通过），READ 端原本是普通 IL 方法可 Patch，但数据在写入时已损坏，Patch 读端不解决根因 |
| `InternalAddString_CondensedToMatch` 捕获队列 | 队列被旧 set_text 过早消费，时序错乱 |
| `TMP_InputField.get_text` / `set_text` 捕获 | 捕获到战役名称等非聊天输入，且每按键触发多次 |
| `TextMeshProUGUI.OnEnable` 全局覆盖层 | 所有 UI 文字变双重叠，界面混乱 |
| Harmony Prefix return false 跳过 Text.OnEnable | 跳过原始方法导致 Text 组件初始化不完整 |
| Harmony Patch ReadString_Condensed | 原以为是 InternalCall 未尝试，实际是普通 IL 方法，理论上可 Patch |
| 顺序队列 + set_text 替换（旧方案） | consumedCount 永不重置，render 偏移；regex 只匹配 `<color>` 包裹的 `_`，遗漏其余 |

#### 核心难点

1. **修改版 TMP FontEngine 源码丢失** — C++ 原生层 FontEngine.LoadFontFace() 永远失败，无法创建运行时 TMP 字体
2. ~~反序列化是 C++ InternalCall~~ **已修正：反序列化方法是普通 IL 方法** — `ReadString_Condensed` / `FillString_Condensed` / `ReadChar_Condensed` 在 `ArcenDeserializationBufferModern` 上均有 IL 体，Harmony 可直接 Patch。但写入端 CharMapping 压缩数据时已将非 ASCII 字符损坏，仅 Patch 读端无法恢复已损数据
3. **I18NFont4UnityGame 冲突** — 该插件 patch `Text.OnEnable` 且内部 NRE，导致所有尝试 AddComponent<Text>() 的操作都崩溃
4. **输入框中文** — 输入框本身能显示中文（原因不明，可能 I18NFont4UnityGame 处理了 TMP_InputField），但提交后序列化损坏

#### 序列化层修复尝试

**目标**：从根源修复 `ReadString_Condensed` / `AddString_Condensed`，让中文字符能正确通过序列化管道，不依赖捕获+覆盖层。

**失败方案汇总：**

| 方案 | 做法 | 失败原因 |
|------|------|---------|
| 扩展 `SupportedCharList`（全量 21000 CJK） | 反射替换 `SupportedCharList`，调用 `InitializeStringHandling()` 重建 `CharMapping`+`CharUETypeData` | `GetUltraEfficientStyleThatFits(21000)` 返回的 BaseBits 风格与实际位宽不匹配，读写两端编码不一致 → 乱码 |
| 扩展 `SupportedCharList`（300 常用字） | 同上，仅加 300 字 | 索引 0-127（7位）工作正常；索引 128 起因 `BaseBits` 算法卡在 2^7 边界 → 溢出为 `?` |
| 扩展 `SupportedCharList`（257 字推过 256 边界） | 同上，加 148 字到总 257 | 过 128 边界后依然锁死，`GetUltraEfficientStyleThatFits` 的 BaseBits 选择算法根本不能处理变长的 Unicode 范围 |
| FullUnicode 旁路（标记位法） | 写端 1-bit 标记 + `WriteBits_InnerHelper16(c,16)` 原始 16-bit；读端 Transpiler 检查标记位 | 标记位消耗了位流，旧存档压缩数据首比特若为 1 则误入 FullUnicode 路径 → 崩溃；不兼容向后 |
| FullUnicode 全量替换（无标记位） | Prefix 强制 `WriteFullUnicode=true`，Prefix 全量替换 `ReadString_Condensed` | 旧存档所有字符串用压缩格式，新读端按原始 16-bit 解析 → 长度/字符完全错位 → 严重破坏存档兼容性（canary 校验失败） |
| 捕获+恢复修复 | 剥离富文本标签后再 `IsContentMatch`；移除 `Contains("_")` 门控 | 捕获系统本身不触发（ChatLog 格式与正则不匹配），恢复永远走不到 |

**根因**：`GetUltraEfficientStyleThatFits(int max)` 的 BaseBits 算法存在 2^n 边界锁死问题。超过 2^7=128 后无论加多少字符，BaseBits 不跟随增长到 8，导致索引 ≥128 的字符编码溢出为 0→`?`。该算法是为 ASCII 范围（109 字符，7 位）设计的，无法安全扩展到 Unicode 范围。

**结论**：不破坏旧存档的前提下，无法通过修改 CharMapping/位宽来支持中文。改用**转义编码**方案（见下方），对所有 condensed 字符串做表内无损编码，100% 兼容旧存档格式。

##### 成功方案：转义编码（Escape Codec）

**2026-07-18 实现**。在 `PatchCharMapping.cs` 中实现。

**原理**：不更改位格式，在表内字符集做无损编码（类似 UTF-7/quoted-printable）：

| 原文 | 编码后 | 说明 |
|------|--------|------|
| `~` | `~~` | 转义符自转义 |
| 任意表外字符（如 `中`） | `~u4E2D` | `~u` + 4 位十六进制 |

编码后字符全部在 109 字符表内 → 位流与旧格式 100% 一致。旧存档读取行为不变。

**Harmony 补丁**（`PatchCharMapping.cs`）：

| 方法 | 补丁类型 | 作用 |
|------|---------|------|
| `ArcenSerializationBuffer.AddString_Condensed` | Prefix (`Priority.Low`) | 写入前将表外字符编码为 `~uXXXX` |
| `ArcenDeserializationBufferModern.ReadString_Condensed` | Postfix | 读出后将 `~uXXXX` 解码回原文 |

**效果**：
- 对所有 condensed 字符串生效：行星名、聊天、行星备注、存档名等
- 旧存档中 2026-07-18 前已损坏的名字（已是 `_`）无法恢复
- Multiplayer 需要所有客户端都装汉化
- 行星名现在存读档后保持中文（配合 WorldTMPFontPatch 字体替换显示正常）

**与捕获覆盖层的关系**：编码方案修复了序列化管道（根源修复），但**聊天 TMP 渲染层**仍受修改版 TMP FontEngine 限制，`ChatLog`/`BasicText` 的中文覆盖层（capture → overlay）作为渲染兜底仍需保留。

### 部署

`deploy.ps1` 自动部署到 `BepInEx/plugins/ChineseTranslation/`。

## QuickStarts2 战役名称翻译

快速开始战役名称存在 `GameData/QuickStarts2/` 的 `.tooltip` 文件中，通过 `#showas:中文名` 指令显示中文名称。

**翻译方式**：编辑 `AIWar2_ChineseTranslation/GameData/QuickStarts2/` 下对应的 `.tooltip` 文件，修改或添加 `#showas:` 行。

**部署**：`deploy.ps1` 会自动复制这些文件覆盖游戏目录。

## QuickStarts2 重复战役排查记录 (2026-07-10)

**问题**：死灵法师 QuickStarts 出现重复战役。

**根因**：`PopulateQuickstartGroups()` 从 `GameData/QuickStarts2/` 和 `Expansions/<exp>/QuickStarts2/` 两处加载。死灵法师 QuickStarts 同时存在于：
- `GameData/QuickStarts2/4-Necromancer Intro/`（基础目录）
- `Expansions/3_The_Neinzul_Abyss/QuickStarts2/4-Necromancer Intro/`（扩展目录）

两目录有相同的 `_folder.tooltip`（`#showas:死灵法师入门`），被加入同一分组，导致同一组有两份目录、战役翻倍。

**修复**：删除了重复的 `GameData/QuickStarts2/4-Necromancer Intro/`（扩展已提供相同内容）。

## 中文行星名 (ChinesePlanetNames)

项目增加了一套写实恒星风行行星名，作为独立 XMLMod 部署。

### 文件结构

```
XMLMods/ChinesePlanetNames/
├── ModDetails.txt              ← Mod 元数据（默认启用）
├── ModDescription.txt          ← Mod 描述文本
├── PlanetNameType/
│   └── PlanetNameType.xml      ← 注册「中文行星」命名风格
└── PlanetNames/
    └── ChinesePlanets/
        └── Names.txt           ← 372 个真实恒星名（写实恒星风，无二十八宿）
```

### 使用方式

1. 用 `deploy.ps1` 部署到游戏目录
2. 进游戏 → Mod 菜单确认 "AI War 2 汉化项目: 中文行星名" 已启用
3. 开新局 → 大厅「地图」选项卡 → 「行星命名风格」下拉选择「中文行星」

### 数据来源

全部名称来自中国传统星官体系（三垣、星官附属星及恒星专名），每条对应一颗真实恒星或星官。共 372 个不重复名称。

### 翻译方式

编辑 `XMLMods/ChinesePlanetNames/PlanetNames/ChinesePlanets/Names.txt`，一条一行。编辑后运行 `deploy.ps1` 部署。

### 不纳入基线快照

行星名 `.txt` 文件不属于游戏源文件（类似 `.tooltip`），`check_update.ps1` 不追踪。游戏更新不会影响这些文件。

## 最近补译记录 (2026-07-10 第2批)

2026-07-10 补译了 9 个 C# 文件，修复 AIWarExternalCode 遗漏的 ~30 条英文字符串：

| 文件 | 遗漏内容 |
|------|---------|
| Window_InGameHoverEntityInfo.cs | "Resource Multipliers After Time..." 区块 12 处字符串未翻译（含 CLARIFICATION x3、Cannot be claimed x1） |
| Window_PrototypeInGameHoverEntityInfo.cs | "Cannot be claimed" x1 |
| EntityText.Attr.cs | "Cannot be claimed" x1 |
| AIPChange.cs | "At ... AIP changed" x1 |
| PublicCrashingNomadPlanetNotifier.cs | 撞击倒计时、行星移动提示 x2 |
| PublicAIReservesNotifier.cs | AI 预备队虫洞提示 x1 |
| PublicDZInvasionNotifier.cs | Dark Zenith 入侵提示 x1 |
| PublicImperialSpireNotifier.cs | 帝国尖塔到达提示 x1 |
| PublicArchitraveExpansionNotifier.cs | 天顶拱门扩张模式描述 x6 |

## 大文件并行翻译记录 (2026-07-11)

本次对两个最大的 UI 文件进行了全量补译，采用**子代理并行翻译**策略：

| 文件 | 行数 | 翻译方式 | 结果 |
|------|------|---------|------|
| Window_PrototypeInGameHoverEntityInfo.cs | 10,672 行 | 22 个子代理 × ~500 行/代理 | ✅ 全部完成 |
| Window_InGameHoverEntityInfo.cs | 9,184 行 | 19 个子代理 × ~500 行/代理 | ✅ 全部完成 |

### 翻译内容
- 武器系统标签（COIL-BEAM→线圈光束、ARMOR-PIERCING→穿甲、INSTA-KILL→秒杀 等 ~50 个标签）
- 状态/行为标签（Paused/Ready/Stand Down 等）
- DamageModifier 区块（DEFENSIVE BONUS→防御加成、ATTACK PENALTY→攻击惩罚 等）
- 完整描述段落（牵引光束/重力场/反隐形/AOE 等系统说明）
- 资源/统计标签

### 已知剩余问题
1. `" of "`（L373）：涉及英文语序，需改代码结构才能正确翻译
2. WorldTMPFontPatch.csproj：`BindingFlags` 编译错误（已有问题，需修复 csproj 引用）

## 最近补译记录 (2026-07-10 第3批)

2026-07-10 补译了 Window_ResourceBar.cs 中残留的 ~60 条英文字符串（含 build + deploy）：

| 区块 | 遗漏内容 |
|------|---------|
| tPlanetName | 右键途径说明 2 处 |
| tEncyclopedia | 百科工具提示全文 |
| tEnergy | 电压不足(Brownout)说明、消耗/生产/流入流出详情标签 8 处 |
| tFuelBase | 燃料名称 3 种、窗口标题、永久消耗说明、生产标签 8 处 |
| tHacking | AI 响应等级名称 x8、响应说明、入侵点统计、黑客计时 2 处 |
| tHacking (DZ) | 暗天顶经济提示全文 |
| tAIP | 当前等级/最高等级标签 |
| tNecromancerEsssence | 总计/Essence/科技来源标题 3 处 |
| tAttackSafe | 攻击按钮操作说明、性能统计/阵营/舰船容量窗口标题 |
| tGeneralTextMessage | 暂停/胜利/失败标签 |
| GetApkalluBreachHistory | 反击历史/伤害标题 |
| GetArmadaOverview | 活跃矿井/冷却/深度标题及标签 |
| GetApkalluOverview | 朝圣者/金属/访问标签及 Malware 状态 |
| GetFactionAllianceDetails / GetNPCShipCapDetails | 派系/结盟/敌对标签 |
| GetDysonSidekickIncome | 戴森球解锁条件提示 |
| 窗口标题 | Current Metal Flows、Dark Zenith Sidekick Income、Scourge State、Apkallu Overview、Armada Mining Overview、Necromancer Resource Acquisition、History of Hacks、History of Tech Unlocks、Current Energy Production and Consumption 共 9 处 |

## 构建注意事项 (2026-07-11 发现)

1. **清理编译缓存**：`build.ps1` 使用 MSBuild `Build` 目标（增量编译），有时不会检测源文件变更。修改翻译后务必先执行 `Remove-Item -Recurse -Force "DLLSource\AIWarExternalCode\obj\Release"` 再 `build.ps1`，否则旧 DLL 会被部署。
2. **禁止 C# 单引号字符串**：AGENTS.md 原规则 `禁止中文引号 ""，用 '' 替代` 有误——C# 中单引号 `'` 是字符字面量，不能包含多字符字符串。正确的做法是：**将中文引号 `""` 替换为 `"..."`（C# 双引号字符串），内部如需引用则用 `「」`**。
3. **之前翻译的多处文件**使用了单引号包裹中文，导致增量编译被缓存掩盖。清理后全量编译时会暴露这些语法错误，本次已修复全部 62 处。

【教训】翻译大型 UI 文件（如 Window_InGameHoverEntityInfo.cs 8390 行、Window_ResourceBar.cs 3413 行）时，必须遍历所有区块确保无遗漏。

修改 part 文件后需运行 merge 生成 merged.json：
```powershell
python tools/ilpatch/merge_all.py ArcenAIW2Core
# 如遇回归检查（pre-existing 格式不匹配的 key 导致退出），加 --force 恢复
python tools/ilpatch/merge_all.py ArcenAIW2Core --force
```

## IL 补丁验证

**ilpatch 条目丢失检测**（2026-07-18 新增）：ilpatch 写回 DLL 后会重新计算 ldstr 条目数，如果比原始条目数少则报警 `[patch] WARNING: dnlib writer dropped N ldstr entries!`。这是 dnlib 元数据写入器的已知 bug，会影响少量字符串的翻译生效。

Patch 后运行验证（覆盖全部 3 个核心 DLL）：
```powershell
python tools/ilpatch/verify_patch.py PatchedAssemblies\ArcenAIW2Core.dll tools\ilpatch\ArcenAIW2Core.merged.json
python tools/ilpatch/verify_patch.py PatchedAssemblies\ArcenUniversal.dll tools\ilpatch\ArcenUniversal.merged.json
python tools/ilpatch/verify_patch.py PatchedAssemblies\ArcenAIW2Visualization.dll tools\ilpatch\ArcenAIW2Visualization.merged.json
```

`deploy.ps1` 会自动运行上述 3 个验证，任一失败即报警。

验证原理：`ilpatch dump-ldstr <dll> <out.json>` 直读 #US 堆全部 ldstr 为 JSON 数组。`verify_patch.py` 加载后逐一检查 key/value 是否存在，0 盲区。

修改源码编译的 DLL 后必须运行 `build.ps1` 重新编译，`deploy` 部署（或手动复制 DLLBin/*.dll 到 GameData/ModdableLogicDLLs/）。修改 XML 后必须重新 `deploy` 或手动复制到对应目录。

patch 前必须从 `AIWar2_Data\Managed\` 复制原始 DLL（禁止在已 patch 的 DLL 上重复运行 `ilpatch patch`）。

## 翻译规则

1. **Edit 工具**逐字符串替换，禁止 Write 覆写整个文件
2. 只改 `"..."` 内文本，不碰引号外代码
3. 保留 `{变量}` 和 `<color>` 标签
4. 每翻译完一个**外部代码** DLL 编译验证，0 错误继续（核心 DLL 走 IL 汉化，不编译，用 `ilpatch inspect` 回读验证）
5. 禁止中文引号 `""` 出现在 XML 属性值或 C# 字符串中，用 `『』` 替代（`""` 在 XML 属性内会被误解析为属性分隔符，导致 XML 解析错误；`''` 在 C# 中不合法）
6. Debug 日志、内部标识符不翻译
7. 核心 DLL（IL 汉化）：用 `tools/ilpatch` 按 JSON 字典替换 `ldstr` 字面量，不碰任何代码结构（详见 SPEC 8.15）
8. **JSON 字典编码**：合并/重写 ilpatch 字典必须用**无 BOM 的 UTF-8**（PowerShell `Set-Content -Encoding UTF8` 会加 BOM，导致 ilpatch/Python 解析失败）。优先用 Python `open(path,'w',encoding='utf-8')` 或 .NET `UTF8Encoding(false)`
9. **大 DLL 并行翻译**：候选 >1000 条时按行切分为 `*.partN.json` 分片交多代理并行；合并时切忌直接拼接分片文件（子代理易破坏 JSON 结构），应重新 `extract` 干净骨架后用正则提取各分片 value 注入（详见 SPEC 8.15.9）

## 日志类 chat_text 补译记录 (2026-07-16)

2026-07-16 补全了 JournalEntries（日志类）中遗留的英文 `chat_text` 条目。经扫描，翻译 mod 下日志类 `chat_text` 共 588 条，其中 565 条已译，23 条为英文；排除 6 条 `CMP_Journal_TestDirectCodeEntries.xml` 测试占位条目（验证 `{FactionName}` 等占位符用，非真实文案，按惯例不译），实际有效未译 17 条，全部补译：

| 文件 | 补译条目数 | 内容 |
|------|----------|------|
| AstroTrains_Journal.xml | 1 | 星际列车防御进一步升级 |
| CMP_Journal_BaseGameDirectCodeEntries.xml | 4 | AI 被击败（3 种结局）、AI 内战已开始 |
| DarkSpire_Journal.xml | 4 | 暗塔已侦测到、暗塔已进入征服模式（×3） |
| Extragalactic_War_Journal.xml | 2 | 河外战争单位已释放、AI 霸主已相位转移正在逃离 |
| Lore_Journal.xml | 4 | 猎手舰队/人工智能预备队/守卫舰队/禁卫军 已侦测到 |
| Nanocaust_Journal.xml | 2 | 纳米浩劫入侵开始（友好/敌对 ×2） |

**结论**：日志类 `chat_text` 翻译全部完成（588 条，仅余 6 条测试占位条目未翻）。

**扫描方法**：PowerShell 遍历 `AIWar2_ChineseTranslation` 下含 `chat_text=` 的 XML，正则提取 `chat_text="..."`，判定含 `[A-Za-z]` 且不含 CJK 的为未译。

## 最近补译记录 (2026-07-16 第5批)

2026-07-16 补译了 C# 源码中 6 个文件的 ~18 条英文字符串，同时修正 IL patch：

| 文件 | 补译内容 |
|------|---------|
| `tools/ilpatch/ArcenAIW2Core.merged.json` | IL patch: Science→科技, Energy→能量（各 1 处 ldstr） |
| `ArcenExternalUIUtilities.cs` | 资源显示名全译（Metal→金属, Energy→能量, Science→科技, Hacking→入侵, Argon→氩, Radon→氡, Xenon→氙, Essence→精华, Cuendillar→库恩达, Strength→战力, Threat→威胁） |
| `Window_ModalSwapFleetMembers.cs` | Fleet→舰队, Swap Away→换入, Empty Slots→空槽位, Elite→精英, Ship Lines→舰线 |
| `Window_TechRefunds.cs` | Ok→确定, (Cost:→（花费： |
| `Window_InGameSidebarHacking.cs` | " on "→" - "（旗舰位置连接符）|
| `Window_DZLogisticsSidebarPopout.cs` | DZ Logistics→暗天顶后勤, Tier 1-4 阶分类, none queued→无队列, building→建造中, queued→已队列, inbound→运输中 |
| `Window_DZEconomySidebarPopout.cs` | none→无, nothing→无需求, ⬇ none here→⬇ 此处无, ⬇ Deposit ×→⬇ 存入 ×, Cost:→成本：, IDLE→空闲, Lock/Unlock→锁定/已锁定, Priority→普通/优先 |
| `Window_HackChoicesSidebarPopout.cs` | Bastion:→堡垒：, Overlord:→母星：, Hack:→入侵：（与已译的 NomadCrash 兄弟类一致）, Mark→等级 |
| `WaveUtils.cs` | CPA→跨星攻击, SOON→即将 |
| `Window_ModalFleetMemberModularEditing.cs` | Edit Loadout For:→编辑配置：, " on "→" - ", Mk→Mk（保留缩写）|
| `Window_ResourceBar.cs` | Argon Fuel→氩燃料, Radon Fuel→氡燃料, Xenon Fuel→氙燃料 |

### 已知剩余未翻译
1. `Window_PrototypeInGameHoverEntityInfo.cs:373` — `" of "` 涉及英文语序，需改代码结构才能正确翻译
2. WorldTMPFontPatch.csproj — `BindingFlags` 编译错误（需修复 csproj 引用）

## 最近翻译修订记录 (2026-07-16 第6批)

2026-07-16 修訂了一批艦隊/機動平台名稱翻譯，使其更貼近英文原意：

| 文件 | 舊值 | 新值 |
|------|------|------|
| `CMP_StartingFleetDesigns.xml` | 默认舰队 | **主力舰队** |
| `CMP_StartingSupportFleetDesigns.xml` | 战斗支援舰队 | **工兵支援舰队** |
| `CMP_StartingBattlestationDesigns.xml` | 多样防御站 | **复合防御阵列** |
| `ArnaudB_StartingBattlestationDesigns.xml` (Spire Rises) | 过度防御 | **火力碾压** |
| `ArnaudB_StartingBattlestationDesigns.xml` (Spire Rises) | 剥除防御 | **破甲打击** |
| `ZO_Battlestations.xml` (Zenith Onslaught) | 逆转防御 | **绝地崩雷网** |
| `tools/ilpatch/ArcenAIW2Core.merged.json` | ` Cmd` (未译) | **` 中枢`**（行星名+Cmd→如"天牢 中枢"） |

### 上批遗留機動平台名前綴修訂（同批次）

| 舊值 | 新值 |
|------|------|
| X型战斗站（display_name_prefix） | 去掉前綴，僅顯示"機動平台" |
| 引力型机动平台 | 重力型机动平台 |
| 捕获型机动平台 | 诱捕型机动平台 |

## 最近修訂記錄 (2026-07-16 第7批)

2026-07-16 修改行星名 MOD 标題：

| 文件 | 舊值 | 新值 |
|------|------|------|
| `XMLMods/ChinesePlanetNames/ModDescription.txt` | AI War 2 汉化项目：中文行星名 | **中文行星名** |
| `XMLMods/ChinesePlanetNames/ModDetails.txt` | AI War 2 汉化项目：中文行星名 | **中文行星名** |

## 最近修复记录 (2026-07-18)

### DarkZenith 终端崩溃 — C# DisplayName 匹配因汉化 XML display_name 变为中文而失败

**现象**：Dark Zenith 金属终端 HandleTerminiiAndEpistyles 抛异常崩溃，错误 "doesn't have a list. Resource Metal"。

**根因**：C# 代码（`DarkZenithResourceConversionTable.cs`）在筛选转换项时使用 `row.DisplayName` 做精确字符串匹配（如 `== "Build Harvester"`）。汉化将 XML 中 `display_name` 属性全部改为中文后，所有匹配失败，`ConversionList` 为空，最终导致 `HasList=true` 但列表为空的崩溃。

**原始 XML 注释曾有警告**：
```xml
<!-- Note that the C# uses both the display name and the internal name,
     so be wary about changing them for pre-existing units -->
```

**受影响的 C# 方法**：

| 方法 | 匹配内容 |
|------|---------|
| `AddMetalHarvesterBuildablesToList` | "Build Harvester"、"Build Epistyle"、"Build Transport" |
| `AddBaseBuildablesToList` | "Build Metal Terminus"、"Build White Terminus"、"Build Blue Terminus"、"Build Epistyle"、"Build Transport" |
| `AddConversionToListByName` | "Make Izumite"、"Make Thaumite"、"Make Alkahest"、"Make Skrith"、"Make Chelonium" |

**修复方案**：将所有 `row.DisplayName` 比较改为使用 `row.InternalName`（对应 XML `name` 属性，不受翻译影响）：

1. `AddMetalHarvesterBuildablesToList` / `AddBaseBuildablesToList`：`==` 精确匹配 → `InternalName.StartsWith()` / `==` 组合匹配
2. `AddConversionToListByName` → 重写为 `AddResourceConversionToList`，改用 `DZResource` 枚举参数 + `InternalName` 匹配
3. 5 处调用处同步更新

**教训**：**禁止翻译 `display_name` 属性**。C# 代码可能依赖 `DisplayName` 做逻辑匹配（不仅是 UI 显示）。修改 XML 前必须 grep C# 源码确认 `DisplayName` 的使用方式。更安全的做法是：若需匹配，用 `name`（InternalName）而非 `display_name`（DisplayName）。

### 加载界面条目回退英文问题（根因：IL patch 未部署）

**现象**：修改某些文件后，加载界面（loading screen）的条目回退为英文。

**根因链路**：
1. `deploy.ps1` 第154行存在多余的 `}`（PowerShell 括号匹配错误），导致整个脚本解析失败
2. 脚本失败 → IL patch DLL 未部署到 `PatchedAssemblies/`
3. `AssemblyRedirector` 加载不到 patch DLL → 回退原版英文 DLL
4. 加载界面文本（来自 `ArcenUniversal.dll` / `ArcenAIW2Core.dll` 的 ldstr）显示英文

**修复**：修正 deploy.ps1 语法错误，IL patch 重新部署，166 条翻译生效。

### 待改进项落实

本次会话落实了以下待改进项：

| 项目 | 修复内容 | 文件 |
|------|---------|------|
| `.new.dll` 残留清理 | `Get-ChildItem` 加 `-Recurse`，递归扫描 3 个目录及其子目录（含 `patched/`），避免子目录 `.new.dll` 漏清导致翻译回退 | `deploy.ps1` 第34行 |
| IL patch 过时条目 | 移除 5 个在当前 DLL 中已不存在的 missing 条目（游戏更新移除了这些文本），验证结果 8 applied, 0 pending, 0 missing | `tools/ilpatch/ArcenAIW2Visualization.merged.json` |
| 模组元数据翻译 | `<u>Xodians</u>` → `<u>克赛迪安</u>`（与 `SpecialFaction.xml` 中 `display_name` 一致） | `XMLMods/Xodians/ModDetails.txt` |
| 扩展翻译合并脚本重构 | 新增 `overlay_children()` 递归函数，支持 `map_option > choice_value` 等多层嵌套的翻译覆盖；匹配策略改为 (tag,name) → (tag,id) → tag 位置回退 | `merge_expansion_translations.py` |
| 地图类型描述翻译 | 蜂窝布局"死愿模式及以上不可用"翻译；EOTA 地图类型文案优化（"最小为N个行星"→"最小行星数为N个"）+ 格式统一 | `KDL_MapTypes.xml`、`EOTA_MapTypes.xml` |

### 教训

- **deploy.ps1 语法错误是静默杀手**：PowerShell 解析失败时不一定报明显错误，可能导致整个部署链路中断而翻译回退。修改 deploy.ps1 后必须验证括号匹配（`$openings = (Get-Content deploy.ps1 -Raw) -split '' | ? { $_ -eq '{' }; $closings = ...`）并实际运行一次确认无误
- **`.new.dll` 必须递归清理**：ilpatch 的 fallback 文件可能出现在 `patched/` 子目录，非递归扫描会漏掉，导致旧 patch 覆盖新 patch
- **IL patch 字典需跟随游戏更新**：游戏更新移除某些文本后，字典中对应条目变为 missing，verify_patch.py 会报警。定期清理过时条目保持字典与 DLL 同步
