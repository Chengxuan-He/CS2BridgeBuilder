[CmdletBinding()]
param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$envRoot=Join-Path $repo 'artifacts/material-resume/Lift05/env'
$resultRoot=Join-Path $repo 'artifacts/material-resume/Lift05/profile-unit'
$project=Join-Path $envRoot 'unity'
$baker=Join-Path $envRoot 'baker'
foreach($dir in @('unity/Assets/Editor','unity/ProjectSettings','unity/Packages','cache/temp','cache/upm','logs','products','baker/tools/GpuBake','baker/src/BridgeBuilder/Bridges/Common/Movable')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $envRoot $dir) | Out-Null
}
foreach($name in @('ProjectSettings.asset','ProjectVersion.txt')) {
    Copy-Item -LiteralPath "$repo/artifacts/agent-material-env/Lift05/unity/ProjectSettings/$name" -Destination "$project/ProjectSettings/$name"
}
Copy-Item -LiteralPath "$repo/artifacts/agent-material-env/Lift05/unity/Packages/manifest.json" -Destination "$project/Packages/manifest.json"
Copy-Item -LiteralPath "$repo/tools/Prepare-RepeatUnits.ps1" -Destination "$baker/tools/Prepare-RepeatUnits.ps1"
foreach($name in @('PreparePaint.Editor.cs','PrepareRepeatUnits.Editor.cs','NativeTextureEncoding.Editor.cs','RepeatUnit.compute')) {
    Copy-Item -LiteralPath "$repo/tools/GpuBake/$name" -Destination "$baker/tools/GpuBake/$name"
}
foreach($name in @('MovableTextureSource.cs','MovableVtTextureSource.cs')) {
    Copy-Item -LiteralPath "$repo/src/BridgeBuilder/Bridges/Common/Movable/$name" -Destination "$baker/src/BridgeBuilder/Bridges/Common/Movable/$name"
}
$previous=@{}
foreach($name in @('TEMP','TMP','UPM_CACHE_ROOT')) {$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:TEMP="$envRoot/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$envRoot/cache/upm"
    & "$baker/tools/Prepare-RepeatUnits.ps1" -Style Lift05 -ProjectPath $project -GameData $GameData -Manifest "$resultRoot/input/manifest.json" -Output "$resultRoot/output" -PreparedMaterials "$repo/assets/Materials" -Report "$resultRoot/bake.json" -LogPath "$envRoot/logs/prepare.log" -ProductStore "$envRoot/products"
} finally {
    foreach($name in $previous.Keys) {[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
