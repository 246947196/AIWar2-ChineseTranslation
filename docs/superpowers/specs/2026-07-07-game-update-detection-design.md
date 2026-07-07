# 游戏更新检测方案设计

**日期**: 2026-07-07
**状态**: 已批准

## 一、概述

AI War 2 汉化项目目前仅有针对 XML 文件的覆盖检测（`check_translation.ps1`），缺少覆盖 DLL 源码、核心 DLL、AssetBundle 三个层面的系统性更新应对方案。

本方案设计一套 **A+B 混合方案**：
- **B（哈希追踪）**：记录每个源文件的 SHA256 哈希，快速过滤未变更文件
- **A（快照基线）**：提取英文字符串存为结构化快照，精确报告新增/修改/删除

## 二、架构

### 两层检测流水线

```
[游戏更新] → Tier 1: 哈希扫描 → 过滤未变文件 → Tier 2: 提取+比对快照 → 保存报告
                (B: 秒级)                              (A: 精准)
```

### 工具：`check_update.ps1`

替换现有的 `check_translation.ps1`。路径使用 `$PSScriptRoot` 动态推导，不依赖硬编码路径。三个模式：

| 命令 | 行为 |
|------|------|
| `check_update.ps1` | 默认模式：检测更新并生成报告 |
| `check_update.ps1 -snapshot` | 基线模式：建立/更新快照 |
| `check_update.ps1 -snapshot -force` | 强制覆盖已有快照 |

### JSON 处理

快照 JSON 的读写使用 `Read-JsonFile` 函数（基于 `System.Web.Extensions.JavaScriptSerializer`），保证键名大小写敏感（区分 `OK` 和 `Ok`）。反序列化后通过 `Convert-PSObjectToHashtable` 递归转换为 `[hashtable]` 以统一后续对比逻辑。

### 覆盖四个层面

| 层面 | 英文来源 | 提取方法 | 基线可用性 |
|------|---------|---------|-----------|
| XML | `GameData/Configuration/` | 正则提取文本属性 | Steam 恢复后可用 |
| DLL 源码 | `CodeExternal/` | 正则提取 `"..."` 字面量 | **始终可用** |
| 核心 DLL | `AIWar2_Data/Managed/` | 反编译/strings 提取 | **始终可用** |
| arcenui | `AssetBundles_Win/arcenui` | UnityPy 提取 | Steam 恢复后可用 |

## 三、快照文件格式

文件名: `translation_snapshot.json`
位置: `AIWar2_ChineseTranslation/`

```json
{
  "game_version": "5.825",
  "taken_at": "2026-07-07T20:00:00Z",
  "layers": {
    "xml": {
      "GameData/Configuration/GameEntity/KDL_Ships_FleetShips.xml": {
        "hash": "e3b0c44298fc1c14...",
        "strings": {
          "display_name": "V-Wing",
          "description": "A low-cost short-range fighter..."
        }
      }
    },
    "dll_source": {
      "AIWarExternalCode/src/UIs/Window_MainMenu.cs": {
        "hash": "abc123...",
        "strings": {
          "L42:Window_MainMenu": "Settings",
          "L48:Window_MainMenu": "Controls"
        }
      }
    },
    "dll_core": {
      "ArcenUniversal.dll": {
        "hash": "def456...",
        "strings": {}
      }
    },
    "arcenui": {
      "hash": "789abc...",
      "strings": {
        "Settings": "Settings",
        "Background Story": "Background Story"
      }
    }
  }
}
```

**注意**：
- JSON 解析使用 `System.Web.Extensions.JavaScriptSerializer`（大小写敏感），替代 PowerShell 内置的 `ConvertFrom-Json`（大小写不敏感）。
- 哈希值不含 `sha256:` 前缀（纯 hex 小写）。
- 反序列化后的 `Dictionary<string,object>` 通过 `Convert-PSObjectToHashtable` 递归转换为 `[hashtable]`，以统一数据格式。

### 字符串 Key 生成规则

| 层面 | Key 格式 |
|------|---------|
| XML | 直接使用属性名（`display_name` / `description` / `full_text` / `category`） |
| DLL 源码 | `L{行号}:{文件名无后缀}`（如 `L42:Window_MainMenu`） |
| 核心 DLL | 哈希追踪 + 备注 `requires ilspycmd`，提取需反编译后人工介入 |
| arcenui | 直接用英文字符串原文做 key |

### 白名单（不纳入快照的文件）

复用并扩展 `check_translation.ps1` 已有的白名单：
- External* 系列（`ExternalConstants`、`ExternalFactionBaseInfo` 等）
- `Balance_*`、`AIShipGroup*`、`UIPrefab/`、`UIWindow/` 等纯配置
- DLL 中的 Debug 日志、内部标识符、`{变量}` 插值模式

## 四、版本一致性与快照安全

### 4.1 各工具版本检查

| 场景 | 工具 | 行为 |
|------|------|------|
| 无快照时运行默认模式 | `check_update.ps1` | 报错退出：`请先运行 -snapshot 建立基线` |
| 快照版本 == 当前游戏版本，运行默认 | `check_update.ps1` | 提前退出，不生成报告 |
| 快照版本 < 当前版本，运行默认 | `check_update.ps1` | 执行完整检测，生成报告 |
| 运行 `-snapshot` 且快照已存在 | `check_update.ps1` | 拒绝，提示使用 `-snapshot -force` |
| 运行 `-snapshot -force` | `check_update.ps1` | 强制覆盖。**先检测游戏目录是否为英文状态**（扫描 3 个已知 XML 文件，检查是否含中文），若发现中文则警告并退出 |

### 4.2 deploy.ps1 部署前检查

`deploy.ps1` 在部署任何翻译文件前执行以下检查：

| 条件 | 行为 |
|------|------|
| 无 `translation_snapshot.json` | 报错退出：`未找到基线快照。请先运行 check_update.ps1 -snapshot 建立基线` |
| 快照版本 < 当前游戏版本 | 报错退出：`基线版本 v{old} 低于游戏版本 v{new}，请先运行 check_update.ps1 检测更新并更新基线` |
| 快照版本 == 当前游戏版本 | ✅ 允许部署 |

版本号来源：
- 快照版本：`translation_snapshot.json` 中 `game_version` 字段
- 当前游戏版本：`GameData/Configuration/GameVersion/KDL_GameVersions.xml`（复用现有逻辑）

## 五、检测报告格式

输出文件: `translation_update_report_YYYY-MM-DD.txt`

```
=== AI War 2 Hanhua Update Detection Report ===
Date: 2026-07-07
Game version: 5.825 -> 5.831

[Layer: XML]
  OK (no change): 247
  Modified (needs retranslation): 5
    - GameData/Configuration/GameEntity/KDL_Ships_FleetShips.xml
       MODIFIED: display_name: "V-Wing" -> "New V-Wing"
    - GameData/Configuration/Tips/CMP_Tips_GettingStarted.xml
       NEW: full_text: "New tip about the Spire"
       DELETED: description
  Hash changed (verify manually): SomeFile.xml
  New files (needs translation): 2
    - GameData/Configuration/JournalEntries/Lore_NewContent.xml
       NEW: display_name: "New Lore Entry"
       NEW: description: "A new lore entry description"
  Deleted files: 1
    - OldRemovedFile.xml

[Layer: DLL Source]
  OK (no change): 42
  Modified (needs retranslation): 2
    - src/Chat/ChatCommands.cs
       NEW: L203:ChatCommands: "New chat command description"
  New files: 1
    - src/UIs/NewUI.cs
       NEW: L15:NewUI: "Hello World"

[Layer: Core DLL]
  OK (no change): 2
  Hash changed (needs re-decompilation): 1
    - ArcenAIW2Core.dll (re-decompile with ilspycmd)

[Layer: arcenui AssetBundle]
  OK (no change): 120
  New strings: 3
    - "New Feature"
  Removed strings: 2
  Bundle binary hash changed (content updated)

[Summary]
  OK (no action needed): 291
  Needs retranslation: 11
  New files to translate: 3
```

注意：报告为纯文本格式，不含 Emoji，使用英文标签。游戏版本字符串直接取自 `KDL_GameVersions.xml` 中的 `major_version.minor_version`。

## 六、日常使用流程

### 6.1 首次基线建立（一次性）

1. Steam → 验证游戏文件完整性（恢复为英文原版）
2. 运行 `check_update.ps1 -snapshot`
3. 运行 `deploy.ps1` 重新部署汉化
4. 提交 `translation_snapshot.json` 到 git

### 6.2 游戏更新后

**关键原则：快照永远在游戏文件为英文状态时建立。**

Steam 更新后，游戏文件被恢复为英文原版。此时（部署翻译前）是最佳快照窗口。

1. Steam 自动更新游戏（游戏此时为英文 v5.831）
2. 运行 `check_update.ps1` → 提取当前英文，比对快照，生成 `translation_update_report_*.txt`
3. **运行 `check_update.ps1 -snapshot -force`** → 从当前英文状态更新基线到 v5.831
4. 按报告逐条翻译修改
5. 运行 `build.ps1` + `deploy.ps1`（游戏变为中文）
6. 提交所有变更到 git

### 6.3 日常新增翻译后

翻译新内容后不需要更新快照。快照只追踪游戏英文原文的变化，不追踪翻译进度。日常新增翻译不影响基线。

## 七、各层提取技术方案

### 7.1 XML 层

正则提取以下属性的文本值：`display_name`、`description`、`full_text`、`category`。
- 使用 `(?s)` 单行模式处理跨行属性（部分 XML 属性值可能换行）
- 属性值提取正则：`(display_name|description|full_text|category)="((?:[^"]|""|\\")*?)"`
- 兼容 XML 转义（`""` 双引号转义、`\"` 转义引号）
- 键直接用属性名（如 `display_name`），不含实体路径前缀
- 白名单文件跳过

### 7.2 DLL 源码层

对 `CodeExternal/` 下的 `.cs` 文件（跳过 `obj/` 和 `bin/` 目录）：
1. 跳过注释行（`//`）和空行
2. 正则 `"((?:[^"\\]|\\.)*)"` 提取字符串字面量，兼容转义字符
3. 排除纯数字/符号/运算符等非文本内容（正则过滤：`^[\d\s\-\.\,...]+$`）
4. 按 `L{行号}:{文件名无后缀}` 记录位置
5. 仅记录长度 ≥ 2 的有效字符串

### 7.3 核心 DLL 层

对 `AIWar2_Data/Managed/` 下的三个 DLL：
- `ArcenUniversal.dll`
- `ArcenAIW2Core.dll`
- `ArcenAIW2Visualization.dll`

使用 `ilspycmd` 反编译为 C# 源码后，复用 7.2 的字符串提取逻辑。不直接使用 `strings` 工具（噪音太大，包含大量非文本二进制字符串）。

### 7.4 arcenui AssetBundle 层

复用现有 `patch_arcenui.py` 的 `extract` 命令逻辑：
1. 用 UnityPy 加载 bundle
2. 提取所有 MonoBehaviour 的 `m_text` 字段
3. 写入快照

## 八、快照生命周期

### 8.1 快照内容

**基线快照**只存储游戏版本的**英文原文**。不存储中文翻译内容。

### 8.2 快照建立时机

快照**必须**在游戏目录为英文状态时建立。允许的时机：
- Steam 刚更新完毕（未部署翻译）
- Steam 验证文件完整性后（未部署翻译）

禁止的时机：
- 翻译已部署后（XML/arcenui 已被中文覆盖）

### 8.3 快照版本对照

| 版本 A（旧快照中的英文） | 版本 B（当前提取的英文） | 意义 |
|------------------------|------------------------|------|
| v5.825 | v5.831 | 检测更新：报告差异 |
| v5.831（刚更新后） | v5.831（刚更新后） | 建立新基线：`-snapshot -force` |
| — | — | 日常翻译后：**不需要**更新快照 |

### 8.4 快照状态机

```
无快照 ──[ -snapshot ]──→ 有快照 (英文基线)
   │                           │
   │                    游戏更新 + Steam 恢复
   │                           │
   │                           ▼
   │                    游戏为英文, 快照为旧版
   │                    运行检测 → 报告
   │                    [-snapshot -force] → 更新基线
   │                           │
   │                    部署翻译 (游戏变中文)
   │                           │
   │                    等待下次 Steam 更新...
   │                           │
   └───────────────────────────┘
```

## 九、与现有工具的兼容性

| 现有工具 | 关系 |
|---------|------|
| `check_translation.ps1` | 被 `check_update.ps1` 完全替代，保留 XML 白名单逻辑 |
| `deploy.ps1` | 增加部署前检查：必须有基线快照且版本匹配游戏版本 |
| `build.ps1` | 不变 |
| `patch_arcenui.py` | 扩展 extract 命令以支持快照导出 |
