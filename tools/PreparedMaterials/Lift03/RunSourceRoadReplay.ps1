param([Parameter(Mandatory=$true)][string]$CandidateDll)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$privateEnvironment=Join-Path $repo 'artifacts/material-remap/environments/Lift03'
Copy-Item -LiteralPath "$PSScriptRoot/SourceRoadReplay.Editor.cs" -Destination "$privateEnvironment/unity/Assets/Editor/Lift03SourceRoadReplay.cs"
$previous=@{}
foreach($name in @('TEMP','TMP','UPM_CACHE_ROOT')){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
 $env:TEMP="$privateEnvironment/cache/temp";$env:TMP=$env:TEMP;$env:UPM_CACHE_ROOT="$privateEnvironment/cache/upm"
 $arguments=@('-batchmode','-nographics','-projectPath',('"'+$privateEnvironment+'/unity"'),'-executeMethod','Lift03SourceRoadReplay.Run','-candidateDll',('"'+[IO.Path]::GetFullPath($CandidateDll)+'"'),'-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"','-fixture',('"'+$repo+'/artifacts/material-remap/Lift03/source-road-replay-fixture.json"'),'-report',('"'+$repo+'/artifacts/material-remap/Lift03/source-road-candidate-replay.json"'),'-logFile',('"'+$privateEnvironment+'/logs/source-road-replay.log"'),'-quit')
 $process=Start-Process 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.WaitForExit()
 if($process.ExitCode -ne 0){throw "Lift03 source-road replay failed ($($process.ExitCode)); inspect private log."}
} finally {foreach($name in $previous.Keys){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}}
