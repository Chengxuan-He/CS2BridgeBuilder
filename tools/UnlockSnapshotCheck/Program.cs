using BridgeBuilder.Runtime;
using Game.Prefabs;
using Unity.Entities;

var count = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; Console.WriteLine("PASS " + label); }
var manager = new EntityManager();
var prefabs = new PrefabSystem();
Entity Add(int id, PrefabBase p, bool locked, params UnlockRequirement[] requirements)
{
    var e = new Entity(id); prefabs.All[e] = p; manager.All[e] = new() { Locked = locked, Requirements = requirements }; return e;
}
UnlockRequirement Edge(Entity e, int flags = 1) => new() { m_Prefab = e, m_Flags = (UnlockFlags)flags };
var milestone = Add(1, new PrefabBase { name = "Milestone" }, true, Edge(new(1)));
var devA = Add(2, new PrefabBase { name = "DevA" }, true, Edge(new(2)));
var devB = Add(3, new PrefabBase { name = "DevB" }, false, Edge(new(3)));
var intermediate = Add(4, new NetGeometryPrefab { name = "OtherExternalNetwork" }, false, Edge(devA, 2), Edge(devB, 2));
var donor = new NetGeometryPrefab { name = "ExternalBridgeDonor" };
var root = Add(5, donor, false, Edge(milestone), Edge(intermediate));
Check(BridgeUnlockSnapshot.Capture(donor, prefabs, manager, out var rule), "capture original native AND/OR graph");
bool? Eval() => rule.Evaluate(id => BridgeUnlockSnapshot.IsUnlocked(id, prefabs, manager));
Check(Eval() == false, "do not copy donor's current unlocked state");
manager.All[milestone].Locked = false;
Check(Eval() == true, "milestone and alternative development unlock");
manager.All[devB].Locked = true;
Check(Eval() == false, "both OR alternatives locked");
manager.All[devA].Locked = false;
Check(Eval() == true, "second OR alternative unlock");
prefabs.All.Remove(intermediate); prefabs.All.Remove(root);
Check(Eval() == true, "snapshot independent after deleting donor and intermediate network");
Check(!System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(rule.Encode().Substring(BridgeUnlockExpression.Prefix.Length))).Contains("External"), "no external network identity retained");
prefabs.All.Remove(milestone);
Check(Eval() == null, "missing progression leaf defers, not unlocks");
var cyclic = new NetGeometryPrefab { name = "Cycle" };
Add(6, cyclic, true, Edge(new(7))); Add(7, new PrefabBase { name = "Cycle2" }, true, Edge(new(6)));
Check(!BridgeUnlockSnapshot.Capture(cyclic, prefabs, manager, out _), "cycles fail closed");
var manual = new NetGeometryPrefab { name = "ManualNetwork" };
Add(8, manual, true, Edge(new(8)));
Check(!BridgeUnlockSnapshot.Capture(manual, prefabs, manager, out _), "unsupported manually unlocked network not retained as a leaf");
var early = new NetGeometryPrefab { name = "NotInitialized" };
var e9 = Add(9, early, true); manager.All[e9].Created = true;
Check(!BridgeUnlockSnapshot.Capture(early, prefabs, manager, out _), "wait for native initialization");
manager.All[e9].Created = false; manager.All[e9].Requirements = null;
Check(!BridgeUnlockSnapshot.Capture(early, prefabs, manager, out _), "missing native buffer fails closed");
manager.All[e9].HasLocked = false;
Check(BridgeUnlockSnapshot.Capture(early, prefabs, manager, out var free) && free.Evaluate(_ => null) == true, "native non-unlockable dependency");
var loaded = new NetGeometryPrefab { name = "Legacy" };
loaded.AddComponent<ManualUnlockable>().name = new BridgeUnlockExpression { Kind = 2, Identity = "CID:" + new string('a', 32) }.Encode();
Check(!BridgeUnlockSnapshot.Read(loaded, prefabs, manager, out _), "unavailable deferred prototype is not eagerly loaded");
var uuid = "b7dff25d3-cf93-4466-9c26-d5aa3be39f3e";
var directory = Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData", uuid);
Directory.CreateDirectory(directory);
var path = Path.Combine(directory, uuid + ".Prefab");
var gateText = new BridgeUnlockExpression { Kind = 2, Identity = "CID:" + new string('a', 32) }.Encode();
var migrated = "{\n    \"name\": \"" + uuid + "\",\n\"components\": ["
    + "{\"$type\":\"1|Game.Prefabs.ManualUnlockable, Game\",\"name\":\"" + gateText + "\",\"active\":true},"
    + "{\"$type\":\"2|BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder\"}]}";
File.WriteAllText(path, migrated);
var broken = new NetGeometryPrefab { name = uuid, asset = new() { path = path } };
var legacy = broken.AddComponent<Unlockable>(); legacy.m_IgnoreDependencies = true;
legacy.m_RequireAll = new PrefabBase[] { null! }; legacy.m_RequireAny = Array.Empty<PrefabBase>();
Check(BridgeUnlockSnapshot.PrepareLegacy(broken), "repair already-deserialized null unlock from migrated disk rule");
Check(broken.GetComponent<Unlockable>() == null && broken.GetComponent<ManualUnlockable>()?.name == gateText,
    "replace only legacy gate, retain its recorded rule");
Check(!BridgeLoadFailures.Restart, "successful in-memory migration does not require restart");
Check(BridgeUnlockSnapshot.PrepareLegacy(broken), "in-memory repair idempotent");
Check(File.ReadAllText(path) == migrated, "already migrated file unchanged");
var missing = new NetGeometryPrefab { name = uuid + "_Lower", asset = new() };
missing.AddComponent<Unlockable>().m_RequireAll = new PrefabBase[] { null! };
Check(!BridgeUnlockSnapshot.PrepareLegacy(missing) && BridgeLoadFailures.Restart,
    "missing recorded identity defers instead of deleting or unlocking");
Check(missing.GetComponent<Unlockable>() != null, "failed repair leaves original component intact");
Check(!BridgeUnlockMigration.RecoverGate(UnityEngine.Application.persistentDataPath, "../../foreign", out _, out _),
    "reject foreign name/path before reading");
Check(!BridgeUnlockMigration.ReadGate(migrated.Replace("\"active\":true", "\"active\":false"), out _),
    "inactive serialized gate is not activated implicitly");
BridgeLoadFailures.Restart = false;
using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    BridgeStartupRecovery.Start();
    Check(BridgePrefabLoadGuard.Installed, "registration protection installed before disk migration");
    Check(!BridgeLoadFailures.Restart, "early file lock allows OnLoad retry without latching failure");
}
BridgeStartupRecovery.Start(finalAttempt: true);
Check(!BridgeLoadFailures.Restart, "OnLoad retry completes without mandatory restart");
BridgeStartupRecovery.Retired.Add(uuid);
BridgeStartupRecovery.Start(finalAttempt: true);
Check(BridgeStartupRecovery.Retired.Contains(uuid), "retired identity survives repeated startup calls in one session");
BridgeStartupRecovery.Stop();
Check(BridgeStartupRecovery.Retired.Count == 0, "mod disposal clears only session retirement identities");
using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    BridgeStartupRecovery.Start(finalAttempt: true);
    Check(BridgeLoadFailures.Restart && File.Exists(path), "unresolved access failure retains assets and suspends deletion");
}
Console.WriteLine($"{count} snapshot/recovery checks passed using fake native buffers; game startup acceptance still required.");
