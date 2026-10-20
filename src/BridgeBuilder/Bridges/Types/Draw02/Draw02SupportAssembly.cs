using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw02SupportAssembly
{
    // Sources are privately reconstructed from the recorded Draw02 descriptors, not
    // borrowed prototype Prefabs. The factory owns the resulting persistent renders.
    internal static RenderPrefab? Create(TowerFactory factory, string name,
        RenderPrefab root, bool column, float halfWidthDelta)
    {
        var children = root.GetComponent<LodProperties>().m_LodMeshes;
        var originals = new[] { root, children[0], children[1] };
        var levels = new MovableRoadAssets.Level[originals.Length];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = originals[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException("Draw02 support requires its recorded single native surface slot.");
                var mesh = meshes[0];
                var plan = Draw02SupportWideningData.CreatePlan(column, lod,
                    mesh.vertices, mesh.GetIndices(0), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
                // Preserve native authored Y/Z stacking reach; do not replace it with
                // compressed vertex extents or a motion-swept bounding box.
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
