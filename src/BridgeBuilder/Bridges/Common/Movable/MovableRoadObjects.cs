using System;
using System.Collections.Generic;
using System.Reflection;
using BridgeBuilder.Runtime;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Resolves selected-road object rules through the installed native composition job.
/// These are placement rules, not instantiated children: native spacing, theme selection,
/// traffic-sign needs and bone attachment still belong to subsequent assembly stages.
/// </summary>
internal static partial class MovableRoadObjects
{
    // The prepared owner includes the native endpoint and edge lanes, composition and geometry.
    // This reads that context; it does not create a road in the city or fabricate empty endpoints.
    internal static ObjectSubObjectInfo[] ResolvePlacements(World laneWorld, Entity laneOwner,
        IReadOnlyList<Rule> rules, NetGeometryData networkGeometry, Entity defaultTheme,
        bool leftHandTraffic, float ownerElevation, Func<Entity, ObjectPrefab> originalObject,
        ref Unity.Mathematics.Random random)
    {
        var sourceWorld = World.DefaultGameObjectInjectionWorld;
        var source = sourceWorld.EntityManager;
        var prefabs = sourceWorld.GetExistingSystemManaged<PrefabSystem>();
        var manager = laneWorld.EntityManager;
        var composition = manager.GetComponentData<Composition>(laneOwner);
        var edge = manager.GetComponentData<EdgeGeometry>(laneOwner);
        var data = manager.GetComponentData<NetCompositionData>(composition.m_Edge);
        var start = manager.GetComponentData<NetCompositionData>(composition.m_StartNode);
        var end = manager.GetComponentData<NetCompositionData>(composition.m_EndNode);
        var startGeometry = manager.GetComponentData<StartNodeGeometry>(laneOwner).m_Geometry;
        var endGeometry = manager.GetComponentData<EndNodeGeometry>(laneOwner).m_Geometry;
        var needs = laneWorld.GetOrCreateSystemManaged<MovableRoadTrafficNeeds>();
        needs.Begin(laneOwner, leftHandTraffic);
        var selection = new MovableRoadObjectSelection(source, prefabs, defaultTheme);
        var output = new List<ObjectSubObjectInfo>();
        void Resolve(MovableRoadObjectPlacement.Candidate candidate, ref Unity.Mathematics.Random state)
        {
            if (!prefabs.TryGetEntity(candidate.Prefab, out var entity))
                throw new InvalidOperationException($"Road object '{candidate.Prefab.name}' has no native traffic classification.");
            var context = needs.Calculate(source.HasComponent<TrafficSignData>(entity),
                source.HasComponent<LaneDirectionData>(entity), candidate.Object.m_Position);
            Select(candidate, context, ref state);
        }
        void Select(MovableRoadObjectPlacement.Candidate candidate,
            MovableRoadObjectSelection.TrafficSignNeeds context, ref Unity.Mathematics.Random state)
        {
            // Retain native selection/group consumption even though the saved network owns
            // the live signal. Skipping selection changes all subsequent random placements.
            if (MovableSectionObjects.IsNetworkSignal(candidate.Prefab))
            {
                if (!prefabs.TryGetEntity(candidate.Prefab, out var entity))
                    throw new InvalidOperationException("Signal has no native selection data: " + candidate.Prefab.name);
                selection.TrySelect(entity, context, ref state, 0, candidate.Probability, out _);
                return;
            }
            if (selection.TryResolve(candidate, context, ownerElevation, ref state, out var placement))
                output.Add(placement!);
        }
        void ResolveLane(MovableRoadObjectPlacement.Candidate candidate, ref Unity.Mathematics.Random state)
            => Select(candidate, default, ref state);
        MovableRoadObjectPlacement.Visit(rules, edge, data, networkGeometry, start, end,
            startGeometry, endGeometry, ref random, Resolve);
        MovableEdgeLaneObjects.Visit(manager, laneOwner, data, originalObject, ref random, ResolveLane);
        return output.ToArray();
    }

    // Input placements have already been resolved from the native object rules. Bind their
    // anchors to the SAME measured partition as the road triangles. Do not use prefab names,
    // nominal road width or bone names to decide which longitudinal leaf carries an object.
    internal static ObjectSubObjectInfo[] Bind(IReadOnlyList<ObjectSubObjectInfo> placements,
        Pose roadToObject, IReadOnlyList<MovableRoadGeometry.Partition> placementPartitions,
        StaticObjectPrefab mechanism, string owner, List<PrefabBase> created,
        Func<ObjectSubObjectInfo, bool>? meshInMechanism = null, bool endSupportsPrepared = false,
        Func<ObjectSubObjectInfo, ObjectPrefab?>? nestedCarrier = null)
    {
        // The moving road is merged into the mechanism's first render graph. Parent IDs are
        // native connection IDs, not indices into its ProceduralAnimationProperties array.
        var bones = mechanism.m_Meshes[0].m_Mesh
            .GetComponent<ProceduralAnimationProperties>().m_Bones;
        if (!endSupportsPrepared)
            AddEndSupports(ref placements, roadToObject, ref placementPartitions, index =>
            {
                for (int bone = index; bone >= 0; bone = bones[bone].parentId)
                    if (bones[bone].m_Type != (BoneType)0) return true;
                return false;
            });
        var result = new List<ObjectSubObjectInfo>(placements.Count);
        var q = roadToObject.rotation;
        q = q.normalized;
        for (var i = 0; i < placements.Count; i++)
        {
            var source = placements[i];
            var position = q * (Vector3)source.m_Position + roadToObject.position;
            int? mount = MountBone(position, placementPartitions[i], i);
            if (!mount.HasValue) continue;
            var bone = bones[mount.Value];
            var parent = bone.m_ConnectionID;
            if (parent != 0)
            {
            }
            var rotation = q * (Quaternion)source.m_Rotation;
            result.Add(new ObjectSubObjectInfo
            {
                m_Object = source.m_Object, m_Position = position,
                m_Rotation = rotation, m_ParentMesh = parent,
                m_GroupIndex = source.m_GroupIndex, m_Probability = source.m_Probability
            });
        }
        // Positions remain closed-pose object-space values. Native FindBoneIndex performs the
        // inverse bind conversion itself; applying bindPose here would transform children twice.
        var bound = result.ToArray();
        var merged = new bool[bound.Length];
        for (int i = 0; i < bound.Length; i++) merged[i] = meshInMechanism?.Invoke(bound[i]) == true;
        for (int i = 0; i < bound.Length; i++)
        {
            if (!merged[i]) continue;
            var carrier = nestedCarrier?.Invoke(bound[i]);
            if (carrier == null) continue;
            bound[i].m_Object = carrier;
            merged[i] = false; // Metadata-only hierarchy retains native exceptional descendants.
        }
        MovableStreetLights.Bind(owner, mechanism, bound, created);
        // Merged geometry already consumes the mechanism skeleton. Do not instantiate
        // a second Relative/BoneHistory-driven visual child for that geometry.
        var children = new List<ObjectSubObjectInfo>();
        for (int i = 0; i < bound.Length; i++)
        {
            if (!merged[i]) { children.Add(bound[i]); continue; }
            if (bound[i].m_Object is StaticObjectPrefab source && source.Has<UtilityObject>())
            {
                bound[i].m_Object = MovableRoadObjectCarrier.Create(owner, source, created);
                children.Add(bound[i]);
            }
        }
        return children.ToArray();
    }

    // Resolve every attachment against its measured physical support. A gap has
    // no mounting bone, regardless of the object type or source asset identity.
    internal static int? MountBone(Vector3 position,
        MovableRoadGeometry.Partition region, int index)
    {
        while (region.Bone < 0 && !region.IsGap)
        {
            var distance = region.ObjectToSeam.MultiplyPoint3x4(position).z;
            region = distance < 0f ? region.Negative! : region.Positive!;
        }
        return region.IsGap ? null : region.Bone;
    }

    internal readonly struct Rule
    {
        internal readonly ObjectPrefab Prefab;
        internal readonly NetCompositionObject Placement;
        internal readonly NetPiecePrefab SourcePiece;
        internal readonly int SourceOccurrence;
        internal readonly int SourceRuleIndex;

        internal Rule(ObjectPrefab prefab, NetCompositionObject placement, NetPiecePrefab sourcePiece,
            int sourceOccurrence, int sourceRuleIndex)
        {
            Prefab = prefab;
            SourcePiece = sourcePiece;
            SourceOccurrence = sourceOccurrence;
            SourceRuleIndex = sourceRuleIndex;
            placement.m_Prefab = Entity.Null;
            Placement = placement;
        }
    }

    internal static Rule[] Extract(World context, Entity composition, MovableNetworkPrefabs imported)
    {
        var origins = context.GetOrCreateSystemManaged<ObjectCompositionSystem>().TraceOrigins(composition,
            context.EntityManager.GetComponentData<NetCompositionData>(composition));
        var placements = context.EntityManager.GetBuffer<NetCompositionObject>(composition, true);
        var pieces = context.EntityManager.GetBuffer<NetCompositionPiece>(composition, true);
        var rules = new Rule[placements.Length];
        for (var i = 0; i < placements.Length; i++)
        {
            var placement = placements[i];
            var original = imported.Original(placement.m_Prefab);
            var prefab = (ObjectPrefab)original;
            var piece = imported.Original(pieces[origins[i].Occurrence].m_Piece);
            var sourcePiece = (NetPiecePrefab)piece;
            rules[i] = new Rule(prefab, placement, sourcePiece, origins[i].Occurrence, origins[i].RuleIndex);
        }
        return rules;
    }

    internal static Rule[] Extract(BridgePreviewComposition.Piece[] pieces, NetCompositionData data)
    {
        var sourceWorld = World.DefaultGameObjectInjectionWorld;
        if (sourceWorld == null || !sourceWorld.IsCreated)
            throw new InvalidOperationException("The native world is unavailable for road object composition.");
        var source = sourceWorld.EntityManager;
        var prefabs = sourceWorld.GetExistingSystemManaged<PrefabSystem>();
        using var world = new World("BridgeBuilder temporary road objects");
        var manager = world.EntityManager;
        var imported = new Dictionary<Entity, Entity>();
        var originals = new Dictionary<Entity, ObjectPrefab>();
        var composition = manager.CreateEntity();
        manager.AddBuffer<NetCompositionPiece>(composition);
        manager.AddBuffer<NetCompositionObject>(composition);

        void Copy<T>(Entity from, Entity to) where T : unmanaged, IComponentData
        {
            if (source.HasComponent<T>(from))
                manager.AddComponentData(to, source.GetComponentData<T>(from));
        }

        Entity Import(Entity entity)
        {
            if (imported.TryGetValue(entity, out var existing)) return existing;
            if (!prefabs.TryGetPrefab<ObjectPrefab>(entity, out var prefab) || prefab == null)
                throw new InvalidOperationException("A selected-road object has an unresolved native Prefab.");
            var result = manager.CreateEntity();
            imported.Add(entity, result);
            originals.Add(result, prefab);
            Copy<StreetLightData>(entity, result);
            Copy<LaneDirectionData>(entity, result);
            Copy<TrafficSignData>(entity, result);
            Copy<UtilityObjectData>(entity, result);
            return result;
        }

        foreach (var occurrence in pieces)
        {
            if (!prefabs.TryGetEntity(occurrence.Prefab, out var sourcePiece))
                throw new InvalidOperationException($"'{occurrence.SourcePath}' has no initialized native piece for object composition.");
            var piece = occurrence.Composition;
            piece.m_Piece = manager.CreateEntity();
            if (source.HasBuffer<NetPieceObject>(sourcePiece))
            {
                // Import first: adding entity components invalidates previously acquired buffers.
                var objects = source.GetBuffer<NetPieceObject>(sourcePiece, true);
                var copied = new NetPieceObject[objects.Length];
                for (var i = 0; i < copied.Length; i++)
                {
                    var item = objects[i];
                    item.m_Prefab = Import(item.m_Prefab);
                    copied[i] = item;
                }
                var buffer = manager.AddBuffer<NetPieceObject>(piece.m_Piece);
                foreach (var item in copied) buffer.Add(item);
            }
            else ;            manager.GetBuffer<NetCompositionPiece>(composition).Add(piece);
        }

        var composer = world.GetOrCreateSystemManaged<ObjectCompositionSystem>();
        composer.Compose(composition, data);
        var origins = composer.TraceOrigins(composition, data);
        var resolved = manager.GetBuffer<NetCompositionObject>(composition, true);
        var output = new Rule[resolved.Length];
        for (var i = 0; i < output.Length; i++)
            output[i] = new Rule(originals[resolved[i].m_Prefab], resolved[i],
                pieces[origins[i].Occurrence].Prefab, origins[i].Occurrence, origins[i].RuleIndex);
        return output;
    }

    [DisableAutoCreation]
    internal sealed partial class ObjectCompositionSystem : SystemBase
    {
        // Invoke the installed implementation instead of duplicating its inversion, streetlight
        // grouping, spacing override and exclusion rules. All lookups belong to this private world.
        private static readonly MethodInfo ComposeObjects = typeof(NetCompositionSystem)
            .GetNestedType("InitializeCompositionJob", BindingFlags.NonPublic)?
            .GetMethod("AddCompositionObjects", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException("NetCompositionSystem.InitializeCompositionJob.AddCompositionObjects");

        protected override void OnUpdate() { }

        internal void Compose(Entity composition, NetCompositionData data)
        {
            try
            {
                ComposeObjects.Invoke(null, new object[]
                {
                    composition, data,
                    EntityManager.GetBuffer<NetCompositionPiece>(composition, true),
                    EntityManager.GetBuffer<NetCompositionObject>(composition),
                    GetComponentLookup<StreetLightData>(true), GetComponentLookup<LaneDirectionData>(true),
                    GetComponentLookup<TrafficSignData>(true), GetComponentLookup<UtilityObjectData>(true),
                    GetBufferLookup<NetPieceObject>(true)
                });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw new InvalidOperationException("Native selected-road object composition failed.", exception.InnerException);
            }
        }
    }
}
