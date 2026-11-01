# 打包成玩家可直接用的安装包
#
# 产出：dist\AIWar2_Chinese_<版本>_<日期>.zip
#   ├─ 安装.ps1          右键运行，自动找游戏目录、备份、覆盖
#   ├─ 卸载.ps1          从备份还原
#   ├─ 说明.txt
#   └─ _payload\         要覆盖进游戏目录的全部文件（与游戏目录同构）
#
# 直接流式写入 zip，不落暂存目录（避免几百个临时文件，也更快）。
# 依赖：先跑 python tools\localize_xml.py build
#
# 用法：
#   .\tools\make_release.ps1              # 含 arcenui UI 包
#   .\tools\make_release.ps1 -NoBundle    # 不含 UI 包（菜单按钮保持英文）

param(
    [switch]$NoBundle,
    [string]$OutDir = "",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path $PSScriptRoot -Parent
$gameDir = Split-Path $projectDir -Parent

if (-not (Test-Path (Join-Path $gameDir "AIWar2.exe"))) {
    Write-Host "ERROR: 汉化目录的上级不是游戏目录，无法打包" -ForegroundColor Red
    exit 1
}
$buildDir = Join-Path $projectDir "build_output"
if (-not (Test-Path (Join-Path $buildDir "GameData\Configuration"))) {
    Write-Host "ERROR: 没有 build_output，先执行: python tools\localize_xml.py build" -ForegroundColor Red
    exit 1
}

if (-not $Version) {
    $vf = Join-Path $gameDir "GameData\Configuration\GameVersion\KDL_GameVersions.xml"
    $ver = "unknown"
    if (Test-Path $vf) {
        $m = [regex]::Matches((Get-Content $vf -Raw), 'major_version="(\d+)"[^>]*minor_version="(\d+)"') |
             Select-Object -Last 1
        if ($m) { $ver = "$($m.Groups[1].Value).$($m.Groups[2].Value)" }
    }
    $Version = $ver
}
if (-not $OutDir) { $OutDir = Join-Path $projectDir "dist" }
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd'
$zipPath = Join-Path $OutDir ("AIWar2_Chinese_v${Version}_${stamp}" + $(if ($NoBundle) { "_nobundle" } else { "" }) + ".zip")
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

function Get-Files($root) {
    if (-not (Test-Path $root)) { return @() }
    return @(Get-ChildItem $root -Recurse -File |
        Where-Object { $_.Extension -notin @('.bak', '.bak2') -and $_.Name -notlike '*.new.dll' })
}

$sources = @()
# 1) XML（build_output 与游戏目录同构）
$sources += Get-Files $buildDir | ForEach-Object {
    @{ Path = $_.FullName; Entry = "_payload/" + $_.FullName.Substring($buildDir.Length + 1).Replace('\', '/') }
}
# 2) IL 补丁后的核心 DLL（只取顶层，备份目录不进包）
$paDir = Join-Path $projectDir "PatchedAssemblies"
$paFull = (Resolve-Path $paDir).Path
$sources += Get-Files $paDir | Where-Object { $_.DirectoryName -eq $paFull } | ForEach-Object {
    @{ Path = $_.FullName; Entry = "_payload/PatchedAssemblies/" + $_.Name }
}
# 3) BepInEx 框架 + 插件 + 字体
foreach ($sub in @('core', 'patchers', 'plugins\I18NFont4UnityGame', 'plugins\ChineseTranslation')) {
    $src = Join-Path $projectDir "BepInEx\$sub"
    $sources += Get-Files $src | ForEach-Object {
        @{ Path = $_.FullName; Entry = "_payload/BepInEx/" + $sub.Replace('\', '/') + "/" + $_.FullName.Substring($src.Length + 1).Replace('\', '/') }
    }
}
# 4) 汉化插件 DLL（从 DLLBin 取，不是外部代码分叉）
foreach ($name in @('ArcenUIAssetRedirect.dll', 'WorldTMPFontPatch.dll')) {
    $src = Join-Path $projectDir "DLLBin\$name"
    if (Test-Path $src) {
        $sources += @{ Path = $src; Entry = "_payload/BepInEx/plugins/ChineseTranslation/$name" }
    }
}
# 5) arcenui UI 包（212MB，压缩后约 35MB）
$bundleSrc = Join-Path $gameDir "BepInEx\plugins\ChineseTranslation\AssetBundles_Win\arcenui"
if ((-not $NoBundle) -and (Test-Path $bundleSrc)) {
    $sources += @{ Path = $bundleSrc; Entry = "_payload/BepInEx/plugins/ChineseTranslation/AssetBundles_Win/arcenui" }
}

Write-Host "打包中（版本 $Version，$($sources.Count) 个文件）..." -ForegroundColor Cyan

$installer = @'
# AI War 2 汉化 安装脚本（游戏版本 __VER__）
param([string]$GameDir = "", [switch]$NoBackup)

$ErrorActionPreference = "Stop"
function Pause { try { if ([Environment]::UserInteractive -and $Host.Name -eq 'ConsoleHost') { Read-Host "按回车退出" } } catch { } }
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$payload = Join-Path $here "_payload"

function Find-GameDir([string]$explicit) {
    if ($explicit -and (Test-Path (Join-Path $explicit "AIWar2.exe"))) { return (Resolve-Path $explicit).Path }
    $d = $here
    for ($i = 0; $i -lt 4; $i++) {
        $d = Split-Path $d -Parent
        if ($d -and (Test-Path (Join-Path $d "AIWar2.exe"))) { return $d }
    }
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

if (-not (Test-Path $payload)) { Write-Host "ERROR: 缺少 _payload 目录，包不完整" -ForegroundColor Red; Pause; exit 1 }
if (Get-Process -Name "AIWar2" -ErrorAction SilentlyContinue) {
    Write-Host "请先关闭 AI War 2 再安装。" -ForegroundColor Yellow; Pause; exit 1
}
$game = Find-GameDir $GameDir
if (-not $game) {
    Write-Host "没找到 AI War 2。请这样指定：" -ForegroundColor Yellow
    Write-Host '  .\安装.ps1 -GameDir "E:\SteamLibrary\steamapps\common\AI War 2"' -ForegroundColor Gray
    Pause; exit 1
}
Write-Host "游戏目录: $game" -ForegroundColor Cyan

if (-not $NoBackup) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $backupDir = Join-Path $here "_backup"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    $zipPath = Join-Path $backupDir ("game_" + (Get-Date -Format 'yyyyMMdd_HHmmss') + ".zip")
    $targets = @('GameData\Configuration','GameData\ModdableLogicDLLs','GameData\QuickStarts2','XMLMods','TotalConversions')
    Get-ChildItem (Join-Path $game 'Expansions') -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { $targets += "Expansions\$($_.Name)\GameData" }
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
    $n = 0
    try {
        foreach ($rel in $targets) {
            $full = Join-Path $game $rel
            if (-not (Test-Path $full)) { continue }
            Get-ChildItem $full -Recurse -File | ForEach-Object {
                $entryName = $_.FullName.Substring($game.Length + 1).Replace('\', '/')
                [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                    $zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal)
                $n++
            }
        }
    } finally { $zip.Dispose() }
    Write-Host ("备份完成: {0} 个文件, {1:N0} MB" -f $n, ((Get-Item $zipPath).Length / 1MB)) -ForegroundColor Green
    Write-Host "  回滚: .\卸载.ps1 -Backup `"$zipPath`"" -ForegroundColor Gray
}

$copied = 0
Get-ChildItem $payload -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($payload.Length).TrimStart('\')
    $dst = Join-Path $game $rel
    $dstDir = Split-Path $dst -Parent
    if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Path $dstDir -Force | Out-Null }
    Copy-Item $_.FullName $dst -Force
    $copied++
}
Write-Host "安装完成：覆盖 $copied 个文件。" -ForegroundColor Green
Write-Host "启动游戏即可；若文字仍是英文，看 BepInEx\LogOutput.log 里有没有 AssemblyRedirector。" -ForegroundColor Gray
Pause
'@

$uninstaller = @'
# AI War 2 汉化 卸载/回滚（游戏版本 __VER__）
param([string]$Backup = "", [string]$GameDir = "")

$ErrorActionPreference = "Stop"
function Pause { try { if ([Environment]::UserInteractive -and $Host.Name -eq 'ConsoleHost') { Read-Host "按回车退出" } } catch { } }
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-GameDir([string]$explicit) {
    if ($explicit -and (Test-Path (Join-Path $explicit "AIWar2.exe"))) { return (Resolve-Path $explicit).Path }
    $d = $here
    for ($i = 0; $i -lt 4; $i++) {
        $d = Split-Path $d -Parent
        if ($d -and (Test-Path (Join-Path $d "AIWar2.exe"))) { return $d }
    }
    return ""
}

$game = Find-GameDir $GameDir
if (-not $game) { Write-Host "找不到游戏目录，用 -GameDir 指定" -ForegroundColor Red; Pause; exit 1 }

if ($Backup) {
    if (-not (Test-Path $Backup)) {
        $cand = Join-Path (Join-Path $here "_backup") $Backup
        if (Test-Path $cand) { $Backup = $cand } else { Write-Host "找不到备份: $Backup" -ForegroundColor Red; Pause; exit 1 }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $Backup).Path, $game, $true)
    Write-Host "已从备份还原: $Backup" -ForegroundColor Green
    Pause
    exit 0
}

Write-Host "未指定 -Backup，只移除汉化运行时文件（游戏原配置保留）..." -ForegroundColor Yellow
foreach ($rel in @('BepInEx\plugins\ChineseTranslation', 'PatchedAssemblies')) {
    $p = Join-Path $game $rel
    if (Test-Path $p) { Remove-Item $p -Recurse -Force; Write-Host "  已删除 $rel" -ForegroundColor Gray }
}
Write-Host "完成。被覆盖过的 XML 需要 -Backup 指定备份才能还原，或用 Steam「验证文件完整性」。" -ForegroundColor Yellow
Pause
'@

$readme = @"
AI War 2 中文汉化  $Version
打包日期: $(Get-Date -Format 'yyyy-MM-dd HH:mm')

安装：
  1. 关闭游戏
  2. 右键 安装.ps1 → 使用 PowerShell 运行
     （若提示禁止运行脚本，先执行一次：
       Set-ExecutionPolicy -Scope Process Bypass -Force）
  3. 脚本自动找游戏目录，先备份再覆盖
  4. 启动游戏

卸载：
  .\卸载.ps1 -Backup <备份文件名>   完整还原
  .\卸载.ps1                        只移除插件与补丁 DLL

说明：
  - 备份位置：本目录 _backup\game_<时间戳>.zip
  - 主菜单按钮等 UI 文字来自 arcenui 包，本包$(if ($NoBundle) { "未包含（保持英文）" } else { "已包含" })
  - 若中文显示为方框，说明字体包未生效，看 BepInEx\LogOutput.log
"@

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    foreach ($s in $sources) {
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $s.Path, $s.Entry, [System.IO.Compression.CompressionLevel]::Optimal)
    }
    foreach ($doc in @(
        @{ Name = '安装.ps1'; Text = ($installer -replace '__VER__', $Version) },
        @{ Name = '卸载.ps1'; Text = ($uninstaller -replace '__VER__', $Version) },
        @{ Name = '说明.txt'; Text = $readme }
    )) {
        $entry = $zip.CreateEntry($doc.Name)
        $w = New-Object System.IO.StreamWriter($entry.Open(), (New-Object System.Text.UTF8Encoding($false)))
        $w.Write($doc.Text)
        $w.Dispose()
    }
} finally { $zip.Dispose() }

Write-Host ""
Write-Host ("完成: {0}" -f $zipPath) -ForegroundColor Green
Write-Host ("  {0:N0} MB / {1} 个文件 / 版本 {2}" -f ((Get-Item $zipPath).Length / 1MB), $sources.Count, $Version)
