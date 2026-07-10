# AI War 2 Deployment Script
# Deploy all translation components to game directory

$translationDir = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
$gameDir = "D:\Steam\steamapps\common\AI War 2"

$ErrorActionPreference = "Stop"

Write-Host "=== AI War 2 Deployment ===" -ForegroundColor Cyan
Write-Host ""

# ---- Version check ----
$snapshotFile = Join-Path $translationDir "translation_snapshot.json"
if (-not (Test-Path $snapshotFile)) {
    Write-Host "ERROR: No baseline snapshot found. Run check_update.ps1 -snapshot first." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to exit..."
    Read-Host
    exit 1
}

$snapshotRaw = Get-Content $snapshotFile -Raw -Encoding UTF8
$snapshotVersion = if ($snapshotRaw -match '"game_version"\s*:\s*"([^"]+)"') { $Matches[1] } else { "unknown" }

$versionFile = Join-Path $gameDir "GameData\Configuration\GameVersion\KDL_GameVersions.xml"
if (-not (Test-Path $versionFile)) {
    Write-Host "ERROR: Game version file KDL_GameVersions.xml not found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to exit..."
    Read-Host
    exit 1
}

$versionContent = Get-Content $versionFile -Raw -ErrorAction SilentlyContinue
$entries = [regex]::Matches($versionContent, '(?s)<game_version\s[^>]*?minor_version="(\d+)"[^>]*?>')
$latest = $entries | ForEach-Object {
    $text = $_.Value
    $minor = [int]$_.Groups[1].Value
    $mMajor = [regex]::Match($text, 'major_version="(\d+)"')
    $major = if ($mMajor.Success) { [int]$mMajor.Groups[1].Value } else { 0 }
    [PSCustomObject]@{ Major=$major; Minor=$minor; Version="$major.$minor" }
} | Sort-Object Major,Minor -Descending | Select-Object -First 1

if (-not $latest) {
    Write-Host "ERROR: Cannot parse game version number." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to exit..."
    Read-Host
    exit 1
}

$gameVersion = $latest.Version

if ($snapshotVersion -ne $gameVersion) {
    Write-Host "ERROR: Baseline version ($snapshotVersion) does not match game version ($gameVersion)." -ForegroundColor Red
    Write-Host "Game may have been updated. Run check_update.ps1 first." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press Enter to exit..."
    Read-Host
    exit 1
}

Write-Host "Version OK: baseline $snapshotVersion = game $gameVersion" -ForegroundColor Green
Write-Host ""

# Deploy BepInEx framework
Write-Host "Deploying BepInEx framework..." -ForegroundColor Yellow
Copy-Item "$translationDir\winhttp.dll" "$gameDir\" -Force
Copy-Item "$translationDir\doorstop_config.ini" "$gameDir\" -Force
New-Item -ItemType Directory -Path "$gameDir\BepInEx\core" -Force | Out-Null
New-Item -ItemType Directory -Path "$gameDir\BepInEx\patchers" -Force | Out-Null
New-Item -ItemType Directory -Path "$gameDir\BepInEx\config" -Force | Out-Null
New-Item -ItemType Directory -Path "$gameDir\PatchedAssemblies" -Force | Out-Null
if (Test-Path "$translationDir\BepInEx\core") {
    Copy-Item "$translationDir\BepInEx\core\*" "$gameDir\BepInEx\core\" -Force
} else {
    Write-Host "  ERROR: BepInEx\core not found in translation directory" -ForegroundColor Red; exit 1
}
Copy-Item "$translationDir\BepInEx\patchers\AssemblyRedirector.dll" "$gameDir\BepInEx\patchers\" -Force
Copy-Item "$translationDir\BepInEx\config\xiaoye97.I18NFont4UnityGame.cfg" "$gameDir\BepInEx\config\" -Force
Copy-Item "$translationDir\BepInEx\config\BepInEx.cfg" "$gameDir\BepInEx\config\" -Force
# Copy Arcen DLLs to PatchedAssemblies for AssemblyRedirector
# Core / Visualization / Universal: 优先使用仓库内的 IL 汉化版（PatchedAssemblies/），否则回退游戏原版。
# 注意：不可无条件从 AIWar2_Data\Managed 拷贝，否则会覆盖掉 ilpatch 写入的中文（见规范 8.15）。
$transPatched = Join-Path $translationDir "PatchedAssemblies"
foreach ($asm in @("ArcenAIW2Core", "ArcenAIW2Visualization", "ArcenUniversal")) {
    $translated = Join-Path $transPatched "$asm.dll"
    if (Test-Path $translated) {
        Copy-Item $translated "$gameDir\PatchedAssemblies\" -Force
        Write-Host "  Deployed IL-patched: $asm.dll" -ForegroundColor Gray
    } else {
        Copy-Item "$gameDir\AIWar2_Data\Managed\$asm.dll" "$gameDir\PatchedAssemblies\" -Force
        Write-Host "  Deployed original (no IL patch yet): $asm.dll" -ForegroundColor DarkYellow
    }
}
Write-Host "BepInEx framework deployed" -ForegroundColor Green

# Deploy I18NFont4UnityGame plugin
Write-Host ""
Write-Host "Deploying I18NFont4UnityGame plugin..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "$gameDir\BepInEx\plugins\I18NFont4UnityGame" -Force | Out-Null
Copy-Item "$translationDir\BepInEx\plugins\I18NFont4UnityGame\I18NFont4UnityGame.dll" "$gameDir\BepInEx\plugins\I18NFont4UnityGame\" -Force
Copy-Item "$translationDir\BepInEx\plugins\I18NFont4UnityGame\sarasa_gothic" "$gameDir\BepInEx\plugins\I18NFont4UnityGame\" -Force -Recurse
Write-Host "I18NFont4UnityGame plugin deployed" -ForegroundColor Green

# Deploy translation XML files
Write-Host ""
Write-Host "Deploying translation XML files..." -ForegroundColor Yellow
$transConfigDir = Join-Path $translationDir "GameData\Configuration"
$gameConfigDir = Join-Path $gameDir "GameData\Configuration"

# NOTE: Expansion (DLC) overrides live under GameData\Configuration\Expansions\ but must be
# merged into the REAL expansion config dirs (Expansions\<exp>\GameData\Configuration), not
# deployed as a nested mirror. Exclude them here; they are handled by the merge step below.
$filesToDeploy = Get-ChildItem -Path $transConfigDir -Recurse -Filter "*.xml" | Where-Object {
    $rel = $_.FullName.Substring($transConfigDir.Length + 1)
    -not $rel.StartsWith("Expansions\")
}
$deployed = 0
$filesToDeploy | ForEach-Object {
    $relPath = $_.FullName.Substring($transConfigDir.Length + 1)
    $destFile = Join-Path $gameConfigDir $relPath
    $destDir = Split-Path $destFile -Parent
    
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    Copy-Item $_.FullName $destFile -Force
    $deployed++
}
Write-Host "Deployed $deployed XML files" -ForegroundColor Green

# Deploy QuickStarts2 translated tooltip files
Write-Host ""
Write-Host "Deploying QuickStarts2 translated tooltip files..." -ForegroundColor Yellow
$transQuickStartsDir = Join-Path $translationDir "GameData\QuickStarts2"
$gameQuickStartsDir = Join-Path $gameDir "GameData\QuickStarts2"

if (Test-Path $transQuickStartsDir) {
    $tooltipFiles = Get-ChildItem -Path $transQuickStartsDir -Recurse -Filter "*.tooltip"
    $deployedTooltips = 0
    $tooltipFiles | ForEach-Object {
        $relPath = $_.FullName.Substring($transQuickStartsDir.Length + 1)
        $destFile = Join-Path $gameQuickStartsDir $relPath
        $destDir = Split-Path $destFile -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $_.FullName $destFile -Force
        $deployedTooltips++
    }
    Write-Host "Deployed $deployedTooltips QuickStarts2 tooltip files" -ForegroundColor Green
} else {
    Write-Host "  QuickStarts2 directory not found, skipping" -ForegroundColor DarkYellow
}

# Merge expansion (DLC) translation overrides into the real expansion config dirs.
# Each override is overlaid onto the game's English source file (by name) so the result is
# a COMPLETE Chinese file written into Expansions\<exp>\GameData\Configuration, guaranteeing
# translated text wins without losing any game content.
Write-Host ""
Write-Host "Merging expansion (DLC) translation overrides into game expansions..." -ForegroundColor Yellow
$mergeScript = Join-Path $translationDir "merge_expansion_translations.py"
if (Test-Path $mergeScript) {
    python $mergeScript
} else {
    Write-Host "  ERROR: merge_expansion_translations.py not found; DLC translations NOT deployed" -ForegroundColor Red
}

# Deploy XMLMods translation files
Write-Host ""
Write-Host "Deploying XMLMods translation files..." -ForegroundColor Yellow
$transModsDir = Join-Path $translationDir "XMLMods"
$gameModsDir = Join-Path $gameDir "XMLMods"

if (Test-Path $transModsDir) {
    $modsFilesToDeploy = Get-ChildItem -Path $transModsDir -Recurse -Filter "*.xml"
    $modsDeployed = 0
    $modsFilesToDeploy | ForEach-Object {
        $relPath = $_.FullName.Substring($transModsDir.Length + 1)
        $destFile = Join-Path $gameModsDir $relPath
        $destDir = Split-Path $destFile -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $_.FullName $destFile -Force
        $modsDeployed++
    }
    Write-Host "Deployed $modsDeployed XMLMods files" -ForegroundColor Green
} else {
    Write-Host "  XMLMods directory not found, skipping" -ForegroundColor DarkYellow
}

# Deploy translated DLLs
Write-Host ""
Write-Host "Deploying translated DLLs..." -ForegroundColor Yellow
$dllBinDir = Join-Path $translationDir "DLLBin"
if (Test-Path $dllBinDir) {
    $dllFiles = Get-ChildItem $dllBinDir -Filter "*.dll"
    $dllDest = Join-Path $gameDir "GameData\ModdableLogicDLLs"
    New-Item -ItemType Directory -Path $dllDest -Force | Out-Null
    foreach ($dll in $dllFiles) {
        Copy-Item $dll.FullName "$dllDest\" -Force
        Write-Host "  Deployed: $($dll.Name) ($([math]::Round($dll.Length/1KB)) KB)" -ForegroundColor Gray
    }
    Write-Host "Deployed $($dllFiles.Count) DLLs" -ForegroundColor Green
} else {
    Write-Host "  DLLBin directory not found, skipping DLL deployment" -ForegroundColor DarkYellow
}

# Deploy ArcenUIAssetRedirect BepInEx plugin
Write-Host ""
Write-Host "Deploying ArcenUIAssetRedirect plugin..." -ForegroundColor Yellow
$pluginDir = "$gameDir\BepInEx\plugins\ChineseTranslation"
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
$redirectDll = Join-Path $translationDir "DLLBin\ArcenUIAssetRedirect.dll"
if (Test-Path $redirectDll) {
    Copy-Item $redirectDll "$pluginDir\" -Force
    Write-Host "  Deployed: ArcenUIAssetRedirect.dll ($([math]::Round((Get-Item $redirectDll).Length/1KB)) KB)" -ForegroundColor Gray
} else {
    Write-Host "  ArcenUIAssetRedirect.dll not found in DLLBin, skipping" -ForegroundColor DarkYellow
}
Write-Host "ArcenUIAssetRedirect plugin deployed" -ForegroundColor Green

# Deploy arcenui AssetBundle
# NOTE: 汉化版 arcenui bundle (约 220MB) 因 GitHub 单文件 100MB 限制无法入库，
#       故不纳入仓库。改为本地生成：仓库内含 patch_arcenui.py + arcenui_translations.json，
#       结合游戏原文 bundle (游戏目录 AssetBundles_Win/arcenui) 重新生成汉化版。
#       若游戏目录已存在汉化 bundle 则跳过；否则提示用户运行 patch_arcenui.py patch。
Write-Host ""
Write-Host "Deploying arcenui AssetBundle..." -ForegroundColor Yellow
$arcenuiBundle = "$pluginDir\AssetBundles_Win\arcenui"
if (Test-Path $arcenuiBundle) {
    Write-Host "  arcenui bundle already present ($([math]::Round((Get-Item $arcenuiBundle).Length/1MB)) MB)" -ForegroundColor Gray
} else {
    Write-Host "  arcenui bundle not found. 请先运行: python patch_arcenui.py patch" -ForegroundColor DarkYellow
    Write-Host "  （需本地游戏原文 bundle；生成的汉化版将写入游戏目录对应位置）" -ForegroundColor DarkYellow
}
Write-Host "arcenui AssetBundle deployment checked" -ForegroundColor Green

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host
