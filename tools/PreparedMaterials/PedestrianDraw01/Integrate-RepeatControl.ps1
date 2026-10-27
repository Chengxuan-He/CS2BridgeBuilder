param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder')
$ErrorActionPreference='Stop'
$style=[IO.Path]::GetFullPath((Join-Path $Repo 'assets/Materials/PedestrianDraw01'))
$candidates=Join-Path $Repo 'artifacts/prepared-materials/PedestrianDraw01/repeat-control-candidates'
$reportPath=Join-Path $Repo 'artifacts/prepared-materials/PedestrianDraw01/repeat-control-integration.json'
$backup=Join-Path $Repo ('artifacts/backups/PedestrianDraw01-repeat-control-'+[Guid]::NewGuid().ToString('N'))
$audit=Get-Content -LiteralPath "$candidates/audit.json" -Raw | ConvertFrom-Json
if($audit.status -ne 'PASS' -or $audit.changes.Count -ne 2 -or $audit.sources.Count -ne 1 -or $audit.sources[0].authoredControl){throw 'Source audit is not eligible.'}
$native=Get-Content -LiteralPath (Join-Path $Repo 'artifacts/prepared-materials/PedestrianDraw01/constant-control/native-1x1/probe-report.json') -Raw | ConvertFrom-Json
if($native.status -ne 'PASS' -or $native.maximumEquivalentError -ne 0 -or $native.opaqueReplacementEquivalent){throw 'Native constant equivalence not established.'}
$oldCid='e998b666087a605187c9285ab1816a23'
$newCid='729893d593df5d52ba911bc1e7eab77b'
$oldTexture=Join-Path $style "Shared/$oldCid.Texture"
$zero=Join-Path $style 'Road/ZeroControl.Texture'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Same([byte[]]$a,[byte[]]$b){[Convert]::ToBase64String($a) -eq [Convert]::ToBase64String($b)}
function HashBytes([string]$cid){[byte[]]@(for($i=0;$i -lt 32;$i+=2){[Convert]::ToByte("$($cid[$i+1])$($cid[$i])",16)})}
function References([string]$cid){$needle=[Convert]::ToHexString((HashBytes $cid)); @(Get-ChildItem -LiteralPath $style -Filter '*.Surface' -Recurse -File | Where-Object { [Convert]::ToHexString([IO.File]::ReadAllBytes($_.FullName)).Contains($needle) } | ForEach-Object {$_.FullName.Substring($style.Length+1).Replace('\','/')})}
if((Hash $oldTexture) -ne $native.reference.sha256 -or (Hash $zero) -ne $native.transparent.sha256){throw 'Verified textures changed.'}
if((Get-Content "$oldTexture.cid" -Raw).Trim() -ne $oldCid -or (Get-Content "$zero.cid" -Raw).Trim() -ne $newCid){throw 'Texture CID mismatch.'}
$beforeRefs=References $oldCid
$surfaceBefore=@(Get-ChildItem -LiteralPath $style -Filter '*.Surface' -Recurse -File | ForEach-Object{[pscustomobject]@{path=$_.FullName.Substring($style.Length+1).Replace('\','/');sha256=Hash $_.FullName}})
$changedPaths=@()
foreach($change in $audit.changes){
 $relative="Repeat/$($change.key)/Repeat.Surface"; $source=Join-Path $style $relative; $candidate=Join-Path $candidates $relative
 if((Hash $source) -ne $change.oldSha256 -or (Hash $candidate) -ne $change.newSha256){throw "Surface hash changed: $relative"}
 if((Get-Content "$source.cid" -Raw).Trim() -ne $change.oldSurfaceCid -or (Get-Content "$candidate.cid" -Raw).Trim() -ne $change.newSurfaceCid){throw 'Surface CID differs.'}
 $a=[IO.File]::ReadAllBytes($source);$b=[IO.File]::ReadAllBytes($candidate);$offset=[int]$change.offset
 if($a.Length -ne $b.Length -or $offset -ne 141){throw 'Descriptor dimensions changed.'}
 for($i=0;$i -lt $a.Length;$i++){if(($i -lt $offset -or $i -ge $offset+16) -and $a[$i] -ne $b[$i]){throw 'Non-control descriptor bytes changed.'}}
 if(!(Same $a[$offset..($offset+15)] (HashBytes $oldCid)) -or !(Same $b[$offset..($offset+15)] (HashBytes $newCid))){throw 'Control reference differs.'}
 foreach($channel in $change.before | Where-Object property -ne '_ControlMask'){
   if((Hash $channel.path) -ne $channel.sha256){throw "Unchanged channel changed: $($channel.property)"}
 }
 $changedPaths += $relative
}
# Only original payloads go into this backup directory; reports stay outside it.
$originals=@($changedPaths | ForEach-Object{$_;"$_.cid"})+@("Shared/$oldCid.Texture","Shared/$oldCid.Texture.cid")
foreach($relative in $originals){$source=Join-Path $style $relative;$dest=Join-Path $backup $relative;[IO.Directory]::CreateDirectory((Split-Path $dest))|Out-Null;Copy-Item -LiteralPath $source -Destination $dest;if((Hash $source) -ne (Hash $dest)){throw 'Backup checksum differs.'}}
foreach($suffix in @('', '.cid')){$source="$zero$suffix";$dest=Join-Path $style "Shared/$newCid.Texture$suffix";if(Test-Path -LiteralPath $dest){if((Hash $source) -ne (Hash $dest)){throw 'Conflicting Shared zero.'}}else{Copy-Item -LiteralPath $source -Destination $dest};if((Hash $source) -ne (Hash $dest)){throw 'Shared zero copy differs.'}}
foreach($relative in $changedPaths){foreach($suffix in @('', '.cid')){$source=Join-Path $candidates "$relative$suffix";$dest=Join-Path $style "$relative$suffix";Copy-Item -LiteralPath $source -Destination $dest;if((Hash $source) -ne (Hash $dest)){throw 'Candidate copy differs.'}}}
$afterRefs=References $oldCid
$deleted=@()
if($afterRefs.Count -eq 0){
 foreach($path in @($oldTexture,"$oldTexture.cid")){
  $resolved=[IO.Path]::GetFullPath($path);$allowed=[IO.Path]::GetFullPath((Join-Path $style 'Shared'))+[IO.Path]::DirectorySeparatorChar
  if(!$resolved.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Deletion target escaped style Shared.'}
  Remove-Item -LiteralPath $resolved
  if(Test-Path -LiteralPath $resolved){throw 'Unreferenced original still exists.'};$deleted+=$resolved
 }
}
foreach($entry in $surfaceBefore){if($entry.path -notin $changedPaths -and (Hash (Join-Path $style $entry.path)) -ne $entry.sha256){throw 'Unrelated Surface changed.'}}
$report=[ordered]@{status='PASS';style='PedestrianDraw01';backupOriginalsDirectory=$backup;backupFiles=$originals;candidateChanges=$audit.changes;surfaceCount=$surfaceBefore.Count;oldReferencesBefore=$beforeRefs;oldReferencesAfter=$afterRefs;deleted=$deleted;newSharedTexture="Shared/$newCid.Texture";newSharedSha256=Hash (Join-Path $style "Shared/$newCid.Texture");newSharedCid=$newCid;newReferences=References $newCid;allOtherSurfaceHashesUnchanged=$true;textureBakes=0;textureEncodes=0;localization='No user-visible strings or keys changed.'}
$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $reportPath -Encoding UTF8
Write-Output "PASS integrated 2 Repeat descriptors; scanned $($surfaceBefore.Count) Surfaces; old references after=$($afterRefs.Count); deleted=$($deleted.Count); backup=$backup"
