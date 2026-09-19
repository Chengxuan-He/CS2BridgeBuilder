using BridgeBuilder.Runtime;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;

static void Require(bool value, string message)
{
    if (!value) throw new Exception(message);
}
static ObjectPrefab Asset(string name, params PrefabBase[] dependencies) =>
    new() { name = name, References = dependencies };
static ObjectPrefab Replacement(string name, ObjectPrefab placeholder, PrefabBase mesh)
{
    var value = Asset(name, mesh);
    value.components.Add(new SpawnableObject { m_Placeholders = [placeholder] });
    return value;
}
static IReadOnlyList<PrefabBase> Remove(ObjectPrefab root, PrefabBase[] all, bool dependencies = true) =>
    BridgePrefabRemoval.Remove([root], all, p => p.name.StartsWith("owned-"), dependencies, new ExportReport());

// Reproduces GoldenGate: road -> placeholder; base/pillar -> placeholder, never the reverse.
{
    var mesh = Asset("owned-mesh");
    var placeholder = Asset("owned-placeholder", mesh);
    var road = Asset("road", placeholder);
    var baseMesh = Asset("owned-base-mesh");
    var footing = Replacement("owned-footing", placeholder, baseMesh);
    var lightMesh = Asset("owned-light-mesh");
    var lit = Replacement("owned-lit-pillar", placeholder, lightMesh);
    PrefabBase[] all = [road, placeholder, mesh, baseMesh, footing, lightMesh, lit];
    placeholder.SavedAsset.BeforeDelete = () => Require(road.SavedAsset.Deleted && footing.SavedAsset.Deleted
        && lit.SavedAsset.Deleted, "placeholder deleted before its referrers");
    baseMesh.SavedAsset.BeforeDelete = () => Require(footing.SavedAsset.Deleted, "mesh deleted before its object");
    Require(Remove(road, all).Count == all.Length, "reverse replacements/meshes were orphaned");
}
// A failed replacement deletion must retain its placeholder and every downstream mesh.
{
    var mesh = Asset("owned-mesh");
    var placeholder = Asset("owned-placeholder", mesh);
    var road = Asset("road", placeholder);
    var footing = Replacement("owned-footing", placeholder, mesh);
    footing.asset!.Fail = true;
    var deleted = Remove(road, [road, placeholder, mesh, footing]);
    Require(deleted.Count == 1 && deleted.Contains(road), "failure cut off a surviving reference");
}
// Protect another bridge even if it is NOT in the exporter registry/state.
{
    var mesh = Asset("owned-shared-mesh");
    var placeholder = Asset("owned-placeholder", mesh);
    var road = Asset("road", placeholder);
    var other = Asset("other-unregistered-road", mesh);
    var deleted = Remove(road, [road, placeholder, mesh, other]);
    Require(deleted.Count == 2 && !mesh.SavedAsset.Deleted && !other.SavedAsset.Deleted, "shared asset removed");
}
// A replacement naming two bridges is not exclusively owned by the removed bridge.
{
    var placeholder = Asset("owned-placeholder");
    var other = Asset("other-placeholder");
    var road = Asset("road", placeholder);
    var shared = Asset("owned-shared-replacement");
    shared.components.Add(new SpawnableObject { m_Placeholders = [placeholder, other] });
    Require(Remove(road, [road, placeholder, other, shared]).Count == 1,
        "multi-placeholder replacement should protect both placeholders");
}
// Retain foreign/source user assets even when they have no other current referrer.
{
    var source = Asset("source-road-dependency");
    var road = Asset("road", source);
    Require(Remove(road, [road, source]).Count == 1 && !source.asset!.Deleted, "source asset removed");
}
// Explicit no-dependency-cleanup preference keeps a valid whole dependency subtree.
{
    var placeholder = Asset("owned-placeholder");
    var road = Asset("road", placeholder);
    var footing = Replacement("owned-footing", placeholder, Asset("owned-mesh"));
    Require(Remove(road, [road, placeholder, footing], false).Count == 1
        && !placeholder.asset!.Deleted && !footing.asset!.Deleted, "cleanup-disabled preference ignored");
}
// External reference to the requested root refuses deletion BEFORE mutating anything.
{
    var road = Asset("road", Asset("owned-mesh"));
    var other = Asset("foreign", road);
    Require(Remove(road, [road, other]).Count == 0 && !road.asset!.Deleted, "referenced root removed");
}
// Cycles remain intact rather than depending on dictionary order to delete part of a cycle.
{
    var a = Asset("owned-a");
    var b = Asset("owned-b", a);
    a.References = [b];
    var road = Asset("road", a);
    Require(Remove(road, [road, a, b]).Count == 1 && !a.asset!.Deleted && !b.asset!.Deleted,
        "cycle was partially deleted");
}
// Read-only and built-in assets cannot be deletion candidates.
{
    var original = Asset("owned-builtin");
    original.isBuiltin = true;
    var readOnly = Asset("owned-readonly");
    readOnly.isReadOnly = true;
    var road = Asset("road", original, readOnly);
    Require(Remove(road, [road, original, readOnly]).Count == 1, "archetype asset removed");
}
// Keeping a shared placeholder must ALSO keep its reverse-linked replacement, not just the mesh.
{
    var mesh = Asset("owned-mesh");
    var placeholder = Asset("owned-placeholder", mesh);
    var road = Asset("road", placeholder);
    var footing = Replacement("owned-footing", placeholder, mesh);
    var other = Asset("other-road", placeholder);
    Require(Remove(road, [road, placeholder, mesh, footing, other]).Count == 1
        && !footing.asset!.Deleted, "shared placeholder lost its replacement");
}
// Failure to delete the road itself must not strip off its base/replacement objects.
{
    var mesh = Asset("owned-mesh");
    var placeholder = Asset("owned-placeholder", mesh);
    var road = Asset("road", placeholder);
    var footing = Replacement("owned-footing", placeholder, mesh);
    road.asset!.Fail = true;
    Require(Remove(road, [road, placeholder, mesh, footing]).Count == 0,
        "failed root deletion stripped its replacements");
}
// A live registered prefab must not retain an asset handle whose SourceMeta was deleted.
{
    var road = Asset("road");
    road.SavedAsset.BeforeDelete = () => Require(road.asset == null, "delete event saw stale metadata handle");
    Require(Remove(road, [road]).Count == 1 && road.asset == null, "deleted asset handle retained");
}
// A failed disk operation must restore the handle and leave its dependencies intact.
{
    var road = Asset("road");
    road.SavedAsset.Fail = true;
    Require(Remove(road, [road]).Count == 0 && ReferenceEquals(road.asset, road.SavedAsset),
        "failed deletion lost its asset handle");
}
Console.WriteLine("PASS: 13 asset-reference deletion regressions (no geometry tests).");
