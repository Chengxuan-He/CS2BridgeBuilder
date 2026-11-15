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
    internal static StaticObjectPrefab Create(TowerFactory factory, string name, RenderPrefab top, RenderPrefab column, RenderPrefab footing, ContentPrefab content) => factory.CreateMovableObject(name, false, new[] { new ObjectMeshInfo { m_Mesh = top, m_Position = float3.zero, m_Rotation = quaternion.identity, m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[0]) }, new ObjectMeshInfo { m_Mesh = column, m_Position = float3.zero, m_Rotation = quaternion.identity, m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[1]) }, new ObjectMeshInfo { m_Mesh = footing, m_Position = float3.zero, m_Rotation = quaternion.identity, m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[2]) }, }, Array.Empty<ObjectSubObjectInfo>(), target =>
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.I32[3];
        pillar.m_AnchorOffset = Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[1], Numeric_Types_Draw01_Draw01PillarData_g_cs.Values.F32[2]);
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
    });

}

// Embedded numerical arrays: Types_Draw01_Draw01PillarData_g_cs
internal static class Numeric_Types_Draw01_Draw01PillarData_g_cs
{
    static Numeric_Types_Draw01_Draw01PillarData_g_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01PillarData_g_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01PillarData_g_cs"));
}
