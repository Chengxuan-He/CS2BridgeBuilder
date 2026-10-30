param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$root=Join-Path $repo 'artifacts/material-resume/Lift05'
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.Editor.cs" -Destination "$root/env/unity/Assets/Editor/Lift05RepeatUnitCheck.cs"
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.compute" -Destination "$root/env/unity/Assets/Lift05RepeatUnitCheck.compute"
$previous=@{}
foreach($name in @('TEMP','TMP','UPM_CACHE_ROOT')){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
 $env:TEMP="$root/env/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$root/env/cache/upm"
 $arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/env/unity"'),'-executeMethod','Lift05RepeatUnitCheck.Run','-gameData',('"'+$GameData+'"'),'-unitRoot',('"'+$root+'/profile-unit"'),'-quit','-logFile',('"'+$root+'/env/logs/check.log"'))
 $process=Start-Process 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.WaitForExit()
 if($process.ExitCode -ne 0 -or !(Select-String "$root/env/logs/check.log" -Pattern '^PASS Lift05 repeat candidate' -Quiet)){throw 'Native profile check failed; inspect isolated check.log'}
 Select-String "$root/env/logs/check.log" -Pattern '^PASS Lift05 repeat candidate' | ForEach-Object Line
} finally {
 foreach($name in $previous.Keys){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
