[CmdletBinding()]
param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$environment=Join-Path $repo 'artifacts/material-remap/environments/Lift03'
$baker=Join-Path $environment 'baker'
$output=Join-Path $repo 'artifacts/material-remap/Lift03/native-units'
# The private baker snapshot is an input to this experiment. Never patch shared baker files.
$producer=Join-Path $baker 'tools/GpuBake/PrepareRepeatUnits.Editor.cs'
if(!([IO.File]::ReadAllText($producer).Contains('Lift03 bands require an authored full longitudinal period.'))){throw 'Private V-only band producer must be staged first.'}
$previous=@{}
foreach($name in @('TEMP','TMP','UPM_CACHE_ROOT')){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
 $env:TEMP="$environment/cache/temp";$env:TMP=$env:TEMP;$env:UPM_CACHE_ROOT="$environment/cache/upm"
 & "$baker/tools/Prepare-RepeatUnits.ps1" -Style Lift03 -ProjectPath "$environment/unity" -GameData $GameData -Manifest "$output/manifest.json" -Output "$output/output" -PreparedMaterials "$repo/assets/Materials" -Report "$output/bake.json" -LogPath "$environment/logs/native-units.log" -ProductStore "$environment/outputs/native-units-store"
 Copy-Item -LiteralPath "$PSScriptRoot/NativeUnitCheck.Editor.cs" -Destination "$environment/unity/Assets/Editor/Lift03NativeUnitCheck.cs"
 Copy-Item -LiteralPath "$PSScriptRoot/NativeUnitCheck.compute" -Destination "$environment/unity/Assets/Lift03NativeUnitCheck.compute"
 $arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$environment+'/unity"'),'-executeMethod','Lift03NativeUnitCheck.Run','-unitRoot',('"'+$output+'"'),'-gameData',('"'+$GameData+'"'),'-logFile',('"'+$environment+'/logs/native-units-check.log"'),'-quit')
 $process=Start-Process 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.WaitForExit()
 if($process.ExitCode -ne 0){throw 'Native unit resource validation failed.'}
} finally {foreach($name in $previous.Keys){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}}
# Header/dependency/repeat checks do not approve compression error or original-atlas boundary changes.
