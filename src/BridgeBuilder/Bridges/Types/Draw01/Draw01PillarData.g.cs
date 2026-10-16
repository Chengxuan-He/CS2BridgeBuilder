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
    internal enum Part { Top, Column, Footing }

    internal static StaticObjectPrefab Create(TowerFactory factory, string name,
        RenderPrefab top, RenderPrefab column, RenderPrefab footing, ContentPrefab content)
        => factory.CreateMovableObject(name, false, new[]
        {
            new ObjectMeshInfo { m_Mesh = top, m_Position = float3.zero,
                m_Rotation = quaternion.identity, m_RequireState = 0 },
            new ObjectMeshInfo { m_Mesh = column, m_Position = float3.zero,
                m_Rotation = quaternion.identity, m_RequireState = 0 },
            new ObjectMeshInfo { m_Mesh = footing, m_Position = float3.zero,
                m_Rotation = quaternion.identity, m_RequireState = 0 },
        }, Array.Empty<ObjectSubObjectInfo>(), target =>
        {
            var pillar = target.AddComponent<PillarObject>();
            pillar.m_Type = (PillarType)2;
            pillar.m_AnchorOffset = 0f;
            pillar.m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);
            target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        });

    // Only the highest-detail render owns native stack/base/LOD components.
    // Coarser renders carry their own content prerequisite without duplicated stack metadata.
    internal static void ConfigureRenderGraph(Part part, RenderPrefab highest,
        RenderPrefab lod1, RenderPrefab lod2, RenderPrefab nativeDefaultBase, ContentPrefab content)
    {
        var order = part switch
        {
            Part.Top => (StackOrder)2,
            Part.Column => (StackOrder)1,
            Part.Footing => (StackOrder)0,
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var stack = highest.AddComponent<StackProperties>();
        stack.m_Direction = (StackDirection)2;
        stack.m_Order = order;
        stack.m_StartOverlap = 0f; stack.m_EndOverlap = 0f;
        stack.m_ForbidScaling = false;
        var lod = highest.AddComponent<LodProperties>();
        lod.m_Bias = 0f; lod.m_ShadowBias = 0f;
        lod.m_LodMeshes = new[] { lod1, lod2 };
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
