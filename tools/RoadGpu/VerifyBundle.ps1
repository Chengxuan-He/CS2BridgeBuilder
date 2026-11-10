$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$manifest=Get-Content -LiteralPath (Join-Path $root 'assets/roadgpu.json') -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath (Join-Path $root 'assets/roadgpu')).Hash -ne $manifest.bundleSha256){throw 'Road GPU bundle fingerprint differs; rebuild with tools/RoadGpu/BuildBundle.ps1.'}
foreach($entry in $manifest.sources.PSObject.Properties){if((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $entry.Name)).Hash -ne $entry.Value){throw "Road GPU source changed without rebuilding bundle: $($entry.Name)"}}
Write-Output 'Verified generation-only double road GPU bundle and shader source fingerprints.'
