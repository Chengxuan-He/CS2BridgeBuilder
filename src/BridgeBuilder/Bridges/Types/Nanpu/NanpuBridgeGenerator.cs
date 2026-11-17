using System;
using System.Linq;
using System.Threading.Tasks;
using Colossal.Mathematics;
using Colossal.IO.AssetDatabase;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class NanpuBridgeGenerator : BridgeGeneratorBase
{
    internal NanpuBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Nanpu";
    protected override bool HasRecordedConstruction => true;
    internal override bool SelectedRoadUnlocks => true;

    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant, TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    {
        var half = (targetWidth - NanpuConstruction.RoadWidth) * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[0];
        var parts = new RenderPrefab[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[0]][];
        for (var part = Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[1]; part < parts.Length; part++)
        {
            var renders = parts[part] = new RenderPrefab[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[2]];
            var rotation = NanpuConstruction.Rotation(part);
            var sourceFrame = Matrix4x4.Rotate(new Quaternion(Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[1], Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[2], -rotation.value.z, rotation.value.w));
            for (var lod = Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[3]; lod < renders.Length; lod++)
            {
                var source = factory.ResolveOriginal<RenderPrefab>(lod == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[4] ? NanpuConstruction.ResourceAnchorName : "nanpudaqiaoLOD" + lod + " Mesh");
                var name = target.name + " Nanpu-" + part + "-LOD" + lod;
                var receiving = part == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[5] ? await factory.CreateRecordedNetworkSurfacesAsync(source, name, Array.Empty<Color>()) : null;
                renders[lod] = await factory.CreateRecordedRenderAsync(source, name, NanpuConstruction.Side(part) == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[3] ? half : Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[4], new[] { NanpuConstruction.Data(lod, part, "Displacement").F32 }, new[] { NanpuConstruction.Data(lod, part, "Frames").F32 }, xShearYCoefficients: new[] { NanpuConstruction.Data(lod, part, "ShearY").F32 }, xShearZCoefficients: new[] { NanpuConstruction.Data(lod, part, "ShearZ").F32 }, recordedOrigins: new[] { NanpuConstruction.Origins(lod, part) }, sourceFrame: sourceFrame, sourceTriangleVertices: new[] { NanpuConstruction.Data(lod, part, "SourceTriangles").I32 }, sourceTriangleWeights: new[] { NanpuConstruction.Data(lod, part, "Weights").F32 }, explicitTriangleIndices: new[] { NanpuConstruction.Data(lod, part, "Triangles").I32 }, sourceFaceIndices: new[] { NanpuConstruction.Data(lod, part, "SourceFaces").I32 }, renderType: part == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[6] && lod == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[7] ? typeof(NetPiecePrefab) : typeof(RenderPrefab), receivingSurfaces: receiving);
            }

            var original = factory.ResolveOriginal<RenderPrefab>(NanpuConstruction.ResourceAnchorName).GetComponent<LodProperties>();
            var levels = renders[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[8]].AddComponent<LodProperties>();
            levels.m_Bias = original.m_Bias;
            levels.m_ShadowBias = original.m_ShadowBias;
            levels.m_LodMeshes = new[]
            {
                renders[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[9]],
                renders[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[10]]
            };
        }

        var piece = (NetPiecePrefab)parts[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[11]][Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[12]];
        piece.m_Layer = NetPieceLayer.Top;
        piece.m_Width = targetWidth + NanpuConstruction.StructureWidth - NanpuConstruction.RoadWidth;
        piece.m_Length = NanpuConstruction.Length;
        piece.m_NodeOffset = Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[5];
        piece.m_HeightRange = new Bounds1(Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[6], NanpuConstruction.Values.F32[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[13]]);
        piece.AddComponent<NetPieceTiling>().m_DisableTextureTiling = true;
        var towers = new StaticObjectPrefab[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[14]];
        for (var role = Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[15]; role < towers.Length; role++)
        {
            var tower = towers[role] = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            tower.name = target.name + " NanpuPylon" + role;
            tower.AddComponent<UnderwaterObject>().m_AllowDryland = true;
            tower.AddComponent<PillarObject>().m_Type = PillarType.None;
            tower.m_Meshes = new[]
            {
                new ObjectMeshInfo
                {
                    m_Mesh = parts[role + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[16]][Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[17]],
                    m_Rotation = quaternion.identity
                }
            };
        }

        var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
        section.name = target.name + " NanpuCableSection";
        section.m_SubSections = Array.Empty<NetSubSectionInfo>();
        section.m_Pieces = new[]
        {
            new NetPieceInfo
            {
                m_Piece = piece,
                m_RequireAll = new[]
                {
                    NetPieceRequirements.Edge
                },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>()
            }
        };
        factory.AdoptMovableSections(new PrefabBase[] { section }.Concat(towers).ToArray());
        target.AddOrGetComponent<OverheadNetSections>().m_Sections = new[]
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
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(item => item == null || !item.m_RequireElevated).Concat(towers.Select((tower, role) => new NetSubObjectInfo { m_Object = tower, m_Placement = NetObjectPlacement.EdgeMiddle, // Root Y remains at the road datum: changing it also changes native collision elevation.
        m_Position = new float3(-NanpuConstruction.Side(role + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[18]) * half, Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[7], Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[8]), m_Rotation = math.mul(new quaternion(Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[9], Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[10], Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[11], Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[12]), NanpuConstruction.Rotation(role + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[19])) })).ToArray();
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>("Bridge");
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.m_SegmentLength = NanpuConstruction.Length;
        bridge.m_Hanging = Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[13];
        bridge.m_CanCurve = false;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = BridgeWaterFlow.Any;
        bridge.m_BuildStyle = BridgeBuildStyle.Elevated;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }
}

internal static class NanpuConstruction
{
    internal const string NativeName = "上海南浦大桥";
    internal const string ResourceAnchorName = "nanpudaqiao Mesh";
    internal const string Icon = "coui://bridgebuilderui/BridgeNanpu.svg";
    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_Nanpu_Parameters");
    internal static float RoadWidth => Values.F32[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[20]];
    internal static float StructureWidth => Values.F32[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[21]];
    internal static float Length => Values.F32[Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[22]];

    internal static HardcodedNumbers Data(int lod, int part, string kind)
    {
        var key = (lod, part, kind) switch
        {
            (0, 0, "Displacement") => "Types_Nanpu_0_0_Displacement",
            (0, 0, "Frames") => "Types_Nanpu_0_0_Frames",
            (0, 0, "Origins") => "Types_Nanpu_0_0_Origins",
            (0, 0, "ShearY") => "Types_Nanpu_0_0_ShearY",
            (0, 0, "ShearZ") => "Types_Nanpu_0_0_ShearZ",
            (0, 0, "SourceFaces") => "Types_Nanpu_0_0_SourceFaces",
            (0, 0, "SourceTriangles") => "Types_Nanpu_0_0_SourceTriangles",
            (0, 0, "Triangles") => "Types_Nanpu_0_0_Triangles",
            (0, 0, "Weights") => "Types_Nanpu_0_0_Weights",
            (0, 1, "Displacement") => "Types_Nanpu_0_1_Displacement",
            (0, 1, "Frames") => "Types_Nanpu_0_1_Frames",
            (0, 1, "Origins") => "Types_Nanpu_0_1_Origins",
            (0, 1, "ShearY") => "Types_Nanpu_0_1_ShearY",
            (0, 1, "ShearZ") => "Types_Nanpu_0_1_ShearZ",
            (0, 1, "SourceFaces") => "Types_Nanpu_0_1_SourceFaces",
            (0, 1, "SourceTriangles") => "Types_Nanpu_0_1_SourceTriangles",
            (0, 1, "Triangles") => "Types_Nanpu_0_1_Triangles",
            (0, 1, "Weights") => "Types_Nanpu_0_1_Weights",
            (0, 2, "Displacement") => "Types_Nanpu_0_2_Displacement",
            (0, 2, "Frames") => "Types_Nanpu_0_2_Frames",
            (0, 2, "Origins") => "Types_Nanpu_0_2_Origins",
            (0, 2, "ShearY") => "Types_Nanpu_0_2_ShearY",
            (0, 2, "ShearZ") => "Types_Nanpu_0_2_ShearZ",
            (0, 2, "SourceFaces") => "Types_Nanpu_0_2_SourceFaces",
            (0, 2, "SourceTriangles") => "Types_Nanpu_0_2_SourceTriangles",
            (0, 2, "Triangles") => "Types_Nanpu_0_2_Triangles",
            (0, 2, "Weights") => "Types_Nanpu_0_2_Weights",
            (0, 3, "Displacement") => "Types_Nanpu_0_3_Displacement",
            (0, 3, "Frames") => "Types_Nanpu_0_3_Frames",
            (0, 3, "Origins") => "Types_Nanpu_0_3_Origins",
            (0, 3, "ShearY") => "Types_Nanpu_0_3_ShearY",
            (0, 3, "ShearZ") => "Types_Nanpu_0_3_ShearZ",
            (0, 3, "SourceFaces") => "Types_Nanpu_0_3_SourceFaces",
            (0, 3, "SourceTriangles") => "Types_Nanpu_0_3_SourceTriangles",
            (0, 3, "Triangles") => "Types_Nanpu_0_3_Triangles",
            (0, 3, "Weights") => "Types_Nanpu_0_3_Weights",
            (0, 4, "Displacement") => "Types_Nanpu_0_4_Displacement",
            (0, 4, "Frames") => "Types_Nanpu_0_4_Frames",
            (0, 4, "Origins") => "Types_Nanpu_0_4_Origins",
            (0, 4, "ShearY") => "Types_Nanpu_0_4_ShearY",
            (0, 4, "ShearZ") => "Types_Nanpu_0_4_ShearZ",
            (0, 4, "SourceFaces") => "Types_Nanpu_0_4_SourceFaces",
            (0, 4, "SourceTriangles") => "Types_Nanpu_0_4_SourceTriangles",
            (0, 4, "Triangles") => "Types_Nanpu_0_4_Triangles",
            (0, 4, "Weights") => "Types_Nanpu_0_4_Weights",
            (0, 5, "Displacement") => "Types_Nanpu_0_5_Displacement",
            (0, 5, "Frames") => "Types_Nanpu_0_5_Frames",
            (0, 5, "Origins") => "Types_Nanpu_0_5_Origins",
            (0, 5, "ShearY") => "Types_Nanpu_0_5_ShearY",
            (0, 5, "ShearZ") => "Types_Nanpu_0_5_ShearZ",
            (0, 5, "SourceFaces") => "Types_Nanpu_0_5_SourceFaces",
            (0, 5, "SourceTriangles") => "Types_Nanpu_0_5_SourceTriangles",
            (0, 5, "Triangles") => "Types_Nanpu_0_5_Triangles",
            (0, 5, "Weights") => "Types_Nanpu_0_5_Weights",
            (0, 6, "Displacement") => "Types_Nanpu_0_6_Displacement",
            (0, 6, "Frames") => "Types_Nanpu_0_6_Frames",
            (0, 6, "Origins") => "Types_Nanpu_0_6_Origins",
            (0, 6, "ShearY") => "Types_Nanpu_0_6_ShearY",
            (0, 6, "ShearZ") => "Types_Nanpu_0_6_ShearZ",
            (0, 6, "SourceFaces") => "Types_Nanpu_0_6_SourceFaces",
            (0, 6, "SourceTriangles") => "Types_Nanpu_0_6_SourceTriangles",
            (0, 6, "Triangles") => "Types_Nanpu_0_6_Triangles",
            (0, 6, "Weights") => "Types_Nanpu_0_6_Weights",
            (0, 7, "Displacement") => "Types_Nanpu_0_7_Displacement",
            (0, 7, "Frames") => "Types_Nanpu_0_7_Frames",
            (0, 7, "Origins") => "Types_Nanpu_0_7_Origins",
            (0, 7, "ShearY") => "Types_Nanpu_0_7_ShearY",
            (0, 7, "ShearZ") => "Types_Nanpu_0_7_ShearZ",
            (0, 7, "SourceFaces") => "Types_Nanpu_0_7_SourceFaces",
            (0, 7, "SourceTriangles") => "Types_Nanpu_0_7_SourceTriangles",
            (0, 7, "Triangles") => "Types_Nanpu_0_7_Triangles",
            (0, 7, "Weights") => "Types_Nanpu_0_7_Weights",
            (0, 8, "Displacement") => "Types_Nanpu_0_8_Displacement",
            (0, 8, "Frames") => "Types_Nanpu_0_8_Frames",
            (0, 8, "Origins") => "Types_Nanpu_0_8_Origins",
            (0, 8, "ShearY") => "Types_Nanpu_0_8_ShearY",
            (0, 8, "ShearZ") => "Types_Nanpu_0_8_ShearZ",
            (0, 8, "SourceFaces") => "Types_Nanpu_0_8_SourceFaces",
            (0, 8, "SourceTriangles") => "Types_Nanpu_0_8_SourceTriangles",
            (0, 8, "Triangles") => "Types_Nanpu_0_8_Triangles",
            (0, 8, "Weights") => "Types_Nanpu_0_8_Weights",
            (1, 0, "Displacement") => "Types_Nanpu_1_0_Displacement",
            (1, 0, "Frames") => "Types_Nanpu_1_0_Frames",
            (1, 0, "Origins") => "Types_Nanpu_1_0_Origins",
            (1, 0, "ShearY") => "Types_Nanpu_1_0_ShearY",
            (1, 0, "ShearZ") => "Types_Nanpu_1_0_ShearZ",
            (1, 0, "SourceFaces") => "Types_Nanpu_1_0_SourceFaces",
            (1, 0, "SourceTriangles") => "Types_Nanpu_1_0_SourceTriangles",
            (1, 0, "Triangles") => "Types_Nanpu_1_0_Triangles",
            (1, 0, "Weights") => "Types_Nanpu_1_0_Weights",
            (1, 1, "Displacement") => "Types_Nanpu_1_1_Displacement",
            (1, 1, "Frames") => "Types_Nanpu_1_1_Frames",
            (1, 1, "Origins") => "Types_Nanpu_1_1_Origins",
            (1, 1, "ShearY") => "Types_Nanpu_1_1_ShearY",
            (1, 1, "ShearZ") => "Types_Nanpu_1_1_ShearZ",
            (1, 1, "SourceFaces") => "Types_Nanpu_1_1_SourceFaces",
            (1, 1, "SourceTriangles") => "Types_Nanpu_1_1_SourceTriangles",
            (1, 1, "Triangles") => "Types_Nanpu_1_1_Triangles",
            (1, 1, "Weights") => "Types_Nanpu_1_1_Weights",
            (1, 2, "Displacement") => "Types_Nanpu_1_2_Displacement",
            (1, 2, "Frames") => "Types_Nanpu_1_2_Frames",
            (1, 2, "Origins") => "Types_Nanpu_1_2_Origins",
            (1, 2, "ShearY") => "Types_Nanpu_1_2_ShearY",
            (1, 2, "ShearZ") => "Types_Nanpu_1_2_ShearZ",
            (1, 2, "SourceFaces") => "Types_Nanpu_1_2_SourceFaces",
            (1, 2, "SourceTriangles") => "Types_Nanpu_1_2_SourceTriangles",
            (1, 2, "Triangles") => "Types_Nanpu_1_2_Triangles",
            (1, 2, "Weights") => "Types_Nanpu_1_2_Weights",
            (1, 3, "Displacement") => "Types_Nanpu_1_3_Displacement",
            (1, 3, "Frames") => "Types_Nanpu_1_3_Frames",
            (1, 3, "Origins") => "Types_Nanpu_1_3_Origins",
            (1, 3, "ShearY") => "Types_Nanpu_1_3_ShearY",
            (1, 3, "ShearZ") => "Types_Nanpu_1_3_ShearZ",
            (1, 3, "SourceFaces") => "Types_Nanpu_1_3_SourceFaces",
            (1, 3, "SourceTriangles") => "Types_Nanpu_1_3_SourceTriangles",
            (1, 3, "Triangles") => "Types_Nanpu_1_3_Triangles",
            (1, 3, "Weights") => "Types_Nanpu_1_3_Weights",
            (1, 4, "Displacement") => "Types_Nanpu_1_4_Displacement",
            (1, 4, "Frames") => "Types_Nanpu_1_4_Frames",
            (1, 4, "Origins") => "Types_Nanpu_1_4_Origins",
            (1, 4, "ShearY") => "Types_Nanpu_1_4_ShearY",
            (1, 4, "ShearZ") => "Types_Nanpu_1_4_ShearZ",
            (1, 4, "SourceFaces") => "Types_Nanpu_1_4_SourceFaces",
            (1, 4, "SourceTriangles") => "Types_Nanpu_1_4_SourceTriangles",
            (1, 4, "Triangles") => "Types_Nanpu_1_4_Triangles",
            (1, 4, "Weights") => "Types_Nanpu_1_4_Weights",
            (1, 5, "Displacement") => "Types_Nanpu_1_5_Displacement",
            (1, 5, "Frames") => "Types_Nanpu_1_5_Frames",
            (1, 5, "Origins") => "Types_Nanpu_1_5_Origins",
            (1, 5, "ShearY") => "Types_Nanpu_1_5_ShearY",
            (1, 5, "ShearZ") => "Types_Nanpu_1_5_ShearZ",
            (1, 5, "SourceFaces") => "Types_Nanpu_1_5_SourceFaces",
            (1, 5, "SourceTriangles") => "Types_Nanpu_1_5_SourceTriangles",
            (1, 5, "Triangles") => "Types_Nanpu_1_5_Triangles",
            (1, 5, "Weights") => "Types_Nanpu_1_5_Weights",
            (1, 6, "Displacement") => "Types_Nanpu_1_6_Displacement",
            (1, 6, "Frames") => "Types_Nanpu_1_6_Frames",
            (1, 6, "Origins") => "Types_Nanpu_1_6_Origins",
            (1, 6, "ShearY") => "Types_Nanpu_1_6_ShearY",
            (1, 6, "ShearZ") => "Types_Nanpu_1_6_ShearZ",
            (1, 6, "SourceFaces") => "Types_Nanpu_1_6_SourceFaces",
            (1, 6, "SourceTriangles") => "Types_Nanpu_1_6_SourceTriangles",
            (1, 6, "Triangles") => "Types_Nanpu_1_6_Triangles",
            (1, 6, "Weights") => "Types_Nanpu_1_6_Weights",
            (1, 7, "Displacement") => "Types_Nanpu_1_7_Displacement",
            (1, 7, "Frames") => "Types_Nanpu_1_7_Frames",
            (1, 7, "Origins") => "Types_Nanpu_1_7_Origins",
            (1, 7, "ShearY") => "Types_Nanpu_1_7_ShearY",
            (1, 7, "ShearZ") => "Types_Nanpu_1_7_ShearZ",
            (1, 7, "SourceFaces") => "Types_Nanpu_1_7_SourceFaces",
            (1, 7, "SourceTriangles") => "Types_Nanpu_1_7_SourceTriangles",
            (1, 7, "Triangles") => "Types_Nanpu_1_7_Triangles",
            (1, 7, "Weights") => "Types_Nanpu_1_7_Weights",
            (1, 8, "Displacement") => "Types_Nanpu_1_8_Displacement",
            (1, 8, "Frames") => "Types_Nanpu_1_8_Frames",
            (1, 8, "Origins") => "Types_Nanpu_1_8_Origins",
            (1, 8, "ShearY") => "Types_Nanpu_1_8_ShearY",
            (1, 8, "ShearZ") => "Types_Nanpu_1_8_ShearZ",
            (1, 8, "SourceFaces") => "Types_Nanpu_1_8_SourceFaces",
            (1, 8, "SourceTriangles") => "Types_Nanpu_1_8_SourceTriangles",
            (1, 8, "Triangles") => "Types_Nanpu_1_8_Triangles",
            (1, 8, "Weights") => "Types_Nanpu_1_8_Weights",
            (2, 0, "Displacement") => "Types_Nanpu_2_0_Displacement",
            (2, 0, "Frames") => "Types_Nanpu_2_0_Frames",
            (2, 0, "Origins") => "Types_Nanpu_2_0_Origins",
            (2, 0, "ShearY") => "Types_Nanpu_2_0_ShearY",
            (2, 0, "ShearZ") => "Types_Nanpu_2_0_ShearZ",
            (2, 0, "SourceFaces") => "Types_Nanpu_2_0_SourceFaces",
            (2, 0, "SourceTriangles") => "Types_Nanpu_2_0_SourceTriangles",
            (2, 0, "Triangles") => "Types_Nanpu_2_0_Triangles",
            (2, 0, "Weights") => "Types_Nanpu_2_0_Weights",
            (2, 1, "Displacement") => "Types_Nanpu_2_1_Displacement",
            (2, 1, "Frames") => "Types_Nanpu_2_1_Frames",
            (2, 1, "Origins") => "Types_Nanpu_2_1_Origins",
            (2, 1, "ShearY") => "Types_Nanpu_2_1_ShearY",
            (2, 1, "ShearZ") => "Types_Nanpu_2_1_ShearZ",
            (2, 1, "SourceFaces") => "Types_Nanpu_2_1_SourceFaces",
            (2, 1, "SourceTriangles") => "Types_Nanpu_2_1_SourceTriangles",
            (2, 1, "Triangles") => "Types_Nanpu_2_1_Triangles",
            (2, 1, "Weights") => "Types_Nanpu_2_1_Weights",
            (2, 2, "Displacement") => "Types_Nanpu_2_2_Displacement",
            (2, 2, "Frames") => "Types_Nanpu_2_2_Frames",
            (2, 2, "Origins") => "Types_Nanpu_2_2_Origins",
            (2, 2, "ShearY") => "Types_Nanpu_2_2_ShearY",
            (2, 2, "ShearZ") => "Types_Nanpu_2_2_ShearZ",
            (2, 2, "SourceFaces") => "Types_Nanpu_2_2_SourceFaces",
            (2, 2, "SourceTriangles") => "Types_Nanpu_2_2_SourceTriangles",
            (2, 2, "Triangles") => "Types_Nanpu_2_2_Triangles",
            (2, 2, "Weights") => "Types_Nanpu_2_2_Weights",
            (2, 3, "Displacement") => "Types_Nanpu_2_3_Displacement",
            (2, 3, "Frames") => "Types_Nanpu_2_3_Frames",
            (2, 3, "Origins") => "Types_Nanpu_2_3_Origins",
            (2, 3, "ShearY") => "Types_Nanpu_2_3_ShearY",
            (2, 3, "ShearZ") => "Types_Nanpu_2_3_ShearZ",
            (2, 3, "SourceFaces") => "Types_Nanpu_2_3_SourceFaces",
            (2, 3, "SourceTriangles") => "Types_Nanpu_2_3_SourceTriangles",
            (2, 3, "Triangles") => "Types_Nanpu_2_3_Triangles",
            (2, 3, "Weights") => "Types_Nanpu_2_3_Weights",
            (2, 4, "Displacement") => "Types_Nanpu_2_4_Displacement",
            (2, 4, "Frames") => "Types_Nanpu_2_4_Frames",
            (2, 4, "Origins") => "Types_Nanpu_2_4_Origins",
            (2, 4, "ShearY") => "Types_Nanpu_2_4_ShearY",
            (2, 4, "ShearZ") => "Types_Nanpu_2_4_ShearZ",
            (2, 4, "SourceFaces") => "Types_Nanpu_2_4_SourceFaces",
            (2, 4, "SourceTriangles") => "Types_Nanpu_2_4_SourceTriangles",
            (2, 4, "Triangles") => "Types_Nanpu_2_4_Triangles",
            (2, 4, "Weights") => "Types_Nanpu_2_4_Weights",
            (2, 5, "Displacement") => "Types_Nanpu_2_5_Displacement",
            (2, 5, "Frames") => "Types_Nanpu_2_5_Frames",
            (2, 5, "Origins") => "Types_Nanpu_2_5_Origins",
            (2, 5, "ShearY") => "Types_Nanpu_2_5_ShearY",
            (2, 5, "ShearZ") => "Types_Nanpu_2_5_ShearZ",
            (2, 5, "SourceFaces") => "Types_Nanpu_2_5_SourceFaces",
            (2, 5, "SourceTriangles") => "Types_Nanpu_2_5_SourceTriangles",
            (2, 5, "Triangles") => "Types_Nanpu_2_5_Triangles",
            (2, 5, "Weights") => "Types_Nanpu_2_5_Weights",
            (2, 6, "Displacement") => "Types_Nanpu_2_6_Displacement",
            (2, 6, "Frames") => "Types_Nanpu_2_6_Frames",
            (2, 6, "Origins") => "Types_Nanpu_2_6_Origins",
            (2, 6, "ShearY") => "Types_Nanpu_2_6_ShearY",
            (2, 6, "ShearZ") => "Types_Nanpu_2_6_ShearZ",
            (2, 6, "SourceFaces") => "Types_Nanpu_2_6_SourceFaces",
            (2, 6, "SourceTriangles") => "Types_Nanpu_2_6_SourceTriangles",
            (2, 6, "Triangles") => "Types_Nanpu_2_6_Triangles",
            (2, 6, "Weights") => "Types_Nanpu_2_6_Weights",
            (2, 7, "Displacement") => "Types_Nanpu_2_7_Displacement",
            (2, 7, "Frames") => "Types_Nanpu_2_7_Frames",
            (2, 7, "Origins") => "Types_Nanpu_2_7_Origins",
            (2, 7, "ShearY") => "Types_Nanpu_2_7_ShearY",
            (2, 7, "ShearZ") => "Types_Nanpu_2_7_ShearZ",
            (2, 7, "SourceFaces") => "Types_Nanpu_2_7_SourceFaces",
            (2, 7, "SourceTriangles") => "Types_Nanpu_2_7_SourceTriangles",
            (2, 7, "Triangles") => "Types_Nanpu_2_7_Triangles",
            (2, 7, "Weights") => "Types_Nanpu_2_7_Weights",
            (2, 8, "Displacement") => "Types_Nanpu_2_8_Displacement",
            (2, 8, "Frames") => "Types_Nanpu_2_8_Frames",
            (2, 8, "Origins") => "Types_Nanpu_2_8_Origins",
            (2, 8, "ShearY") => "Types_Nanpu_2_8_ShearY",
            (2, 8, "ShearZ") => "Types_Nanpu_2_8_ShearZ",
            (2, 8, "SourceFaces") => "Types_Nanpu_2_8_SourceFaces",
            (2, 8, "SourceTriangles") => "Types_Nanpu_2_8_SourceTriangles",
            (2, 8, "Triangles") => "Types_Nanpu_2_8_Triangles",
            (2, 8, "Weights") => "Types_Nanpu_2_8_Weights",
        };
        return HardcodedNumbers.Load(key);
    }
    internal static float Side(int part) => part == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[23] ? Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[14] : HardcodedNumbers.Load("Types_Nanpu_TowerPoses").F32[(part - Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[24]) * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[25] + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[26]];
    internal static quaternion Rotation(int part)
    {
        if (part == Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[27])
            return quaternion.identity;
        var values = HardcodedNumbers.Load("Types_Nanpu_TowerPoses").F32;
        return new quaternion(Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[15], Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.F32[16], values[(part - Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[28]) * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[29]], values[(part - Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[30]) * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[31] + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[32]]);
    }

    internal static float3[] Origins(int lod, int part) => GenerationParameters.Get("Nanpu.Origins." + lod + "." + part, () =>
    {
        var values = Data(lod, part, "Origins").F32;
        var result = new float3[values.Length / Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[33]];
        for (var i = Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[34]; i < result.Length; i++)
            result[i] = new float3(values[i * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[35]], values[i * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[36] + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[37]], values[i * Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[38] + Numeric_Types_Nanpu_NanpuBridgeGenerator_cs.Values.I32[39]]);
        return result;
    });
}

// Embedded numerical arrays: Types_Nanpu_NanpuBridgeGenerator_cs
internal static class Numeric_Types_Nanpu_NanpuBridgeGenerator_cs
{
    static Numeric_Types_Nanpu_NanpuBridgeGenerator_cs()
    {
    }

    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_Nanpu_NanpuBridgeGenerator_cs");
}
