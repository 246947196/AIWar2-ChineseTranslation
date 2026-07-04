# DLL汉化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 汉化三个开源DLL项目中所有未翻译的文件（约724个文件）

**Architecture:** 按文件类型分阶段汉化：先UI文件，再游戏逻辑文件，最后其他文件。每个文件翻译后编译验证，确保0错误。

**Tech Stack:** C# / .NET Framework 4.7.2 / Roslyn 4.12.0 / UTF-8 BOM编码

## Global Constraints

- 编译器版本：必须使用Roslyn 4.12.0 / C# 13.0
- 文件编码：所有含中文的.cs文件必须为UTF-8 with BOM
- 翻译规则：只改引号内内容，保留插值和标签，禁止中文引号
- 编译验证：每翻译完一个文件编译验证，0错误再继续

---

## 文件结构

### AIWarExternalCode (600个文件)
- `src/UIs/` - 96个UI文件（阶段1）
- `src/BaseInfo/` - 游戏逻辑文件（阶段2）
- `src/Sim/` - 模拟逻辑文件（阶段2）
- 其他目录 - 游戏逻辑文件（阶段2）

### AIWarExternalDeepProcessingCode (133个文件)
- `src/GameCommands_HostOnly/` - 游戏命令（阶段2）
- `src/DeepInfo/` - 深度信息（阶段2）
- `src/Notifications/` - 通知（阶段2）
- 其他目录 - 游戏逻辑文件（阶段2）

### AIWarExternalVisualizationCode (45个文件)
- `src/GalaxyMapDisplayModes/` - 银河地图显示模式（阶段1）
- `src/GalaxyMapTextboxFunctions/` - 银河地图文本框功能（阶段1）
- `src/UnitSelection/` - 单位选择（阶段1）
- 其他目录 - 可视化逻辑文件（阶段2）

---

## 任务分解

### 阶段1：UI文件汉化（优先级最高）

#### Task 1: AIWarExternalCode UI文件汉化

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/UIs/*.cs` (96个文件)

**Interfaces:**
- Consumes: 原始英文字符串
- Produces: 翻译后的中文字符串

- [ ] **Step 1: 选择第一个UI文件进行汉化**

选择 `Window_Tips.cs` 作为示例文件。

- [ ] **Step 2: 分析文件中的可翻译字符串**

使用grep搜索英文字符串：
```powershell
Get-ChildItem -Path "DLLSource/AIWarExternalCode/src/UIs/Window_Tips.cs" | Select-String -Pattern '".*"' | Select-Object -First 10
```

- [ ] **Step 3: 使用Edit工具翻译字符串**

使用Edit工具逐个替换英文字符串为中文。

- [ ] **Step 4: 编译验证**

运行编译脚本：
```powershell
.\build.ps1
```
预期：编译成功，0错误

- [ ] **Step 5: 继续翻译其他UI文件**

重复步骤2-4，直到所有96个UI文件翻译完成。

- [ ] **Step 6: 提交翻译**

```bash
git add DLLSource/AIWarExternalCode/src/UIs/
git commit -m "feat: 汉化AIWarExternalCode UI文件"
```

#### Task 2: AIWarExternalVisualizationCode UI相关文件汉化

**Files:**
- Modify: `DLLSource/AIWarExternalVisualizationCode/src/GalaxyMapDisplayModes/*.cs`
- Modify: `DLLSource/AIWarExternalVisualizationCode/src/GalaxyMapTextboxFunctions/*.cs`
- Modify: `DLLSource/AIWarExternalVisualizationCode/src/UnitSelection/*.cs`

**Interfaces:**
- Consumes: 原始英文字符串
- Produces: 翻译后的中文字符串

- [ ] **Step 1: 选择第一个UI相关文件进行汉化**

选择 `GalaxyMapDisplayMode_AISentinelAlertLevels.cs` 作为示例文件。

- [ ] **Step 2: 分析文件中的可翻译字符串**

使用grep搜索英文字符串。

- [ ] **Step 3: 使用Edit工具翻译字符串**

使用Edit工具逐个替换英文字符串为中文。

- [ ] **Step 4: 编译验证**

运行编译脚本：
```powershell
.\build.ps1
```
预期：编译成功，0错误

- [ ] **Step 5: 继续翻译其他UI相关文件**

重复步骤2-4，直到所有UI相关文件翻译完成。

- [ ] **Step 6: 提交翻译**

```bash
git add DLLSource/AIWarExternalVisualizationCode/src/
git commit -m "feat: 汉化AIWarExternalVisualizationCode UI相关文件"
```

### 阶段2：游戏逻辑文件汉化

#### Task 3: AIWarExternalCode游戏逻辑文件汉化

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/BaseInfo/*.cs`
- Modify: `DLLSource/AIWarExternalCode/src/Sim/*.cs`
- Modify: `DLLSource/AIWarExternalCode/src/其他目录/*.cs`

**Interfaces:**
- Consumes: 原始英文字符串
- Produces: 翻译后的中文字符串

- [ ] **Step 1: 选择第一个游戏逻辑文件进行汉化**

选择 `BaseInfo/` 目录下的一个文件作为示例。

- [ ] **Step 2: 分析文件中的可翻译字符串**

使用grep搜索英文字符串。

- [ ] **Step 3: 使用Edit工具翻译字符串**

使用Edit工具逐个替换英文字符串为中文。

- [ ] **Step 4: 编译验证**

运行编译脚本：
```powershell
.\build.ps1
```
预期：编译成功，0错误

- [ ] **Step 5: 继续翻译其他游戏逻辑文件**

重复步骤2-4，直到所有游戏逻辑文件翻译完成。

- [ ] **Step 6: 提交翻译**

```bash
git add DLLSource/AIWarExternalCode/src/
git commit -m "feat: 汉化AIWarExternalCode游戏逻辑文件"
```

#### Task 4: AIWarExternalDeepProcessingCode所有文件汉化

**Files:**
- Modify: `DLLSource/AIWarExternalDeepProcessingCode/src/**/*.cs` (133个文件)

**Interfaces:**
- Consumes: 原始英文字符串
- Produces: 翻译后的中文字符串

- [ ] **Step 1: 选择第一个文件进行汉化**

选择 `GameCommands_HostOnly/NA_GameCommands.cs` 作为示例文件。

- [ ] **Step 2: 分析文件中的可翻译字符串**

使用grep搜索英文字符串。

- [ ] **Step 3: 使用Edit工具翻译字符串**

使用Edit工具逐个替换英文字符串为中文。

- [ ] **Step 4: 编译验证**

运行编译脚本：
```powershell
.\build.ps1
```
预期：编译成功，0错误

- [ ] **Step 5: 继续翻译其他文件**

重复步骤2-4，直到所有133个文件翻译完成。

- [ ] **Step 6: 提交翻译**

```bash
git add DLLSource/AIWarExternalDeepProcessingCode/src/
git commit -m "feat: 汉化AIWarExternalDeepProcessingCode所有文件"
```

#### Task 5: AIWarExternalVisualizationCode游戏逻辑文件汉化

**Files:**
- Modify: `DLLSource/AIWarExternalVisualizationCode/src/BattleVisualization/*.cs`
- Modify: `DLLSource/AIWarExternalVisualizationCode/src/Icons/*.cs`
- Modify: `DLLSource/AIWarExternalVisualizationCode/src/其他目录/*.cs`

**Interfaces:**
- Consumes: 原始英文字符串
- Produces: 翻译后的中文字符串

- [ ] **Step 1: 选择第一个游戏逻辑文件进行汉化**

选择 `BattleVisualization/` 目录下的一个文件作为示例。

- [ ] **Step 2: 分析文件中的可翻译字符串**

使用grep搜索英文字符串。

- [ ] **Step 3: 使用Edit工具翻译字符串**

使用Edit工具逐个替换英文字符串为中文。

- [ ] **Step 4: 编译验证**

运行编译脚本：
```powershell
.\build.ps1
```
预期：编译成功，0错误

- [ ] **Step 5: 继续翻译其他游戏逻辑文件**

重复步骤2-4，直到所有游戏逻辑文件翻译完成。

- [ ] **Step 6: 提交翻译**

```bash
git add DLLSource/AIWarExternalVisualizationCode/src/
git commit -m "feat: 汉化AIWarExternalVisualizationCode游戏逻辑文件"
```

### 阶段3：其他文件汉化

#### Task 6: 辅助文件汉化

**Files:**
- Modify: `DLLSource/AIWarExternalCode/src/Helpers/*.cs`
- Modify: `DLLSource/AIWarExternalCode/src/Input/*.cs`
- Modify: `DLLSource/AIWarExternalCode/src/其他辅助文件/*.cs`

**Interfaces:**
- Consumes: 原始英文字符串
- Produces: 翻译后的中文字符串

- [ ] **Step 1: 选择第一个辅助文件进行汉化**

选择 `Helpers/` 目录下的一个文件作为示例。

- [ ] **Step 2: 分析文件中的可翻译字符串**

使用grep搜索英文字符串。

- [ ] **Step 3: 使用Edit工具翻译字符串**

使用Edit工具逐个替换英文字符串为中文。

- [ ] **Step 4: 编译验证**

运行编译脚本：
```powershell
.\build.ps1
```
预期：编译成功，0错误

- [ ] **Step 5: 继续翻译其他辅助文件**

重复步骤2-4，直到所有辅助文件翻译完成。

- [ ] **Step 6: 提交翻译**

```bash
git add DLLSource/
git commit -m "feat: 汉化辅助文件"
```

---

## 验证计划

### 编译验证
- 每翻译完一个文件运行 `.\build.ps1`
- 确保编译成功，0错误
- 如有错误，立即修复

### 功能测试
- 翻译完成后启动游戏
- 测试UI显示是否正常
- 测试游戏功能是否正常

### 质量检查
- 检查中文显示是否正常
- 检查是否有遗漏的翻译
- 检查翻译质量

---

## 预计时间

- 阶段1：UI文件汉化（约2-3天）
- 阶段2：游戏逻辑文件汉化（约5-7天）
- 阶段3：其他文件汉化（约1-2天）
- 总计：约8-12天

---

## 风险和缓解措施

### 风险1：编译器版本问题
- **缓解措施**：严格使用Roslyn 4.12.0 / C# 13.0

### 风险2：文件编码问题
- **缓解措施**：翻译后转换为UTF-8 with BOM

### 风险3：翻译质量问题
- **缓解措施**：定期进行质量检查和游戏测试

### 风险4：遗漏翻译
- **缓解措施**：使用grep搜索未翻译的字符串
