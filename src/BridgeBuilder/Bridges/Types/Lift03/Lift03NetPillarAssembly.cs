using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift03NetPillarAssembly
{
    private static MovableStructureModel.Plan CreatePlan(int part, int lod, Vector3[] vertices, int[] indices, float delta) => part switch
    {
        0 => Lift03NetTopGeometry.Create(lod, vertices, indices, delta),
        1 => Lift03NetColumnGeometry.Create(lod, vertices, indices, delta),
        2 => Lift03NetFootingGeometry.Create(lod, vertices, indices, delta),
        _ => throw new ArgumentOutOfRangeException(nameof(part))};
    internal static StaticObjectPrefab? Create(TowerFactory factory, string name, Lift03PillarSources sources, ContentPrefab content, float halfWidthDelta)
    {
        var roots = new RenderPrefab[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[0]];
        for (var part = Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[1]; part < roots.Length; part++)
        {
            var root = CreatePart(factory, name + " Part " + part, sources.Roots[part], part, halfWidthDelta);
            if (root == null)
                return null;
            roots[part] = root;
        }

        return Lift03PillarSources.CreateObject(factory, name, roots[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[2]], roots[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[3]], roots[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[4]], content);
    }

    private static RenderPrefab? CreatePart(TowerFactory factory, string name, RenderPrefab source, int part, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[5])
            throw new InvalidOperationException("Lift03 ordinary pillars require their three recorded native LODs.");
        var originals = new[]
        {
            source,
            lods[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[6]],
            lods[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[7]]
        };
        var levels = new MovableRoadAssets.Level[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[8]];
        for (var lod = Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[9]; lod < levels.Length; lod++)
        {
            var original = originals[lod];
            var stem = part switch
            {
                0 => "LiftBridge03NetPillarTop",
                1 => "LiftBridge03NetPillar",
                2 => "LiftBridge03NetPillarBase",
                _ => throw new ArgumentOutOfRangeException(nameof(part))};
            var expected = stem + (lod == Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[10] ? "" : "_LOD" + lod) + " Mesh";
            if (original == null || original.asset != null || original.name != expected)
                throw new InvalidOperationException("Lift03 ordinary pillars require their reconstructed native render inputs.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[11] || meshes[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[12]].subMeshCount != Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[13])
                    throw new InvalidOperationException("Lift03 ordinary pillars require their recorded single native surface slot.");
                var plan = CreatePlan(part, lod, meshes[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[14]].vertices, meshes[Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[15]].GetIndices(Numeric_Types_Lift03_Lift03NetPillarAssembly_cs.Values.I32[16]), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x = plan.Positions.Min(p => p.x);
                bounds.max.x = plan.Positions.Max(p => p.x);
                // Keep authored Y/Z bounds and native stacking semantics.
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return factory.CreateMovableRenderGraph(name, levels);
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03NetPillarAssembly_cs
internal static class Numeric_Types_Lift03_Lift03NetPillarAssembly_cs
{
    static Numeric_Types_Lift03_Lift03NetPillarAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03NetPillarAssembly_cs");
}
