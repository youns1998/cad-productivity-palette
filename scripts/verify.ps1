param([string]$Dotnet='dotnet',[string]$AutoCADVersion='2027',[string]$AutoCADDir)
$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
  & $Dotnet run --project tools/ReviewRunner -- --self-test
  if($LASTEXITCODE -ne 0){throw 'Core checks failed.'}
  & $Dotnet run --project tools/ReviewRunner -- samples/inspection-demo.json .codex-preview/reports/demo-review
  if($LASTEXITCODE -ne 0){throw 'Example generation failed.'}
  $argsList=@('build','-c','Release',('-p:AutoCADVersion='+$AutoCADVersion))
  if($AutoCADDir){$argsList+=('-p:AutoCADDir='+$AutoCADDir)}
  & $Dotnet @argsList
  if($LASTEXITCODE -ne 0){throw 'AutoCAD plugin build failed.'}
} finally {Pop-Location}
