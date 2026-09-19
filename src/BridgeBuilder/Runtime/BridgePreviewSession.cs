using BridgeBuilder.Bridges;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>
/// One disposable preview recipe/result, or a read-only view of an existing
/// bridge. Never enters WorldRegistration, the asset writer or the permanent
/// UUID registry. The renderer must be disposed before this owner so that no
/// draw can reference a released private mesh.
/// </summary>
internal sealed class BridgePreviewSession : IDisposable
{
    private readonly HashSet<PrefabBase> _owned = new();
    private bool _disposed;
    private ObjectGeometryPrefab[]? _objectCandidates;

    internal BridgePreviewSession() => Name = "tmp" + Guid.NewGuid().ToString("D");

    // Existing bridges are borrowed by identity, never cloned or regenerated.
    // Their prefab/geometry ownership remains with the game and asset database;
    // the empty ownership sets below can only release private render resources.
    internal BridgePreviewSession(NetGeometryPrefab existing)
    {
        Name = existing.name;
        Root = existing;
        IsBorrowed = true;
    }

    internal string Name { get; }
    internal bool IsBorrowed { get; }
    internal PreviewGeometry Geometry { get; } = new();
    internal NetGeometryPrefab? Root { get; private set; }
    internal BridgeStyleVariant? Variant { get; private set; }
    internal IEnumerable<PrefabBase> OwnedPrefabs => _owned;
    internal IEnumerable<ObjectGeometryPrefab> ObjectCandidates => _objectCandidates ??= _owned
        .Concat(PrefabCatalog.GetAll(World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>()))
        .OfType<ObjectGeometryPrefab>().Distinct().ToArray();

    internal void SetResult(NetGeometryPrefab root, BridgeStyleVariant variant)
    {
        if (IsBorrowed) return;
        Root = root;
        Variant = variant;
    }

    internal void Adopt(IEnumerable<PrefabCloneNode> nodes, IEnumerable<PrefabBase> geometryPrefabs)
    {
        if (IsBorrowed) return;
        foreach (var node in nodes)
            if (node.NeedsSave) Own(node.Target);
        foreach (var prefab in geometryPrefabs) Own(prefab);
    }

    private void Own(PrefabBase prefab)
    {
        if (prefab == null || !_owned.Add(prefab)) return;
        prefab.hideFlags = HideFlags.HideAndDontSave;
        if (!prefab.name.StartsWith(Name, StringComparison.Ordinal))
            prefab.name = Name + "_" + prefab.name;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Root = null;
        Variant = null;
        _objectCandidates = null;
        Geometry.Dispose();
        foreach (var prefab in _owned)
            if (prefab != null) UnityEngine.Object.DestroyImmediate(prefab);
        _owned.Clear();
    }
}
