[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameDll,
      [Parameter(Mandatory=$true)][string]$Ilspy,
      [Parameter(Mandatory=$true)][string]$EvidenceDirectory)
$ErrorActionPreference='Stop'
[IO.Directory]::CreateDirectory($EvidenceDirectory)|Out-Null
function Read-Type([string]$name) {
    $text=(& $Ilspy -t $name $GameDll | Out-String)
    if($LASTEXITCODE -ne 0){throw "Native decompilation failed: $name"}
    [IO.File]::WriteAllText((Join-Path $EvidenceDirectory ($name+'.cs')),$text)
    return $text
}
$system=Read-Type 'Game.Prefabs.PrefabSystem'
$initialize=Read-Type 'Game.Prefabs.PrefabInitializeSystem'
$manager=Read-Type 'Game.SceneFlow.GameManager'
if($system -notmatch 'Dictionary<PrefabBase, Entity> m_UpdateMap' -or
   $system -notmatch 'm_Index = -1000000000' -or
   $system -notmatch 'm_Prefabs\[prefabData.m_Index\]') {throw 'Native retirement/index contract changed'}
if($initialize -notmatch 'GetEntityQuery\(ComponentType.ReadOnly<Created>\(\), ComponentType.ReadOnly<PrefabData>\(\)\)' -or
   $initialize -notmatch 'GetPrefab<PrefabBase>') {throw 'Native initialization query changed'}
if($manager -notmatch '(?s)m_State = State.WorldReady;.*?WaitXFrames\(4\).*?onWorldReady') {
    throw 'Native early WorldReady sequence changed'
}
"PASS native assembly metadata: Created initialization reads the invalidated index; WorldReady precedes the four-frame barrier. Game.dll SHA256=$((Get-FileHash -LiteralPath $GameDll).Hash)"
"This is a native assembly contract check, not an executed game lifecycle test."
$ordered=@('m_State = State.Quitting;', 'm_ModManager?.Dispose();', 'DestroyWorld();',
    'TaskManager.instance.CompleteAndClear()', 'AssetDatabase.global.Dispose();',
    'LogManager.ReleaseResources();')
$position=$manager.IndexOf('private async Task TerminateGame()')
foreach($step in $ordered){
    $next=$manager.IndexOf($step,$position,[StringComparison]::Ordinal)
    if($next -lt 0){throw "Native shutdown order changed: $step"}
    $position=$next+$step.Length
}
if($manager -notmatch '(?s)finally\s*\{\s*m_State = State.Terminated;\s*QuitGame\(\);' -or
   $manager -notmatch '(?s)if \(m_State == State.Quitting\).*?return false;.*?return true;') {
    throw 'Final native quit admission changed'
}
"PASS native shutdown order: mods, worlds, task drain, database, logging, then terminal quit admission."
