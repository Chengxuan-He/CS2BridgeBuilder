param([Parameter(Mandatory)][string]$AssemblyPath,[Parameter(Mandatory)][string]$EvidenceRoot,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($AssemblyPath))
$type=$assembly.GetType('BridgeBuilder.Bridges.MovableMaterialSlots',$true)
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$bindings=Get-Content -LiteralPath (Join-Path $EvidenceRoot 'actual-bindings.json') -Raw | ConvertFrom-Json
$levels=@('Mechanism','Mechanism LOD1','Mechanism LOD2')
$records=@()
foreach($level in $levels){
    $slots=@()
    foreach($name in @("$level.Prefab","$level Appearance Group 1.Prefab")){
        $binding=@($bindings | Where-Object prefab -eq $name)
        if($binding.Count-ne 1){throw "Expected one actual binding: $name"}
        $attributes=Get-Content -LiteralPath (Join-Path $EvidenceRoot "attributes/$($binding[0].geometry).attributes.json") -Raw | ConvertFrom-Json
        for($slot=0;$slot-lt $binding[0].surfaces.Count;$slot++){
            $mesh=$attributes.meshes[$slot]
            $bytes=[Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $mesh -Depth 100 -Compress))
            $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
            $slots+=@{surface=$binding[0].surfaces[$slot];triangles=$mesh.faces.Count;vertices=$mesh.positions.Count;attributePayloadSha256=$hash}
        }
    }
    $records+=@{name=$level;slots=$slots}
}
[int[]]$totals=@($records | ForEach-Object {$_.slots.Count})
# Source declarations in Draw03SourceRenders plus actual owner output establish
# the structural prefix. Derive boundaries from matching saved prefix CIDs,
# rather than passing the expected 2/2/1 counts directly to the planner.
$nativeStructure=@(
    @('792a855e9d0c5c34d2c41d48b062139d','56e7aa2e4da82b8989419135bc6e5496'),
    @('792a855e9d0c5c34d2c41d48b062139d','56e7aa2e4da82b8989419135bc6e5496'),
    @('9212c7ed45a80abe105853ffe71bc8c4'))
[int[]]$starts=@(0,0,0)
for($lod=0;$lod-lt 3;$lod++){
    foreach($slot in $records[$lod].slots){
        if($nativeStructure[$lod]-contains $slot.surface){$starts[$lod]++}else{break}
    }
    if($starts[$lod]-ne $nativeStructure[$lod].Count){throw 'Saved structural prefix differs from the measured producer.'}
    if(($records[$lod].slots[0..($starts[$lod]-1)].surface-join ',')-ne ($nativeStructure[$lod]-join ',')){throw 'Structural source ordering changed.'}
    foreach($slot in $records[$lod].slots[$starts[$lod]..($totals[$lod]-1)]){
        if($nativeStructure[$lod]-contains $slot.surface){throw 'Structural slot found after the road boundary.'}
    }
}
$plan=$type.GetMethod('RoadRenderGroups',$flags).Invoke($null,[object[]]@($totals,$starts))
$old=$type.GetMethod('RenderGroups',$flags).Invoke($null,[object[]]@($totals,$totals))
if($null-eq $plan){throw 'Actual owner did not select semantic grouping.'}
$budgets=@($plan | ForEach-Object {($_ | ForEach-Object {$_.Count} | Measure-Object -Sum).Sum})
if(($budgets-join ',')-ne '5,15'){throw 'Unexpected actual-owner batch budgets.'}
for($lod=0;$lod-lt 3;$lod++){
    $seen=@($plan | ForEach-Object {$_[$lod]} | Sort-Object)
    if(($seen-join ',')-ne ((0..($totals[$lod]-1))-join ',')){throw "Slot conservation failed at LOD $lod"}
}
$walk=@('58690c1db88db29a8c141ff353d97f7e','58690c1db88db29a8c141ff353d97f7e','6e6733f56525f3dd0962fd3571b58c24')
$checks=@()
for($main=0;$main-lt 3;$main++){for($road=0;$road-lt 3;$road++){
    $walkCount=0;$oldWalkCount=0
    foreach($pair in @(@(0,$main),@(1,$road))){
        $group=$pair[0];$lod=$pair[1]
        foreach($slot in $plan[$group][$lod]){if($records[$lod].slots[$slot].surface-eq $walk[$lod]){$walkCount++}}
        foreach($slot in $old[$group][$lod]){if($records[$lod].slots[$slot].surface-eq $walk[$lod]){$oldWalkCount++}}
    }
    if($walkCount-ne 1){throw 'Independent LOD choices lost or duplicated the sidewalk.'}
    $checks+=@{mainLod=$main;roadLod=$road;sidewalkCount=$walkCount;oldSidewalkCount=$oldWalkCount}
}}
if(@($checks | Where-Object { $_.mainLod-eq 1 -and $_.roadLod-eq 2 })[0].oldSidewalkCount-ne 0){throw 'Old missing-sidewalk counterexample did not reproduce.'}
$result=@{assemblySha256=(Get-FileHash -LiteralPath $AssemblyPath).Hash;actualOwner='b82088213-1623-460e-9c56-d0d32bcfe848';totals=$totals;measuredRoadStarts=$starts;plan=$plan;budgets=$budgets;independentLodChecks=$checks;originalPayloads=$records;scope='Actual frozen DLL planner executed against saved owner slots; original per-mesh payload hashes retained. Not a newly generated native owner or visual acceptance.'}
$result | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Output -Encoding utf8
Write-Output "PASS frozen DLL actual-owner planner: budgets $($budgets-join '/'), nine independent LOD combinations preserve sidewalk; old gap reproduced."
