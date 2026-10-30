param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameManaged,
    [Parameter(Mandatory)][string]$Fixtures,
    [Parameter(Mandatory)][string]$Materials,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Initialize an independent Unity project first.' }
if (Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/')) }) { throw 'The assigned Unity project is busy.' }
Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/NativeMidMipCheck.Editor.cs" -Destination "$project/Assets/Editor/NativeMidMipCheck.cs"
$arguments = @('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','NativeMidMipCheck.Run',
    '-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-fixtures',('"'+[IO.Path]::GetFullPath($Fixtures)+'"'),
    '-materials',('"'+[IO.Path]::GetFullPath($Materials)+'"'),'-logFile',('"'+[IO.Path]::GetFullPath($LogPath)+'"'))
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $LogPath -Pattern '^PASS native MidMips reservation/load:' -Quiet)) { throw "Native VT load regression failed; see $LogPath" }
Select-String -LiteralPath $LogPath -Pattern '^PASS native MidMips reservation/load:' | ForEach-Object Line
