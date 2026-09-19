using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.Mathematics;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using NetSubLane = Game.Net.SubLane;

namespace BridgeBuilder.Runtime;

/// <summary>
/// A disposable straight-edge lane build. Run the installed game's lane and
/// secondary-lane jobs, not an approximation that paints stripes at guessed Xs.
/// Every entity, archetype, lookup and command buffer belongs to this world.
/// The loaded city supplies read-only prefab data and theme/traffic handedness.
/// </summary>
internal sealed class BridgePreviewLanes : IDisposable
{
    internal readonly struct Lane
    {
        internal Lane(NetLaneGeometryPrefab prefab, Curve curve, EdgeLane? edge)
        { Prefab = prefab; Curve = curve; Edge = edge; }
        internal NetLaneGeometryPrefab Prefab { get; }
        internal Curve Curve { get; }
        internal EdgeLane? Edge { get; }
    }

    private readonly World _world = new("BridgeBuilder preview lanes");
    private readonly EntityManager _source = World.DefaultGameObjectInjectionWorld.EntityManager;
    private readonly PrefabSystem _prefabs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
    private readonly Dictionary<Entity, Entity> _entities = new();
    private readonly Dictionary<Entity, Entity> _originals = new();
    private readonly Dictionary<EntityArchetype, EntityArchetype> _archetypes = new();
    private static readonly MethodInfo? CreateArchetype = typeof(EntityManager).GetMethod("CreateArchetype",
        new[] { typeof(NativeArray<ComponentType>) });
    internal bool LeftHandTraffic { get; } = World.DefaultGameObjectInjectionWorld
        .GetExistingSystemManaged<CityConfigurationSystem>().leftHandTraffic;

    internal bool Build(BridgePreviewComposition.Piece[] pieces, NetCompositionData data,
        float length, out Lane[] result)
    {
        result = Array.Empty<Lane>();
        if (CreateArchetype == null)
        {
            return false;
        }
        try
        {
            var manager = _world.EntityManager;
            var theme = Import(World.DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<CityConfigurationSystem>().defaultTheme);
            using var input = new NativeList<NetCompositionPiece>(Allocator.Temp);
            foreach (var entry in pieces)
            {
                var piece = entry.Composition;
                piece.m_Piece = manager.CreateEntity();
                var lanes = new List<NetPieceLane>();
                if (entry.Prefab.TryGet<NetPieceLanes>(out var source))
                    foreach (var lane in source.m_Lanes ?? Array.Empty<NetLaneInfo>())
                    {
                        if (lane?.m_Lane == null || !_prefabs.TryGetEntity(lane.m_Lane, out var entity) ||
                            !_source.HasComponent<NetLaneData>(entity))
                        {
                            return false;
                        }
                        lanes.Add(new NetPieceLane { m_Lane = Import(entity), m_Position = lane.m_Position,
                            m_ExtraFlags = lane.m_FindAnchor ? LaneFlags.FindAnchor : 0 });
                    }
                var buffer = manager.AddBuffer<NetPieceLane>(piece.m_Piece);
                foreach (var lane in lanes) buffer.Add(lane);
                if (buffer.Length > 1) buffer.AsNativeArray().Sort();
                input.Add(piece);
            }
            var helper = _world.GetOrCreateSystemManaged<LaneSystem>();
            using var composed = new NativeList<NetCompositionLane>(Allocator.Temp);
            helper.Compose(input, composed, ref data);
            if (composed.Length == 0) return true;
            var composition = manager.CreateEntity();
            manager.AddComponentData(composition, data);
            var compositionLanes = manager.AddBuffer<NetCompositionLane>(composition);
            foreach (var lane in composed.AsArray()) compositionLanes.Add(lane);
            var owner = manager.CreateEntity();
            manager.AddBuffer<NetSubLane>(owner);
            manager.AddComponentData(owner, new Composition
                { m_Edge = composition, m_StartNode = composition, m_EndNode = composition });
            var segment = StraightSegment(data.m_Width, length);
            manager.AddComponentData(owner, new EdgeGeometry
            {
                m_Start = StraightSegment(data.m_Width, length * .5f),
                m_End = StraightSegment(data.m_Width, length * .5f, length * .5f)
            });
            // Use the same straight span as the already assembled road surface.
            // LaneSystem handles inversion, group/master/slave flags and prefab
            // replacement. No node intersection or traffic simulation is run.
            if (!helper.CreatePrimary(owner, composition, segment, data, theme, LeftHandTraffic)) return false;
            using (var query = manager.CreateEntityQuery(ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<Curve>()))
            using (var primary = query.ToEntityArray(Allocator.Temp))
            {
                var subLanes = manager.GetBuffer<NetSubLane>(owner);
                foreach (var entity in primary)
                    if (manager.GetComponentData<Owner>(entity).m_Owner == owner)
                        subLanes.Add(new NetSubLane { m_SubLane = entity });
            }
            if (!helper.CreateSecondary(owner, theme, LeftHandTraffic)) return false;
            var output = new List<Lane>();
            using (var query = manager.CreateEntityQuery(ComponentType.ReadOnly<Owner>(),
                       ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<PrefabRef>()))
            using (var all = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in all)
                {
                    if (manager.GetComponentData<Owner>(entity).m_Owner != owner ||
                        manager.HasComponent<Game.Net.MasterLane>(entity) || manager.HasComponent<Game.Tools.Hidden>(entity)) continue;
                    var reference = manager.GetComponentData<PrefabRef>(entity).m_Prefab;
                    if (!_originals.TryGetValue(reference, out var original) ||
                        !_prefabs.TryGetPrefab<NetLanePrefab>(original, out var source) || source == null)
                    {
                        return false;
                    }
                    // The game's TryGetPrefab<T> returns true for a valid index
                    // even when its `as T` cast yields null. Ordinary traffic
                    // lanes are NetLanePrefab, not NetLaneGeometryPrefab. Keep
                    // them for primary/secondary generation above, but only
                    // export actual geometry lanes to the renderer.
                    if (source is not NetLaneGeometryPrefab prefab)
                    {
                        // RequiredBatchesSystem does not draw lanes without a
                        // SubMesh buffer. Do not, however, silently discard a
                        // modded non-geometry lane with real render data.
                        if (_source.HasBuffer<SubMesh>(original) && _source.GetBuffer<SubMesh>(original, true).Length != 0)
                        {
                            return false;
                        }
                        continue;
                    }
                    output.Add(new Lane(prefab, manager.GetComponentData<Curve>(entity),
                        manager.HasComponent<EdgeLane>(entity) ? manager.GetComponentData<EdgeLane>(entity) : null));
                }
            result = output.ToArray();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static Segment StraightSegment(float width, float length, float start = 0f) => new()
    {
        m_Left = NetUtils.StraightCurve(new float3(-width * .5f, 0f, start), new float3(-width * .5f, 0f, start + length)),
        m_Right = NetUtils.StraightCurve(new float3(width * .5f, 0f, start), new float3(width * .5f, 0f, start + length)),
        m_Length = new float2(length)
    };

    private Entity Import(Entity source)
    {
        if (source == Entity.Null) return Entity.Null;
        if (_entities.TryGetValue(source, out var existing)) return existing;
        var target = _world.EntityManager.CreateEntity();
        _entities.Add(source, target);
        _originals.Add(target, source);
        if (!_source.HasComponent<NetLaneData>(source)) return target; // theme/requirement identity only
        Copy<PrefabData>(source, target);
        Copy<NetLaneData>(source, target);
        Copy<NetLaneGeometryData>(source, target);
        Copy<NetLaneArchetypeData>(source, target);
        Copy<CarLaneData>(source, target);
        Copy<TrackLaneData>(source, target);
        Copy<PedestrianLaneData>(source, target);
        Copy<ParkingLaneData>(source, target);
        Copy<UtilityLaneData>(source, target);
        Copy<SecondaryLaneData>(source, target);
        Copy<SpawnableObjectData>(source, target);
        CopyBuffer<SecondaryNetLane>(source, target);
        CopyBuffer<PlaceholderObjectElement>(source, target);
        CopyBuffer<ObjectRequirementElement>(source, target);
        if (_source.HasComponent<PrefabData>(source))
            _world.EntityManager.SetComponentEnabled<PrefabData>(target, _source.IsComponentEnabled<PrefabData>(source));
        return target;
    }

    private T Remap<T>(T value) where T : struct
    {
        object boxed = value;
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (field.FieldType == typeof(Entity))
                field.SetValue(boxed, Import((Entity)field.GetValue(boxed)));
            else if (field.FieldType == typeof(EntityArchetype))
            {
                var source = (EntityArchetype)field.GetValue(boxed);
                if (!source.Valid) continue;
                if (!_archetypes.TryGetValue(source, out var target))
                {
                    using var types = source.GetComponentTypes(Allocator.Temp);
                    // Select the NativeArray overload explicitly: the shipped
                    // ECS assembly also exposes a Mono ReadOnlySpan overload
                    // which cannot be resolved by the netstandard compiler.
                    target = (EntityArchetype)CreateArchetype!.Invoke(_world.EntityManager, new object[] { types });
                    _archetypes.Add(source, target);
                }
                field.SetValue(boxed, target);
            }
        return (T)boxed;
    }

    private void Copy<T>(Entity source, Entity target) where T : unmanaged, IComponentData
    {
        if (_source.HasComponent<T>(source))
            _world.EntityManager.AddComponentData(target, Remap(_source.GetComponentData<T>(source)));
    }

    private void CopyBuffer<T>(Entity source, Entity target) where T : unmanaged, IBufferElementData
    {
        if (!_source.HasBuffer<T>(source)) return;
        var values = _source.GetBuffer<T>(source, true).ToNativeArray(Allocator.Temp);
        try
        {
            var remapped = new T[values.Length];
            for (var i = 0; i < values.Length; i++) remapped[i] = Remap(values[i]);
            var buffer = _world.EntityManager.AddBuffer<T>(target);
            foreach (var value in remapped) buffer.Add(value);
        }
        finally { values.Dispose(); }
    }

    public void Dispose() => _world.Dispose();

    [DisableAutoCreation]
    internal sealed partial class LaneSystem : SystemBase
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        protected override void OnUpdate() { }

        internal void Compose(NativeList<NetCompositionPiece> pieces, NativeList<NetCompositionLane> lanes,
            ref NetCompositionData data) => NetCompositionHelpers.AddCompositionLanes(Entity.Null, ref data,
                pieces, lanes, default, GetComponentLookup<NetLaneData>(true), GetBufferLookup<NetPieceLane>(true));

        // SystemBase hides two of ComponentSystemBase's generic factories.
        // Enumerating the inherited methods and choosing Single(name/arity)
        // is ambiguous on the shipped Mono runtime. Reflect only these unique
        // local adapters; their ECS calls are resolved by the C# compiler and
        // all handles still belong to this disposable preview system/world.
        private ComponentLookup<T> ReadComponent<T>() where T : unmanaged, IComponentData
            => GetComponentLookup<T>(true);
        private BufferLookup<T> ReadBuffer<T>() where T : unmanaged, IBufferElementData
            => GetBufferLookup<T>(true);
        private ComponentTypeHandle<T> ReadComponentHandle<T>() where T : unmanaged, IComponentData
            => GetComponentTypeHandle<T>(true);
        private BufferTypeHandle<T> ReadBufferHandle<T>() where T : unmanaged, IBufferElementData
            => GetBufferTypeHandle<T>(true);

        // Bind the actual installed job type. No copied ABI struct or offsets;
        // reflection failures are caught by Build and reported as assembly failure.
        private object? BindJob(Type system, EntityCommandBuffer commands, Entity theme, bool leftHandTraffic)
        {
            var type = system.GetNestedType("UpdateLanesJob", BindingFlags.NonPublic);
            if (type == null) return null;
            var job = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldType = field.FieldType;
                if (fieldType == typeof(EntityTypeHandle)) { field.SetValue(job, GetEntityTypeHandle()); continue; }
                if (!fieldType.IsGenericType) continue;
                var generic = fieldType.GetGenericTypeDefinition();
                var methodName = generic == typeof(ComponentLookup<>) ? nameof(ReadComponent) :
                    generic == typeof(BufferLookup<>) ? nameof(ReadBuffer) :
                    generic == typeof(ComponentTypeHandle<>) ? nameof(ReadComponentHandle) :
                    generic == typeof(BufferTypeHandle<>) ? nameof(ReadBufferHandle) : null;
                if (methodName == null) continue;
                var method = typeof(LaneSystem).GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
                field.SetValue(job, method.MakeGenericMethod(fieldType.GenericTypeArguments).Invoke(this, null));
            }
            type.GetField("m_CommandBuffer")!.SetValue(job, commands.AsParallelWriter());
            type.GetField("m_DefaultTheme")!.SetValue(job, theme);
            type.GetField("m_LeftHandTraffic")!.SetValue(job, leftHandTraffic);
            type.GetField("m_HideLaneTypes")?.SetValue(job, new ComponentTypeSet(
                ComponentType.ReadWrite<CullingInfo>(), ComponentType.ReadWrite<MeshBatch>(), ComponentType.ReadWrite<MeshColor>()));
            return job;
        }

        internal bool CreatePrimary(Entity owner, Entity composition, Segment segment, NetCompositionData data,
            Entity theme, bool leftHandTraffic)
        {
            using var commands = new EntityCommandBuffer(Allocator.TempJob);
            var job = BindJob(typeof(Game.Net.LaneSystem), commands, theme, leftHandTraffic);
            var method = job?.GetType().GetMethod("CreateEdgeLane", Instance);
            var bufferType = typeof(Game.Net.LaneSystem).GetNestedType("LaneBuffer", BindingFlags.NonPublic);
            if (job == null || method == null || bufferType == null) return false;
            var buffer = Activator.CreateInstance(bufferType, new object[] { Allocator.Temp });
            try
            {
                var compositionData = job.GetType().GetMethod("GetCompositionData", Instance)!
                    .Invoke(job, new object[] { composition });
                var parameters = method.GetParameters();
                var lanes = EntityManager.GetBuffer<NetCompositionLane>(composition);
                var random = new Unity.Mathematics.Random(1);
                foreach (var lane in lanes)
                {
                    var args = new object[] { 0, random, owner, buffer!, segment, data, compositionData!, lanes, lane,
                        new int2(0, 4), new float2(0f, 1f), Activator.CreateInstance(parameters[11].ParameterType)!,
                        Activator.CreateInstance(parameters[12].ParameterType)!, new bool2(false), false, default(Game.Tools.Temp) };
                    method.Invoke(job, args);
                    random = (Unity.Mathematics.Random)args[1];
                }
                commands.Playback(EntityManager);
                return true;
            }
            finally { bufferType.GetMethod("Dispose")!.Invoke(buffer, null); }
        }

        internal bool CreateSecondary(Entity owner, Entity theme, bool leftHandTraffic)
        {
            using var commands = new EntityCommandBuffer(Allocator.TempJob);
            var job = BindJob(typeof(SecondaryLaneSystem), commands, theme, leftHandTraffic);
            var method = job?.GetType().GetMethod("UpdateLanes", Instance);
            if (job == null || method == null) return false;
            method.Invoke(job, new object[] { EntityManager.GetChunk(owner), 0 });
            commands.Playback(EntityManager);
            return true;
        }

    }
}
