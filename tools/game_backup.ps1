# 游戏原始文件备份 / 回滚
#
# 部署会覆盖游戏自己的配置和 DLL。没有备份就没法回滚，所以这一步必须跑在
# 任何写操作之前。只备份「游戏原装」的部分：
#   GameData\Configuration、GameData\ModdableLogicDLLs、GameData\QuickStarts2、
#   XMLMods、TotalConversions、Expansions\*\GameData
# BepInEx、PatchedAssemblies 是汉化自己的产物，不备份（卸载时直接删即可）。
#
# 用法：
#   .\tools\game_backup.ps1                      # 备份到 _backup\game_<时间戳>.zip
#   .\tools\game_backup.ps1 -Label before_font   # 带标签
#   .\tools\game_backup.ps1 -List                # 列出已有备份
#   .\tools\game_backup.ps1 -Restore <zip路径>    # 回滚

param(
    [string]$GameDir = "",
    [string]$Label = "",
    [string]$Restore = "",
    [switch]$List
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path $PSScriptRoot -Parent

function Resolve-GameDir([string]$explicit) {
    if ($explicit -and (Test-Path (Join-Path $explicit "AIWar2.exe"))) { return (Resolve-Path $explicit).Path }
    $parent = Split-Path $projectDir -Parent
    if ($parent -and (Test-Path (Join-Path $parent "AIWar2.exe"))) { return $parent }
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

$backupDir = Join-Path $projectDir "_backup"

if ($List) {
    if (-not (Test-Path $backupDir)) { Write-Host "还没有任何备份"; exit 0 }
    Get-ChildItem $backupDir -Filter "game_*.zip" | Sort-Object LastWriteTime -Descending |
        ForEach-Object { "{0,8:N1} MB  {1}  {2}" -f ($_.Length / 1MB), $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm'), $_.Name }
    exit 0
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression

if ($Restore) {
    if (-not (Test-Path $Restore)) {
        $cand = Join-Path $backupDir $Restore
        if (Test-Path $cand) { $Restore = $cand } else { Write-Host "ERROR: 找不到备份文件 $Restore" -ForegroundColor Red; exit 1 }
    }
    $gameDir = Resolve-GameDir $GameDir
    if (-not $gameDir) { Write-Host "ERROR: 找不到游戏目录，用 -GameDir 指定" -ForegroundColor Red; exit 1 }
    if (Get-Process -Name "AIWar2" -ErrorAction SilentlyContinue) {
        Write-Host "ERROR: 游戏正在运行，先关掉再回滚" -ForegroundColor Red; exit 1
    }
    Write-Host "回滚 $Restore -> $gameDir" -ForegroundColor Yellow
    [System.IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $Restore).Path, $gameDir, $true)
    Write-Host "完成。" -ForegroundColor Green
    exit 0
}

$gameDir = Resolve-GameDir $GameDir
if (-not $gameDir) { Write-Host "ERROR: 找不到游戏目录，用 -GameDir 指定" -ForegroundColor Red; exit 1 }

$targets = @(
    'GameData\Configuration',
    'GameData\ModdableLogicDLLs',
    'GameData\QuickStarts2',
    'XMLMods',
    'TotalConversions'
)
Get-ChildItem (Join-Path $gameDir 'Expansions') -Directory -ErrorAction SilentlyContinue |
    ForEach-Object { $targets += "Expansions\$($_.Name)\GameData" }

New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$suffix = if ($Label) { "_$Label" } else { "" }
$zipPath = Join-Path $backupDir "game_${stamp}${suffix}.zip"

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
$count = 0
$bytes = 0
try {
    foreach ($rel in $targets) {
        $full = Join-Path $gameDir $rel
        if (-not (Test-Path $full)) { continue }
        Get-ChildItem $full -Recurse -File | ForEach-Object {
            $entryName = $_.FullName.Substring($gameDir.Length + 1).Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal)
            $count++
            $bytes += $_.Length
        }
    }
    # 清单：写进压缩包，回滚时能知道这是什么版本
    $manifest = @{
        created     = (Get-Date).ToString('o')
        game_dir    = $gameDir
        label       = $Label
        targets     = $targets
        file_count  = $count
    } | ConvertTo-Json -Depth 4
    $entry = $zip.CreateEntry('_backup_manifest.json')
    $w = New-Object System.IO.StreamWriter($entry.Open())
    $w.Write($manifest)
    $w.Dispose()
}
finally {
    $zip.Dispose()
}

Write-Host ("备份完成: {0}" -f $zipPath) -ForegroundColor Green
Write-Host ("  {0} 个文件，原始 {1:N1} MB，压缩后 {2:N1} MB" -f $count, ($bytes / 1MB), ((Get-Item $zipPath).Length / 1MB))
Write-Host "  回滚: .\tools\game_backup.ps1 -Restore `"$zipPath`"" -ForegroundColor Gray
