using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01ApproachAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        ContentPrefab content, float halfWidthDelta)
    {
        using var sources = new Lift01ApproachSources(content);
        var root = (NetPiecePrefab)sources.Roots[0];
        var children = root.GetComponent<LodProperties>().m_LodMeshes;
        var originals = new[] { (RenderPrefab)root, children[0], children[1] };
        var levels = new MovableRoadAssets.Level[originals.Length];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = originals[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException("Lift01 approach requires its recorded single native surface slot.");
                var plan = Lift01ApproachStructure.Create(lod,
                    meshes[0].vertices, meshes[0].GetIndices(0), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x = plan.Positions.Min(position => position.x);
                bounds.max.x = plan.Positions.Max(position => position.x);
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        var generated = factory.CreateMovableRenderGraph(target.name + " Fixed Approach", levels)
            as NetPiecePrefab ?? throw new InvalidOperationException("Lift01 approach render creation failed.");
        generated.m_Width = root.m_Width + 2f * halfWidthDelta;
        Lift01ApproachLayout.Apply(target, factory, new[] { generated }, content);
    }
}
