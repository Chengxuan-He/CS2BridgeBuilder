using System;
using System.Linq;
using System.Threading.Tasks;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Recorded from the native Arc Pedestrian Bridge. The only render has no external LODs.
internal static class ArcPedestrianConstruction
{
    internal const string NativeName = "Arc Pedestrian Bridge";
    internal const string ResourceAnchorName = "Pedestrian Arc Bridge Piece";
    internal const string Icon = "Media/Game/Icons/ArchPedestrian.svg";
    internal static float RoadWidth => Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[0];
    internal static float StructureWidth => Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[1];
    internal static int Clearance => Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.I32[0];
    private static HardcodedNumbers Displacements => GenerationParameters.Get<HardcodedNumbers>("ArcPedestrian.Displacements", () => HardcodedNumbers.Load("Types_ArcPedestrian_Coefficients"));
    private static HardcodedNumbers Frames => GenerationParameters.Get<HardcodedNumbers>("ArcPedestrian.Frames", () => HardcodedNumbers.Load("Types_ArcPedestrian_Frames"));

    internal static async Task ApplyAsync(NetGeometryPrefab target, TowerFactory factory, float width)
    {
        using var inputs = new ArcPedestrianSourceRenders();
        var halfDelta = (width - RoadWidth) / Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[2];
        var authoredBounds = inputs.Piece.bounds;
        var a = authoredBounds.min - new float3(halfDelta, Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[3], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[4]);
        var b = authoredBounds.max + new float3(halfDelta, Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[5], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[6]);
        var piece = await factory.CreateRecordedRenderAsync(inputs.Piece, target.name + "-ArcPiece", halfDelta, new[] { Displacements.F32 }, new[] { Frames.F32 }, new Bounds3(math.min(a, b), math.max(a, b)));
        ConfigurePiece(piece, width - RoadWidth);
        var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
        section.name = target.name + "-ArcSection";
        factory.AdoptMovableSections(new PrefabBase[] { section });
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
                m_RequireNone = Array.Empty<NetPieceRequirements>(),
                m_Offset = float3.zero
            }
        };
        var overhead = target.AddOrGetComponent<OverheadNetSections>();
        overhead.active = true;
        overhead.m_Sections = new[]
        {
            new NetSectionInfo
            {
                m_Section = section,
                m_Median = true,
                m_RequireAll = Array.Empty<NetPieceRequirements>(),
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>(),
                m_Offset = float3.zero
            }
        };
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.active = true;
        bridge.m_SegmentLength = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[7];
        bridge.m_Hanging = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[8];
        bridge.m_ElevationOnWater = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[9];
        bridge.m_CanCurve = false;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = BridgeWaterFlow.Any;
        bridge.m_BuildStyle = BridgeBuildStyle.Elevated;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
        var placement = target.AddOrGetComponent<PlaceableNet>();
        placement.m_ElevationRange = new Bounds1(Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[10], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[11]);
        placement.m_AllowParallelMode = true;
        placement.m_DisableZoneOverlay = false;
        placement.m_UndergroundPrefab = null;
        placement.m_XPReward = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.I32[1];
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>("Pathway");
        // The centered native support remains rigid; only the superstructure changes width.
        var objects = target.AddOrGetComponent<NetSubObjects>();
        var retained = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(item => item == null || !item.m_RequireElevated);
        objects.m_SubObjects = retained.Concat(new[] { new NetSubObjectInfo { m_Object = factory.ResolveOriginal<StaticObjectPrefab>("Pillar Pedestrian Placeholder"), m_Position = new float3(Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[12], -Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[13], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[14]), m_Rotation = quaternion.identity, m_Placement = NetObjectPlacement.Node, m_RequireElevated = true } }).ToArray();
        objects.active = true;
    }

    internal static void ConfigurePiece(NetPiecePrefab piece, float extra)
    {
        piece.m_Layer = NetPieceLayer.Top;
        piece.m_Width = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[15] + extra;
        piece.m_Length = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[16];
        piece.m_HeightRange = new Bounds1(Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[17], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[18]);
        piece.m_WidthOffset = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[19];
        piece.m_NodeOffset = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[20];
        piece.m_SideConnectionOffset = Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[21];
        piece.m_SurfaceHeights = float4.zero;
        piece.AddOrGetComponent<NetPieceTiling>().m_DisableTextureTiling = true;
    }
}

internal sealed class ArcPedestrianSourceRenders : MovableSourceRenders
{
    internal NetPiecePrefab Piece { get; }

    internal ArcPedestrianSourceRenders()
    {
        try
        {
            Piece = Create<NetPiecePrefab>(ArcPedestrianConstruction.ResourceAnchorName, "868884d0d55c3ec427cf44fdc8d4a322", new[] { "5d2454994f1ec785bae61f9314cd0975" }, new Bounds3(new float3(-Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[22], -Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[23], -Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[24]), new float3(Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[25], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[26], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[27])), Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.I32[2], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.I32[3], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.I32[4], Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[28]);
            ArcPedestrianConstruction.ConfigurePiece(Piece, Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs.Values.F32[29]);
            Roots = new RenderPrefab[]
            {
                Piece
            };
        }
        catch
        {
            Dispose();
            throw;
        }
    }
}

// Embedded numerical arrays: Types_ArcPedestrian_ArcPedestrianConstruction_cs
internal static class Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs
{
    static Numeric_Types_ArcPedestrian_ArcPedestrianConstruction_cs()
    {
    }

    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_ArcPedestrian_ArcPedestrianConstruction_cs");
}
