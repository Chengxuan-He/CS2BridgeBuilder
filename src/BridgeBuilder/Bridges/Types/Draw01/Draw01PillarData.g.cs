// Generated from native DrawBridge01NetPillar, game 1.6.2f1.
// Audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Supplied render graphs must already be private, widened and complete at every LOD.
using System;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static class Draw01PillarData
{
    internal enum Part
    {
        Top,
        Column,
        Footing
    }

    internal static StaticObjectPrefab Create(TowerFactory factory, string name, RenderPrefab top, RenderPrefab column, RenderPrefab footing, ContentPrefab content) => factory.CreateMovableObject(name, false, new[] { new ObjectMeshInfo { m_Mesh = top, m_Position = float3.zero, m_Rotation = quaternion.identity, m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[0]) }, new ObjectMeshInfo { m_Mesh = column, m_Position = float3.zero, m_Rotation = quaternion.identity, m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[1]) }, new ObjectMeshInfo { m_Mesh = footing, m_Position = float3.zero, m_Rotation = quaternion.identity, m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[2]) }, }, Array.Empty<ObjectSubObjectInfo>(), target =>
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[3];
        pillar.m_AnchorOffset = Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[1], Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[2]);
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
    });
    // Only the highest-detail render owns native stack/base/LOD components.
    // Coarser renders carry their own content prerequisite without duplicated stack metadata.
    internal static void ConfigureRenderGraph(Part part, RenderPrefab highest, RenderPrefab lod1, RenderPrefab lod2, RenderPrefab nativeDefaultBase, ContentPrefab content)
    {
        var order = part switch
        {
            Part.Top => (StackOrder)Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[4],
            Part.Column => (StackOrder)Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[5],
            Part.Footing => (StackOrder)Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[6],
            _ => throw new ArgumentOutOfRangeException(nameof(part))};
        var stack = highest.AddComponent<StackProperties>();
        stack.m_Direction = (StackDirection)Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[7];
        stack.m_Order = order;
        stack.m_StartOverlap = Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[3];
        stack.m_EndOverlap = Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[4];
        stack.m_ForbidScaling = false;
        var lod = highest.AddComponent<LodProperties>();
        lod.m_Bias = Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[5];
        lod.m_ShadowBias = Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[6];
        lod.m_LodMeshes = new[]
        {
            lod1,
            lod2
        };
        if (part == Part.Footing)
        {
            var foundation = highest.AddComponent<BaseProperties>();
            foundation.m_BaseType = nativeDefaultBase;
            foundation.m_UseMinBounds = true;
        }

        highest.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        lod1.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        lod2.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01PillarData_g_cs
internal static class Numeric_Types_Draw01_Draw01PillarData_g_cs
{
    static Numeric_Types_Draw01_Draw01PillarData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01PillarData_g_cs");
}
