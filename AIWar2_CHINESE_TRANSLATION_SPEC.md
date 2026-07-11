# AI War 2 汉化项目规范

**适配游戏版本: 5.825 (June 30th, 2026)**

## 一、技术方案

**XML 文件整体替换 + DLL 替换 (Preloader Patcher) + AssetBundle 拦截重定向**

| ❌ 已排除的方案 | 原因 |
|----------------|------|
| XMLMod (游戏原生覆盖机制) | DLL 覆盖机制有问题 |
| Harmony 运行时 Patch（DLL 替换场景） | 实测出现大量 BUG |
| AutoTranslator | 不能全部翻译 |

## 二、项目结构

```
AIWar2_ChineseTranslation/
├── winhttp.dll                        ← BepInEx 入口
├── doorstop_config.ini                ← BepInEx 配置
├── BepInEx/
│   ├── config/
│   │   ├── xiaoye97.I18NFont4UnityGame.cfg
│   │   └── BepInEx.cfg
│   ├── core/                          ← BepInEx 核心库
│   ├── patchers/
│   │   └── AssemblyRedirector.dll     ← Preloader Patcher
│   ├── plugins/
│   │   ├── I18NFont4UnityGame/
│   │   │   ├── I18NFont4UnityGame.dll ← 中文字体插件
│   │   │   ├── mi_sans               ← 小米字体（默认）
│   │   │   ├── sarasa_gothic          ← 更纱黑体
│   │   │   └── unifont                ← Unicode 字体
│   │   └── ChineseTranslation/
│   │       ├── ArcenUIAssetRedirect.dll ← AssetBundle 重定向插件
│   │       └── AssetBundles_Win/
│   │           └── arcenui            ← 汉化版 arcenui AssetBundle
├── GameData/Configuration/            ← 翻译后的 XML 文件
├── deploy.ps1                         ← 一键部署脚本（含版本检查）
├── check_update.ps1                   ← 更新检测脚本（替代 check_translation.ps1）
├── check_translation.ps1              ← 旧版检查脚本（已退役）
├── translation_snapshot.json          ← 基线快照（游戏英文原文快照）
├── AIWar2_CHINESE_TRANSLATION_SPEC.md ← 规范文档
├── AGENTS.md                          ← AI 助手上下文
└── translated_files.txt               ← 翻译记录（⚠ 已弃用，被 translation_snapshot.json 取代）
```

## 三、工作流程

### 3.1 翻译流程

1. 在 `AIWar2_ChineseTranslation/GameData/Configuration/` 中编辑 XML 文件
2. 运行 `deploy.ps1` 部署到游戏目录
3. 启动游戏验证翻译效果
4. 提交翻译到 Git 仓库

### 3.2 首次部署

1. 克隆仓库到本地
2. Steam → 验证游戏文件完整性（确保游戏为英文原版）
3. **运行 `check_update.ps1 -snapshot` 建立基线快照**（基于 SHA256 哈希 + 字符串提取的结构化快照，覆盖 XML/DLL 源码/核心 DLL/arcenui 四层）
4. 运行 `deploy.ps1` 将翻译文件部署到游戏目录

### 3.3 游戏更新后

`deploy.ps1` 现在包含部署前版本检查：**基线版本必须与游戏版本一致**才能部署。

**检测覆盖范围：** 快照系统监控四层来源——XML 配置 (`GameData/Configuration/`)、C# 源码 (`CodeExternal/`)、核心 DLL (`AIWar2_Data/Managed/`) 和 arcenui AssetBundle。

**分层对比策略：**

| 层 | 对比方式 | 说明 |
|---|---------|------|
| XML | 文件哈希 + 字符串级对比 | 用 `display_name`/`description` 等稳定属性名作 ID，精确定位改动 |
| C# 源码 (`CodeExternal/`) | **仅文件哈希** | 开源代码有翻译副本 `DLLSource/`，文件变了直接用 diff 工具看 |
| 核心 DLL | 文件哈希 | 无源码，IL 汉化需重新用 `ilpatch` 提取翻译 |
| arcenui | 文件哈希 + 字符串级对比 | 用英文原文作 key，精确定位改动 |

其中 `CodeExternal/` 对应翻译项目的 `DLLSource/`。`CodeExternal/` 变更时，快照只报告哪些 `.cs` 文件哈希变了，翻译者用 `git diff` 或其他 diff 工具对比 `CodeExternal/` 与 `DLLSource/` 即可看到精确差异。

1. Steam 更新游戏
2. **运行 `check_update.ps1`**（替代 `check_translation.ps1`）检测变更并生成报告
3. **运行 `check_update.ps1 -snapshot -force`** 从英文状态更新基线快照
4. 按报告重新翻译变更的 XML 和 DLL 源码文件
5. 运行 `build.ps1` 重新编译 DLL
6. 运行 `deploy.ps1` 重新部署
7. 更新规范中的版本号 → 提交并打 tag

## 四、技术原理

### 4.1 XML 文件整体替换

`GameData/Configuration/` 下的 XML 文件包含实体名称、描述、日志等文本内容。直接替换整个 XML 文件。

### 4.2 XML 文件翻译范围

**重要：并非所有 XML 文件都需要翻译。** 只有包含玩家可见文本的文件才需要翻译。

#### 需要翻译的文件类型

| 文件类型 | 说明 | 示例 |
|---------|------|------|
| `GameEntity/` | 实体名称、描述 | `KDL_Ships_FleetShips.xml` |
| `JournalEntries/` | 剧情日志 | `Lore_Journal.xml` |
| `Achievement/` | 成就名称和描述 | `KDL_Achievements.xml` |
| `Tips/` | 游戏提示 | `CMP_Tips_GettingStarted.xml` |
| `Tutorials/` | 教程文本 | `Tutorial 1 - Basic Planetary Controls.xml` |
| `SpecialFaction/` | 阵营名称和描述 | `KDL_VanillaEntries.xml` |
| `HackingType/` | 黑客类型描述 | `PlanetHacks.xml` |
| `ScourgeTypeData/` | 天灾战士描述 | `TSR_ScourgeTypeData.xml` |

#### 不需要翻译的文件类型（纯配置文件）

| 文件类型 | 说明 | 原因 |
|---------|------|------|
| `External*` | 外部接口配置 | 技术标识符，非玩家可见 |
| `Balance_*` | 数值平衡配置 | 纯数值，无文本 |
| `UIPrefab/` | UI 预制体路径 | 文件路径，非文本 |
| `UIWindow/` | 窗口配置 | 类名和路径，非文本 |
| `AIShipGroup/` | AI 舰队分组 | 编号配置，非文本 |
| `AIShipGroupCategory/` | AI 舰队分组类别 | 编号配置，非文本 |
| `CameraType/` | 相机类型 | 技术配置 |
| `FramerateType/` | 帧率类型 | 数值配置 |
| `ParticlePattern/` | 粒子效果路径 | 文件路径 |
| `SpaceboxDefinition/` | 天空盒定义 | 文件路径 |
| `PlanetDefinition/` | 星球定义 | 数值配置 |
| `TextEmbededSprites/` | 文本嵌入精灵 | 图标配置 |
| `TextStyles/` | 文本样式 | 样式配置 |
| `TextVarMaps/` | 文本变量映射 | 变量配置。注意：`TextVarMaps_Vanilla.xml` 包含快速开始、始祖格式等玩家可见文本，属于例外需翻译 |
| `SurrogateTable/` | 代理表 | 编号配置 |
| `SpecialFactionProcessingGroup/` | 特殊阵营处理组 | 编号配置 |

#### 如何判断文件是否需要翻译

1. **检查 Description 字段**：如果文件包含 `Description="..."` 且内容非空，则需要翻译
2. **检查 Name 字段**：如果 Name 字段是英文句子（如 "Burlust warriors glory in combat"），则需要翻译；如果是技术标识符（如 "PlanetExplosion"、"Window_MainMenu"），则不需要翻译
3. **检查文件类型**：参考上述列表，External*、Balance_* 等类型通常不需要翻译

#### 当前翻译状态

| 类别 | 文件数 | 已翻译 | 需要翻译 | 状态 |
|------|--------|--------|----------|------|
| 基础游戏 XML | 398 | 297 | 0 | ✅ 完成 |
| DLC1 XML | 64 | 30 | 0 | ✅ 完成 |
| DLC2 XML | 69 | 35 | 0 | ✅ 完成 |
| DLC3 XML | 90 | 60 | 0 | ✅ 完成 |
| XMLMods | 5 | 5 | 0 | ✅ 完成 |
| **合计** | **626** | **427** | **0** | **✅ 完成** |

### 4.2 DLL 替换 (Preloader Patcher)

BepInEx Preloader 在游戏程序集加载前调用 Patcher，通过 Mono.Cecil 读取并替换 `PatchedAssemblies/` 中的 DLL 文件。

### 4.3 AssetBundle 拦截重定向

**arcenui AssetBundle**（`AssetBundles_Win/arcenui`）是 Unity 资源包，包含大量 UI 预制体中的英文文本（`m_text:` 字段）。这类资源无法通过 XML 或 DLL 替换修改。

解决方案：使用 BepInEx Harmony 插件在运行时拦截 `ArcenAssetBundleManager.LoadOrRetrieveBundle()` 方法。当请求 `arcenui` 时，改为加载 `BepInEx/plugins/ChineseTranslation/AssetBundles_Win/arcenui`，实现 UI 文本替换。

**为什么这个 Harmony Patch 可行，而之前 Harmony 方案被排除？**

| | 之前失败的尝试 | 本次方案 |
|--|--------------|---------|
| 范围 | Patch 游戏逻辑 DLL 中的众多方法 | Patch 单个私有方法，只拦截文件名 |
| 风险 | 大量竞争条件和逻辑冲突 | 无状态，要么加载自定义文件，要么回退 |
| 影响 | 多处覆盖导致 BUG | 极有限，只影响 AssetBundle 加载路径 |

**流程：**
1. 使用 Unity 工具（如 AssetStudio）从原始 `arcenui` 提取预制体
2. 替换预制体中的英文 `m_text:` 为中文字符串
3. 重新打包为 AssetBundle，放到 `BepInEx/plugins/ChineseTranslation/AssetBundles_Win/arcenui`
4. 游戏加载时，ArcenUIAssetRedirect 插件自动拦截并加载汉化版

## 五、游戏 DLL 加载链路

| 加载顺序 | DLL 文件 | 说明 |
|----------|---------|------|
| 1 | `ArcenAIW2Core.dll` | 游戏主逻辑、实体、阵营、舰队、科技等 |
| 2 | `ArcenUniversal.dll` | UI 组件、通用工具、字体、输入、网络等 |
| 3 | `ArcenAIW2Visualization.dll` | 渲染、特效、模型、Shader 等 |
| 4 | `AIWarExternalCode.dll` | Mod 可扩展的游戏逻辑 |
| 5 | 扩展包/Mod 程序集 | 如 SpireRises、ZenithOnslaught 等 |

## 六、禁止事项

- 禁止修改 `AIWar2_Data/Managed/` 下任何原始 DLL
- 禁止使用 Harmony 运行时 patch 游戏逻辑 DLL（ArcenAIW2Core 等）中的方法（曾出现大量 BUG）
- Harmony 仅限用于 AssetBundle 拦截重定向（`ArcenUIAssetRedirect` 项目），不涉及游戏逻辑
- 禁止使用 XMLMod 的 DLL 覆盖机制

## 七、卸载方法

删除 `AIWar2_ChineseTranslation/` 文件夹，然后删除游戏目录中部署的文件：
- `BepInEx/patchers/AssemblyRedirector.dll`
- `BepInEx/plugins/I18NFont4UnityGame/`
- `GameData/Configuration/` 下已替换的 XML 文件

---

## 八、DLL 源码汉化

### 8.1 概述

七个 DLL 项目分为三组：

**第一组：有源码的外部代码项目（已完成汉化）**
三个外部 DLL 项目有完整源码（位于 `CodeExternal/`），已全部汉化并编译。

**第二组：核心 DLL 的 IL 汉化（进行中）**
三个核心 DLL（ArcenUniversal、ArcenAIW2Core、ArcenAIW2Visualization）用 `ilpatch` 工具（dnlib）直接替换 `ldstr` 字符串字面量（见 8.15）。

**第三组：BepInEx 插件项目（新增）**
一个 Harmony 插件（ArcenUIAssetRedirect），用于拦截 AssetBundle 加载，实现 UI 文本替换。

### 8.2 已汉化的 DLL 项目

| 项目 | 源码位置 | 汉化内容 | 编译状态 | 翻译状态 |
|------|---------|---------|---------|---------|
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/ | 全量汉化 (~655 条字符串，涵盖 EntityText/、UIs/、Scenarios/、Hacking/、BaseInfo/、Helpers/ 等 ~30 个文件) | ✅ 0 错误 | ✅ 已完成 |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | 聊天消息、少量 UI 文本 | ✅ 0 错误 | ✅ 已完成 |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | 银河地图显示模式文本 | ✅ 0 错误 | ✅ 已完成 |
| ArcenUIAssetRedirect | DLLSource/ArcenUIAssetRedirect/src/ | arcenui AssetBundle 拦截重定向 | ✅ 0 错误 | ✅ 已完成 |
| ArcenUniversal (IL 汉化) | —（无源码，见 8.15） | UI 组件、通用工具、输入、网络等 | — | ⏳ 待翻译 |
| ArcenAIW2Core (IL 汉化) | —（无源码，见 8.15） | 游戏主逻辑、实体、阵营、舰队、科技等 | — | 🔶 进行中 |
| ArcenAIW2Visualization (IL 汉化) | —（无源码，见 8.15） | 渲染、特效、模型、Shader 等 | — | ✅ 已完成 |

### 8.3 目录结构

```
AIWar2_ChineseTranslation/
├── DLLSource/                                    ← 汉化源码
│   ├── AIWarExternalCode/                        ← 有源码的外部项目
│   │   ├── src/
│   │   └── AIWarExternalCode.csproj
│   ├── AIWarExternalDeepProcessingCode/
│   │   ├── src/
│   │   └── AIWarExternalDeepProcessingCode.csproj
│   ├── AIWarExternalVisualizationCode/
│   │   ├── src/
│   │   └── AIWarExternalVisualizationCode.csproj
│   ├── ArcenUIAssetRedirect/                     ← BepInEx 插件
│   │   ├── src/
│   │   │   └── ArcenUIRedirectPlugin.cs           ← Harmony 拦截逻辑
│   │   └── ArcenUIAssetRedirect.csproj
│   └── （核心 DLL 无源码，不经 DLLSource；其 IL 汉化见 8.15，产物在 PatchedAssemblies/）
├── DLLBin/                                       ← 编译产物（外部代码项目）
│   ├── AIWarExternalCode.dll                     (3778 KB)
│   ├── AIWarExternalDeepProcessingCode.dll        (1762 KB)
│   ├── AIWarExternalVisualizationCode.dll         (228 KB)
│   └── ArcenUIAssetRedirect.dll                  (6 KB)
├── BepInEx/
├── GameData/
├── build.ps1                                     ← DLL 编译脚本
├── deploy.ps1                                    ← 部署脚本
├── translated_files.txt
├── AGENTS.md
└── AIWar2_CHINESE_TRANSLATION_SPEC.md
```

### 8.4 编译环境

| 组件 | 版本 | 说明 |
|------|------|------|
| Roslyn 编译器 | 4.12.0 | `C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472\csc.exe` |
| C# 语言版本 | 13.0 | 与原版编译器一致 |
| MSBuild | 4.8.9037.0 | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe` |
| 目标框架 | .NET Framework 4.7.2 | 需安装 targeting pack 或使用引用程序集 |

**重要规则：**

1. **编译器版本必须与原版一致**（Roslyn 4.12.0 / C# 13.0），低版本编译器生成的 IL 代码会导致运行时错误（如对象池异常）。

### 8.5 csproj 格式规范

所有有源码的 DLL 项目使用旧格式（非 SDK 风格），`ToolsVersion="14.0"`，与官方 `CodeExternal` 一致。

### 8.6 （无内容，节号保留）

### 8.7 编译流程

```powershell
# 一键编译（推荐）
.\build.ps1

# 或手动编译
$roslynDir = "C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

# 第一阶段：外部代码（无依赖关系）
& $msbuild DLLSource\AIWarExternalCode\AIWarExternalCode.csproj /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir"
& $msbuild DLLSource\AIWarExternalDeepProcessingCode\AIWarExternalDeepProcessingCode.csproj /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir"
& $msbuild DLLSource\AIWarExternalVisualizationCode\AIWarExternalVisualizationCode.csproj /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir"

# 注：核心 DLL 走 ilpatch IL 字面量替换（8.15），无此 msbuild 步骤。
```

### 8.8 部署

`deploy.ps1` 已集成所有部署步骤：
- XML 翻译文件 → `GameData/Configuration/`
- DLL 编译产物 → `GameData/ModdableLogicDLLs/`
- BepInEx 插件 (ArcenUIAssetRedirect) → `BepInEx/plugins/ChineseTranslation/`
- 汉化 AssetBundle → `BepInEx/plugins/ChineseTranslation/AssetBundles_Win/arcenui`（**不纳入仓库**：约 220MB，超 GitHub 单文件 100MB 限制。改由 `patch_arcenui.py` 据仓库内 `arcenui_translations.json` + 游戏原文 bundle 本地生成；deploy.ps1 在缺失时提示运行该脚本）
- **核心 DLL IL 汉化版**（见 8.15）→ 游戏 `PatchedAssemblies/`

运行 `.\deploy.ps1` 即可部署所有翻译文件（含 arcenui UI 文本与核心 DLL 的 IL 汉化），**低级用户一键部署即可获得完整中文**。

#### 核心 DLL 的仓库管理与部署

核心 DLL（ArcenAIW2Core / ArcenAIW2Visualization）的 IL 汉化产物**已纳入本仓库**（`PatchedAssemblies/` 目录，与游戏目录同名；`.bak` 备份由 `.gitignore` 排除）。这样高级用户可查看/复用汉化成果，低级用户经 `deploy.ps1` 直接获得。

`deploy.ps1` 的部署逻辑（2026-07-08 修正）：
- **Core / Visualization**：优先使用仓库内 `PatchedAssemblies/ArcenAIW2Core.dll` / `ArcenAIW2Visualization.dll`（IL 汉化版）；若仓库尚无对应汉化版，则回退拷贝游戏原版。
- **Universal**：目前无 IL 汉化，始终从游戏 `AIWar2_Data\Managed\` 拷贝原版。

**重要**：原脚本曾无条件从 `AIWar2_Data\Managed\` 拷贝原版覆盖 `PatchedAssemblies/`，会冲掉 ilpatch 写入的中文，已修复。切勿改回该逻辑。

译者本地经 `ilpatch` 修改后，应把更新后的核心 DLL 提交进仓库 `PatchedAssemblies/`（而非仅留本地游戏目录），以保证一键部署包含最新汉化。

### 8.9 （无内容，节号保留）

### 8.10 依赖关系与编译顺序

```
AIWarExternalCode (基础项目)
  └─ 依赖: ArcenUniversal, ArcenAIW2Core, ArcenAIW2ThirdParty, ArcenAIW2Visualization

AIWarExternalVisualizationCode
  └─ 依赖: ArcenUniversal, ArcenAIW2Core, ArcenAIW2Visualization, ArcenAIW2ThirdParty
  └─ 依赖: AIWarExternalCode (编译产物)

AIWarExternalDeepProcessingCode
  └─ 依赖: ArcenUniversal, ArcenAIW2Core
  └─ 依赖: AIWarExternalCode (编译产物)
```

ArcenUIAssetRedirect (BepInEx 插件)
  └─ 依赖: ArcenUniversal, BepInEx, 0Harmony, UnityEngine

**编译顺序：** AIWarExternalCode → AIWarExternalDeepProcessingCode + AIWarExternalVisualizationCode → ArcenUIAssetRedirect（可独立编译）

### 8.11 翻译优先级

| 优先级 | 目录 | 内容 | 状态 |
|--------|------|------|:----:|
| 高 | `src/UIs/` | 主菜单、设置、存档、侧边栏等 UI 文本 | ✅ 完成 |
| 中 | `src/EntityText/` | 实体文本格式化（属性、描述、统计） | ✅ 完成 |
| 中 | `src/Scenarios/` | 教程文本 | ✅ 完成 |
| 中 | `src/Hacking/` | 黑客描述（15 个文件） | ✅ 完成 |
| 低 | `src/BaseInfo/`, `src/Helpers/`, `src/CoreInterfaces/` | 舰队统计、界面辅助、自定义系统 | ✅ 完成 |
| 低 | `src/Orders/`, `src/Input/` | 命令、快捷键相关 | ✅ 完成 |

### 8.12 已知限制

- 翻译时只能替换字符串字面量，不能修改代码逻辑
- 编译器版本必须与原版一致（Roslyn 4.12.0），否则会产生运行时错误
- Debug 日志、内部标识符、错误码（如 `Immune to All Damage`、`CODE `、`PrimaryKeyID `）保持英文，不翻译

### 8.13 汉化统计

| 项目 | 类型 | 文件数 | 编译状态 | 翻译状态 |
|------|------|--------|---------|---------|
| AIWarExternalCode | 有源码 | 600 | ✅ 0 错误 | ✅ 完成（全量 ~655 条字符串） |
| AIWarExternalDeepProcessingCode | 有源码 | 133 | ✅ 0 错误 | ✅ 完成 |
| AIWarExternalVisualizationCode | 有源码 | 45 | ✅ 0 错误 | ✅ 完成 |
| ArcenUIAssetRedirect | BepInEx 插件 | 1 | ✅ 0 错误 | ✅ 完成 |
| ArcenUniversal | IL 汉化 | 613 | — | ✅ 已部署（151 条 ldstr） |
| ArcenAIW2Core | IL 汉化 | ~350 | — | ✅ 已部署（637 条 ldstr） |
| ArcenAIW2Visualization | IL 汉化 | ~100 | — | ✅ 已完成（20 条 ldstr） |
| **合计** | | **~1841** | | |

### 8.14 翻译注意事项

1. **使用 Edit 工具**：翻译时必须使用 Edit 工具逐字符串替换，禁止使用 Write 覆写整个文件
2. **只改引号内内容**：只能替换 `"..."` 内的文本，不能修改引号外的任何代码
3. **保留插值和标签**：`$"{variable}"` 中的变量部分保持不变，`<color>` 等标签保持不变
4. **（外部代码项目）编译验证**：翻译有源码的外部代码项目时，每翻译完一个文件后编译验证，0 错误再继续下一个。核心 DLL 走 IL 汉化（8.15），不编译，用 `ilpatch inspect` 回读验证即可。
5. **翻译优先级**：UI 文件（src/UIs/）优先，游戏逻辑文件（BaseInfo/、Sim/ 等）通常不需要翻译。但 BaseInfo/ 下的 Notifier 通报文本（如 AI Reserves、Crashing Nomad、Architrave Expansion 等）是玩家可见提示，必须翻译
6. **检查遗漏**：翻译大型 UI 文件（如 Window_InGameHoverEntityInfo.cs 8390 行）时，需确保不遗漏任何包含玩家可见文本的区块。2026-07-10 曾发现 "Resource Multipliers After Time Being Here And Not Crippled" 区块 11 处英文字符串被遗漏
7. **禁止中文引号**：C# 字符串中不能使用 `""`（中文左右双引号），会被编译器误认为字符串分隔符。**不要使用单引号 `''`** —— C# 中 `'...'` 是字符字面量，只允许单个字符。正确做法是保持 C# 双引号 `"..."`，如需在字符串内引用则用 `「」`。例如：`"点击「是」确认"`。
8. **引号嵌套**：如果翻译文本中需要引用按钮名称或其他 UI 元素，使用 `「」` 包裹，不要使用 `""` 或 `''`

### 8.15 核心 DLL 的 IL 级汉化（dnlib 工具方案）

**背景：** 过去尝试反编译为 C# 项目再编译替换的方式出现了大量运行时 BUG（因反编译-重编译过程改变了 IL 结构，导致对象池异常、类型初始化错误等），故改用 IL 字面量替换。

**关键约束（历史坑）：** 不可用手写/脚本方式构造外部类型引用 —— 一旦 `ldstr` 之外的指令需要引用外部类型，必须用 `module.Import()` 从磁盘真实 DLL 导入（dnlib 曾把 `System.Text.Encodings.Web` 的 `PublicKeyToken` 误写为 `c5cd5de6caeeedcf`，正确值应为 `cc7b13ffcd2ddd51`，错误会导致 CLR 解析程序集失败）。本方案只改 `ldstr` 字面量，不引入外部类型，故不触发该风险。

#### 8.15.1 环境要求

- .NET 8 SDK（`dotnet build` 编译 `ilpatch`）
- `dnlib`（引用 `dnSpy\...\net48\dnlib.dll`）
- 目标 DLL：`PatchedAssemblies\ArcenAIW2Core.dll` / `ArcenAIW2Visualization.dll`（BepInEx `AssemblyRedirector` 在加载前读取此目录，故此处即游戏实际加载版本）
- **重要：** 修改前确认目标 DLL 未被占用（dnSpy 加载中 / 游戏运行中 / 文件监视器）。否则工具会降级输出 `.new.dll`，需手动 `Copy-Item -Force` 覆盖。

#### 8.15.2 工具 `ilpatch` 三种模式

| 命令 | 用途 |
|------|------|
| `ilpatch inspect <dll> [contains]` | 列出 DLL 中全部唯一 `ldstr`（可按子串过滤），用于探查 |
| `ilpatch extract <dll> <out.json>` | 提取全部"疑似可翻译"的 `ldstr` 为 `{"原文":""}` 骨架字典 |
| `ilpatch patch <dll> <dict.json> [--dry]` | 按字典逐条替换 `ldstr` 操作数，原位写回（写前自动备份 `.bak`） |

`patch` 行为细节：
- 命中字典且值非空 → `instr.Operand = zh`（赋值 .NET `string`，dnlib 自动处理 `#US` 堆，**必须赋 `string` 而非 `new UTF8String(...)`，否则 writer 报 `Invalid instruction operand`**）。
- 未命中或值为空 → 跳过（保留英文）。
- 写回用 `File.Copy` 覆盖（非 `File.Move`，避开文件监视器对替换删除的独占锁）；若目标仍被锁，降级写 `<name>.new.dll` 并提示手动复制。
- 写后做回读（`ModuleDefMD.Load`）校验 DLL 可重新加载。

#### 8.15.3 翻译字典格式

JSON，`{ "English text": "中文", ... }`：
- key 为 DLL 中**完整** `ldstr` 原文（含前导/尾随空格、`\n`、`<size>` 等标签，必须与 DLL 内逐字节一致才能匹配）。
- value 为中文；保留原文中的 `{0}`、`{1}`、`<color>`、`<size=70%>`、`\n` 等格式符与标签。
- 译文字符串中**禁止中文引号 `""`**，需引用时用 `'单引号'`（规范 8.14）。

#### 8.15.4 汉化工作流

```
┌─────────────────────────────────────────────────────────────┐
│ 1. ilpatch extract <dll> <skeleton.json>                     │
│    → 导出疑似可翻译字符串骨架                                  │
├─────────────────────────────────────────────────────────────┤
│ 2. 人工/LLM 筛选真正"玩家可见"文本（剔除事件标识符、         │
│    调试日志、资源校验报错、同步错误描述），填入中文 value     │
├─────────────────────────────────────────────────────────────┤
│ 3. ilpatch patch <dll> <dict.json> --dry                     │
│    → 确认 will replace 条数无误                               │
├─────────────────────────────────────────────────────────────┤
│ 4. ilpatch patch <dll> <dict.json>                           │
│    → 自动 .bak 备份，原位写回中文                             │
├─────────────────────────────────────────────────────────────┤
│ 5. 启动游戏验证；若 DLL 被锁导致降级 .new.dll，             │
│    关闭占用进程后 Copy-Item -Force 覆盖                       │
└─────────────────────────────────────────────────────────────┘
```

> **大型 DLL 并行翻译**：候选条目 >1000 时，按 8.15.9 的分片+合并流程用多代理并行，避免在单个长上下文里逐条处理。

#### 8.15.5 操作示例

以 `ArcenAIW2Visualization.dll` 为例，把行星选择提示汉化：

**Step 1 - 提取骨架：**
```
ilpatch extract "PatchedAssemblies\ArcenAIW2Visualization.dll" viz_skeleton.json
```

**Step 2 - 筛选并填中文（dict.json 节选）：**
```json
{
  "\nStarting Planet For: ": "\n起始行星归属：",
  "(extremely hard to defend)": "（极难防守）",
  "(a bit harder to defend)": "（稍难防守）",
  "\nClicking on this planet will make it the starting planet for ": "\n点击此行星将把它设为以下阵营的起始行星：",
  "Nomad planet '{0}' has moved.": "游牧行星 '{0}' 已移动。"
}
```

**Step 3 - 试运行确认匹配数：**
```
ilpatch patch "PatchedAssemblies\ArcenAIW2Visualization.dll" dict.json --dry
# [dry] ... would replace 33, skip 449
```

**Step 4 - 正式写入（自动 .bak）：**
```
ilpatch patch "PatchedAssemblies\ArcenAIW2Visualization.dll" dict.json
```

**Step 5 - 验证（字节级确认 UTF-16LE 中文、无转义）：**
```
ilpatch inspect "PatchedAssemblies\ArcenAIW2Visualization.dll" "起始行星归属"
# 起始行星归属：
```

#### 8.15.6 修改规则

1. **只改 `ldstr` 指令**的字符串操作数，不改其他任何指令、不引入外部类型
2. 保留方法原有 IL 结构（条件分支、方法调用、局部变量等）
3. 保留格式标签（`<color>`、`<size=70%>`、`<b>` 等）与插值变量（`{0}`、`{1}`、`{Count}`）
4. Debug 日志、内部标识符、同步错误描述（如 `Client thinks squad with pkid ...`）、资源加载校验报错**不翻译**
5. 事件/警报标识符（如 `ArkChiefOfStaff_HomeCommandStationUnderAttack`、`WormholeTransit`）是代码查找 key，**严禁翻译**（会破坏查找逻辑）
6. 译文字符串禁止中文引号 `""`，用 `'单引号'`
7. 字典 key 必须与 DLL 内原文逐字节一致（含空格/换行/标签），否则不匹配

#### 8.15.7 与 C# 源码汉化的区别

| 对比项 | 外部代码 (AIWarExternalCode 等) | 核心 DLL (ArcenUniversal 等) |
|--------|-------------------------------|------------------------------|
| 修改方式 | 直接在 C# 源码中翻译字符串 | 用 `ilpatch` 改 IL 中的 `ldstr` 操作数 |
| 编译 | 需要 Roslyn 重新编译整个项目 | 不需要编译，直接写回 DLL |
| 风险 | 低（修改源码后编译，结构不变） | 低（仅改字符串常量，结构不变） |
| 工具 | VS Code / Edit 工具 | `ilpatch`（dnlib 控制台） |
| 中文编码 | 源码 UTF-8，正常 | `#US` 堆 UTF-16LE，无转义（已验证） |
| 修改后 | 提交汉化 DLL 进仓库 `PatchedAssemblies/` → `deploy.ps1` 一键部署 | 提交进仓库 `PatchedAssemblies/` 对应 DLL，`deploy.ps1` 自动部署到游戏目录 |

#### 8.15.8 常见问题

- **writer 报 `Invalid instruction operand`**：`ldstr` 操作数必须赋 `.NET string`（`instr.Operand = zh`），不能赋 `new UTF8String(zh)`。
- **patch 报 `target locked`，生成 `.new.dll`**：目标 DLL 被 dnSpy/游戏/文件监视器占用。关闭占用进程后 `Copy-Item -Force` 覆盖原 DLL 即可（不要用 `Move`，替换删除会被锁）。
- **中文显示乱码**：确认未误用 `new UTF8String(...)`；正确方式下中文以 UTF-16LE 存于 `#US` 堆，CLR 原生支持。
- **部分字符串没翻到**：字典 key 与 DLL 内原文不一致（多/少空格、`\n`、标签差异）。用 `ilpatch inspect <dll> <子串>` 比对真实原文。
- **游戏崩溃**：检查是否误翻了事件标识符或改动了非 `ldstr` 指令。
- **部署位置**：核心 DLL 经 `ilpatch` 改完后，提交进仓库 `PatchedAssemblies/`（`.bak` 不入库），由 `deploy.ps1` 在部署时优先拷贝到游戏 `PatchedAssemblies/`，`AssemblyRedirector` 在加载前读取。无需手动放置（见 8.8）。

#### 8.15.9 并行翻译（多代理分片）经验

大型核心 DLL（如 `ArcenAIW2Core` 有 2347 条候选）用多个 LLM 子代理并行翻译能显著提速，但分片与合并有固定坑，已实测踩过：

**分片方式（推荐按行切，不要按语义切）：**
- 用 `ilpatch extract` 导出完整骨架后，按行数均分为 N 个分片文件（`ArcenAIW2Core.part1.json` … `partN.json`）。
- **分片文件本身不含首尾 `{}`**（只是条目行），不能直接 `json.load`，只能作为"待翻译片段"交给子代理用 Edit 逐条填 value。
- 每个子代理只改 `"key": ""` 里的 value，禁止增删条目、禁止改 key。

**合并与校验（关键，曾因此翻车）：**
1. **禁止用 PowerShell `Set-Content -Encoding UTF8` 写 JSON**：它会加 UTF-8 **BOM**（EF BB BF），`ilpatch`（System.Text.Json）和 Python `json` 都无法解析，报 `'"' is invalid after a value` 等诡异错误。合并/重写 JSON 时必须用 **无 BOM 的 UTF-8**（如 Python `open(path,'w',encoding='utf-8')`，或 .NET `UTF8Encoding(false)`）。
2. **子代理编辑易破坏 JSON 结构**（缺逗号、行粘连成"Extra data: line 1"、value 结束引号丢失导致后续整段错位）。合并后务必先用 Python 严格 `json.loads` 校验；若报错，错误信息（行号/列）往往滞后于真实破损点。
3. **稳健的合并/修复脚本**（已验证可行）：不要直接拼接分片文件。而是——
   - 重新从 DLL `extract` 一份**干净的完整骨架**（保证 key 集合与顺序合法）；
   - 用正则逐行从各分片提取 `"key": "value"`（value 非空）对，按 key 注入干净骨架；
   - 用 `json.dump(..., ensure_ascii=False)`（无 BOM）回写。
   - 这样即使分片文件结构损坏，只要 value 字段还在，翻译就能无损恢复。
4. 合并后用 `ilpatch patch <dll> <dict> --dry` 验证 `would replace` 条数是否合理（应≈翻译条目数 × 重复 key 复现次数）。

**配套工具：**
- `merge_parts.py` — 位于 `tools/ilpatch/merge_parts.py`，用于从各 `partN.json` 分片中提取非空翻译值、注入干净的 `extract.json` 骨架。用法：直接运行，输出 `ArcenAIW2Core.merged.json`。
- 合并后用 `ilpatch patch <dll> <merged.json> --dry` 验证 `would replace` 条数。

**其他实测要点：**
- `extract` 导出的字典 key 可能含**重复原文**（dnlib 提取的 ldstr 有重复），`ilpatch` 的 `JsonSerializer.Deserialize<Dictionary>` 会按 .NET 行为处理重复 key；填充时各子代理可能各自填一份，合并脚本取"后者覆盖"即可，不影响最终 `patch` 匹配（patch 按原文精确匹配 ldstr，与字典 key 唯一性无关）。
- 实际可翻译比例远低于候选数：ArcenAIW2Core 候选 2347 条，最终约 549 个独立 key（ldstr 替换 637 次）为玩家可见文本，其余多为 Debug/Error/标识符，按 8.15.6 规则跳过。
- 部署目标不是 `ReliableDLLStorage/`（那是原版源，规范禁止改动），而是仓库 `PatchedAssemblies/`；`deploy.ps1` 优先用它覆盖游戏目录。

**进度记录（2026-07-09 实测完成）：**
| DLL | 候选条数 | 实际替换 ldstr | 翻译条目 | 状态 |
|-----|---------|---------------|---------|------|
| ArcenAIW2Visualization | 113 | 20 | 20 | ✅ 已完成 |
| ArcenUniversal | 1495 | 151 | 120 | ✅ 已部署 |
| ArcenAIW2Core | 2347 | 889 | 748 | ✅ 已部署 |

> 2026-07-09 本次新增 ArcenAIW2Core 翻译 +199 条目（252 处 ldstr 替换），涵盖星域调查、坐标验证、出哨站（Outguard）描述、小队行为调试信息、AI 预算分配等类别。合并脚本 `merge_parts.py` 已编写至 `tools/ilpatch/`，后续可直接复用。

**2026-07-10 补翻：** 发现 `merged.json` 中有 2 条 wormhole 相关格式字符串翻译为空：
- `" wormholes linking to their own planet in this map.  Harmless now."` — 用于生成地图界面动态数字拼接（`{0} wormholes linking...`），空翻译导致英文原文直接显示。
- `"\n<color=#888888>Arrives somewhere between your station and enemy wormholes.</color>  "` — 空翻译。

修复方式：直接在 `merged.json` 中填入中文翻译，重新运行 `ilpatch patch` 生成 DLL，提交 `PatchedAssemblies/` 后部署。

**教训**：`extract` 导出的候选条目中，部分调试/日志字符串被保留为空（`""`）。ilpatch 遇到空值会跳过替换，导致原文保留。应定期用 `ilpatch inspect` 或 Python 脚本检查空翻译条目，区分"不应翻译的调试信息"和"遗漏的玩家可见文本"。

## 九、DLC 翻译（第二波）

### 9.1 概述

补充翻译三个 DLC 的缺失文件，包括剧情日志、成就和模组舰船。

### 9.2 已翻译内容

#### DLC1 - The Spire Rises (尖塔崛起)

| 类型 | 文件数 | 内容 |
|------|--------|------|
| JournalEntries | 3 | 尖塔族战役剧情、球体战争、弃用文件 |
| Achievement | 1 | 成就名称和描述 |

#### DLC2 - Zenith Onslaught (天顶星攻势)

| 类型 | 文件数 | 内容 |
|------|--------|------|
| JournalEntries | 4 | 黑暗泽尼斯、游牧星球、拱顶石、矿工剧情 |
| Achievement | 1 | 成就名称和描述 |

#### DLC3 - The Neinzul Abyss (奈因祖尔深渊)

| 类型 | 文件数 | 内容 |
|------|--------|------|
| JournalEntries | 7 | 野生蜂巢、决战装置、工兵、死灵法师、移民者、长老、守护者剧情 |
| Achievement | 1 | 成就名称和描述 |

#### ExoticShips 模组

| 类型 | 文件数 | 内容 |
|------|--------|------|
| DLC1 Ships | 2 | 护卫舰、舰队舰船名称和描述 |
| DLC2 Ships | 2 | 护卫舰、舰队舰船名称和描述 |
| DLC3 Ships | 1 | 护卫舰名称和描述 |

### 9.3 文件位置

翻译文件存放在 `AIWar2_ChineseTranslation` 目录下：

```
AIWar2_ChineseTranslation/
├── GameData/Configuration/Expansions/
│   ├── 1_The_Spire_Rises/GameData/Configuration/
│   │   ├── JournalEntries/          ← 新增 3 个文件
│   │   └── Achievement/             ← 新增 1 个文件
│   ├── 2_Zenith_Onslaught/GameData/Configuration/
│   │   ├── JournalEntries/          ← 新增 4 个文件
│   │   └── Achievement/             ← 新增 1 个文件
│   └── 3_The_Neinzul_Abyss/GameData/Configuration/
│       ├── JournalEntries/          ← 新增 7 个文件
│       └── Achievement/             ← 新增 1 个文件
└── XMLMods/ExoticShips/GameEntity/  ← 新增 5 个文件
```

### 9.4 部署说明

`deploy.ps1` 已更新，包含 `XMLMods` 目录的部署。运行 `.\deploy.ps1` 即可部署所有翻译文件。

### 9.5 白名单更新

`check_update.ps1` 白名单已更新（继承自 `check_translation.ps1`），包含以下不需要翻译的文件类型：
- External* 系列文件（纯数值配置）
- 其他弃用/调试文件

### 9.6 翻译统计

| 类别 | 新增文件数 | 总翻译文件数 |
|------|-----------|-------------|
| DLC JournalEntries | 14 | 14 |
| DLC Achievement | 3 | 3 |
| DLC ScourgeTypeData | 2 | 2 |
| ExoticShips 模组 | 5 | 5 |
| **合计** | **24** | **24** |

### 9.7 完整翻译清单

| 类型 | 文件数 | 内容 |
|------|--------|------|
| 基础游戏 GameEntity | 34 | 所有舰船、建筑、防御设施 |
| 基础游戏 JournalEntries | 9 | 所有剧情日志 |
| 基础游戏 Achievement | 1 | 成就 |
| 基础游戏 Tips | 12 | 所有游戏提示 |
| 基础游戏 Tutorials | 6 | 所有教程 |
| 基础游戏 TextVarMaps | 1 | 快速开始菜单文本、状态消息、格式标签 |
| 基础游戏 SpecialFaction | 6 | 阵营名称和描述 |
| 基础游戏 HackingType | 5 | 黑客类型 |
| DLC1 JournalEntries | 3 | 尖塔族剧情 |
| DLC1 Achievement | 1 | 成就 |
| DLC1 GameEntity | 21 | DLC1 舰船和实体 |
| DLC1 其他 | 5 | 设置、阵营等 |
| DLC2 JournalEntries | 4 | 天顶星剧情 |
| DLC2 Achievement | 1 | 成就 |
| DLC2 GameEntity | 17 | DLC2 舰船和实体 |
| DLC2 ScourgeTypeData | 1 | 天顶战士描述 |
| DLC2 其他 | 11 | 设置、阵营等 |
| DLC3 JournalEntries | 7 | 奈因祖尔剧情 |
| DLC3 Achievement | 1 | 成就 |
| DLC3 GameEntity | 22 | DLC3 舰船和实体 |
| DLC3 其他 | 28 | 设置、阵营等 |
| XMLMods | 5 | ExoticShips 模组舰船 |
| **总计** | **210** | - |

### 9.8 合并脚本 bug 修复（2026-07-10）

`merge_expansion_translations.py` 曾有两个 bug，导致 DLC 部署后游戏报错：

**Bug 1：多 system 覆盖错位**

合并脚本在处理同实体下多个 `<system>` 子元素时，总是将翻译覆盖到第一个匹配标签的元素 (`candidates[0]`)，而非按 `name` 属性精确匹配。导致武器系统（如 `name="W1"`）的属性被隐形装置（`name="C"`）的翻译覆盖，产生重复 `name="C"` 的 system 条目（305处，涉及全部3个DLC）。

修复：改为按 `tag` + `name` 属性精确匹配子元素；且只覆盖可翻译属性（`display_name`、`description` 等），不碰 `name`、`category`、游戏数值等非翻译字段。

**Bug 2：ObjectiveCategory 缺失**

`NA_ObjectiveCategories.xml` 汉化覆盖文件遗漏了 `NecromancerStrategy` 类别定义，但 `NA_ObjectiveDetailsHooks.xml` 中有6个 hook 引用了该类别，导致游戏运行时查表失败（`Table ObjectiveCategoryTable was asked for record with name='NecromancerStrategy' but it's not there`）。

修复：在汉化覆盖文件中补回 `NecromancerStrategy` 类别（display_name="死灵策略"）。

**Bug 3：Journal XML 截断**

`ZO_Journal_DarkZenith.xml` 的 GameData\Configuration\Expansions 参考副本缺少结尾 `</root>` 标签，导致 XML 解析失败。修复：从 ChineseTranslation 目录的完整中文版恢复。

**教训**：合并覆盖文件必须包含源文件中所有被引用的条目，不能遗漏。合并脚本不应修改非翻译属性。

### 9.9 C# UI 文本补译（2026-07-10 第2批）

修复 AIWarExternalCode 9 个文件中遗漏的 ~30 条英文字符串（编译通过，0 错误）：

修复 AIWarExternalCode 9 个文件中遗漏的 ~30 条英文字符串（编译通过，0 错误）：

| 文件 | 遗漏内容 | 补译条数 |
|------|---------|:--------:|
| Window_InGameHoverEntityInfo.cs | "Resource Multipliers After Time Here And Not Crippled" 区块（含 CLARIFICATION x3、Cannot be claimed x1） | 12 |
| Window_PrototypeInGameHoverEntityInfo.cs | "Cannot be claimed for another..." | 1 |
| EntityText.Attr.cs | "Cannot be claimed for another..." | 1 |
| AIPChange.cs | "At ... AIP changed" | 1 |
| PublicCrashingNomadPlanetNotifier.cs | 撞击倒计时、星球移动提示 | 2 |
| PublicAIReservesNotifier.cs | AI 预备队虫洞提示 | 1 |
| PublicDZInvasionNotifier.cs | Dark Zenith 入侵提示 | 1 |
| PublicImperialSpireNotifier.cs | 帝国尖塔到达提示 | 1 |
| PublicArchitraveExpansionNotifier.cs | 天顶拱门扩张模式描述 | 6 |

**根因**：`Window_InGameHoverEntityInfo.cs`（8390 行）中 "Resource Multipliers" 区块在大规模翻译时未被覆盖。

**教训**：翻译大型 UI 文件（>5000 行）时，必须逐区块检查确保无遗漏。BaseInfo/ 下的 Notifier 类包含玩家可见通报文本，必须翻译。

### 9.10 C# UI 文本补译（2026-07-10 第3批——补遗）

修复 `Window_UnitEncyclopedia.cs` 中遗漏的 10 条英文字符串（编译通过，0 错误）：

| 行号 | 英文 | 中文 |
|------|------|------|
| 1136, 1278 | `"Details for " + name` | `name + " 的详细信息"` |
| 1137, 1279 | `"Close"` | `"关闭"` |
| 1464, 1475 | 文本框搜索提示 | `"选择文本框如何对上方单位列表进行搜索。"` |
| 1609, 1620 | 分类筛选提示 | `"选择一种分类筛选方式来过滤上方的列表，通过单位的某些特性来缩小查找范围。"` |
| 1709, 1720 | 排序提示 | `"选择单位的排序方式。"` |
| 1806, 1817 | 阵营筛选提示 | `"选择要用于右侧星系图显示模式中作为筛选条件的阵营或阵营类型。"` |

**根因**：4 组下拉框的 `HandleMouseover()` / `HandleItemMouseover()` tooltip 字符串在初次翻译时被遗漏。`"Details for "` 和 `"Close"` 亦为 UnitEncyclopedia 弹窗反复出现的漏译。

### 9.11 QuickStarts2 战役名称翻译（2026-07-10）

快速开始菜单中的战役名称（文件夹分类 + 单个战役）来自 `GameData/QuickStarts2/` 下的 `.tooltip` 文件，**不走翻译系统**——名称直接由 `SaveLoadMethods.LoadTooltipFromDisk()` 读取 `#showas:` 指令或 `.save` 文件名展示。

**方案**：在翻译仓库创建 `GameData/QuickStarts2/` 镜像目录，为每个 `.tooltip` 文件添加/改写 `#showas:中文名`。

**文件结构**：95 个 `.tooltip` 文件（10 个 `_folder.tooltip` + 85 个战役 `.tooltip`），目录结构与游戏完全一致：
```
GameData/QuickStarts2/
├── 1-Basic/_folder.tooltip          # #showas:基础
├── 2-Moderate/_folder.tooltip       # #showas:中等
├── 3-Expansions Intro/              # 扩展包入门
├── 4-Necromancer Intro/             # 死灵法师入门
├── 5-Moderate (Community)/          # 中等（社区）
├── 6-Harder/                        # 困难
├── 7-Harder (Community)/            # 困难（社区）
├── 8-Extreme/                       # 极限
├── 9-Extreme (Community)/           # 极限（社区）
└── Archived/                        # 归档
```

**部署**：`deploy.ps1` 已新增 QuickStarts2 部署步骤，将翻译仓库中的 `.tooltip` 文件复制到游戏目录同名位置。

**不纳入基线**：`.tooltip` 文件不是游戏代码/配置，不会随游戏更新而变，无需用 `check_update.ps1 -snapshot` 追踪。

**生成工具**：`GameData/QuickStarts2/generate_translated_tooltips.py` 用于批量生成翻译后的 tooltip 文件。

### 9.12 QuickStarts2 工具提示描述翻译 + C# 修复（2026-07-10）

在上一步战役名称翻译基础上，进一步翻译了全部战役 `.tooltip` 文件中的长篇英文描述文本（85 个战役 + 10 个分类文件夹），覆盖难度说明、阵营介绍、玩法策略等玩家可见内容。

**C# 兼容修复**：`Window_LoadQuickStartMenu.cs` 的 `HasDifficulty` 条件判断原本硬编码英文组名（`"Basic"`、`"Moderate"` 等），由于 `#showas:` 已改为中文，这些条件全部失效。修复为同时匹配中英文：
```
// 原：if (Instance?.CurrentGroup.DisplayName == "Basic")
// 改：string dn = Instance?.CurrentGroup.DisplayName;
//     if ( dn == "Basic" || dn == "基础" )
```
需重新编译 DLL 后部署。

**注意**：`4-Necromancer Intro` 文件夹的 tooltip 原始在基本游戏中为空（0 字节），实际内容位于 DLC3 `Expansions/3_The_Neinzul_Abyss/QuickStarts2/`。2026-07-10 之前翻译镜像也仅含 `#showas:` 无描述正文，导致游戏加载此目录下 `.save` 时回退到 `WriteTooltip()` 显示元数据。

**2026-07-10 修复**：将 DLC3 版本中的描述正文同步到 `GameData/QuickStarts2/4-Necromancer Intro/` 下的 4 个 `.tooltip` 文件（含 `_folder.tooltip`），确保无论游戏从哪个目录加载 quickstart，都能显示正确的中文描述。详见 9.14。

`deploy.ps1` 同时部署两处。

### 9.13 C# UI 文本补译大扫除（2026-07-10 第4批）

多子代理并行修复 20 个文件中遗漏的 ~60 条英文字符串（编译通过，0 错误）：

| 文件 | 遗漏内容 | 条数 |
|------|---------|:----:|
| Window_ResourceBar.cs | 金属/黑客/特殊资源/威胁/胜负 tooltip 大段说明 | 16 |
| SpireRelicMoveHandler.cs | 尖塔城市建造确认对话框 + 错误提示（两个同名内部类） | 14 |
| Window_ModalSwapFleetMembers.cs | 舰队编组交换确认/错误消息 | 15 |
| GameFlowGameCommands.cs | 转化失败弹窗 4 组（标题 + 消息 + 按钮） | 8 |
| SuperCommonGameCommands.cs | 指挥站/数量上限/能量不足等聊天消息 | 8 |
| PlayerDrivenLessCommonGameCommands.cs | 黑客点数不足弹窗 | 2 |
| EndpointFunctions.cs | 地图生成进行中弹窗 | 2 |
| GameCommand_EditFleetData.cs | 资源不足升级消息 | 3 |
| Window_ErrorReportMenu.cs | 错误报告按钮 tooltip | 3 |
| PublicSpireCityUpgradeNotifier.cs | 城市升级对话框 | 3 |
| SpireSidekickUpgradeNotifier.cs | 城市升级对话框（同前） | 3 |
| Window_SettingsMenu.cs | OK 按钮 + 未安装/未启用文本 | 4 |
| Window_ModalFleetMemberModularEditing.cs | Ok 按钮标签 | 3 |
| Window_UnitEncyclopedia.cs | 图鉴 lore 弹窗 OK 按钮 | 1 |
| JournalOrTipChatHandler.cs | 日志弹窗 OK 按钮 | 1 |
| MalwareFactionBaseInfo.cs | 突破弹窗 OK 按钮 | 1 |
| MalwareFactionDeepInfo.cs | 突破完成弹窗 OK 按钮 | 1 |
| MalwareNotifiers.cs | Conduit 调查提示 tooltip | 1 |
| Window_InGameSidebarHacking.cs | 空类型回退字符串 | 1 |
| Window_InGameGalaxyOptions.cs + 2 个 Setup 文件 | 空子类别回退字符串 | 3 |
| **合计** | **20 个文件** | **~60 条** |

**根因**：大规模初翻后遗漏了大量玩家可见字符串，`Window_ResourceBar.cs` 尤为严重（16 条大段 tooltip 均为空）。尖塔城市建造和舰队编组交换对话框完全未翻译。

**教训**：UIs/ 目录下的大型文件（Window_ResourceBar.cs 约 3000+ 行）和 AlternativeMoveOrderHandlers/ 下的交互对话框容易整体漏翻，需逐类检查。

### 9.14 Tooltip 描述缺失修复（2026-07-10）

**问题**：死灵法师入门（4-Necromancer Intro）分类下的 4 个 `.tooltip` 文件在 `GameData/QuickStarts2/` 镜像目录中缺少描述正文，仅含 `#showas:` 行。而 DLC3 `Expansions/` 下的对应文件有完整描述文本。

游戏同时扫描 `GameData/` 和 `Expansions/` 目录，当从 `GameData/` 加载 `.save` 时，`SaveLoadMethods.LoadTooltipFromDisk()` 读到的 `.tooltip` 文件无体文本，`TooltipData` 为空，导致 `HandleMouseover()` 回退到 `WriteTooltip()` 自动生成元数据。显示内容为：
- `Map Type: HO`（mapTypeShort 缩写而非全名）
- `AI Type: nullAI`（AI 类型未设置时的占位符）

**修复文件**（`AIWar2_ChineseTranslation\GameData\QuickStarts2\4-Necromancer Intro\`）：

| 文件 | 修复内容 |
|------|---------|
| `_folder.tooltip` | 添加 AI 难度说明和死灵法师派系介绍 |
| `Neinzul Galaxy.tooltip` | 添加 Neinzul 守护者/迁徙舰队等战役描述 |
| `Necromancer Introduction (Less Easy).tooltip` | 添加完整战役介绍 |
| `Necromancer Introduction (Easy).tooltip` | 添加完整战役介绍 |

描述内容从 `Expansions\3_The_Neinzul_Abyss\QuickStarts2\4-Necromancer Intro\` 下的对应文件同步。

**根因**：初次翻译时，`GameData/` 镜像目录被认为"仅含 `#showas:`"无需描述，未意识到缺少体文本会导致回退到元数据自动生成。

**教训**：具有 `metaExists=true`（有 `.savemet`）的 quickstart 如果 `.tooltip` 文件无体文本，会自动回退到 `WriteTooltip()` 生成格式化元数据。必须确保 `GameData/QuickStarts2/` 下所有 `.tooltip` 文件包含描述正文，否则悬浮窗会显示缩写/占位符而非可读描述。

### 9.15 `custom_NameForLobby` 派系大厅名称翻译（2026-07-10）

**问题**：游戏大厅中「添加派系」下拉菜单和派系标签页中，各个派系的 `custom_NameForLobby` 属性值未翻译，显示为英文。

**修改文件（4 个 XML，6 处修改，仅改 `custom_NameForLobby` 属性值，颜色标签保留）：**

| 文件 | 英文 | 中文 |
|------|------|------|
| `SpecialFaction/KDL_VanillaEntries.xml`（Human） | `Additional <color=#318CE7>Player</color> Faction` | `额外<color=#318CE7>玩家</color>派系` |
| `SpecialFaction/AIAndSubfactions.xml`（AI） | `Additional <color=#CC5500>AI</color> Faction` | `额外<color=#CC5500>AI</color>阵营` |
| `SpecialFaction/Badger_RandomSpecialFactions.xml`（Random） | `Additional <color=#9EB9D4>Random</color> Faction` | `额外<color=#9EB9D4>随机</color>派系` |
| `SpecialFaction/KDL_VanillaEntries.xml`（ZenithDysonSphere） | `Dyson Sphere: <color=#324AB2>Zenith</color>` | `戴森球: <color=#324AB2>天顶</color>` |
| `Expansions/.../TSR_SpecialFactions.xml`（SpireSphere_Gray） | `Dyson Sphere: <color=#A1A1A1>Gray</color>` | `戴森球: <color=#A1A1A1>灰色</color>` |
| `Expansions/.../TSR_SpecialFactions.xml`（SpireSphere_Chromatic） | `Dyson Sphere: <color=#FF00FF>Chromatic</color>` | `戴森球: <color=#FF00FF>多彩</color>` |

**背景**：`display_name` 在 XML 中已有中文翻译（如 `display_name="天顶戴森球"`），但 `custom_NameForLobby` 是独立属性，专门用于大厅 UI 的派系选择下拉列表和标签页，需单独翻译。`AI风险分析器` 的 `custom_NameForLobby` 不存在（使用 `display_name` 回退），所以无需翻译。

**注意**：TSR_SpecialFactions.xml 中 `SphereType` 自定义字段的 description 属性（第 118-120 行，关于 Gray Spire/Chromatic Spire 的英文介绍）仍有待翻译，因 XML 解析需要保留内部属性格式且原文较长，留待后续处理。

### 9.16 C# 实例字符串补译 + 单引号语法修复（2026-07-11）

补译 `DLLSource/` 下约 30 个 C# 文件中的遗漏英文字符串 ~100 条（全量编译通过，3 项目 0 错误）：

| 文件 | 遗漏内容 | 条数 |
|------|---------|:----:|
| Window_ResourceBar.cs | 能量/燃料/入侵/AIP/攻击/百科/暂停等 tooltip 及 UI 标签（第 2 轮补翻） | ~30 |
| EntityText.Writer.cs | 威胁/猎杀/隐形状态 tooltip | 8 |
| GameFlowGameCommands.cs | 无法拆除各处类型提示 + SuperCommon 资源不足 | 9 |
| TSR_GameCommands.cs | 尖塔城市升级消息（含 sidekick 副本） | 7 |
| SuperCommonGameCommands.cs | 资源/能量/指挥站上限聊天消息 + 放置拒绝原因 | 7 |
| SelfHacking_Base.cs / Hacking.cs | 入侵拒绝原因 5 处 | 5 |
| NeinzulAbyss_Hacking.cs | 死灵法师入侵/升级/精英入侵等拒绝原因 & 描述 | 19 |
| 目标生成器（6 个文件） | 任务前缀 "Destroy/Hack/Hold/Capture and hold" | 13 |
| DeepInfo 事件消息（多种族） | 尖塔遗物/Scourge/Renegade/Ark/Doomsday 等 ~25 处 | ~25 |
| PlanetViewSelector.cs / CheatsAndCommands.cs | 发送舰船/客户端限制消息 | 4 |
| _EntityText.cs / Window_PrototypeInGameHoverEntityInfo.cs | 窗口标题/按钮文本 | 8 |
| 其他（Fireteam / Outguard / ExoGalactic 等） | 工具提示及聊天消息 | 5 |
| **合计** | **~30 个文件** | **~200 条** |

**C# 单引号语法修复**：增量编译掩盖了 10 个文件中的 62 处 `'中文...'` 错误（C# 中 `'` 是 char 字面量，不能多字符）。清理 `obj/Release` 后全量编译暴露了这些错误。本次批量修复：将 `'...'` 改为 `"..."`，内部中文引号改用 `「」`（替代原规则建议的 `''`）。

**构建注意事项**：
- `build.ps1` 使用的 MSBuild `Build` 目标（增量编译）有时不会检测源文件变更。修改翻译后**必须**先 `Remove-Item -Recurse -Force "DLLSource/AIWarExternalCode/obj/Release"` 再 `build.ps1`，否则旧 DLL 会被部署
- 原翻译规则第 7 条「禁止中文引号，用 `''` 替代」有误——C# 单引号只允许单个字符。已更正为：用 `"..."` + 内部 `「」`
