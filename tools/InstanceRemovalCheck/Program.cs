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

var modSource = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../../src/BridgeBuilder/Mod.cs")));
Check(!modSource.Contains("BridgeMissingAssetSystem"), "no automatic memory scan on save loading");
