# AI War 2 Translation Check Script
# Check which translation files were overwritten after game update

$translationDir = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation"
$gameDir = "D:\Steam\steamapps\common\AI War 2"
$configDir = Join-Path $gameDir "GameData\Configuration"
$transConfigDir = Join-Path $translationDir "GameData\Configuration"

Write-Host "=== AI War 2 Translation Check ===" -ForegroundColor Cyan
Write-Host ""

# Check game version
$versionFile = Join-Path $configDir "GameVersion\KDL_GameVersions.xml"
if (Test-Path $versionFile) {
    $versionContent = Get-Content $versionFile -Raw -ErrorAction SilentlyContinue
    if ($versionContent) {
        $versions = [regex]::Matches($versionContent, 'major_version="(\d+)".*?minor_version="(\d+)"')
        $latest = $versions | ForEach-Object {
            $major = [int]$_.Groups[1].Value
            $minor = [int]$_.Groups[2].Value
            [PSCustomObject]@{ Major=$major; Minor=$minor; Version="$major.$minor" }
        } | Sort-Object Major,Minor -Descending | Select-Object -First 1
        Write-Host "Game Version: $($latest.Version)" -ForegroundColor Green
    } else {
        Write-Host "Cannot read version file" -ForegroundColor Red
    }
} else {
    Write-Host "Version file not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Scanning translation files..." -ForegroundColor Yellow

# Files that don't need translation (whitelist)
$whitelist = @(
    "AstroTrainBehaviorType\AstroTrainBehaviorType.xml",
    "CustomSystemType\Vanilla_CustomSystemTypes.xml",
    "GameVersion\KDL_GameVersions.xml",
    "HackingType\DeprecatedHacks.xml",
    "ObjectiveDetailsHooks\CMMP_ObjectiveDetailsHooksForCapturablesOfSpecificSorts.xml",
    "TextStyles\TextStyles_Vanilla.xml"
)

# Scan translated files in game directory
$translated = @()
$overwritten = @()

Get-ChildItem -Path $transConfigDir -Recurse -Filter "*.xml" | ForEach-Object {
    $relPath = $_.FullName.Substring($transConfigDir.Length + 1)
    
    # Skip whitelisted files
    if ($whitelist -contains $relPath) { return }
    
    $gameFile = Join-Path $configDir $relPath
    if (Test-Path $gameFile) {
        $bytes = [System.IO.File]::ReadAllBytes($gameFile)
        $hasChinese = $false
        for ($i = 0; $i -lt $bytes.Length - 1; $i++) {
            if (($bytes[$i] -ge 0xE4) -and ($bytes[$i] -le 0xE9) -and ($bytes[$i+1] -ge 0x80) -and ($bytes[$i+1] -le 0xBF)) {
                $hasChinese = $true
                break
            }
        }
        
        if ($hasChinese) {
            $translated += $relPath
        } else {
            $overwritten += $relPath
        }
    }
}

# Output results
Write-Host ""
Write-Host "=== Results ===" -ForegroundColor Cyan

Write-Host ""
Write-Host "Translated files: $($translated.Count)" -ForegroundColor Green

if ($overwritten.Count -gt 0) {
    Write-Host ""
    Write-Host "Possibly overwritten files: $($overwritten.Count)" -ForegroundColor Red
    $overwritten | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
} else {
    Write-Host ""
    Write-Host "All translation files are OK" -ForegroundColor Green
}

Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host
