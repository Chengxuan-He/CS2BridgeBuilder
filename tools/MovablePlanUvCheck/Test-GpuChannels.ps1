param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$LogPath,
    [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project=[IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the existing Unity GPU test project.' }
Copy-Item -LiteralPath "$repo/tools/GpuBake/AtlasChannelCheck.Editor.cs" -Destination "$project/Assets/Editor/AtlasChannelCheck.cs"
foreach($name in @('MovableAtlasChannel','MovableTextureSource')) {
    $code=[IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
    $code=$code.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+"`n}"
    # Editor-only cleanup; sampling, mip generation and readback stay unchanged.
    $code=$code.Replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
    [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs",$code)
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/BridgeBake.compute" -Destination "$project/Assets/PackingProbe.compute"
$log=[IO.Path]::GetFullPath($LogPath)
$process=Start-Process -FilePath $Unity -ArgumentList @('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','AtlasChannelCheck.Run','-quit','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'Shader error' -Quiet)) { throw "Atlas channel GPU test failed; see $log" }
if(!(Select-String -LiteralPath $log -Pattern 'PASS native atlas channel:' -Quiet)) { throw "GPU test did not report completion; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS native atlas channel:' | ForEach-Object Line
