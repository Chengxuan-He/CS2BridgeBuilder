$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$style=[IO.Path]::GetFullPath((Join-Path $repo 'artifacts/agent-material-env/Draw01/fresh-native-materials/output/Draw01'))
$sources=@('Cable/ZeroControl.Texture','Road/ZeroControl.Texture') | ForEach-Object { [IO.Path]::GetFullPath((Join-Path $style $_)) }
foreach($path in $sources){if(!$path.StartsWith($style+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output path escaped Draw01.'}}
$cid=[IO.File]::ReadAllText($sources[0]+'.cid').Trim()
if($cid -ne '729893d593df5d52ba911bc1e7eab77b'){throw 'Unexpected fresh control identity.'}
$hash=(Get-FileHash -LiteralPath $sources[0] -Algorithm SHA256).Hash
foreach($path in $sources){
    if([IO.File]::ReadAllText($path+'.cid').Trim() -ne $cid -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $hash){throw 'Control resources are not byte-identical.'}
}
$target=Join-Path $style ('Shared/'+$cid+'.Texture')
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
if(Test-Path -LiteralPath $target){throw 'Fresh control has already been compacted; verify before rerunning.'}
Copy-Item -LiteralPath $sources[0] -Destination $target
Copy-Item -LiteralPath ($sources[0]+'.cid') -Destination ($target+'.cid')
if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash){throw 'Shared control copy differs.'}
foreach($path in $sources){Remove-Item -LiteralPath $path; Remove-Item -LiteralPath ($path+'.cid')}
Write-Output ('PASS fresh Draw01 control: one physical payload at '+$target)
