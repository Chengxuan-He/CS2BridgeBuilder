using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift02NetPillarAssembly
{
    internal static StaticObjectPrefab? Create(TowerFactory factory, string name,
        Lift02PillarSources sources, ContentPrefab content, float halfWidthDelta)
    {
        var roots = new RenderPrefab[3];
        for (var part = 0; part < roots.Length; part++)
        {
            var root = CreatePart(factory, name + " Part " + part,
                sources.Roots[part], part, halfWidthDelta);
            if (root == null) return null;
            roots[part] = root;
        }
        return Lift02PillarSources.CreateObject(factory, name,
            roots[0], roots[1], roots[2], content);
    }

    private static RenderPrefab? CreatePart(TowerFactory factory, string name,
        RenderPrefab source, int part, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 2)
            throw new InvalidOperationException("Lift02 ordinary pillars require their three recorded native LODs.");
        var originals = new[] { source, lods[0], lods[1] };
        var levels = new MovableRoadAssets.Level[3];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            var original = originals[lod];
            var stem = part switch
            {
                0 => "LiftBridge02NetPillarTop",
                1 => "LiftBridge02NetPillar",
                2 => "LiftBridge02NetPillarBase",
                _ => throw new ArgumentOutOfRangeException(nameof(part))
            };
            var expected = stem +
                (lod == 0 ? "" : "_LOD" + lod) + " Mesh";
            if (original == null || original.asset != null || original.name != expected)
                throw new InvalidOperationException("Lift02 ordinary pillars require their reconstructed native render inputs.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException("Lift02 ordinary pillars require their recorded single native surface slot.");
                var plan = Lift02NetPillarGeometry.Create(part, lod,
                    meshes[0].vertices, meshes[0].GetIndices(0), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x = plan.Positions.Min(p => p.x);
                bounds.max.x = plan.Positions.Max(p => p.x);
                // Keep authored Y/Z bounds and native stacking semantics.
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }
}

