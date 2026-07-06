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
    "DLLSource\ArcenUIAssetRedirect\ArcenUIAssetRedirect.csproj"
)

function Build-MSBuildProject {
    param($projPath)
    Write-Host "Building $([System.IO.Path]::GetFileName($projPath))..." -ForegroundColor Cyan
    return & $msbuild $projPath /t:Build /p:Configuration=Release "/p:CscToolPath=$roslynDir" /nologo 2>&1
}

foreach ($proj in $projects) {
    $projPath = Join-Path $baseDir $proj
    $output = Build-MSBuildProject $projPath
    
    $projDir = Split-Path $projPath
    $dllName = [System.IO.Path]::GetFileNameWithoutExtension($proj) + ".dll"
    $objDll = Join-Path $projDir "obj\Release\$dllName"
    $binDll = Join-Path $baseDir "DLLBin\$dllName"
    
    if (Test-Path $objDll) {
        Copy-Item $objDll $binDll -Force
        $size = [math]::Round((Get-Item $binDll).Length / 1KB)
        Write-Host "  OK: $dllName ($size KB)" -ForegroundColor Green
    } else {
        Write-Host "  FAILED: $dllName not found" -ForegroundColor Red
        $output | Select-Object -Last 10
        exit 1
    }
}

foreach ($proj in $bepInExProjects) {
    $projPath = Join-Path $baseDir $proj
    $output = Build-MSBuildProject $projPath
    
    $projDir = Split-Path $projPath
    $dllName = [System.IO.Path]::GetFileNameWithoutExtension($proj) + ".dll"
    $binDll = Join-Path $projDir "bin\Release\$dllName"
    $outputDll = Join-Path $baseDir "DLLBin\$dllName"
    
    if (Test-Path $binDll) {
        Copy-Item $binDll $outputDll -Force
        $size = [math]::Round((Get-Item $outputDll).Length / 1KB)
        Write-Host "  OK: $dllName ($size KB)" -ForegroundColor Green
    } else {
        Write-Host "  FAILED: $dllName not found at $binDll" -ForegroundColor Red
        $output | Select-Object -Last 10
        exit 1
    }
}

Write-Host "`nAll DLLs built successfully!" -ForegroundColor Green
Get-ChildItem (Join-Path $baseDir "DLLBin") -Filter "*.dll" | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1KB)) KB)"
}
