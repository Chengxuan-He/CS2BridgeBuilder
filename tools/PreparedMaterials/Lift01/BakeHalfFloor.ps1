[CmdletBinding()]
param([string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$private=Join-Path $repo 'artifacts/material-remap/environments/Lift01'
$project=Join-Path $private 'unity'
$output=Join-Path $repo 'artifacts/material-remap/Lift01/half-floor'
$env:TEMP=Join-Path $private 'cache/temp'
$env:TMP=$env:TEMP
$env:UPM_CACHE_ROOT=Join-Path $private 'cache/upm'
$settings=Join-Path $project 'ProjectSettings/ProjectSettings.asset'
[IO.File]::WriteAllText($settings,([IO.File]::ReadAllText($settings) -replace 'm_ActiveColorSpace: [01]','m_ActiveColorSpace: 1'))
foreach($name in @('PreparePaint','PrepareRepeatUnits','NativeTextureEncoding')) {
 $code=[IO.File]::ReadAllText("$repo/tools/GpuBake/$name.Editor.cs")
 if($name -eq 'PrepareRepeatUnits') {
  # Original near floor panels have a finite cross-section and repeated length.
  $code=$code.Replace('if(u.bands?.Length>0&&(!u.RepeatU||!u.RepeatV))','if(u.bands?.Length>0&&!u.RepeatV)')
 }
 [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs",$code)
}
foreach($name in @('MovableTextureSource','MovableVtTextureSource')) {
 $code=[IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
 $code=$code.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+"`n}"
 $code=$code.Replace('UnityEngine.Object.Destroy(','UnityEngine.Object.DestroyImmediate(')
 [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs",$code)
}
$compute=[IO.File]::ReadAllText("$repo/tools/GpuBake/RepeatUnit.compute")
# The clamped finite-axis guard must also use the last original panel at q=1.
$compute=$compute.Replace('q.x>=b.x&&q.x<b.y','q.x>=b.x&&(q.x<b.y||(i==BandCount-1&&q.x==b.y))')
[IO.File]::WriteAllText("$project/Assets/RepeatUnit.compute",$compute)
$pipeline=(@(Get-ChildItem "$project/Assets/Editor" -Filter '*.cs')+@(Get-Item "$project/Assets/RepeatUnit.compute") | ForEach-Object {(Get-FileHash -LiteralPath $_.FullName).Hash}) -join ':'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PrepareRepeatUnits.Run','-style','Lift01','-unitPipeline',$pipeline)
foreach($pair in @(@('-gameData','D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'),@('-unitManifest',"$output/inputs/manifest.json"),@('-unitOutput',"$output/output"),@('-preparedMaterials',"$repo/assets/Materials"),@('-unitStore',"$private/cache/products"),@('-unitReport',"$output/report.json"),@('-logFile',"$private/logs/half-floor-bake.log"))) {
 $arguments+=@($pair[0],('"'+$pair[1]+'"'))
}
$arguments+='-quit'
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$private/logs/half-floor-bake.log" -Pattern 'PASS genuine repeat units:' -Quiet)){throw 'Lift01 half-floor preparation failed; see private log.'}
Write-Output 'PASS Lift01 half-floor preparation.'
