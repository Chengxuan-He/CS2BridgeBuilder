param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$LogPath,
    [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project=[IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the existing Unity GPU test project.' }
Copy-Item -LiteralPath "$repo/tools/GpuBake/PackingCheck.Editor.cs" -Destination "$project/Assets/Editor/PackingCheck.cs"
foreach($source in @('src/BridgeBuilder/Bridges/Common/Movable/MovableUvChart.cs','src/BridgeBuilder/Bridges/Common/Movable/MovableStructurePacking.cs','src/BridgeBuilder/Bridges/Common/Movable/MovableTextureFootprint.cs')) {
    $code=[IO.File]::ReadAllText((Join-Path $repo $source))
    $code=$code.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+"`n}"
    [IO.File]::WriteAllText((Join-Path "$project/Assets/Editor" ([IO.Path]::GetFileName($source))),$code)
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/BridgeBake.compute" -Destination "$project/Assets/PackingProbe.compute"
$log=[IO.Path]::GetFullPath($LogPath)
$process=Start-Process -FilePath $Unity -ArgumentList @('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PackingCheck.Run','-quit','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'Shader error' -Quiet)) { throw "Structural packing GPU test failed; see $log" }
if(!(Select-String -LiteralPath $log -Pattern 'PASS structural atlas GPU crop:' -Quiet)) { throw "GPU test did not report completion; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS structural atlas GPU crop:' | ForEach-Object Line
