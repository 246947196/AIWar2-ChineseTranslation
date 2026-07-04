# Add UTF-8 BOM to all XML files in translation folder and deploy
$translationDir = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
$gameDir = "D:\Steam\steamapps\common\AI War 2"

Write-Host "=== Adding UTF-8 BOM to XML files ===" -ForegroundColor Cyan

# Process translation folder XML files
$files = Get-ChildItem -Path $translationDir -Filter "*.xml" -Recurse
$added = 0; $skipped = 0
foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    if ($hasBom) { $skipped++; continue }
    
    $newBytes = [byte[]]::new($bytes.Length + 3)
    $newBytes[0] = [byte]0xEF
    $newBytes[1] = [byte]0xBB
    $newBytes[2] = [byte]0xBF
    [Array]::Copy($bytes, 0, $newBytes, 3, $bytes.Length)
    [System.IO.File]::WriteAllBytes($f.FullName, $newBytes)
    $added++
}
Write-Host "Added BOM to $added files, skipped $skipped (already had BOM)" -ForegroundColor Green

# Deploy GameData
Write-Host "`nDeploying GameData..." -ForegroundColor Yellow
$transConfig = Join-Path $translationDir "GameData\Configuration"
$gameConfig = Join-Path $gameDir "GameData\Configuration"
$deployed = 0
Get-ChildItem -Path $transConfig -Recurse -Filter "*.xml" | ForEach-Object {
    $rel = $_.FullName.Substring($transConfig.Length + 1)
    $dest = Join-Path $gameConfig $rel
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $_.FullName $dest -Force
    $deployed++
}
Write-Host "Deployed $deployed GameData XML files" -ForegroundColor Green

# Deploy XMLMods
Write-Host "Deploying XMLMods..." -ForegroundColor Yellow
$transMods = Join-Path $translationDir "XMLMods"
$gameMods = Join-Path $gameDir "XMLMods"
$modsDeployed = 0
Get-ChildItem -Path $transMods -Recurse -Filter "*.xml" -ErrorAction SilentlyContinue | ForEach-Object {
    $rel = $_.FullName.Substring($transMods.Length + 1)
    $dest = Join-Path $gameMods $rel
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $_.FullName $dest -Force
    $modsDeployed++
}
Write-Host "Deployed $modsDeployed XMLMods files" -ForegroundColor Green

# Verify
Write-Host "`n=== Verification ===" -ForegroundColor Cyan
$bomCount = 0; $total = 0
Get-ChildItem -Path $gameConfig -Filter "*.xml" -Recurse | ForEach-Object {
    $total++
    $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { $bomCount++ }
}
Write-Host "GameData: $bomCount BOM / $total total" -ForegroundColor $(if ($bomCount -eq $total) {"Green"} else {"Yellow"})

$corrupted = 0
Get-ChildItem -Path $gameConfig -Filter "*.xml" -Recurse | ForEach-Object {
    $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0 -and $bytes[1] -eq 0 -and $bytes[2] -eq 0) { $corrupted++ }
}
if ($corrupted -gt 0) { Write-Host "WARNING: $corrupted corrupted files!" -ForegroundColor Red }
else { Write-Host "No corrupted files" -ForegroundColor Green }

Write-Host "`nDone. Start game to test." -ForegroundColor Cyan
