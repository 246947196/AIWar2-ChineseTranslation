# Core DLL 反编译汉化方案

**适配游戏版本：5.825 (June 30th, 2026)**

## 一、背景

已有汉化项目覆盖了 XML 文件翻译和三个有源码的 DLL（AIWarExternalCode 等）翻译。现需汉化三个无源码的核心 DLL：

| DLL | 大小 | 说明 |
|-----|------|------|
| ArcenUniversal.dll | 1280 KB | UI 组件、通用工具、输入、网络等（独立，无依赖） |
| ArcenAIW2Core.dll | 2016 KB | 游戏主逻辑、实体、阵营、舰队、科技等（依赖 ArcenUniversal） |
| ArcenAIW2Visualization.dll | 162 KB | 渲染、特效、模型、Shader 等（依赖 ArcenUniversal、ArcenAIW2Core） |

## 二、技术方案

### 2.1 总体方案

**反编译 → 创建 C# 项目 → 编译验证 → 逐文件翻译 → 重新编译**

与现有有源码 DLL 的工作流保持一致。

### 2.2 已排除的方案

| 方案 | 原因 |
|-----|------|
| Harmony 运行时 Patch | 实测出现大量 BUG |
| Mono.Cecil IL 替换 | 用户有反编译经验，选择直接改源码 |
| 脚本自动替换 | 用户要求手工 Edit，避免脚本问题 |

### 2.3 反编译工具

**ilspycmd 8.2**（dnSpy 6.1.8 被否决原因：缺失 yield 迭代器状态机类、泛型输出 `!0`/`!1` 占位符、基于过时 ICSharpCode.Decompiler 2.3.1）。

```powershell
dotnet exec --roll-forward Major "$env:TEMP\ilspycmd82\tools\net6.0\any\ilspycmd.dll" <dll> -p -v -o <output_dir>
```

导出目录结构（无 `src/` 子文件夹，ILSpy 直接输出到目标目录）：

```
DLLSource/
├── ArcenUniversal/              ← ilspycmd 反编译导出
│   ├── ArcenUniversal.csproj     ← SDK 风格，需转为旧格式
│   ├── GlobalUsings.cs           ← 编译修复：Object 歧义
│   └── *.cs
├── ArcenAIW2Core/
├── ArcenAIW2Visualization/
└── (已有 AIWarExternalCode/ 等)
```

### 2.4 编译器

与现有项目一致，使用 **Roslyn 4.12.0（C# 13.0）**。

### 2.5 项目引用关系

```
ArcenUniversal          → 依赖: UnityEngine.CoreModule 等 Unity 标准程序集（独立，不依赖其他核心 DLL）
ArcenAIW2Core           → 依赖: ArcenUniversal, UnityEngine
ArcenAIW2Visualization  → 依赖: ArcenAIW2Core, ArcenUniversal, UnityEngine
```

编译顺序：**ArcenUniversal → ArcenAIW2Core → ArcenAIW2Visualization**

### 2.6 csproj 格式

反编译产出的 SDK 风格 csproj 需转为旧格式（ToolsVersion="15.0"），因为使用 MSBuild 4.8 编译：

| 项目 | 旧格式关键配置 |
|------|--------------|
| TargetFrameworkVersion | v4.7.1 |
| LangVersion | 11.0 |
| CscToolPath | Roslyn 4.12.0 |
| Reference HintPath | 指向 `..\..\..\ReliableDLLStorage\` |

### 2.7 已知反编译修复模式

| 问题 | 原因 | 修复方式 |
|------|------|---------|
| `op_Implicit` 调用 | ILSpy 反编译隐式运算符为 `Type.op_Implicit(x)`，Roslyn 4.12（C# 13）禁止直接调用 | 替换为 `(bool)(Object)x` 或 `(TargetType)x` |
| `_002Ector` | IL 构造函数 `.ctor` 被 ILSpy 转义为 `_002Ector`，C# 不能直接调用 | `default(T) + ._002Ector(args)` → `new T(args)` |
| `(Type)(ref var)` | ILSpy 8.x 对 ref 变量的类型转换输出冗余 | `((Type)(ref var))` → `var` |
| `System.Numerics` 命名空间冲突 | Mat.cs 同时使用 `System.Numerics` 和 `UnityEngine` 的 Vector2/3/4/Quaternion | 移除 `using System.Numerics;`，显式限定 System.Numerics 类型 |
| `Object` 歧义 | `System.Object` 和 `UnityEngine.Object` 冲突 | `global using Object = UnityEngine.Object;` |
| yield 状态机 | ILSpy 正确生成 `_003C`/`_003E` 转义标识符 | 保留，正常编译 |
| 泛型参数 | ILSpy 输出正确类型名（无 `!N`） | 无需处理 |

## 三、字符串分类统计

通过 Mono.Cecil 分析 IL 指令得出的数据：

| 分类 | 数量 | 说明 |
|------|------|------|
| LDSTR 总数 | ~16,800 | 所有 ldstr 指令（含重复） |
| 唯一字符串 | 9,711 | 去重后 |
| Debug/日志/错误 | 764 | **不翻译** |
| 内部标识符/Key | 5,667 | **不翻译** |
| 格式化模板 | 226 | 部分需要翻译 |
| 句子/游戏文本 | 1,531 | ✅ 需要翻译 |
| 其他（可能 UI） | 1,523 | 需要人工判断 |
| **实际需翻译** | **~2,000-3,000** | |

## 四、工作流程

### 4.1 反编译（一次性）

用 ilspycmd 8.2 分别反编译三个 DLL：
```
dotnet exec --roll-forward Major "$env:TEMP\ilspycmd82\tools\net6.0\any\ilspycmd.dll" <dll> -p -v -o <output_dir>
```

### 4.2 项目配置（一次性）

- 将 SDK 风格 csproj 转为旧格式（ToolsVersion="15.0"）
- 引用指向 `..\..\..\ReliableDLLStorage\`
- 输出目录指向 `..\..\DLLBin\`
- 添加 GlobalUsings.cs（`global using Object = UnityEngine.Object;`）
- 用 `**/*.cs` wildcard 替代显式文件列表

### 4.3 编译验证 + 修复

按依赖顺序：ArcenUniversal → ArcenAIW2Core → ArcenAIW2Visualization

反编译修复：
1. 替换 `Object.op_Implicit((Object)(object)x)` → `(bool)(Object)x`
2. 替换 `Vector2/4.op_Implicit(x)` → `(Vector2/4)x`
3. 替换 `val._002Ector(args)` → `val = new Type(args)`
4. 处理缺失类型引用（Unity 枚举如 InputButton、Axis 等）
5. 其他编译器版本差异修复

### 4.4 逐文件翻译（后续轮次）

- 使用 **Edit** 工具逐字符串替换
- 只改 `"..."` 内的文本，不碰引号外的任何代码
- 保留 `{变量}` 插值和 `<color>` 等标签
- 禁止中文双引号 `""`，使用 `''` 替代
- Debug 日志、内部标识符不翻译
- 每翻译完一个 DLL 就编译验证一次

### 4.5 构建

扩展 `build.ps1`，增加第二阶段编译：
```
第一阶段（已有）: AIWarExternalCode → AIWarExternalDeepProcessingCode → AIWarExternalVisualizationCode
第二阶段（新增）: ArcenUniversal → ArcenAIW2Core → ArcenAIW2Visualization
```

输出目录：`DLLBin/`

### 4.6 部署

扩展 `deploy.ps1`，将编译后的核心 DLL 从 `DLLBin/` 复制到 `PatchedAssemblies/`。

## 五、版本号管理

- 每次游戏更新后，Steam 会覆盖 `AIWar2_Data/Managed/` 下的原始 DLL
- 需要用 `PatchedAssemblies/` 中的版本覆盖（AssemblyRedirector 机制）
- 游戏更新后需检查反编译代码与新版本 DLL 的兼容性
- 更新 `AGENTS.md` 中的版本号

## 六、翻译规则

1. **使用 Edit 工具**：逐字符串替换，禁止用 Write 覆写整个文件
2. **只改引号内内容**：只能替换 `"..."` 内的文本，不能修改引号外的任何代码
3. **保留插值和标签**：`$"{variable}"` 中的变量部分保持不变，`<color>` 等标签保持不变
4. **编译验证**：每翻译完一个 DLL 后编译验证，0 错误再继续下一个
5. **禁止中文引号**：C# 字符串中不能使用 `""`（中文双引号），会被编译器误认为字符串分隔符。必须用 `''`（单引号）替代
6. **引号嵌套**：如果翻译文本中需要引用按钮名称或其他 UI 元素，使用 `'单引号'` 包裹
7. **不翻译调试日志**：以 Error、Exception、Null、Cannot、Could not、Failed、Warning、Debug、Trace、Log、START_、FINISH_ 开头的字符串不翻译
8. **不翻译内部标识符**：纯 PascalCase/camelCase 且无空格的字符串不翻译

## 七、注意事项

- 编译器版本 **必须使用 Roslyn 4.12.0**，低版本编译器生成的 IL 代码会导致运行时错误（如对象池异常）
- 游戏版本号更新后需更新此文档
- 禁止修改 `AIWar2_Data/Managed/` 下任何原始 DLL
- csproj 中 HintPath 使用相对路径 `..\..\..\ReliableDLLStorage\`（从 DLLSource/项目名/ 到 ReliableDLLStorage/）
