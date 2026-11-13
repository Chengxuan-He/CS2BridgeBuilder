using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Colossal.Mathematics;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class CoveredPedestrianBridgeGenerator : BridgeGeneratorBase
{
    internal CoveredPedestrianBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "CoveredPedestrian";
    protected override bool HasRecordedConstruction => true;

    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, List<PrefabBase> created) => MovableUnlockInputs.Apply(target, _report, created, new[] { new PrefabID("NetSectionPrefab", "Pavement Path Side Section 0"), new PrefabID("NetSectionPrefab", "Pavement Path Section 3"), new PrefabID("AggregateNetPrefab", "Pathway"), new PrefabID("ServicePrefab", "Landscaping"), new PrefabID("UIAssetCategoryPrefab", "Pathways") }, new[] { new PrefabID("FeaturePrefab", "Pathways") }, Array.Empty<PrefabID>(), false);
    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant, TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    {
        using var source = new CoveredPedestrianConstruction();
        var extra = targetWidth - CoveredPedestrianConstruction.RoadWidth;
        var piece = await factory.CreateRecordedRenderAsync(source.Piece, target.name + " CoveredRoof", extra / Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[0], new[] { CoveredPedestrianConstruction.Coefficients }, new[] { CoveredPedestrianConstruction.FrameCoefficients });
        piece.m_Layer = source.Piece.m_Layer;
        piece.m_Width = source.Piece.m_Width + extra;
        piece.m_Length = source.Piece.m_Length;
        piece.m_HeightRange = source.Piece.m_HeightRange;
        piece.m_WidthOffset = source.Piece.m_WidthOffset;
        piece.m_NodeOffset = source.Piece.m_NodeOffset;
        piece.m_SideConnectionOffset = source.Piece.m_SideConnectionOffset;
        piece.m_SurfaceHeights = source.Piece.m_SurfaceHeights;
        var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
        section.name = target.name + " CoveredRoofSection";
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
        factory.AdoptMovableSections(new PrefabBase[] { section });
        target.AddOrGetComponent<OverheadNetSections>().m_Sections = new[]
        {
            new NetSectionInfo
            {
                m_Section = section,
                m_Median = true,
                m_RequireAll = Array.Empty<NetPieceRequirements>(),
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>()
            }
        };
        var bridge = target.AddOrGetComponent<Bridge>();
        bridge.m_SegmentLength = Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[1];
        bridge.m_Hanging = Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[2];
        bridge.m_ElevationOnWater = Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[3];
        bridge.m_CanCurve = false;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = BridgeWaterFlow.Any;
        bridge.m_BuildStyle = BridgeBuildStyle.Elevated;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
        bridge.active = true;
        var objects = target.AddOrGetComponent<NetSubObjects>();
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(item => item == null || !item.m_RequireElevated).Concat(new[] { new NetSubObjectInfo { m_Object = factory.ResolveOriginal<ObjectPrefab>("Pillar Pedestrian Placeholder"), m_Position = new float3(Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[4], -Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[5], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[6]), m_Rotation = quaternion.identity, m_Placement = NetObjectPlacement.Node, m_RequireElevated = true } }).ToArray();
        var placeable = target.AddOrGetComponent<PlaceableNet>();
        placeable.m_ElevationRange = new Bounds1(Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[7], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[8]);
        placeable.m_AllowParallelMode = true;
        placeable.m_DisableZoneOverlay = false;
        placeable.m_UndergroundPrefab = null;
        placeable.m_XPReward = Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.I32[0];
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>("Pathway");
    }
}

internal sealed class CoveredPedestrianConstruction : MovableSourceRenders
{
    internal const string NativeName = "Covered Pedestrian Bridge";
    internal const string ResourceAnchorName = "Pedestrian Covered Bridge Piece";
    internal const string Icon = "Media/Game/Icons/CoveredPedestrian.svg";
    internal static float RoadWidth => Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[9];
    internal static float StructureWidth => Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[10];
    internal static int Clearance => Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.I32[1];
    internal static float[] Coefficients => GenerationParameters.Get<HardcodedNumbers>("CoveredPedestrian.Membership", () => HardcodedNumbers.Load("Types_CoveredPedestrian_Membership")).F32;
    internal static float[] FrameCoefficients => GenerationParameters.Get<HardcodedNumbers>("CoveredPedestrian.Frames", () => HardcodedNumbers.Load("Types_CoveredPedestrian_Frames")).F32;
    internal NetPiecePrefab Piece { get; }

    internal CoveredPedestrianConstruction()
    {
        try
        {
            Piece = Create<NetPiecePrefab>(ResourceAnchorName, "fb756834d6da50c0bce6692b1bfafb2e", new[] { "5d2454994f1ec785bae61f9314cd0975" }, new Bounds3(new float3(-Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[11], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[12], -Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[13]), new float3(Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[14], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[15], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[16])), Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.I32[2], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.I32[3], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.I32[4], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[17]);
            Piece.m_Layer = NetPieceLayer.Top;
            Piece.m_Width = StructureWidth;
            Piece.m_Length = Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[18];
            Piece.m_HeightRange = new Bounds1(Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[19], Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs.Values.F32[20]);
            Piece.AddOrGetComponent<NetPieceTiling>().m_DisableTextureTiling = true;
        }
        catch
        {
            Dispose();
            throw;
        }
    }
}

// Embedded numerical arrays: Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs
internal static class Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs
{
    static Numeric_Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs()
    {
    }

    internal static HardcodedNumbers Values => HardcodedNumbers.Load("Types_CoveredPedestrian_CoveredPedestrianBridgeGenerator_cs");
}
