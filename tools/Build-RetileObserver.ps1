param(
    [Parameter(Mandatory=$true)][string]$Python,
    [string]$Output = 'artifacts/agent-material-env/shared-resume/retile-observer'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$target = [IO.Path]::GetFullPath((Join-Path $root $Output))
& $Python (Join-Path $PSScriptRoot 'ExportRetileObserver.py') $target --cpu
if ($LASTEXITCODE -ne 0) { throw 'Observer source export failed.' }
# Build.ps1 must have completed for the intended coherent source/resource snapshot.
$response = Join-Path $root 'src/BridgeBuilder/obj/Release/DirectBuild/BridgeBuilder.rsp'
$lines = Get-Content -LiteralPath $response | ForEach-Object {
    if ($_ -match '^"/out:') { '"/out:' + (Join-Path $target 'BridgeBuilder.Observer.dll') + '"' }
    else { $_ }
}
$lines += '"' + (Join-Path $target 'MovableStructureRetile.cs') + '"'
$observerResponse = Join-Path $target 'observer.rsp'
$lines | Set-Content -LiteralPath $observerResponse
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$compiler = Join-Path (Split-Path $dotnet) ('sdk/' + ((& $dotnet --version).Trim()) + '/Roslyn/bincore/csc.dll')
Push-Location $root
try {
    & $dotnet $compiler "@$observerResponse"
    if ($LASTEXITCODE -ne 0) { throw 'Observer assembly compilation failed.' }
} finally { Pop-Location }
Get-FileHash -LiteralPath (Join-Path $target 'BridgeBuilder.Observer.dll') -Algorithm SHA256
