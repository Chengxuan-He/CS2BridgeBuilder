[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$Style,
 [Parameter(Mandatory)][string]$ProjectPath,
 [Parameter(Mandatory)][string]$GameData,
 [Parameter(Mandatory)][string]$Manifest,
 [Parameter(Mandatory)][string]$Output,
 [Parameter(Mandatory)][string]$PreparedMaterials,
 [Parameter(Mandatory)][string]$Report,
 [Parameter(Mandatory)][string]$LogPath,
 [string]$ProductStore,
 [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project=[IO.Path]::GetFullPath($ProjectPath)
if(!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)){throw 'Use an independent Unity project.'}
$settings="$project/ProjectSettings/ProjectSettings.asset"
$content=[IO.File]::ReadAllText($settings)
if($content -notmatch 'm_ActiveColorSpace: [01]'){throw 'Missing color space setting.'}
[IO.File]::WriteAllText($settings,($content -replace 'm_ActiveColorSpace: [01]','m_ActiveColorSpace: 1'))
foreach($name in @('PreparePaint','PrepareRepeatUnits','NativeTextureEncoding')){
 Copy-Item -LiteralPath "$repo/tools/GpuBake/$name.Editor.cs" -Destination "$project/Assets/Editor/$name.cs"
}
foreach($name in @('MovableTextureSource','MovableVtTextureSource')){
 $code=[IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
 $code=$code.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+"`n}"
 $code=$code.Replace('UnityEngine.Object.Destroy(','UnityEngine.Object.DestroyImmediate(')
 [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs",$code)
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/RepeatUnit.compute" -Destination "$project/Assets/RepeatUnit.compute"
foreach($path in @($Report,$LogPath)){[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($path)))|Out-Null}
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PrepareRepeatUnits.Run','-style',$Style)
if(!$ProductStore){$ProductStore=[IO.Path]::GetFullPath($Output)+'.products'}
$pipeline=@("$repo/tools/GpuBake/PrepareRepeatUnits.Editor.cs","$repo/tools/GpuBake/NativeTextureEncoding.Editor.cs","$repo/tools/GpuBake/RepeatUnit.compute","$repo/tools/GpuBake/PreparePaint.Editor.cs","$repo/src/BridgeBuilder/Bridges/Common/Movable/MovableTextureSource.cs") | ForEach-Object {(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}
$arguments+=@('-unitStore',('"'+[IO.Path]::GetFullPath($ProductStore)+'"'),'-unitPipeline',($pipeline -join ':'))
foreach($pair in @(@('-gameData',$GameData),@('-unitManifest',$Manifest),@('-unitOutput',$Output),@('-preparedMaterials',$PreparedMaterials),@('-unitReport',$Report),@('-logFile',$LogPath))){
 $arguments+=@($pair[0],('"'+[IO.Path]::GetFullPath($pair[1])+'"'))
}
$arguments+='-quit'
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $LogPath -Pattern 'PASS genuine repeat units:' -Quiet)){throw "Repeat unit preparation failed: $LogPath"}
Select-String -LiteralPath $LogPath -Pattern 'PASS genuine repeat units:' | ForEach-Object Line
