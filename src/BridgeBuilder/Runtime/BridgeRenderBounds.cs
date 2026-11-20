using System;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Runtime;

// GeometrySystem's height interval also controls water validation. Keep it intact and
// complete the spatial envelope before the native search tree and culling consume it.
internal static class BridgeRenderBounds
{
    private const string HarmonyId = "BridgeBuilder.RenderBounds";
    internal static void Start()
    {
        new Harmony(HarmonyId).Patch(AccessTools.DeclaredMethod(typeof(SearchSystem), "OnUpdate"),
            prefix: new HarmonyMethod(typeof(BridgeRenderBounds), nameof(BeforeSearch)));
    }
    internal static void Stop() => new Harmony(HarmonyId).UnpatchAll(HarmonyId);

    private static void BeforeSearch(SearchSystem __instance, bool ___m_Loaded,
        EntityQuery ___m_AllNetsQuery, EntityQuery ___m_UpdatedNetsQuery)
    {
        var query = ___m_Loaded ? ___m_AllNetsQuery : ___m_UpdatedNetsQuery;
        if (query.IsEmptyIgnoreFilter) return;
        var manager = __instance.EntityManager;
        var prefabs = __instance.World.GetOrCreateSystemManaged<PrefabSystem>();
        using var entities = query.ToEntityArray(Allocator.Temp);
        var completed = false;
        foreach (var entity in entities)
        {
            if (!manager.HasComponent<EdgeGeometry>(entity) || manager.HasComponent<Deleted>(entity)
                || !manager.HasComponent<Composition>(entity) || !manager.HasComponent<PrefabRef>(entity)) continue;
            if (!prefabs.TryGetPrefab<NetPrefab>(manager.GetComponentData<PrefabRef>(entity), out var prefab)
                || !BridgeAssetInfo.TryFileOwner(prefab.name, out _)) continue;
            if (!completed)
            {
                manager.CompleteAllTrackedJobs();
                completed = true;
            }
            var composition = manager.GetComponentData<Composition>(entity);
            var edge = manager.GetComponentData<EdgeGeometry>(entity);
            edge.m_Bounds = Include(edge.m_Bounds, CurveBounds(edge.m_Start) | CurveBounds(edge.m_End),
                Height(manager, composition.m_Edge));
            manager.SetComponentData(entity, edge);
            var start = manager.GetComponentData<StartNodeGeometry>(entity);
            start.m_Geometry.m_Bounds = Include(start.m_Geometry.m_Bounds,
                CurveBounds(start.m_Geometry.m_Left) | CurveBounds(start.m_Geometry.m_Right)
                    | MathUtils.TightBounds(start.m_Geometry.m_Middle), Height(manager, composition.m_StartNode));
            manager.SetComponentData(entity, start);
            var end = manager.GetComponentData<EndNodeGeometry>(entity);
            end.m_Geometry.m_Bounds = Include(end.m_Geometry.m_Bounds,
                CurveBounds(end.m_Geometry.m_Left) | CurveBounds(end.m_Geometry.m_Right)
                    | MathUtils.TightBounds(end.m_Geometry.m_Middle), Height(manager, composition.m_EndNode));
            manager.SetComponentData(entity, end);
        }
    }

    private static Bounds3 CurveBounds(Segment segment)
        => MathUtils.TightBounds(segment.m_Left) | MathUtils.TightBounds(segment.m_Right);

    internal static Bounds3 Include(Bounds3 original, Bounds3 curves, Bounds1 height)
    {
        curves.min.y += height.min;
        curves.max.y += height.max;
        return original | curves;
    }

    private static Bounds1 Height(EntityManager manager, Entity composition)
    {
        var height = manager.GetComponentData<NetCompositionData>(composition).m_HeightRange;
        var mesh = manager.GetComponentData<NetCompositionMeshRef>(composition).m_Mesh;
        Accumulate(manager, mesh, ref height);
        if (manager.HasBuffer<LodMesh>(mesh))
            foreach (var lod in manager.GetBuffer<LodMesh>(mesh, true)) Accumulate(manager, lod.m_LodMesh, ref height);
        return height;
    }

    private static void Accumulate(EntityManager manager, Entity mesh, ref Bounds1 height)
    {
        if (!manager.HasBuffer<NetCompositionPiece>(mesh)) return;
        foreach (var piece in manager.GetBuffer<NetCompositionPiece>(mesh, true))
        {
            var bounds = manager.GetComponentData<MeshData>(piece.m_Piece).m_Bounds;
            height.min = math.min(height.min, bounds.min.y + piece.m_Offset.y);
            height.max = math.max(height.max, bounds.max.y + piece.m_Offset.y);
        }
    }
}
