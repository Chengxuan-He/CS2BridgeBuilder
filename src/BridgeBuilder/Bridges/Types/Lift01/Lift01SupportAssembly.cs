using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01SupportAssembly
{
    // The two native pier columns span the centreline at Z +/-80.31 m.
    // They have no lateral placement offset: retain their authored shape rather
    // than treating these columns as the widened road-adjacent base/platform.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string name, RenderPrefab source)
        => Create(factory, name, source, false, 0f);

    internal static RenderPrefab? CreateFooting(TowerFactory factory, string name,
        RenderPrefab source, float halfWidthDelta)
        => Create(factory, name, source, true, halfWidthDelta);

    private static RenderPrefab? Create(TowerFactory factory, string name, RenderPrefab source,
        bool footing, float delta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 2)
            throw new InvalidOperationException("Lift01 support requires its three recorded native LODs.");
        var sources = new[] { source, lods[0], lods[1] };
        var vertexCounts = footing ? new[] { 10836, 11530, 717 } : new[] { 960, 1374, 224 };
        var indexCounts = footing ? new[] { 26880, 16128, 1500 } : new[] { 3888, 2328, 300 };
        var levels = new MovableRoadAssets.Level[3];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            var original = sources[lod];
            var expected = "LiftBridge01Pillar" + (footing ? "Base" : "") +
                (lod == 0 ? "" : "_LOD" + lod) + " Mesh";
            // Inputs are reconstructed privately by Lift01SourceRenders, whose
            // recorded CIDs resolve only built-in resources. Never load a saved
            // bridge or require the original prototype Prefab here.
            if (original == null || original.asset != null ||
                !string.Equals(original.name, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("Lift01 support requires its reconstructed native render inputs.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0] == null || meshes[0].subMeshCount != 1 ||
                    meshes[0].vertexCount != vertexCounts[lod])
                    throw new InvalidOperationException("Lift01 support source vertex layout changed.");
                var mesh = meshes[0];
                var indices = mesh.GetIndices(0);
                if (indices.Length != indexCounts[lod])
                    throw new InvalidOperationException("Lift01 support source index layout changed.");
                var positions = mesh.vertices;
                var plan = footing
                    ? lod == 2 ? Lift01FarFooting.Create(positions, indices, delta)
                        : Lift01Footing.CreateDetailed(lod, positions, indices, delta)
                    : new MovableStructureModel.Plan(positions, positions, new[] { indices },
                        new[] { new bool[indices.Length / 3] });
                var bounds = original.bounds;
                if (footing)
                {
                    bounds.min.x -= delta;
                    bounds.max.x += delta;
                }
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
