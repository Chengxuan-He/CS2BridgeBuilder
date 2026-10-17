using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BridgeBuilder.Runtime;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Read-only import of selected-road prefab data into the caller's disposable network world.
// This is not asset persistence: original assets and CIDs remain unchanged.
internal sealed partial class MovableNetworkPrefabs
{
    private readonly EntityManager _source;
    private readonly EntityManager _target;
    private readonly PrefabSystem _prefabs;
    private readonly string _owner;
    private readonly Dictionary<Entity, Entity> _imported = new();
    private readonly Dictionary<Entity, PrefabBase> _originals = new();
    private readonly Dictionary<EntityArchetype, EntityArchetype> _archetypes = new();
    private static readonly MethodInfo CreateArchetype = typeof(EntityManager).GetMethod("CreateArchetype",
        new[] { typeof(NativeArray<ComponentType>) })!;

    internal MovableNetworkPrefabs(World target, string owner)
    {
        var source = World.DefaultGameObjectInjectionWorld;
        if (target == source) throw new InvalidOperationException("Prefab import requires a private network world.");
        _source = source.EntityManager;
        _target = target.EntityManager;
        _prefabs = source.GetExistingSystemManaged<PrefabSystem>();
        _owner = owner;
    }

    internal PrefabBase Original(Entity imported) => _originals[imported];

    internal BuildingConfigurationData ImportConfiguration(BuildingConfigurationData source)
        => Remap(source);

    // Stage an already assembled mechanism without registering it in the city or resolving a
    // live DLC prototype. Native geometry is calculated from its generated render graph.
    internal Entity StageMechanism(StaticObjectPrefab prefab)
    {
        CheckGraph(prefab);
        var components = new List<ComponentBase>();
        prefab.GetComponents(components);
        if (!components.Exists(component => component is MoveableBridge) ||
            !components.Exists(component => component is NetObject))
            throw new InvalidOperationException("A staged mechanism requires active native MoveableBridge and NetObject components.");
        if (prefab.m_Meshes != null)
            foreach (var mesh in prefab.m_Meshes) StageRender((RenderPrefab)mesh.m_Mesh);
        var types = new HashSet<ComponentType>();
        foreach (var component in components) component.GetArchetypeComponents(types);
        types.Add(ComponentType.ReadWrite<Created>());
        types.Add(ComponentType.ReadWrite<Updated>());
        var array = new ComponentType[types.Count];
        types.CopyTo(array);
        using var nativeTypes = new NativeArray<ComponentType>(array, Allocator.Temp);
        var archetype = (EntityArchetype)CreateArchetype.Invoke(_target, new object[] { nativeTypes });
        var entity = _target.CreateEntity();
        _target.AddComponentData(entity, new ObjectData { m_Archetype = archetype });
        _target.AddComponentData(entity, default(ObjectGeometryData));
        _target.AddComponentData(entity, default(PlaceableObjectData));
        _target.AddComponentData(entity, default(NetObjectData));
        _target.AddComponentData(entity, default(MoveableBridgeData));
        if (components.Exists(component => component is PillarObject))
            _target.AddComponentData(entity, default(PillarData));
        // Only the native metadata consumed by the private network calculation is initialized.
        // Native component order matters, particularly for placement offsets and flags.
        foreach (var component in components)
            if (component is PillarObject or StandingObject or NetObject or MoveableBridge or
                PlaceableObject or AttachedObject or FloatingObject or HoveringObject or
                ShorelineObject or UnderwaterObject or UniqueObject or SubObjectDefaultProbability)
                component.Initialize(_target, entity);
        InitializeMechanismGeometry(prefab, entity);
        _originals.Add(entity, prefab);
        return entity;
    }

    private void CheckGraph(PrefabBase prefab)
    {
        var visited = new HashSet<PrefabBase>();
        Visit(prefab);
        void Visit(PrefabBase current)
        {
            CheckSource(current);
            if (!visited.Add(current)) return;
            var parts = new List<ComponentBase>();
            current.GetComponents(parts);
            var dependencies = new List<PrefabBase>();
            foreach (var part in parts) part.GetDependencies(dependencies);
            // Native dependency enumeration includes absent optional references.
            foreach (var dependency in dependencies) if (dependency != null) Visit(dependency);
        }
    }

    internal Entity Import(Entity source)
    {
        if (source == Entity.Null) return Entity.Null;
        if (_imported.TryGetValue(source, out var existing)) return existing;
        if (!_source.Exists(source) || !_prefabs.TryGetPrefab<PrefabBase>(source, out var prefab) || prefab == null)
            throw new InvalidOperationException("Network prefab import encountered an invalid source prefab entity.");
        CheckSource(prefab);
        // Cache identity first so cycles preserve one private entity per source without recursion loops.
        var target = _target.CreateEntity();
        _imported.Add(source, target);
        _originals.Add(target, prefab);
        Copy<PrefabData>(source, target);
        Copy<NetData>(source, target);
        Copy<NetGeometryData>(source, target);
        Copy<NetObjectData>(source, target);
        Copy<NetCustomColorData>(source, target);
        Copy<NetCompositionData>(source, target);
        Copy<RoadComposition>(source, target);
        Copy<TrackComposition>(source, target);
        Copy<PathwayComposition>(source, target);
        Copy<TaxiwayComposition>(source, target);
        Copy<WaterwayComposition>(source, target);
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
        Copy<ObjectGeometryData>(source, target);
        Copy<ObjectData>(source, target);
        Copy<PlaceableObjectData>(source, target);
        Copy<PlaceableNetData>(source, target);
        Copy<BuildingData>(source, target);
        Copy<MoveableBridgeData>(source, target);
        Copy<NetPieceData>(source, target);
        Copy<NetVertexMatchData>(source, target);
        Copy<StreetLightData>(source, target);
        Copy<TrafficSignData>(source, target);
        Copy<TrafficLightData>(source, target);
        Copy<LaneDirectionData>(source, target);
        Copy<UtilityObjectData>(source, target);
        Copy<RoadData>(source, target);
        Copy<TrackData>(source, target);
        Copy<WaterwayData>(source, target);
        Copy<PathwayData>(source, target);
        Copy<TaxiwayData>(source, target);
        Copy<NetTerrainData>(source, target);
        Copy<BridgeData>(source, target);
        Copy<MeshData>(source, target);
        Copy<PlaceableNetPieceData>(source, target);
        Copy<NetCrosswalkData>(source, target);
        CopyBuffer<NetGeometrySection>(source, target);
        CopyBuffer<NetGeometryEdgeState>(source, target);
        CopyBuffer<NetGeometryNodeState>(source, target);
        CopyBuffer<FixedNetElement>(source, target);
        // Native state selection owns this world's cache. Never import city composition entities.
        if (_target.HasComponent<NetGeometryData>(target))
            _target.AddBuffer<NetGeometryComposition>(target);
        CopyBuffer<NetSubSection>(source, target);
        CopyBuffer<NetSectionPiece>(source, target);
        CopyBuffer<NetPieceLane>(source, target);
        CopyBuffer<NetPieceObject>(source, target);
        CopyBuffer<NetPieceArea>(source, target);
        CopyBuffer<NetCompositionPiece>(source, target);
        CopyBuffer<NetCompositionLane>(source, target);
        CopyBuffer<NetCompositionCrosswalk>(source, target);
        CopyBuffer<AuxiliaryNetLane>(source, target);
        CopyBuffer<DefaultNetLane>(source, target);
        CopyBuffer<SecondaryNetLane>(source, target);
        CopyBuffer<PlaceholderObjectElement>(source, target);
        CopyBuffer<ObjectRequirementElement>(source, target);
        CopyBuffer<Game.Prefabs.SubLane>(source, target);
        _target.SetComponentEnabled<PrefabData>(target, _source.IsComponentEnabled<PrefabData>(source));
        return target;
    }

    internal void CheckSource(PrefabBase prefab)
    {
        if (prefab == null || BridgeAssetInfo.IsOtherBridgePrefab(prefab.asset?.path, _owner,
            Path.Combine(Application.persistentDataPath, "ImportedData")))
            throw new InvalidOperationException("Network conversion cannot read a missing or foreign bridge-owned source.");
    }

    // All copied records have direct Entity/EntityArchetype fields. Numeric/nested geometric
    // fields are copied intact; no city entity, shared asset or source archetype is mutated.
    internal T Remap<T>(T value) where T : struct
    {
        object boxed = value;
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.FieldType == typeof(Entity))
                field.SetValue(boxed, Import((Entity)field.GetValue(boxed)));
            else if (field.FieldType == typeof(EntityArchetype))
            {
                var original = (EntityArchetype)field.GetValue(boxed);
                if (!original.Valid) continue;
                if (!_archetypes.TryGetValue(original, out var archetype))
                {
                    using var types = original.GetComponentTypes(Allocator.Temp);
                    archetype = (EntityArchetype)CreateArchetype.Invoke(_target, new object[] { types });
                    _archetypes.Add(original, archetype);
                }
                field.SetValue(boxed, archetype);
            }
        }
        return (T)boxed;
    }

    private void Copy<T>(Entity source, Entity target) where T : unmanaged, IComponentData
    {
        if (_source.HasComponent<T>(source))
            _target.AddComponentData(target, Remap(_source.GetComponentData<T>(source)));
    }

    private void CopyBuffer<T>(Entity source, Entity target) where T : unmanaged, IBufferElementData
    {
        if (!_source.HasBuffer<T>(source)) return;
        using var snapshot = _source.GetBuffer<T>(source, true).ToNativeArray(Allocator.Temp);
        var remapped = new T[snapshot.Length];
        for (var i = 0; i < snapshot.Length; i++) remapped[i] = Remap(snapshot[i]);
        // Recursive import can change archetypes, so obtain the buffer only after remapping.
        var output = _target.AddBuffer<T>(target);
        foreach (var item in remapped) output.Add(item);
    }
}
