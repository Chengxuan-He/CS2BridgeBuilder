using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using System;
using System.Reflection;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>Reads authored geometry without borrowing or releasing the renderer's streaming data.</summary>
internal static class PrivateGeometryReader
{
    private delegate Mesh[] MeshCreator(string name, ref GeometryAsset.Data data);
    private static readonly MeshCreator? CreateMeshes = ResolveCreator();

    private static MeshCreator? ResolveCreator()
    {
        var method = typeof(GeometryAsset).GetMethod("CreateMeshes",
            BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(string), typeof(GeometryAsset.Data).MakeByRefType() }, null);
        return method == null ? null
            : Delegate.CreateDelegate(typeof(MeshCreator), method, false) as MeshCreator;
    }

    internal static Mesh[] Read(RenderPrefab prefab)
    {
        var asset = prefab.geometryAsset;
        if (asset == null || CreateMeshes == null) return Array.Empty<Mesh>();
        var data = default(GeometryAsset.Data);
        var loading = default(GeometryAsset.Loading);
        try
        {
            // GeometryAsset.ObtainMeshes and the asynchronous city renderer use the same m_Data
            // and m_Loading, but DIFFERENT reference counters. ReleaseMeshes disposes both when
            // its instance count reaches zero, without consulting the renderer's request count.
            // Even a balanced Obtain/Release pair can invalidate a live prototype's GPU source.
            // Read through the descriptor into private buffers, preserving the exact native layout.
            var descriptor = asset.database.GetAsyncReadDescriptor(asset.id);
            GeometryAsset.LoadSync(descriptor, GeometryAsset.Attribute.All, ref data, ref loading);
            var meshes = CreateMeshes("BridgeBuilder_Read_" + prefab.name, ref data);
            foreach (var mesh in meshes)
                if (mesh != null) mesh.hideFlags = HideFlags.HideAndDontSave;
            return meshes;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, $"Could not read independent geometry for '{prefab.name}'.");
            return Array.Empty<Mesh>();
        }
        finally
        {
            try { loading.Dispose(); }
            finally { data.Dispose(); }
        }
    }

    internal static void Release(Mesh[]? meshes)
    {
        if (meshes == null) return;
        foreach (var mesh in meshes)
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
    }
}
