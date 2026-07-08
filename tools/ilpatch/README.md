# ilpatch — 核心 DLL IL 级汉化工具

基于 dnlib 的控制台工具，用于修改 `ArcenAIW2Core.dll` / `ArcenAIW2Visualization.dll` 中的 `ldstr` 字符串字面量（核心 DLL 的 IL 汉化方案，详见 `../AIWar2_CHINESE_TRANSLATION_SPEC.md` 第 8.15 节）。

## 编译

需要 .NET 8 SDK。dnlib 引用见 `ilpatch.csproj` 中的 `HintPath`（指向本机 dnSpy 安装目录下的 `net48\dnlib.dll`）。

```powershell
dotnet build -c Release
# 产物: bin\Release\net8.0\ilpatch.exe
```

## 用法

```powershell
# 1. 探查 DLL 中全部唯一 ldstr（可按子串过滤）
ilpatch inspect <dll> [contains]

# 2. 导出疑似可翻译字符串骨架 -> {"原文":""}
ilpatch extract <dll> <out.json>

# 3. 试运行，确认匹配条数（不写文件）
ilpatch patch <dll> <dict.json> --dry

# 4. 正式写入：自动备份 <dll>.bak，原位覆盖
ilpatch patch <dll> <dict.json>
```

字典格式（`{"English": "中文"}`）：
- key 为 DLL 内**完整** `ldstr` 原文（含前导/尾随空格、`\n`、`<size>` 等标签），须逐字节一致。
- value 为中文，保留 `{0}`、`<color>`、`<size=70%>`、`\n` 等格式符。
- 译文中禁止中文引号 `""`，用 `'单引号'`。

## 注意事项

- 若目标 DLL 被占用（dnSpy/游戏/文件监视器），工具降级输出 `<name>.new.dll`，需关闭占用进程后 `Copy-Item -Force` 覆盖。
- `ldstr` 操作数必须赋 `.NET string`，不可赋 `new UTF8String(...)`（否则 writer 报 `Invalid instruction operand`）。
- 改完后核心 DLL 放在游戏 `PatchedAssemblies/`，由 BepInEx `AssemblyRedirector` 在加载前读取。
