# AI War 2 汉化术语统一优化计划

创建日期：2026-07-16

## 背景

基于 AI War 1 代精校版中英对照表，对 2 代现有汉化进行术语一致性审查。经语义分析和用户确认，已确定 6 项需替换的术语映射。

## 术语映射表（已确认）

| 英文 | 1代标准 | 2代现状 | 替换为 | 限制条件 |
|------|---------|---------|--------|---------|
| Ship (Fleet Ships 常规舰) | 单位 | 舰船 | **单位** | 仅替换泛称"舰船"，不影响专有术语（星舰/魔像/守护者/炮塔/护卫舰） |
| Constructor (固定) | 兵工厂 | 工厂 | **兵工厂** | 仅 `stationary_constructor` 或 `special_entity_type="FactoryForPlayerMobileFleets"` |
| Constructor (移动) | — | 战斗工厂 | **机动兵工厂** | `special_entity_type="MobileSupportFleetFlagship"` 的移动 Constructor |
| Warhead | 飞弹 | 弹头 | **飞弹** | 全局 |
| Scrap (操作动词) | 废弃 | 拆解/废弃混用 | **废弃** | 仅操作动词，不影响"废料"（scrap metal 名词） |
| Hack/Hacking | — | 黑客/入侵混用 | **入侵** | 全局（名词"入侵"，动词"入侵/破解"） |

### 不需修改（分析后保留）

| 术语 | 保留 | 原因 |
|------|------|------|
| Golem | **魔像** | 游戏 lore：天顶古文明"傀儡""废弃残骸" |
| Wave | **波次** | 语义分析：周期性自动刷怪机制，非战略攻势 |
| Starship | **星舰** | ✅ 已一致 |
| Guardian | **守护者** | ✅ 已一致 |
| Turret | **炮塔** | ✅ 已一致 |
| Fleet | **舰队** | ✅ 已一致 |
| Force Field | **力场** | ✅ 已一致 |
| Frigate | **护卫舰** | ✅ 已一致 |

---

## 影响范围估算

| 术语 | XML 层 | C# 层 | IL 补丁层 | arcenui | 总计估算 |
|------|--------|-------|-----------|---------|---------|
| Ship (舰船→单位) | ~1500 | ~339 | ~90 | 0 | ~2000 |
| Warhead (弹头→飞弹) | ~24 | ~5 | ~5 | 0 | ~35 |
| Hack (黑客→入侵) | ~40 | ~30 | ~10 | 0 | ~80 |
| Scrap (拆解→废弃) | ~5 | ~3 | ~2 | 0 | ~10 |
| Constructor (兵工厂) | ~8 | ~5 | ~2 | 0 | ~15 |
| Constructor (机动兵工厂) | ~5 | ~2 | 0 | 0 | ~7 |

> 注：计数值为初步扫描估算，实际可能有所出入。

---

## 执行策略

### 优先级排序

| 优先级 | 术语 | 理由 |
|--------|------|------|
| P0 | Warhead (弹头→飞弹) | 范围小、风险低，先跑通流程 |
| P0 | Scrap (拆解→废弃) | 范围小，热身 |
| P0 | Constructor (兵工厂/机动兵工厂) | 范围小，验证实体名替换流程 |
| P1 | Hack (黑客→入侵) | 中等范围，需 IL + XML + C# 三层同步 |
| P1 | Ship (舰船→单位) | **最大范围**，需分片并行处理，最后做 |

### 每项执行步骤

1. **精确扫描**：统计所有层中该术语的精确位置（文件+行号）
2. **分类标记**：Ship 项需要区分泛称 vs 专有术语（星舰/战舰/护卫舰/魔像/守护者/炮塔）
3. **逐层替换**：
   - IL 补丁 JSON → 直接修改字典 value
   - C# 源码 → `edit` 工具逐文件替换
   - XML → `edit` 工具逐文件替换
4. **构建验证**：
   - IL 补丁层：运行 `ilpatch` 重新 patch → `verify_patch.py` 验证
   - C# 层：`build.ps1` 编译，0 错误通过
   - XML 层：无编译需求，语义复查
5. **提交**: git commit（按术语分批提交）

---

## Ship 项风险控制（重点）

Ship (舰船→单位) 是最大风险项。**核心约束：区分泛称"舰船"与专有术语。**

```
泛称"ship"（应替换为"单位"）：
  ├── display_name="舰船" → "单位"
  ├── description 中的"舰船"（指代常规 fleet ship） → "单位"
  ├── 提示/Tutorial 中的"舰船" → "单位"
  └── IL 补丁中的"ship"/"ships" → "单位"

专有术语（不应替换）：
  ├── "星舰" (Starship) — 保留
  ├── "战舰" (Battleship/Warship) — 保留
  ├── "护卫舰" (Frigate) — 保留
  ├── "魔像" (Golem) — 保留
  ├── "守护者" (Guardian) — 保留
  ├── "旗舰" (Flagship) — 保留
  ├── "炮塔" (Turret) — 保留
  └── "近战/狙击/轰炸 舰船" — 作为 Fleet Ship 子类，应改为"近战/狙击/轰炸 单位"
```

### Ship 替换方案

1. **XML 文件**：用子代理分片并行处理
   - 每个子代理处理 5-10 个 XML 文件
   - 只替换孤立出现的"舰船"（前后不是"星""战""护卫""魔""守护""旗""炮"等前缀）
   - 保留复合词中的"舰船"若其在专有术语中（如"星舰"不拆分）

2. **C# 源码**：同样分片并行处理
   - 走 string literal 替换（`"舰船"` → `"单位"`）
   - 注意上下文：`"XX舰船"` vs `"舰船XX"` 的边界

3. **IL 补丁**：
   - 修改 `*.merged.json` 中的 value 字段
   - 重新 patch → verify

---

## 构建与部署验证

每完成一项术语修改后执行：

```powershell
# DLL 编译
Remove-Item -Recurse -Force DLLSource\AIWarExternalCode\obj\Release
.\build.ps1

# IL 补丁（如涉及）
python tools\ilpatch\ilpatch.py patch ...

# 部署
.\deploy.ps1

# 验证 IL 补丁
python tools\ilpatch\verify_patch.py PatchedAssemblies\ArcenAIW2Core.dll tools\ilpatch\ArcenAIW2Core.merged.json
```

启动游戏验证：
- 主菜单 / 大厅 UI 无英文残留
- 游戏内实体名称、提示、描述术语一致
- 不复现方框/崩溃

---

## 预计耗时

| 术语 | 预计耗时 | 备注 |
|------|---------|------|
| Warhead + Scrap + Constructor | 1 次会话 | 范围小，三合一 |
| Hack | 1 次会话 | 三层同步 |
| Ship | 2-3 次会话 | 需分片并行 + 大量验证 |
| **总计** | **4-5 次会话** | |
