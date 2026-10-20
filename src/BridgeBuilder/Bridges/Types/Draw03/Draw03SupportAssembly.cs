using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw03SupportAssembly
{
    internal static RenderPrefab? Create(TowerFactory factory, string name, RenderPrefab source,
        bool footing, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 2)
            throw new InvalidOperationException("Draw03 supports require their three recorded native LODs.");
        var originals = new[] { source, lods[0], lods[1] };
        var levels = new MovableRoadAssets.Level[3];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            var original = originals[lod];
            var expected = "DrawBridge03Pillar" + (footing ? "Base" : "") +
                (lod == 0 ? "" : "_LOD" + lod) + " Mesh";
            if (original == null || original.asset != null || original.name != expected)
                throw new InvalidOperationException("Draw03 supports require their reconstructed native render inputs.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException("Draw03 supports require their recorded single native surface slot.");
                var plan = lod == 2
                    ? Draw03FarSupports.Create(footing, meshes[0].vertices, meshes[0].GetIndices(0), halfWidthDelta)
                    : Draw03DetailedSupports.Create(footing, lod, meshes[0].vertices, meshes[0].GetIndices(0), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
                // Keep authored Y/Z bounds and native stacking semantics.
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
