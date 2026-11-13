using System;
using System.Reflection;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkLanes
{
    internal Entity PreviewComposition(Entity prefab, NetCompositionData data)
    {
        var geometry = EntityManager.GetComponentData<NetGeometryData>(prefab);
        var entity = CreateNativeEntity((data.m_Flags.m_General & CompositionFlags.General.Edge) != 0
            ? geometry.m_EdgeCompositionArchetype : geometry.m_NodeCompositionArchetype);
        EntityManager.SetComponentData(entity, new PrefabRef(prefab));
        EntityManager.SetComponentData(entity, data);
        return entity;
    }

    internal void InitializePreviewCompositions() => InitializeCompositions();

    internal async System.Threading.Tasks.Task FinishPreviewNetworkAsync(Game.Simulation.TerrainHeightData terrain,
        Entity theme, bool leftHandTraffic, BuildingConfigurationData configuration)
    {
        await GenerationWork.Next("preview native network initialization");
        InitializeNetworkComponents(true);
        await GenerationWork.Next("preview native primary lanes");
        CreatePrimary(terrain, theme, leftHandTraffic, default, configuration);
        await GenerationWork.Next("preview native lane overlaps");
        CalculateOverlaps(leftHandTraffic);
        await GenerationWork.Next("preview native traffic signals");
        InitializeSignals();
        await GenerationWork.Next("preview native secondary lanes");
        CreateSecondary(theme, leftHandTraffic);
    }

    internal CompositionFlags PreviewNodeFlags(Entity edge, Entity node, Entity prefab,
        CompositionFlags flags, bool isStart, bool leftHandTraffic)
    {
        RequirePrivateWorld();
        var type = typeof(CompositionSelectSystem).GetNestedType("SelectCompositionJob", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(typeof(CompositionSelectSystem).FullName, "SelectCompositionJob");
        var job = Activator.CreateInstance(type)!;
        foreach (var field in type.GetFields(Instance))
        {
            if (field.Name == "m_LeftHandTraffic") field.SetValue(job, leftHandTraffic);
            else if (field.FieldType.IsGenericType &&
                (field.FieldType.GetGenericTypeDefinition() == typeof(ComponentLookup<>) ||
                 field.FieldType.GetGenericTypeDefinition() == typeof(BufferLookup<>)))
                field.SetValue(job, Bind(field));
        }
        var method = type.GetMethod("GetNodeFlags", Instance)
            ?? throw new MissingMemberException(type.FullName, "GetNodeFlags");
        var args = new object[] { edge, node, Entity.Null, prefab,
            EntityManager.GetComponentData<NetData>(prefab), EntityManager.GetComponentData<NetGeometryData>(prefab),
            flags, EntityManager.GetComponentData<Curve>(edge), isStart, default(CompositionFlags), default(CompositionFlags) };
        return (CompositionFlags)method.Invoke(job, args)!;
    }
}
