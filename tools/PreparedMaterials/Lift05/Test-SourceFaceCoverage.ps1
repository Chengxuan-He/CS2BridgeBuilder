param([Parameter(Mandatory)][string]$ModAssembly,[string]$RunName='native-face-coverage')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $root 'artifacts/material-remap/environments/Lift05/unity'
$output=Join-Path $root 'artifacts/material-remap/Lift05/source-face-coverage'
if(!(Test-Path "$project/Assets/Editor")){throw 'Assigned Lift05 environment is missing.'}
if(Get-CimInstance Win32_Process -Filter "name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}){throw 'Lift05 environment is busy.'}
foreach($name in @('Lift05FaceDispositionCheck','Lift05FullNativeCheck','Lift05RetileSlots')){Copy-Item "$PSScriptRoot/$name.Editor.cs" "$project/Assets/Editor/$name.cs"}
$args=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','Lift05FaceDispositionCheck.Run','-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"','-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),'-inputs',('"'+$output+'"'),'-finishedSurface',('"'+$root+'/assets/Materials/Lift05/RepeatUnits/85057d5f0c21ff93c646cdc60fa02affe454e15a39b7cca0c899aa1aa861ffe1/Repeat.Surface"'),'-quit','-logFile',('"'+$output+'/'+$RunName+'.log"'))
$p=Start-Process 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
$p.WaitForExit()
if($p.ExitCode -ne 0){throw 'Native coverage fixture failed.'}
if(@(Select-String "$output/$RunName.log" -Pattern 'PASS Lift05 full TryBuild/upload').Count -ne 15){throw 'Incomplete native coverage cases.'}
Get-FileHash -LiteralPath $ModAssembly
