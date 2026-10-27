param([string]$Suffix = '')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'C:/Users/admin/Documents/Codex/CS2BridgeBuilder'
$destinationRoot=Join-Path $PWD "artifacts/shared-nontiling-package$Suffix"
if(Test-Path -LiteralPath $destinationRoot){throw 'Package destination already exists'}
$content=Join-Path $destinationRoot 'content'
New-Item -ItemType Directory -Path $content | Out-Null
$output=Join-Path $PWD 'src/BridgeBuilder/bin/Release'
$files=@('BridgeBuilder.dll','BridgeBuilder.pdb','0Harmony.dll','Harmony-LICENSE.txt','BridgeBuilder.mjs','BridgeBuilder.css','BridgeBuilder.svg','BridgeBuilderToolbar.svg','BridgeBuilderPack.svg','BridgeBuilderSearch.svg','BridgeBuilderFilter.svg','BridgeBuilderArrowDown.svg')
foreach($file in $files){Copy-Item -LiteralPath (Join-Path $output $file) -Destination $content}
./tools/Copy-PreparedMaterials.ps1 -Source (Join-Path $output 'Materials') -DestinationRoot $content
Copy-Item -LiteralPath LICENSE -Destination (Join-Path $content 'LICENSE.txt')
$pp=[Environment]::GetEnvironmentVariable('CSII_MODPOSTPROCESSORPATH','User')
$ppArgs=@('PostProcess',(Join-Path $content 'BridgeBuilder.dll'),'-u',[Environment]::GetEnvironmentVariable('CSII_UNITYMODPROJECTPATH','User'))
foreach($reference in Get-ChildItem -LiteralPath ([Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH','User')) -Filter '*.dll'){$ppArgs+=@('-r',$reference.FullName)}
& $pp @ppArgs *> (Join-Path $destinationRoot 'PostProcess.log')
if($LASTEXITCODE -ne 0){throw 'Official postprocessing failed'}
if(!(Select-String -LiteralPath (Join-Path $destinationRoot 'PostProcess.log') -SimpleMatch 'BridgeBuilder.dll processing complete' -Quiet)){throw 'Official postprocessing completion missing'}
dotnet build tools/ParadoxPackage *> (Join-Path $destinationRoot 'package-build.log')
if($LASTEXITCODE -ne 0){throw 'Package tool build failed'}
dotnet tools/ParadoxPackage/bin/Debug/net6.0/ParadoxPackage.dll (Split-Path ([Environment]::GetEnvironmentVariable('CSII_MODPUBLISHERPATH','User'))) $content (Join-Path $destinationRoot 'content.zip') > (Join-Path $destinationRoot 'measurement.json')
if($LASTEXITCODE -ne 0){throw 'Official SDK packaging failed'}
$measurement=Get-Content -LiteralPath (Join-Path $destinationRoot 'measurement.json') -Raw|ConvertFrom-Json
if($measurement.bytes -gt 50000000){throw "Package exceeds limit: $($measurement.bytes)"}
$measurement|Select-Object bytes,sha256,files|Format-List
