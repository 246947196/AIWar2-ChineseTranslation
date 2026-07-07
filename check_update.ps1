# AI War 2 Update Detection Script
# Usage:
#   .\check_update.ps1                  -- Detect changes, generate report
#   .\check_update.ps1 -snapshot        -- Take baseline snapshot (fails if exists)
#   .\check_update.ps1 -snapshot -force -- Force overwrite snapshot

param(
    [switch]$snapshot,
    [switch]$force
)

$translationDir = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
$gameDir = "D:\Steam\steamapps\common\AI War 2"
$snapshotFile = Join-Path $translationDir "translation_snapshot.json"
$configDir = Join-Path $gameDir "GameData\Configuration"
$codeExtDir = Join-Path $gameDir "CodeExternal"
$managedDir = Join-Path $gameDir "AIWar2_Data\Managed"
$bundleDir = Join-Path $gameDir "AssetBundles_Win"
$arcenuiBundle = Join-Path $bundleDir "arcenui"

# ---- Helper: Get game version ----
function Get-GameVersionFunc {
    $versionFile = Join-Path $configDir "GameVersion\KDL_GameVersions.xml"
    if (-not (Test-Path $versionFile)) { return $null }
    $content = Get-Content $versionFile -Raw -ErrorAction SilentlyContinue
    if (-not $content) { return $null }
    $versions = [regex]::Matches($content, 'major_version="(\d+)".*?minor_version="(\d+)"')
    $latest = $versions | ForEach-Object {
        $major = [int]$_.Groups[1].Value
        $minor = [int]$_.Groups[2].Value
        [PSCustomObject]@{ Major=$major; Minor=$minor; Version="$major.$minor" }
    } | Sort-Object Major,Minor -Descending | Select-Object -First 1
    return $latest.Version
}

# ---- Helper: SHA256 hash of a file ----
function Get-SHA256Hash {
    param($path)
    if (-not (Test-Path $path)) { return $null }
    $h = Get-FileHash -Path $path -Algorithm SHA256
    return $h.Hash.ToLower()
}

# ---- Helper: Detect if game dir is Chinese ----
function Test-GameIsEnglish {
    $testFiles = @(
        "Achievement\KDL_Achievements.xml",
        "GameEntity\KDL_Ships_FleetShips.xml",
        "Tips\CMP_Tips_GettingStarted.xml"
    )
    foreach ($rel in $testFiles) {
        $f = Join-Path $configDir $rel
        if (-not (Test-Path $f)) { continue }
        $bytes = [System.IO.File]::ReadAllBytes($f)
        for ($i = 0; $i -lt $bytes.Length - 1; $i++) {
            if (($bytes[$i] -ge 0xE4) -and ($bytes[$i] -le 0xE9) -and ($bytes[$i+1] -ge 0x80) -and ($bytes[$i+1] -le 0xBF)) {
                return $false
            }
        }
    }
    return $true
}

# ---- Whitelist patterns (same as check_translation.ps1) ----
function Test-IsWhitelisted {
    param($relPath)
    $patterns = @(
        "AstroTrainBehaviorType\*", "CustomSystemType\*", "GameVersion\*",
        "HackingType\DeprecatedHacks.xml", "ObjectiveDetailsHooks\*",
        "TextStyles\*", "TextVarMaps\TextVarMaps_Vanilla.xml",
        "ExternalConstants\*", "ExternalFactionBaseInfo\*", "ExternalFactionDeepInfo\*",
        "ExternalFleetBaseInfo\*", "ExternalGameEntityTypeDataExtension\*",
        "ExternalSquadBaseInfo\*", "ExternalSquadDeepInfo\*",
        "ExternalWorldBaseInfo\*", "ExternalWorldDeepInfo\*",
        "ExternalCodeHook\*", "ExternalDeepLink\*", "ExternalDllInitialLoadCall\*",
        "ExternalVisualConstants\*", "ExternalVisualCoreConstants\*",
        "Balance_*", "AIShipGroup\*", "AIShipGroupCategory\*",
        "AIDefensePlacer\*", "AIGuardPostAndCommandPlacer\*",
        "ArcenThreadingRequestType\*", "CameraType\*", "FramerateType\*",
        "UIPrefab\*", "UIWindow\*", "ParticlePattern\*", "SpaceboxDefinition\*",
        "PlanetDefinition\*", "PlanetNames\*", "TextEmbededSprites\*",
        "IconShipStatus\*", "CustomOrderType\*", "ObjectiveGenerator\*",
        "SpecialFactionProcessingGroup\*", "SurrogateTable\*",
        "XmlModNamesToIgnoreNow\*", "ChatClickHandler\*", "GameCommand\*",
        "ShipVoiceGroup\*", "SFXBus\*", "TeamColorPrefabs\*",
        "VassalOrderCategory\*", "DarkSpire_HarvestRatio\*", "InstigatorData\*",
        "LookupSwaps\*", "NonSimEffect\*", "NPCShipCapType\*",
        "SphereFactionDifficulty\*", "TargetedInputAction\*", "TargetEvaluator\*",
        "TechUpgrade\*", "TestChamber\*",
        "Expansions\1_The_Spire_Rises\GameData\Configuration\External*",
        "Expansions\2_Zenith_Onslaught\GameData\Configuration\External*",
        "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\External*",
        "Expansions\*\GameData\Configuration\AIShipGroup\*",
        "Expansions\*\GameData\Configuration\AIShipGroupCategory\*",
        "Expansions\*\GameData\Configuration\Balance_*",
        "Expansions\*\GameData\Configuration\SpecialFactionProcessingGroup\*",
        "Expansions\*\GameData\Configuration\SurrogateTable\*",
        "Expansions\*\GameData\Configuration\*Difficulty\*"
    )
    foreach ($pattern in $patterns) {
        if ($relPath -like $pattern) { return $true }
    }
    return $false
}

# ============ EXTRACTION FUNCTIONS ============

# ---- XML extraction ----
function Extract-XMLStrings {
    param($baseDir)
    Write-Host "  Scanning XML files..." -ForegroundColor Gray
    $result = @{}
    if (-not (Test-Path $baseDir)) { Write-Host "    NOT FOUND: $baseDir" -ForegroundColor Red; return $result }
    $files = Get-ChildItem -Path $baseDir -Recurse -Filter "*.xml"
    $count = 0
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($baseDir.Length + 1)
        if (Test-IsWhitelisted $rel) { continue }
        $hash = Get-SHA256Hash $f.FullName

        $strings = @{}
        $content = Get-Content $f.FullName -Raw -Encoding UTF8
        if ($content) {
            $m = [regex]::Matches($content, '(display_name|description|full_text|category)="((?:[^"]|""|\\")*?)"')
            foreach ($match in $m) {
                $attr = $match.Groups[1].Value
                $val = $match.Groups[2].Value
                if ($val -and $val.Length -ge 2) {
                    $strings[$attr] = $val
                }
            }
        }
        $result[$rel] = @{ hash = $hash; strings = $strings }
        $count++
        if ($count % 200 -eq 0) { Write-Host "    ... $count files" -ForegroundColor DarkGray }
    }
    Write-Host "    $count XML files scanned" -ForegroundColor Green
    return $result
}

# ---- DLL source extraction ----
function Extract-DLLSourceStrings {
    param($baseDir)
    Write-Host "  Scanning DLL source files..." -ForegroundColor Gray
    $result = @{}
    if (-not (Test-Path $baseDir)) {
        Write-Host "    CodeExternal/ not found, skipping" -ForegroundColor DarkYellow
        return $result
    }
    $files = Get-ChildItem -Path $baseDir -Recurse -Filter "*.cs" | Where-Object {
        $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*"
    }
    $count = 0
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($baseDir.Length + 1)
        $hash = Get-SHA256Hash $f.FullName
        $strings = @{}
        $lines = Get-Content $f.FullName -Encoding UTF8
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $trimmed = $line.Trim()
            if ($trimmed -match '^//' -or $trimmed -eq '' -or $trimmed -match '^#') { continue }
            $strMatches = [regex]::Matches($line, '"((?:[^"\\]|\\.)*)"')
            foreach ($m in $strMatches) {
                $val = $m.Groups[1].Value
                if ($val.Length -ge 2 -and $val -notmatch '^[\d\s\-\.\,\:\;\+\=\/\*\(\)\[\]\{\}\\\&\|\!\?\<\>\%\$\#\@\^\~'']+$') {
                    $key = "L$($i+1):$([System.IO.Path]::GetFileNameWithoutExtension($f.Name))"
                    $strings[$key] = $val
                }
            }
        }
        if ($strings.Count -gt 0) {
            $result[$rel] = @{ hash = $hash; strings = $strings }
        }
        $count++
    }
    Write-Host "    $count .cs files scanned" -ForegroundColor Green
    return $result
}

# ---- Core DLL extraction (hash only, strings via ilspycmd later) ----
function Extract-CoreDLLs {
    param($baseDir)
    Write-Host "  Scanning core DLLs..." -ForegroundColor Gray
    $result = @{}
    if (-not (Test-Path $baseDir)) { Write-Host "    NOT FOUND: $baseDir" -ForegroundColor Red; return $result }
    $dlls = @("ArcenUniversal.dll", "ArcenAIW2Core.dll", "ArcenAIW2Visualization.dll")
    foreach ($dll in $dlls) {
        $path = Join-Path $baseDir $dll
        if (-not (Test-Path $path)) {
            Write-Host "    $dll not found, skipping" -ForegroundColor DarkYellow
            continue
        }
        $hash = Get-SHA256Hash $path
        $result[$dll] = @{ hash = $hash; strings = @{}; note = "requires ilspycmd for string extraction" }
    }
    Write-Host "    $($result.Count) DLLs hashed" -ForegroundColor Green
    return $result
}

# ---- arcenui AssetBundle extraction ----
function Extract-ArcenUIStrings {
    Write-Host "  Scanning arcenui AssetBundle..." -ForegroundColor Gray
    if (-not (Test-Path $arcenuiBundle)) {
        Write-Host "    arcenui bundle not found, skipping" -ForegroundColor DarkYellow
        return $null
    }
    $hash = Get-SHA256Hash $arcenuiBundle
    $strings = @{}

    # Import known English strings from existing translation file if present
    $transJson = Join-Path $translationDir "arcenui_translations.json"
    if (Test-Path $transJson) {
        $data = Get-Content $transJson -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($k in $data.translations.PSObject.Properties.Name) {
            $strings[$k] = $k
        }
        Write-Host "    $($strings.Count) strings from arcenui_translations.json" -ForegroundColor Gray
    } else {
        Write-Host "    no arcenui_translations.json, hash only" -ForegroundColor DarkYellow
    }

    return @{ hash = $hash; strings = $strings }
}

# ============ SNAPSHOT BUILDING ============

function Build-Snapshot {
    param($gameVer)
    Write-Host "Taking baseline snapshot..." -ForegroundColor Cyan

    $xmlData = Extract-XMLStrings $configDir
    $dllSourceData = Extract-DLLSourceStrings $codeExtDir
    $coreDLLData = Extract-CoreDLLs $managedDir
    $arcenuiData = Extract-ArcenUIStrings

    $layers = @{
        xml = $xmlData
        dll_source = $dllSourceData
        dll_core = $coreDLLData
    }
    if ($arcenuiData) { $layers.arcenui = $arcenuiData }

    $snapshot = @{
        game_version = $gameVer
        taken_at = (Get-Date -Format "o")
        layers = $layers
    }

    $json = $snapshot | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($snapshotFile, $json, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Snapshot written: $snapshotFile" -ForegroundColor Green

    $totalStrings = 0
    foreach ($v in $xmlData.Values) { $totalStrings += $v.strings.Count }
    $dllSrcStrings = 0
    foreach ($v in $dllSourceData.Values) { $dllSrcStrings += $v.strings.Count }
    Write-Host "  XML files: $($xmlData.Count), strings: $totalStrings"
    Write-Host "  DLL source files: $($dllSourceData.Count), strings: $dllSrcStrings"
    Write-Host "  Core DLLs: $($coreDLLData.Count)"
    if ($arcenuiData) { Write-Host "  arcenui: $($arcenuiData.strings.Count) strings" }
}

# ============ COMPARISON ============

function Convert-PSObjectToHashtable {
    param($obj)
    if ($null -eq $obj) { return @{} }
    if ($obj -is [array]) {
        $result = @()
        foreach ($item in $obj) { $result += Convert-PSObjectToHashtable $item }
        return $result
    }
    $ht = @{}
    foreach ($prop in $obj.PSObject.Properties.Name) {
        $val = $obj.$prop
        if ($val -is [PSCustomObject]) {
            $ht[$prop] = Convert-PSObjectToHashtable $val
        } elseif ($val -is [array] -and $val.Count -gt 0 -and $val[0] -is [PSCustomObject]) {
            $arr = @()
            foreach ($item in $val) { $arr += Convert-PSObjectToHashtable $item }
            $ht[$prop] = $arr
        } else {
            $ht[$prop] = $val
        }
    }
    return $ht
}

function Compare-Layers {
    param($snapshot, $current)
    $changes = @()

    # Files in snapshot but not in current (deleted)
    foreach ($file in $snapshot.Keys) {
        if (-not $current.ContainsKey($file)) {
            $changes += [PSCustomObject]@{ File = $file; Type = "deleted"; Details = @() }
        }
    }

    foreach ($file in $current.Keys) {
        if (-not $snapshot.ContainsKey($file)) {
            $details = @()
            $curStrings = $current[$file].strings
            if ($curStrings) {
                foreach ($k in $curStrings.Keys) {
                    $details += [PSCustomObject]@{ Key = $k; Old = $null; New = $curStrings[$k]; Change = "added" }
                }
            }
            $changes += [PSCustomObject]@{ File = $file; Type = "new_file"; Details = $details }
        } else {
            $oldHash = $snapshot[$file].hash
            $newHash = $current[$file].hash
            if ($oldHash -ne $newHash) {
                $oldStrings = $snapshot[$file].strings
                $newStrings = $current[$file].strings
                if (-not $oldStrings) { $oldStrings = @{} }
                if (-not $newStrings) { $newStrings = @{} }
                $details = @()
                foreach ($k in $oldStrings.Keys) {
                    if (-not $newStrings.ContainsKey($k)) {
                        $details += [PSCustomObject]@{ Key = $k; Old = $oldStrings[$k]; New = $null; Change = "deleted" }
                    } elseif ($oldStrings[$k] -ne $newStrings[$k]) {
                        $details += [PSCustomObject]@{ Key = $k; Old = $oldStrings[$k]; New = $newStrings[$k]; Change = "modified" }
                    }
                }
                foreach ($k in $newStrings.Keys) {
                    if (-not $oldStrings.ContainsKey($k)) {
                        $details += [PSCustomObject]@{ Key = $k; Old = $null; New = $newStrings[$k]; Change = "added" }
                    }
                }
                if ($details.Count -gt 0) {
                    $changes += [PSCustomObject]@{ File = $file; Type = "modified"; Details = $details }
                } else {
                    $changes += [PSCustomObject]@{ File = $file; Type = "hash_changed_only"; Details = @() }
                }
            }
        }
    }
    return $changes
}

# ============ REPORT ============

function Write-Report {
    param($gameVerOld, $gameVerNew, $xmlChanges, $dllSrcChanges, $coreDLLChanges, $arcenuiOld, $arcenuiNew)

    $date = Get-Date -Format "yyyy-MM-dd"
    $reportPath = Join-Path $translationDir "translation_update_report_$date.txt"
    $lines = @()

    $lines += "=== AI War 2 Hanhua Update Detection Report ==="
    $lines += "Date: $date"
    $lines += "Game version: $gameVerOld -> $gameVerNew"
    $lines += ""

    # ---- XML ----
    $lines += "[Layer: XML]"
    $xmlOk = 0; $xmlMod = 0; $xmlNew = 0; $xmlDeleted = 0
    foreach ($c in $xmlChanges) {
        if ($c.Type -eq "new_file") { $xmlNew++ }
        elseif ($c.Type -eq "deleted") { $xmlDeleted++ }
        elseif ($c.Type -eq "modified") { $xmlMod++ }
        else { $xmlOk++ }
    }
    $lines += "  OK (no change): $xmlOk"
    if ($xmlMod -gt 0) {
        $lines += "  Modified (needs retranslation): $xmlMod"
        foreach ($c in $xmlChanges) {
            if ($c.Type -ne "modified") { continue }
            $lines += "    - $($c.File)"
            foreach ($d in $c.Details) {
                if ($d.Change -eq "modified") { $lines += "       MODIFIED: $($d.Key): `"$($d.Old)`" -> `"$($d.New)`"" }
                elseif ($d.Change -eq "added") { $lines += "       NEW: $($d.Key): `"$($d.New)`"" }
                elseif ($d.Change -eq "deleted") { $lines += "       DELETED: $($d.Key)" }
            }
        }
    }
    foreach ($c in $xmlChanges) {
        if ($c.Type -ne "hash_changed_only") { continue }
        $lines += "  Hash changed (verify manually): $($c.File)"
    }
    if ($xmlNew -gt 0) {
        $lines += "  New files (needs translation): $xmlNew"
        foreach ($c in $xmlChanges) { if ($c.Type -eq "new_file") { $lines += "    - $($c.File)" } }
    }
    if ($xmlDeleted -gt 0) {
        $lines += "  Deleted files: $xmlDeleted"
        foreach ($c in $xmlChanges) { if ($c.Type -eq "deleted") { $lines += "    - $($c.File)" } }
    }
    $lines += ""

    # ---- DLL Source ----
    $lines += "[Layer: DLL Source]"
    $dllOk = 0; $dllMod = 0; $dllNew = 0
    foreach ($c in $dllSrcChanges) {
        if ($c.Type -eq "new_file") { $dllNew++ }
        elseif ($c.Type -eq "modified") { $dllMod++ }
        else { $dllOk++ }
    }
    $lines += "  OK (no change): $dllOk"
    if ($dllMod -gt 0) {
        $lines += "  Modified (needs retranslation): $dllMod"
        foreach ($c in $dllSrcChanges) {
            if ($c.Type -ne "modified") { continue }
            $lines += "    - $($c.File)"
            $added = $c.Details | Where-Object { $_.Change -eq "added" }
            $mod = $c.Details | Where-Object { $_.Change -eq "modified" }
            $del = $c.Details | Where-Object { $_.Change -eq "deleted" }
            if ($added) { foreach ($d in $added) { $lines += "       NEW: $($d.Key): `"$($d.New)`"" } }
            if ($mod) { foreach ($d in $mod) { $lines += "       MODIFIED: $($d.Key): `"$($d.Old)`" -> `"$($d.New)`"" } }
            if ($del) { foreach ($d in $del) { $lines += "       DELETED: $($d.Key)" } }
        }
    }
    if ($dllNew -gt 0) {
        $lines += "  New files: $dllNew"
        foreach ($c in $dllSrcChanges) { if ($c.Type -eq "new_file") { $lines += "    - $($c.File)" } }
    }
    $lines += ""

    # ---- Core DLL ----
    $lines += "[Layer: Core DLL]"
    $coreChanged = ($coreDLLChanges | Where-Object { $_.Type -eq "modified" -or $_.Type -eq "hash_changed_only" }).Count
    $coreOk = $coreDLLChanges.Count - $coreChanged
    $lines += "  OK (no change): $coreOk"
    if ($coreChanged -gt 0) {
        $lines += "  Hash changed (needs re-decompilation): $coreChanged"
        foreach ($c in $coreDLLChanges) {
            if ($c.Type -eq "hash_changed_only") {
                $lines += "    - $($c.File) (re-decompile with ilspycmd)"
            }
        }
    }
    $lines += ""

    # ---- arcenui ----
    $lines += "[Layer: arcenui AssetBundle]"
    if ($arcenuiOld -and $arcenuiNew) {
        $oldS = $arcenuiOld.strings
        $newS = $arcenuiNew.strings
        $added = @(); $removed = @()
        foreach ($k in $newS.Keys) { if (-not $oldS.ContainsKey($k)) { $added += $k } }
        foreach ($k in $oldS.Keys) { if (-not $newS.ContainsKey($k)) { $removed += $k } }
        $lines += "  OK (no change): $($oldS.Count - $removed.Count)"
        if ($added.Count -gt 0) { $lines += "  New strings: $($added.Count)"; foreach ($s in $added) { $lines += "    - `"${s}`"" } }
        if ($removed.Count -gt 0) { $lines += "  Removed strings: $($removed.Count)" }
        if ($arcenuiOld.hash -ne $arcenuiNew.hash) { $lines += "  Bundle binary hash changed (content updated)" }
    } elseif ($arcenuiNew) {
        $lines += "  $($arcenuiNew.strings.Count) strings tracked"
    } else {
        $lines += "  Not available"
    }
    $lines += ""

    # ---- Summary ----
    $totalOk = $xmlOk + $dllOk + $coreOk
    $totalWarn = $xmlMod + $dllMod + $coreChanged
    $totalNew = $xmlNew + $dllNew
    $lines += "[Summary]"
    $lines += "  OK (no action needed): $totalOk"
    $lines += "  Needs retranslation: $totalWarn"
    if ($totalNew -gt 0) { $lines += "  New files to translate: $totalNew" }

    $reportContent = $lines -join "`r`n"
    [System.IO.File]::WriteAllText($reportPath, $reportContent, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Report written: $reportPath" -ForegroundColor Green
    Write-Host ""
    Write-Host $reportContent
    return $reportPath
}

# ============ MAIN ============
$gameVersion = Get-GameVersionFunc
if (-not $gameVersion) {
    Write-Host "ERROR: Cannot determine game version. Check KDL_GameVersions.xml." -ForegroundColor Red
    exit 1
}

if ($snapshot) {
    # ---- SNAPSHOT MODE ----
    if ((Test-Path $snapshotFile) -and -not $force) {
        Write-Host "ERROR: Snapshot already exists. Use -snapshot -force to overwrite." -ForegroundColor Red
        exit 1
    }
    if (Test-Path $snapshotFile) {
        $existingSnap = Get-Content $snapshotFile -Raw -Encoding UTF8 | ConvertFrom-Json
        Write-Host "WARNING: Overwriting existing snapshot (version $($existingSnap.game_version))" -ForegroundColor Yellow
    }
    if (-not (Test-GameIsEnglish)) {
        Write-Host "ERROR: Game directory contains Chinese text. Snapshot must be taken from English game files." -ForegroundColor Red
        Write-Host "  Run Steam Verify, or take snapshot before deploying translations." -ForegroundColor Yellow
        exit 1
    }
    Build-Snapshot $gameVersion
} else {
    # ---- DEFAULT: DETECTION MODE ----
    if (-not (Test-Path $snapshotFile)) {
        Write-Host "ERROR: No baseline snapshot found. Run 'check_update.ps1 -snapshot' first." -ForegroundColor Red
        exit 1
    }

    $snapData = Get-Content $snapshotFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $snapVersion = $snapData.game_version

    if ($snapVersion -eq $gameVersion) {
        Write-Host "Version $gameVersion unchanged. Snapshot is current. Nothing to report." -ForegroundColor Green
        exit 0
    }

    Write-Host "Game updated: snapshot v$snapVersion -> current v$gameVersion" -ForegroundColor Cyan
    Write-Host "Extracting current game files..." -ForegroundColor Yellow

    $xmlCurrent = Extract-XMLStrings $configDir
    $dllSrcCurrent = Extract-DLLSourceStrings $codeExtDir
    $coreDLLCurrent = Extract-CoreDLLs $managedDir
    $arcenuiCurrent = Extract-ArcenUIStrings

    $snapXml = Convert-PSObjectToHashtable $snapData.layers.xml
    $snapDllSrc = Convert-PSObjectToHashtable $snapData.layers.dll_source
    $snapCoreDLL = Convert-PSObjectToHashtable $snapData.layers.dll_core
    $snapArcenui = Convert-PSObjectToHashtable $snapData.layers.arcenui

    Write-Host "Comparing with snapshot..." -ForegroundColor Yellow
    $xmlChanges = Compare-Layers $snapXml $xmlCurrent
    $dllSrcChanges = Compare-Layers $snapDllSrc $dllSrcCurrent
    $coreDLLChanges = Compare-Layers $snapCoreDLL $coreDLLCurrent

    $reportPath = Write-Report $snapVersion $gameVersion $xmlChanges $dllSrcChanges $coreDLLChanges $snapArcenui $arcenuiCurrent
    Write-Host ""
    Write-Host "Full report saved to: $reportPath" -ForegroundColor Cyan
}
