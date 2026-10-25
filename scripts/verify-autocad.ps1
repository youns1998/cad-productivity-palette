param(
    [string]$Dotnet = 'dotnet',
    [string]$AutoCADDir = "$env:ProgramFiles\Autodesk\AutoCAD 2027"
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$console = Join-Path $AutoCADDir 'accoreconsole.exe'
if (-not (Test-Path -LiteralPath $console)) { throw "AutoCAD Core Console not found: $console" }
$work = Join-Path $root '.codex-preview/engine-check'
New-Item -ItemType Directory -Path $work -Force | Out-Null
$previousRoot = $env:DOTNET_ROOT
$previousRootX64 = $env:DOTNET_ROOT_X64
Push-Location $root
try {
    & $Dotnet build -c Release "-p:AutoCADDir=$AutoCADDir"
    if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
    $dotnetCommand = Get-Command $Dotnet
    $env:DOTNET_ROOT = Split-Path $dotnetCommand.Source
    $env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
    $dll = (Join-Path $root 'src/CadProductivityPalette/bin/Release/net10.0-windows/CadProductivityPalette.dll').Replace('\','/')
    $dllFolder = (Split-Path $dll).Replace('\','/')
    $report = (Join-Path $work 'review.json').Replace('\','/')
    $csv = (Join-Path $work 'review.csv').Replace('\','/')
    # Remove only previous outputs from this script; a failed load must not pass with stale evidence.
    foreach ($file in @($report,$csv)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    $script = @"
(setvar "FILEDIA" 0)
(setvar "INSUNITS" 0)
(setq portfolioOldPaths (getvar "TRUSTEDPATHS"))
(setvar "TRUSTEDPATHS" (strcat portfolioOldPaths ";$dllFolder"))
_.NETLOAD
"$dll"
(setvar "TRUSTEDPATHS" portfolioOldPaths)
(entmake '((0 . "LWPOLYLINE") (100 . "AcDbEntity") (8 . "0") (62 . 1) (100 . "AcDbPolyline") (90 . 2) (70 . 0) (10 0.0 0.0) (10 20.0 0.0)))
(entmake '((0 . "LINE") (8 . "0") (62 . 256) (10 0.0 10.0 0.0) (11 20.0 10.0 0.0)))
(entmake '((0 . "TEXT") (8 . "0") (62 . 256) (10 0.0 20.0 0.0) (40 . 2.5) (1 . " ")))
CADREVIEWBATCH
$report
CADREVIEWBATCH
$csv
_.QUIT
_Y

"@
    $scriptPath = Join-Path $work 'verify.scr'
    [IO.File]::WriteAllText($scriptPath,$script,[Text.UTF8Encoding]::new($false))
    $stdout = Join-Path $work 'engine.stdout'
    $stderr = Join-Path $work 'engine.stderr'
    $process = Start-Process -FilePath $console -ArgumentList @('/s',('"'+$scriptPath+'"')) -WorkingDirectory $work -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    if (-not $process.WaitForExit(60000)) {
        Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
        throw "AutoCAD timed out. Inspect $work"
    }
    if (-not (Test-Path -LiteralPath $report)) { throw "No report generated. Inspect $work" }
    $result = Get-Content -LiteralPath $report -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($result.EntityCount -ne 3 -or $result.CandidateCount -ne 4 -or $result.Units -ne 0) { throw 'Unexpected AutoCAD analysis result.' }
    foreach ($id in @('OPEN_POLYLINE','DIRECT_COLOR','EMPTY_TEXT','UNSPECIFIED_UNITS')) {
        $finding = $result.Findings | Where-Object RuleId -eq $id
        if ($finding.Count -ne 1) { throw "Unexpected rule count: $id" }
    }
    if (-not (Test-Path -LiteralPath $csv)) { throw 'CSV report missing.' }
    Write-Host "Passed actual AutoCAD engine check: 3 entities, 4 review candidates. Reports: $work"
} finally {
    $env:DOTNET_ROOT = $previousRoot
    $env:DOTNET_ROOT_X64 = $previousRootX64
    Pop-Location
}
