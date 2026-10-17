using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

// Native primary edge/node lanes and reference maintenance for a private network context.
[DisableAutoCreation]
internal sealed partial class MovableNetworkLanes : SystemBase
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    protected override void OnUpdate() { }

    // Input is a complete network-only context: no building/area owners, and all native
    // mechanism SubObject/PointOfInterest relationships already exist in this private world.
    internal void CalculateNetwork(TerrainHeightData terrain, Entity theme, bool leftHandTraffic,
        RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration, bool loaded)
    {
        RequirePrivateWorld();
        var geometry = World.GetOrCreateSystemManaged<MovableNetworkGeometry>();
        SelectCompositions(leftHandTraffic);
        InitializeCompositions();
        geometry.Calculate(terrain, loaded);
        InitializeNetworkComponents(loaded);
        CreatePrimary(terrain, theme, leftHandTraffic, randomSeed, buildingConfiguration);
        CalculateOverlaps(leftHandTraffic);
        InitializeSignals();
        CreateSecondary(theme, leftHandTraffic);
    }

    internal void InitializeSignals()
    {
        RequirePrivateWorld();
        using var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<TrafficLights>(),
            ComponentType.ReadOnly<Updated>());
        Run(typeof(TrafficLightInitializationSystem), "InitializeTrafficLightsJob", "Execute", query,
            new Dictionary<string, object>());
    }

    internal void CreatePrimary(TerrainHeightData terrain, Entity theme, bool leftHandTraffic,
        RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration)
    {
        if (World == World.DefaultGameObjectInjectionWorld)
            throw new InvalidOperationException("Movable lanes require a private network world.");
        if (!terrain.isCreated)
            throw new ArgumentException("Movable lanes require initialized terrain data.", nameof(terrain));
        EntityManager.CompleteAllTrackedJobs();
        using var owners = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Game.Net.SubLane>(), ComponentType.ReadOnly<Updated>() },
            Any = new[] { ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadOnly<Node>() },
            None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Game.Objects.Transform>() }
        });
        using var lanes = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Owner>() },
            Any = new[] { ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Deleted>() },
            None = new[] { ComponentType.ReadOnly<Game.Net.SecondaryLane>() }
        });
        using var skipped = new NativeQueue<Lane>(Allocator.TempJob);
        using var nodeMap = new NativeHashMap<PathNode, PathNode>(100, Allocator.TempJob);
        var values = new Dictionary<string, object>
        {
            ["m_LeftHandTraffic"] = leftHandTraffic,
            ["m_EditorMode"] = false,
            ["m_RandomSeed"] = randomSeed,
            ["m_DefaultTheme"] = theme,
            ["m_TerrainHeightData"] = terrain,
            ["m_BuildingConfigurationData"] = buildingConfiguration,
            ["m_AppliedTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(),
                ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>()),
            ["m_DeletedTempTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<Deleted>(), ComponentType.ReadWrite<Temp>()),
            ["m_TempOwnerTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<Temp>(), ComponentType.ReadWrite<Owner>()),
            ["m_HideLaneTypes"] = new ComponentTypeSet(ComponentType.ReadWrite<CullingInfo>(),
                ComponentType.ReadWrite<MeshBatch>(), ComponentType.ReadWrite<MeshColor>()),
            ["m_SkipLaneQueue"] = skipped.AsParallelWriter(),
            ["m_PathNodeMap"] = nodeMap
        };
        Run(typeof(Game.Net.LaneSystem), "UpdateLanesJob", "UpdateLanes", owners, values);
        Run(typeof(LaneReferencesSystem), "UpdateLaneReferencesJob", "Execute", lanes, values);
        values["m_SkipLaneQueue"] = skipped;
        Run(typeof(LaneReferencesSystem), "FillNodeMapJob", "Execute", null, values);
        Run(typeof(LaneReferencesSystem), "FixSkippedLanesJob", "Execute", owners, values);
        Run(typeof(LaneReferencesSystem), "UpdateLaneIndicesJob", "Execute", owners, values);
    }

    private void Run(Type system, string jobName, string methodName, EntityQuery? query,
        Dictionary<string, object> values, int? iterations = null)
    {
        using var commands = new EntityCommandBuffer(Allocator.TempJob);
        var type = system.GetNestedType(jobName, BindingFlags.NonPublic)
            ?? throw new MissingMemberException(system.FullName, jobName);
        var job = Activator.CreateInstance(type)!;
        foreach (var field in type.GetFields(Instance))
        {
            object value;
            if (field.Name == "m_CommandBuffer") value = field.FieldType == typeof(EntityCommandBuffer)
                ? (object)commands : commands.AsParallelWriter();
            else if (!values.TryGetValue(field.Name, out value!)) value = Bind(field);
            field.SetValue(job, value);
        }
        // LaneSystem overloads UpdateLanes for a lane list; select the chunk signature explicitly.
        var method = methodName == "UpdateLanes"
            ? type.GetMethod(methodName, Instance, null, new[] { typeof(ArchetypeChunk), typeof(int) }, null)!
            : type.GetMethod(methodName, Instance)!;
        if (iterations.HasValue)
            for (var i = 0; i < iterations.Value; i++) method.Invoke(job, new object[] { i });
        else if (!query.HasValue) method.Invoke(job, null);
        else
        {
            using var chunks = query.Value.ToArchetypeChunkArray(Allocator.TempJob);
            var parameters = method.GetParameters();
            var mask = parameters.Length == 4
                ? Activator.CreateInstance(parameters[3].ParameterType.GetElementType()!) : null;
            for (var i = 0; i < chunks.Length; i++)
                method.Invoke(job, parameters.Length == 4
                    ? new object[] { chunks[i], i, false, mask! } : new object[] { chunks[i], i });
        }
        // No chunk arrays or component handles are reused across structural playback.
        commands.Playback(EntityManager);
    }

    private object Bind(FieldInfo field)
    {
        var type = field.FieldType;
        if (type == typeof(EntityTypeHandle)) return GetEntityTypeHandle();
        if (type.IsGenericType)
        {
            var generic = type.GetGenericTypeDefinition();
            var name = generic == typeof(ComponentLookup<>) ? nameof(Component) :
                generic == typeof(BufferLookup<>) ? nameof(Buffer) :
                generic == typeof(ComponentTypeHandle<>) ? nameof(ComponentHandle) :
                generic == typeof(BufferTypeHandle<>) ? nameof(BufferHandle) : null;
            if (name != null)
                return typeof(MovableNetworkLanes).GetMethod(name, Instance | BindingFlags.DeclaredOnly)!
                    .MakeGenericMethod(type.GenericTypeArguments)
                    .Invoke(this, new object[] { field.IsDefined(typeof(ReadOnlyAttribute), false) })!;
        }
        throw new InvalidOperationException($"Unbound native lane field: {field.DeclaringType?.FullName}.{field.Name}.");
    }

    private ComponentLookup<T> Component<T>(bool readOnly) where T : unmanaged, IComponentData
        => GetComponentLookup<T>(readOnly);
    private BufferLookup<T> Buffer<T>(bool readOnly) where T : unmanaged, IBufferElementData
        => GetBufferLookup<T>(readOnly);
    private ComponentTypeHandle<T> ComponentHandle<T>(bool readOnly) where T : unmanaged, IComponentData
        => GetComponentTypeHandle<T>(readOnly);
    private BufferTypeHandle<T> BufferHandle<T>(bool readOnly) where T : unmanaged, IBufferElementData
        => GetBufferTypeHandle<T>(readOnly);
}
