using System;
using System.Linq;
using System.Threading.Tasks;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static class ArtDecoViaductConstruction
{
    internal const string NativeName = "BXP Art Deco Viaduct - 6 Lanes";
    internal const string ResourceAnchorName = "BXP Art Deco Pylon";
    internal const string Icon = "assetdb://global/9eb341f8d09f1db23c86a0d75b585096";
    internal static float RoadWidth => Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[0];
    internal static float StructureWidth => Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[1];
    internal static int Clearance => Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.I32[0];

    internal static async Task ApplyAsync(NetGeometryPrefab target, TowerFactory factory, float width)
    {
        var delta = (width - RoadWidth) / Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[2];
        var source = factory.ResolveOriginal<StaticObjectPrefab>(ResourceAnchorName);
        var input = factory.ResolveOriginal<RenderPrefab>("ArtDecoPylon Mesh");
        var box = input.bounds;
        box.min.x -= delta;
        box.max.x += delta;
        box = new Bounds3(math.min(box.min, box.max), math.max(box.min, box.max));
        var render = await factory.CreateRecordedRenderAsync(input, target.name + "-ArtDecoPylonMesh", delta, new[] { HardcodedNumbers.Load("Types_ArtDecoViaduct_PylonDisplacements").F32 }, new[] { HardcodedNumbers.Load("Types_ArtDecoViaduct_PylonFrameX").F32 }, placementBounds: box, xShearYCoefficients: new[] { HardcodedNumbers.Load("Types_ArtDecoViaduct_PylonFrameY").F32 }, xShearZCoefficients: new[] { HardcodedNumbers.Load("Types_ArtDecoViaduct_PylonFrameZ").F32 });
        var pylon = (StaticObjectPrefab)source.Clone(target.name + "-ArtDecoPylon");
        foreach (var component in pylon.components)
            if (component != null)
                component.prefab = pylon;
        pylon.isDirty = true;
        pylon.m_Meshes = new[]
        {
            new ObjectMeshInfo
            {
                m_Mesh = render,
                m_Position = float3.zero,
                m_Rotation = quaternion.identity
            }
        };
        factory.AdoptMovableSections(new PrefabBase[] { pylon });
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.active = true;
        bridge.m_SegmentLength = Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[3];
        bridge.m_Hanging = Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[4];
        bridge.m_ElevationOnWater = Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[5];
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = BridgeWaterFlow.Any;
        bridge.m_BuildStyle = BridgeBuildStyle.Elevated;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
        var place = target.AddOrGetComponent<PlaceableNet>();
        place.active = true;
        place.m_ElevationRange = new Bounds1(Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[6], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[7]);
        place.m_AllowParallelMode = true;
        place.m_UndergroundPrefab = null;
        place.m_XPReward = Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.I32[1];
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>("Bridge");
        var objects = target.AddOrGetComponent<NetSubObjects>();
        var retained = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(x => x == null || !x.m_RequireElevated);
        var wall = factory.ResolveOriginal<StaticObjectPrefab>("WallLightTwowayFarRange02");
        var ground = factory.ResolveOriginal<StaticObjectPrefab>("GroundLightMidRange01");
        objects.m_SubObjects = retained.Concat(new[] { Bind(pylon, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[8], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[9], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[10], quaternion.identity), Bind(wall, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[11] + delta, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[12], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[13], new quaternion(Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[14], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[15], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[16], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[17])), Bind(wall, -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[18] - delta, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[19], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[20], new quaternion(Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[21], -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[22], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[23], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[24])), Bind(ground, -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[25] - delta, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[26], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[27], new quaternion(Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[28], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[29], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[30], -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[31])), Bind(ground, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[32] + delta, Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[33], Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[34], new quaternion(Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[35], -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[36], -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[37], -Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs.Values.F32[38])) }).ToArray();
        objects.active = true;
    // The source sidewalk piece is ordinary paving with road street lights and traffic props.
    // The selected elevated road supplies those lanes, sidewalks, markings and street lights.
    }

    private static NetSubObjectInfo Bind(ObjectPrefab item, float x, float y, float z, quaternion rotation) => new()
    {
        m_Object = item,
        m_Position = new float3(x, y, z),
        m_Rotation = rotation,
        m_Placement = NetObjectPlacement.Node,
        m_RequireElevated = true
    };
}

// Embedded numerical arrays: Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs
internal static class Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs
{
    static Numeric_Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs()
    {
    }

    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_ArtDecoViaduct_ArtDecoViaductConstruction_cs");
}
