# DLL 源码汉化实施计划

**目标：** 汉化三个外部 DLL 项目中的硬编码英文 UI 文本

## 项目概览

| 项目 | 源码文件数 | UI 字符串量 | 优先级 |
|------|-----------|------------|--------|
| AIWarExternalCode | ~330 | ~500+ | 高（主菜单、设置、存档、侧边栏等） |
| AIWarExternalVisualizationCode | ~45 | ~5-10 | 低（银河地图模式文本） |
| AIWarExternalDeepProcessingCode | ~140 | ~1-2 | 极低（仅1条聊天消息） |

## 依赖关系

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

**编译顺序：** AIWarExternalCode → AIWarExternalDeepProcessingCode + AIWarExternalVisualizationCode

---

## 实施步骤

### 阶段一：项目搭建（步骤 1-3）

#### 步骤 1：复制源码到 DLLSource/

```
AIWar2_ChineseTranslation/DLLSource/
├── AIWarExternalCode/
│   ├── src/                    ← 从 CodeExternal/AIWarExternalCode/src/ 复制
│   ├── AIWarExternalCode.csproj
│   └── AIWarExternalCode.csproj.user (如有)
├── AIWarExternalDeepProcessingCode/
│   ├── src/
│   ├── AIWarExternalDeepProcessingCode.csproj
│   └── AIWarExternalDeepProcessingCode.csproj.user (如有)
└── AIWarExternalVisualizationCode/
    ├── src/
    ├── AIWarExternalVisualizationCode.csproj
    └── AIWarExternalVisualizationCode.csproj.user (如有)
```

#### 步骤 2：修改 csproj 引用路径

三个项目的 csproj 都使用 `<Choose>` 块切换项目引用/DLL 引用。汉化版需要：

1. **禁用项目引用分支** — 将 `ArcenUseProjectReferences` 默认值改为 `false`（或直接删除 `<Choose>` 块中的 ProjectReferences 分支）
2. **修改 DLL 引用路径** — 从相对于 `CodeExternal/` 改为相对于 `DLLSource/`

示例（AIWarExternalCode.csproj）：

原路径：
```xml
<HintPath>..\..\ReliableDLLStorage\ArcenDLLs\ArcenUniversal.dll</HintPath>
```

新路径（相对于 `DLLSource/AIWarExternalCode/`）：
```xml
<HintPath>..\..\..\ReliableDLLStorage\ArcenDLLs\ArcenUniversal.dll</HintPath>
```

对于引用其他项目编译产物的路径：
```xml
<!-- AIWarExternalDeepProcessingCode 引用 AIWarExternalCode 的产物 -->
<HintPath>..\..\GameData\ModdableLogicDLLs\AIWarExternalCode.dll</HintPath>
```
此路径不变（因为输出目录不变）。

#### 步骤 3：确认编译通过

```powershell
cd AIWar2_ChineseTranslation\DLLSource\AIWarExternalCode
msbuild /p:Configuration=Release

cd ..\AIWarExternalDeepProcessingCode
msbuild /p:Configuration=Release

cd ..\AIWarExternalVisualizationCode
msbuild /p:Configuration=Release
```

编译产物应输出到 `AIWar2_ChineseTranslation/DLLBin/`（需修改 csproj 的 OutputPath）。

---

### 阶段二：识别待翻译字符串（步骤 4）

按优先级扫描以下目录中的硬编码英文字符串：

**高优先级（AIWarExternalCode/src/UIs/）：**
- `MainMenu/` — 主菜单、存档、设置、阵营选择
- `MasterMenu/` — 游戏内菜单、存档管理
- `InGamePassiveDisplay/` — 资源栏、侧边栏、通知
- `LobbyGameSetup/` — 大厅设置
- `Window_SettingsMenu.cs` — 设置界面
- `Window_FactionsWindow.cs` — 阵营窗口
- 各种 Modal 对话框

**中优先级（AIWarExternalCode/src/EntityText/）：**
- 实体文本格式化（属性、描述、统计）

**低优先级：**
- AIWarExternalVisualizationCode/src/GalaxyMapDisplayModes/ — 银河地图模式名称
- AIWarExternalDeepProcessingCode — 几乎无可翻译内容

---

### 阶段三：逐文件汉化（步骤 5）

对每个包含 UI 字符串的文件：

1. 读取源文件
2. 识别所有硬编码英文字符串（字符串字面量、插值字符串）
3. 替换为中文翻译
4. 保持代码逻辑不变
5. 确保字符串编码正确（UTF-8 with BOM）

**翻译原则：**
- 仅翻译面向玩家的 UI 文本
- 不翻译开发者调试信息、异常消息、日志输出
- 不翻译内部标识符、枚举值名称
- 保持变量名和方法名英文不变
- 中文标点使用全角（，。！？）

---

### 阶段四：编译测试（步骤 6）

```powershell
# 清理旧产物
Remove-Item AIWar2_ChineseTranslation\DLLBin\*.dll -ErrorAction SilentlyContinue

# 按顺序编译
msbuild AIWar2_ChineseTranslation\DLLSource\AIWarExternalCode\AIWarExternalCode.csproj /p:Configuration=Release
msbuild AIWar2_ChineseTranslation\DLLSource\AIWarExternalDeepProcessingCode\AIWarExternalDeepProcessingCode.csproj /p:Configuration=Release
msbuild AIWar2_ChineseTranslation\DLLSource\AIWarExternalVisualizationCode\AIWarExternalVisualizationCode.csproj /p:Configuration=Release

# 验证产物
Get-ChildItem AIWar2_ChineseTranslation\DLLBin\*.dll
```

---

### 阶段五：部署集成（步骤 7）

修改 `deploy.ps1`，新增 DLL 部署逻辑：

```powershell
# Deploy translated DLLs
$dllBinDir = "$translationDir\DLLBin"
if (Test-Path $dllBinDir) {
    $dllFiles = Get-ChildItem "$dllBinDir\*.dll"
    foreach ($dll in $dllFiles) {
        Copy-Item $dll.FullName "$gameDir\GameData\ModdableLogicDLLs\" -Force
        Write-Host "Deployed DLL: $($dll.Name)"
    }
}
```

---

## 风险与注意事项

1. **编译环境要求** — 需要 .NET Framework 4.7.2 SDK 和 MSBuild
2. **DLL 版本匹配** — ReliableDLLStorage 中的 DLL 必须与游戏版本匹配
3. **字符串长度** — 中文通常比英文短，但需注意 UI 布局是否受影响
4. **编码问题** — 确保所有源文件保存为 UTF-8 with BOM，否则 Unity/Mono 可能无法正确读取中文
5. **Harmony 兼容** — AIWarExternalCode 引用了 0Harmony，编译时需要但运行时由游戏加载
