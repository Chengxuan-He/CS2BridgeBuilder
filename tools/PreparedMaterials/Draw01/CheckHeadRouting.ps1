param([string]$Repository = 'D:/CS2BridgeBuilder')
$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw (Join-Path $Repository 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01HeadRoad.cs')
$assembly = Get-Content -Raw (Join-Path $Repository 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01NetworkAssembly.cs')
if ([regex]::Matches($source,'native\.Compose\(').Count -ne 1) { throw 'Expected exactly one head composition query.' }
foreach ($forbidden in @('actualCompositions','foreach (bool elevated','RecordMapping','Priority(','ambiguous')) {
 if($source.Contains($forbidden)) { throw "Obsolete branch selection: $forbidden" }
}
foreach ($required in @('BridgeRoadGeneration.Find(target)','var flags = context.Flags;', 'context.Flags, flags','headPaths.Contains(occurrence.SourcePath)','MovableRoadAssets.Level.NativeOriginal(sources[lod], sources[lod].bounds)','Sources(occurrence.Prefab).Any')) {
 if(-not $source.Contains($required)) { throw "Missing context/scope/LOD invariant: $required" }
}
if($assembly.Contains('road.Road.Compositions')) { throw 'Physical contributions are still passed as branch candidates.' }
if($source.Contains('~CompositionFlags.General.Elevated') -or $source.Contains('General.Tunnel')) { throw 'Head changes the selected road level.' }
'PASS: single shared-context head query; no alternative selection/ranking; head scope and per-LOD Original retained. Static source checks only, not native game execution.'