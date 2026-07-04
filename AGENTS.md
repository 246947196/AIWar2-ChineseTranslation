# AI War 2 汉化项目规范

本文件是 AI 助手的项目上下文。项目详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

**唯一方案：Preloader Patcher + DLL 替换**

- ❌ XMLMod — DLL 覆盖机制有问题，已排除
- ❌ Harmony 运行时 Patch — 实测巨量 BUG，已排除
- ❌ AutoTranslator — 不能全部翻译，已排除
- ✅ **DLL 替换** — 修改 DLL 后放入 `PatchedAssemblies/`，Preloader Patcher 在启动时替换加载
- ✅ **I18NFont4UnityGame** — 中文字体渲染，核心组件

## 已验证的技术方案

| 方案 | 状态 |
|------|------|
| dll_search_path_override (doorstop) | ❌ 未生效 |
| Preloader Patcher (BepInEx patchers/) | ✅ 生效 |
| I18NFont4UnityGame 字体 | ✅ 生效，使用 sarasa_gothic |
| Harmony Patch | ❌ 禁用，实测巨量 BUG |
| XMLMod 内容翻译 | ❌ 禁用，DLL 覆盖机制有问题 |
| AutoTranslator | ❌ 禁用，不能全部翻译 |

## 关键约定

- 禁止修改 AIWar2_Data/Managed/ 下任何原始 DLL
- 禁止 Harmony 运行时方法 patch
- 禁止 XMLMod 的 DLL 覆盖机制
- DLL 修改采用完整文件替换，通过 Preloader Patcher 加载
- PatchedAssemblies/ 版本必须与游戏版本精确匹配
- 删除 BepInEx/patchers/ + PatchedAssemblies/ 即可完全卸载
