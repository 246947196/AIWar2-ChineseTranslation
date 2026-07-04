# AI War 2 汉化项目规范

本文件是 AI 助手的项目上下文。详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

XML 文件整体替换 + DLL 替换 (Preloader Patcher)

## 工作流程

1. 编辑翻译文件夹里的 XML
2. 运行 `deploy.ps1` 部署
3. 启动游戏验证
4. 提交到 Git

## 关键约定

- Git 分支统一使用 `main`
- 翻译和使用分开，文件结构一致
