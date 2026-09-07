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

$source = Join-Path $projectRoot 'src\BridgeBuilder\bin\Release'
$userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
if ([string]::IsNullOrWhiteSpace($userProfile)) { $userProfile = $env:USERPROFILE }
if ([string]::IsNullOrWhiteSpace($userProfile)) { throw 'Unable to resolve the current user profile directory.' }
$modsRoot = Join-Path $userProfile 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods'
$target = Join-Path $modsRoot 'BridgeBuilder'
$legacyTarget = Join-Path $modsRoot 'BridgePrefabGenerator'

# BridgeBuilder replaces the old internal mod identity. Keeping both DLLs would load two copies of
# the same systems, settings page and exporter, so installation removes only the exact legacy folder.
$resolvedModsRoot = [IO.Path]::GetFullPath($modsRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$resolvedLegacyTarget = [IO.Path]::GetFullPath($legacyTarget).TrimEnd([IO.Path]::DirectorySeparatorChar)
if ((Split-Path -Parent $resolvedLegacyTarget) -ne $resolvedModsRoot `
    -or (Split-Path -Leaf $resolvedLegacyTarget) -ne 'BridgePrefabGenerator') {
    throw "Refusing unexpected legacy mod target: $resolvedLegacyTarget"
}
if (Test-Path -LiteralPath $resolvedLegacyTarget -PathType Container) {
    Remove-Item -LiteralPath $resolvedLegacyTarget -Recurse -Force
}
[IO.Directory]::CreateDirectory($target) | Out-Null

Copy-Item -LiteralPath (Join-Path $source 'BridgeBuilder.dll') -Destination $target -Force
$pdb = Join-Path $source 'BridgeBuilder.pdb'
if (Test-Path -LiteralPath $pdb) { Copy-Item -LiteralPath $pdb -Destination $target -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\BridgeBuilder.svg') -Destination $target -Force
Write-Host "Installed local code mod: $target"
