# AI War 2 汉化项目 — 2026-07-15 工作记录

> 本文档可直接复制给其他 AI，包含完整的背景、过程和结论。

---

## 一、前置清理：序列化层死代码

### 背景

项目文档 `AGENTS.md` 第 294-313 行明确记录了序列化层修复是死路——`SupportedCharList` 扩展、FullUnicode 旁路、BaseBits 动态调整均已验证失败，会破坏存档兼容性。

### 操作

清理了 `DLLSource/WorldTMPFontPatch/src/PatchCharMapping.cs`，从 233 行精简到 31 行，移除：

1. **FullUnicode 标记位旁路**：`TranspileReadString` Transpiler + `ReadFullUnicodeString` 方法 + 4 个 FieldInfo（`MiReadFullHelper`、`MiWriteBits16` 等）+ `FULL_UNICODE_MARKER` 常量 + `_fullUnicodeMode` 字段
2. **BaseBits 动态调整 Postfix**：`OnInitializeStringHandling` 方法 + `FiSupportedCharList` / `FiCharUETypeData` FieldInfo

编译后 DLL 从 18 KB 缩到 14 KB。

---

## 二、右上角瞬时消息框中文恢复

### 问题

右上角动态消息框（`Window_OngoingMessageDisplay`）中，走序列化管道的内容（`GameCommand.RelatedString`）中文显示为 `_`。

### 调查发现

消息框有三类文字，只有一类有问题：

| 消息来源 | 数据流 | 显示 |
|----------|--------|:--:|
| `OnUpdate()` 硬编码中文 | C# 直接拼进 `ArcenDoubleCharacterBuffer`，不走序列化 | ✅ |
| 教程 XML | 不走序列化 | ✅ |
| `LocalMomentaryDisplayLog`（瞬时日志） | `GameCommand.RelatedString` → 序列化管道 | ❌ `_` |

**关键发现**：TMP 渲染层本身能处理中文（`OnUpdate()` 硬编码中文正常显示证明字体替换已生效），瓶颈只在序列化解码。

### 现有代码的问题

`WorldTMPFontPatchPlugin.cs` 的捕获层已经在抓所有 `RelatedString` 中文原文（`OnAddStringCondensed` Prefix），但恢复层有三个致命缺陷：

1. **只覆盖 ChatLog**：`OnChatLogOverlay` 和 `OnTMPTextSetText` 都写了 `if (__instance.name != "ChatLog") return;`
2. **正则不匹配**：ChatLog 格式是 `<link=N><u>HH:MM</u> content</link>`，瞬时日志是 `<link=N>content</link>`（无 `<u>` 时间前缀），正则永远匹配不上
3. **覆盖层创建依赖 OnEnable 时序**，不可靠

### 修复方案

在 `WorldTMPFontPatchPlugin.cs` 中做 6 处改动：

#### 1. 新增白名单匹配（代替硬编码 `== "ChatLog"`）

```csharp
private static bool NeedsChineseOverlay(TMP_Text instance)
{
    string name = instance.name;
    return name == "ChatLog" || name == "BasicText";
}
```

**踩坑**：AssetBundle 中的 prefab 文件名叫 `BasicTextUnderlay.prefab`，Unity 实例化后运行时 GameObject 名是 `BasicText`。初版用 `StartsWith("BasicTextUnderlay")` 永远匹配不到。

#### 2. 新增 `EnsureOverlay()` 按需创建覆盖层

覆盖层创建从 `OnEnable` 时机改为 `set_text` 时懒创建，不依赖 Unity 生命周期时序：

```csharp
private static UnityEngine.UI.Text EnsureOverlay(TMP_Text tmp)
{
    Transform existing = tmp.transform.Find("TMP_LegacyOverlay");
    if (existing != null) return existing.GetComponent<UnityEngine.UI.Text>();
    
    // 创建子 GameObject + Text 组件
    // 锚点 0→1 填满父级尺寸
    // horizontalOverflow = Wrap（自动换行）
    // raycastTarget = false（不拦截点击）
}
```

#### 3. 新增 `OngoingEntryRegex`

```csharp
private static readonly Regex OngoingEntryRegex = new Regex(
    @"<link=\d+>(?<content>.*?)</link>",
    RegexOptions.Compiled | RegexOptions.Singleline);
```

与 ChatLog 正则的区别：没有 `<u>[^<]+</u>\s+` 时间前缀。

#### 4. `OnTMPTextSetText` 按组件名分发

```csharp
if (IsChatLogTarget(__instance))
    restored = RestoreChatLogText(text);   // ChatLog 正则
else
    restored = RestoreOngoingText(text);   // Ongoing 正则
```

#### 5. `RestoreOngoingText()` 新方法

复用已有的 `TryRestoreContent` + `IsContentMatch` 匹配引擎，从捕获列表中找回中文原文。

#### 6. `InternalUpdate` 排除范围扩展

从 `if (name == "ChatLog") return;` 改为 `if (NeedsChineseOverlay(__instance)) return;`。

### 排查过程踩坑

- **兜底路径误伤**：为排查名字匹配问题，临时加了"任何含 `_` 的组件都拦截"的兜底逻辑，确认了 `BasicText` 名字，但也误伤了 `SubjectSummaryText`、`Cost Text`、`MainText`、`BodyText` 等组件，已移除
- **编译缓存**：`strings` 命令在 Git Bash 对 DLL 二进制不生效，需用 `grep -a -o -P` 验证 DLL 内容

---

## 三、修复 Legacy Text 覆盖层不换行

### 问题

长句子中文冲出屏幕右侧，不自动换行。

### 原因

`EnsureOverlay()` 和 `OnChatLogOverlay` 中创建 Legacy Text 时：

```csharp
t.horizontalOverflow = HorizontalWrapMode.Overflow;  // ❌ 永不换行
```

### 修复

改为：

```csharp
t.horizontalOverflow = HorizontalWrapMode.Wrap;      // ✅ 按父级宽度自动换行
```

覆盖层 RectTransform 已锚定到父 TMP 组件完整尺寸（anchor 0→1），启用 Wrap 后 ChatLog 和 BasicText 各自按自己的实际宽度正确换行。

---

## 四、文档更新

### AGENTS.md

1. 第 311 行结论修正：~~"捕获覆盖层也未达到可用状态"~~ → "捕获覆盖层已实现并验证有效，覆盖 ChatLog 和 BasicText"
2. "唯一有效方案"从 `SupportedCharList ≤128 字` 改为捕获覆盖层
3. 新增完整章节"右上角瞬时消息框（BasicText）中文恢复"，含调查发现、修复方案、踩坑记录
4. 补丁列表更新：覆盖范围 ChatLog → ChatLog + BasicText，从 5 个补丁扩展到 6 个

### SPEC.md (AIWar2_CHINESE_TRANSLATION_SPEC.md)

1. 8.12 节：聊天/消息日志中文方框从"无解"更新为已解决
2. 8.13 统计表：状态从 ❌ 已知限制 → ✅ 已解决

---

## 五、修改文件清单

| 文件 | 改动类型 |
|------|---------|
| `DLLSource/WorldTMPFontPatch/src/PatchCharMapping.cs` | 清理（233→31行） |
| `DLLSource/WorldTMPFontPatch/src/WorldTMPFontPatchPlugin.cs` | 核心修复（~100行新增/修改） |
| `AGENTS.md` | 文档更新 |
| `AIWar2_CHINESE_TRANSLATION_SPEC.md` | 文档更新 |

编译输出：`DLLBin/WorldTMPFontPatch.dll`（16 KB）
部署位置：`BepInEx/plugins/ChineseTranslation/WorldTMPFontPatch.dll`

---

## 六、已知限制（未解决）

1. **像素化**：Legacy Text + CanvasScaler 的固有限制，不可彻底解决（TMP SDF 渲染中文需修改版 TMP 源码，已丢失）
2. **点击偏移**：覆盖层中文（全角宽）和 TMP `_`（半角窄）宽度不同，`<link>` hitbox 偏移，需点文字左侧

---

## 七、编译和部署命令

```powershell
cd "d:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
.\build.ps1    # 编译所有 DLL 项目
.\deploy.ps1   # 部署到游戏目录
```

编译器：Roslyn 4.12.0，MSBuild v4.0，目标框架 .NET Framework 4.7.2
