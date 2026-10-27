$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) ('BridgeBuilder-material-package-' + [Guid]::NewGuid().ToString('N'))
$source = Join-Path $work 'source'
$output = Join-Path $work 'output'
$copy = Join-Path $PSScriptRoot 'Copy-PreparedMaterials.ps1'
[IO.Directory]::CreateDirectory((Join-Path $source 'Draw01/variant')) | Out-Null
[IO.Directory]::CreateDirectory((Join-Path $source 'Lift01')) | Out-Null
[IO.Directory]::CreateDirectory($output) | Out-Null
[IO.File]::WriteAllText((Join-Path $source 'Lift01/.gitkeep'), '')
[IO.File]::WriteAllText((Join-Path $source 'Draw01/README.md'), 'Authoring documentation')
[IO.File]::WriteAllText((Join-Path $output 'unrelated.txt'), 'Preserve unrelated output')
& $copy -Source $source -DestinationRoot $output
if (Test-Path -LiteralPath (Join-Path $output 'Materials')) { throw 'Empty material root was packaged.' }
# Serialization is outside this packaging check; these bytes test lossless transport only.
$payload = Join-Path $source 'Draw01/variant/unit.Texture'
[IO.File]::WriteAllBytes($payload, [byte[]](0..255))
$rejected = $false
try { & $copy -Source $source -DestinationRoot $output } catch { $rejected = $true }
if (-not $rejected) { throw 'A native payload without a CID was accepted.' }
[IO.File]::WriteAllText(($payload + '.cid'), '1234567890abcdef1234567890abcdef')
& $copy -Source $source -DestinationRoot $output
$packed = Join-Path $output 'Materials/Draw01/variant/unit.Texture'
if ((Get-FileHash -LiteralPath $payload).Hash -ne (Get-FileHash -LiteralPath $packed).Hash) { throw 'Payload changed.' }
if (@(Get-ChildItem -LiteralPath (Join-Path $output 'Materials') -Recurse -File).Count -ne 2) { throw 'Unexpected packaged files.' }
if (Test-Path -LiteralPath (Join-Path $output 'Materials/Lift01')) { throw 'Placeholder-only style was packaged.' }
[IO.File]::WriteAllText(($payload + '.cid'), 'invalid')
$rejected = $false
try { & $copy -Source $source -DestinationRoot $output } catch { $rejected = $true }
if (-not $rejected -or -not (Test-Path -LiteralPath $packed)) { throw 'Invalid input replaced healthy output.' }
$emptySource = Join-Path $work 'absent-source'
& $copy -Source $emptySource -DestinationRoot $output
if (Test-Path -LiteralPath (Join-Path $output 'Materials')) { throw 'Stale material root survived an empty package.' }
if ([IO.File]::ReadAllText((Join-Path $output 'unrelated.txt')) -ne 'Preserve unrelated output') { throw 'Unrelated output changed.' }
$rejected = $false
[IO.File]::WriteAllText(($payload + '.cid'), '1234567890abcdef1234567890abcdef')
try { & $copy -Source $source -DestinationRoot $source } catch { $rejected = $true }
if (-not $rejected) { throw 'Overlapping source and destination were accepted.' }
Write-Host 'PASS prepared-material packaging: native bytes/CIDs, nested style paths, filtering, empty roots, stale output, validation before replacement, unrelated output preserved.'
