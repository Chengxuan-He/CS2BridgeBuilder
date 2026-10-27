[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$ProjectPath,
 [Parameter(Mandatory)][string]$Candidate,
 [Parameter(Mandatory)][string]$GameData,
 [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
$candidateRoot=[IO.Path]::GetFullPath($Candidate)
if(!(Test-Path -LiteralPath "$project/Assets/Editor/PrepareRepeatUnits.cs")){throw 'First prepare the candidate in the independent Unity project.'}
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.Editor.cs" -Destination "$project/Assets/Editor/Lift03RepeatUnitCheck.cs"
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.compute" -Destination "$project/Assets/Lift03RepeatUnitCheck.compute"
$log=Join-Path $candidateRoot 'native-check.log'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','Lift03RepeatUnitCheck.Run',
 '-unitRoot',('"'+$candidateRoot+'"'),'-gameData',('"'+[IO.Path]::GetFullPath($GameData)+'"'),'-logFile',('"'+$log+'"'),'-quit')
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "Candidate native check failed: $log"}
$passes=@(Select-String -LiteralPath $log -Pattern 'PASS Lift03 repeat candidate')
if($passes.Count -ne 2){throw "Expected both measured Lift03 units: $log"}
$passes | ForEach-Object Line
