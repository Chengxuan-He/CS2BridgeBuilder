using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Temporary input Prefabs reconstructed from measured native resource descriptors.
// Resource assets remain shared and are never destroyed with this owner.
internal abstract class MovableSourceRenders : IDisposable
{
    internal RenderPrefab[] Roots { get; private protected set; } = Array.Empty<RenderPrefab>();
    private readonly List<RenderPrefab> _owned = new();
    protected RenderPrefab Create(string name, string geometryId, string[] surfaceIds,
        Bounds3 bounds, int vertices, int indices, int meshes, float area)
        => Create<RenderPrefab>(name, geometryId, surfaceIds, bounds, vertices, indices, meshes, area);

    protected T Create<T>(string name, string geometryId, string[] surfaceIds,
        Bounds3 bounds, int vertices, int indices, int meshes, float area) where T : RenderPrefab
    {
        // These recorded inputs belong to the built-in DLC. Do not substitute an
        // ImportedData dependency carrying the same CID from another bridge.
        if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(geometryId), out GeometryAsset geometry)
            || !geometry.isBuiltin)
            throw new InvalidOperationException("Recorded native geometry unavailable: " + geometryId);
        var surfaces = new SurfaceAsset[surfaceIds.Length];
        for (var i = 0; i < surfaces.Length; i++)
        {
            if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(surfaceIds[i]), out SurfaceAsset surface)
                || !surface.isBuiltin)
                throw new InvalidOperationException("Recorded native surface unavailable: " + surfaceIds[i]);
            surfaces[i] = surface;
        }
        var render = ScriptableObject.CreateInstance<T>();
        _owned.Add(render);
        render.name = name;
        render.hideFlags = HideFlags.HideAndDontSave;
        render.geometryAsset = geometry;
        render.surfaceAssets = surfaces;
        render.bounds = bounds;
        render.vertexCount = vertices;
        render.indexCount = indices;
        render.meshCount = meshes;
        render.surfaceArea = area;
        render.isImpostor = false;
        render.manualVTRequired = false;
        return render;
    }

    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            foreach (var component in _owned[i].components)
                if (component != null) UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(_owned[i]);
        }
        _owned.Clear();
    }
}
