param(
    [Parameter(Mandatory)] [string]$ModAssembly,
    [string]$GameAssembly = (Join-Path ([Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH', 'User')) 'Game.dll')
)
$ErrorActionPreference = 'Stop'
function Read-Type($assembly, $type) {
    $source = (& ilspycmd -r (Split-Path (Resolve-Path $GameAssembly)) -t $type $assembly) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw "Could not inspect $type in $assembly" }
    $source
}
function Require-Order($source, $patterns) {
    $after = 0
    foreach ($pattern in $patterns) {
        $match = [regex]::new($pattern).Match($source, $after)
        if (!$match.Success) { throw "Missing or out of order: $pattern" }
        $after = $match.Index + $match.Length
    }
}
# The native main loop registers these systems in update order. The final snapshot
# and actual destruction run after all normal updates, including UI.
$order = Read-Type $GameAssembly 'Game.Common.SystemOrder'
Require-Order $order @('UpdateAt<PrefabSystem>\(SystemUpdatePhase.MainLoop', 'UpdateAt<ToolSystem>\(SystemUpdatePhase.MainLoop', 'UpdateAt<ModificationSystem>\(SystemUpdatePhase.MainLoop', 'UpdateAt<UIUpdateSystem>\(SystemUpdatePhase.MainLoop')
Require-Order $order @('UpdateAfter<PrepareCleanUpSystem>\(SystemUpdatePhase.MainLoop','UpdateAt<CleanUpSystem>\(SystemUpdatePhase.Cleanup','UpdateAt<SubElementDeleteSystem>\(SystemUpdatePhase.PostTool')
Require-Order (Read-Type $GameAssembly 'Game.Tools.ToolSystem') @('Update\(SystemUpdatePhase.PostTool\)')
Require-Order (Read-Type $GameAssembly 'Game.Common.PrepareCleanUpSystem') @('ReadOnly<Deleted>', 'm_DeletedQuery.ToEntityListAsync', 'AddDeleted\(')
Require-Order (Read-Type $GameAssembly 'Game.Common.CleanUpSystem') @('m_DeletedDeps.Complete\(', 'DestroyEntity\(m_DeletedEntities.AsArray\(')
$mod = Read-Type $ModAssembly 'BridgeBuilder.Mod'
if ([regex]::Matches($mod, 'Update\w*<BridgeOwnerCleanupSystem\b').Count -ne 1) { throw 'Unexpected number of owner-cleanup registrations' }
Require-Order $mod @('UpdateBefore<BridgeOwnerCleanupSystem, (?:Game\.Prefabs\.)?PrefabSystem>\(SystemUpdatePhase.MainLoop\)')
'PASS: compiled cleanup precedes native prefab, PostTool and modification consumers; UI updates follow those consumers and precede same-frame destruction.'
