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
New-Item -ItemType Directory -Path "$gameDir\PatchedAssemblies" -Force | Out-Null
Copy-Item "$translationDir\BepInEx\core\*" "$gameDir\BepInEx\core\" -Force
Copy-Item "$translationDir\BepInEx\patchers\AssemblyRedirector.dll" "$gameDir\BepInEx\patchers\" -Force
Copy-Item "$translationDir\BepInEx\config\xiaoye97.I18NFont4UnityGame.cfg" "$gameDir\BepInEx\config\" -Force
Copy-Item "$translationDir\BepInEx\config\BepInEx.cfg" "$gameDir\BepInEx\config\" -Force
# Copy Arcen DLLs to PatchedAssemblies for AssemblyRedirector
Copy-Item "$gameDir\AIWar2_Data\Managed\ArcenAIW2Core.dll" "$gameDir\PatchedAssemblies\" -Force
Copy-Item "$gameDir\AIWar2_Data\Managed\ArcenAIW2Visualization.dll" "$gameDir\PatchedAssemblies\" -Force
Copy-Item "$gameDir\AIWar2_Data\Managed\ArcenUniversal.dll" "$gameDir\PatchedAssemblies\" -Force
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

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host
