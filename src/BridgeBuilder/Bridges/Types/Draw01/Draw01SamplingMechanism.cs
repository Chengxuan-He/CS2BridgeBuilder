using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Metadata-only input to the private native network calculation. Never submitted
// to TowerFactory, the asset writer, the city registry or the preview renderer.
internal sealed class Draw01SamplingMechanism : IDisposable
{
    internal readonly StaticObjectPrefab Prefab;
    private readonly List<PrefabBase> _owned = new();
    internal Draw01SamplingMechanism(string owner, IReadOnlyList<RenderPrefab> sources, ContentPrefab content, float halfWidthDelta)
    {
        if (sources.Count != Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.I32[0] || !float.IsFinite(halfWidthDelta) || content == null)
            throw new ArgumentException("Draw01 sampling requires three native render roots, content and a finite width change.");
        var names = new[]
        {
            "DrawBridge01 Mesh",
            "DrawBridge01Pillar Mesh",
            "DrawBridge01PillarBase Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var copies = new Dictionary<RenderPrefab, RenderPrefab>();
        try
        {
            var prepared = new RenderPrefab[Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.I32[1]];
            for (var i = Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.I32[2]; i < prepared.Length; i++)
            {
                if (sources[i] == null || sources[i].name != names[i])
                    throw new InvalidOperationException("Draw01 sampling requires recorded render source: " + names[i]);
                prepared[i] = Copy(sources[i]);
            }

            Prefab = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            _owned.Add(Prefab);
            Prefab.name = owner + " Draw01 Sampling Object";
            Prefab.hideFlags = HideFlags.HideAndDontSave;
            Prefab.m_Meshes = Draw01MechanismData.Meshes(prepared);
            Draw01MechanismData.ApplyComponents(Prefab, content);
            Prefab.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.F32[0];
        }
        catch
        {
            Dispose();
            throw;
        }

        RenderPrefab Copy(RenderPrefab source)
        {
            if (source == null || BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner, imported))
                throw new InvalidOperationException("Draw01 sampling cannot use missing or foreign bridge render metadata.");
            if (copies.TryGetValue(source, out var existing))
                return existing;
            var copy = ScriptableObject.CreateInstance<RenderPrefab>();
            _owned.Add(copy);
            copies.Add(source, copy);
            copy.name = owner + " Sampling " + source.name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            foreach (var component in source.components)
                if (component != null)
                    copy.AddComponentFrom(component);
            // ObjectInitializeSystem consumes the private MeshData projection, not
            // geometry/material assets. Preserve native Y/Z stacking reach and all
            // render components while applying the requested transverse envelope.
            var bounds = source.bounds;
            bounds.min.x -= halfWidthDelta;
            bounds.max.x += halfWidthDelta;
            copy.bounds = bounds;
            copy.vertexCount = source.vertexCount;
            copy.indexCount = source.indexCount;
            copy.meshCount = source.meshCount;
            copy.surfaceArea = source.surfaceArea;
            copy.isImpostor = source.isImpostor;
            var lods = copy.GetComponent<LodProperties>();
            if (lods?.m_LodMeshes != null)
            {
                var children = new RenderPrefab[lods.m_LodMeshes.Length];
                for (var i = Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.I32[3]; i < children.Length; i++)
                    children[i] = Copy(lods.m_LodMeshes[i]);
                lods.m_LodMeshes = children;
            }

            return copy;
        }
    }

    public void Dispose()
    {
        for (var i = _owned.Count - Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.I32[4]; i >= Numeric_Types_Draw01_Draw01SamplingMechanism_cs.Values.I32[5]; i--)
        {
            var prefab = _owned[i];
            foreach (var component in prefab.components)
                if (component != null)
                    UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(prefab);
        }

        _owned.Clear();
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01SamplingMechanism_cs
internal static class Numeric_Types_Draw01_Draw01SamplingMechanism_cs
{
    static Numeric_Types_Draw01_Draw01SamplingMechanism_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01SamplingMechanism_cs");
}
