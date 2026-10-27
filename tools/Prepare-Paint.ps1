[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameData,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$Output = (Join-Path $PSScriptRoot '../assets/Materials'),
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the dedicated Unity GPU preparation project.' }
$settings = Join-Path $project 'ProjectSettings/ProjectSettings.asset'
$settingsText = [IO.File]::ReadAllText($settings)
if ($settingsText -notmatch 'm_ActiveColorSpace: [01]') { throw 'Unity color-space setting is missing.' }
[IO.File]::WriteAllText($settings, ($settingsText -replace 'm_ActiveColorSpace: [01]', 'm_ActiveColorSpace: 1'))
Copy-Item -LiteralPath "$repo/tools/GpuBake/PreparePaint.Editor.cs" -Destination "$project/Assets/Editor/PreparePaint.cs"
$code = [IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/MovableVtTextureSource.cs")
$code = $code.Replace('namespace BridgeBuilder.Bridges;', 'namespace BridgeBuilder.Bridges {') + "`n}"
$code = $code.Replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
[IO.File]::WriteAllText("$project/Assets/Editor/MovableVtTextureSource.cs", $code)
$log = [IO.Path]::GetFullPath($LogPath)
$arguments = @('-batchmode', '-force-d3d11', '-projectPath', ('"'+$project+'"'), '-executeMethod', 'PreparePaint.Run',
    '-gameData', ('"'+[IO.Path]::GetFullPath($GameData)+'"'), '-preparedOutput', ('"'+[IO.Path]::GetFullPath($Output)+'"'), '-quit', '-logFile', ('"'+$log+'"'))
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS prepared paint:' -Quiet)) { throw "Paint preparation failed; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS prepared paint:' | ForEach-Object Line
