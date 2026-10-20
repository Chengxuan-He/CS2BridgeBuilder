using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw02PillarAssembly
{
    // Ordinary network supports are distinct from the mechanism's stacking supports.
    // Reconstruct native inputs privately; only factory-owned results escape this scope.
    internal static StaticObjectPrefab? Create(TowerFactory factory, string name,
        RenderPrefab nativeDefaultBase, ContentPrefab content, float halfWidthDelta)
    {
        using var sources = new Draw02PillarSources(nativeDefaultBase, content);
        var generated = new RenderPrefab[3];
        var labels = new[] { "Top", "Column", "Footing" };
        for (var part = 0; part < generated.Length; part++)
        {
            var root = sources.Roots[part];
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
                        throw new InvalidOperationException("Draw02 ordinary pillar requires its recorded single native surface slot.");
                    var mesh = meshes[0];
                    var plan = Draw02NetPillarGeometry.Create(part, lod,
                        mesh.vertices, mesh.GetIndices(0), halfWidthDelta);
                    var bounds = original.bounds;
                    bounds.min.x -= halfWidthDelta;
                    bounds.max.x += halfWidthDelta;
                    // Native Y/Z bounds control stacking: compressed vertices may slightly
                    // exceed them, but model/submesh bounds already account for those vertices.
                    // Expand only the changed lateral extent.
                    foreach (var position in plan.Positions)
                    {
                        bounds.min.x = Math.Min(bounds.min.x, position.x);
                        bounds.max.x = Math.Max(bounds.max.x, position.x);
                    }
                    levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
                }
                finally { PrivateGeometryReader.Release(meshes); }
            }
            var render = factory.CreateMovableRenderGraph($"{name} {labels[part]}", levels);
            if (render == null) return null;
            generated[part] = render;
        }
        return Draw02PillarSources.CreateObject(factory, name,
            generated[0], generated[1], generated[2], content);
    }
}
