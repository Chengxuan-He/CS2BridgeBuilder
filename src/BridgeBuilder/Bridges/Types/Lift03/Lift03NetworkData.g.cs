// Native LiftBridge03 main and auxiliary network measurements, game 1.6.2f1.
// Source audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static class Lift03NetworkData
{
    internal enum MainRole { Side, Deck }

    internal static NetSectionInfo[] ConvertMainSections(string owner, NetSectionInfo[] source,
        IReadOnlyList<MainRole> roles, out NetSectionPrefab[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift03 main mapping needs one role per source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = 0; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                MainRole.Side => MainSideBranches(),
                MainRole.Deck => MainDeckBranches(),
                _ => throw new InvalidOperationException("Unknown Lift03 section role: " + roles[i])
            };
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections,
            out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] MainSideBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
    };

    private static MovableSectionConversion.Branch[] MainDeckBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-1), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(2), false),
    };

    internal enum AuxiliaryRole { Side, TrackOrShoulder, Median }

    internal static NetSectionInfo[] ConvertAuxiliarySections(string owner, NetSectionInfo[] source,
        IReadOnlyList<AuxiliaryRole> roles, out NetSectionPrefab[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift03 auxiliary mapping needs one role per source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = 0; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                AuxiliaryRole.Side => AuxiliarySideBranches(),
                AuxiliaryRole.TrackOrShoulder => AuxiliaryTrackOrShoulderBranches(),
                AuxiliaryRole.Median => AuxiliaryMedianBranches(),
                _ => throw new InvalidOperationException("Unknown Lift03 section role: " + roles[i])
            };
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections,
            out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] AuxiliarySideBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
    };

    private static MovableSectionConversion.Branch[] AuxiliaryTrackOrShoulderBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-1), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(2), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(2), true),
    };

    private static MovableSectionConversion.Branch[] AuxiliaryMedianBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-1), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(2), false),
    };

    internal static void ApplyMainBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = 200.0f;
        bridge.m_Hanging = 0.0f;
        bridge.m_ElevationOnWater = 20.0f;
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)0;
        bridge.m_BuildStyle = (BridgeBuildStyle)0;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    internal static void ApplyAuxiliaryBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = 200.0f;
        bridge.m_Hanging = 0.0f;
        bridge.m_ElevationOnWater = 10.0f;
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)0;
        bridge.m_BuildStyle = (BridgeBuildStyle)0;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    // Apply once to the private main network. The auxiliary network adds no state rules.
    internal static void AddMainStates(NetGeometryPrefab target)
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
                m_SetState = new[] { (NetPieceRequirements)51 },
                m_MatchType = (NetEdgeMatchType)0,
            },
        }).ToArray();
    }

    internal static NetSubObjectInfo[] MainPlacements(ObjectPrefab pillar, ObjectPrefab mechanism, ObjectPrefab outsideConnection) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(0.0f, 0.0f, 5.0f),
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
            m_Position = new float3(0.0f, 0.0f, 0.0f),
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
        },
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

    internal static NetSubObjectInfo[] AuxiliaryPlacements(ObjectPrefab outsideConnection) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = outsideConnection,
            m_Position = new float3(0.0f, 8.0f, 0.0f),
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

    // The supplied auxiliary is the generated selected lower network, not the native track.
    internal static void ApplyAuxiliary(AuxiliaryNets target, NetPrefab auxiliary)
    {
        target.m_LinkEndOffsets = true;
        target.m_AuxiliaryNets = new[]
        {
            new AuxiliaryNetInfo
            {
                m_Prefab = auxiliary,
                m_Position = new float3(0.0f, -10.0f, 0.0f),
                m_InvertWhen = (NetInvertMode)0
            }
        };
    }
}
