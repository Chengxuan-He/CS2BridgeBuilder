using System;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkContext
{
    // Use the same prepared owners as the surface pass. SourceTheme is deliberately a source-
    // world identity: the selector reads original prefab requirements, not remapped requirements.
    internal ObjectSubObjectInfo[] EdgeObjects(int edgeIndex, Entity sourceTheme, float3 origin)
    {
        var manager = World.EntityManager;
        var edge = Edges[edgeIndex];
        var random = manager.GetComponentData<PseudoRandomSeed>(edge)
            .GetRandom(PseudoRandomSeed.kSecondaryObject);
        var composition = manager.GetComponentData<Composition>(edge);
        var network = manager.GetComponentData<NetGeometryData>(
            manager.GetComponentData<PrefabRef>(edge).m_Prefab);
        var elevation = manager.HasComponent<Elevation>(edge)
            ? math.cmin(manager.GetComponentData<Elevation>(edge).m_Elevation) : 0f;
        var placements = MovableRoadObjects.ResolvePlacements(World, edge,
            ObjectRules(composition.m_Edge), network, sourceTheme, _leftHandTraffic,
            elevation, OriginalObject, ref random);
        Localize(placements, origin);
        return placements;
    }

    internal MovableNodeObjects.Result NodeObjects(int nodeIndex, Entity sourceTheme, float3 origin)
    {
        var node = Nodes[nodeIndex];
        var random = World.EntityManager.GetComponentData<PseudoRandomSeed>(node)
            .GetRandom(PseudoRandomSeed.kSecondaryObject);
        var result = World.GetOrCreateSystemManaged<MovableNodeObjects>().Resolve(node,
            ObjectRules, OriginalObject, sourceTheme, _leftHandTraffic, ref random);
        Localize(result.Placements, origin);
        return result;
    }

    private ObjectPrefab OriginalObject(Entity entity)
    {
        var original = Prefabs.Original(entity);
        Prefabs.CheckSource(original);
        return original as ObjectPrefab
            ?? throw new InvalidOperationException("A native road object references a non-object prefab.");
    }

    private static void Localize(ObjectSubObjectInfo[] placements, float3 origin)
    {
        // Resolve traffic needs in prepared-world coordinates first. Only completed placements
        // change frame, exactly as the surface and lane extraction do; preserve rotations and IDs.
        foreach (var placement in placements) placement.m_Position -= origin;
    }
}
