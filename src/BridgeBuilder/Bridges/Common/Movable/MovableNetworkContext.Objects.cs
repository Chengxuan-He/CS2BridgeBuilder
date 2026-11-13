using System;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkContext
{
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
