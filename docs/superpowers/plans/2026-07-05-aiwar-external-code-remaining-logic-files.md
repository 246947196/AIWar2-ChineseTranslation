# AIWarExternalCode 剩余游戏逻辑文件汉化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete Chinese localization of remaining untranslated game logic files in AIWarExternalCode project (BaseInfo, BaseInfo_World, DescriptionAppenders directories).

**Architecture:** Use Edit tool to replace English strings with Chinese translations inside quoted strings only. Preserve interpolation (`$"{variable}"`), HTML tags (`<color>`, `<size>`), and code structure. Use single quotes (`''`) for Chinese quote nesting within C# strings. Verify build after each file. Ensure UTF-8 BOM encoding for all modified files.

**Tech Stack:** C# / .NET Framework 4.7.2 / Roslyn 4.12.0 / UTF-8 BOM encoding

## Global Constraints

- Only change text within quotes (`"..."`)
- Preserve all interpolation (`$"{variable}"`) and HTML tags (`<color>`, `<size>`, `<b>`)
- Use single quotes (`''`) for Chinese quotes, not double quotes (`""`)
- Ensure UTF-8 with BOM encoding for files with Chinese characters
- Use Edit tool for string replacement, not Write tool
- Run build verification after each file: `.\build.ps1`
- Compiler version: Roslyn 4.12.0 / C# 13.0
- Do NOT translate debug logs, exception messages, internal identifiers, tracker names
- Do NOT translate variable/method/class names

---

## File Classification

### Files that need English→Chinese translation (still have English tooltip strings):

| # | File | Approx strings | Priority |
|---|------|----------------|----------|
| 1 | `src/DescriptionAppenders/BaseGame/AstroTrainDescriptionAppender.cs` | ~8 (garbled Chinese, needs re-translation) | HIGH |
| 2 | `src/DescriptionAppenders/BaseGame/HarvesterDescriptionAppender.cs` | ~5 (garbled Chinese) | HIGH |
| 3 | `src/BaseInfo/DLC2/ZenithArchitrave/PublicZenithArchitraveNotifier.cs` | ~30+ English tooltip strings | HIGH |
| 4 | `src/BaseInfo/DLC3/DLC3GameEntityTypeDataExtension.cs` | ~40+ English tooltip strings | HIGH |
| 5 | `src/BaseInfo/DLC3/Human/Necromancer/NecromancerNotifiers.cs` | ~10 English strings | HIGH |
| 6 | Other DescriptionAppenders/DLC files | variable | MEDIUM |

### Files with garbled Chinese (encoding issue — need BOM fix + possible re-translation):
Multiple DescriptionAppenders files already have garbled Chinese text, indicating they were previously translated but saved without UTF-8 BOM. These need encoding fix via `add-bom.ps1`.

### Files that are DONE (already translated correctly):
- `src/BaseInfo/BaseGame/FleetMetricsBaseInfo.cs`
- `src/BaseInfo/Sidekicks/ScourgeVassalFactionBaseInfo.cs`
- `src/BaseInfo/Sidekicks/ScourgeInfusedHumanEmpireNotifiers.cs`
- `src/BaseInfo/BaseGame/RandomFactionBaseInfo.cs`
- `src/BaseInfo_World/Notifiers/AstroTrainNotifier.cs`
- `src/BaseInfo_World/Notifiers/BrownoutNotifier.cs`
- Various other Sidekicks files

### Files that should NOT be translated (debug/internal):
- Sim/ directory files (mostly debug strings)
- Exception messages, ArcenDebugging log calls
- Tracker/serializer string identifiers

---

### Task 1: Fix encoding on garbled DescriptionAppenders files

**Files:**
- Modify: `src/DescriptionAppenders/BaseGame/AstroTrainDescriptionAppender.cs`
- Modify: `src/DescriptionAppenders/BaseGame/HarvesterDescriptionAppender.cs`
- Verify: All other DescriptionAppenders files with garbled Chinese

**Interfaces:**
- Consumes: Existing garbled Chinese text (UTF-8 without BOM)
- Produces: Properly encoded UTF-8 BOM files with readable Chinese

- [ ] **Step 1: Check which DescriptionAppenders files have Chinese characters**

```powershell
$baseDir = "DLLSource\AIWarExternalCode\src\DescriptionAppenders"
Get-ChildItem $baseDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    if ($content -match '[\u4e00-\u9fff]') {
        Write-Output $_.FullName
    }
}
```

- [ ] **Step 2: Run add-bom.ps1 on all files with Chinese**

```powershell
.\add-bom.ps1
```

Expected: All .cs files with Chinese get UTF-8 BOM encoding.

- [ ] **Step 3: Build and verify**

Run: `.\build.ps1`
Expected: All DLLs built successfully with 0 errors.

- [ ] **Step 4: Verify garbled text is now readable**

Read `AstroTrainDescriptionAppender.cs` and `HarvesterDescriptionAppender.cs` — confirm Chinese text displays correctly (e.g., "这列火车当前正在闲置" instead of garbled text).

---

### Task 2: Translate DLC2/PublicZenithArchitraveNotifier.cs

**Files:**
- Modify: `src/BaseInfo/DLC2/ZenithArchitrave/PublicZenithArchitraveNotifier.cs`

**Interfaces:**
- Consumes: English tooltip strings about Zenith Architrave civil war mechanics
- Produces: Chinese translated tooltip strings

- [ ] **Step 1: Read the file and identify all translatable strings**

The file contains ~30+ tooltip strings about Architrave civil war. Key strings include:
- "Architraves have" / "Architrave has"
- "Once a Zenith Architrave takes too many planets..."
- "At the moment..."
- "there is one..."
- "will attack in..." / "is attacking."
- "remaining..."
- "Architraves"
- "will ally against in..." / "are allied against."
- "All Architraves are in a free for all."
- "The remaining..."
- "architraves are allied against them."
- "the Architraves over the limit are:"
- "the Architraves that will be over the limit are:"
- "All Architraves will be in a free for all."
- "Architraves in Civil War are exceptionally powerful..."
- "Civil War" (notification label)
- "The Architraves are currently ignoring any truces..."

- [ ] **Step 2: Translate each string using Edit tool**

Key translations:
- "Architraves have" → "拱顶石阵营拥有"
- "Architrave has" → "拱顶石阵营拥有"
- "Once a Zenith Architrave takes too many planets, the other Architraves will all unite against it." → "一旦某个天顶拱顶石占据过多星球，其他拱顶石将联合对抗它。"
- "At the moment" → "目前"
- "there is one" → "有"
- "will attack in" → "将在...后进攻"
- "is attacking." → "正在进攻。"
- "remaining" → "剩余"
- "Architraves" → "个拱顶石阵营"
- "will ally against in" → "将在...后联合对抗"
- "are allied against." → "正在联合对抗。"
- "All Architraves are in a free for all." → "所有拱顶石阵营处于混战状态。"
- "The remaining" → "剩余"
- "architraves are allied against them." → "个拱顶石阵营正在联合对抗它们。"
- "the Architraves over the limit are:" → "超过限制的拱顶石阵营有："
- "the Architraves that will be over the limit are:" → "即将超过限制的拱顶石阵营有："
- "All Architraves will be in a free for all." → "所有拱顶石阵营将进入混战状态。"
- "Architraves in Civil War are exceptionally powerful, and will produce many Golems to fight eachother; once the offending Architraves have been weakened, the war will end and the other Architraves will retreat back to their territory.\nYour ships and planets could get caught in the crossfire." → "内战中的拱顶石阵营异常强大，会生产大量魔像互相战斗。一旦挑起战争的拱顶石阵营被削弱，战争将结束，其他拱顶石阵营将撤回各自的领地。\n您的舰船和星球可能会被卷入战火。"
- "Civil War" → "内战"
- "The Architraves are currently ignoring any truces with you until the end of the civil war." → "在内战结束之前，拱顶石阵营将无视与您的任何休战协议。"

- [ ] **Step 3: Build and verify**

Run: `.\build.ps1`
Expected: 0 errors.

- [ ] **Step 4: Convert to UTF-8 BOM**

```powershell
$content = [System.IO.File]::ReadAllText("DLLSource\AIWarExternalCode\src\BaseInfo\DLC2\ZenithArchitrave\PublicZenithArchitraveNotifier.cs", [System.Text.Encoding]::UTF8)
$utf8BOM = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText("DLLSource\AIWarExternalCode\src\BaseInfo\DLC2\ZenithArchitrave\PublicZenithArchitraveNotifier.cs", $content, $utf8BOM)
```

---

### Task 3: Translate DLC3/DLC3GameEntityTypeDataExtension.cs

**Files:**
- Modify: `src/BaseInfo/DLC3/DLC3GameEntityTypeDataExtension.cs`

**Interfaces:**
- Consumes: English tooltip strings about Necromancer, Dyson, Dark Zenith, Spire, Armada, Apkallu sidekick mechanics
- Produces: Chinese translated tooltip strings

- [ ] **Step 1: Read the file and identify all translatable strings**

Key strings in `AddToTooltip_MidSection_ForEntity` and `AddToTooltip_GainsSection_ForEntity`:
- "the fleet bolstered by this necropolis" / "The fleet bolstered by this necropolis"
- "that fleet" / "That fleet"
- "This fleet is bolstering"
- "If a skeleton would be created for..." (multiple skeleton/wight/mummy strings)
- "If the necromancer helps kill this unit, it will get a new upgrade."
- "If a Necromancer helps destroy this, they get"
- "If a DZ helps destroy this, they get"
- "If a scourge infused empire helps destroy this, they get"
- "If a Spire Sidekick helps destroy this, they get"
- "If an Armada helps destroy this, they get"
- "If a Dyson helps destroy this, they get"
- "If an Apkallu helps destroy this, they get"

- [ ] **Step 2: Translate each string using Edit tool**

Key translations:
- "the fleet bolstered by this necropolis" → "被这座死灵城支持的舰队"
- "The fleet bolstered by this necropolis" → "被这座死灵城支持的舰队"
- "that fleet" → "那支舰队"
- "That fleet" → "那支舰队"
- "This fleet is bolstering" → "这支舰队正在支持"
- Skeleton/wight/mummy creation strings: translate mechanic descriptions
- "If the necromancer helps kill this unit, it will get a new upgrade." → "如果死灵法师协助击杀此单位，它将获得一项新升级。"
- "If a Necromancer helps destroy this, they get" → "如果死灵法师协助摧毁此单位，他们将获得"
- "If a DZ helps destroy this, they get" → "如果黑暗泽尼斯协助摧毁此单位，他们将获得"
- "If a scourge infused empire helps destroy this, they get" → "如果天灾帝国协助摧毁此单位，他们将获得"
- "If a Spire Sidekick helps destroy this, they get" → "如果尖塔副官协助摧毁此单位，他们将获得"
- "If an Armada helps destroy this, they get" → "如果舰队协助摧毁此单位，他们将获得"
- "If a Dyson helps destroy this, they get" → "如果戴森协助摧毁此单位，他们将获得"
- "If an Apkallu helps destroy this, they get" → "如果阿普卡鲁协助摧毁此单位，他们将获得"

- [ ] **Step 3: Build and verify**

Run: `.\build.ps1`
Expected: 0 errors.

- [ ] **Step 4: Convert to UTF-8 BOM**

---

### Task 4: Translate DLC3/NecromancerNotifiers.cs

**Files:**
- Modify: `src/BaseInfo/DLC3/Human/Necromancer/NecromancerNotifiers.cs`

**Interfaces:**
- Consumes: English tooltip strings about Templar wave leaders and constructors
- Produces: Chinese translated tooltip strings

- [ ] **Step 1: Identify translatable strings**

- "The Templar have" (appears twice)
- "wave leaders in command of attacks against the necromancer."
- "Ships visible to you:"
- "No visible ships"
- " on " (contextual, keep as is)
- "Templar" (notification label)
- "constructors to build new defenses. Killing these can be very valuable, both to weaken the Templar and to get resources. Currently visible to you:"

- [ ] **Step 2: Translate each string using Edit tool**

- "The Templar have" → "圣殿骑士拥有"
- "wave leaders in command of attacks against the necromancer." → "个波次领袖指挥着对死灵法师的进攻。"
- "Ships visible to you:" → "您可见的舰船："
- "No visible ships" → "没有可见的舰船"
- "Templar" (notification label) → "圣殿骑士"
- "constructors to build new defenses. Killing these can be very valuable, both to weaken the Templar and to get resources. Currently visible to you:" → "个建造者用于建造新防御。击杀它们非常有价值，既能削弱圣殿骑士又能获取资源。您目前可见的有："

- [ ] **Step 3: Build and verify**

Run: `.\build.ps1`
Expected: 0 errors.

- [ ] **Step 4: Convert to UTF-8 BOM**

---

### Task 5: Scan and translate remaining DescriptionAppenders DLC files

**Files:**
- Modify: `src/DescriptionAppenders/DLC1/*.cs`
- Modify: `src/DescriptionAppenders/DLC2/*.cs`
- Modify: `src/DescriptionAppenders/DLC3/*.cs`
- Modify: `src/DescriptionAppenders/DLC4/*.cs`
- Modify: `src/DescriptionAppenders/Sidekicks/*.cs`

**Interfaces:**
- Consumes: English description text for DLC entities
- Produces: Chinese translated descriptions

- [ ] **Step 1: List all DescriptionAppenders files**

```powershell
Get-ChildItem "DLLSource\AIWarExternalCode\src\DescriptionAppenders" -Recurse -Filter "*.cs" | Select-Object FullName
```

- [ ] **Step 2: Search each file for English translatable strings**

Use grep to find quoted strings with spaces (likely tooltip text). Skip files that already have correct Chinese.

- [ ] **Step 3: Translate each file that has untranslated English strings**

Follow the same pattern: Edit tool for each string → Build verify → BOM fix.

- [ ] **Step 4: Build and verify all**

Run: `.\build.ps1`
Expected: 0 errors.

---

### Task 6: Scan remaining BaseInfo subdirectories for missed files

**Files:**
- Scan: `src/BaseInfo/BaseGame/AI/*.cs`
- Scan: `src/BaseInfo/BaseGame/AstroTrains/*.cs`
- Scan: `src/BaseInfo/BaseGame/Human/*.cs`
- Scan: `src/BaseInfo/BaseGame/Nanocaust/*.cs`
- Scan: `src/BaseInfo/BaseGame/SphereFactions/*.cs`
- Scan: `src/BaseInfo/BaseGame/Zombies/*.cs`
- Scan: `src/BaseInfo/DLC1/**/*`
- Scan: `src/BaseInfo/DLC2/**/*`
- Scan: `src/BaseInfo/DLC3/**/*`
- Scan: `src/BaseInfo/DLC4/**/*`

**Interfaces:**
- Consumes: Any remaining untranslated English tooltip strings
- Produces: Chinese translated strings

- [ ] **Step 1: Comprehensive grep for English tooltip strings**

Search for patterns like `"[A-Z][a-z]+ [a-z]+"` across all BaseInfo subdirectories.

- [ ] **Step 2: Filter out debug/log/exception strings**

Exclude strings inside:
- `ArcenDebugging.ArcenDebugLog` calls
- `WriteHeaderStringToLogIfLoggingActive` calls
- `throw new Exception` calls
- Comment lines (//)
- Tracker/serializer identifiers

- [ ] **Step 3: Translate remaining English strings**

For each file with player-visible English strings, translate using Edit tool.

- [ ] **Step 4: Build and verify**

Run: `.\build.ps1`
Expected: 0 errors.

---

### Task 7: Final encoding verification and build

- [ ] **Step 1: Run BOM conversion on all files with Chinese**

```powershell
.\add-bom.ps1
```

- [ ] **Step 2: Full build verification**

Run: `.\build.ps1`
Expected: All three DLLs built successfully.

- [ ] **Step 3: Write translation report**

Write report to `D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\.git\sdd\task-3-report-4.md`:
- What was translated (directories and files)
- Build verification results
- Any issues encountered

---
