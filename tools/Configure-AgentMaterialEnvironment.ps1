[CmdletBinding()]
param([Parameter(Mandatory)][string]$Style, [switch]$Refresh)
$ErrorActionPreference = 'Stop'
$styles = @('Draw01','Draw02','Draw03','Lift01','Lift02','Lift03','Lift04','Lift05','PedestrianDraw01','PedestrianDraw02')
if ($Style -notin $styles) { throw 'Unknown bridge style.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root = Join-Path $repo "artifacts/agent-material-env/$Style"
if ($Refresh) {
    if (!(Test-Path -LiteralPath "$root/unity/ProjectSettings/ProjectSettings.asset")) { throw "Missing independent environment: $root" }
} elseif (Test-Path -LiteralPath $root) { throw "Preserve existing environment: $root" }
$snapshotName = if ($Refresh) { 'baker-current' } else { 'baker' }
$snapshot = Join-Path $root $snapshotName
if (Test-Path -LiteralPath $snapshot) { throw "Preserve existing snapshot: $snapshot" }
$project = Join-Path $root 'unity'
if (Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))
}) { throw "Unity is using the independent project: $project" }
$sourceNames = @('MovableUvChart','MovableStructurePacking','MovableStructureKey','MovableTextureFootprint','MovableAtlasChannel','MovableVtTextureSource','MovableTextureSource')
foreach ($relative in @("$snapshotName/tools/GpuBake","$snapshotName/tools/PreparedMaterials/Draw03","$snapshotName/src/BridgeBuilder/Bridges/Common/Movable",'unity/Assets/Editor','unity/Packages','unity/ProjectSettings','cache/upm','cache/temp','products','output','logs','reports')) {
    [IO.Directory]::CreateDirectory((Join-Path $root $relative)) | Out-Null
}
Copy-Item -LiteralPath "$repo/tools/Prepare-Repeats.ps1" -Destination "$snapshot/tools/Prepare-Repeats.ps1"
Copy-Item -LiteralPath "$repo/tools/Prepare-Structure.ps1" -Destination "$snapshot/tools/Prepare-Structure.ps1"
Copy-Item -LiteralPath "$repo/tools/Get-PreparedPipelineHashes.ps1" -Destination "$snapshot/tools/Get-PreparedPipelineHashes.ps1"
foreach ($name in @('PreparePaint.Editor.cs','PrepareStructure.Editor.cs','PrepareRepeat.Editor.cs','PreparedBakeStore.cs','BridgeBake.compute','PipelineEquivalence.json')) {
    Copy-Item -LiteralPath "$repo/tools/GpuBake/$name" -Destination "$snapshot/tools/GpuBake/$name"
}
foreach ($name in $sourceNames) {
    Copy-Item -LiteralPath "$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs" -Destination "$snapshot/src/BridgeBuilder/Bridges/Common/Movable/$name.cs"
}
foreach ($name in @('Test-Repeats.ps1','RepeatNativeCheck.Editor.cs')) {
    Copy-Item -LiteralPath "$repo/tools/PreparedMaterials/Draw03/$name" -Destination "$snapshot/tools/PreparedMaterials/Draw03/$name"
}
if (!$Refresh) {
    Copy-Item -LiteralPath "$repo/artifacts/unity-repeat-Draw01/ProjectSettings/ProjectSettings.asset" -Destination "$project/ProjectSettings/ProjectSettings.asset"
    Copy-Item -LiteralPath "$repo/artifacts/unity-repeat-Draw01/ProjectSettings/ProjectVersion.txt" -Destination "$project/ProjectSettings/ProjectVersion.txt"
    Copy-Item -LiteralPath "$repo/artifacts/unity-repeat-Draw01/Packages/manifest.json" -Destination "$project/Packages/manifest.json"
}
# Populate the isolated project's baker now; its local launcher refreshes only from its snapshot.
foreach ($name in @('PreparePaint','PrepareStructure','PrepareRepeat')) {
    Copy-Item -LiteralPath "$snapshot/tools/GpuBake/$name.Editor.cs" -Destination "$project/Assets/Editor/$name.cs"
}
Copy-Item -LiteralPath "$snapshot/tools/GpuBake/PreparedBakeStore.cs" -Destination "$project/Assets/Editor/PreparedBakeStore.cs"
foreach ($name in $sourceNames) {
    $code = [IO.File]::ReadAllText("$snapshot/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
    $code = $code.Replace('namespace BridgeBuilder.Bridges;', 'namespace BridgeBuilder.Bridges {') + "`n}"
    $code = $code.Replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
    $code = $code.Replace('texture.Apply(false,true)', 'texture.Apply(false,false)')
    $code = $code.Replace('makeNoLongerReadable: true', 'makeNoLongerReadable: false')
    [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs", $code)
}
Copy-Item -LiteralPath "$snapshot/tools/GpuBake/BridgeBake.compute" -Destination "$project/Assets/RepeatBake.compute"
$launcher = @'
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Inventory,
      [string]$ReusePipelineReport,
      [string[]]$ReadOnlyProductStores=@(),
      [switch]$ReuseOnly,
      [string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$snapshot=Join-Path $root '__SNAPSHOT__'
$run=Join-Path $root '__RUN__'
if ('__REFRESH__' -eq 'True') {
    if (!$ReusePipelineReport) { $ReusePipelineReport=Join-Path $root 'reports/bake.json' }
    $ReadOnlyProductStores=@((Join-Path $root 'products')) + $ReadOnlyProductStores
}
foreach($relative in @('products','output','logs','reports')){[IO.Directory]::CreateDirectory((Join-Path $run $relative)) | Out-Null}
$names=@('TEMP','TMP','UPM_CACHE_ROOT')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:TEMP=Join-Path $root 'cache/temp'; $env:TMP=$env:TEMP
    $env:UPM_CACHE_ROOT=Join-Path $root 'cache/upm'
    & "$snapshot/tools/Prepare-Repeats.ps1" -ProjectPath "$root/unity" -GameData $GameData -Inventory $Inventory -Output "$run/output" -Report "$run/reports/bake.json" -LogPath "$run/logs/bake.log" -ProductStore "$run/products" -ReusePipelineReport $ReusePipelineReport -ReadOnlyProductStores $ReadOnlyProductStores -ReuseOnly:$ReuseOnly
    & "$snapshot/tools/PreparedMaterials/Draw03/Test-Repeats.ps1" -ProjectPath "$root/unity" -GameManaged "$GameData/Managed" -Inventory $Inventory -Output "$run/output" -Report "$run/reports/bake.json" -LogPath "$run/logs/native.log"
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
'@
$launcher=$launcher.Replace('__SNAPSHOT__',$snapshotName).Replace('__REFRESH__',$Refresh.IsPresent.ToString()).Replace('__RUN__',$(if($Refresh){'current-repeat'}else{'.'}))
[IO.File]::WriteAllText((Join-Path $root $(if($Refresh){'Run-Current.ps1'}else{'Run.ps1'})), $launcher)
# A launcher must never silently point at a stale or partial private tool tree.
foreach ($file in Get-ChildItem -LiteralPath $snapshot -File -Recurse) {
    $relative=[IO.Path]::GetRelativePath($snapshot,$file.FullName)
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $repo $relative)).Hash) {
        throw "Independent baker snapshot differs: $relative"
    }
}
Write-Output $root
