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

## XML 翻译范围

**重要：并非所有 XML 文件都需要翻译。**

### 需要翻译的文件类型
- `GameEntity/` - 实体名称和描述
- `JournalEntries/` - 剧情日志
- `Achievement/` - 成就
- `Tips/` - 游戏提示
- `Tutorials/` - 教程
- `SpecialFaction/` - 阵营描述
- `HackingType/` - 黑客类型描述
- `ScourgeTypeData/` - 天灾战士描述

### 不需要翻译的文件类型（纯配置文件）
- `External*` - 外部接口配置（技术标识符）
- `Balance_*` - 数值平衡配置
- `UIPrefab/` - UI 预制体路径
- `UIWindow/` - 窗口配置（类名）
- `AIShipGroup*` - AI 舰队分组（编号）
- `CameraType/` - 相机类型
- `FramerateType/` - 帧率类型
- `ParticlePattern/` - 粒子效果路径
- `SpaceboxDefinition/` - 天空盒路径
- `PlanetDefinition/` - 星球定义（数值）
- `TextEmbededSprites/` - 图标配置
- `TextStyles/` - 文本样式
- `TextVarMaps/` - 文本变量映射
- `SurrogateTable/` - 代理表（编号）
- `SpecialFactionProcessingGroup/` - 处理组（编号）

### 判断方法
检查文件是否包含 `Description="..."` 且内容为英文句子。如果是，则需要翻译；如果是技术标识符或数值，则不需要翻译。

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

## XML 文件编码规范

所有 `.xml` 文件必须为 **UTF-8 无 BOM** 编码。游戏 XML 解析器不支持 BOM，添加 BOM 会导致 NullReferenceException 和设置丢失。

### 验证方法

```powershell
$bytes = [System.IO.File]::ReadAllBytes($file)
$hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
Write-Output "BOM: $hasBom"  # 应为 False
```

### 去除 BOM 方法

```powershell
$bytes = [System.IO.File]::ReadAllBytes($file)
if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    $noBomBytes = [byte[]]::new($bytes.Length - 3)
    [Array]::Copy($bytes, 3, $noBomBytes, 0, $bytes.Length - 3)
    [System.IO.File]::WriteAllBytes($file, $noBomBytes)
}
```

### 注意

- **C# 源码 (.cs)** 必须使用 UTF-8 with BOM（编译器要求）
- **XML 文件 (.xml)** 必须使用 UTF-8 无 BOM（游戏解析器要求）

## 中文字体支持

使用 I18NFont4UnityGame 插件为游戏添加中文字符显示支持。

### 组件

| 组件 | 路径 | 说明 |
|------|------|------|
| 插件 DLL | `BepInEx\plugins\I18NFont4UnityGame\I18NFont4UnityGame.dll` | 插件主程序 |
| 字库文件 | `BepInEx\plugins\I18NFont4UnityGame\sarasa_gothic` | 中文字体文件（更纱黑体） |
| 配置文件 | `BepInEx\config\xiaoye97.I18NFont4UnityGame.cfg` | 插件配置 |

### 配置文件内容

```ini
## Settings file was created by plugin I18NFont4UnityGame v1.0
## Plugin GUID: xiaoye97.I18NFont4UnityGame

[config]

## put font package to <GameName>/BepInEx/plugins/I18NFont4UnityGame
# Setting type: String
# Default value: unifont
FontName = sarasa_gothic
```

### 字库文件说明

- **字体名称**：sarasa_gothic（更纱黑体）
- **用途**：为不支持中文的游戏提供中文字符显示
- **文件格式**：Unity AssetBundle（文件头为 "UnityFS"）
- **文件大小**：约 9.9 MB
- **说明**：这是一个 Unity 资源包文件，包含了更纱黑体字体，由 I18NFont4UnityGame 插件加载使用
