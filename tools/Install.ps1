[CmdletBinding()]
param(
    [string]$GameDir,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'Build.ps1') -GameDir $GameDir -Configuration Release
}

$source = Join-Path $projectRoot 'src\BridgePrefabGenerator\bin\Release'
$userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
if ([string]::IsNullOrWhiteSpace($userProfile)) { $userProfile = $env:USERPROFILE }
if ([string]::IsNullOrWhiteSpace($userProfile)) { throw 'Unable to resolve the current user profile directory.' }
$target = Join-Path $userProfile 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\BridgePrefabGenerator'
[IO.Directory]::CreateDirectory($target) | Out-Null

Copy-Item -LiteralPath (Join-Path $source 'BridgePrefabGenerator.dll') -Destination $target -Force
$pdb = Join-Path $source 'BridgePrefabGenerator.pdb'
if (Test-Path -LiteralPath $pdb) { Copy-Item -LiteralPath $pdb -Destination $target -Force }
Write-Host "Installed local code mod: $target"
