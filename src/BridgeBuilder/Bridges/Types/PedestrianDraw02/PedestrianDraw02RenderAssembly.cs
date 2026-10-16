using System;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw02RenderAssembly
{
    // The native stacked footing is a separate render graph from the column and mechanism.
    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name,
        RenderPrefab source, float halfWidthDelta)
    {
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        if (source == null || !string.Equals(source.name, "PedestrianDrawBridge02PillarBase Mesh", StringComparison.Ordinal) ||
            BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner, imported))
            throw new InvalidOperationException("PedestrianDraw02 footing requires its permitted native source.");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 1)
            throw new InvalidOperationException("PedestrianDraw02 footing requires its one native LOD render.");
        var sources = new[] { source, lods[0] };
        if (sources[1] == null || !string.Equals(sources[1].name,
                "PedestrianDrawBridge02PillarBase_LOD1 Mesh", StringComparison.Ordinal) ||
            BridgeAssetInfo.IsOtherBridgePrefab(sources[1].asset?.path, owner, imported))
            throw new InvalidOperationException("PedestrianDraw02 footing LOD1 is not its permitted native source.");

        var levels = new MovableRoadAssets.Level[2];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0] == null || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException($"PedestrianDraw02 footing LOD {lod} requires one native mesh and surface slot.");
                var mesh = meshes[0];
                var plan = PedestrianDraw02FootingWideningData.CreatePlan(lod, mesh.vertices,
                    halfWidthDelta, new[] { mesh.GetIndices(0) });
                // Preserve native vertical stacking reach and longitudinal bounds.
                var bounds = original.bounds;
                var widened = new Bounds3(
                    new float3(bounds.min.x - halfWidthDelta, bounds.min.y, bounds.min.z),
                    new float3(bounds.max.x + halfWidthDelta, bounds.max.y, bounds.max.z));
                levels[lod] = new MovableRoadAssets.Level(original, widened, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
