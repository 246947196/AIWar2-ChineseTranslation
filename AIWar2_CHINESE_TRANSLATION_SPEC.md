# AI War 2 汉化项目规范

## 一、技术方案

**两种手段：DLL 替换 + XML 文件整体替换**

| ❌ 已排除的方案 | 原因 |
|----------------|------|
| XMLMod (游戏原生覆盖机制) | DLL 覆盖机制有问题 |
| Harmony 运行时 Patch | 实测出现大量 BUG |
| AutoTranslator | 不能全部翻译 |

## 二、原理

BepInEx Preloader 在游戏程序集加载前调用 Patcher，通过 Mono.Cecil 读取并替换 `PatchedAssemblies/` 中的 DLL 文件。
游戏实际加载的是修改后的 DLL，原始文件位于 `AIWar2_Data/Managed/` 不受影响。

## 三、游戏 DLL 加载链路

### 3.1 主程序集加载路径

游戏启动时从 `AIWar2_Data/Managed/` 加载以下核心程序集：

| 加载顺序 | DLL 文件 | 所属 | 说明 |
|----------|---------|------|------|
| 1 | `ArcenAIW2Core.dll` | 核心逻辑 | 游戏主逻辑、实体、阵营、舰队、科技等 |
| 2 | `ArcenUniversal.dll` | 通用/UI | UI 组件、通用工具、字体、输入、网络等 |
| 3 | `ArcenAIW2Visualization.dll` | 可视化 | 渲染、特效、模型、Shader 等 |
| 4 | `AIWarExternalCode.dll` | 外部代码 | Mod 可扩展的游戏逻辑 (在 `External/` 子目录) |
| 5 | 扩展包/Mod 程序集 | — | 如 SpireRises、ZenithOnslaught 等 |

### 3.2 Preloader Patcher 拦截机制

1. BepInEx `winhttp.dll` 注入进程
2. `doorstop_config.ini` 指向 `BepInEx/core/BepInEx.Preloader.dll`
3. Preloader 扫描 `BepInEx/patchers/` 下的 `AssemblyRedirector.dll`
4. `AssemblyRedirector` 实现 `IBepInExPreloaderPatcher`，在 `Patch(AssemblyDefinition)` 中：
   - 对比程序集名称
   - 若匹配 `ArcenAIW2Core`/`ArcenUniversal`/`ArcenAIW2Visualization`
   - 从 `PatchedAssemblies/` 读取同名修改版 DLL
   - 用 `ModuleDefinition.ReadModule(patchedPath)` 替换 `AssemblyDefinition.MainModule`
5. 游戏后续加载的是**已替换的模块**，原始 `AIWar2_Data/Managed/` 文件不被读取

## 四、修改方式

### 4.1 有源码的部分 (CodeExternal/)

游戏自带 C# 源码，可用 VS2022 打开 `AIWarExternalCode.sln` 直接编译。

| 项目 | 输出 DLL | 主要内容 |
|------|---------|---------|
| `AIWarExternalCode` | `AIWarExternalCode.dll` | UI 窗口、工具提示、入侵实现、设置菜单 |
| `AIWarExternalVisualizationCode` | `AIWarExternalVisualizationCode.dll` | 可视化逻辑 |
| `AIWarExternalDeepProcessingCode` | `AIWarExternalDeepProcessingCode.dll` | 深度处理逻辑 |

**修改流程**: 编辑 .cs 源码 → VS2022 编译 → 替换 `AIWar2_Data/Managed/` 下的 DLL&PDB

### 4.2 无源码的部分 (需反编译)

以下 DLL 没有源码，需要通过 dnSpy/ILSpy 反编译后修改：

| DLL | 内容 |
|-----|------|
| `ArcenAIW2Core.dll` | 游戏核心逻辑 |
| `ArcenUniversal.dll` | UI/通用框架 |

**修改流程**: 复制 DLL → dnSpy/ILSpy 反编译 → 修改字符串 → 重新编译 → 放入 `PatchedAssemblies/` → Preloader Patcher 自动替换加载

### 4.3 XML 文件整体替换

`GameData/Configuration/` 下的 XML 文件包含实体名称、描述、日志等文本内容。直接替换整个 XML 文件，不使用 XMLMod 的 DLL 覆盖机制。

**修改流程**: 编辑 XML 文件 → 直接替换 `GameData/Configuration/` 下的对应文件

> 注: 游戏更新后需确认 XML 结构无破坏性变更。Steam 验证会还原 GameData/ 文件，需要重新替换。

## 五、组件清单

### 5.1 BepInEx 框架 (必须)

| 文件/目录 | 说明 |
|-----------|------|
| `winhttp.dll` | BepInEx 加载入口 |
| `doorstop_config.ini` | 加载配置 |
| `BepInEx/core/` | BepInEx 核心库 |
| `BepInEx/patchers/AssemblyRedirector.dll` | Preloader Patcher |

### 5.2 字体插件 (核心)

| 插件 | 说明 |
|------|------|
| `BepInEx/plugins/I18NFont4UnityGame/` | ✅ 核心 — 中文字体渲染，必须保留 |
| `BepInEx/plugins/DynamicFontLoader/` | ❌ 备选方案，已禁用 |

当前使用的字体: `sarasa_gothic` (更纱黑体)

## 六、禁止事项

- 禁止修改 `AIWar2_Data/Managed/` 下任何原始 DLL
- 禁止使用 Harmony 运行时方法 patch
- 禁止使用 XMLMod 的 DLL 覆盖机制
- `PatchedAssemblies/` 中的 DLL 版本必须与游戏版本精确匹配

## 七、卸载方法

删除以下即可完全卸载汉化：
- `BepInEx/patchers/AssemblyRedirector.dll`
- `PatchedAssemblies/`
