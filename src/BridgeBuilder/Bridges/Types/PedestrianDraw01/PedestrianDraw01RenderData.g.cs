// Native mechanism render configuration, game 1.6.2f1.
// Audit SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Geometry and procedural skeletons are supplied separately; no shared donor is mutated.
using System;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw01RenderData
{
    internal enum Part { Mechanism, Column, Footing }

    // Apply once to fresh private renders. Every level is supplied in native LOD order.
    internal static void Configure(Part part, RenderPrefab[] levels,
        RenderPrefab nativeDefaultBase, ContentPrefab content)
    {
        switch (part)
        {
            case Part.Mechanism:
            {
                if (levels.Length != 3)
                    throw new ArgumentException("PedestrianDraw01 Mechanism requires 3 prepared render levels.");
                var c0_LodProperties = levels[0].AddComponent<LodProperties>();
                c0_LodProperties.m_Bias = 0f;
                c0_LodProperties.m_ShadowBias = 0f;
                c0_LodProperties.m_LodMeshes = new[] { levels[1], levels[2] };
                var c0_StackProperties = levels[0].AddComponent<StackProperties>();
                c0_StackProperties.m_Direction = (StackDirection)2;
                c0_StackProperties.m_Order = (StackOrder)2;
                c0_StackProperties.m_StartOverlap = 0f;
                c0_StackProperties.m_EndOverlap = 0f;
                c0_StackProperties.m_ForbidScaling = true;
                var c0_ContentPrerequisite = levels[0].AddComponent<ContentPrerequisite>();
                c0_ContentPrerequisite.m_ContentPrerequisite = content;
                var c1_ContentPrerequisite = levels[1].AddComponent<ContentPrerequisite>();
                c1_ContentPrerequisite.m_ContentPrerequisite = content;
                var c2_ContentPrerequisite = levels[2].AddComponent<ContentPrerequisite>();
                c2_ContentPrerequisite.m_ContentPrerequisite = content;
                break;
            }
            case Part.Column:
            {
                if (levels.Length != 3)
                    throw new ArgumentException("PedestrianDraw01 Column requires 3 prepared render levels.");
                var c0_LodProperties = levels[0].AddComponent<LodProperties>();
                c0_LodProperties.m_Bias = 0f;
                c0_LodProperties.m_ShadowBias = 0f;
                c0_LodProperties.m_LodMeshes = new[] { levels[1], levels[2] };
                var c0_StackProperties = levels[0].AddComponent<StackProperties>();
                c0_StackProperties.m_Direction = (StackDirection)2;
                c0_StackProperties.m_Order = (StackOrder)1;
                c0_StackProperties.m_StartOverlap = 0f;
                c0_StackProperties.m_EndOverlap = 0f;
                c0_StackProperties.m_ForbidScaling = false;
                var c0_ContentPrerequisite = levels[0].AddComponent<ContentPrerequisite>();
                c0_ContentPrerequisite.m_ContentPrerequisite = content;
                var c1_ContentPrerequisite = levels[1].AddComponent<ContentPrerequisite>();
                c1_ContentPrerequisite.m_ContentPrerequisite = content;
                var c2_ContentPrerequisite = levels[2].AddComponent<ContentPrerequisite>();
                c2_ContentPrerequisite.m_ContentPrerequisite = content;
                break;
            }
            case Part.Footing:
            {
                if (levels.Length != 3)
                    throw new ArgumentException("PedestrianDraw01 Footing requires 3 prepared render levels.");
                var c0_LodProperties = levels[0].AddComponent<LodProperties>();
                c0_LodProperties.m_Bias = 0f;
                c0_LodProperties.m_ShadowBias = 0f;
                c0_LodProperties.m_LodMeshes = new[] { levels[1], levels[2] };
                var c0_StackProperties = levels[0].AddComponent<StackProperties>();
                c0_StackProperties.m_Direction = (StackDirection)2;
                c0_StackProperties.m_Order = (StackOrder)0;
                c0_StackProperties.m_StartOverlap = 0f;
                c0_StackProperties.m_EndOverlap = 1.6000000238418579f;
                c0_StackProperties.m_ForbidScaling = true;
                var c0_BaseProperties = levels[0].AddComponent<BaseProperties>();
                c0_BaseProperties.m_BaseType = nativeDefaultBase;
                c0_BaseProperties.m_UseMinBounds = true;
                var c0_ContentPrerequisite = levels[0].AddComponent<ContentPrerequisite>();
                c0_ContentPrerequisite.m_ContentPrerequisite = content;
                var c1_ContentPrerequisite = levels[1].AddComponent<ContentPrerequisite>();
                c1_ContentPrerequisite.m_ContentPrerequisite = content;
                var c2_ContentPrerequisite = levels[2].AddComponent<ContentPrerequisite>();
                c2_ContentPrerequisite.m_ContentPrerequisite = content;
                break;
            }
            default: throw new ArgumentOutOfRangeException(nameof(part));
        }
    }
}
