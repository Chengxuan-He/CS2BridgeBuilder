using BridgeBuilder.Runtime;
using BridgeBuilder.Systems;
using Game;
using Game.SceneFlow;
static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
static Probe Fresh()
{
    BridgeStartupAssetSystem.StopInspection();
    GameManager.instance = new() { state = GameManager.State.Booting, isGameLoading = true };
    BridgeDiskAudit.Calls = 0; BridgeDiskAudit.Incomplete = false; BridgeDiskAudit.Raise = false;
    BridgeStartupRecovery.Retired.Clear(); BridgeBuilder.Mod.Messages.Clear();
    Game.Prefabs.PrefabSystem.Loaded = new() { new Game.Prefabs.NetGeometryPrefab { name = "bridge", asset = new() } };
    Colossal.IO.AssetDatabase.AssetDatabase.user.Assets = new() { new() {
        path = Path.Combine(BridgeAssetCatalog.Root,"bridge","bridge.Prefab"), Instance = Game.Prefabs.PrefabSystem.Loaded[0] } };
    BridgeAssetMigration.Changed = false; BridgeAssetMigration.Calls = 0; BridgeAssetMigration.Fail = false;
    BridgeLegacyNames.Changed = false; BridgeLegacyNames.Fail = false;
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
Check(BridgeNetworkValidation.Calls == 2 && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckSuccess", "Manual read-only success");
p = Fresh(); Menu(p); Check(BridgeNetworkValidation.Calls == 0, "Game alone cannot inspect");
p.ScheduleInspectionAfterLoad(); p.Tick(); Check(BridgeNetworkValidation.Calls == 1, "Late mod starts from already completed game state");
p = Fresh(); p.ScheduleInspectionAfterLoad(); GameManager.instance.gameMode = GameMode.MainMenu;
p.Complete(); p.Tick(); Check(BridgeNetworkValidation.Calls == 0, "Completion callback still waits for loading state transition");
Menu(p); Check(BridgeNetworkValidation.Calls == 1, "State transition releases check");
p = Fresh(); BridgeNetworkValidation.Invalid = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Memory evidence retires only owned files with restart notice");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeDiskAudit.Incomplete = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 0 && BridgeBuilder.Mod.Messages.Single().Contains("BridgeSelfCheckIncomplete"), "Incomplete audit retains assets");
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
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeAssetMigration.Calls == 0,
    "Unregistered cached prefab excluded from PrefabSystem is retired, not migrated (case 03)");
p = Fresh(); Game.Prefabs.PrefabSystem.Loaded[0].Available = false;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeAssetMigration.Calls == 0, "Unavailable bridge content is retired");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0].Instance = null;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Failed loading retires owner");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0].Instance = null;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge"), "Explicit file damage covers missing cached object");
p = Fresh(); BridgeAssetMigration.Fail = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeAssetMigration.Calls == 1 && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Dependency/migration failure retires bridge (missing geometry case 12)");
p = Fresh(); p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeAssetMigration.Calls == 1 && BridgeDiskAudit.Calls == 0, "Healthy bridge migrates without disk integrity audit");
p = Fresh(); Colossal.IO.AssetDatabase.AssetDatabase.user.Assets[0].path = Path.GetFullPath("unused/elsewhere/bridge/bridge.Prefab");
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeAssetMigration.Calls == 0 && BridgeNetworkValidation.Calls == 0, "Assets outside exact imported root excluded");
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
    && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Loaded CID copies dependencies, retains bridge and requests restart");
p.RequestManualCheck(); p.Tick(); Check(BridgeDependencyPersistence.Calls == 1, "Same-session recovered bridge remains idempotent");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Recoverable; BridgeDependencyPersistence.Fail = true;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Unsuccessful recovery copy retires bridge");
p = Fresh(); BridgeNetworkValidation.Invalid = true; BridgeLoadedCidRecovery.Outcome = BridgeLoadedCidRecovery.Result.Incomplete;
p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Contains("bridge") && BridgeDependencyPersistence.Calls == 0,
    "Unknown reason for required null is retired, never retained as inconclusive");
p = Fresh(); BridgeAssetMigration.Changed = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeStartupRecovery.Retired.Count == 0 && BridgeDependencyPersistence.Calls == 0
    && BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Successful dependency-only migration automatically shows repair popup without removal");
p = Fresh(); BridgeLegacyNames.Changed = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckRepaired", "Legacy rename requests restart");
p.RequestManualCheck(); p.Tick();
Check(BridgeAssetMigration.Calls == 1, "Renamed bridge skips stale in-memory asset index until restart");
p = Fresh(); BridgeLegacyNames.Fail = true; p.ScheduleInspectionAfterLoad(); Menu(p);
Check(BridgeBuilder.Mod.Messages.Single() == "BridgeSelfCheckIncomplete" && BridgeStartupRecovery.Retired.Count == 0,
    "Rename IO failure reports incomplete without removing healthy bridge");
Console.WriteLine("PASS memory lifecycle: both load orders, healthy skips disk, manual inspection, retirement, incomplete inventory, city guard and idempotence");
class Probe : BridgeStartupAssetSystem
{
    public void Create() => OnCreate();
    public void Complete() => OnGameLoadingComplete(default, GameMode.MainMenu);
    public void Tick() { if (Enabled) OnUpdate(); }
}
