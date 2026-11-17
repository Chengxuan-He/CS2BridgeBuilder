using System;
using System.Linq;
using System.Threading.Tasks;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

internal sealed class LupuBridgeGenerator : BridgeGeneratorBase
{
    internal LupuBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Lupu";
    protected override bool HasRecordedConstruction => true;
    internal override bool SelectedRoadUnlocks => true;

    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant,
        TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    {
        // Source root rotates -90 degrees around X. This additional construction-axis
        // rotation keeps a right-handed frame; native network evaluation reverses X/Z.
        var frame = Matrix4x4.zero;
        frame.m01 = frame.m12 = frame.m20 = frame.m33 = 1f;
        var parts = new RenderPrefab[5][];
        for (var part = 0; part < parts.Length; part++)
        {
            // All structural roles share the measured arch-edge frame.
            var referenceWidth = LupuConstruction.RoadWidth;
            var half = (targetWidth - referenceWidth) / 2f;
            var renders = parts[part] = new RenderPrefab[2];
            for (var lod = 0; lod < renders.Length; lod++)
            {
                var source = factory.ResolveOriginal<RenderPrefab>(LupuConstruction.RenderName(lod));
                var name = target.name + " Lupu-" + part + "-LOD" + lod;
                var slots = 2;
                var coefficients = new float[slots][];
                var frames = new float[slots][];
                var shearY = new float[slots][];
                var shearZ = new float[slots][];
                var origins = new float3[slots][];
                var localFrames = new Matrix4x4[slots][];
                var sourceVertices = new int[slots][];
                var corners = new int[slots][];
                var omitted = new bool[slots][];
                for (var slot = 0; slot < slots; slot++)
                {
                    coefficients[slot] = LupuConstruction.Data(lod, slot, part, "Displacement").F32;
                    frames[slot] = LupuConstruction.Data(lod, slot, part, "Frames").F32;
                    shearY[slot] = LupuConstruction.Data(lod, slot, part, "ShearY").F32;
                    shearZ[slot] = LupuConstruction.Data(lod, slot, part, "ShearZ").F32;
                    // The recorded geometry already meets the fixed construction plane y=-0.5.
                    // No selected-road height sampling or runtime height adjustment is required.
                    origins[slot] = LupuConstruction.Origins(lod, slot, part);
                    if (part == 0 && slot == 0)
                    {
                        var gradients = LupuConstruction.SupportFrames(lod).F32;
                        localFrames[slot] = new Matrix4x4[origins[slot].Length];
                        for (var vertex = 0; vertex < origins[slot].Length; vertex++)
                        {
                            var transform = Matrix4x4.identity;
                            transform.m00 += gradients[vertex * 6];
                            transform.m01 = gradients[vertex * 6 + 1];
                            transform.m02 = gradients[vertex * 6 + 2];
                            transform.m10 = gradients[vertex * 6 + 3];
                            transform.m11 += gradients[vertex * 6 + 4];
                            transform.m12 = gradients[vertex * 6 + 5];
                            localFrames[slot][vertex] = transform;
                        }
                    }
                    sourceVertices[slot] = LupuConstruction.Data(lod, slot, part, "SourceVertices").I32;
                    corners[slot] = LupuConstruction.Data(lod, slot, part, "Corners").I32;
                    omitted[slot] = new bool[corners[slot].Length / 3];
                    foreach (var face in LupuConstruction.Data(lod, slot, part, "Omitted").I32)
                        omitted[slot][face] = true;
                }
                var surfaces = part == 0
                    ? await factory.CreateRecordedNetworkSurfacesAsync(source, name, Array.Empty<Color>(), sourceMeshIndices: new[] { 0 }) : null;
                renders[lod] = await factory.CreateRecordedRenderAsync(source, name, half, coefficients, frames,
                    xShearYCoefficients: shearY, xShearZCoefficients: shearZ,
                    recordedOrigins: origins, sourceFrame: frame, omittedTriangles: omitted,
                    sourceVertexIndices: sourceVertices, recordedCornerIndices: corners,
                    renderType: part == 0 && lod == 0 ? typeof(NetPiecePrefab) : typeof(RenderPrefab),
                    receivingSurfaces: surfaces, recordedFrameTransforms: part == 0 ? localFrames : null,
                    sourceMeshIndices: part != 0 ? new[] { 0, 1 } : new[] { 0 });
            }
            // The measured source LOD group has zero bias for both ordinary and shadow selection.
            var levels = renders[0].AddComponent<LodProperties>();
            levels.m_Bias = levels.m_ShadowBias = 0f;
            levels.m_LodMeshes = new[] { renders[1] };
        }
        var piece = (NetPiecePrefab)parts[0][0];
        piece.m_Layer = NetPieceLayer.Top;
        piece.m_Width = targetWidth + LupuConstruction.StructureWidth - LupuConstruction.RoadWidth;
        piece.m_Length = LupuConstruction.Length;
        piece.m_NodeOffset = 0f;
        // Structural depth must not contribute an InWater blocker to the selected road.
        piece.m_HeightRange = new Bounds1(0f, LupuConstruction.Parameters.F32[6]);
        piece.AddComponent<NetPieceTiling>().m_DisableTextureTiling = true;
        var rules = new NetPieceObjectInfo[4];
        for (var part = 1; part < parts.Length; part++)
        {
            var support = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            support.name = target.name + " LupuSupport-" + part;
            // Native structural collision identity without terrain anchoring or pose changes.
            support.AddComponent<PillarObject>().m_Type = PillarType.None;
            support.AddComponent<UnderwaterObject>().m_AllowDryland = true;
            support.m_Meshes = new[] { new ObjectMeshInfo { m_Mesh = parts[part][0], m_Rotation = quaternion.identity } };
            factory.AdoptMovableSections(new PrefabBase[] { support });
            rules[part - 1] = new NetPieceObjectInfo
            {
                m_Object = support, m_Rotation = new quaternion(0f, 1f, 0f, 0f),
                m_RequireAll = new[] { NetPieceRequirements.Edge },
                m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>(),
                m_Probability = 100,
                m_CurveOffsetRange = new float2(0.5f - (LupuConstruction.Parameters.F32[8 + (part - 1) % 2] - LupuConstruction.Parameters.F32[5]) / LupuConstruction.Length),
                m_UseCurveRotation = new float2(1f, 1f)
            };
        }
        piece.AddComponent<NetPieceObjects>().m_PieceObjects = rules;
        var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
        section.name = target.name + " LupuSection";
        section.m_SubSections = Array.Empty<NetSubSectionInfo>();
        section.m_Pieces = new[] { new NetPieceInfo { m_Piece = piece,
            m_RequireAll = new[] { NetPieceRequirements.Edge }, m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>() } };
        factory.AdoptMovableSections(new PrefabBase[] { section });
        target.AddOrGetComponent<OverheadNetSections>().m_Sections = new[] { new NetSectionInfo {
            m_Section = section, m_Median = true, m_RequireAll = new[] { NetPieceRequirements.Elevated },
            m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>() } };
        var objects = target.AddOrGetComponent<NetSubObjects>();
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(item => item == null || !item.m_RequireElevated).ToArray();
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>("Bridge");
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.m_SegmentLength = LupuConstruction.Length;
        bridge.m_Hanging = 0f;
        bridge.m_CanCurve = false;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = BridgeWaterFlow.Any;
        bridge.m_BuildStyle = BridgeBuildStyle.Elevated;
        // The source continuous arches include both end regions. One finite native
        // course scales every longitudinal region proportionally, without repeating towers.
        bridge.m_FixedSegments = new[] { new FixedNetSegmentInfo { m_CountRange = new int2(1),
            m_Length = LupuConstruction.Length, m_CanCurve = false,
            m_SetState = Array.Empty<NetPieceRequirements>(), m_UnsetState = Array.Empty<NetPieceRequirements>() } };
    }
}

internal static class LupuConstruction
{
    internal const string NativeName = "卢浦大桥";
    internal const string ResourceAnchorName = "lupudaqiao Mesh";
    internal const string Icon = "coui://bridgebuilderui/BridgeLupu.svg";
    internal static HardcodedNumbers Parameters => HardcodedNumbers.Load("Types_Lupu_Parameters");
    internal static float RoadWidth => Parameters.F32[0];
    internal static float StructureWidth => Parameters.F32[1];
    internal static float Length => Parameters.F32[2];
    internal static string RenderName(int lod) => lod == 0 ? ResourceAnchorName : "lupudaqiaoLOD" + lod + " Mesh";
    internal static float3[] Origins(int lod, int slot, int part) => GenerationParameters.Get("Lupu.Origins." + lod + "." + slot + "." + part, () =>
    {
        var data = Data(lod, slot, part, "Origins").F32;
        var result = new float3[data.Length / 3];
        for (var i = 0; i < result.Length; i++) result[i] = new float3(data[i * 3], data[i * 3 + 1], data[i * 3 + 2]);
        return result;
    });
    internal static HardcodedNumbers SupportFrames(int lod) => HardcodedNumbers.Load(lod switch
    {
        0 => "Types_Lupu_0_0_SupportFrames",
        1 => "Types_Lupu_1_0_SupportFrames",
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    });
    internal static HardcodedNumbers Data(int lod, int slot, int part, string kind)
    {
        var key = (lod, slot, part, kind) switch
        {
            (0, 0, 0, "Origins") => "Types_Lupu_0_0_0_Origins",
            (0, 0, 0, "Displacement") => "Types_Lupu_0_0_0_Displacement",
            (0, 0, 0, "Frames") => "Types_Lupu_0_0_0_Frames",
            (0, 0, 0, "ShearY") => "Types_Lupu_0_0_0_ShearY",
            (0, 0, 0, "ShearZ") => "Types_Lupu_0_0_0_ShearZ",
            (0, 0, 0, "SourceVertices") => "Types_Lupu_0_0_0_SourceVertices",
            (0, 0, 0, "Corners") => "Types_Lupu_0_0_0_Corners",
            (0, 0, 0, "Omitted") => "Types_Lupu_0_0_0_Omitted",
            (0, 0, 1, "Origins") => "Types_Lupu_0_0_1_Origins",
            (0, 0, 1, "Displacement") => "Types_Lupu_0_0_1_Displacement",
            (0, 0, 1, "Frames") => "Types_Lupu_0_0_1_Frames",
            (0, 0, 1, "ShearY") => "Types_Lupu_0_0_1_ShearY",
            (0, 0, 1, "ShearZ") => "Types_Lupu_0_0_1_ShearZ",
            (0, 0, 1, "SourceVertices") => "Types_Lupu_0_0_1_SourceVertices",
            (0, 0, 1, "Corners") => "Types_Lupu_0_0_1_Corners",
            (0, 0, 1, "Omitted") => "Types_Lupu_0_0_1_Omitted",
            (0, 0, 2, "Origins") => "Types_Lupu_0_0_2_Origins",
            (0, 0, 2, "Displacement") => "Types_Lupu_0_0_2_Displacement",
            (0, 0, 2, "Frames") => "Types_Lupu_0_0_2_Frames",
            (0, 0, 2, "ShearY") => "Types_Lupu_0_0_2_ShearY",
            (0, 0, 2, "ShearZ") => "Types_Lupu_0_0_2_ShearZ",
            (0, 0, 2, "SourceVertices") => "Types_Lupu_0_0_2_SourceVertices",
            (0, 0, 2, "Corners") => "Types_Lupu_0_0_2_Corners",
            (0, 0, 2, "Omitted") => "Types_Lupu_0_0_2_Omitted",
            (0, 1, 0, "Origins") => "Types_Lupu_0_1_0_Origins",
            (0, 1, 0, "Displacement") => "Types_Lupu_0_1_0_Displacement",
            (0, 1, 0, "Frames") => "Types_Lupu_0_1_0_Frames",
            (0, 1, 0, "ShearY") => "Types_Lupu_0_1_0_ShearY",
            (0, 1, 0, "ShearZ") => "Types_Lupu_0_1_0_ShearZ",
            (0, 1, 0, "SourceVertices") => "Types_Lupu_0_1_0_SourceVertices",
            (0, 1, 0, "Corners") => "Types_Lupu_0_1_0_Corners",
            (0, 1, 0, "Omitted") => "Types_Lupu_0_1_0_Omitted",
            (0, 1, 1, "Origins") => "Types_Lupu_0_1_1_Origins",
            (0, 1, 1, "Displacement") => "Types_Lupu_0_1_1_Displacement",
            (0, 1, 1, "Frames") => "Types_Lupu_0_1_1_Frames",
            (0, 1, 1, "ShearY") => "Types_Lupu_0_1_1_ShearY",
            (0, 1, 1, "ShearZ") => "Types_Lupu_0_1_1_ShearZ",
            (0, 1, 1, "SourceVertices") => "Types_Lupu_0_1_1_SourceVertices",
            (0, 1, 1, "Corners") => "Types_Lupu_0_1_1_Corners",
            (0, 1, 1, "Omitted") => "Types_Lupu_0_1_1_Omitted",
            (0, 1, 2, "Origins") => "Types_Lupu_0_1_2_Origins",
            (0, 1, 2, "Displacement") => "Types_Lupu_0_1_2_Displacement",
            (0, 1, 2, "Frames") => "Types_Lupu_0_1_2_Frames",
            (0, 1, 2, "ShearY") => "Types_Lupu_0_1_2_ShearY",
            (0, 1, 2, "ShearZ") => "Types_Lupu_0_1_2_ShearZ",
            (0, 1, 2, "SourceVertices") => "Types_Lupu_0_1_2_SourceVertices",
            (0, 1, 2, "Corners") => "Types_Lupu_0_1_2_Corners",
            (0, 1, 2, "Omitted") => "Types_Lupu_0_1_2_Omitted",
            (1, 0, 0, "Origins") => "Types_Lupu_1_0_0_Origins",
            (1, 0, 0, "Displacement") => "Types_Lupu_1_0_0_Displacement",
            (1, 0, 0, "Frames") => "Types_Lupu_1_0_0_Frames",
            (1, 0, 0, "ShearY") => "Types_Lupu_1_0_0_ShearY",
            (1, 0, 0, "ShearZ") => "Types_Lupu_1_0_0_ShearZ",
            (1, 0, 0, "SourceVertices") => "Types_Lupu_1_0_0_SourceVertices",
            (1, 0, 0, "Corners") => "Types_Lupu_1_0_0_Corners",
            (1, 0, 0, "Omitted") => "Types_Lupu_1_0_0_Omitted",
            (1, 0, 1, "Origins") => "Types_Lupu_1_0_1_Origins",
            (1, 0, 1, "Displacement") => "Types_Lupu_1_0_1_Displacement",
            (1, 0, 1, "Frames") => "Types_Lupu_1_0_1_Frames",
            (1, 0, 1, "ShearY") => "Types_Lupu_1_0_1_ShearY",
            (1, 0, 1, "ShearZ") => "Types_Lupu_1_0_1_ShearZ",
            (1, 0, 1, "SourceVertices") => "Types_Lupu_1_0_1_SourceVertices",
            (1, 0, 1, "Corners") => "Types_Lupu_1_0_1_Corners",
            (1, 0, 1, "Omitted") => "Types_Lupu_1_0_1_Omitted",
            (1, 0, 2, "Origins") => "Types_Lupu_1_0_2_Origins",
            (1, 0, 2, "Displacement") => "Types_Lupu_1_0_2_Displacement",
            (1, 0, 2, "Frames") => "Types_Lupu_1_0_2_Frames",
            (1, 0, 2, "ShearY") => "Types_Lupu_1_0_2_ShearY",
            (1, 0, 2, "ShearZ") => "Types_Lupu_1_0_2_ShearZ",
            (1, 0, 2, "SourceVertices") => "Types_Lupu_1_0_2_SourceVertices",
            (1, 0, 2, "Corners") => "Types_Lupu_1_0_2_Corners",
            (1, 0, 2, "Omitted") => "Types_Lupu_1_0_2_Omitted",
            (1, 1, 0, "Origins") => "Types_Lupu_1_1_0_Origins",
            (1, 1, 0, "Displacement") => "Types_Lupu_1_1_0_Displacement",
            (1, 1, 0, "Frames") => "Types_Lupu_1_1_0_Frames",
            (1, 1, 0, "ShearY") => "Types_Lupu_1_1_0_ShearY",
            (1, 1, 0, "ShearZ") => "Types_Lupu_1_1_0_ShearZ",
            (1, 1, 0, "SourceVertices") => "Types_Lupu_1_1_0_SourceVertices",
            (1, 1, 0, "Corners") => "Types_Lupu_1_1_0_Corners",
            (1, 1, 0, "Omitted") => "Types_Lupu_1_1_0_Omitted",
            (1, 1, 1, "Origins") => "Types_Lupu_1_1_1_Origins",
            (1, 1, 1, "Displacement") => "Types_Lupu_1_1_1_Displacement",
            (1, 1, 1, "Frames") => "Types_Lupu_1_1_1_Frames",
            (1, 1, 1, "ShearY") => "Types_Lupu_1_1_1_ShearY",
            (1, 1, 1, "ShearZ") => "Types_Lupu_1_1_1_ShearZ",
            (1, 1, 1, "SourceVertices") => "Types_Lupu_1_1_1_SourceVertices",
            (1, 1, 1, "Corners") => "Types_Lupu_1_1_1_Corners",
            (1, 1, 1, "Omitted") => "Types_Lupu_1_1_1_Omitted",
            (1, 1, 2, "Origins") => "Types_Lupu_1_1_2_Origins",
            (1, 1, 2, "Displacement") => "Types_Lupu_1_1_2_Displacement",
            (1, 1, 2, "Frames") => "Types_Lupu_1_1_2_Frames",
            (1, 1, 2, "ShearY") => "Types_Lupu_1_1_2_ShearY",
            (1, 1, 2, "ShearZ") => "Types_Lupu_1_1_2_ShearZ",
            (1, 1, 2, "SourceVertices") => "Types_Lupu_1_1_2_SourceVertices",
            (1, 1, 2, "Corners") => "Types_Lupu_1_1_2_Corners",
            (1, 1, 2, "Omitted") => "Types_Lupu_1_1_2_Omitted",
























            (0, 0, 3, "Origins") => "Types_Lupu_0_0_3_Origins",
            (0, 0, 3, "Displacement") => "Types_Lupu_0_0_3_Displacement",
            (0, 0, 3, "Frames") => "Types_Lupu_0_0_3_Frames",
            (0, 0, 3, "ShearY") => "Types_Lupu_0_0_3_ShearY",
            (0, 0, 3, "ShearZ") => "Types_Lupu_0_0_3_ShearZ",
            (0, 0, 3, "SourceVertices") => "Types_Lupu_0_0_3_SourceVertices",
            (0, 0, 3, "Corners") => "Types_Lupu_0_0_3_Corners",
            (0, 0, 3, "Omitted") => "Types_Lupu_0_0_3_Omitted",
            (0, 0, 4, "Origins") => "Types_Lupu_0_0_4_Origins",
            (0, 0, 4, "Displacement") => "Types_Lupu_0_0_4_Displacement",
            (0, 0, 4, "Frames") => "Types_Lupu_0_0_4_Frames",
            (0, 0, 4, "ShearY") => "Types_Lupu_0_0_4_ShearY",
            (0, 0, 4, "ShearZ") => "Types_Lupu_0_0_4_ShearZ",
            (0, 0, 4, "SourceVertices") => "Types_Lupu_0_0_4_SourceVertices",
            (0, 0, 4, "Corners") => "Types_Lupu_0_0_4_Corners",
            (0, 0, 4, "Omitted") => "Types_Lupu_0_0_4_Omitted",
            (0, 1, 3, "Origins") => "Types_Lupu_0_1_3_Origins",
            (0, 1, 3, "Displacement") => "Types_Lupu_0_1_3_Displacement",
            (0, 1, 3, "Frames") => "Types_Lupu_0_1_3_Frames",
            (0, 1, 3, "ShearY") => "Types_Lupu_0_1_3_ShearY",
            (0, 1, 3, "ShearZ") => "Types_Lupu_0_1_3_ShearZ",
            (0, 1, 3, "SourceVertices") => "Types_Lupu_0_1_3_SourceVertices",
            (0, 1, 3, "Corners") => "Types_Lupu_0_1_3_Corners",
            (0, 1, 3, "Omitted") => "Types_Lupu_0_1_3_Omitted",
            (0, 1, 4, "Origins") => "Types_Lupu_0_1_4_Origins",
            (0, 1, 4, "Displacement") => "Types_Lupu_0_1_4_Displacement",
            (0, 1, 4, "Frames") => "Types_Lupu_0_1_4_Frames",
            (0, 1, 4, "ShearY") => "Types_Lupu_0_1_4_ShearY",
            (0, 1, 4, "ShearZ") => "Types_Lupu_0_1_4_ShearZ",
            (0, 1, 4, "SourceVertices") => "Types_Lupu_0_1_4_SourceVertices",
            (0, 1, 4, "Corners") => "Types_Lupu_0_1_4_Corners",
            (0, 1, 4, "Omitted") => "Types_Lupu_0_1_4_Omitted",
            (1, 0, 3, "Origins") => "Types_Lupu_1_0_3_Origins",
            (1, 0, 3, "Displacement") => "Types_Lupu_1_0_3_Displacement",
            (1, 0, 3, "Frames") => "Types_Lupu_1_0_3_Frames",
            (1, 0, 3, "ShearY") => "Types_Lupu_1_0_3_ShearY",
            (1, 0, 3, "ShearZ") => "Types_Lupu_1_0_3_ShearZ",
            (1, 0, 3, "SourceVertices") => "Types_Lupu_1_0_3_SourceVertices",
            (1, 0, 3, "Corners") => "Types_Lupu_1_0_3_Corners",
            (1, 0, 3, "Omitted") => "Types_Lupu_1_0_3_Omitted",
            (1, 0, 4, "Origins") => "Types_Lupu_1_0_4_Origins",
            (1, 0, 4, "Displacement") => "Types_Lupu_1_0_4_Displacement",
            (1, 0, 4, "Frames") => "Types_Lupu_1_0_4_Frames",
            (1, 0, 4, "ShearY") => "Types_Lupu_1_0_4_ShearY",
            (1, 0, 4, "ShearZ") => "Types_Lupu_1_0_4_ShearZ",
            (1, 0, 4, "SourceVertices") => "Types_Lupu_1_0_4_SourceVertices",
            (1, 0, 4, "Corners") => "Types_Lupu_1_0_4_Corners",
            (1, 0, 4, "Omitted") => "Types_Lupu_1_0_4_Omitted",
            (1, 1, 3, "Origins") => "Types_Lupu_1_1_3_Origins",
            (1, 1, 3, "Displacement") => "Types_Lupu_1_1_3_Displacement",
            (1, 1, 3, "Frames") => "Types_Lupu_1_1_3_Frames",
            (1, 1, 3, "ShearY") => "Types_Lupu_1_1_3_ShearY",
            (1, 1, 3, "ShearZ") => "Types_Lupu_1_1_3_ShearZ",
            (1, 1, 3, "SourceVertices") => "Types_Lupu_1_1_3_SourceVertices",
            (1, 1, 3, "Corners") => "Types_Lupu_1_1_3_Corners",
            (1, 1, 3, "Omitted") => "Types_Lupu_1_1_3_Omitted",
            (1, 1, 4, "Origins") => "Types_Lupu_1_1_4_Origins",
            (1, 1, 4, "Displacement") => "Types_Lupu_1_1_4_Displacement",
            (1, 1, 4, "Frames") => "Types_Lupu_1_1_4_Frames",
            (1, 1, 4, "ShearY") => "Types_Lupu_1_1_4_ShearY",
            (1, 1, 4, "ShearZ") => "Types_Lupu_1_1_4_ShearZ",
            (1, 1, 4, "SourceVertices") => "Types_Lupu_1_1_4_SourceVertices",
            (1, 1, 4, "Corners") => "Types_Lupu_1_1_4_Corners",
            (1, 1, 4, "Omitted") => "Types_Lupu_1_1_4_Omitted",
















        };
        return HardcodedNumbers.Load(key);
    }
}
// Embedded numerical arrays: Types_Lupu_Parameters


