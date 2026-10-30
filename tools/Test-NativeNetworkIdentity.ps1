param([Parameter(Mandatory)][string]$ProjectPath,[Parameter(Mandatory)][string]$GameManaged,[Parameter(Mandatory)][string]$Inputs,[Parameter(Mandatory)][string]$Style,[Parameter(Mandatory)][string]$ModAssembly,[Parameter(Mandatory)][string]$LogPath,[switch]$ExpectOldRejection)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
if(!(Test-Path -LiteralPath "$project/Assets/Editor")){throw 'Initialize an independent Unity project first.'}
if(Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}){throw 'Assigned Unity project is busy.'}
Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/NativeNetworkIdentityCheck.Editor.cs" -Destination "$project/Assets/Editor/NativeNetworkIdentityCheck.cs"
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','NativeNetworkIdentityCheck.Run','-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-inputs',('"'+[IO.Path]::GetFullPath($Inputs)+'"'),'-style',$Style,'-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),'-logFile',('"'+[IO.Path]::GetFullPath($LogPath)+'"'))
if($ExpectOldRejection){$arguments+='-expectOldRejection'}
$process=Start-Process 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()
$pattern=if($ExpectOldRejection){'PASS reproduced old native gate rejection:'}else{'PASS URI-bearing native network:'}
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $LogPath -Pattern $pattern -Quiet)){throw "Native identity check failed: $LogPath"}
Select-String -LiteralPath $LogPath -Pattern $pattern | ForEach-Object Line
