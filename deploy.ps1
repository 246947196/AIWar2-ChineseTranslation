# AI War 2 Deployment Script
# Deploy all translation components to game directory
#
# 路径可移植：默认按「脚本所在目录 = 汉化项目目录、其父目录 = 游戏根目录」推导，
# 也可以用 -GameDir 显式指定，例如：
#   .\deploy.ps1 -GameDir "E:\SteamLibrary\steamapps\common\AI War 2"

param(
    [string]$GameDir = "",
    [switch]$Force,
    [switch]$IncludeExternalDlls
)

$translationDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($translationDir)) {
    $translationDir = (Get-Location).Path
}

function Resolve-GameDir([string]$explicit) {
    if ($explicit -and (Test-Path (Join-Path $explicit "AIWar2.exe"))) { return (Resolve-Path $explicit).Path }
    $parent = Split-Path $translationDir -Parent
    if ($parent -and (Test-Path (Join-Path $parent "AIWar2.exe"))) { return $parent }
    # 兜底：从 Steam 注册表找库目录
    foreach ($key in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $steam = (Get-ItemProperty -Path $key -ErrorAction Stop).SteamPath
            if (-not $steam) { continue }
            $libs = @($steam)
            $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
            if (Test-Path $vdf) {
                $libs += (Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' -AllMatches |
                          ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value -replace '\\\\', '\' })
            }
            foreach ($lib in $libs) {
                $cand = Join-Path $lib 'steamapps\common\AI War 2'
                if (Test-Path (Join-Path $cand "AIWar2.exe")) { return (Resolve-Path $cand).Path }
            }
        } catch { }
    }
    return ""
}

$gameDir = Resolve-GameDir $GameDir
if (-not $gameDir) {
    Write-Host "ERROR: 找不到 AI War 2 游戏目录。" -ForegroundColor Red
    Write-Host "  请把汉化项目放在游戏目录下，或用参数指定：" -ForegroundColor Yellow
    Write-Host '    .\deploy.ps1 -GameDir "E:\SteamLibrary\steamapps\common\AI War 2"' -ForegroundColor Yellow
    exit 1
}

Write-Host "  游戏目录: $gameDir"
Write-Host "  汉化目录: $translationDir"

$ErrorActionPreference = "Stop"

Write-Host "=== AI War 2 Deployment ===" -ForegroundColor Cyan
Write-Host ""

# ---- Pre-flight: game process check ----
$gameProc = Get-Process -Name "AIWar2" -ErrorAction SilentlyContinue
if ($gameProc) {
    Write-Host "WARNING: AI War 2 is still running!" -ForegroundColor Yellow
    Write-Host "  Patched DLLs will NOT take effect until the game is restarted." -ForegroundColor Yellow
    Write-Host "  Also, ilpatch may produce .new.dll if the Managed DLLs are locked." -ForegroundColor Yellow
    $choice = Read-Host "  Close game now? (y/N)"
    if ($choice -eq 'y') {
        Stop-Process -Name "AIWar2" -Force
        Start-Sleep -Seconds 2
        Write-Host "  Game closed." -ForegroundColor Green
    }
}

# ---- 备份游戏原文件（任何覆盖之前）----
# 部署会改写游戏自己的 Configuration / XMLMods / ModdableLogicDLLs 等，
# 没有备份就无法回滚。只备份游戏原装部分，BepInEx 与 PatchedAssemblies 是
# 汉化产物，不占备份体积。
$backupScript = Join-Path $translationDir "tools\game_backup.ps1"
if (Test-Path $backupScript) {
    & $backupScript -GameDir $gameDir
    if (-not $?) {
        Write-Host "ERROR: 备份失败，已中止部署（不做无备份的覆盖）。" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "WARNING: 未找到 tools\game_backup.ps1，跳过备份" -ForegroundColor Yellow
    $ans = Read-Host "  没有备份仍继续？(y/N)"
    if ($ans -ne 'y') { Write-Host "  已取消。" -ForegroundColor Yellow; exit 1 }
}

# ---- Clean .new.dll residues from Managed and PatchedAssemblies ----
# These are left over from previous ilpatch runs when DLLs were locked.
# They silently override the real patched DLLs and cause translations to revert.
$managedDir = Join-Path $gameDir "AIWar2_Data\Managed"
$patchedDir = Join-Path $gameDir "PatchedAssemblies"
$transPatched = Join-Path $translationDir "PatchedAssemblies"
foreach ($dir in @($managedDir, $patchedDir, $transPatched)) {
    if (Test-Path $dir) {
        $leftovers = Get-ChildItem $dir -Filter "*.new.dll" -Recurse -ErrorAction SilentlyContinue
        foreach ($f in $leftovers) {
            Remove-Item $f.FullName -Force
            Write-Host "  Cleaned .new.dll residue: $($f.Name)" -ForegroundColor DarkYellow
        }
    }
}

# ---- Pre-flight: XML validation ----
# 属性值里的裸 < > 或半角 " 会让 .NET XmlDocument 抛异常，整个文件被游戏跳过，
# 结果是该模组/实体全部回退英文甚至消失，而且日志里只有一行，极难发现。
# 校验失败一律中止部署，避免把坏文件铺到游戏里。
$xmlGuard = Join-Path $translationDir "tools\xml_guard.py"
if (Test-Path $xmlGuard) {
    $pythonCmd = Get-Command python -ErrorAction SilentlyContinue
    if ($pythonCmd) {
        Push-Location $translationDir
        try {
            & python $xmlGuard validate
            $guardExit = $LASTEXITCODE
        } finally {
            Pop-Location
        }
        if ($guardExit -ne 0) {
            Write-Host ""
            Write-Host "ERROR: XML 校验未通过，已中止部署。" -ForegroundColor Red
            Write-Host "  自动修复：python tools\\xml_guard.py fix --apply" -ForegroundColor Yellow
            Write-Host "  结构比对：python tools\\xml_guard.py diff" -ForegroundColor Yellow
            exit 1
        }
        Write-Host "  XML 校验通过" -ForegroundColor Green
    }
    else {
        Write-Host "WARNING: 未找到 python，跳过 XML 校验" -ForegroundColor Yellow
    }
}

# ---- Version check ----
$snapshotFile = Join-Path $translationDir "translation_snapshot.json"
if (-not (Test-Path $snapshotFile)) {
    Write-Host "ERROR: No baseline snapshot found. Run check_update.ps1 -snapshot first." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to exit..."
if ([Environment]::UserInteractive -and $Host.Name -eq 'ConsoleHost') { Read-Host "按回车退出" }
    exit 1
}

$snapshotRaw = Get-Content $snapshotFile -Raw -Encoding UTF8
$snapshotVersion = if ($snapshotRaw -match '"game_version"\s*:\s*"([^"]+)"') { $Matches[1] } else { "unknown" }

$versionFile = Join-Path $gameDir "GameData\Configuration\GameVersion\KDL_GameVersions.xml"
if (-not (Test-Path $versionFile)) {
    Write-Host "ERROR: Game version file KDL_GameVersions.xml not found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to exit..."
if ([Environment]::UserInteractive -and $Host.Name -eq 'ConsoleHost') { Read-Host "按回车退出" }
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
if ([Environment]::UserInteractive -and $Host.Name -eq 'ConsoleHost') { Read-Host "按回车退出" }
    exit 1
}

$gameVersion = $latest.Version

if ($snapshotVersion -ne $gameVersion) {
    Write-Host "WARNING: 基线版本 ($snapshotVersion) 与游戏版本 ($gameVersion) 不一致。" -ForegroundColor Yellow
    Write-Host "  XML 部分没问题：build_output 是按当前游戏版本现生成的，不会回退数值。" -ForegroundColor Gray
    Write-Host "  DLL 部分有风险：IL 补丁是针对特定游戏 DLL 做的，版本不符可能导致启动异常。" -ForegroundColor Yellow
    $genExists = Test-Path (Join-Path $translationDir "build_output\GameData\Configuration")
    if (-not $genExists) {
        Write-Host "ERROR: 既没有匹配的基线，也没有 build_output —— 部署会把游戏数据退回旧版本，已中止。" -ForegroundColor Red
        Write-Host "  先执行: python tools\localize_xml.py build" -ForegroundColor Yellow
        exit 1
    }
    if (-not $Force) {
        $ans = Read-Host "  继续部署？(y/N)"
        if ($ans -ne 'y') { Write-Host "  已取消。" -ForegroundColor Yellow; exit 1 }
    } else {
        Write-Host "  已指定 -Force，继续。" -ForegroundColor Yellow
    }
} else {
    Write-Host "Version OK: baseline $snapshotVersion = game $gameVersion" -ForegroundColor Green
}
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
    $newDll = Join-Path $transPatched "$asm.new.dll"
    # Prefer .new.dll (side-by-side fallback from ilpatch when DLL was locked)
    if (Test-Path $newDll) {
        Copy-Item $newDll "$gameDir\PatchedAssemblies\$asm.dll" -Force
        Write-Host "  Deployed IL-patched (from .new.dll fallback): $asm.dll" -ForegroundColor Gray
        Remove-Item $newDll -Force
    } elseif (Test-Path $translated) {
        Copy-Item $translated "$gameDir\PatchedAssemblies\" -Force
        Write-Host "  Deployed IL-patched: $asm.dll" -ForegroundColor Gray
    } else {
        Copy-Item "$gameDir\AIWar2_Data\Managed\$asm.dll" "$gameDir\PatchedAssemblies\" -Force
        Write-Host "  Deployed original (no IL patch yet): $asm.dll" -ForegroundColor DarkYellow
    }
}
Write-Host "BepInEx framework deployed" -ForegroundColor Green

# ---- Verify IL-patched DLLs ----
$verifyScript = Join-Path $translationDir "tools\ilpatch\verify_patch.py"
$transPatched = Join-Path $translationDir "PatchedAssemblies"
$deployedPatched = Join-Path $gameDir "PatchedAssemblies"

foreach ($asmName in @("ArcenAIW2Core", "ArcenUniversal", "ArcenAIW2Visualization")) {
    $mergedJson = Join-Path $translationDir "tools\ilpatch\$asmName.merged.json"
    $deployedDll = "$deployedPatched\$asmName.dll"
    
    if ((Test-Path $verifyScript) -and (Test-Path $mergedJson) -and (Test-Path $deployedDll)) {
        Write-Host "  Verifying $asmName..." -ForegroundColor Yellow
        $result = & python $verifyScript $deployedDll $mergedJson 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    PASS: $asmName" -ForegroundColor Green
        } else {
            Write-Host "    FAIL: $asmName - some translations may not have been written" -ForegroundColor Red
            Write-Host "    Run: python tools/ilpatch/verify_patch.py PatchedAssemblies\$asmName.dll tools/ilpatch/$asmName.merged.json" -ForegroundColor Gray
        }
    }
}

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

# 优先使用「按当前游戏版本现生成」的 XML（老做法是整份 XML 分叉，会把游戏数值
# 一并退回分叉那天）。生成命令：python tools\localize_xml.py build
$genRoot = Join-Path $translationDir "build_output"
$useGenerated = Test-Path (Join-Path $genRoot "GameData\Configuration")
if ($useGenerated) {
    $transConfigDir = Join-Path $genRoot "GameData\Configuration"
    Write-Host "  使用 build_output（按当前游戏版本生成，数值/新字段不会被回退）" -ForegroundColor Green
} else {
    $transConfigDir = Join-Path $translationDir "GameData\Configuration"
    Write-Host "  WARNING: 未找到 build_output，退回使用整份 XML 分叉（可能与当前游戏版本不符）" -ForegroundColor Yellow
}
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
    $tooltipFiles = @(Get-ChildItem -Path $transQuickStartsDir -Recurse -Filter "*.tooltip")
    $deployedTooltips = 0
    foreach ($f in $tooltipFiles) {
        $relPath = $f.FullName.Substring($transQuickStartsDir.Length + 1)
        $destFile = Join-Path $gameQuickStartsDir $relPath
        $destDir = Split-Path $destFile -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $f.FullName $destFile -Force
        $deployedTooltips++
    }
    Write-Host "Deployed $deployedTooltips QuickStarts2 tooltip files" -ForegroundColor Green
} else {
    Write-Host "  QuickStarts2 directory not found, skipping" -ForegroundColor DarkYellow
}

# Deploy Expansion (DLC) QuickStarts2 translated tooltip files
Write-Host ""
Write-Host "Deploying Expansion QuickStarts2 translated tooltip files..." -ForegroundColor Yellow
$transExpQuickStartsDir = Join-Path $translationDir "Expansions"
if (Test-Path $transExpQuickStartsDir) {
    $expTooltipFiles = @(Get-ChildItem -Path $transExpQuickStartsDir -Recurse -Filter "*.tooltip")
    $deployedExpTooltips = 0
    foreach ($f in $expTooltipFiles) {
        $relPath = $f.FullName.Substring($transExpQuickStartsDir.Length + 1)
        $destFile = "$gameDir\Expansions\$relPath"
        $destDir = Split-Path $destFile -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $f.FullName $destFile -Force
        $deployedExpTooltips++
    }
    Write-Host "Deployed $deployedExpTooltips Expansion QuickStarts2 tooltip files" -ForegroundColor Green
} else {
    Write-Host "  Expansions QuickStarts2 directory not found, skipping" -ForegroundColor DarkYellow
}

# Merge expansion (DLC) translation overrides into the real expansion config dirs.
# Each override is overlaid onto the game's English source file (by name) so the result is
# a COMPLETE Chinese file written into Expansions\<exp>\GameData\Configuration, guaranteeing
# translated text wins without losing any game content.
Write-Host ""
Write-Host "Merging expansion (DLC) translation overrides into game expansions..." -ForegroundColor Yellow
if ($useGenerated) {
    $genExp = Join-Path $genRoot "Expansions"
    $expCopied = 0
    if (Test-Path $genExp) {
        Get-ChildItem -Path $genExp -Directory | ForEach-Object {
            $srcCfg = Join-Path $_.FullName "GameData\Configuration"
            $dstCfg = Join-Path $gameDir "Expansions\$($_.Name)\GameData\Configuration"
            if (Test-Path $srcCfg) {
                Get-ChildItem -Path $srcCfg -Recurse -Filter "*.xml" | ForEach-Object {
                    $rel = $_.FullName.Substring($srcCfg.Length + 1)
                    $dst = Join-Path $dstCfg $rel
                    $dstDir = Split-Path $dst -Parent
                    if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Path $dstDir -Force | Out-Null }
                    Copy-Item $_.FullName $dst -Force
                    $expCopied++
                }
            }
        }
    }
    Write-Host "  Copied $expCopied expansion XML files from build_output" -ForegroundColor Green
} else {
    $mergeScript = Join-Path $translationDir "merge_expansion_translations.py"
    if (Test-Path $mergeScript) {
        python $mergeScript
    } else {
        Write-Host "  ERROR: merge_expansion_translations.py not found; DLC translations NOT deployed" -ForegroundColor Red
    }
}

# Deploy XMLMods translation files
Write-Host ""
Write-Host "Deploying XMLMods translation files..." -ForegroundColor Yellow
if ($useGenerated) {
    $transModsDir = Join-Path $genRoot "XMLMods"
} else {
    $transModsDir = Join-Path $translationDir "XMLMods"
}
$gameModsDir = Join-Path $gameDir "XMLMods"

if (Test-Path $transModsDir) {
    # Copy XML files (entity definitions, planet name type metadata, etc.)
    $modsFilesToDeploy = @(Get-ChildItem -Path $transModsDir -Recurse -Filter "*.xml")
    $modsDeployed = 0
    foreach ($f in $modsFilesToDeploy) {
        $relPath = $f.FullName.Substring($transModsDir.Length + 1)
        $destFile = Join-Path $gameModsDir $relPath
        $destDir = Split-Path $destFile -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $f.FullName $destFile -Force
        $modsDeployed++
    }
    # Copy text files (planet name lists, etc.)
    $txtFilesToDeploy = @(Get-ChildItem -Path $transModsDir -Recurse -Filter "*.txt")
    $txtDeployed = 0
    foreach ($f in $txtFilesToDeploy) {
        $relPath = $f.FullName.Substring($transModsDir.Length + 1)
        $destFile = Join-Path $gameModsDir $relPath
        $destDir = Split-Path $destFile -Parent
        
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        
        Copy-Item $f.FullName $destFile -Force
        $txtDeployed++
    }
    Write-Host "Deployed $modsDeployed XML files and $txtDeployed text files to XMLMods" -ForegroundColor Green
} else {
    Write-Host "  XMLMods directory not found, skipping" -ForegroundColor DarkYellow
}

# Deploy TotalConversions translation files
Write-Host ""
Write-Host "Deploying TotalConversions translation files..." -ForegroundColor Yellow
$transTConsDir = Join-Path $translationDir "TotalConversions"
$gameTConsDir = Join-Path $gameDir "TotalConversions"

if (Test-Path $transTConsDir) {
    $tcFilesToDeploy = @(Get-ChildItem -Path $transTConsDir -Recurse -Filter "*.xml")
    $tcFilesToDeploy += @(Get-ChildItem -Path $transTConsDir -Recurse -Filter "*.txt")
    $tcDeployed = 0
    foreach ($f in $tcFilesToDeploy) {
        $relPath = $f.FullName.Substring($transTConsDir.Length + 1)
        $destFile = Join-Path $gameTConsDir $relPath
        $destDir = Split-Path $destFile -Parent

        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        Copy-Item $f.FullName $destFile -Force
        $tcDeployed++
    }
    Write-Host "Deployed $tcDeployed files to TotalConversions" -ForegroundColor Green
} else {
    Write-Host "  TotalConversions directory not found, skipping" -ForegroundColor DarkYellow
}

# Deploy translated DLLs
Write-Host ""
Write-Host "Deploying translated DLLs..." -ForegroundColor Yellow
$dllBinDir = Join-Path $translationDir "DLLBin"
if (Test-Path $dllBinDir) {
    # AIWarExternal* 是「7 月源码分叉」编译出来的，装上去等于把游戏的代码逻辑
    # 退回 7 月（和整份 XML 分叉同理）。除非显式 -IncludeExternalDlls，默认跳过；
    # 正确做法是把译文抽成 IL 补丁打到当前版本的 DLL 上（见 AGENTS）。
    $skippedForked = @()
    $dllFiles = Get-ChildItem $dllBinDir -Filter "*.dll" | Where-Object {
        if ($IncludeExternalDlls) { return $true }
        if ($_.Name -like 'AIWarExternal*') { $skippedForked += $_.Name; return $false }
        return $true
    }
    if ($skippedForked.Count -gt 0) {
        Write-Host "  跳过 7 月源码分叉的外部代码 DLL（加 -IncludeExternalDlls 强制部署）:" -ForegroundColor Yellow
        $skippedForked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkYellow }
    }
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

# Deploy ChineseTranslation BepInEx plugins
Write-Host ""
Write-Host "Deploying ChineseTranslation plugins..." -ForegroundColor Yellow
$pluginDir = "$gameDir\BepInEx\plugins\ChineseTranslation"
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null

$redirectDll = Join-Path $translationDir "DLLBin\ArcenUIAssetRedirect.dll"
if (Test-Path $redirectDll) {
    Copy-Item $redirectDll "$pluginDir\" -Force
    Write-Host "  Deployed: ArcenUIAssetRedirect.dll ($([math]::Round((Get-Item $redirectDll).Length/1KB)) KB)" -ForegroundColor Gray
} else {
    Write-Host "  ArcenUIAssetRedirect.dll not found in DLLBin, skipping" -ForegroundColor DarkYellow
}

$worldTmpDll = Join-Path $translationDir "DLLBin\WorldTMPFontPatch.dll"
if (Test-Path $worldTmpDll) {
    Copy-Item $worldTmpDll "$pluginDir\" -Force
    Write-Host "  Deployed: WorldTMPFontPatch.dll ($([math]::Round((Get-Item $worldTmpDll).Length/1KB)) KB)" -ForegroundColor Gray
} else {
    Write-Host "  WorldTMPFontPatch.dll not found in DLLBin, skipping" -ForegroundColor DarkYellow
}

# Deploy extracted TTF font for WorldTMPFontPatch
$ttfSrc = Join-Path $translationDir "tools\mi_sans.ttf"
$ttfDst = "$pluginDir\mi_sans.ttf"
if (Test-Path $ttfSrc) {
    Copy-Item $ttfSrc $ttfDst -Force
    Write-Host "  Deployed: mi_sans.ttf ($([math]::Round((Get-Item $ttfDst).Length/1KB)) KB)" -ForegroundColor Gray
}

Write-Host "ChineseTranslation plugins deployed" -ForegroundColor Green

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
if ($gameProc) {
    Write-Host "IMPORTANT: Restart AI War 2 for the new translations to take effect!" -ForegroundColor Yellow
}
Write-Host "If translations still show English after restart, check:" -ForegroundColor Gray
Write-Host "  1. Was ilpatch run? Confirm: dotnet tools/ilpatch/bin/ilpatch.dll dump-ldstr" -ForegroundColor Gray
Write-Host "     PatchedAssemblies/ArcenAIW2Core.dll | findstr 等待" -ForegroundColor Gray
Write-Host "  2. Is AssemblyRedirector.dll in BepInEx/patchers/?" -ForegroundColor Gray
Write-Host "  3. Run: python tools/ilpatch/verify_patch.py" -ForegroundColor Gray
Write-Host "     PatchedAssemblies\ArcenAIW2Core.dll tools\ilpatch\ArcenAIW2Core.merged.json" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Enter to exit..."
if ([Environment]::UserInteractive -and $Host.Name -eq 'ConsoleHost') { Read-Host "按回车退出" }
