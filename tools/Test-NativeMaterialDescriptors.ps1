[CmdletBinding()]
param([Parameter(Mandatory)][string]$ProjectPath,[Parameter(Mandatory)][string]$Materials,[Parameter(Mandatory)][string]$GameManaged,[Parameter(Mandatory)][string]$LogPath,
    [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
if(!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)){throw 'An independent Unity project is required.'}
Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/NativeMaterialDescriptorCheck.Editor.cs" -Destination "$project/Assets/Editor/NativeMaterialDescriptorCheck.cs"
$log=[IO.Path]::GetFullPath($LogPath)
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','NativeMaterialDescriptorCheck.Run',
    '-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-materials',('"'+[IO.Path]::GetFullPath($Materials)+'"'),'-quit','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode-ne 0-or !(Select-String -LiteralPath $log -Pattern 'PASS native source descriptors:' -Quiet)){throw "Native descriptor validation failed: $log"}
Select-String -LiteralPath $log -Pattern 'PASS native source descriptors:'|ForEach-Object Line
