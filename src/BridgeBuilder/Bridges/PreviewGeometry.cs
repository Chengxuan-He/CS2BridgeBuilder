using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
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
    private readonly Dictionary<RenderPrefab, SurfaceAsset[]> _surfaces = new();
    private readonly List<Mesh> _owned = new();
    private readonly HashSet<SurfaceAsset> _surfaceLeases = new();
    private bool _disposed;
    private ILocalAssetDatabase? _nativeAssets;

    // Converted preview materials need native SurfaceAsset identities and may carry VT headers.
    // Their database is temporary and private to this preview, never AssetDatabase.user.
    internal ILocalAssetDatabase NativeAssets => !_disposed
        ? _nativeAssets ??= AssetDatabase.GetTransient()
        : throw new ObjectDisposedException(nameof(PreviewGeometry));

    // Serialized surface references resolve through the global database, whereas our
    // generated textures live in a private transient database. Keep their direct
    // property references alive until the preview render resources have released them.
    internal void RetainSurfaceProperties(SurfaceAsset surface)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PreviewGeometry));
        if (!_surfaceLeases.Add(surface)) return;
        surface.LoadProperties(useVT: true);
    }

    internal bool Capture(RenderPrefab prefab, IReadOnlyList<ModelImporter.Model> models,
        IReadOnlyList<SurfaceAsset>? surfaces = null)
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
        // RenderPrefab stores only CIDs; private transient surfaces cannot be
        // resolved through its global asset lookup. Keep the original references.
        if (surfaces != null)
        {
            var captured = new SurfaceAsset[surfaces.Count];
            for (var index = 0; index < captured.Length; index++) captured[index] = surfaces[index];
            _surfaces.Add(prefab, captured);
        }
        return true;
    }

    internal bool TryGetMeshes(RenderPrefab prefab, out Mesh[] meshes)
        => _meshes.TryGetValue(prefab, out meshes!);

    internal bool TryGetSurfaces(RenderPrefab prefab, out SurfaceAsset[] surfaces)
        => _surfaces.TryGetValue(prefab, out surfaces!);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var mesh in _owned)
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
        _owned.Clear();
        _meshes.Clear();
        _surfaces.Clear();
        foreach (var surface in _surfaceLeases) surface.UnloadProperties();
        _surfaceLeases.Clear();
        if (_nativeAssets != null)
        {
            _nativeAssets.MarkForDeletion();
            _nativeAssets.Dispose();
            _nativeAssets = null;
        }
    }
}
