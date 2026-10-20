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
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        MovableRoadAssets.Level[] roadLevels, float halfWidthDelta)
    {
        if (roadLevels.Length != 2)
            throw new ArgumentException("PedestrianDraw02 mechanism requires its two native LODs.");
        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { 2, 2 },
            (lod, mesh, source, indices) => Structure(lod, mesh, source, indices, halfWidthDelta));
    }

    private static MovableStructureModel.Plan Structure(int lod, int mesh,
        Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        if (mesh == 0)
        {
            PedestrianDraw02RigidParts.Apply(lod, source, positions, delta);
            PedestrianDraw02TransverseParts.Apply(lod, source, positions, delta);
            PedestrianDraw02Bases.Apply(lod, source, positions, delta);
            var structure = PedestrianDraw02JoinedSupports.Complete(lod, source, positions, indices, delta);
            return PedestrianDraw02RoadReplacement.WithTopRemoved(lod, structure);
        }
        // Mesh 1 is the six authored control-room glass quads. Native LOD1
        // reorders them: its right-room vertices are 3..14 instead of 12..23.
        if (mesh != 1 || (lod != 0 && lod != 1) || source.Length != 24 || indices.Length != 36)
            throw new InvalidOperationException("PedestrianDraw02 glass requires its recorded six-quad mesh.");
        var rightStart = lod == 0 ? 12 : 3;
        var rightEnd = lod == 0 ? 23 : 14;
        for (var i = 0; i < positions.Length; i++)
            positions[i].x += i >= rightStart && i <= rightEnd ? delta : -delta;
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[12] });
    }

    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name,
        RenderPrefab source, float halfWidthDelta)
    {
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        if (source == null || !string.Equals(source.name, "PedestrianDrawBridge02Pillar Mesh", StringComparison.Ordinal) ||
            BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner, imported))
            throw new InvalidOperationException("PedestrianDraw02 column requires its permitted native source.");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 2)
            throw new InvalidOperationException("PedestrianDraw02 column requires both native LOD renders.");
        var sources = new[] { source, lods[0], lods[1] };
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            var original = sources[lod];
            var expected = "PedestrianDrawBridge02Pillar" + (lod == 0 ? "" : "_LOD" + lod) + " Mesh";
            if (original == null || !string.Equals(original.name, expected, StringComparison.Ordinal) ||
                BridgeAssetInfo.IsOtherBridgePrefab(original.asset?.path, owner, imported))
                throw new InvalidOperationException("PedestrianDraw02 column LOD is not its permitted native source.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0] == null || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException("PedestrianDraw02 column requires one native mesh and surface slot per LOD.");
                var mesh = meshes[0];
                var plan = lod == 2
                    ? PedestrianDraw02FarColumn.CreatePlan(mesh.vertices, mesh.GetIndices(0), halfWidthDelta)
                    : PedestrianDraw02ColumnWideningData.CreateDetailedPlan(lod, mesh.vertices,
                        mesh.GetIndices(0), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }

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
