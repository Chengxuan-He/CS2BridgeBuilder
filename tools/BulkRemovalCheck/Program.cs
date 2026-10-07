using BridgeBuilder;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using BridgeBuilder.Systems;
using Game.SceneFlow;

var sandbox = Path.Combine(Path.GetTempPath(), "BBBulk-" + Guid.NewGuid().ToString("N"));
var game = Path.Combine(sandbox, "game");
UnityEngine.Application.persistentDataPath = game;
BridgeRecoveryLocation.Path = Path.Combine(sandbox, "backup");
var owner = "b11111111-1111-1111-1111-111111111111";
var second = "b22222222-2222-2222-2222-222222222222";
var imported = Path.Combine(game, "ImportedData");
var source = Path.Combine(imported, "prefix-" + owner + "-suffix");
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
try
{
    Directory.CreateDirectory(Path.Combine(source, "nested", "empty"));
    File.WriteAllText(Path.Combine(source, "unreadable.Prefab"), "no valid metadata or CID");
    var dependencies = Path.Combine(imported, second + "_Dependencies");
    Directory.CreateDirectory(dependencies); File.WriteAllText(Path.Combine(dependencies, "external.Material"), "unchanged");
    var geometry = Path.Combine(game, "BridgeBuilder"); Directory.CreateDirectory(geometry);
    File.WriteAllText(Path.Combine(geometry, owner + ".Geometry"), "mesh");
    File.WriteAllText(Path.Combine(geometry, "shared.Geometry"), "shared");
    Directory.CreateDirectory(Path.Combine(imported, "OtherMod"));
    var ui = GameManager.instance.userInterface!.appBindings!;
    BridgeStartupAssetSystem.CanCheck = false;
    BridgeBulkRemoval.RequestConfirmation();
    Check(ui.Calls == 0 && Directory.Exists(source), "loading/city gate blocks operation");
    BridgeStartupAssetSystem.CanCheck = true; BridgeBulkRemoval.RequestConfirmation();
    Check(ui.Calls == 1 && Directory.Exists(source) && !Directory.Exists(BridgeRecoveryLocation.Path), "button opens confirmation without file IO");
    BridgeBulkRemoval.RequestConfirmation(); Check(ui.Calls == 1, "repeated click cannot open duplicate confirmation");
    var cancelled = ui.Reply!; cancelled(1);
    Check(Directory.Exists(source) && BridgeBulkRemoval.CanRequest, "cancel changes no assets");
    BridgeBulkRemoval.RequestConfirmation(); ui.Reply!(-1);
    Check(Directory.Exists(source), "closing dialog changes no assets");
    BridgeBulkRemoval.RequestConfirmation(); var ready = ui.Reply!;
    cancelled(0); Check(Directory.Exists(source) && !BridgeBulkRemoval.CanRequest, "stale callback cannot confirm a new dialog");
    BridgeStartupAssetSystem.CanCheck = false; ready(0);
    Check(Directory.Exists(source), "leaving ready main menu invalidates confirmation");
    BridgeStartupAssetSystem.CanCheck = true;
    ui.Fail = true; BridgeBulkRemoval.RequestConfirmation(); ui.Fail = false;
    Check(BridgeBulkRemoval.CanRequest && Directory.Exists(source), "UI failure never removes files");
    BridgeBulkRemoval.RequestConfirmation(); var disposed = ui.Reply!; BridgeBulkRemoval.Stop();
    disposed(0); Check(Directory.Exists(source), "mod disposal invalidates pending confirmation");
    BridgeBulkRemoval.RequestConfirmation(); var confirmed = ui.Reply!; confirmed(0);
    Check(!Directory.Exists(source) && !Directory.Exists(dependencies) && !File.Exists(Path.Combine(geometry, owner + ".Geometry")), "confirmed removal covers all UUID-owned assets and dependency directories");
    Check(Directory.Exists(Path.Combine(imported, "OtherMod")) && File.ReadAllText(Path.Combine(geometry, "shared.Geometry")) == "shared", "unrelated assets and shared resources preserved");
    var backup = Directory.GetDirectories(BridgeRecoveryLocation.Path).Single();
    Check(File.ReadAllText(Path.Combine(backup, Path.GetFileName(source), "unreadable.Prefab")) == "no valid metadata or CID"
        && Directory.Exists(Path.Combine(backup, Path.GetFileName(source), "nested", "empty")), "whole directory backed up without metadata checks");
    Check(BridgeSessionState.Restart && BridgeStartupRecovery.Retired.SetEquals(new[] {owner, second})
        && Mod.Messages.Last() == "RemoveAllBridgesDone", "restart and stale-cache exclusion after removal");
    var messages = Mod.Messages.Count; confirmed(0);
    Check(Mod.Messages.Count == messages, "confirmation is single-use");
    BridgeSessionState.Restart = false; BridgeBulkRemoval.RequestConfirmation(); ui.Reply!(0);
    Check(Mod.Messages.Last() == "RemoveAllBridgesEmpty" && !BridgeSessionState.Restart, "empty asset set produces no restart or removal");
    Directory.CreateDirectory(source); File.WriteAllText(Path.Combine(source, "kept"), "keep");
    BridgeRecoveryLocation.Path = Path.Combine(sandbox, "blocked"); File.WriteAllText(BridgeRecoveryLocation.Path, "file");
    BridgeBulkRemoval.RequestConfirmation(); ui.Reply!(0);
    Check(Directory.Exists(source) && Mod.Log.Criticals == 1 && Mod.Messages.Last() == "RemoveAllBridgesFailed", "move failure reports CRITICAL and never claims completion");
}
finally
{
    var full = Path.GetFullPath(sandbox);
    if (full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(full).StartsWith("BBBulk-")) Directory.Delete(full, true);
}
