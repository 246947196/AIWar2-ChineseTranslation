# Build script for translated DLLs
# Usage: .\build.ps1

$ErrorActionPreference = "Stop"
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
$roslynDir = "C:\Users\Administrator\AppData\Local\Temp\roslyn412\tasks\net472"

$projects = @(
    "DLLSource\AIWarExternalCode\AIWarExternalCode.csproj",
    "DLLSource\AIWarExternalDeepProcessingCode\AIWarExternalDeepProcessingCode.csproj",
    "DLLSource\AIWarExternalVisualizationCode\AIWarExternalVisualizationCode.csproj"
)

$bepInExProjects = @(
    "DLLSource\ArcenUIAssetRedirect\ArcenUIAssetRedirect.csproj",
    "DLLSource\WorldTMPFontPatch\WorldTMPFontPatch.csproj"
)

function Build-MSBuildProject {
    param($projPath)
    $projName = [System.IO.Path]::GetFileName($projPath)
    Write-Host "Building $projName..." -ForegroundColor Cyan
    
    # Record pre-build hash for cache detection
    $projDir = Split-Path $projPath
    $dllName = [System.IO.Path]::GetFileNameWithoutExtension($projName) + ".dll"
    $outDll = Join-Path $baseDir "DLLBin\$dllName"
    $preHash = $null
    if (Test-Path $outDll) {
        $preHash = (Get-FileHash $outDll -Algorithm SHA256).Hash
    }
    
    # Clean obj/Release to avoid stale incremental cache
    $objDir = Join-Path $projDir "obj\Release"
    if (Test-Path $objDir) {
        Remove-Item -Recurse -Force $objDir
    }
    
    $buildOutput = & $msbuild $projPath /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir" /nologo 2>&1
    
    # Post-build hash comparison
    $objDll = Join-Path $projDir "obj\Release\$dllName"
    if (Test-Path $objDll) {
        Copy-Item $objDll $outDll -Force
        $postHash = (Get-FileHash $outDll -Algorithm SHA256).Hash
        $size = [math]::Round((Get-Item $outDll).Length / 1KB)
        if ($preHash -and $preHash -eq $postHash) {
            Write-Host "  WARNING: $dllName hash unchanged ($size KB) - cache may be stale!" -ForegroundColor DarkYellow
            Write-Host "  If translations didn't take effect, run: Remove-Item -Recurse -Force '$objDir'" -ForegroundColor DarkYellow
        } else {
            Write-Host "  OK: $dllName ($size KB)" -ForegroundColor Green
        }
    } else {
        Write-Host "  FAILED: $dllName not found" -ForegroundColor Red
        $buildOutput | Select-Object -Last 10
        exit 1
    }
}

foreach ($proj in $projects) {
    $projPath = Join-Path $baseDir $proj
    Build-MSBuildProject $projPath
}

foreach ($proj in $bepInExProjects) {
    $projPath = Join-Path $baseDir $proj
    Build-MSBuildProject $projPath
}

# Build arcenui AssetBundle (patch with Chinese translations)
Write-Host "`nPatching arcenui AssetBundle..." -ForegroundColor Cyan
$patchScript = Join-Path $baseDir "patch_arcenui.py"
if (Test-Path $patchScript) {
    python $patchScript patch 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  arcenui patched successfully" -ForegroundColor Green
    } else {
        Write-Host "  arcenui patch FAILED" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  patch_arcenui.py not found, skipping" -ForegroundColor DarkYellow
}

Write-Host "`nAll DLLs built successfully!" -ForegroundColor Green
Get-ChildItem (Join-Path $baseDir "DLLBin") -Filter "*.dll" | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1KB)) KB)"
}
