# AI War 2 汉化项目规范

本文件是 AI 助手的项目上下文。项目详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

**XML 文件整体替换 + DLL 替换 (Preloader Patcher)**

- ❌ XMLMod — DLL 覆盖机制有问题，已排除
- ❌ Harmony 运行时 Patch — 实测巨量 BUG，已排除
- ❌ AutoTranslator — 不能全部翻译，已排除
- ✅ **XML 文件整体替换** — 直接替换 `GameData/Configuration/` 下的 XML 文件
- ✅ **DLL 替换** — 修改 DLL 后放入 `PatchedAssemblies/`，Preloader Patcher 在启动时替换加载
- ✅ **I18NFont4UnityGame** — 中文字体渲染，核心组件

## 工作流程

1. 在 `AIWar2_ChineseTranslation/GameData/Configuration/` 中编辑 XML 文件
2. 运行 `deploy.ps1` 部署到游戏目录
3. 启动游戏验证翻译效果
4. 提交翻译到 Git 仓库

## 关键约定

- 禁止修改 AIWar2_Data/Managed/ 下任何原始 DLL
- 禁止 Harmony 运行时方法 patch
- 禁止 XMLMod 的 DLL 覆盖机制
- 翻译和使用分开，但文件结构一致
