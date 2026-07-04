# AI War 2 Deployment Script
# Deploy all translation components to game directory

$translationDir = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
$gameDir = "D:\Steam\steamapps\common\AI War 2"

Write-Host "=== AI War 2 Deployment ===" -ForegroundColor Cyan
Write-Host ""

# Deploy BepInEx framework
Write-Host "Deploying BepInEx framework..." -ForegroundColor Yellow
Copy-Item "$translationDir\winhttp.dll" "$gameDir\" -Force
Copy-Item "$translationDir\doorstop_config.ini" "$gameDir\" -Force
New-Item -ItemType Directory -Path "$gameDir\BepInEx\core" -Force | Out-Null
New-Item -ItemType Directory -Path "$gameDir\BepInEx\patchers" -Force | Out-Null
New-Item -ItemType Directory -Path "$gameDir\BepInEx\config" -Force | Out-Null
Copy-Item "$translationDir\BepInEx\core\*" "$gameDir\BepInEx\core\" -Force
Copy-Item "$translationDir\BepInEx\patchers\AssemblyRedirector.dll" "$gameDir\BepInEx\patchers\" -Force
Copy-Item "$translationDir\BepInEx\config\xiaoye97.I18NFont4UnityGame.cfg" "$gameDir\BepInEx\config\" -Force
Write-Host "BepInEx framework deployed" -ForegroundColor Green

# Deploy I18NFont4UnityGame plugin
Write-Host ""
Write-Host "Deploying I18NFont4UnityGame plugin..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "$gameDir\BepInEx\plugins\I18NFont4UnityGame" -Force | Out-Null
Copy-Item "$translationDir\BepInEx\plugins\I18NFont4UnityGame\I18NFont4UnityGame.dll" "$gameDir\BepInEx\plugins\I18NFont4UnityGame\" -Force
Copy-Item "$translationDir\BepInEx\plugins\I18NFont4UnityGame\sarasa_gothic" "$gameDir\BepInEx\plugins\I18NFont4UnityGame\" -Force
Write-Host "I18NFont4UnityGame plugin deployed" -ForegroundColor Green

# Deploy translation XML files
Write-Host ""
Write-Host "Deploying translation XML files..." -ForegroundColor Yellow
$transConfigDir = Join-Path $translationDir "GameData\Configuration"
$gameConfigDir = Join-Path $gameDir "GameData\Configuration"

$filesToDeploy = Get-ChildItem -Path $transConfigDir -Recurse -Filter "*.xml"
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

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host
