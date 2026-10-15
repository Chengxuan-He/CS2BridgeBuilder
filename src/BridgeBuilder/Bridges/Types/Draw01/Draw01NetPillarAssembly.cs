using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw01NetPillarAssembly
{
    // Native 1.6.2f1: one centred cylindrical column at all three LODs. Columns
    // translate with their authored centre; this centre is zero, so the displacement
    // is zero. Do not stretch its cross-section with the road or infer it at runtime.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name,
        RenderPrefab source)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 2)
            throw new InvalidOperationException("Draw01 ordinary column requires both native LODs.");
        var sources = new[] { source, lods[0], lods[1] };
        var names = new[] { "DrawBridge01NetPillar Mesh", "DrawBridge01NetPillar_LOD1 Mesh",
            "DrawBridge01NetPillar_LOD2 Mesh" };
        var vertexCounts = new[] { 160, 148, 210 };
        var triangleCounts = new[] { 128, 76, 100 };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        for (var lod = 0; lod < sources.Length; lod++)
            if (sources[lod] == null || !string.Equals(sources[lod].name, names[lod], StringComparison.Ordinal) ||
                BridgeAssetInfo.IsOtherBridgePrefab(sources[lod].asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 ordinary column LOD {lod} is not its permitted native source '{names[lod]}'.");

        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = 0; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                if (meshes.Length != 1 || meshes[0] == null || meshes[0].subMeshCount != 1 ||
                    meshes[0].vertexCount != vertexCounts[lod])
                    throw new InvalidOperationException($"Draw01 ordinary column LOD {lod} differs from its recorded mesh layout.");
                var positions = meshes[0].vertices;
                var indices = meshes[0].GetIndices(0);
                if (indices.Length != triangleCounts[lod] * 3)
                    throw new InvalidOperationException($"Draw01 ordinary column LOD {lod} differs from its recorded topology.");
                var plan = new MovableStructureModel.Plan(positions, (Vector3[])positions.Clone(),
                    new[] { indices }, new[] { new bool[triangleCounts[lod]] });
                levels[lod] = new MovableRoadAssets.Level(sources[lod], sources[lod].bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        // Allocate owned geometry/renders even for this zero-displacement column.
        // The shared writer retains raw attributes, native surfaces and stack metadata,
        // repoints both LOD references and rolls back incomplete resource creation.
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
