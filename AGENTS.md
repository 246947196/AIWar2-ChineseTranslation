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

`I18NFont4UnityGame` 插件只 patch 了 `TMPro.TextMeshProUGUI`（画布 UI 文字）。银河地图星球名、实体标签等使用 `TMPro.TextMeshPro`（世界空间 3D 文字），两者是不同的 Unity 组件，I18NFont4UnityGame 未覆盖。

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
│   └── WorldTMPFontPatchPlugin.cs
└── WorldTMPFontPatch.csproj
```

### 字体类型对比

| 组件类型 | 用途 | 处理插件 |
|---------|------|---------|
| `TextMeshProUGUI` | 画布 UI 文字（菜单、面板、提示框） | I18NFont4UnityGame + WorldTMPFontPatch（后备） |
| `TextMeshPro` | 世界空间 3D 文字（星球名、实体标签） | WorldTMPFontPatch |

### 已知问题：聊天/消息日志中文方框

**2026-07-11 记录**。聊天日志（ChatLog）的中文字符显示为 `_` / `__`，而非正确的中文。

**根因**：游戏使用了高度修改的 TextMeshPro（位于 `AIW2ModdingAndGUI/Assets/com.unity.textmeshpro@2.0.1/`），其 `FontEngine` 在运行时无法加载字体数据，导致 `TMP_FontAsset.CreateFontAsset()` 创建的字形图集为空。

尝试过的方案均无效：

| 方案 | 结果 |
|------|------|
| I18NFont4UnityGame 自带 `mi_sans` bundle | TMP_FontAsset 图集为空（0 字形） |
| `TMP_FontAsset.CreateFontAsset(Font)` | 运行时 `FontEngine.LoadFontFace()` 失败 |
| 系统字体 `Microsoft YaHei` 等 | 同上，FontEngine 无法加载 |
| 标准 Unity TMP 生成的 AssetBundle | 格式与游戏修改版 TMP 不兼容，渲染全空白 |
| UnityPy 注入标准字体图集到 bundle | 8192x8192 图集格式不兼容，渲染全空白 |

**唯一可行方案**：用游戏的修改版 TMP（`AIW2ModdingAndGUI` Unity 项目）在 Unity Editor 中生成 TMP_FontAsset。需要将 `tools/mi_sans.ttf` 导入该项目，使用 TMP Font Asset Creator 生成（Dynamic，4096x4096），然后打成 AssetBundle 替换 `BepInEx/plugins/I18NFont4UnityGame/mi_sans`。

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
| PublicCrashingNomadPlanetNotifier.cs | 撞击倒计时、星球移动提示 x2 |
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

## IL 补丁验证

Patch 后运行：
```powershell
python tools/ilpatch/verify_patch.py PatchedAssemblies\ArcenAIW2Core.dll tools\ilpatch\ArcenAIW2Core.merged.json
```

验证原理：`ilpatch dump-ldstr <dll> <out.json>` 直读 #US 堆全部 ldstr 为 JSON 数组。`verify_patch.py` 加载后逐一检查 key/value 是否存在，0 盲区。

修改源码编译的 DLL 后必须运行 `build.ps1` 重新编译，`deploy` 部署（或手动复制 DLLBin/*.dll 到 GameData/ModdableLogicDLLs/）。修改 XML 后必须重新 `deploy` 或手动复制到对应目录。

patch 前必须从 `AIWar2_Data\Managed\` 复制原始 DLL（禁止在已 patch 的 DLL 上重复运行 `ilpatch patch`）。

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
