using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Rendering;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkLanes
{
    internal void CalculateOverlaps(bool leftHandTraffic)
    {
        RequirePrivateWorld();
        // A prepared context is evaluated as a whole, matching the native loaded/full queries.
        using var owners = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SubLane>(),
            ComponentType.Exclude<Deleted>());
        using var lanes = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Lane>(),
            ComponentType.Exclude<Deleted>(), ComponentType.Exclude<SecondaryLane>());
        using var entities = owners.ToEntityArray(Allocator.TempJob);
        var capacity = lanes.CalculateEntityCount();
        var allocated = new List<IDisposable>();
        try
        {
            var source = Allocate(nameof(AllocateMap), "LaneSourceData", capacity);
            var target = Allocate(nameof(AllocateMap), "LaneTargetData", capacity);
            var extra = Allocate(nameof(AllocateQueue), "OverlapData", 0);
            var values = new Dictionary<string, object>
            {
                ["m_Entities"] = entities,
                ["m_LeftHandTraffic"] = leftHandTraffic,
                ["m_UpdateAll"] = true,
                ["m_ExtraOverlaps"] = Writer(extra)
            };
            Run(typeof(LaneOverlapSystem), "UpdateLaneOverlapsJob", "Execute", null, values, entities.Length);
            values["m_ExtraOverlaps"] = extra;
            Run(typeof(LaneOverlapSystem), "ApplyExtraOverlapsJob", "Execute", null, values);
            Run(typeof(LaneOverlapSystem), "SortLaneOverlapsJob", "Execute", null, values, entities.Length);
            values["m_SourceMap"] = Writer(source);
            values["m_TargetMap"] = Writer(target);
            Run(typeof(LaneOverlapSystem), "CollectLaneDirectionsJob", "Execute", lanes, values);
            values["m_SourceMap"] = source;
            values["m_TargetMap"] = target;
            Run(typeof(LaneOverlapSystem), "UpdateLaneFlagsJob", "Execute", lanes, values);
        }
        finally
        {
            for (var i = allocated.Count - 1; i >= 0; i--) allocated[i].Dispose();
        }

        object Allocate(string factory, string nativeType, int count)
        {
            var type = typeof(LaneOverlapSystem).GetNestedType(nativeType, BindingFlags.NonPublic)
                ?? throw new MissingMemberException(typeof(LaneOverlapSystem).FullName, nativeType);
            var value = typeof(MovableNetworkLanes).GetMethod(factory, BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(type).Invoke(null, new object[] { count })!;
            allocated.Add((IDisposable)value);
            return value;
        }
    }

    // Keep this stage separate: native connection/signal/object initialization occurs between
    // overlap calculation and secondary lanes and must be supplied by the context builder.
    internal void CreateSecondary(Entity theme, bool leftHandTraffic)
    {
        RequirePrivateWorld();
        using var owners = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SubLane>(), ComponentType.ReadOnly<Updated>() },
            Any = new[] { ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadOnly<Node>() },
            None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Game.Objects.Transform>() }
        });
        using var lanes = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Owner>(),
                ComponentType.ReadOnly<SecondaryLane>() },
            Any = new[] { ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Deleted>() }
        });
        var values = new Dictionary<string, object>
        {
            ["m_DefaultTheme"] = theme,
            ["m_LeftHandTraffic"] = leftHandTraffic,
            ["m_AppliedTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(),
                ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>()),
            ["m_DeletedTempTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<Deleted>(), ComponentType.ReadWrite<Temp>()),
            ["m_HideLaneTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<CullingInfo>(),
                ComponentType.ReadWrite<MeshBatch>(), ComponentType.ReadWrite<MeshColor>())
        };
        Run(typeof(SecondaryLaneSystem), "UpdateLanesJob", "UpdateLanes", owners, values);
        Run(typeof(SecondaryLaneReferencesSystem), "UpdateLaneReferencesJob", "Execute", lanes, values);
    }

    private void RequirePrivateWorld()
    {
        if (World == World.DefaultGameObjectInjectionWorld)
            throw new InvalidOperationException("Movable lane calculation requires a private network world.");
        EntityManager.CompleteAllTrackedJobs();
    }

    private static object AllocateMap<T>(int capacity) where T : unmanaged
        => new NativeParallelMultiHashMap<PathNode, T>(capacity, Allocator.TempJob);
    private static object AllocateQueue<T>(int unused) where T : unmanaged
        => new NativeQueue<T>(Allocator.TempJob);
    private static object Writer(object container)
        => container.GetType().GetMethod("AsParallelWriter", Type.EmptyTypes)!.Invoke(container, null)!;
}
