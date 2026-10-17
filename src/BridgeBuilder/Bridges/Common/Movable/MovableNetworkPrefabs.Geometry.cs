using System.Linq;
using System.Reflection;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Entities;
using GeometryFlags = Game.Objects.GeometryFlags;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkPrefabs
{
    private static readonly MethodInfo InitializeObjectGeometry = typeof(ObjectInitializeSystem)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(method => method.Name == "InitializePrefab" &&
            method.GetParameters()[0].ParameterType == typeof(ObjectGeometryPrefab));

    private void InitializeMechanismGeometry(StaticObjectPrefab prefab, Entity entity)
    {
        // Invoke only the native object-geometry routine. Running the system's whole query would
        // interpret imported city PrefabData indices in this private registry.
        var system = _target.World.GetOrCreateSystemManaged<ObjectInitializeSystem>();
        _target.CompleteAllTrackedJobs();
        var geometry = _target.GetComponentData<ObjectGeometryData>(entity);
        geometry.m_MinLod = 255;
        geometry.m_Layers = 0;
        var stack = new StackData
        {
            m_FirstBounds = new Bounds1(float.MaxValue, float.MinValue),
            m_MiddleBounds = new Bounds1(float.MaxValue, float.MinValue),
            m_LastBounds = new Bounds1(float.MaxValue, float.MinValue)
        };
        var meshes = _target.AddBuffer<SubMesh>(entity);
        object[] arguments =
        {
            prefab, _target.GetComponentData<PlaceableObjectData>(entity), geometry,
            default(GrowthScaleData), stack, default(QuantityObjectData), default(CreatureData),
            default(LocaleMeshData), meshes, default(DynamicBuffer<SubMeshGroup>),
            default(DynamicBuffer<CharacterElement>), false, false, false, false, false, false
        };
        InitializeObjectGeometry.Invoke(system, arguments);
        geometry = (ObjectGeometryData)arguments[2];
        // Static movable mechanism branch immediately following InitializePrefab in the native
        // OnUpdate: pillars retain collision/attachment behavior; movable pillars are walkable.
        if (_target.HasComponent<PillarData>(entity))
        {
            geometry.m_Flags |= _target.GetComponentData<PillarData>(entity).m_Type == PillarType.Horizontal
                ? GeometryFlags.IgnoreBottomCollision : GeometryFlags.BaseCollision;
            geometry.m_Flags |= GeometryFlags.WalkThrough | GeometryFlags.OccupyZone |
                GeometryFlags.CanSubmerge | GeometryFlags.OptionalAttach;
        }
        else geometry.m_Flags |= GeometryFlags.OccupyZone;
        _target.SetComponentData(entity, geometry);
        _target.AddComponentData(entity, (StackData)arguments[4]);
        _target.AddComponentData(entity, (LocaleMeshData)arguments[7]);
    }
}
