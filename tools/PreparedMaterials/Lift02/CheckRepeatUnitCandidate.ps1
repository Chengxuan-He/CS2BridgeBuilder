[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$ProjectPath,
 [Parameter(Mandatory)][string]$Candidate,
 [Parameter(Mandatory)][string]$Manifest,
 [Parameter(Mandatory)][string]$GameData,
 [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
$candidateRoot=[IO.Path]::GetFullPath($Candidate)
if(!(Test-Path -LiteralPath "$project/Assets/Editor/PrepareRepeatUnits.cs")){throw 'Prepare the candidate in the independent Unity project first.'}
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.Editor.cs" -Destination "$project/Assets/Editor/Lift02RepeatUnitCheck.cs"
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.compute" -Destination "$project/Assets/Lift02RepeatUnitCheck.compute"
$log=Join-Path $candidateRoot 'native-check.log'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','Lift02RepeatUnitCheck.Run',
 '-unitRoot',('"'+$candidateRoot+'"'),'-manifest',('"'+[IO.Path]::GetFullPath($Manifest)+'"'),
 '-gameData',('"'+[IO.Path]::GetFullPath($GameData)+'"'),'-logFile',('"'+$log+'"'),'-quit')
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'MEASURED Lift02 repeat candidate:' -Quiet)){throw "Candidate native measurement failed: $log"}
Select-String -LiteralPath $log -Pattern 'MEASURED Lift02 repeat candidate:' | ForEach-Object Line
