using System.Collections.Generic;
using System;
using System.Reflection;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkLanes
{
    // Resolve source edge/node rules, elevation, handedness, upgrades and mechanism flags
    // after all owners, connections and SubObjects exist. Native creation also clears
    // obsolete upgrade states and shares equal compositions inside this private world.
    private void SelectCompositions(bool leftHandTraffic)
    {
        RequirePrivateWorld();
        var record = typeof(CompositionSelectSystem).GetNestedType("CompositionCreateInfo",
            BindingFlags.NonPublic) ?? throw new MissingMemberException(
                typeof(CompositionSelectSystem).FullName, "CompositionCreateInfo");
        typeof(MovableNetworkLanes).GetMethod(nameof(SelectCompositionQueue),
            BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(record)
            .Invoke(this, new object[] { leftHandTraffic });
    }

    private void SelectCompositionQueue<T>(bool leftHandTraffic) where T : unmanaged
    {
        using var queue = new NativeQueue<T>(Allocator.TempJob);
        using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Edge>(),
            ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.ReadWrite<Composition>());
        Run(typeof(CompositionSelectSystem), "SelectCompositionJob", "Execute", query,
            new Dictionary<string, object>
            {
                ["m_LeftHandTraffic"] = leftHandTraffic,
                ["m_CompositionCreateQueue"] = queue.AsParallelWriter()
            });
        Run(typeof(CompositionSelectSystem), "CreateCompositionJob", "Execute", null,
            new Dictionary<string, object> { ["m_CompositionCreateQueue"] = queue });
    }

    private void InitializeCompositions()
    {
        using var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetCompositionData>(),
            ComponentType.ReadWrite<NetCompositionPiece>(), ComponentType.ReadOnly<NetCompositionMeshRef>(),
            ComponentType.ReadOnly<Created>());
        Run(typeof(NetCompositionSystem), "InitializeCompositionJob", "Execute", query,
            new Dictionary<string, object>());
        // Initialization appends objects/areas/crosswalks. A second calculation must not append
        // them again. No live prefab or external-world Created marker is touched.
        EntityManager.RemoveComponent<Created>(query);
    }
}
