[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$DestinationRoot,
    [string]$Source = (Join-Path $PSScriptRoot '../assets/Materials')
)
$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath($Source).TrimEnd('\', '/')
$destination = [IO.Path]::GetFullPath($DestinationRoot).TrimEnd('\', '/')
$target = [IO.Path]::GetFullPath((Join-Path $destination '~Materials'))
if ((Split-Path -Parent $target) -ne $destination -or (Split-Path -Leaf $target) -ne '~Materials' `
    -or $target -eq $sourceRoot -or $sourceRoot.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase) `
    -or $target.StartsWith($sourceRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Prepared-material source and destination must be separate directories.'
}
# Native payloads only; authoring files, documentation and placeholders never ship.
$extensions = @('.Surface', '.Material', '.Texture', '.VTTexture', '.VTSurface', '.MidMips', '.Geometry', '.Prefab')
$payloads = @()
if (Test-Path -LiteralPath $sourceRoot) {
    $entries = @(Get-Item -LiteralPath $sourceRoot) + @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -Force)
    if ($entries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
        throw 'Prepared-material source must not contain reparse points.'
    }
    $payloads = @($entries | Where-Object { -not $_.PSIsContainer -and $_.Extension -in $extensions })
}
$normalRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../assets/Materials')).TrimEnd('\', '/')
if ($sourceRoot -eq $normalRoot -or @($payloads | Where-Object Extension -eq '.Surface').Length) {
    dotnet run --project (Join-Path $PSScriptRoot 'MaterialTexturePolicy') -- $sourceRoot (Join-Path $PSScriptRoot 'PreparedMaterials/Common/RoadTextureConsumers.json')
    if ($LASTEXITCODE -ne 0) { throw 'Prepared texture consumer validation failed.' }
}
$copies = [Collections.Generic.List[object]]::new()
foreach ($payload in $payloads) {
    $relative = [IO.Path]::GetRelativePath($sourceRoot, $payload.FullName)
    if ($relative -notmatch '^[A-Za-z][A-Za-z0-9]*[\\/]') {
        throw "Prepared material must belong to a stable bridge style directory: $relative"
    }
    $cid = $payload.FullName + '.cid'
    if (-not (Test-Path -LiteralPath $cid -PathType Leaf) -or
        [IO.File]::ReadAllText($cid).Trim() -notmatch '^[a-fA-F0-9]{32}$') {
        throw "Prepared native payload requires its original CID sidecar: $relative"
    }
    if ($relative -match '^Shared[\\/]') {
        if (($relative -notmatch '^Shared[\\/]MidMips[\\/]([a-f0-9]{32})[\\/]TS[0-9]+_MB[0-9]+\.MidMips$' -and
             $relative -notmatch '^Shared[\\/]Paint[\\/]([a-f0-9]{32})\.Texture$') -or
            [IO.File]::ReadAllText($cid).Trim() -ne $Matches[1]) {
            throw "Only reviewed CID-scoped Paint textures and native MidMips are permitted in the common material root: $relative"
        }
    }
    $copies.Add([pscustomobject]@{ Source = $payload.FullName; Relative = $relative })
    $copies.Add([pscustomobject]@{ Source = $cid; Relative = $relative + '.cid' })
}
# Exposed legacy inputs are unsafe even when no bridge is generated. Preserve originals
# outside the game and build trees before removing their automatic discovery path.
$legacy = [IO.Path]::GetFullPath((Join-Path $destination 'Materials'))
if ((Split-Path -Parent $legacy) -ne $destination) { throw 'Invalid legacy material path.' }
if (Test-Path -LiteralPath $legacy) {
    if ($legacy -eq $sourceRoot -or $sourceRoot.StartsWith($legacy + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Legacy output is the requested source; cannot relocate it.'
    }
    $legacyEntries = @(Get-Item -LiteralPath $legacy) + @(Get-ChildItem -LiteralPath $legacy -Recurse -Force)
    if ($legacyEntries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw 'Legacy materials contain reparse points.' }
    $backupRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'BridgeBuilder/MaterialBackups'
    $backup = [IO.Path]::GetFullPath((Join-Path $backupRoot ([Guid]::NewGuid().ToString('N'))))
    if ((Split-Path -Parent $backup) -ne [IO.Path]::GetFullPath($backupRoot)) { throw 'Invalid backup path.' }
    [IO.Directory]::CreateDirectory($backupRoot) | Out-Null
    Move-Item -LiteralPath $legacy -Destination $backup
    Write-Host "Legacy exposed materials moved to original-only backup: $backup"
}
if (Test-Path -LiteralPath $target) {
    $existing = @(Get-Item -LiteralPath $target) + @(Get-ChildItem -LiteralPath $target -Recurse -Force)
    if ($existing | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
        throw 'Prepared-material destination must not contain reparse points.'
    }
    # This exact, verified mod-owned subtree is replaced; unrelated output stays intact.
    Remove-Item -LiteralPath $target -Recurse -Force
}
foreach ($copy in $copies) {
    $path = Join-Path $target $copy.Relative
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    Copy-Item -LiteralPath $copy.Source -Destination $path
    if ((Get-FileHash -LiteralPath $copy.Source).Hash -ne (Get-FileHash -LiteralPath $path).Hash) {
        throw "Prepared-material copy differs: $($copy.Relative)"
    }
}
Write-Host "Prepared materials: $($payloads.Count) native payloads copied; bytes and relative paths preserved."
