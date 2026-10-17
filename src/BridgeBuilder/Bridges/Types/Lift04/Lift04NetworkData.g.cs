// Native LiftBridge04 network measurements, game 1.6.2f1.
// Source audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static class Lift04NetworkData
{
    // Roles come from the resolved selected-road mapping, never from names or width guesses.
    internal enum Role { IntersectionMedian, Span }

    internal static NetSectionInfo[] ConvertSections(string owner, NetSectionInfo[] source,
        IReadOnlyList<Role> roles, out PrefabBase[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift04 requires a measured role for each source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = 0; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                Role.IntersectionMedian => IntersectionMedianBranches(),
                Role.Span => SpanBranches(),
                _ => throw new InvalidOperationException("Unknown Lift04 section role: " + roles[i])
            };
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections,
            out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] IntersectionMedianBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Intersection }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
    };

    private static MovableSectionConversion.Branch[] SpanBranches() => new[]
    {
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(0), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-1), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(0), true),
    };

    // No extra edge/node state rules exist on the measured Lift04 network.
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

    // Supply the native pillar placement object and private generated mechanism.
    internal static NetSubObjectInfo[] Placements(ObjectPrefab pillar, ObjectPrefab mechanism) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(0.0f, -3.0f, 0.0f),
            m_Rotation = new quaternion(0.0f, 0.0f, 0.0f, 1.0f),
            m_Placement = (NetObjectPlacement)17,
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
            m_Position = new float3(0.0f, -3.0f, 0.0f),
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
            m_Position = new float3(0.0f, 68.75f, 0.0f),
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
    };
}
