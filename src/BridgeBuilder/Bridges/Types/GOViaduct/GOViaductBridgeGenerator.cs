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
internal sealed class GOViaductBridgeGenerator : BridgeGeneratorBase
{
    internal GOViaductBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "GOViaduct";
    protected override bool HasRecordedConstruction => true;

    // The static source has no unlock graph. Finalize the selected network's native rules before saving.
    internal override bool SelectedRoadUnlocks => true;
    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant, TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    {
        var half = (targetWidth - GOViaductConstruction.RoadWidth) / Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[0];
        var frame = Matrix4x4.identity;
        frame.m00 = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[0];
        frame.m02 = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[1];
        frame.m20 = -Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[2];
        frame.m22 = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[3];
        var parts = new RenderPrefab[5][];
        var networkMaterials = new SurfaceAsset[3][];
        for (var part = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[5]; part < parts.Length; part++)
        {
            var renders = parts[part] = new RenderPrefab[Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[6]];
            for (var lod = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[7]; lod < renders.Length; lod++)
            {
                var source = factory.ResolveOriginal<RenderPrefab>(lod == Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[8] ? GOViaductConstruction.ResourceAnchorName : "GO-224m-ViaductBridge_LOD" + lod + " Mesh");
                var name = target.name + " GOArch-" + part + "-LOD" + lod;
                // This concrete abutment's authored LOD2 control atlas overlaps red and green,
                // unlike its neutral LOD0/LOD1 walls. Preserve their concrete identity and PBR
                // channels without applying the arch's brown palette to the erroneous mask.
                var materials = part == 2 && lod == 2
                    ? await factory.CreateRecordedNetworkSurfacesAsync(source, name, new[] { Color.white, Color.white, Color.white })
                    : part < 3 ? networkMaterials[lod] ??= await factory.CreateRecordedNetworkSurfacesAsync(source, name, new[] { Color.white, new Color(Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[1], Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[2], Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[3], Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[4]), Color.white }) : null;
                var omitted = new bool[source.indexCount / Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[10]];
                foreach (var face in GOViaductConstruction.Data(lod, part, "Omitted").I32)
                    omitted[face] = true;
                renders[lod] = await factory.CreateRecordedRenderAsync(source, name, half, new[] { GOViaductConstruction.Data(lod, part, "Displacement").F32 }, new[] { GOViaductConstruction.Data(lod, part, "Frames").F32 }, xShearYCoefficients: new[] { GOViaductConstruction.Data(lod, part, "ShearY").F32 }, xShearZCoefficients: new[] { GOViaductConstruction.Data(lod, part, "ShearZ").F32 }, recordedOrigins: new[] { GOViaductConstruction.Origins(lod, part) }, sourceFrame: frame, omittedTriangles: new[] { omitted }, sourceVertexIndices: new[] { GOViaductConstruction.Data(lod, part, "SourceVertices").I32 }, recordedCornerIndices: new[] { GOViaductConstruction.Data(lod, part, "Corners").I32 }, renderType: part < 3 && lod == 0 ? typeof(NetPiecePrefab) : typeof(RenderPrefab), receivingSurfaces: materials, sourceTriangleVertices: lod == 2 && part is 1 or 2 ? new[] { GOViaductConstruction.Data(lod, part, "SourceTriangles").I32 } : null, sourceTriangleWeights: lod == 2 && part is 1 or 2 ? new[] { GOViaductConstruction.Data(lod, part, "Weights").F32 } : null, explicitTriangleIndices: lod == 2 && part is 1 or 2 ? new[] { GOViaductConstruction.Data(lod, part, "Triangles").I32 } : null, sourceFaceIndices: lod == 2 && part is 1 or 2 ? new[] { GOViaductConstruction.Data(lod, part, "SourceFaces").I32 } : null, recordedUv0: new[] { GOViaductConstruction.Uv0(lod, part) });
            }

            var original = factory.ResolveOriginal<RenderPrefab>(GOViaductConstruction.ResourceAnchorName).GetComponent<LodProperties>() ?? throw new InvalidOperationException("GO source LOD descriptor is unavailable.");
            var levels = renders[Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[13]].AddComponent<LodProperties>();
            levels.m_Bias = original.m_Bias;
            levels.m_ShadowBias = original.m_ShadowBias;
            levels.m_LodMeshes = new[]
            {
                renders[Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[14]],
                renders[Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[15]]
            };
        }

        var piece = (NetPiecePrefab)parts[0][0];
        for (var part = 0; part < 3; part++)
        {
            var networkPiece = (NetPiecePrefab)parts[part][0];
            networkPiece.m_Layer = NetPieceLayer.Top;
            networkPiece.m_Width = targetWidth + GOViaductConstruction.StructureWidth - GOViaductConstruction.RoadWidth;
            networkPiece.m_Length = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[part == 0 ? 5 : 6];
            networkPiece.m_NodeOffset = 0f;
            networkPiece.m_HeightRange = new Bounds1(0f, Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[part == 0 ? 8 : 13]);
            networkPiece.AddComponent<NetPieceTiling>().m_DisableTextureTiling = true;
        }
        var supportRules = new NetPieceObjectInfo[Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[18]];
        for (var end = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[19]; end < supportRules.Length; end++)
        {
            var support = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            support.name = target.name + " GOArchEnd-" + end;
            // Native placement permits these structural foundations on dry land and in water.
            support.AddComponent<UnderwaterObject>().m_AllowDryland = true;
            // Structural collision without native vertical/base alignment changing the measured pose.
            support.AddComponent<PillarObject>().m_Type = PillarType.None;
            support.m_Meshes = new[]
            {
                new ObjectMeshInfo
                {
                    m_Mesh = parts[end + 3][Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[21]],
                    m_Rotation = quaternion.identity
                }
            };
            factory.AdoptMovableSections(new PrefabBase[] { support });
            supportRules[end] = new NetPieceObjectInfo
            {
                m_Object = support,
                // Native network pieces reverse local X/Z; object placement does not.
                m_Rotation = new quaternion(0f, 1f, 0f, 0f),
                m_RequireAll = new[]
                {
                    NetPieceRequirements.Edge
                },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>(),
                m_Probability = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[22],
                m_CurveOffsetRange = new float2(1 - end),
                m_UseCurveRotation = new float2(Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[23], Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[24])
            };
        }

        piece.AddComponent<NetPieceObjects>().m_PieceObjects = supportRules;
        var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
        section.name = target.name + " GOArchSection";
        factory.AdoptMovableSections(new PrefabBase[] { section });
        section.m_SubSections = Array.Empty<NetSubSectionInfo>();
        section.m_Pieces = new[]
        {
            new NetPieceInfo
            {
                m_Piece = piece,
                m_RequireAll = new[] { NetPieceRequirements.Edge },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = new[] { NetPieceRequirements.Front, NetPieceRequirements.Back }
            },
            new NetPieceInfo
            {
                m_Piece = (NetPiecePrefab)parts[2][0],
                m_RequireAll = new[] { NetPieceRequirements.Edge, NetPieceRequirements.Front },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>()
            },
            new NetPieceInfo
            {
                m_Piece = (NetPiecePrefab)parts[1][0],
                m_RequireAll = new[] { NetPieceRequirements.Edge, NetPieceRequirements.Back },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>()
            }
        };
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
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(item => item == null || !item.m_RequireElevated).ToArray();
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>("Bridge");
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.m_SegmentLength = GOViaductConstruction.Length;
        bridge.m_Hanging = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[25];
        // Keep the selected network's native elevation-on-water value (or the native Bridge default).
        bridge.m_CanCurve = false;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = BridgeWaterFlow.Any;
        bridge.m_BuildStyle = BridgeBuildStyle.Elevated;
        // All-positive native courses allocate the two approaches and steel deck proportionally.
        // Only the measured foundation blocks remain rigid at the central course boundaries.
        bridge.m_FixedSegments = new[]
        {
            new FixedNetSegmentInfo
            {
                m_CountRange = new int2(1, 1),
                m_Length = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[6],
                m_CanCurve = false,
                m_SetState = new[] { NetPieceRequirements.Front },
                m_UnsetState = new[] { NetPieceRequirements.Back }
            },
            new FixedNetSegmentInfo
            {
                m_CountRange = new int2(1, 1),
                m_Length = piece.m_Length,
                m_CanCurve = false,
                m_SetState = Array.Empty<NetPieceRequirements>(),
                m_UnsetState = new[] { NetPieceRequirements.Front, NetPieceRequirements.Back }
            },
            new FixedNetSegmentInfo
            {
                m_CountRange = new int2(1, 1),
                m_Length = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[6],
                m_CanCurve = false,
                m_SetState = new[] { NetPieceRequirements.Back },
                m_UnsetState = new[] { NetPieceRequirements.Front }
            }
        };
    }
}

internal static class GOViaductConstruction
{
    internal const string NativeName = "GO-224m-ViaductBridge";
    internal const string ResourceAnchorName = "GO-224m-ViaductBridge Mesh";
    internal const string Icon = "assetdb://global/da615ceecc671d7bf338fdab3c0ccfda";
    internal static float RoadWidth => Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[10];
    internal static float StructureWidth => Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[11];
    internal static float Length => Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.F32[12];

    internal static HardcodedNumbers Data(int lod, int part, string kind)
    {
        var key = (lod, part, kind) switch
        {
            (0, 0, "Uv0") => "Types_GOViaduct_0_0_Uv0",
            (0, 1, "Uv0") => "Types_GOViaduct_0_1_Uv0",
            (0, 2, "Uv0") => "Types_GOViaduct_0_2_Uv0",
            (0, 3, "Uv0") => "Types_GOViaduct_0_3_Uv0",
            (0, 4, "Uv0") => "Types_GOViaduct_0_4_Uv0",
            (1, 0, "Uv0") => "Types_GOViaduct_1_0_Uv0",
            (1, 1, "Uv0") => "Types_GOViaduct_1_1_Uv0",
            (1, 2, "Uv0") => "Types_GOViaduct_1_2_Uv0",
            (1, 3, "Uv0") => "Types_GOViaduct_1_3_Uv0",
            (1, 4, "Uv0") => "Types_GOViaduct_1_4_Uv0",
            (2, 0, "Uv0") => "Types_GOViaduct_2_0_Uv0",
            (2, 1, "Uv0") => "Types_GOViaduct_2_1_Uv0",
            (2, 1, "SourceTriangles") => "Types_GOViaduct_2_1_SourceTriangles",
            (2, 1, "Weights") => "Types_GOViaduct_2_1_Weights",
            (2, 1, "Triangles") => "Types_GOViaduct_2_1_Triangles",
            (2, 1, "SourceFaces") => "Types_GOViaduct_2_1_SourceFaces",
            (2, 2, "Uv0") => "Types_GOViaduct_2_2_Uv0",
            (2, 2, "SourceTriangles") => "Types_GOViaduct_2_2_SourceTriangles",
            (2, 2, "Weights") => "Types_GOViaduct_2_2_Weights",
            (2, 2, "Triangles") => "Types_GOViaduct_2_2_Triangles",
            (2, 2, "SourceFaces") => "Types_GOViaduct_2_2_SourceFaces",
            (2, 3, "Uv0") => "Types_GOViaduct_2_3_Uv0",
            (2, 4, "Uv0") => "Types_GOViaduct_2_4_Uv0",
            (0, 0, "Origins") => "Types_GOViaduct_0_0_Origins",
            (0, 0, "Displacement") => "Types_GOViaduct_0_0_Displacement",
            (0, 0, "Frames") => "Types_GOViaduct_0_0_Frames",
            (0, 0, "ShearY") => "Types_GOViaduct_0_0_ShearY",
            (0, 0, "ShearZ") => "Types_GOViaduct_0_0_ShearZ",
            (0, 0, "Omitted") => "Types_GOViaduct_0_0_Omitted",
            (0, 0, "SourceVertices") => "Types_GOViaduct_0_0_SourceVertices",
            (0, 0, "Corners") => "Types_GOViaduct_0_0_Corners",
            (0, 1, "Origins") => "Types_GOViaduct_0_1_Origins",
            (0, 1, "Displacement") => "Types_GOViaduct_0_1_Displacement",
            (0, 1, "Frames") => "Types_GOViaduct_0_1_Frames",
            (0, 1, "ShearY") => "Types_GOViaduct_0_1_ShearY",
            (0, 1, "ShearZ") => "Types_GOViaduct_0_1_ShearZ",
            (0, 1, "Omitted") => "Types_GOViaduct_0_1_Omitted",
            (0, 1, "SourceVertices") => "Types_GOViaduct_0_1_SourceVertices",
            (0, 1, "Corners") => "Types_GOViaduct_0_1_Corners",
            (0, 2, "Origins") => "Types_GOViaduct_0_2_Origins",
            (0, 2, "Displacement") => "Types_GOViaduct_0_2_Displacement",
            (0, 2, "Frames") => "Types_GOViaduct_0_2_Frames",
            (0, 2, "ShearY") => "Types_GOViaduct_0_2_ShearY",
            (0, 2, "ShearZ") => "Types_GOViaduct_0_2_ShearZ",
            (0, 2, "Omitted") => "Types_GOViaduct_0_2_Omitted",
            (0, 2, "SourceVertices") => "Types_GOViaduct_0_2_SourceVertices",
            (0, 2, "Corners") => "Types_GOViaduct_0_2_Corners",
            (0, 3, "Origins") => "Types_GOViaduct_0_3_Origins",
            (0, 3, "Displacement") => "Types_GOViaduct_0_3_Displacement",
            (0, 3, "Frames") => "Types_GOViaduct_0_3_Frames",
            (0, 3, "ShearY") => "Types_GOViaduct_0_3_ShearY",
            (0, 3, "ShearZ") => "Types_GOViaduct_0_3_ShearZ",
            (0, 3, "Omitted") => "Types_GOViaduct_0_3_Omitted",
            (0, 3, "SourceVertices") => "Types_GOViaduct_0_3_SourceVertices",
            (0, 3, "Corners") => "Types_GOViaduct_0_3_Corners",
            (0, 4, "Origins") => "Types_GOViaduct_0_4_Origins",
            (0, 4, "Displacement") => "Types_GOViaduct_0_4_Displacement",
            (0, 4, "Frames") => "Types_GOViaduct_0_4_Frames",
            (0, 4, "ShearY") => "Types_GOViaduct_0_4_ShearY",
            (0, 4, "ShearZ") => "Types_GOViaduct_0_4_ShearZ",
            (0, 4, "Omitted") => "Types_GOViaduct_0_4_Omitted",
            (0, 4, "SourceVertices") => "Types_GOViaduct_0_4_SourceVertices",
            (0, 4, "Corners") => "Types_GOViaduct_0_4_Corners",
            (1, 0, "Origins") => "Types_GOViaduct_1_0_Origins",
            (1, 0, "Displacement") => "Types_GOViaduct_1_0_Displacement",
            (1, 0, "Frames") => "Types_GOViaduct_1_0_Frames",
            (1, 0, "ShearY") => "Types_GOViaduct_1_0_ShearY",
            (1, 0, "ShearZ") => "Types_GOViaduct_1_0_ShearZ",
            (1, 0, "Omitted") => "Types_GOViaduct_1_0_Omitted",
            (1, 0, "SourceVertices") => "Types_GOViaduct_1_0_SourceVertices",
            (1, 0, "Corners") => "Types_GOViaduct_1_0_Corners",
            (1, 1, "Origins") => "Types_GOViaduct_1_1_Origins",
            (1, 1, "Displacement") => "Types_GOViaduct_1_1_Displacement",
            (1, 1, "Frames") => "Types_GOViaduct_1_1_Frames",
            (1, 1, "ShearY") => "Types_GOViaduct_1_1_ShearY",
            (1, 1, "ShearZ") => "Types_GOViaduct_1_1_ShearZ",
            (1, 1, "Omitted") => "Types_GOViaduct_1_1_Omitted",
            (1, 1, "SourceVertices") => "Types_GOViaduct_1_1_SourceVertices",
            (1, 1, "Corners") => "Types_GOViaduct_1_1_Corners",
            (1, 2, "Origins") => "Types_GOViaduct_1_2_Origins",
            (1, 2, "Displacement") => "Types_GOViaduct_1_2_Displacement",
            (1, 2, "Frames") => "Types_GOViaduct_1_2_Frames",
            (1, 2, "ShearY") => "Types_GOViaduct_1_2_ShearY",
            (1, 2, "ShearZ") => "Types_GOViaduct_1_2_ShearZ",
            (1, 2, "Omitted") => "Types_GOViaduct_1_2_Omitted",
            (1, 2, "SourceVertices") => "Types_GOViaduct_1_2_SourceVertices",
            (1, 2, "Corners") => "Types_GOViaduct_1_2_Corners",
            (1, 3, "Origins") => "Types_GOViaduct_1_3_Origins",
            (1, 3, "Displacement") => "Types_GOViaduct_1_3_Displacement",
            (1, 3, "Frames") => "Types_GOViaduct_1_3_Frames",
            (1, 3, "ShearY") => "Types_GOViaduct_1_3_ShearY",
            (1, 3, "ShearZ") => "Types_GOViaduct_1_3_ShearZ",
            (1, 3, "Omitted") => "Types_GOViaduct_1_3_Omitted",
            (1, 3, "SourceVertices") => "Types_GOViaduct_1_3_SourceVertices",
            (1, 3, "Corners") => "Types_GOViaduct_1_3_Corners",
            (1, 4, "Origins") => "Types_GOViaduct_1_4_Origins",
            (1, 4, "Displacement") => "Types_GOViaduct_1_4_Displacement",
            (1, 4, "Frames") => "Types_GOViaduct_1_4_Frames",
            (1, 4, "ShearY") => "Types_GOViaduct_1_4_ShearY",
            (1, 4, "ShearZ") => "Types_GOViaduct_1_4_ShearZ",
            (1, 4, "Omitted") => "Types_GOViaduct_1_4_Omitted",
            (1, 4, "SourceVertices") => "Types_GOViaduct_1_4_SourceVertices",
            (1, 4, "Corners") => "Types_GOViaduct_1_4_Corners",
            (2, 0, "Origins") => "Types_GOViaduct_2_0_Origins",
            (2, 0, "Displacement") => "Types_GOViaduct_2_0_Displacement",
            (2, 0, "Frames") => "Types_GOViaduct_2_0_Frames",
            (2, 0, "ShearY") => "Types_GOViaduct_2_0_ShearY",
            (2, 0, "ShearZ") => "Types_GOViaduct_2_0_ShearZ",
            (2, 0, "Omitted") => "Types_GOViaduct_2_0_Omitted",
            (2, 0, "SourceVertices") => "Types_GOViaduct_2_0_SourceVertices",
            (2, 0, "Corners") => "Types_GOViaduct_2_0_Corners",
            (2, 1, "Origins") => "Types_GOViaduct_2_1_Origins",
            (2, 1, "Displacement") => "Types_GOViaduct_2_1_Displacement",
            (2, 1, "Frames") => "Types_GOViaduct_2_1_Frames",
            (2, 1, "ShearY") => "Types_GOViaduct_2_1_ShearY",
            (2, 1, "ShearZ") => "Types_GOViaduct_2_1_ShearZ",
            (2, 1, "Omitted") => "Types_GOViaduct_2_1_Omitted",
            (2, 1, "SourceVertices") => "Types_GOViaduct_2_1_SourceVertices",
            (2, 1, "Corners") => "Types_GOViaduct_2_1_Corners",
            (2, 2, "Origins") => "Types_GOViaduct_2_2_Origins",
            (2, 2, "Displacement") => "Types_GOViaduct_2_2_Displacement",
            (2, 2, "Frames") => "Types_GOViaduct_2_2_Frames",
            (2, 2, "ShearY") => "Types_GOViaduct_2_2_ShearY",
            (2, 2, "ShearZ") => "Types_GOViaduct_2_2_ShearZ",
            (2, 2, "Omitted") => "Types_GOViaduct_2_2_Omitted",
            (2, 2, "SourceVertices") => "Types_GOViaduct_2_2_SourceVertices",
            (2, 2, "Corners") => "Types_GOViaduct_2_2_Corners",
            (2, 3, "Origins") => "Types_GOViaduct_2_3_Origins",
            (2, 3, "Displacement") => "Types_GOViaduct_2_3_Displacement",
            (2, 3, "Frames") => "Types_GOViaduct_2_3_Frames",
            (2, 3, "ShearY") => "Types_GOViaduct_2_3_ShearY",
            (2, 3, "ShearZ") => "Types_GOViaduct_2_3_ShearZ",
            (2, 3, "Omitted") => "Types_GOViaduct_2_3_Omitted",
            (2, 3, "SourceVertices") => "Types_GOViaduct_2_3_SourceVertices",
            (2, 3, "Corners") => "Types_GOViaduct_2_3_Corners",
            (2, 4, "Origins") => "Types_GOViaduct_2_4_Origins",
            (2, 4, "Displacement") => "Types_GOViaduct_2_4_Displacement",
            (2, 4, "Frames") => "Types_GOViaduct_2_4_Frames",
            (2, 4, "ShearY") => "Types_GOViaduct_2_4_ShearY",
            (2, 4, "ShearZ") => "Types_GOViaduct_2_4_ShearZ",
            (2, 4, "Omitted") => "Types_GOViaduct_2_4_Omitted",
            (2, 4, "SourceVertices") => "Types_GOViaduct_2_4_SourceVertices",
            (2, 4, "Corners") => "Types_GOViaduct_2_4_Corners"
        };
        return HardcodedNumbers.Load(key);
    }

    internal static Vector2[] Uv0(int lod, int part) => GenerationParameters.Get("GOViaduct.Uv0." + lod + "." + part, () =>
    {
        var values = Data(lod, part, "Uv0").F32;
        var result = new Vector2[values.Length / 2];
        for (var i = 0; i < result.Length; i++) result[i] = new Vector2(values[2 * i], values[2 * i + 1]);
        return result;
    });

    internal static float3[] Origins(int lod, int part) => GenerationParameters.Get("GOViaduct.Origins." + lod + "." + part, () =>
    {
        var values = Data(lod, part, "Origins").F32;
        var result = new float3[values.Length / Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[26]];
        for (var i = Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[27]; i < result.Length; i++)
            result[i] = new float3(values[i * Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[28]], values[i * Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[29] + Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[30]], values[i * Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[31] + Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs.Values.I32[32]]);
        return result;
    });
}

// Embedded numerical arrays: Types_GOViaduct_GOViaductBridgeGenerator_cs
internal static class Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs
{
    static Numeric_Types_GOViaduct_GOViaductBridgeGenerator_cs()
    {
    }

    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_GOViaduct_GOViaductBridgeGenerator_cs");
}
