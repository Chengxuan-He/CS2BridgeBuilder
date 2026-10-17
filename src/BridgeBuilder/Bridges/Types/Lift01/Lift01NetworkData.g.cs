// Native LiftBridge01 network measurements, game 1.6.2f1.
// Source audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static class Lift01NetworkData
{
    // Roles come from the resolved selected-road mapping, never from names or width guesses.
    internal enum Role { Side, Deck }

    internal static NetSectionInfo[] ConvertSections(string owner, NetSectionInfo[] source,
        IReadOnlyList<Role> roles, out PrefabBase[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift01 requires a measured role for each source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = 0; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                Role.Side => SideBranches(),
                Role.Deck => DeckBranches(),
                _ => throw new InvalidOperationException("Unknown Lift01 section role: " + roles[i])
            };
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections,
            out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] SideBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
    };

    private static MovableSectionConversion.Branch[] DeckBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-1), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(2), false),
    };

    // No extra edge/node state rules exist on the measured Lift01 network.
    internal static void ApplyBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = 168.0f;
        bridge.m_Hanging = 0.0f;
        bridge.m_ElevationOnWater = 10.0f;
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)0;
        bridge.m_BuildStyle = (BridgeBuildStyle)0;
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    // Supply generated Lift01 objects and the selected network outside connection.
    internal static NetSubObjectInfo[] Placements(ObjectPrefab pillar, ObjectPrefab mechanism,
        ObjectPrefab outsideConnection) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(0.0f, -2.75f, 13.5f),
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
            m_Position = new float3(0.0f, -2.75f, 0.0f),
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
            m_Position = new float3(0.0f, 77.87999725341797f, 0.0f),
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
}
