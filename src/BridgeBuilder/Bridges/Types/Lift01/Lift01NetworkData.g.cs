// Native LiftBridge01 network measurements, game 1.6.2f1.
// Source audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static class Lift01NetworkData
{
    // Roles come from the resolved selected-road mapping, never from names or width guesses.
    internal enum Role
    {
        Side,
        Deck
    }

    internal static NetSectionInfo[] ConvertSections(string owner, NetSectionInfo[] source, IReadOnlyList<Role> roles, out PrefabBase[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift01 requires a measured role for each source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[0]; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                Role.Side => SideBranches(),
                Role.Deck => DeckBranches(),
                _ => throw new InvalidOperationException("Unknown Lift01 section role: " + roles[i])};
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections, out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] SideBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[1]), false),
    };
    private static MovableSectionConversion.Branch[] DeckBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[2]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[3]), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[4]), false),
    };
    // No extra edge/node state rules exist on the measured Lift01 network.
    internal static void ApplyBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[0];
        bridge.m_Hanging = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[1];
        bridge.m_ElevationOnWater = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[2];
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[5];
        bridge.m_BuildStyle = (BridgeBuildStyle)Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[6];
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    // Supply generated Lift01 objects and the selected network outside connection.
    internal static NetSubObjectInfo[] Placements(ObjectPrefab pillar, ObjectPrefab mechanism, ObjectPrefab outsideConnection) => StructurePlacements(pillar, mechanism).Concat(new[] { new NetSubObjectInfo { m_Object = outsideConnection, m_Position = new float3(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[3], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[4], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[5]), m_Rotation = quaternion.identity, m_Placement = (NetObjectPlacement)Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[7], m_FixedIndex = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[8], m_Spacing = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[6], m_AnchorTop = false, m_AnchorCenter = false, m_RequireElevated = false, m_RequireOutsideConnection = true, m_RequireDeadEnd = false, m_RequireOrphan = false } }).ToArray();
    internal static NetSubObjectInfo[] StructurePlacements(ObjectPrefab pillar, ObjectPrefab mechanism) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[7], -Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[8], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[9]),
            m_Rotation = new quaternion(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[10], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[11], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[12], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[13]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[9],
            m_FixedIndex = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[10],
            m_Spacing = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[14],
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
            m_Position = new float3(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[15], -Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[16], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[17]),
            m_Rotation = new quaternion(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[18], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[19], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[20], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[21]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[11],
            m_FixedIndex = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[12],
            m_Spacing = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[22],
            m_AnchorTop = false,
            m_AnchorCenter = false,
            m_RequireElevated = true,
            m_RequireOutsideConnection = false,
            m_RequireDeadEnd = false,
            m_RequireOrphan = false,
        },
        MechanismPlacement(mechanism),
    };
    internal static NetSubObjectInfo MechanismPlacement(ObjectPrefab mechanism) => new NetSubObjectInfo
    {
        m_Object = mechanism,
        m_Position = new float3(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[23], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[24], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[25]),
        m_Rotation = new quaternion(Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[26], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[27], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[28], Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[29]),
        m_Placement = (NetObjectPlacement)Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[13],
        m_FixedIndex = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.I32[14],
        m_Spacing = Numeric_Types_Lift01_Lift01NetworkData_g_cs.Values.F32[30],
        m_AnchorTop = false,
        m_AnchorCenter = false,
        m_RequireElevated = false,
        m_RequireOutsideConnection = false,
        m_RequireDeadEnd = false,
        m_RequireOrphan = false,
    };
}

// Embedded numerical arrays: Types_Lift01_Lift01NetworkData_g_cs
internal static class Numeric_Types_Lift01_Lift01NetworkData_g_cs
{
    static Numeric_Types_Lift01_Lift01NetworkData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01NetworkData_g_cs");
}
