[CmdletBinding()]
param([string]$SnapshotRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'vendor/CS2ModShared'))
$ErrorActionPreference = 'Stop'
$SnapshotRoot = [IO.Path]::GetFullPath($SnapshotRoot).TrimEnd('\', '/')
$manifest = @(Get-Content -LiteralPath (Join-Path $SnapshotRoot 'sources.lock.json') -Raw | ConvertFrom-Json)
$actual = @(Get-ChildItem -LiteralPath (Join-Path $SnapshotRoot 'src') -Recurse -File -Filter '*.cs')
if ($actual.Count -ne $manifest.Count) { throw 'Pinned shared source file count differs from manifest.' }
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $manifest) {
    $path = [IO.Path]::GetFullPath((Join-Path $SnapshotRoot $entry.path))
    if (-not $path.StartsWith($SnapshotRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or -not $seen.Add($path)) {
        throw 'Invalid or duplicate shared source manifest path.'
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256) {
        throw "Pinned shared source changed: $($entry.path). Review and update the source snapshot and manifest together."
    }
}
Write-Output "Verified $($manifest.Count) pinned shared source files."
