using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using Game.Prefabs.Effects;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Preserve native road lighting ownership without a stationary duplicate of each moving lamp.
internal static class MovableStreetLights
{
    internal static ObjectPrefab CreateProducer(string owner, ObjectPrefab source, List<PrefabBase> created)
    {
        CheckSource(owner, source);
        var result = (ObjectPrefab)Clone(owner, source, "Lighting Producer", created);
        // A private placeholder has no registered replacements. Native node/edge aggregation
        // still sees StreetLightData and sets IsLit/AlwaysLit, but emits no stationary object.
        if (result.TryGet<SpawnableObject>(out var spawnable))
        {
            result.Remove<SpawnableObject>();
            UnityEngine.Object.DestroyImmediate(spawnable);
        }
        if (!result.Has<PlaceholderObject>()) result.AddComponent<PlaceholderObject>();
        return result;
    }

    internal static void Bind(string owner, StaticObjectPrefab mechanism,
        ObjectSubObjectInfo[] placements, List<PrefabBase> created)
    {
        var visuals = new Dictionary<ObjectPrefab, ObjectPrefab>();
        var effects = new List<EffectSource.EffectSettings>();
        StreetLightObject? firstLight = null;
        foreach (var placement in placements)
        {
            var source = placement.m_Object;
            CheckSource(owner, source);
            if (!source.TryGet<StreetLightObject>(out var light)) continue;
            firstLight ??= light;
            if (source.Has<ObjectSubObjects>())
                throw new NotSupportedException("Moving streetlight contains nested object placements: " + source.name);
            if (!source.TryGet<EffectSource>(out var lighting) || lighting.m_Effects == null) continue;
            foreach (var effect in lighting.m_Effects)
            {
                if (effect?.m_Effect == null)
                    throw new InvalidOperationException("Moving streetlight has a missing effect: " + source.name);
                CheckSource(owner, effect.m_Effect);
                if (effect.m_ParentMesh != 0 || effect.m_AnimationIndex != -1)
                    throw new NotSupportedException("Moving streetlight requires its own effect parent or animation mapping: " + source.name);
                if (effect.m_Effect.TryGet<EffectColor>(out var color) && color.m_Source != EffectColorSource.Effect)
                    throw new NotSupportedException("Moving streetlight effect reads lamp mesh colors: " + source.name);
                if (effect.m_Effect.TryGet<RandomTransform>(out var random) &&
                    (math.any(random.m_MinPosition != 0f) || math.any(random.m_MaxPosition != 0f)))
                    throw new NotSupportedException("Moving streetlight effect has an owner-local random position: " + source.name);
                effects.Add(new EffectSource.EffectSettings
                {
                    m_Effect = effect.m_Effect,
                    m_PositionOffset = placement.m_Position + math.rotate(placement.m_Rotation, effect.m_PositionOffset),
                    m_Rotation = math.mul(placement.m_Rotation, effect.m_Rotation),
                    m_Scale = effect.m_Scale, m_Intensity = effect.m_Intensity,
                    m_ParentMesh = placement.m_ParentMesh, m_AnimationIndex = -1
                });
            }
            if (!visuals.TryGetValue(source, out var visual))
            {
                visual = (ObjectPrefab)Clone(owner, source, "Moving Lamp", created);
                visual.TryGet<EffectSource>(out var copiedLighting);
                visual.Remove<EffectSource>();
                UnityEngine.Object.DestroyImmediate(copiedLighting);
                // This concrete lamp was already selected. Registering its copied spawn
                // membership would insert an effect-free alternative into native placeholders.
                if (visual.TryGet<SpawnableObject>(out var membership))
                {
                    visual.Remove<SpawnableObject>();
                    UnityEngine.Object.DestroyImmediate(membership);
                }
                visuals.Add(source, visual);
            }
            placement.m_Object = visual;
        }
        if (firstLight == null) return;
        // Audited B&P mechanism roots have no EffectSource/StreetLightObject. Preserve any
        // unrelated root effects, but do not silently change an existing LightsOff condition.
        if (mechanism.TryGet<EffectSource>(out var rootEffects) && rootEffects.m_Effects != null)
            foreach (var effect in rootEffects.m_Effects)
                if (((effect.m_Effect.m_Conditions.m_RequiredFlags | effect.m_Effect.m_Conditions.m_ForbiddenFlags) &
                    EffectConditionFlags.LightsOff) != 0)
                    throw new NotSupportedException("Mechanism already has owner-sensitive lighting effects: " + mechanism.name);
        if (!mechanism.Has<StreetLightObject>())
            mechanism.AddComponent<StreetLightObject>().m_Layer = firstLight.m_Layer;
        if (effects.Count != 0)
        {
            rootEffects ??= mechanism.AddComponent<EffectSource>();
            var combined = rootEffects.m_Effects == null ? new List<EffectSource.EffectSettings>()
                : new List<EffectSource.EffectSettings>(rootEffects.m_Effects);
            combined.AddRange(effects);
            rootEffects.m_Effects = combined;
        }
        mechanism.isDirty = true;
    }

    private static PrefabBase Clone(string owner, PrefabBase source, string role, List<PrefabBase> created)
    {
        var result = source.Clone(owner + " " + role + " " + created.Count);
        created.Add(result);
        foreach (var component in result.components) component.prefab = result;
        result.isDirty = true;
        return result;
    }

    private static void CheckSource(string owner, PrefabBase source)
    {
        if (source == null || BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner,
            Path.Combine(Application.persistentDataPath, "ImportedData")))
            throw new InvalidOperationException("Moving lighting cannot read a missing or foreign bridge-owned source.");
    }
}
