param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$FixturesPath,
    [Parameter(Mandatory=$true)][string]$LogPath,
    [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project=[IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the existing Unity GPU test project.' }
Copy-Item -LiteralPath "$repo/tools/GpuBake/VtSourceCheck.Editor.cs" -Destination "$project/Assets/Editor/VtSourceCheck.cs"
foreach($name in @('MovableAtlasChannel','MovableVtTextureSource')) {
    $code=[IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
    $code=$code.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+"`n}"
    $code=$code.Replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
    [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs",$code)
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/BridgeBake.compute" -Destination "$project/Assets/PackingProbe.compute"
$log=[IO.Path]::GetFullPath($LogPath)
$fixtures=[IO.Path]::GetFullPath($FixturesPath)
$process=Start-Process -FilePath $Unity -ArgumentList @('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','VtSourceCheck.Run','-vtFixtures',('"'+$fixtures+'"'),'-quit','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'Shader error' -Quiet)) { throw "VT source GPU test failed; see $log" }
foreach($message in @('PASS native VT tile layout:','PASS native VT full-resolution source:')) {
    if(!(Select-String -LiteralPath $log -Pattern $message -Quiet)) { throw "GPU test did not report completion; see $log" }
    Select-String -LiteralPath $log -Pattern $message | ForEach-Object Line
}
