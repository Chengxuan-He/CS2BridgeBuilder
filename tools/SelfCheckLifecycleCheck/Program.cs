using BridgeBuilder.Runtime;
using BridgeBuilder.Systems;
using Game;
using Game.SceneFlow;
static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
static Probe Fresh()
{
    BridgeStartupAssetSystem.StopInspection();
    GameManager.instance = new() { state = GameManager.State.Booting, isGameLoading = true };
    BridgeDiskAudit.Calls = 0; BridgeDiskAudit.Incomplete = false; BridgeDiskAudit.Raise = false; BridgeDiskAudit.BackupFailed = false; BridgeDiskAudit.DeleteFailed = false; BridgeDiskAudit.Retirement = null;
    Colossal.IO.AssetDatabase.PrefabAsset.Reads = 0;
    BridgeStartupRecovery.Retired.Clear(); BridgeBuilder.Mod.Messages.Clear();
    Game.Prefabs.PrefabSystem.Loaded = new() { new Game.Prefabs.NetGeometryPrefab { name = "bridge", asset = new() } };
    Colossal.IO.AssetDatabase.AssetDatabase.user.Assets = new() { new() {
        path = Path.Combine(BridgeAssetCatalog.Root,"bridge","bridge.Prefab"), Instance = Game.Prefabs.PrefabSystem.Loaded[0] } };
    BridgeDependencyPersistence.MigrationChanged = false; BridgeDependencyPersistence.MigrationCalls = 0; BridgeDependencyPersistence.MigrationFail = false;
    BridgeLegacyNames.Changed = false; BridgeLegacyNames.Fail = false;
    BridgeAssetLayout.Changed = false; BridgeAssetLayout.Fail = false; BridgeAssetLayout.Owners = 0;
    BridgePortableMigration.Changed = false; BridgePortableMigration.Fail = false;
    BridgeNetworkValidation.Calls = 0; BridgeNetworkValidation.Invalid = false;
    BridgeNetworkValidation.Raise = false;
    ((Colossal.IO.AssetDatabase.ParadoxModsDataSource)Colossal.IO.AssetDatabase.AssetDatabase<Colossal.IO.AssetDatabase.ParadoxMods>.instance.dataSource).Cached = true;
    BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Broken;
    BridgeDependencyPersistence.Calls = 0; BridgeDependencyPersistence.Fail = false;
    var p = new Probe(); p.Create(); return p;
}
static void Menu(Probe p)
{
    GameManager.instance.gameMode = GameMode.MainMenu;
    GameManager.instance.state = GameManager.State.WorldReady;
    GameManager.instance.isGameLoading = false; p.Complete(); p.Tick();
}
var p = Fresh(); p.ScheduleInspectionAfterLoad(); p.Tick();
Check(BridgeNetworkValidation.Calls == 0, "Mod alone cannot inspect");
Menu(p); Check(BridgeDiskAudit.Calls == 0 && BridgeNetworkValidation.Calls == 1 && BridgeBuilder.Mod.Messages.Count == 0, "Ready menu runs memory inspection");
Menu(p); Check(BridgeNetworkValidation.Calls == 1, "Automatic check runs once");
p.RequestManualCheck(); p.Tick();
Check(BridgeNetworkValidation.Calls == 2 && BridgeBuilder.Mod.Messages.Count == 0, "Manual read-only success");
p = Fresh(); BridgeStartupRecovery.Retired.Add("bridge"); p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Count == 0, "User-requested removal is not reported as repaired damage by self-check");
p = Fresh(); Menu(p); Check(BridgeNetworkValidation.Calls == 0, "Game alone cannot inspect");
p.ScheduleInspectionAfterLoad(); p.Tick(); Check(BridgeNetworkValidation.Calls == 1, "Late mod starts from already completed game state");
p = Fresh(); p.ScheduleInspectionAfterLoad(); GameManager.instance.gameMode = GameMode.MainMenu;
p.Complete(); p.Tick(); Check(BridgeNetworkValidation.Calls == 0, "Completion callback still waits for loading state transition");
Menu(p); Check(BridgeNetworkValidation.Calls == 1, "State transition releases check");
p = Fresh(); BridgeNetworkValidation.Invalid = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart", "Memory evidence retires only owned files with restart notice");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeDiskAudit.Incomplete = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 0 && BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckIncomplete"), "Incomplete enumeration reports removal failure");
p = Fresh(); p.ScheduleInspectionAfterLoad(); GameManager.instance.gameMode = GameMode.Game;
GameManager.instance.state = GameManager.State.WorldReady; GameManager.instance.isGameLoading = false; p.Tick();
Check(BridgeNetworkValidation.Calls == 0, "No retirement while city loaded");
Menu(p); Check(BridgeNetworkValidation.Calls == 1, "Pending check resumes at menu");
BridgeStartupAssetSystem.StopInspection(); p.RequestManualCheck(); p.Tick(); Check(BridgeNetworkValidation.Calls == 1, "Disposed mod cannot inspect");
p = Fresh(); BridgeNetworkValidation.Raise = true; p.ScheduleInspectionAfterLoad(); var propagated = false;
try { Menu(p); } catch (InvalidOperationException) { propagated = true; }
Check(propagated && !BridgeStartupAssetSystem.IsStartupInspection, "Exception propagated with lifecycle released");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets.Clear(); p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeNetworkValidation.Calls == 0 && BridgeDiskAudit.Calls == 0 && BridgeStartupRecovery.Retired.Count == 0,
    "Unloaded prefab is not corruption and does not trigger disk scan");
p = Fresh(); BridgeNetworkValidation.Invalid = true; p.ScheduleInspectionAfterLoad(); Menu(p);
p.RequestManualCheck(); p.Tick();
Check(BridgeDiskAudit.Calls == 1 && BridgeNetworkValidation.Calls == 1, "Retired owners are not reprocessed");
p = Fresh(); Game.Prefabs.PrefabSystem.Loaded[0].Registered = false;
Game.Prefabs.PrefabSystem.Loaded.Clear(); p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeDependencyPersistence.MigrationCalls == 1,
    "Dependency migration is attempted before retiring an unregistered cached prefab (case 03)");
p = Fresh(); Game.Prefabs.PrefabSystem.Loaded[0].Available = false;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeDependencyPersistence.MigrationCalls == 1, "Unavailable bridge content is retired after attempted dependency migration");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0].Instance = null;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart", "Failed loading retires owner");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0].Instance = null;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge"), "Explicit file damage covers missing cached object");
p = Fresh(); BridgeDependencyPersistence.MigrationFail = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 1 && BridgeDiskAudit.Calls == 1 && BridgeDependencyPersistence.MigrationCalls == 1
    && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart",
    "Dependency migration failure falls back to removal");
p = Fresh(); p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeDependencyPersistence.MigrationCalls == 1 && BridgeDiskAudit.Calls == 0, "Healthy bridge migrates without disk integrity audit");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0].path = Path.GetFullPath("unused/elsewhere/bridge/bridge.Prefab");
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeDependencyPersistence.MigrationCalls == 0 && BridgeNetworkValidation.Calls == 0, "Assets outside exact imported root excluded");
p = Fresh(); var pdx = (Colossal.IO.AssetDatabase.ParadoxModsDataSource)Colossal.IO.AssetDatabase.AssetDatabase<Colossal.IO.AssetDatabase.ParadoxMods>.instance.dataSource;
pdx.Cached = false; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeNetworkValidation.Calls == 0, "Main menu alone cannot inspect before PDX assets are cached");
pdx.Cached = true; var caching = new System.Threading.Tasks.TaskCompletionSource<bool>(); GameManager.instance.SetCachingTask(caching.Task); p.Tick();
Check(BridgeNetworkValidation.Calls == 0, "Wait for PDX registration task");
caching.SetResult(true); p.Tick(); Check(BridgeNetworkValidation.Calls == 1, "State polling catches completion without event");
pdx.Begin(); p.Tick(); Check(BridgeNetworkValidation.Calls == 1, "A new asset batch blocks pending inspection");
pdx.Complete(); p.Tick(); Check(BridgeNetworkValidation.Calls == 2, "Batch completion wakes inspection");
p = Fresh(); GameManager.instance.modManager.isInitialized = false; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeNetworkValidation.Calls == 0, "Wait for all mod initialization");
GameManager.instance.modManager.isInitialized = true; p.Tick(); Check(BridgeNetworkValidation.Calls == 1, "Initialization gate releases late check");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Recoverable;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeDependencyPersistence.Calls == 1 && BridgeDiskAudit.Calls == 0 && BridgeStartupRecovery.Retired.Count == 0
    && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart", "Loaded CID dependency supplementation is legacy migration, never damage repair");
p.RequestManualCheck(); p.Tick(); Check(BridgeDependencyPersistence.Calls == 1, "Same-session recovered bridge remains idempotent");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Recoverable; BridgeDependencyPersistence.Fail = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart", "Unsuccessful recovery copy retires bridge");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Incomplete;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeDependencyPersistence.Calls == 0,
    "Unknown reason for required null is retired, never retained as inconclusive");
p = Fresh(); BridgeDependencyPersistence.MigrationChanged = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 0 && BridgeDependencyPersistence.Calls == 0
    && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart", "Healthy dependency-only migration is not reported as damage");
Check(BridgeDependencyPersistence.MigrationCalls == 1 && BridgeNetworkValidation.Calls == 1,
    "Healthy live references do not bypass external material snapshot supplementation");
BridgeDependencyPersistence.MigrationChanged = false;
p.RequestManualCheck(); p.Tick();
Check(BridgeDependencyPersistence.MigrationCalls == 1 && BridgeDiskAudit.Calls == 0
    && BridgeBuilder.Mod.Messages.Last() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart",
    "Migrated snapshot owners skip stale references without repeating the notice");
p = Fresh(); BridgeLegacyNames.Changed = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart", "Legacy rename requests restart as migration");
p.RequestManualCheck(); p.Tick();
Check(BridgeDependencyPersistence.MigrationCalls == 1, "Renamed bridge skips stale in-memory asset index until restart");
p = Fresh(); BridgeLegacyNames.Fail = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart" && BridgeStartupRecovery.Retired.Count == 1,
    "Rename failure falls back to removal");
p = Fresh(); BridgePortableMigration.Changed = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart" && BridgeStartupRecovery.Retired.Count == 0,
    "Additive native conversion requests restart without removing bridge");
p.RequestManualCheck(); p.Tick();
Check(BridgeDependencyPersistence.MigrationCalls == 1, "Converted bridge skips stale cached objects until restart");
p = Fresh(); BridgePortableMigration.Fail = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart" && BridgeStartupRecovery.Retired.Count == 1,
    "Conversion failure falls back to removal");
p = Fresh(); BridgePortableMigration.Changed = true; BridgeAssetLayout.Changed = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeAssetLayout.Owners == 1 && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart",
    "Native conversion and file layout run in the same inspection");
p = Fresh(); BridgeAssetLayout.Changed = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeAssetLayout.Owners == 1 && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart",
    "Already native bridge still receives file layout migration");
p.RequestManualCheck(); p.Tick();
Check(BridgeAssetLayout.Owners == 1 && BridgeDependencyPersistence.MigrationCalls == 1, "Moved files never reenter stale asset streams");
p = Fresh(); BridgeAssetLayout.Fail = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckIncomplete") && BridgeStartupRecovery.Retired.Count == 0,
    "Layout conflict preserves healthy bridge");
p = Fresh(); BridgePortableMigration.Changed = true; BridgeAssetLayout.Fail = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckIncomplete") && !BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckMigrated") && BridgeStartupRecovery.Retired.Count == 0, "Partial conversion plus layout conflict is only unfinished");
p.RequestManualCheck(); p.Tick();
Check(BridgeBuilder.Mod.Messages.Last().Contains("BridgeSelfCheckIncomplete"), "Old unfinished result is not recounted for session-protected owner");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Recoverable;
BridgeAssetLayout.Changed = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeAssetLayout.Owners == 1 && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart",
    "Dependency supplementation and layout are both migration, not damage repair");
p = Fresh(); BridgePortableMigration.Fail = true; BridgeDiskAudit.Incomplete = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 0
    && BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckIncomplete"),
    "Failed removal is not reported as retired");
// Every subset of the four final categories, with repeated writes to the same owner.
foreach (var mask in Enumerable.Range(0, 16))
{
    var report = new BridgeSelfCheckReport();
    for (var i = 0; i < 4; i++) if ((mask & (1 << i)) != 0)
    {
        report.Set("owner" + i, BridgeSelfCheckReport.Outcome.Unfinished);
        report.Set("owner" + i, (BridgeSelfCheckReport.Outcome)i);
        report.Set("owner" + i, (BridgeSelfCheckReport.Outcome)i);
    }
    Check(report.Total == System.Numerics.BitOperations.PopCount((uint)mask), "UUID results are mutually exclusive");
    Check(report.HasNotice == (mask != 0), "Empty run stays silent");
    Check(report.Message().Split("BridgeSelfCheckRestart").Length - 1 == (mask == 0 ? 0 : 1), "One shared restart reminder");
    for (var i = 0; i < 4; i++) Check(report.Count((BridgeSelfCheckReport.Outcome)i) == ((mask >> i) & 1), "Only selected nonzero outcomes");
}
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeDiskAudit.BackupFailed = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRemovedWithoutBackup(1)\n\nBridgeSelfCheckRestart", "Backup failure with completed deletion has its own final category");
p.RequestManualCheck(); p.Tick(); Check(BridgeBuilder.Mod.Messages.Count == 1, "Removed result does not accumulate or repeat");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeDiskAudit.BackupFailed = true; BridgeDiskAudit.DeleteFailed = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 0 && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckIncomplete(1)\n\nBridgeSelfCheckRestart", "Deletion failure is unfinished, not removed");
BridgeDiskAudit.DeleteFailed = false; p.RequestManualCheck(); p.Tick();
Check(BridgeBuilder.Mod.Messages.Count == 1 && BridgeDiskAudit.Calls == 1 && Colossal.IO.AssetDatabase.PrefabAsset.Reads == 1 && BridgeStartupRecovery.Retired.Count == 0, "Partial removal failure protects stale assets without claiming retired or repeating notices");
p = Fresh(); BridgeNetworkValidation.Raise = true; p.ScheduleInspectionAfterLoad();
try { Menu(p); } catch (InvalidOperationException) { }
BridgeNetworkValidation.Raise = false; p.Tick();
Check(BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckIncomplete(1)") && BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckInterrupted"), "Exception retains known unfinished owner and releases gate");
p = Fresh(); BridgeDependencyPersistence.MigrationChanged = true;
Colossal.IO.AssetDatabase.AssetDatabase.user.Assets.Add(Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0]);
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRestart", "Multiple Prefabs count one owner");
p.RequestManualCheck(); p.Tick(); Check(BridgeBuilder.Mod.Messages.Count == 1, "Migrated owner not notified twice");
p = Fresh(); BridgePortableMigration.Changed = true; BridgeAssetLayout.Fail = true;
p.ScheduleInspectionAfterLoad(); Menu(p); p.RequestManualCheck(); p.Tick();
Check(BridgeBuilder.Mod.Messages.Count == 1, "Session-protected partial migration not recounted");
p = Fresh(); BridgeDependencyPersistence.MigrationChanged = true;
var assets = Colossal.IO.AssetDatabase.AssetDatabase.user.Assets;
assets.Clear();
foreach (var i in Enumerable.Range(0, 4))
{
    var owner = "bridge-" + i;
    assets.Add(new() { path = Path.Combine(BridgeAssetCatalog.Root, owner, owner + ".Prefab"),
        Instance = new Game.Prefabs.NetGeometryPrefab { name = owner, Available = i == 0 } });
    assets.Add(assets.Last()); // Multiple cached assets never multiply UUID counts.
}
BridgeDiskAudit.Retirement = owner => owner == "bridge-1" ? (true, true) : owner == "bridge-2" ? (true, false) : (false, false);
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckMigrated(1)\n\nBridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRemovedWithoutBackup(1)\n\nBridgeSelfCheckIncomplete(1)\n\nBridgeSelfCheckRestart", "Actual lifecycle combines all four mutually exclusive final categories");
p.RequestManualCheck(); p.Tick();
Check(BridgeBuilder.Mod.Messages.Count == 1, "Next run skips all session-protected owners without repeating old outcomes");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeDiskAudit.Incomplete = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
BridgeDiskAudit.Incomplete = false; p.RequestManualCheck(); p.Tick();
Check(BridgeBuilder.Mod.Messages.Last() == "BridgeSelfCheckRemoved(1)\n\nBridgeSelfCheckRestart", "Read-only enumeration failure may retry, without accumulating prior unfinished result");
var unknown = new BridgeSelfCheckReport { Interrupted = true };
Check(unknown.Total == 0 && unknown.Message() == "BridgeSelfCheckInterrupted\n\nBridgeSelfCheckRestart", "Global unknown failure invents no UUID or count");
foreach (var kind in Enumerable.Range(0, 4))
{
    p = Fresh();
    BridgeDependencyPersistence.MigrationChanged = kind == 0;
    BridgePortableMigration.Changed = kind == 1;
    BridgeLegacyNames.Changed = kind == 2;
    BridgeAssetLayout.Changed = kind == 3;
    p.ScheduleInspectionAfterLoad(); Menu(p); p.RequestManualCheck(); p.Tick();
    Check(Colossal.IO.AssetDatabase.PrefabAsset.Reads == 1 && BridgeBuilder.Mod.Messages.Count == 1,
        "Every migration path filters copied owners before cached GetInstance");
}
p = Fresh();
Colossal.IO.AssetDatabase.AssetDatabase.user.Assets.Add(new() { path = Path.Combine(BridgeAssetCatalog.Root, "bridge-other", "bridge-other.Prefab"), Instance = new() });
Check(BridgeInspectionAssets.Read(owner => owner == "bridge").Count() == 1 && Colossal.IO.AssetDatabase.PrefabAsset.Reads == 1,
    "Migration owner predicate excludes unrelated cached instances before reading");
Console.WriteLine("PASS memory lifecycle, native conversion, repair-or-remove, both load orders and idempotence");
class Probe : BridgeStartupAssetSystem
{
    public void Create() => OnCreate();
    public void Complete() => OnGameLoadingComplete(default, GameMode.MainMenu);
    public void Tick() { if (Enabled) OnUpdate(); }
}
