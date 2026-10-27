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
    internal static float ApproachLength => Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[0];
    internal static float MainElevation => Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[1];

    internal static readonly float3 AuxiliaryOffset = new(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[2], -Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[3], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[4]);
    internal enum MainRole
    {
        Side,
        Deck
    }

    internal static NetSectionInfo[] ConvertMainSections(string owner, NetSectionInfo[] source, IReadOnlyList<MainRole> roles, out PrefabBase[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift03 main mapping needs one role per source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[0]; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                MainRole.Side => MainSideBranches(),
                MainRole.Deck => MainDeckBranches(),
                _ => throw new InvalidOperationException("Unknown Lift03 section role: " + roles[i])};
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections, out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] MainSideBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[1]), false),
    };
    private static MovableSectionConversion.Branch[] MainDeckBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[2]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[3]), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[4]), false),
    };
    internal enum AuxiliaryRole
    {
        Side,
        TrackOrShoulder,
        Median
    }

    internal static NetSectionInfo[] ConvertAuxiliarySections(string owner, NetSectionInfo[] source, IReadOnlyList<AuxiliaryRole> roles, out PrefabBase[] created)
    {
        if (roles.Count != source.Length)
            throw new InvalidOperationException("Lift03 auxiliary mapping needs one role per source occurrence.");
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[5]; i < source.Length; i++)
            rules[i] = roles[i] switch
            {
                AuxiliaryRole.Side => AuxiliarySideBranches(),
                AuxiliaryRole.TrackOrShoulder => AuxiliaryTrackOrShoulderBranches(),
                AuxiliaryRole.Median => AuxiliaryMedianBranches(),
                _ => throw new InvalidOperationException("Unknown Lift03 section role: " + roles[i])};
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections, out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] AuxiliarySideBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[6]), false),
    };
    private static MovableSectionConversion.Branch[] AuxiliaryTrackOrShoulderBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[7]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[8]), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[9]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[10]), true),
    };
    private static MovableSectionConversion.Branch[] AuxiliaryMedianBranches() => new[]
    {
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[11]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[12]), false),
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[13]), false),
    };
    internal static void ApplyMainBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = ApproachLength;
        bridge.m_Hanging = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[5];
        bridge.m_ElevationOnWater = MainElevation;
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[14];
        bridge.m_BuildStyle = (BridgeBuildStyle)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[15];
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    internal static void ApplyAuxiliaryBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = ApproachLength;
        bridge.m_Hanging = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[6];
        bridge.m_ElevationOnWater = MainElevation + AuxiliaryOffset.y;
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[16];
        bridge.m_BuildStyle = (BridgeBuildStyle)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[17];
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    // Apply once to the private main network. The auxiliary network adds no state rules.
    internal static void AddMainStates(NetGeometryPrefab target)
    {
        target.m_EdgeStates = (target.m_EdgeStates ?? Array.Empty<NetEdgeStateInfo>()).Concat(new[] { new NetEdgeStateInfo { m_RequireAll = Array.Empty<NetPieceRequirements>(), m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>(), m_SetState = new[] { (NetPieceRequirements)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[18] }, }, }).ToArray();
        target.m_NodeStates = (target.m_NodeStates ?? Array.Empty<NetNodeStateInfo>()).Concat(new[] { new NetNodeStateInfo { m_RequireAll = Array.Empty<NetPieceRequirements>(), m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>(), m_SetState = new[] { (NetPieceRequirements)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[19] }, m_MatchType = (NetEdgeMatchType)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[20], }, }).ToArray();
    }

    internal static NetSubObjectInfo[] StructurePlacements(ObjectPrefab pillar, ObjectPrefab mechanism) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[7], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[8], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[9]),
            m_Rotation = new quaternion(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[10], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[11], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[12], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[13]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[21],
            m_FixedIndex = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[22],
            m_Spacing = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[14],
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
            m_Position = new float3(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[15], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[16], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[17]),
            m_Rotation = new quaternion(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[18], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[19], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[20], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[21]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[23],
            m_FixedIndex = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[24],
            m_Spacing = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[22],
            m_AnchorTop = false,
            m_AnchorCenter = false,
            m_RequireElevated = true,
            m_RequireOutsideConnection = false,
            m_RequireDeadEnd = false,
            m_RequireOrphan = false,
        },
        MechanismPlacement(mechanism),
    };
    internal static NetSubObjectInfo[] MainPlacements(ObjectPrefab pillar, ObjectPrefab mechanism, ObjectPrefab outsideConnection) => StructurePlacements(pillar, mechanism).Concat(new[] { new NetSubObjectInfo { m_Object = outsideConnection, m_Position = new float3(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[23], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[24], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[25]), m_Rotation = new quaternion(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[26], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[27], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[28], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[29]), m_Placement = (NetObjectPlacement)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[25], m_FixedIndex = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[26], m_Spacing = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[30], m_AnchorTop = false, m_AnchorCenter = false, m_RequireElevated = false, m_RequireOutsideConnection = true, m_RequireDeadEnd = false, m_RequireOrphan = false, }, }).ToArray();
    internal static NetSubObjectInfo MechanismPlacement(ObjectPrefab mechanism) => new()
    {
        m_Object = mechanism,
        m_Position = new float3(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[31], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[32], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[33]),
        m_Rotation = quaternion.identity,
        m_Placement = (NetObjectPlacement)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[27],
        m_FixedIndex = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[28],
        m_Spacing = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[34],
        m_AnchorTop = false,
        m_AnchorCenter = false,
        m_RequireElevated = false,
        m_RequireOutsideConnection = false,
        m_RequireDeadEnd = false,
        m_RequireOrphan = false,
    };
    internal static NetSubObjectInfo[] AuxiliaryPlacements(ObjectPrefab outsideConnection) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = outsideConnection,
            m_Position = new float3(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[35], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[36], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[37]),
            m_Rotation = new quaternion(Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[38], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[39], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[40], Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[41]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[29],
            m_FixedIndex = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[30],
            m_Spacing = Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.F32[42],
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
                m_Position = AuxiliaryOffset,
                m_InvertWhen = (NetInvertMode)Numeric_Types_Lift03_Lift03NetworkData_g_cs.Values.I32[31]
            }
        };
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03NetworkData_g_cs
internal static class Numeric_Types_Lift03_Lift03NetworkData_g_cs
{
    static Numeric_Types_Lift03_Lift03NetworkData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03NetworkData_g_cs");
}
