// Native DrawBridge03 network measurements, game 1.6.2f1.
// Source audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// No runtime prototype lookup or road-section-name classification.
using System;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static class Draw03NetworkData
{
    // Every one of the eleven native lateral occurrences has these same three branches.
    // Selected-road conditions and lateral roles remain on the outer occurrence.
    internal static NetSectionInfo[] ConvertSections(string owner, NetSectionInfo[] source, out PrefabBase[] created)
    {
        var rules = new MovableSectionConversion.Branch[source.Length][];
        for (var i = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[0]; i < source.Length; i++)
            rules[i] = Branches();
        if (!MovableSectionConversion.TryConvert(owner, source, rules, out var sections, out created, out _, out var error))
            throw new InvalidOperationException(error);
        return sections;
    }

    private static MovableSectionConversion.Branch[] Branches() => new[]
    {
        new MovableSectionConversion.Branch(Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.FixedNodeSize }, (NetPieceLayerMask)(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[1]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.Node, NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), Array.Empty<NetPieceRequirements>(), (NetPieceLayerMask)(-Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[2]), false),
        new MovableSectionConversion.Branch(new[] { NetPieceRequirements.FixedNodeSize }, Array.Empty<NetPieceRequirements>(), new[] { NetPieceRequirements.Node }, (NetPieceLayerMask)(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[3]), true),
    };
    internal static void ApplyBridge(Bridge bridge)
    {
        bridge.m_SegmentLength = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[0];
        bridge.m_Hanging = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[1];
        bridge.m_ElevationOnWater = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[2];
        bridge.m_CanCurve = true;
        bridge.m_AllowMinimalLength = false;
        bridge.m_WaterFlow = (BridgeWaterFlow)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[4];
        bridge.m_BuildStyle = (BridgeBuildStyle)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[5];
        bridge.m_FixedSegments = Array.Empty<FixedNetSegmentInfo>();
    }

    // Apply once to a private selected-road clone, preserving its existing state rules.
    internal static void AddMechanismStates(NetGeometryPrefab target)
    {
        target.m_EdgeStates = (target.m_EdgeStates ?? Array.Empty<NetEdgeStateInfo>()).Concat(new[] { new NetEdgeStateInfo { m_RequireAll = Array.Empty<NetPieceRequirements>(), m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>(), m_SetState = new[] { (NetPieceRequirements)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[6] }, }, }).ToArray();
        target.m_NodeStates = (target.m_NodeStates ?? Array.Empty<NetNodeStateInfo>()).Concat(new[] { new NetNodeStateInfo { m_RequireAll = Array.Empty<NetPieceRequirements>(), m_RequireAny = Array.Empty<NetPieceRequirements>(), m_RequireNone = Array.Empty<NetPieceRequirements>(), m_SetState = new[] { (NetPieceRequirements)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[7], (NetPieceRequirements)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[8], (NetPieceRequirements)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[9] }, m_MatchType = (NetEdgeMatchType)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[10], }, }).ToArray();
    }

    // Only structural placements are inherited. Outside connections retain the selected
    // network's complete original rules through Draw03NetworkAssembly.
    internal static NetSubObjectInfo[] StructurePlacements(ObjectPrefab pillar, ObjectPrefab mechanism) => new[]
    {
        new NetSubObjectInfo
        {
            m_Object = pillar,
            m_Position = new float3(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[3], -Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[4], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[5]),
            m_Rotation = new quaternion(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[6], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[7], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[8], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[9]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[11],
            m_FixedIndex = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[12],
            m_Spacing = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[10],
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
            m_Position = new float3(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[11], -Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[12], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[13]),
            m_Rotation = new quaternion(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[14], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[15], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[16], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[17]),
            m_Placement = (NetObjectPlacement)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[13],
            m_FixedIndex = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[14],
            m_Spacing = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[18],
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
        m_Position = new float3(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[19], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[20], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[21]),
        m_Rotation = new quaternion(Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[22], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[23], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[24], Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[25]),
        m_Placement = (NetObjectPlacement)Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[15],
        m_FixedIndex = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.I32[16],
        m_Spacing = Numeric_Types_Draw03_Draw03NetworkData_g_cs.Values.F32[26],
        m_AnchorTop = false,
        m_AnchorCenter = false,
        m_RequireElevated = false,
        m_RequireOutsideConnection = false,
        m_RequireDeadEnd = false,
        m_RequireOrphan = false,
    };
}

// Embedded numerical arrays: Types_Draw03_Draw03NetworkData_g_cs
internal static class Numeric_Types_Draw03_Draw03NetworkData_g_cs
{
    static Numeric_Types_Draw03_Draw03NetworkData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03NetworkData_g_cs");
}
