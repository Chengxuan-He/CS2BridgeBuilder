param([Parameter(Mandatory)][string]$ModAssembly,[string]$RunName='road-continuous')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$envRoot=Join-Path $repo 'artifacts/material-remap/environments/Lift05'
$project=Join-Path $envRoot 'unity'
$inputs=Join-Path $repo 'artifacts/material-remap/Lift05/native-wrap-inputs'
$log=Join-Path $envRoot "logs/$RunName.log"
$report=Join-Path $repo "artifacts/material-remap/Lift05/$RunName.json"
if (!(Test-Path -LiteralPath "$project/Assets/Editor")) { throw 'Assigned Lift05 environment is missing.' }
if (Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}) { throw 'Assigned Lift05 environment is busy.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RoadContinuousCheck.Editor.cs') -Destination "$project/Assets/Editor/RoadContinuousCheck.cs"
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','Lift05RoadContinuousCheck.Run',
 '-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"',
 '-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),'-inputs',('"'+$inputs+'"'),'-preparedRoot',('"'+(Join-Path $repo 'assets/Materials/Lift05')+'"'),'-report',('"'+$report+'"'),'-quit','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
$retained=Join-Path $repo "artifacts/material-remap/Lift05/$RunName.log"
Copy-Item -LiteralPath $log -Destination $retained
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS Lift05 road native projection:' -Quiet)) { throw "Native Lift05 road check failed: $retained" }
Select-String -LiteralPath $retained -Pattern 'Lift05 road native case|PASS Lift05' | ForEach-Object Line
