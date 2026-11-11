[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$directory=Join-Path $root 'assets/MaterialPrograms'
foreach($manifestFile in Get-ChildItem -LiteralPath $directory -Filter '*.manifest.json') {
 $manifest=Get-Content -LiteralPath $manifestFile.FullName -Raw | ConvertFrom-Json
 if($manifest.schema -ne 1 -or $manifest.program -notmatch '\A[A-Za-z0-9]+\z'){throw "Invalid material program manifest: $($manifestFile.Name)"}
 $package=Join-Path $directory ($manifest.program+'.bbs1')
 if((Get-Item -LiteralPath $package).Length -ne $manifest.packageBytes -or (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $manifest.packageSha256){throw "Material package differs from authored manifest: $($manifest.program)"}
 $producer=if($manifest.PSObject.Properties.Name -contains 'producer'){$manifest.producer}else{'BuildNativeLayered.py'}
 if($producer -notmatch '\ABuildNative[A-Za-z]+\.py\z'){throw 'Invalid material program producer.'}
 if((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $producer) -Algorithm SHA256).Hash -ne $manifest.producerSha256){throw 'Material program producer changed; regenerate the package.'}
 foreach($property in $manifest.sources.PSObject.Properties) {
  $path=[IO.Path]::GetFullPath((Join-Path $root $property.Name))
  if(!$path.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Material source is outside the repository.'}
  if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $property.Value){throw "Material program source changed; regenerate: $($property.Name)"}
 }
}
foreach($package in Get-ChildItem -LiteralPath $directory -Filter '*.bbs1') {
 if(!(Test-Path -LiteralPath (Join-Path $directory ($package.BaseName+'.manifest.json')))){throw "Material program has no authoring manifest: $($package.Name)"}
}
Write-Output 'Verified generated material packages and authoring source fingerprints.'
