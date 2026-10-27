[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameManaged,
    [Parameter(Mandatory)][string]$Inventory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
if(!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)){throw 'Use a dedicated offline Unity project.'}
Copy-Item -LiteralPath "$PSScriptRoot/RepeatNativeCheck.Editor.cs" -Destination "$project/Assets/Editor/RepeatNativeCheck.cs"
$log=[IO.Path]::GetFullPath($LogPath)
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','RepeatNativeCheck.Run',
    '-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-repeatInventory',('"'+[IO.Path]::GetFullPath($Inventory)+'"'),
    '-preparedOutput',('"'+[IO.Path]::GetFullPath($Output)+'"'),'-preparationReport',('"'+[IO.Path]::GetFullPath($Report)+'"'),
    '-quit','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS native repeat readers:' -Quiet)){throw "Native repeat verification failed; see $log"}
Select-String -LiteralPath $log -Pattern 'PASS native repeat readers:' | ForEach-Object Line
