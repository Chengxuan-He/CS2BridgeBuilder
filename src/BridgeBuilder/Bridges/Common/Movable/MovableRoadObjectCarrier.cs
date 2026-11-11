using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Native functional ownership survives visual merging. Bounds remain authored
// metadata; no mesh, material or visible LOD is retained by this carrier.
internal static class MovableRoadObjectCarrier
{
    internal static StaticObjectPrefab Create(string owner, StaticObjectPrefab source, List<PrefabBase> created,
        bool preserveSelection = false)
    {
        var result = (StaticObjectPrefab)source.Clone(owner + " Utility Carrier " + created.Count);
        created.Add(result);
        foreach (var component in result.components) component.prefab = result;
        // Unity JSON also omits the float3 utility anchor in the native-type
        // clone probe. It is functional connection data, not a visual offset.
        var utility = source.GetComponent<UtilityObject>();
        var copiedUtility = result.GetComponent<UtilityObject>();
        if (utility != null && copiedUtility != null)
        { copiedUtility.m_UtilityType = utility.m_UtilityType; copiedUtility.m_UtilityPosition = utility.m_UtilityPosition; }
        if (source.TryGet<StandingObject>(out var standing) && result.TryGet<StandingObject>(out var copiedStanding))
        {
            copiedStanding.m_LegSize = standing.m_LegSize;
            copiedStanding.m_LegGap = standing.m_LegGap;
            copiedStanding.m_CircularLeg = standing.m_CircularLeg;
        }
        if (source.TryGet<EffectSource>(out var effects) && result.TryGet<EffectSource>(out var copiedEffects) && effects.m_Effects != null)
        {
            copiedEffects.m_Effects = new List<EffectSource.EffectSettings>();
            foreach (var effect in effects.m_Effects)
                copiedEffects.m_Effects.Add(new EffectSource.EffectSettings
                {
                    m_Effect = effect.m_Effect, m_PositionOffset = effect.m_PositionOffset,
                    m_Rotation = effect.m_Rotation, m_Scale = effect.m_Scale,
                    m_Intensity = effect.m_Intensity, m_ParentMesh = effect.m_ParentMesh,
                    m_AnimationIndex = effect.m_AnimationIndex
                });
        }
        result.m_Meshes = new ObjectMeshInfo[source.m_Meshes?.Length ?? 0];
        for (int i = 0; i < result.m_Meshes.Length; i++)
        {
            var info = source.m_Meshes[i];
            var original = (RenderPrefab)info.m_Mesh;
            var render = (RenderPrefab)original.Clone(owner + " Utility Bounds " + created.Count);
            created.Add(render);
            foreach (var component in render.components) component.prefab = render;
            // PrefabBase.Clone uses Unity JSON. Bounds3 is not copied by that
            // route in the native-type probe; preserve this metadata explicitly.
            render.bounds = original.bounds;
            render.surfaceArea = original.surfaceArea;
            // Source eligibility excludes BaseProperties: its extra generated
            // foundation cannot be silently lost by this representation.
            if (render.TryGet<LodProperties>(out var lods))
            { render.Remove<LodProperties>(); UnityEngine.Object.DestroyImmediate(lods); }
            render.geometryAsset = null!;
            render.surfaceAssets = Array.Empty<SurfaceAsset>();
            render.meshCount = render.vertexCount = render.indexCount = 0;
            render.isDirty = true;
            result.m_Meshes[i] = new ObjectMeshInfo { m_Mesh = render,
                m_Position = info.m_Position, m_Rotation = info.m_Rotation, m_RequireState = info.m_RequireState };
        }
        // Already selected at the road placement boundary, not a new alternative.
        if (result.TryGet<SpawnableObject>(out var membership))
        {
            if (preserveSelection)
            {
                // Keep native random-group correlation, but never register a private
                // metadata-only alternative in the source placeholder's candidates.
                var originalMembership = source.GetComponent<SpawnableObject>();
                membership.m_Placeholders = Array.Empty<ObjectPrefab>();
                membership.m_Probability = originalMembership.m_Probability;
                membership.m_RandomizationGroup = originalMembership.m_RandomizationGroup;
            }
            else { result.Remove<SpawnableObject>(); UnityEngine.Object.DestroyImmediate(membership); }
        }
        result.isDirty = true;
        return result;
    }
}
