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

1. 从游戏目录复制原版 XML 文件到翻译文件夹
2. 在 `AIWar2_ChineseTranslation/GameData/Configuration/` 中编辑 XML 文件
3. 运行 `deploy.ps1` 部署到游戏目录
4. 启动游戏验证翻译效果
5. 提交翻译到 Git 仓库

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
- 翻译时只修改 `default_display_name` 属性，不添加新条目

## 七、卸载方法

删除 `AIWar2_ChineseTranslation/` 文件夹，然后删除游戏目录中部署的文件：
- `BepInEx/patchers/AssemblyRedirector.dll`
- `BepInEx/plugins/I18NFont4UnityGame/`
- `GameData/Configuration/` 下已替换的 XML 文件
