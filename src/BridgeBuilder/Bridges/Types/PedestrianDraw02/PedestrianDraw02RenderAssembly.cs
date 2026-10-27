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
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, MovableRoadAssets.Level[] roadLevels, float halfWidthDelta)
    {
        if (roadLevels.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[0])
            throw new ArgumentException("PedestrianDraw02 mechanism requires its two native LODs.");
        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[1], Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[2] }, (lod, mesh, source, indices) => Structure(lod, mesh, source, indices, halfWidthDelta));
    }

    private static MovableStructureModel.Plan Structure(int lod, int mesh, Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        if (mesh == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[3])
        {
            PedestrianDraw02RigidParts.Apply(lod, source, positions, delta);
            PedestrianDraw02TransverseParts.Apply(lod, source, positions, delta);
            PedestrianDraw02Bases.Apply(lod, source, positions, delta);
            var structure = PedestrianDraw02JoinedSupports.Complete(lod, source, positions, indices, delta);
            return PedestrianDraw02RoadReplacement.WithTopRemoved(lod, structure);
        }

        // Mesh 1 is the six authored control-room glass quads. Native LOD1
        // reorders them: its right-room vertices are 3..14 instead of 12..23.
        if (mesh != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[4] || (lod != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[5] && lod != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[6]) || source.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[7] || indices.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[8])
            throw new InvalidOperationException("PedestrianDraw02 glass requires its recorded six-quad mesh.");
        var rightStart = lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[9] ? Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[10] : Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[11];
        var rightEnd = lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[12] ? Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[13] : Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[14];
        AffineGeometry.For(0, positions.Length, i => { positions[i].x = AffineGeometry.Translate(positions[i].x, i >= rightStart && i <= rightEnd ? delta : -delta); });
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[16]] });
    }

    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        if (source == null || !string.Equals(source.name, "PedestrianDrawBridge02Pillar Mesh", StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner, imported))
            throw new InvalidOperationException("PedestrianDraw02 column requires its permitted native source.");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[17])
            throw new InvalidOperationException("PedestrianDraw02 column requires both native LOD renders.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[18]],
            lods[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[19]]
        };
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[20]; lod < levels.Length; lod++)
        {
            var original = sources[lod];
            var expected = "PedestrianDrawBridge02Pillar" + (lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[21] ? "" : "_LOD" + lod) + " Mesh";
            if (original == null || !string.Equals(original.name, expected, StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(original.asset?.path, owner, imported))
                throw new InvalidOperationException("PedestrianDraw02 column LOD is not its permitted native source.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[22] || meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[23]] == null || meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[24]].subMeshCount != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[25])
                    throw new InvalidOperationException("PedestrianDraw02 column requires one native mesh and surface slot per LOD.");
                var mesh = meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[26]];
                var plan = lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[27] ? PedestrianDraw02FarColumn.CreatePlan(mesh.vertices, mesh.GetIndices(Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[28]), halfWidthDelta) : PedestrianDraw02ColumnWideningData.CreateDetailedPlan(lod, mesh.vertices, mesh.GetIndices(Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[29]), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return factory.CreateMovableRenderGraph(name, levels);
    }

    // The native stacked footing is a separate render graph from the column and mechanism.
    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        if (source == null || !string.Equals(source.name, "PedestrianDrawBridge02PillarBase Mesh", StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, owner, imported))
            throw new InvalidOperationException("PedestrianDraw02 footing requires its permitted native source.");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[30])
            throw new InvalidOperationException("PedestrianDraw02 footing requires its one native LOD render.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[31]]
        };
        if (sources[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[32]] == null || !string.Equals(sources[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[33]].name, "PedestrianDrawBridge02PillarBase_LOD1 Mesh", StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(sources[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[34]].asset?.path, owner, imported))
            throw new InvalidOperationException("PedestrianDraw02 footing LOD1 is not its permitted native source.");
        var levels = new MovableRoadAssets.Level[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[35]];
        for (var lod = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[36]; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[37] || meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[38]] == null || meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[39]].subMeshCount != Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[40])
                    throw new InvalidOperationException($"PedestrianDraw02 footing LOD {lod} requires one native mesh and surface slot.");
                var mesh = meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[41]];
                var plan = PedestrianDraw02FootingWideningData.CreatePlan(lod, mesh.vertices, halfWidthDelta, new[] { mesh.GetIndices(Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[42]) });
                // Preserve native vertical stacking reach and longitudinal bounds.
                var bounds = original.bounds;
                var widened = new Bounds3(new float3(bounds.min.x - halfWidthDelta, bounds.min.y, bounds.min.z), new float3(bounds.max.x + halfWidthDelta, bounds.max.y, bounds.max.z));
                levels[lod] = new MovableRoadAssets.Level(original, widened, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return factory.CreateMovableRenderGraph(name, levels);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs");
}
