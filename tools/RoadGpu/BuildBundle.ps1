[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$UnityEditor,[Parameter(Mandatory=$true)][string]$Project)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'An editor process is already using the test environment; do not run concurrently.'}
$assets=Join-Path $Project 'Assets/RoadGpu136'
New-Item -ItemType Directory -Force -Path $assets,(Join-Path $Project 'Assets/Editor')|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RoadClip.compute'),(Join-Path $PSScriptRoot 'RoadEncode.compute') -Destination $assets -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RoadGpuBuild.Editor.cs') -Destination (Join-Path $Project 'Assets/Editor') -Force
$log=Join-Path $Project 'roadgpu-build.log'
$arguments='-batchmode -force-d3d11 -projectPath "'+$Project+'" -executeMethod RoadGpuBuild.Build -logFile "'+$log+'"'
$p=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0){throw "Compute bundle build failed; see $log"}
$bundle=Join-Path $root 'assets/roadgpu'
Copy-Item -LiteralPath (Join-Path $Project 'BuildRoadGpu/roadgpu') -Destination $bundle -Force
$sources=@{}
foreach($name in @('RoadClip.compute','RoadEncode.compute','RoadGpuBuild.Editor.cs')){$sources[$name]=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $name)).Hash}
@{bundleSha256=(Get-FileHash -LiteralPath $bundle).Hash;sources=$sources;editor=$UnityEditor;target='StandaloneWindows64';precision='double seam construction; float attributes';fxc='enable_d3d11_debug_symbols'}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $root 'assets/roadgpu.json')
