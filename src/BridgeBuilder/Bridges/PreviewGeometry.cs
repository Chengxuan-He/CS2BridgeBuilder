using Colossal.AssetPipeline.Importers;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Owns derived preview meshes without creating GeometryAssets. Source meshes and
/// materials remain owned by the game; only meshes produced by BuildModel belong here.
/// Dispose this after the preview renderers have been destroyed.
/// </summary>
internal sealed class PreviewGeometry : IDisposable
{
    private readonly Dictionary<RenderPrefab, Mesh[]> _meshes = new();
    private readonly List<Mesh> _owned = new();
    private bool _disposed;

    internal bool Capture(RenderPrefab prefab, IReadOnlyList<ModelImporter.Model> models)
    {
        if (_disposed || _meshes.ContainsKey(prefab)) return false;
        var meshes = new Mesh[models.Count];
        for (var index = 0; index < models.Count; index++)
        {
            var mesh = models[index].ToUnityMesh();
            if (mesh == null) return false;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            _owned.Add(mesh);
            meshes[index] = mesh;
        }
        _meshes.Add(prefab, meshes);
        return true;
    }

    internal bool TryGetMeshes(RenderPrefab prefab, out Mesh[] meshes)
        => _meshes.TryGetValue(prefab, out meshes!);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var mesh in _owned)
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
        _owned.Clear();
        _meshes.Clear();
    }
}
