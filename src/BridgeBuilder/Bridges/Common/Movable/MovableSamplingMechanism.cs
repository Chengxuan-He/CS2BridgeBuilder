using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Metadata-only input to the private native network calculation. Never submitted
// to TowerFactory, the asset writer, the city registry or the preview renderer.
internal sealed class MovableSamplingMechanism : IDisposable
{
    internal readonly StaticObjectPrefab Prefab;
    private readonly List<PrefabBase> _owned = new();

    internal MovableSamplingMechanism(string owner, IReadOnlyList<RenderPrefab> sources,
        ContentPrefab content, float halfWidthDelta, IReadOnlyList<string> names,
        Func<RenderPrefab[], ObjectMeshInfo[]> meshes, Action<StaticObjectPrefab, ContentPrefab> applyComponents)
    {
        if (sources.Count != names.Count || sources.Count == 0 || !float.IsFinite(halfWidthDelta) || content == null)
            throw new ArgumentException("Movable sampling requires matching native render roots, content and a finite width change.");
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var copies = new Dictionary<RenderPrefab, RenderPrefab>();
        try
        {
            var prepared = new RenderPrefab[sources.Count];
            for (var i = 0; i < prepared.Length; i++)
            {
                if (sources[i] == null || sources[i].name != names[i])
                    throw new InvalidOperationException("Movable sampling requires recorded render source: " + names[i]);
                prepared[i] = Copy(sources[i]);
            }
            Prefab = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            _owned.Add(Prefab);
            Prefab.name = owner + " Sampling Object";
            Prefab.hideFlags = HideFlags.HideAndDontSave;
            Prefab.m_Meshes = meshes(prepared);
            applyComponents(Prefab, content);
            Prefab.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
        }
        catch
        {
            Dispose();
            throw;
        }

        RenderPrefab Copy(RenderPrefab source)
        {
            if (source == null || BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner, imported))
                throw new InvalidOperationException("Movable sampling cannot use missing or foreign bridge render metadata.");
            if (copies.TryGetValue(source, out var existing)) return existing;
            var copy = ScriptableObject.CreateInstance<RenderPrefab>();
            _owned.Add(copy);
            copies.Add(source, copy);
            copy.name = owner + " Sampling " + source.name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            foreach (var component in source.components)
                if (component != null) copy.AddComponentFrom(component);
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
                for (var i = 0; i < children.Length; i++) children[i] = Copy(lods.m_LodMeshes[i]);
                lods.m_LodMeshes = children;
            }
            return copy;
        }
    }

    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            var prefab = _owned[i];
            foreach (var component in prefab.components)
                if (component != null) UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
        _owned.Clear();
    }
}
