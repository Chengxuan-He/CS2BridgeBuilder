using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace BridgeBuilder.Bridges;

// Native 1.6.2f1 CreateSecondaryNodeObjects streetlight aggregation. One instance
// covers one node, in native connected-edge order; it is not shared between bridges.
internal sealed class MovableNodeStreetLights
{
    private readonly EntityManager _source;
    private readonly PrefabSystem _prefabs;
    private readonly List<Entry> _pending = new();
    private float _length;
    private float3 _center;
    private int _contributions;
    internal bool AlwaysLit { get; private set; }

    private readonly struct Entry
    {
        internal readonly MovableRoadObjectPlacement.Candidate Candidate;
        internal readonly StreetLightLayer Layer;
        internal readonly float Spacing, Priority;
        internal readonly bool Lowered;
        internal Entry(MovableRoadObjectPlacement.Candidate candidate, StreetLightLayer layer,
            float spacing, float priority, bool lowered)
        { Candidate = candidate; Layer = layer; Spacing = spacing; Priority = priority; Lowered = lowered; }
    }

    internal MovableNodeStreetLights(EntityManager source, PrefabSystem prefabs)
    { _source = source; _prefabs = prefabs; }

    // Non-light candidates continue through the caller's native sign/utility pipeline.
    // Random phase consumption therefore retains its position in connected-edge traversal;
    // concrete streetlight selection happens only after all contributions are collected.
    internal void Visit(IReadOnlyList<MovableRoadObjects.Rule> rules, EdgeGeometry edge,
        EdgeNodeGeometry geometry, NetCompositionData composition, NetGeometryData network,
        bool isEnd, ref Random random, MovableNodeObjectPlacement.ResolveCandidate other)
    {
        _length += geometry.m_MiddleRadius > 0f
            ? math.max(math.distance(geometry.m_Middle.a, geometry.m_Middle.d) * 2f,
                geometry.m_Left.m_Length.x + geometry.m_Left.m_Length.y +
                geometry.m_Right.m_Length.x + geometry.m_Right.m_Length.y)
            : math.max(math.distance(geometry.m_Middle.a, geometry.m_Middle.d) * 2f,
                geometry.m_Left.m_Length.x + geometry.m_Right.m_Length.y);
        _center = geometry.m_Middle.d;
        _contributions++;
        AlwaysLit |= (composition.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0 &&
            ((composition.m_Flags.m_Left | composition.m_Flags.m_Right) &
                (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0;

        void Collect(MovableRoadObjectPlacement.Candidate candidate, float priority,
            bool lowered, ref Random state)
        {
            if (!_prefabs.TryGetEntity(candidate.Prefab, out var entity))
                throw new InvalidOperationException($"Node object '{candidate.Prefab.name}' has no native classification.");
            if (!_source.HasComponent<StreetLightData>(entity))
            {
                other(candidate, priority, lowered, ref state);
                return;
            }
            // Native duplicate rejection precedes layer arbitration and keeps the first
            // candidate even if a later coincident candidate has another light layer.
            foreach (var existing in _pending)
                if (math.distancesq(existing.Candidate.Object.m_Position, candidate.Object.m_Position) < 1f)
                    return;
            _pending.Add(new Entry(candidate, _source.GetComponentData<StreetLightData>(entity).m_Layer,
                rules[candidate.RuleIndex].Placement.m_Spacing, priority, lowered));
        }
        MovableNodeObjectPlacement.Visit(rules, edge, geometry, composition, network,
            isEnd, ref random, Collect);
    }

    // Callback performs ordinary concrete-prefab selection with default sign needs.
    // Do not sort layers or use stable removal: native swap-back ordering also determines
    // which randomization group consumes the next values in the shared stream.
    internal bool Resolve(ref Random random, MovableNodeObjectPlacement.ResolveCandidate resolve)
    {
        // Native hasStreetLights describes collected candidates, even if the spacing
        // calculation retains none; callers use it independently of selected objects.
        var hasStreetLights = _pending.Count != 0;
        while (_pending.Count != 0)
        {
            var layer = _pending[0].Layer;
            var best = -1f;
            var selected = 0;
            var spacing = 0f;
            var count = 0;
            for (var i = 0; i < _pending.Count; i++)
            {
                var entry = _pending[i];
                if (entry.Layer != layer) continue;
                var priority = math.distance(_center, entry.Candidate.Object.m_Position) + entry.Priority;
                if (priority > best) { best = priority; selected = i; }
                spacing += entry.Spacing;
                count++;
            }
            Swap(0, selected);
            var keep = math.min(count, Mathf.RoundToInt(_length * count / math.max(1f, spacing)));
            keep = math.select(keep, 2, keep == 3 && _contributions == 4);
            for (var slot = 1; slot < keep; slot++)
            {
                best = -1f;
                selected = slot;
                for (var i = slot; i < _pending.Count; i++)
                {
                    var entry = _pending[i];
                    if (entry.Layer != layer) continue;
                    var distance = float.MaxValue;
                    for (var earlier = 0; earlier < slot; earlier++)
                        distance = math.min(distance, math.distance(entry.Candidate.Object.m_Position,
                            _pending[earlier].Candidate.Object.m_Position));
                    distance += entry.Priority;
                    if (distance > best) { best = distance; selected = i; }
                }
                Swap(slot, selected);
            }
            for (var i = 0; i < keep; i++)
            {
                var entry = _pending[i];
                resolve(entry.Candidate, entry.Priority, entry.Lowered, ref random);
            }
            var index = 0;
            while (index < _pending.Count)
            {
                if (_pending[index].Layer != layer) { index++; continue; }
                _pending[index] = _pending[_pending.Count - 1];
                _pending.RemoveAt(_pending.Count - 1);
            }
        }
        return hasStreetLights;
    }

    private void Swap(int first, int second)
    {
        var entry = _pending[first];
        _pending[first] = _pending[second];
        _pending[second] = entry;
    }
}
