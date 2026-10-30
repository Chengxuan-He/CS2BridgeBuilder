param([Parameter(Mandatory)][string]$ModAssembly, [string]$RunName='structural-wrap')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$envRoot=Join-Path $repo 'artifacts/material-remap/environments/Lift05'
$project=Join-Path $envRoot 'unity'
$inputRoot=Join-Path $repo 'artifacts/material-remap/Lift05/native-wrap-inputs'
$log=Join-Path $envRoot "logs/$RunName.log"
if (!(Test-Path -LiteralPath "$project/Assets/Editor")) { throw 'Assigned isolated Lift05 Unity environment is missing.' }
if (Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}) { throw 'The assigned Lift05 Unity environment is busy.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StructuralWrapCheck.Editor.cs') -Destination "$project/Assets/Editor/StructuralWrapCheck.cs"
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','StructuralWrapCheck.Run',
 '-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"',
 '-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),'-inputs',('"'+$inputRoot+'"'),'-quit','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
$retained=Join-Path $repo "artifacts/material-remap/Lift05/$RunName.log"
Copy-Item -LiteralPath $log -Destination $retained
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS Lift05 native structural wrap:' -Quiet)) { throw "Native Lift05 wrap check failed: $retained" }
Select-String -LiteralPath $retained -Pattern 'SHA256=|PASS Lift05' | ForEach-Object Line
