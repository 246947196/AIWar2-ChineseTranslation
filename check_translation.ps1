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
# These are configuration files with technical identifiers, not player-visible text
$whitelist = @(
    # 基础游戏 - 不需要翻译的文件类型
    "AstroTrainBehaviorType\AstroTrainBehaviorType.xml",
    "CustomSystemType\Vanilla_CustomSystemTypes.xml",
    "GameVersion\KDL_GameVersions.xml",
    "HackingType\DeprecatedHacks.xml",
    "ObjectiveDetailsHooks\CMMP_ObjectiveDetailsHooksForCapturablesOfSpecificSorts.xml",
    "TextStyles\TextStyles_Vanilla.xml",
    "TextStyles\TextStyles_Terms_Vanilla.xml",
    "TextVarMaps\TextVarMaps_Vanilla.xml",
    # External* 系列（纯数值配置）
    "ExternalConstants\*.xml",
    "ExternalFactionBaseInfo\*.xml",
    "ExternalFactionDeepInfo\*.xml",
    "ExternalFleetBaseInfo\*.xml",
    "ExternalGameEntityTypeDataExtension\*.xml",
    "ExternalSquadBaseInfo\*.xml",
    "ExternalSquadDeepInfo\*.xml",
    "ExternalWorldBaseInfo\*.xml",
    "ExternalWorldDeepInfo\*.xml",
    "ExternalCodeHook\*.xml",
    "ExternalDeepLink\*.xml",
    "ExternalDllInitialLoadCall\*.xml",
    "ExternalVisualConstants\*.xml",
    "ExternalVisualCoreConstants\*.xml",
    # Balance_* 系列（纯数值配置）
    "Balance_*.xml",
    # 其他配置文件
    "AIShipGroup\*.xml",
    "AIShipGroupCategory\*.xml",
    "AIDefensePlacer\*.xml",
    "AIGuardPostAndCommandPlacer\*.xml",
    "ArcenThreadingRequestType\*.xml",
    "CameraType\*.xml",
    "FramerateType\*.xml",
    "UIPrefab\*.xml",
    "UIWindow\*.xml",
    "ParticlePattern\*.xml",
    "SpaceboxDefinition\*.xml",
    "PlanetDefinition\*.xml",
    "PlanetNames\*.xml",
    "TextEmbededSprites\*.xml",
    "IconShipStatus\*.xml",
    "CustomOrderType\*.xml",
    "ObjectiveGenerator\*.xml",
    "SpecialFactionProcessingGroup\*.xml",
    "SurrogateTable\*.xml",
    "XmlModNamesToIgnoreNow\*.xml",
    "ChatClickHandler\*.xml",
    "GameCommand\*.xml",
    "ShipVoiceGroup\*.xml",
    "SFXBus\*.xml",
    "TeamColorPrefabs\*.xml",
    "VassalOrderCategory\*.xml",
    "DarkSpire_HarvestRatio\*.xml",
    "InstigatorData\*.xml",
    "LookupSwaps\*.xml",
    "NonSimEffect\*.xml",
    "NPCShipCapType\*.xml",
    "SphereFactionDifficulty\*.xml",
    "TargetedInputAction\*.xml",
    "TargetEvaluator\*.xml",
    "TechUpgrade\*.xml",
    "TestChamber\*.xml",
    # DLC1 External files
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalFactionBaseInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalFactionDeepInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalFleetBaseInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalGameEntityTypeDataExtension\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalSquadBaseInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalSquadDeepInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalWorldBaseInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalWorldDeepInfo\*.xml",
    "Expansions\1_The_Spire_Rises\GameData\Configuration\ExternalConstants\*.xml",
    # DLC2 External files
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalConstants\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalFactionBaseInfo\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalFactionDeepInfo\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalFleetBaseInfo\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalGameEntityTypeDataExtension\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalSquadBaseInfo\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalSquadDeepInfo\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalWorldBaseInfo\*.xml",
    "Expansions\2_Zenith_Onslaught\GameData\Configuration\ExternalWorldDeepInfo\*.xml",
    # DLC3 External files
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalFactionBaseInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalFactionDeepInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalFleetBaseInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalGameEntityTypeDataExtension\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalSquadBaseInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalSquadDeepInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalWorldBaseInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalWorldDeepInfo\*.xml",
    "Expansions\3_The_Neinzul_Abyss\GameData\Configuration\ExternalConstants\*.xml",
    # DLC 配置文件
    "Expansions\*\GameData\Configuration\AIShipGroup\*.xml",
    "Expansions\*\GameData\Configuration\AIShipGroupCategory\*.xml",
    "Expansions\*\GameData\Configuration\Balance_*.xml",
    "Expansions\*\GameData\Configuration\SpecialFactionProcessingGroup\*.xml",
    "Expansions\*\GameData\Configuration\SurrogateTable\*.xml",
    "Expansions\*\GameData\Configuration\*Difficulty\*.xml"
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
