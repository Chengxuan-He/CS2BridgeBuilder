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
var portable = new NetGeometryPrefab { name = "b12345678-1234-1234-1234-123456789abc" };
portable.AddComponent<ManualUnlockable>();
var lower = new NetGeometryPrefab { name = portable.name + "_Lower" };
lower.AddComponent<ManualUnlockable>();
portable.AddComponent<AuxiliaryNets>().m_AuxiliaryNets = new[] { new AuxiliaryNetInfo { m_Prefab = lower } };
var nativeGroups = new List<PrefabBase>();
var untouchedLower = lower.AddComponent<Unlockable>();
untouchedLower.m_IgnoreDependencies = true;
var isolated = new List<PrefabBase>();
Check(BridgeNativeUnlock.Apply(portable, rule, prefabs, isolated, out var isolatedError, includeAuxiliary: false), isolatedError);
Check(ReferenceEquals(lower.GetComponent<Unlockable>(), untouchedLower), "individual migration preserves sibling unlock component");
Check(BridgeNativeUnlock.Apply(portable, rule, prefabs, nativeGroups, out var nativeError), nativeError);
bool Native(PrefabBase p)
{
    if (p.GetComponent<Unlockable>() is not { } gate)
        return prefabs.TryGetEntity(p, out var e) && !manager.All[e].Locked;
    return gate.m_RequireAll!.All(Native) && (gate.m_RequireAny!.Length == 0 || gate.m_RequireAny.Any(Native));
}
for (var bits = 0; bits < 8; bits++)
{
    manager.All[milestone].Locked = (bits & 1) != 0;
    manager.All[devA].Locked = (bits & 2) != 0;
    manager.All[devB].Locked = (bits & 4) != 0;
    Check(Native(portable) == Eval() && Native(lower) == Eval(), "native-only AND/OR truth table " + bits);
    Check(BridgeNativeUnlock.Evaluate(portable, prefabs, manager) == Eval()
        && BridgeNativeUnlock.Evaluate(lower, prefabs, manager) == Eval(),
        "immediate build evaluates persisted conditions before new group state settles " + bits);
}
Check(portable.GetComponent<ManualUnlockable>() == null && lower.GetComponent<ManualUnlockable>() == null
    && nativeGroups.All(g => g.GetType() == typeof(AssetPackPrefab) && g.name.Contains(portable.name)),
    "native gates remove manual manager dependency; private groups retain bridge UUID");
prefabs.All.Remove(milestone);
Check(Eval() == null, "missing progression leaf defers, not unlocks");
Check(BridgeNativeUnlock.Evaluate(portable, prefabs, manager) == null, "missing native leaf is not reported as locked");
var nativeCycle = new NetGeometryPrefab();
var cycleGate = nativeCycle.AddComponent<Unlockable>();
cycleGate.m_IgnoreDependencies = true;
cycleGate.m_RequireAll = new[] { nativeCycle };
Check(BridgeNativeUnlock.Evaluate(nativeCycle, prefabs, manager) == null, "cyclic native gates defer safely");
var emptyGate = new NetGeometryPrefab();
emptyGate.AddComponent<Unlockable>().m_IgnoreDependencies = true;
Check(BridgeNativeUnlock.Evaluate(emptyGate, prefabs, manager) == true, "empty native gate needs no unlock tick");
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
BridgeSessionState.Restart = false;
using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    BridgeStartupRecovery.Start();
    Check(!BridgeSessionState.Restart, "early file lock allows OnLoad retry without latching failure");
}
BridgeStartupRecovery.Start(finalAttempt: true);
Check(!BridgeSessionState.Restart, "OnLoad retry completes without mandatory restart");
BridgeStartupRecovery.Retired.Add(uuid);
BridgeStartupRecovery.Start(finalAttempt: true);
Check(BridgeStartupRecovery.Retired.Contains(uuid), "retired identity survives repeated startup calls in one session");
BridgeStartupRecovery.Stop();
Check(BridgeStartupRecovery.Retired.Count == 0, "mod disposal clears only session retirement identities");
using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    BridgeStartupRecovery.Start(finalAttempt: true);
    Check(BridgeSessionState.Restart && File.Exists(path), "unresolved access failure retains assets and suspends deletion");
}
Console.WriteLine($"{count} snapshot/recovery checks passed using fake native buffers; game startup acceptance still required.");
