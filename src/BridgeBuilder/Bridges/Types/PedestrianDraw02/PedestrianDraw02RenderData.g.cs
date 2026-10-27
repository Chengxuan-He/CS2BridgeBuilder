// Native mechanism render configuration, game 1.6.2f1.
// Audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Geometry and procedural skeletons are supplied separately; no shared donor is mutated.
using System;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw02RenderData
{
    internal enum Part
    {
        Mechanism,
        Column,
        Footing
    }

    // Apply once to fresh private renders. Every level is supplied in native LOD order.
    internal static void Configure(Part part, RenderPrefab[] levels, RenderPrefab nativeDefaultBase, ContentPrefab content)
    {
        switch (part)
        {
            case Part.Mechanism:
            {
                if (levels.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[0])
                    throw new ArgumentException("PedestrianDraw02 Mechanism requires 2 prepared render levels.");
                var c0_LodProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[1]].AddComponent<LodProperties>();
                c0_LodProperties.m_Bias = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[0];
                c0_LodProperties.m_ShadowBias = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[1];
                c0_LodProperties.m_LodMeshes = new[]
                {
                    levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[2]]
                };
                var c0_StackProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[3]].AddComponent<StackProperties>();
                c0_StackProperties.m_Direction = (StackDirection)Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[4];
                c0_StackProperties.m_Order = (StackOrder)Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[5];
                c0_StackProperties.m_StartOverlap = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[2];
                c0_StackProperties.m_EndOverlap = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[3];
                c0_StackProperties.m_ForbidScaling = true;
                var c0_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[6]].AddComponent<ContentPrerequisite>();
                c0_ContentPrerequisite.m_ContentPrerequisite = content;
                var c1_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[7]].AddComponent<ContentPrerequisite>();
                c1_ContentPrerequisite.m_ContentPrerequisite = content;
                break;
            }

            case Part.Column:
            {
                if (levels.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[8])
                    throw new ArgumentException("PedestrianDraw02 Column requires 3 prepared render levels.");
                var c0_LodProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[9]].AddComponent<LodProperties>();
                c0_LodProperties.m_Bias = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[4];
                c0_LodProperties.m_ShadowBias = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[5];
                c0_LodProperties.m_LodMeshes = new[]
                {
                    levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[10]],
                    levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[11]]
                };
                var c0_StackProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[12]].AddComponent<StackProperties>();
                c0_StackProperties.m_Direction = (StackDirection)Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[13];
                c0_StackProperties.m_Order = (StackOrder)Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[14];
                c0_StackProperties.m_StartOverlap = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[6];
                c0_StackProperties.m_EndOverlap = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[7];
                c0_StackProperties.m_ForbidScaling = false;
                var c0_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[15]].AddComponent<ContentPrerequisite>();
                c0_ContentPrerequisite.m_ContentPrerequisite = content;
                var c1_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[16]].AddComponent<ContentPrerequisite>();
                c1_ContentPrerequisite.m_ContentPrerequisite = content;
                var c2_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[17]].AddComponent<ContentPrerequisite>();
                c2_ContentPrerequisite.m_ContentPrerequisite = content;
                break;
            }

            case Part.Footing:
            {
                if (levels.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[18])
                    throw new ArgumentException("PedestrianDraw02 Footing requires 2 prepared render levels.");
                var c0_LodProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[19]].AddComponent<LodProperties>();
                c0_LodProperties.m_Bias = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[8];
                c0_LodProperties.m_ShadowBias = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[9];
                c0_LodProperties.m_LodMeshes = new[]
                {
                    levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[20]]
                };
                var c0_StackProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[21]].AddComponent<StackProperties>();
                c0_StackProperties.m_Direction = (StackDirection)Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[22];
                c0_StackProperties.m_Order = (StackOrder)Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[23];
                c0_StackProperties.m_StartOverlap = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[10];
                c0_StackProperties.m_EndOverlap = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.F32[11];
                c0_StackProperties.m_ForbidScaling = true;
                var c0_BaseProperties = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[24]].AddComponent<BaseProperties>();
                c0_BaseProperties.m_BaseType = nativeDefaultBase;
                c0_BaseProperties.m_UseMinBounds = true;
                var c0_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[25]].AddComponent<ContentPrerequisite>();
                c0_ContentPrerequisite.m_ContentPrerequisite = content;
                var c1_ContentPrerequisite = levels[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs.Values.I32[26]].AddComponent<ContentPrerequisite>();
                c1_ContentPrerequisite.m_ContentPrerequisite = content;
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(part));
        }
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RenderData_g_cs");
}
