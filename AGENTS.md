# AI War 2 汉化项目上下文

详细规范见 `AIWar2_CHINESE_TRANSLATION_SPEC.md`。

## 核心架构

XML 文件整体替换 + DLL 源码编译替换 + AssetBundle 拦截重定向

## 工作流程

1. 编辑 `GameData/Configuration/` 中 XML、`DLLSource/` 中 C# 源码、或替换 `AssetBundles_Win/arcenui`
2. 运行 `build.ps1` 编译（修改 DLL 后需要）
3. 运行 `deploy.ps1` 部署
4. 启动游戏验证
5. 提交 Git

## DLL 项目一览

| 项目 | 源码 | 编译 | 翻译 |
|------|------|------|------|
| AIWarExternalCode | DLLSource/AIWarExternalCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalDeepProcessingCode | DLLSource/AIWarExternalDeepProcessingCode/src/ | ✅ | ✅ 完成 |
| AIWarExternalVisualizationCode | DLLSource/AIWarExternalVisualizationCode/src/ | ✅ | ✅ 完成 |
| ArcenUIAssetRedirect（BepInEx 插件） | DLLSource/ArcenUIAssetRedirect/src/ | ✅ | ✅ 完成 |
| ArcenUniversal（反编译） | DLLSource/ArcenUniversal/ | ✅ 0 错误 | ⏳ |
| ArcenAIW2Core（反编译） | DLLSource/ArcenAIW2Core/ | ❌ | ⏳ |
| ArcenAIW2Visualization（反编译） | DLLSource/ArcenAIW2Visualization/ | ❌ | ⏳ |

## 编译

```powershell
.\build.ps1
```

编译器：Roslyn 4.12.0（`C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472\csc.exe`）
MSBuild：`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`
目标框架：
- 外部代码/反编译项目：.NET Framework 4.7.1
- BepInEx 插件 (ArcenUIAssetRedirect)：.NET Framework 4.7.2
引用：`..\..\..\ReliableDLLStorage\`（插件额外引用 `..\..\..\BepInEx\core\`）

## 翻译规则

1. **Edit 工具**逐字符串替换，禁止 Write 覆写整个文件
2. 只改 `"..."` 内文本，不碰引号外代码
3. 保留 `{变量}` 和 `<color>` 标签
4. 每翻译完一个 DLL 编译验证，0 错误继续
5. 禁止中文引号 `""`，用 `''` 替代
6. Debug 日志、内部标识符不翻译
7. **反编译项目**：只改 `"..."` 内字符串，不改 csproj 配置、GlobalUsings.cs、编译修复代码
