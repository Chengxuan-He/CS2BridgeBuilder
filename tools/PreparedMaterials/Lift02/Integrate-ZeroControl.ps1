[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$style = Join-Path $repo 'assets/Materials/Lift02'
$candidate = Join-Path $repo 'artifacts/prepared-materials/Lift02/repeat-zero-control-candidate'
$audit = Get-Content -LiteralPath "$candidate/audit.json" -Raw | ConvertFrom-Json
if ($audit.status -cne 'PASS' -or @($audit.changes).Count -ne 1 -or @($audit.sources).Count -ne 1 -or $audit.sources[0].authoredControl) { throw 'Lift02 source proof differs.' }
$change = $audit.changes[0]
$oldControl = @($change.before | Where-Object property -ceq '_ControlMask')[0]
$newControl = @($change.after | Where-Object property -ceq '_ControlMask')[0]
$zero = Join-Path $style 'Road/ZeroControl.Texture'
$surface = Join-Path $style "Repeat/$($change.key)/Repeat.Surface"
$replacement = Join-Path $candidate "Repeat/$($change.key)/Repeat.Surface"
$oldTexture = Join-Path $style "Shared/$($oldControl.cid).Texture"
$newTexture = Join-Path $style "Shared/$($newControl.cid).Texture"
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Same([byte[]]$A, [byte[]]$B) {
    if ($A.Length -ne $B.Length) { return $false }
    for ($i=0; $i -lt $A.Length; $i++) { if ($A[$i] -ne $B[$i]) { return $false } }
    return $true
}
function HashBytes([string]$Cid) {
    if ($Cid -cnotmatch '^[0-9a-f]{32}$') { throw 'Invalid CID.' }
    [byte[]]$bytes = 0..15 | ForEach-Object { [Convert]::ToByte(([string]$Cid[2*$_+1]+$Cid[2*$_]),16) }
    return ,$bytes
}
function References([string]$Cid) {
    $needle = HashBytes $Cid
    @(Get-ChildItem -LiteralPath $style -Filter '*.Surface' -Recurse -File | Where-Object {
        $bytes = [IO.File]::ReadAllBytes($_.FullName)
        $found = $false
        for ($at=0; $at -le $bytes.Length-16 -and !$found; $at++) {
            if ($bytes[$at] -ne $needle[0]) { continue }
            $match = $true
            for ($j=1; $j -lt 16; $j++) { if ($bytes[$at+$j] -ne $needle[$j]) { $match=$false; break } }
            if ($match) { $found=$true }
        }
        $found
    } | ForEach-Object FullName)
}
$before = @{}
Get-ChildItem -LiteralPath $style -Recurse -File | ForEach-Object { $before[$_.FullName] = Hash $_.FullName }
if ((Hash $surface) -cne $change.oldSha256 -or (Hash $replacement) -cne $change.newSha256 -or
    [IO.File]::ReadAllText("$surface.cid").Trim() -cne $change.oldSurfaceCid -or
    [IO.File]::ReadAllText("$replacement.cid").Trim() -cne $change.newSurfaceCid) { throw 'Surface evidence changed.' }
if ((Hash $oldTexture) -cne $oldControl.sha256 -or (Hash $zero) -cne $newControl.sha256 -or
    [IO.File]::ReadAllText("$oldTexture.cid").Trim() -cne $oldControl.cid -or
    [IO.File]::ReadAllText("$zero.cid").Trim() -cne $newControl.cid) { throw 'Control evidence changed.' }
foreach ($proof in $audit.textureProofs) {
    if (@($proof.mips).Count -eq 0 -or @($proof.mips | Where-Object maximumAbsoluteValue -ne 0).Count) { throw 'Control mip proof is not zero.' }
}
$old = [IO.File]::ReadAllBytes($surface); $next = [IO.File]::ReadAllBytes($replacement)
$expected = [byte[]]$old.Clone(); [Array]::Copy((HashBytes $newControl.cid),0,$expected,[int]$change.offset,16)
if (!(Same $expected $next)) { throw 'Candidate changes more than the control CID.' }
$surfaceHash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($audit.recipe+'/'+(Hash $replacement)))
$surfaceCid = [Convert]::ToHexString($surfaceHash).ToLowerInvariant().Substring(0,32)
if ($surfaceCid -cne $change.newSurfaceCid) { throw 'Candidate CID derivation differs.' }
foreach ($binding in $change.before | Where-Object property -cne '_ControlMask') {
    $afterBinding = @($change.after | Where-Object property -ceq $binding.property)[0]
    if ($binding.cid -cne $afterBinding.cid -or $binding.sha256 -cne $afterBinding.sha256 -or (Hash $binding.path) -cne $binding.sha256) { throw 'Other channel changed.' }
}
if ((Test-Path -LiteralPath $newTexture) -and (!(Same ([IO.File]::ReadAllBytes($zero)) ([IO.File]::ReadAllBytes($newTexture))) -or
    !(Same ([IO.File]::ReadAllBytes("$zero.cid")) ([IO.File]::ReadAllBytes("$newTexture.cid"))))) { throw 'Conflicting shared zero.' }
$referencesBefore = @(References $oldControl.cid)
$backup = Join-Path $repo ('artifacts/backups/Lift02/repeat-zero-control-'+[Guid]::NewGuid().ToString('N'))
$originals = @($surface,"$surface.cid",$oldTexture,"$oldTexture.cid")
foreach ($path in $originals) {
    $relative = [IO.Path]::GetRelativePath($style,$path)
    $destination = Join-Path $backup $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $path -Destination $destination
    if ((Hash $path) -cne (Hash $destination)) { throw 'Backup hash differs.' }
}
Copy-Item -LiteralPath $zero -Destination $newTexture
Copy-Item -LiteralPath "$zero.cid" -Destination "$newTexture.cid"
Copy-Item -LiteralPath $replacement -Destination $surface
Copy-Item -LiteralPath "$replacement.cid" -Destination "$surface.cid"
if ((Hash $surface) -cne $change.newSha256 -or (Hash $newTexture) -cne $newControl.sha256) { throw 'Written payload differs.' }
$referencesAfter = @(References $oldControl.cid)
$removed = @()
if ($referencesAfter.Count -eq 0) {
    foreach ($path in @($oldTexture,"$oldTexture.cid")) {
        $resolved = (Resolve-Path -LiteralPath $path).Path
        if (!$resolved.StartsWith((Resolve-Path -LiteralPath $style).Path+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Deletion outside Lift02.' }
        Remove-Item -LiteralPath $resolved
        if (Test-Path -LiteralPath $resolved) { throw 'Old control deletion failed.' }
        $removed += $resolved
    }
}
$after = @{}
Get-ChildItem -LiteralPath $style -Recurse -File | ForEach-Object { $after[$_.FullName] = Hash $_.FullName }
$changes = @($before.Keys | Where-Object { !$after.ContainsKey($_) -or $before[$_] -cne $after[$_] })
$allowed = @($surface,"$surface.cid") + $removed
if (@($changes | Where-Object { $_ -cnotin $allowed }).Count) { throw 'Unexpected Lift02 mutation.' }
$added = @($after.Keys | Where-Object { !$before.ContainsKey($_) })
if (@($added | Where-Object { $_ -cnotin @($newTexture,"$newTexture.cid") }).Count) { throw 'Unexpected Lift02 additions.' }
foreach ($binding in $change.after) {
    $path = Join-Path $style "Shared/$($binding.cid).Texture"
    if ((Hash $path) -cne $binding.sha256 -or [IO.File]::ReadAllText("$path.cid").Trim() -cne $binding.cid) { throw 'Incomplete local repeat dependency.' }
}
$report = [ordered]@{ status='PASS'; style='Lift02'; backup=$backup; backupFiles=$originals.Count; oldSurfaceCid=$change.oldSurfaceCid; newSurfaceCid=$change.newSurfaceCid; onlyControl16BytesChanged=$true; oldControlReferencesBefore=$referencesBefore; oldControlReferencesAfter=$referencesAfter; surfaceFilesScanned=@(Get-ChildItem -LiteralPath $style -Filter '*.Surface' -Recurse -File).Count; changedFiles=$changes; addedFiles=$added; removedFiles=$removed; allRepeatChannelsLocal=$true; otherFilesUnchanged=$true; noBaking=$true; noUserVisibleTextChanges=$true; hashes=$after }
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $repo 'artifacts/prepared-materials/Lift02/zero-control-integration.json') -Encoding utf8
'PASS Lift02 transparent control integration; backup verified; only control CID changed; local dependencies complete.'
