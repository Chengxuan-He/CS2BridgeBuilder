param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameManaged,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$Structures,
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the dedicated Unity GPU preparation project.' }
Copy-Item -LiteralPath "$repo/tools/GpuBake/PreparedNativeCheck.Editor.cs" -Destination "$project/Assets/Editor/PreparedNativeCheck.cs"
$log = [IO.Path]::GetFullPath($LogPath)
$arguments = @('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),
    '-executeMethod','PreparedNativeCheck.Run','-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),
    '-preparedPaint',('"'+$repo+'/assets/Materials/Draw01/Paint"'),'-quit','-logFile',('"'+$log+'"'))
if ($Structures) { $arguments += @('-preparedStructures', ('"'+[IO.Path]::GetFullPath($Structures)+'"')) }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS native prepared readers:' -Quiet)) { throw "Native material read check failed; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS native prepared readers:' | ForEach-Object Line
