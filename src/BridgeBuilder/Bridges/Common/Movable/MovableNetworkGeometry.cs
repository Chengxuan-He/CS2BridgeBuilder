using System;
using System.Reflection;
using Game.Net;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

// Executes the installed native geometry pipeline on a prepared, private network world.
// It does not create city entities or substitute straight segments for node geometry.
[DisableAutoCreation]
internal sealed partial class MovableNetworkGeometry : SystemBase
{
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    protected override void OnUpdate() { }

    // The caller owns the terrain arrays and must complete their writer before this call.
    // All network entities and their referenced composition/prefab data belong to this World.
    internal void Calculate(TerrainHeightData terrain, bool loaded)
    {
        if (World == World.DefaultGameObjectInjectionWorld)
            throw new InvalidOperationException("Movable geometry requires a private network world.");
        if (!terrain.isCreated)
            throw new ArgumentException("Movable geometry requires initialized terrain height data.", nameof(terrain));
        EntityManager.CompleteAllTrackedJobs();
        using var edgesQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<EdgeGeometry>());
        using var nodesQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NodeGeometry>());
        using var edgeArray = edgesQuery.ToEntityArray(Allocator.TempJob);
        using var edges = new NativeList<Entity>(edgeArray.Length, Allocator.TempJob);
        edges.AddRange(edgeArray);
        using var chunks = nodesQuery.ToArchetypeChunkArray(Allocator.TempJob);
        using var heights = new NativeParallelHashMap<int2, float4>(0, Allocator.TempJob);
        var intersection = typeof(GeometrySystem).GetNestedType("IntersectionData", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(typeof(GeometrySystem).FullName, "IntersectionData");
        var buffer = typeof(MovableNetworkGeometry)
            .GetMethod(nameof(AllocateIntersections), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(intersection).Invoke(null, null)!;
        try
        {
            Run("AllocateBuffersJob", 0);
            Run("InitializeNodeGeometryJob", 1);
            Run("CalculateEdgeGeometryJob", 2);
            Run("FlattenNodeGeometryJob", 1);
            Run("FinishEdgeGeometryJob", 2);
            Run("CalculateNodeGeometryJob", 2, 0);
            Run("CalculateNodeGeometryJob", 2, 1);
            Run("CalculateIntersectionGeometryJob", 2);
            Run("CopyNodeGeometryJob", 2);
            Run("UpdateNodeGeometryJob", 1);
        }
        finally { ((IDisposable)buffer).Dispose(); }

        void Run(string name, int mode, int iteration = 0)
        {
            var type = typeof(GeometrySystem).GetNestedType(name, BindingFlags.NonPublic)
                ?? throw new MissingMemberException(typeof(GeometrySystem).FullName, name);
            var job = Activator.CreateInstance(type)!;
            foreach (var field in type.GetFields(Instance))
            {
                object value;
                switch (field.Name)
                {
                    case "m_Entities":
                        value = field.FieldType == typeof(NativeList<Entity>) ? (object)edges : edges.AsArray();
                        break;
                    case "m_IntersectionData":
                    case "m_BufferedData": value = buffer; break;
                    case "m_EdgeHeightMap":
                        value = field.FieldType == typeof(NativeParallelHashMap<int2, float4>)
                            ? (object)heights : heights.AsParallelWriter();
                        break;
                    case "m_TerrainBounds": value = TerrainUtils.GetBounds(ref terrain); break;
                    case "m_TerrainHeightData": value = terrain; break;
                    case "m_Loaded": value = loaded; break;
                    case "m_IterationIndex": value = iteration; break;
                    default: value = Bind(field); break;
                }
                field.SetValue(job, value);
            }
            var execute = type.GetMethod("Execute", Instance)!;
            if (mode == 0) execute.Invoke(job, null);
            else if (mode == 1)
            {
                var maskType = execute.GetParameters()[3].ParameterType.GetElementType()!;
                var mask = Activator.CreateInstance(maskType)!;
                for (var i = 0; i < chunks.Length; i++)
                    execute.Invoke(job, new object[] { chunks[i], i, false, mask });
            }
            else
                for (var i = 0; i < edges.Length; i++) execute.Invoke(job, new object[] { i });
        }
    }

    private static object AllocateIntersections<T>() where T : unmanaged
        => new NativeList<T>(0, Allocator.TempJob);

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
                return typeof(MovableNetworkGeometry).GetMethod(name, Instance | BindingFlags.DeclaredOnly)!
                    .MakeGenericMethod(type.GenericTypeArguments)
                    .Invoke(this, new object[] { field.IsDefined(typeof(ReadOnlyAttribute), false) })!;
        }
        throw new InvalidOperationException($"Unbound native geometry field: {field.DeclaringType?.FullName}.{field.Name}.");
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
