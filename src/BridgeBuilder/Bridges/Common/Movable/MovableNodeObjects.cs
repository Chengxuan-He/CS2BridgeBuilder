using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Candidate = BridgeBuilder.Bridges.MovableRoadObjectPlacement.Candidate;
using TrafficSignNeeds = BridgeBuilder.Bridges.MovableRoadObjectSelection.TrafficSignNeeds;

namespace BridgeBuilder.Bridges;

// Native node ordering: immediate objects, aggregated lights, lane-derived signs, utility nodes.
// This operates on a prepared native lane world. It never creates a network in the city.
[DisableAutoCreation]
internal sealed partial class MovableNodeObjects : SystemBase
{
    internal readonly struct Result
    {
        internal readonly ObjectSubObjectInfo[] Placements;
        internal readonly MovableRoadObjects.Rule?[] Sources;
        internal readonly bool HasStreetLights, AlwaysLit;
        internal Result(ObjectSubObjectInfo[] placements, MovableRoadObjects.Rule?[] sources,
            bool hasStreetLights, bool alwaysLit)
        { Placements = placements; Sources = sources; HasStreetLights = hasStreetLights; AlwaysLit = alwaysLit; }
    }

    protected override void OnUpdate() { }

    // Both resolvers translate prepared-world identities into original, permitted source data.
    // Rules must include retained signals: their candidates affect native closest-sign selection.
    internal Result Resolve(Entity node, Func<Entity, IReadOnlyList<MovableRoadObjects.Rule>> rules,
        Func<Entity, ObjectPrefab> originalObject, Entity defaultTheme, bool leftHandTraffic,
        ref Random random)
    {
        var nativeWorld = World.DefaultGameObjectInjectionWorld;
        var source = nativeWorld.EntityManager;
        var prefabs = nativeWorld.GetExistingSystemManaged<PrefabSystem>();
        var signs = World.GetOrCreateSystemManaged<MovableNodeSignNeeds>();
        var utilities = World.GetOrCreateSystemManaged<MovableNodeUtilities>();
        signs.Begin(leftHandTraffic);
        utilities.Begin();
        var lights = new MovableNodeStreetLights(source, prefabs);
        var selection = new MovableRoadObjectSelection(source, prefabs, defaultTheme);
        var pendingSigns = new List<Candidate>();
        var utilityAnchors = new List<float3>();
        var output = new List<ObjectSubObjectInfo>();
        var origins = new List<MovableRoadObjects.Rule?>();
        var ownerElevation = EntityManager.HasComponent<Game.Net.Elevation>(node)
            ? math.cmin(EntityManager.GetComponentData<Game.Net.Elevation>(node).m_Elevation) : 0f;

        Entity Source(Candidate candidate)
        {
            if (!prefabs.TryGetEntity(candidate.Prefab, out var entity))
                throw new InvalidOperationException("Node object has no native selection data: " + candidate.Prefab.name);
            return entity;
        }
        void Select(Candidate candidate, TrafficSignNeeds needs, ref Random state)
        {
            if (MovableSectionObjects.IsNetworkSignal(candidate.Prefab))
            {
                // Preserve native randomization-group consumption for later visual objects,
                // but let the saved network producer create and control the actual signal.
                selection.TrySelect(Source(candidate), needs, ref state, 0, candidate.Probability, out _);
                return;
            }
            if (selection.TryResolve(candidate, needs, ownerElevation, ref state, out var placement))
            {
                output.Add(placement!);
                origins.Add(candidate.SourceRule);
            }
        }
        void SelectLight(Candidate candidate, float priority, bool lowered, ref Random state)
            => Select(candidate, default, ref state);

        var iterator = new EdgeIterator(Entity.Null, node, GetBufferLookup<ConnectedEdge>(true),
            GetComponentLookup<Edge>(true), GetComponentLookup<Game.Tools.Temp>(true),
            GetComponentLookup<Game.Tools.Hidden>(true), includeMiddleConnections: true);
        var contributions = 0;
        while (iterator.GetNext(out var value))
        {
            var firstSign = signs.Count;
            if (!value.m_Middle)
            {
                var composition = EntityManager.GetComponentData<Composition>(value.m_Edge);
                var edge = EntityManager.GetComponentData<EdgeGeometry>(value.m_Edge);
                var edgeData = EntityManager.GetComponentData<NetCompositionData>(composition.m_Edge);
                var nodeComposition = value.m_End ? composition.m_EndNode : composition.m_StartNode;
                var data = EntityManager.GetComponentData<NetCompositionData>(nodeComposition);
                var geometry = value.m_End
                    ? EntityManager.GetComponentData<EndNodeGeometry>(value.m_Edge).m_Geometry
                    : EntityManager.GetComponentData<StartNodeGeometry>(value.m_Edge).m_Geometry;
                var network = EntityManager.GetComponentData<NetGeometryData>(
                    EntityManager.GetComponentData<PrefabRef>(value.m_Edge).m_Prefab);
                var tangent = value.m_End
                    ? MathUtils.EndTangent(edge.m_End.m_Left) + MathUtils.StartTangent(edge.m_End.m_Right)
                    : -(MathUtils.StartTangent(edge.m_Start.m_Left) + MathUtils.StartTangent(edge.m_Start.m_Right));
                var direction = geometry.m_MiddleRadius > 0f
                    ? MathUtils.StartTangent(geometry.m_Left.m_Left).xz + MathUtils.StartTangent(geometry.m_Left.m_Right).xz
                    : (math.any(geometry.m_Left.m_Length > .05f) | math.any(geometry.m_Right.m_Length > .05f))
                        ? MathUtils.StartTangent(geometry.m_Left.m_Left).xz + MathUtils.StartTangent(geometry.m_Right.m_Right).xz
                        : tangent.xz;
                var initial = signs.Contribution(edgeData, data, network, direction, value.m_End);
                void Collect(Candidate candidate, float priority, bool lowered, ref Random state)
                {
                    var entity = Source(candidate);
                    if (source.HasComponent<TrafficSignData>(entity) || source.HasComponent<TrafficLightData>(entity))
                    {
                        signs.Add(candidate, initial);
                        pendingSigns.Add(candidate);
                        return;
                    }
                    if (source.HasComponent<UtilityObjectData>(entity))
                    {
                        var anchor = ObjectUtils.LocalToWorld(candidate.Object,
                            source.GetComponentData<UtilityObjectData>(entity).m_UtilityPosition);
                        foreach (var existing in utilityAnchors)
                            if (math.distancesq(existing, anchor) < 1f) return;
                        utilityAnchors.Add(anchor);
                    }
                    Select(candidate, default, ref state);
                }
                lights.Visit(rules(nodeComposition), edge, geometry, data, network, value.m_End, ref random, Collect);
                contributions++;
            }
            utilities.CollectEdge(value.m_Edge, value.m_End, value.m_Middle);
            signs.CollectEdgeTargets(node, value.m_Edge, value.m_End, firstSign);
        }
        var hasStreetLights = lights.Resolve(ref random, SelectLight);
        if (contributions == 0) utilities.CollectIsolatedNode(node);
        utilities.CollectNode(node);
        var calculated = signs.Calculate(node);
        for (var i = 0; i < calculated.Length; i++)
            if (MovableNodeSignNeeds.Requested(calculated[i])) Select(pendingSigns[i], calculated[i], ref random);
        foreach (var entry in utilities.Placements())
        {
            // UtilityLane.m_NodeObject remains a native network producer: hiding
            // road mesh layers and suppressing NetPieceObjects does not remove it.
            // Keep native selection/group consumption, but do not create a second
            // utility node beneath the moving object or guess a road-surface bone.
            var prefab = originalObject(entry.m_Prefab);
            if (!prefabs.TryGetEntity(prefab, out var entity))
                throw new InvalidOperationException("Utility node has no native selection data: " + prefab.name);
            selection.TrySelect(entity, default, ref random, 0, 100, out _);
        }
        return new Result(output.ToArray(), origins.ToArray(), hasStreetLights, lights.AlwaysLit);
    }
}
