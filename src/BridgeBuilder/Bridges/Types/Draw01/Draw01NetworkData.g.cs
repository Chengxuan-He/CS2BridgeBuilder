// Native DrawBridge01 network measurements, game 1.6.2f1.
// Source audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// No runtime prototype lookup or road-section-name classification.
using System;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static class Draw01NetworkData
{
    // Every one of the seven native lateral occurrences has these same three branches.
    // Selected-road conditions and lateral roles remain on the outer occurrence.
    internal static NetSectionInfo[] ConvertSections(string owner, NetSectionInfo[] source,
        out PrefabBase[] created)
    {
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = 0; i < source.Length; i++) rules[i] = Branches();
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections,
            out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] Branches() => new[]
    {
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-1), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(0), true),
    };

    internal static void ApplyBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = 144.0f;
        bridge.m_Hanging = 0.0f;
        bridge.m_ElevationOnWater = 10.0f;
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)0;
        bridge.m_BuildStyle = (BridgeBuildStyle)0;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    // Apply once to a private selected-road clone, preserving its existing state rules.
    internal static void AddMechanismStates(NetGeometryPrefab target)
    {
        target.m_EdgeStates = (target.m_EdgeStates ?? Array.Empty<NetEdgeStateInfo>()).Concat(new[]
        {
            new NetEdgeStateInfo
            {
                m_RequireAll = Array.Empty<NetPieceRequirements>(),
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>(),
                m_SetState = new[] { (NetPieceRequirements)51 },
            },
        }).ToArray();
        target.m_NodeStates = (target.m_NodeStates ?? Array.Empty<NetNodeStateInfo>()).Concat(new[]
        {
            new NetNodeStateInfo
            {
                m_RequireAll = Array.Empty<NetPieceRequirements>(),
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = Array.Empty<NetPieceRequirements>(),
                m_SetState = new[] { (NetPieceRequirements)24, (NetPieceRequirements)31, (NetPieceRequirements)51 },
                m_MatchType = (NetEdgeMatchType)0,
            },
        }).ToArray();
    }

    // The caller supplies private generated pillar/mechanism objects and the selected
    // road outside-connection object. Original native placement fields are preserved.
    internal static NetSubObjectInfo[] Placements(ObjectPrefab pillar, ObjectPrefab mechanism,
        ObjectPrefab outsideConnection) => StructurePlacements(pillar, mechanism)
            .Concat(OutsidePlacements(outsideConnection)).ToArray();

    internal static NetSubObjectInfo[] StructurePlacements(ObjectPrefab pillar, ObjectPrefab mechanism) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(0.0f, -4.0f, 3.0f),
            m_Rotation = new quaternion(0.0f, 0.0f, 0.0f, 1.0f),
            m_Placement = (NetObjectPlacement)19,
            m_FixedIndex = 0, m_Spacing = 0.0f,
            m_AnchorTop = false,
            m_AnchorCenter = false,
            m_RequireElevated = true,
            m_RequireOutsideConnection = false,
            m_RequireDeadEnd = false,
            m_RequireOrphan = false,
        },
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(0.0f, -4.0f, 0.0f),
            m_Rotation = new quaternion(0.0f, 0.0f, 0.0f, 1.0f),
            m_Placement = (NetObjectPlacement)18,
            m_FixedIndex = 0, m_Spacing = 0.0f,
            m_AnchorTop = false,
            m_AnchorCenter = false,
            m_RequireElevated = true,
            m_RequireOutsideConnection = false,
            m_RequireDeadEnd = false,
            m_RequireOrphan = false,
        },
        MechanismPlacement(mechanism),
    };

    // Used by private closed-road sampling and by the persisted network. The
    // mechanism's node anchoring must not acquire a second set of caller defaults.
    internal static NetSubObjectInfo MechanismPlacement(ObjectPrefab mechanism) =>
        new NetSubObjectInfo
        {
            m_Object = mechanism,
            m_Position = new float3(0.0f, 0.0f, 0.0f),
            m_Rotation = new quaternion(0.0f, 0.0f, 0.0f, 1.0f),
            m_Placement = (NetObjectPlacement)16,
            m_FixedIndex = 0, m_Spacing = 0.0f,
            m_AnchorTop = false,
            m_AnchorCenter = false,
            m_RequireElevated = false,
            m_RequireOutsideConnection = false,
            m_RequireDeadEnd = false,
            m_RequireOrphan = false,
        };

    private static NetSubObjectInfo[] OutsidePlacements(ObjectPrefab outsideConnection) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = outsideConnection,
            m_Position = new float3(0.0f, 5.0f, 0.0f),
            m_Rotation = new quaternion(0.0f, 0.0f, 0.0f, 1.0f),
            m_Placement = (NetObjectPlacement)0,
            m_FixedIndex = 0, m_Spacing = 0.0f,
            m_AnchorTop = false,
            m_AnchorCenter = false,
            m_RequireElevated = false,
            m_RequireOutsideConnection = true,
            m_RequireDeadEnd = false,
            m_RequireOrphan = false,
        },
    };
}
