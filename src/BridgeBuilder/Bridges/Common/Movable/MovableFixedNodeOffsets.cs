using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Game.Common;
using Game.Net;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

// The native fixed-offset helper uses isStart only to select the owning edge's node.
// Auxiliary edges can run in the opposite direction. Their node Owner is the actual
// correspondence; changing the edge ownership or copying a mechanism is not equivalent.
internal sealed class MovableFixedNodeOffsets : IDisposable
{
    [ThreadStatic] private static Dictionary<(Entity Edge, Entity Node), bool>? _current;
    [ThreadStatic] private static int _hits;
    private static bool _installed;
    private readonly Dictionary<(Entity Edge, Entity Node), bool>? _previous;
    private readonly int _previousHits;

    internal MovableFixedNodeOffsets(EntityManager manager)
    {
        _previous = _current;
        _previousHits = _hits;
        var correspondence = new Dictionary<(Entity Edge, Entity Node), bool>();
        using var query = manager.CreateEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Owner>());
        using var edges = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in edges)
        {
            var parent = manager.GetComponentData<Owner>(entity).m_Owner;
            if (!manager.HasComponent<Edge>(parent)) continue;
            var edge = manager.GetComponentData<Edge>(entity);
            var parentEdge = manager.GetComponentData<Edge>(parent);
            Record(edge.m_Start, true);
            Record(edge.m_End, false);

            void Record(Entity node, bool childStart)
            {
                if (!manager.HasComponent<Owner>(node)) return;
                var parentNode = manager.GetComponentData<Owner>(node).m_Owner;
                if (parentNode == parentEdge.m_Start && !childStart)
                    correspondence[(entity, node)] = true;
                else if (parentNode == parentEdge.m_End && childStart)
                    correspondence[(entity, node)] = false;
            }
        }
        if (correspondence.Count != 0) Install();
        _current = correspondence;
        _hits = 0;
    }

    private static void Install()
    {
        if (_installed) return;
        var job = typeof(GeometrySystem).GetNestedType("CalculateEdgeGeometryJob", BindingFlags.NonPublic)
            !;
        var method = job.GetMethod("CalculateFixedOffsets", BindingFlags.Instance | BindingFlags.NonPublic)
            !;
        new Harmony("BridgeBuilder.PrivateMovableFixedOffsets").Patch(method,
            prefix: new HarmonyMethod(typeof(MovableFixedNodeOffsets), nameof(SelectParentEndpoint)));
        _installed = true;
    }

    private static void SelectParentEndpoint(Entity edge, Entity node, ref bool isStart)
    {
        if (_current != null && _current.TryGetValue((edge, node), out var parentStart))
        {
            isStart = parentStart;
            _hits++;
        }
    }

    internal void Report(EntityManager manager)
    {
        if (_current == null || _current.Count == 0) return;
        foreach (var edge in _current.Keys.Select(key => key.Edge).Distinct())
        {
            var endpoints = manager.GetComponentData<Edge>(edge);
            var start = manager.GetComponentData<StartNodeGeometry>(edge).m_Geometry.m_Bounds;
            var end = manager.GetComponentData<EndNodeGeometry>(edge).m_Geometry.m_Bounds;
            Mod.Log.Info($"Private movable fixed offsets: edge={edge}; endpoints={endpoints.m_Start}/{endpoints.m_End}; "
                + $"correctedQueries={_hits}; startBounds={start.min}/{start.max}; endBounds={end.min}/{end.max}.");
        }
    }

    public void Dispose()
    {
        _current = _previous;
        _hits = _previousHits;
    }
}
