param([string]$Dotnet='dotnet',[string]$AutoCADDir="$env:ProgramFiles\Autodesk\AutoCAD 2027")
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & $Dotnet build -c Release "-p:AutoCADDir=$AutoCADDir"
    if($LASTEXITCODE -ne 0){throw 'Plugin build failed.'}
    $output=Join-Path $root '.artifacts'
    $bundle=Join-Path $output 'CadProductivityPalette.bundle'
    $contents=Join-Path $bundle 'Contents/Windows'
    New-Item -ItemType Directory -Path $contents -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'packaging/PackageContents.xml') -Destination $bundle -Force
    foreach($name in @('CadProductivityPalette.dll','CadProductivityPalette.deps.json')) {
        Copy-Item -LiteralPath (Join-Path $root "src/CadProductivityPalette/bin/Release/net10.0-windows/$name") -Destination $contents -Force
    }
    $zip=Join-Path $output 'CadProductivityPalette-AutoCAD2027.zip'
    Compress-Archive -LiteralPath $bundle -DestinationPath $zip -Force
    Write-Host "Bundle ready: $zip"
} finally {Pop-Location}
