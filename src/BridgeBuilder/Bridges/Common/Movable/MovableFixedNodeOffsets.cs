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
            ?? throw new MissingMemberException(typeof(GeometrySystem).FullName, "CalculateEdgeGeometryJob");
        var method = job.GetMethod("CalculateFixedOffsets", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(job.FullName, "CalculateFixedOffsets");
        var parameters = method.GetParameters();
        if (parameters.Length < 3 || parameters[0].Name != "edge" || parameters[0].ParameterType != typeof(Entity)
            || parameters[1].Name != "node" || parameters[1].ParameterType != typeof(Entity)
            || parameters[2].Name != "isStart" || parameters[2].ParameterType != typeof(bool))
            throw new MissingMethodException("Native fixed-offset endpoint signature changed.");
        // In the inspected native helper this argument has exactly one use: the
        // owning edge endpoint lookup. Do not change a future helper which also
        // uses it to orient the child geometry.
        var readInstructions = typeof(PatchProcessor).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(candidate => candidate.Name == "GetOriginalInstructions" && candidate.GetParameters().Length == 2
                && !candidate.GetParameters()[1].ParameterType.IsByRef)
            ?? throw new MissingMethodException("Harmony instruction reader unavailable.");
        var instructions = (IEnumerable<CodeInstruction>)readInstructions.Invoke(null, new object?[] { method, null })!;
        var uses = instructions.Count(instruction =>
            instruction.opcode == OpCodes.Ldarg_3 ||
            ((instruction.opcode == OpCodes.Ldarg || instruction.opcode == OpCodes.Ldarg_S)
                && Convert.ToInt32(instruction.operand) == 3));
        if (uses != 1) throw new MissingMethodException("Native fixed-offset endpoint usage changed.");
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
