using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw03ApproachAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        ContentPrefab content, float halfWidthDelta)
    {
        using var sources = new Draw03ApproachSources(content);
        var pieces = new NetPiecePrefab[sources.Roots.Length];
        var layoutRemainder = 0f;
        for (var family = 0; family < pieces.Length; family++)
        {
            var root = (NetPiecePrefab)sources.Roots[family];
            var originals = new[] { (RenderPrefab)root }.Concat(
                root.GetComponent<LodProperties>()?.m_LodMeshes ?? Array.Empty<RenderPrefab>()).ToArray();
            var levels = new MovableRoadAssets.Level[originals.Length];
            for (var lod = 0; lod < levels.Length; lod++)
            {
                Mesh[]? meshes = null;
                try
                {
                    var original = originals[lod];
                    meshes = PrivateGeometryReader.Read(original);
                    if (meshes.Length != 1 || meshes[0].subMeshCount != 1)
                        throw new InvalidOperationException("Draw03 approach requires its recorded single native surface slot.");
                    var plan = Draw03ApproachGeometry.Create(family, lod,
                        meshes[0].vertices, meshes[0].GetIndices(0), halfWidthDelta);
                    var bounds = original.bounds;
                    bounds.min.x = plan.Positions.Min(position => position.x);
                    bounds.max.x = plan.Positions.Max(position => position.x);
                    levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
                }
                finally { PrivateGeometryReader.Release(meshes); }
            }
            var generated = factory.CreateMovableRenderGraph(target.name + " Fixed Approach " + family, levels)
                as NetPiecePrefab ?? throw new InvalidOperationException("Draw03 approach render creation failed.");
            // Only the first inner strip on each side gains width. Native lateral layout translates all
            // remaining parts rigidly and keeps both centre seams at the origin.
            if (family == 0)
            {
                var signedWidth = root.m_Width + halfWidthDelta;
                generated.m_Width = Mathf.Max(0f, signedWidth);
                // Keep native layout sizes nonnegative without clamping mesh displacement.
                layoutRemainder = signedWidth - generated.m_Width;
            }
            pieces[family] = generated;
        }
        Draw03ApproachLayout.Apply(target, factory, pieces, layoutRemainder);
    }
}
