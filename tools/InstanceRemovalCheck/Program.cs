using BridgeBuilder.Runtime;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;

var manager = new EntityManager();
var upper = manager.Create(new PrefabData());
var lower = manager.Create(new PrefabData());
var foreign = manager.Create(new PrefabData());
var roots = new HashSet<Entity> { upper, lower };
PrefabRef Reference(Entity prefab) => new() { m_Prefab = prefab };
Entity NodeFor(Entity prefab) => manager.Create(new Node(), Reference(prefab));
Entity EdgeFor(Entity prefab, Entity from, Entity to)
{
    var edge = manager.Create(Reference(prefab), new Edge { m_Start = from, m_End = to });
    foreach (var node in new[] { from, to })
    {
        if (!manager.HasBuffer<ConnectedEdge>(node)) manager.Set(node, new List<ConnectedEdge>());
        manager.GetBuffer<ConnectedEdge>(node).Add(new ConnectedEdge { m_Edge = edge });
    }
    return edge;
}
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS: " + name);
}

var first = NodeFor(upper);
var joint = NodeFor(upper);
var otherEnd = NodeFor(foreign);
var upperEdge = EdgeFor(upper, first, joint);
var otherEdge = EdgeFor(foreign, joint, otherEnd);
var lowerStart = NodeFor(lower);
var lowerEnd = NodeFor(lower);
var lowerEdge = EdgeFor(lower, lowerStart, lowerEnd);
var composition = manager.Create(Reference(upper), new NetCompositionData());
var prefabLike = manager.Create(Reference(upper), new PrefabData(), new Node());
var unrelatedReference = manager.Create(Reference(upper));
var tempNode = manager.Create(Reference(upper), new Node(), new Temp());
var tempEdge = manager.Create(Reference(upper), new Edge { m_Start = tempNode, m_End = tempNode }, new Temp());
var plan = BridgeInstanceRemoval.Collect(manager, roots);
Check(plan.DeletedEntities.SetEquals([upperEdge, lowerEdge, first, lowerStart, lowerEnd]),
    "delete only main/auxiliary placed edges and orphan endpoints");
Check(plan.UpdatedEntities.SetEquals([joint, otherEdge]), "preserve and update shared junction and foreign road");
plan.Apply(manager);
Check(new[] { composition, prefabLike, unrelatedReference, tempNode, tempEdge, upper, lower, foreign }
    .All(e => !manager.HasComponent<Deleted>(e)), "never delete composition/prefab/tool cache entities with identical PrefabRef");
Check(manager.HasComponent<Updated>(joint) && manager.HasComponent<Updated>(otherEdge), "refresh surviving topology");
manager.RemoveComponent<Updated>(joint);
manager.RemoveComponent<Updated>(otherEdge);
plan.Apply(manager);
Check(!manager.HasComponent<Updated>(joint) && !manager.HasComponent<Updated>(otherEdge),
    "waiting does not requeue surviving roads after native cleanup consumes Updated");
Check(!plan.IsComplete(manager), "Deleted is not destruction: wait for native cleanup");
Check(plan.RemainingEntityCount(manager) == 5, "progress counts remaining entities instead of historical total");
foreach (var entity in plan.DeletedEntities) manager.Destroy(entity);
Check(plan.IsComplete(manager), "finish only after scheduled native entity cleanup");
Check(manager.GetComponentData<PrefabRef>(joint).m_Prefab == foreign,
    "shared junction adopts an actual initialized surviving road prefab");
Check(!BridgeInstanceRemoval.HasPlacedReferences(manager, roots), "composition and temporary references do not count as placed roads");
var empty = BridgeInstanceRemoval.Collect(manager, roots);
Check(empty.DeletedEntities.Count == 0 && empty.IsComplete(manager), "repeat deletion is harmless");

// The subnetwork junction is discovered only during owned-element expansion, not Collect.
var parent = manager.Create(new Applied(), new Created(), new Updated());
var nestedJoint = NodeFor(foreign);
manager.Set(nestedJoint, new Owner { m_Owner = parent });
var nestedEnd = NodeFor(foreign);
var ownedEdge = EdgeFor(foreign, nestedEnd, nestedJoint);
manager.Set(ownedEdge, new Owner { m_Owner = parent });
manager.Set(nestedEnd, new Owner { m_Owner = parent });
var external = EdgeFor(foreign, nestedJoint, otherEnd);
var lane = manager.Create(new Owner { m_Owner = nestedJoint });
var lamp = manager.Create(new Owner { m_Owner = nestedJoint });
var effect = manager.Create(new Owner { m_Owner = lamp });
manager.Set(nestedJoint, new List<SubLane> { new() { m_SubLane = lane } });
manager.Set(nestedJoint, new List<Game.Objects.SubObject> { new() { m_SubObject = lamp } });
var foreignChild = manager.Create(new Owner { m_Owner = otherEnd });
var secondaryLane = manager.Create(new Owner { m_Owner = parent }, new SecondaryLane());
var secondaryObject = manager.Create(new Owner { m_Owner = parent }, new Game.Objects.Secondary());
manager.Set(parent, new List<SubLane> { new() { m_SubLane = secondaryLane } });
manager.Set(parent, new List<Game.Objects.SubObject> { new() { m_SubObject = foreignChild } });
manager.GetBuffer<Game.Objects.SubObject>(parent).Add(new() { m_SubObject = secondaryObject });
// The rescued node also owns an edge to a second candidate node. Its protection must propagate.
var secondJoint = NodeFor(foreign);
manager.Set(secondJoint, new Owner { m_Owner = parent });
var rescuedEdge = EdgeFor(foreign, nestedJoint, secondJoint);
manager.Set(rescuedEdge, new Owner { m_Owner = nestedJoint });
var secondLamp = manager.Create(new Owner { m_Owner = secondJoint });
var nested = new BridgeInstanceRemoval();
nested.DeletedEntities.Add(secondJoint);
nested.DeletedEntities.Add(parent);
nested.IncludeOwnedEntities(manager);
Check(nested.DeletedEntities.SetEquals([parent, ownedEdge, nestedEnd]),
    "shared subnetwork junction protects lanes, lamps, effects and dependent junctions");
Check(manager.HasComponent<Owner>(nestedJoint) && manager.HasComponent<Owner>(secondJoint),
    "planning does not mutate surviving junction ownership");
nested.IncludeOwnedEntities(manager);
Check(nested.DeletedEntities.SetEquals([parent, ownedEdge, nestedEnd]), "repeated closure keeps rescued subtrees safe");
nested.Apply(manager);
Check(!manager.HasComponent<Owner>(nestedJoint) && !manager.HasComponent<Owner>(secondJoint),
    "validated application detaches surviving junctions from deleted owner");
Check(new[] { nestedJoint, lane, lamp, effect, external, foreignChild, secondJoint, rescuedEdge, secondLamp }
    .All(e => !manager.HasComponent<Deleted>(e)), "no surviving or foreign subtree is marked Deleted");
Check(!manager.HasComponent<Applied>(parent) && !manager.HasComponent<Created>(parent)
    && !manager.HasComponent<Updated>(parent), "native deletion flags exclude concurrent geometry updates");
Check(!nested.DeletedEntities.Contains(secondaryLane) && !nested.DeletedEntities.Contains(secondaryObject)
    && !manager.HasComponent<Deleted>(secondaryLane) && !manager.HasComponent<Deleted>(secondaryObject),
    "secondary lane/object retirement belongs to native reference systems, not recursive deletion");

var staleNode = NodeFor(upper);
manager.Set(staleNode, new List<ConnectedEdge> { new() { m_Edge = otherEdge } });
var stale = BridgeInstanceRemoval.Collect(manager, roots);
Check(stale.DeletedEntities.Contains(staleNode), "stale ConnectedEdge entry cannot retain an unrelated junction");

// Player city: the bridge and its neighbouring highway are both missing. Protecting the
// shared node without transferring PrefabRef previously caused an endless completion wait.
var missingRoad = manager.Create(new PrefabData { m_Index = -1 });
var blockedJoint = NodeFor(upper);
var blockedStart = NodeFor(upper);
var blockedEnd = NodeFor(missingRoad);
var blockedBridge = EdgeFor(upper, blockedStart, blockedJoint);
var missingRoadEdge = EdgeFor(missingRoad, blockedJoint, blockedEnd);
manager.Set(blockedJoint, new Owner { m_Owner = blockedBridge });
var blockedPlan = BridgeInstanceRemoval.Collect(manager, roots);
blockedPlan.IncludeOwnedEntities(manager);
Check(!blockedPlan.CanApply(manager, out var blockedReason) && blockedReason.Contains("dependencies"),
    "missing neighbouring highway is reported before starting destructive cleanup");
blockedPlan.Apply(manager);
Check(!manager.HasComponent<Deleted>(blockedBridge) && !manager.HasComponent<Deleted>(blockedStart)
    && !manager.HasComponent<Updated>(missingRoadEdge) && manager.HasComponent<Owner>(blockedJoint),
    "blocked plan leaves bridge, foreign road and shared junction untouched");
Check(BridgeInstanceRemoval.CountPlacedReferences(manager, roots) > 0,
    "protected references are reported independently of pending destruction");
manager.SetComponentData(missingRoad, new PrefabData { m_Index = 3 });
var recoveredPlan = BridgeInstanceRemoval.Collect(manager, roots);
recoveredPlan.IncludeOwnedEntities(manager);
Check(recoveredPlan.CanApply(manager, out _), "restored neighbouring dependency makes a fresh plan safe");
recoveredPlan.Apply(manager);
Check(manager.GetComponentData<PrefabRef>(blockedJoint).m_Prefab == missingRoad
    && !manager.HasComponent<Deleted>(missingRoadEdge), "repaired dependency receives junction without deleting the neighbour");
manager.SetComponentData(missingRoad, new PrefabData { m_Index = -1 });
var additionalNode = NodeFor(upper);
var additionalStart = NodeFor(upper);
EdgeFor(upper, additionalNode, additionalStart);
EdgeFor(missingRoad, additionalNode, blockedEnd);
EdgeFor(foreign, additionalNode, otherEnd);
var mixedPlan = BridgeInstanceRemoval.Collect(manager, roots);
Check(mixedPlan.CanApply(manager, out _), "one initialized connected neighbour is sufficient even when another is missing");

var confirmation = new BridgeCleanupConfirmation();
var failures = new Dictionary<string, string> { ["bridge"] = "missing section" };
Check(confirmation.Observe(failures, 1).Count == 0, "first failure does not authorize deletion");
Check(confirmation.Observe(failures, 1).Count == 0, "same-frame reentry does not authorize deletion");
Check(confirmation.Observe(failures, 2).Contains("bridge"), "persistent current evidence confirms on later frame");
confirmation.Observe(new Dictionary<string, string>(), 3);
Check(confirmation.Observe(failures, 4).Count == 0, "recovered failure does not survive in historical evidence");
failures["bridge"] = "different failure";
Check(confirmation.Observe(failures, 5).Count == 0, "changed evidence must be reconfirmed");
confirmation.Clear();
Check(confirmation.Observe(failures, 6).Count == 0, "map transition clears cleanup evidence");

var modSource = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../../src/BridgeBuilder/Mod.cs")));
Check(modSource.Contains("UpdateBefore<BridgeMissingAssetSystem, Game.Objects.SubElementDeleteSystem>(")
    && modSource.Contains("SystemUpdatePhase.PostTool);")
    && !modSource.Contains("UpdateAt<BridgeMissingAssetSystem>(SystemUpdatePhase.UIUpdate)"),
    "automatic cleanup is scheduled before native sub-element deletion, never after render preparation");

var systemsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../../src/BridgeBuilder/Systems"));
var saveCleanup = File.ReadAllText(Path.Combine(systemsPath, "BridgeMissingAssetSystem.cs"));
var titleCleanup = File.ReadAllText(Path.Combine(systemsPath, "BridgeStartupAssetSystem.cs"));
Check(!saveCleanup.Contains("BridgeDiskAudit.Read") && !saveCleanup.Contains("RemoveInvalidBridge")
    && !saveCleanup.Contains("PrepareLegacy") && !saveCleanup.Contains(".AddPrefab("),
    "save cleanup does not audit, repair, register or retire prefab files");
Check(titleCleanup.Contains("GameMode.MainMenu") && titleCleanup.Contains("RetireInvalidBridgeFilesAtTitle")
    && !titleCleanup.Contains("BridgeInstanceRemoval.Collect")
    && modSource.Contains("UpdateAt<BridgeStartupAssetSystem>(SystemUpdatePhase.UIUpdate)"),
    "title-screen recovery is scheduled separately and does not delete city entities");
Check(saveCleanup.Contains("BridgeStartupRecovery.Retired.Contains")
    && saveCleanup.Contains("GetObsoleteID"),
    "save cleanup recognizes both cached retired identities and obsolete saved identities");
Check(saveCleanup.Contains("if (_loadPlanned) return;") && saveCleanup.Contains("_loadPlanned = true;")
    && !saveCleanup.Contains("BridgeLoadFailures.Revision"), "one scan per load; later failure revisions cannot restart cleanup");
Check(saveCleanup.Contains("StopBlocked(blocked)") && !saveCleanup.Contains("Defer(")
    && !saveCleanup.Contains("_retryAt"), "blocked cleanup ends without any automatic retry");
var applyBlock = saveCleanup.Split("if (!_applied)", StringSplitOptions.None)[1]
    .Split("// Read-only completion observation", StringSplitOptions.None)[0];
Check(applyBlock.Contains("_removal.Apply(EntityManager)") && applyBlock.Contains("_applied = true;")
    && saveCleanup.Split("_removal.Apply(EntityManager)", StringSplitOptions.None).Length == 2,
    "native deletion is submitted once, later updates only observe completion");
Check(saveCleanup.Contains("_finished = true;") && saveCleanup.Contains("if (_finished)")
    && saveCleanup.Contains("_finished = false;"), "terminal attempt stays ended until next map preload");
Check(!modSource.Contains("BridgeSaveGuard") && !saveCleanup.Contains("BridgeSaveGuard")
    && !File.Exists(Path.Combine(systemsPath, "../Runtime/BridgeSaveGuard.cs")),
    "no save interception remains, including incomplete or failed cleanup");
Check(saveCleanup.Contains("retiredPrefabs.TryGetValue(prefab"),
    "retired identities still require actual placed networks");
Check(saveCleanup.Contains("override void OnGameLoaded(Context serializationContext)")
    && !saveCleanup.Contains("BridgeRuntimeRequests.CatalogLoading")
    && !saveCleanup.Contains("_confirmation") && !saveCleanup.Contains("_completedFrame"),
    "save loading plans cleanup without UI-catalog or redundant confirmation-frame delays");
var scanSource = saveCleanup.Split("private void Scan()", StringSplitOptions.None)[1]
    .Split("private void Defer", StringSplitOptions.None)[0];
Check(!scanSource.Contains(".Apply(") && !scanSource.Contains("SuspendForCleanup")
    && saveCleanup.Contains("_removal.Apply(EntityManager)"),
    "deserialization planning is read-only; mutation remains in the scheduled PostTool update");
Check(!saveCleanup.Contains("Mod.Log.Critical") && !saveCleanup.Contains("new MessageDialog")
    && saveCleanup.Contains("Notice(\"MissingBridgesBlocked\")")
    && saveCleanup.Contains("Notice(\"MissingBridgesTimeout\", remaining, references)"),
    "controlled cleanup failures use localized notices rather than English error popups");
