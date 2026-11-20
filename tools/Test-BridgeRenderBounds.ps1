[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$managed = Join-Path $GameDir 'Cities2_Data/Managed'
foreach ($name in @('Unity.Mathematics', 'Colossal.Mathematics', 'Unity.Entities', 'Game')) {
    [Reflection.Assembly]::LoadFrom((Join-Path $managed ($name + '.dll'))) | Out-Null
}
$mod = [Reflection.Assembly]::LoadFrom((Join-Path $projectRoot 'src/BridgeBuilder/bin/Release/BridgeBuilder.dll'))
$include = $mod.GetType('BridgeBuilder.Runtime.BridgeRenderBounds').GetMethod('Include', [Reflection.BindingFlags]'Static,NonPublic')
function Bounds([float]$low, [float]$high) {
    [Colossal.Mathematics.Bounds3]::new([Unity.Mathematics.float3]::new(-20,$low,-281), [Unity.Mathematics.float3]::new(20,$high,281))
}
function Need([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
# Measured Lupu owner b66a1e3a8: both retained LODs extend beneath the road.
$water = [Colossal.Mathematics.Bounds1]::new(0,40.333984)
$mesh = [Colossal.Mathematics.Bounds1]::new(-37.22777,40.333984)
foreach ($road in @(@(0,0), @(10,30), @(-5,15))) {
    $curves = Bounds $road[0] $road[1]
    $old = Bounds ($road[0]+$water.min) ($road[1]+$water.max)
    $result = $include.Invoke($null, @($old,$curves,$mesh))
    Need ([Math]::Abs($result.min.y-($road[0]+$mesh.min)) -lt 0.0001) 'Missing below-road structure'
    Need ($result.max.y -eq $old.max.y) 'Existing upper range changed'
    Need ($result.min.x -eq $old.min.x -and $result.max.z -eq $old.max.z) 'Horizontal envelope changed'
    $again = $include.Invoke($null, @($result,$curves,$mesh))
    Need ($again.min.y -eq $result.min.y -and $again.max.y -eq $result.max.y) 'Repeated update accumulates depth'
}
$old = Bounds 0 $water.max
$result = $include.Invoke($null, @($old,(Bounds 0 0),$mesh))
# The lower-arch camera interval was disjoint from the old broadphase box.
Need (!($old.min.y -le -5 -and $old.max.y -ge -35)) 'Fixture does not reproduce old rejection'
Need ($result.min.y -le -5 -and $result.max.y -ge -35) 'Lower arch remains outside culling envelope'
Need ($water.min -eq 0 -and $water.max -eq [float]40.333984) 'Water validation range changed'
$terrain = Bounds -60 50
$result = $include.Invoke($null, @($terrain,(Bounds 0 0),$mesh))
Need ($result.min.y -eq -60 -and $result.max.y -eq 50) 'Native terrain envelope lost'
Write-Host 'PASS measured lower-arch frustum, sloped and elevated curves, repeat updates, native terrain bounds and unchanged water interval.'
