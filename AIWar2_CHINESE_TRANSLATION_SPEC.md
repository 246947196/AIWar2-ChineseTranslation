# AI War 2 汉化项目规范

**适配游戏版本: 5.825 (June 30th, 2026)**

## 一、技术方案

**XML 文件整体替换 + DLL 替换 (Preloader Patcher)**

| ❌ 已排除的方案 | 原因 |
|----------------|------|
| XMLMod (游戏原生覆盖机制) | DLL 覆盖机制有问题 |
| Harmony 运行时 Patch | 实测出现大量 BUG |
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
│   └── plugins/
│       └── I18NFont4UnityGame/
│           ├── I18NFont4UnityGame.dll ← 中文字体插件
│           └── sarasa_gothic          ← 字体文件
├── GameData/Configuration/            ← 翻译后的 XML 文件
├── deploy.ps1                         ← 一键部署脚本
├── check_translation.ps1              ← 检查脚本
├── AIWar2_CHINESE_TRANSLATION_SPEC.md ← 规范文档
├── AGENTS.md                          ← AI 助手上下文
└── translated_files.txt               ← 翻译记录
```

## 三、工作流程

### 3.1 翻译流程

1. 在 `AIWar2_ChineseTranslation/GameData/Configuration/` 中编辑 XML 文件
2. 运行 `deploy.ps1` 部署到游戏目录
3. 启动游戏验证翻译效果
4. 提交翻译到 Git 仓库

### 3.2 首次部署

1. 克隆仓库到本地
2. 运行 `deploy.ps1` 将翻译文件部署到游戏目录

### 3.3 游戏更新后

1. Steam 更新游戏
2. 运行 `check_translation.ps1` 检查哪些文件被覆盖
3. 重新翻译被覆盖的文件
4. 运行 `deploy.ps1` 重新部署
5. 更新规范中的版本号 → 提交并打 tag

## 四、技术原理

### 4.1 XML 文件整体替换

`GameData/Configuration/` 下的 XML 文件包含实体名称、描述、日志等文本内容。直接替换整个 XML 文件。

### 4.2 DLL 替换 (Preloader Patcher)

BepInEx Preloader 在游戏程序集加载前调用 Patcher，通过 Mono.Cecil 读取并替换 `PatchedAssemblies/` 中的 DLL 文件。

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
- 禁止使用 Harmony 运行时方法 patch
- 禁止使用 XMLMod 的 DLL 覆盖机制

## 七、卸载方法

删除 `AIWar2_ChineseTranslation/` 文件夹，然后删除游戏目录中部署的文件：
- `BepInEx/patchers/AssemblyRedirector.dll`
- `BepInEx/plugins/I18NFont4UnityGame/`
- `GameData/Configuration/` 下已替换的 XML 文件

---

## 八、DLL 源码汉化（已完成）

### 8.1 概述

三个外部 DLL 项目有完整源码（位于 `CodeExternal/`），已编译并汉化硬编码的 UI 文本。

### 8.2 已汉化的 DLL 项目

| 项目 | 源码位置 | 汉化内容 | 状态 |
|------|---------|---------|------|
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/UIs/ | 主菜单、设置、存档、侧边栏等 UI 文本 | ✅ 已完成 |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | 聊天消息、少量 UI 文本 | ✅ 已完成 |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | 银河地图显示模式文本 | ✅ 已完成 |

### 8.3 目录结构

```
AIWar2_ChineseTranslation/
├── DLLSource/                                    ← 汉化源码
│   ├── AIWarExternalCode/
│   │   ├── src/                                  ← 汉化后的 C# 源码
│   │   └── AIWarExternalCode.csproj
│   ├── AIWarExternalDeepProcessingCode/
│   │   ├── src/
│   │   └── AIWarExternalDeepProcessingCode.csproj
│   └── AIWarExternalVisualizationCode/
│       ├── src/
│       └── AIWarExternalVisualizationCode.csproj
├── DLLBin/                                       ← 编译产物
│   ├── AIWarExternalCode.dll                     (3778 KB)
│   ├── AIWarExternalDeepProcessingCode.dll        (1762 KB)
│   └── AIWarExternalVisualizationCode.dll         (228 KB)
├── BepInEx/                                      ← 已有
├── GameData/                                     ← 已有
├── build.ps1                                     ← DLL 编译脚本
├── deploy.ps1                                    ← 部署脚本（含 DLL 部署）
├── translated_files.txt                          ← 已有
├── AGENTS.md                                     ← 已有
└── AIWar2_CHINESE_TRANSLATION_SPEC.md            ← 已有
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
2. **所有含中文的 .cs 源文件必须保存为 UTF-8 with BOM 编码**，否则游戏中文会显示为方框。Unity/Mono 不识别无 BOM 的 UTF-8 中文字符。

### 8.5 csproj 修改

从 `CodeExternal/` 复制到 `DLLSource/` 后需修改：

1. **引用路径** — `..\..\ReliableDLLStorage\` → `..\..\..\ReliableDLLStorage\`
2. **引用路径** — `..\..\GameData\` → `..\..\..\GameData\`
3. **输出路径** — `..\..\GameData\ModdableLogicDLLs\` → `..\..\DLLBin\`
4. **禁用 PostBuildEvent** — 原版有批处理脚本拷贝，汉化版不需要

### 8.6 编译流程

```powershell
# 一键编译（推荐）
.\build.ps1

# 或手动编译
$roslynDir = "C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

# 按顺序编译（后两个依赖第一个的产物）
& $msbuild DLLSource\AIWarExternalCode\AIWarExternalCode.csproj /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir"
& $msbuild DLLSource\AIWarExternalDeepProcessingCode\AIWarExternalDeepProcessingCode.csproj /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir"
& $msbuild DLLSource\AIWarExternalVisualizationCode\AIWarExternalVisualizationCode.csproj /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir"

# 编译后确保所有含中文的 .cs 文件为 UTF-8 with BOM
$baseDir = "DLLSource"
Get-ChildItem $baseDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    if ($content -match '[\u4e00-\u9fff]') {
        $utf8BOM = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($_.FullName, $content, $utf8BOM)
    }
}
```

### 8.7 部署

`deploy.ps1` 已集成 DLL 部署，运行 `.\deploy.ps1` 即可部署所有翻译文件（XML + DLL）。

### 8.8 已知限制

- 部分大型文件（如 `Window_InGameHoverEntityInfo.cs` 8390 行、`Window_PrototypeInGameHoverEntityInfo.cs` 9824 行）的长篇描述文本未翻译，保留英文
- 翻译时只能替换字符串字面量，不能修改代码逻辑
- 编译器版本必须与原版一致（Roslyn 4.12.0），否则会产生运行时错误
- **文件编码**：所有含中文的 .cs 文件必须为 UTF-8 with BOM 编码，否则游戏中文显示为方框。翻译完成后需执行 BOM 转换脚本

### 8.9 汉化统计

| 项目 | 已翻译文件数 | 未翻译文件数 |
|------|------------|------------|
| AIWarExternalCode | 48+ | ~550 |
| AIWarExternalDeepProcessingCode | 1 | 132 |
| AIWarExternalVisualizationCode | 3 | 42 |

### 8.10 翻译注意事项

1. **使用 Edit 工具**：翻译时必须使用 Edit 工具逐字符串替换，禁止使用 Write 覆写整个文件
2. **只改引号内内容**：只能替换 `"..."` 内的文本，不能修改引号外的任何代码
3. **保留插值和标签**：`$"{variable}"` 中的变量部分保持不变，`<color>` 等标签保持不变
4. **编译验证**：每翻译完一个文件后编译验证，0 错误再继续下一个
5. **BOM 编码**：翻译完成后对所有含中文的 .cs 文件执行 UTF-8 BOM 转换
6. **翻译优先级**：UI 文件（src/UIs/）优先，游戏逻辑文件（BaseInfo/、Sim/ 等）通常不需要翻译
7. **禁止中文引号**：C# 字符串中不能使用 `""`（中文左右双引号），会被编译器误认为字符串分隔符。必须用 `''`（单引号）替代。例如：`"点击'是'确认"` 而非 `"点击"是"确认"`
8. **引号嵌套**：如果翻译文本中需要引用按钮名称或其他 UI 元素，使用 `'单引号'` 包裹，不要使用 `"双引号"`

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

`check_translation.ps1` 白名单已更新，包含以下不需要翻译的文件类型：
- External* 系列文件（纯数值配置）
- 其他弃用/调试文件

### 9.6 翻译统计

| 类别 | 新增文件数 | 总翻译文件数 |
|------|-----------|-------------|
| DLC JournalEntries | 14 | 14 |
| DLC Achievement | 3 | 3 |
| ExoticShips 模组 | 5 | 5 |
| **合计** | **22** | **22** |
