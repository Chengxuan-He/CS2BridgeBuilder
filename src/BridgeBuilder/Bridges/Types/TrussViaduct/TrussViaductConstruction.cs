using System;
using System.Linq;
using System.Threading.Tasks;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Recorded BXP train variant: a zero-width carrier preserves its native object spacing.
internal static class TrussViaductConstruction
{
    internal const string NativeName = "BXP Truss Viaduct - Train";
    internal const string Icon = "assetdb://global/a1ee65e0add6bd288e7b767c53306935";
    internal const string ResourceAnchorName = "BXP TrussViaductNetPillar";
    internal static float RoadWidth => Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[0];
    internal static float StructureWidth => Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[1];
    internal static int Clearance => Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[0];

    private static float[][] Maps(int index, string kind) => GenerationParameters.Get("TrussViaduct:" + index + kind, () =>
    {
        var key = (index, kind) switch
        {
            (0, "Displacement") => "Types_TrussViaduct_0_Displacement",
            (0, "Frames") => "Types_TrussViaduct_0_Frames",
            (1, "Displacement") => "Types_TrussViaduct_1_Displacement",
            (1, "Frames") => "Types_TrussViaduct_1_Frames",
            (2, "Displacement") => "Types_TrussViaduct_2_Displacement",
            (2, "Frames") => "Types_TrussViaduct_2_Frames",
            (3, "Displacement") => "Types_TrussViaduct_3_Displacement",
            (3, "Frames") => "Types_TrussViaduct_3_Frames",
            (4, "Displacement") => "Types_TrussViaduct_4_Displacement",
            (4, "Frames") => "Types_TrussViaduct_4_Frames",
            (5, "Displacement") => "Types_TrussViaduct_5_Displacement",
            (5, "Frames") => "Types_TrussViaduct_5_Frames"};
        var n = HardcodedNumbers.Load(key);
        var result = new float[n.I32.Length][];
        int offset = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[1];
        for (int i = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[2]; i < result.Length; i++)
        {
            result[i] = new float[n.I32[i]];
            Array.Copy(n.F32, offset, result[i], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[3], result[i].Length);
            offset += result[i].Length;
        }

        return result;
    });
    internal static async Task ApplyAsync(NetGeometryPrefab target, TowerFactory factory, float width)
    {
        float half = (width - RoadWidth) / Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[2];
        Colossal.IO.AssetDatabase.AssetDatabase.global.resources.prefabsMap.TryGetObject("832dd95516847474bb6e013c831b5f7e", out var aggregate);
        target.m_AggregateType = (AggregateNetPrefab)aggregate;
        async Task<RenderPrefab> Render(string sourceName, string suffix, int map, bool lods)
        {
            var source = factory.ResolveOriginal<RenderPrefab>(sourceName);
            var b = source.bounds;
            var a = b.min - new float3(half, Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[4], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[5]);
            var z = b.max + new float3(half, Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[6], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[7]);
            var result = await factory.CreateRecordedRenderAsync(source, target.name + suffix, half, Maps(map, "Displacement"), Maps(map, "Frames"), new Bounds3(math.min(a, z), math.max(a, z)));
            if (lods)
            {
                var sourceLods = source.GetComponent<LodProperties>();
                var l = result.AddComponent<LodProperties>();
                l.m_Bias = sourceLods.m_Bias;
                l.m_ShadowBias = sourceLods.m_ShadowBias;
                l.m_LodMeshes = new[]
                {
                    await Render(sourceLods.m_LodMeshes[Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[9]].name, suffix + "-LOD1", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[10], false),
                    await Render(sourceLods.m_LodMeshes[Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[11]].name, suffix + "-LOD2", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[12], false)
                };
            }

            return result;
        }

        StaticObjectPrefab Object(string suffix, PillarType type, ObjectMeshInfo[] meshes)
        {
            var o = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            o.name = target.name + suffix;
            o.m_Meshes = meshes;
            o.m_Circular = false;
            var p = o.AddComponent<PillarObject>();
            p.m_Type = type;
            p.m_AnchorOffset = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[13];
            p.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[14], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[15]);
            factory.AdoptMovableSections(new PrefabBase[] { o });
            return o;
        }

        ObjectMeshInfo Mesh(RenderPrefab r, float3 p, quaternion q) => new()
        {
            m_Mesh = r,
            m_Position = p,
            m_Rotation = q
        };
        var pier = Object("-TrussPier", (PillarType)Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[16], new[] { Mesh(await Render("BXP TrussViaductNetPillarBase Mesh", "-TrussBase", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[17], false), float3.zero, quaternion.identity), Mesh(await Render("TrussArchBridge01NetPillar Mesh", "-TrussMiddle", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[18], true), float3.zero, quaternion.identity), Mesh(await Render("BXP TrussViaductNetPillarTop Mesh", "-TrussTop", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[19], true), float3.zero, quaternion.identity) });
        var terrain = pier.AddComponent<BuildingTerraformOverride>();
        terrain.m_LevelFrontLeft = terrain.m_LevelFrontRight = terrain.m_LevelBackLeft = terrain.m_LevelBackRight = new float2(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[20]);
        terrain.m_DontRaise = terrain.m_DontLower = true;
        var support = await Render("BXP TrussViaductSupport Mesh", "-TrussSpan", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[21], false);
        var underside = await Render("BXP TrussViaductUnderside Mesh", "-TrussPanels", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[22], false);
        var q = new quaternion(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[3], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[23], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[24], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[4]);
        var span = Object("-TrussSpanObject", (PillarType)Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[25], new[] { Mesh(support, float3.zero, q), Mesh(underside, new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[26], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[5], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[27]), new quaternion(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[6], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[7], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[8], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[9])), Mesh(underside, new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[28], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[10], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[29]), q), Mesh(underside, new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[30], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[11], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[31]), new quaternion(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[12], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[32], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[33], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[13])), Mesh(underside, new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[34], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[14], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[35]), new quaternion(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[15], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[16], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[17], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[18])), Mesh(underside, new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[36], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[19], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[37]), new quaternion(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[20], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[38], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[39], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[21])), Mesh(underside, new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[40], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[22], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[41]), new quaternion(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[23], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[24], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[25], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[26])) });
        var sourcePiece = factory.ResolveOriginal<NetPiecePrefab>("BXP Train Elevated Bottom Piece 0 - Truss Viaduct");
        var piece = await factory.CreateRecordedRenderAsync(sourcePiece, target.name + "-TrussCarrier", Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[42], Maps(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[43], "Displacement"));
        piece.m_Layer = NetPieceLayer.Bottom;
        piece.m_Width = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[44];
        piece.m_Length = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[45];
        piece.m_HeightRange = new Bounds1(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[27], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[28]);
        piece.m_NodeOffset = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[29];
        piece.m_SurfaceHeights = new float4(-Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[30]);
        piece.AddOrGetComponent<AsymmetricPieceMesh>().m_Lengthwise = true;
        NetPieceObjectInfo Placement(bool node) => new()
        {
            m_Object = span,
            m_Position = new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[46], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[47], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[48]),
            m_Rotation = quaternion.identity,
            m_RequireAll = node ? new[]
            {
                NetPieceRequirements.Node
            }

            : Array.Empty<NetPieceRequirements>(),
            m_RequireAny = node ? new[]
            {
                NetPieceRequirements.DeadEnd,
                NetPieceRequirements.Intersection
            }

            : Array.Empty<NetPieceRequirements>(),
            m_RequireNone = node ? Array.Empty<NetPieceRequirements>() : new[]
            {
                NetPieceRequirements.Node
            },
            m_Probability = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[49],
            m_Spacing = new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[50], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[51], node ? Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[52] : Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[53]),
            m_UseCurveRotation = new float2(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[54])
        };
        piece.AddOrGetComponent<NetPieceObjects>().m_PieceObjects = new[]
        {
            Placement(false),
            Placement(true)
        };
        var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
        section.name = target.name + "-TrussCarrierSection";
        section.m_SubSections = Array.Empty<NetSubSectionInfo>();
        section.m_Pieces = new[]
        {
            new NetPieceInfo
            {
                m_Piece = piece,
                m_RequireAll = Array.Empty<NetPieceRequirements>(),
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>()
            }
        };
        factory.AdoptMovableSections(new PrefabBase[] { section });
        var overhead = target.AddOrGetComponent<OverheadNetSections>();
        overhead.m_Sections = new[]
        {
            new NetSectionInfo
            {
                m_Section = section,
                m_Median = true,
                m_RequireAll = new[]
                {
                    NetPieceRequirements.Elevated
                },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>()
            }
        };
        var objects = target.AddOrGetComponent<NetSubObjects>();
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(p => p == null || !p.m_RequireElevated).Concat(new[] { new NetSubObjectInfo { m_Object = pier, m_Position = new float3(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[55], -Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.F32[31], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[56]), m_Rotation = quaternion.identity, m_Placement = NetObjectPlacement.Node, m_RequireElevated = true } }).ToArray();
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.m_SegmentLength = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[57];
        bridge.m_Hanging = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[58];
        bridge.m_ElevationOnWater = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[59];
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[60];
        bridge.m_BuildStyle = (BridgeBuildStyle)Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[61];
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
        var placement = target.AddOrGetComponent<PlaceableNet>();
        placement.m_ElevationRange = new Bounds1(Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[62], Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[63]);
        placement.m_AllowParallelMode = true;
        placement.m_UndergroundPrefab = null;
        placement.m_XPReward = Numeric_Types_TrussViaduct_TrussViaductConstruction_cs.Values.I32[64];
    }
}

// Embedded numerical arrays: Types_TrussViaduct_TrussViaductConstruction_cs
internal static class Numeric_Types_TrussViaduct_TrussViaductConstruction_cs
{
    static Numeric_Types_TrussViaduct_TrussViaductConstruction_cs()
    {
    }

    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_TrussViaduct_TrussViaductConstruction_cs");
}
