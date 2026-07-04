# AI War 2 Deployment Script
# Deploy translated files from translation folder to game directory

$translationDir = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
$gameDir = "D:\Steam\steamapps\common\AI War 2"
$configDir = Join-Path $gameDir "GameData\Configuration"

Write-Host "=== AI War 2 Deployment ===" -ForegroundColor Cyan
Write-Host ""

# Check translation folder
$transConfigDir = Join-Path $translationDir "GameData\Configuration"
if (-not (Test-Path $transConfigDir)) {
    Write-Host "Translation folder not found: $transConfigDir" -ForegroundColor Red
    Write-Host "Press Enter to exit..."
    Read-Host
    return
}

# Count files to deploy
$filesToDeploy = Get-ChildItem -Path $transConfigDir -Recurse -Filter "*.xml"
Write-Host "Files to deploy: $($filesToDeploy.Count)" -ForegroundColor Green

# Deploy files
$deployed = 0
$filesToDeploy | ForEach-Object {
    $relPath = $_.FullName.Substring($transConfigDir.Length + 1)
    $destFile = Join-Path $configDir $relPath
    $destDir = Split-Path $destFile -Parent
    
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    Copy-Item $_.FullName $destFile -Force
    $deployed++
}

Write-Host ""
Write-Host "Deployed: $deployed files" -ForegroundColor Green
Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host
