using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01SupportAssembly
{
    // The two native pier columns span the centreline at Z +/-80.31 m.
    // They have no lateral placement offset: retain their authored shape rather
    // than treating these columns as the widened road-adjacent base/platform.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string name, RenderPrefab source) => Create(factory, name, source, false, Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.F32[0]);
    internal static RenderPrefab? CreateFooting(TowerFactory factory, string name, RenderPrefab source, float halfWidthDelta) => Create(factory, name, source, true, halfWidthDelta);
    private static RenderPrefab? Create(TowerFactory factory, string name, RenderPrefab source, bool footing, float delta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[0])
            throw new InvalidOperationException("Lift01 support requires its three recorded native LODs.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[1]],
            lods[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[2]]
        };
        var vertexCounts = footing ? new[]
        {
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[3],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[4],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[5]
        }

        : new[]
        {
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[6],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[7],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[8]
        };
        var indexCounts = footing ? new[]
        {
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[9],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[10],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[11]
        }

        : new[]
        {
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[12],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[13],
            Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[14]
        };
        var levels = new MovableRoadAssets.Level[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[15]];
        for (var lod = Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[16]; lod < levels.Length; lod++)
        {
            var original = sources[lod];
            var expected = "LiftBridge01Pillar" + (footing ? "Base" : "") + (lod == Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[17] ? "" : "_LOD" + lod) + " Mesh";
            // Inputs are reconstructed privately by Lift01SourceRenders, whose
            // recorded CIDs resolve only built-in resources. Never load a saved
            // bridge or require the original prototype Prefab here.
            if (original == null || original.asset != null || !string.Equals(original.name, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("Lift01 support requires its reconstructed native render inputs.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[18] || meshes[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[19]] == null || meshes[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[20]].subMeshCount != Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[21] || meshes[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[22]].vertexCount != vertexCounts[lod])
                    throw new InvalidOperationException("Lift01 support source vertex layout changed.");
                var mesh = meshes[Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[23]];
                var indices = mesh.GetIndices(Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[24]);
                if (indices.Length != indexCounts[lod])
                    throw new InvalidOperationException("Lift01 support source index layout changed.");
                var positions = mesh.vertices;
                var plan = footing ? lod == Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[25] ? Lift01FarFooting.Create(positions, indices, delta) : Lift01Footing.CreateDetailed(lod, positions, indices, delta) : new MovableStructureModel.Plan(positions, positions, new[] { indices }, new[] { new bool[indices.Length / Numeric_Types_Lift01_Lift01SupportAssembly_cs.Values.I32[26]] });
                var bounds = original.bounds;
                if (footing)
                {
                    bounds.min.x -= delta;
                    bounds.max.x += delta;
                }

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

// Embedded numerical arrays: Types_Lift01_Lift01SupportAssembly_cs
internal static class Numeric_Types_Lift01_Lift01SupportAssembly_cs
{
    static Numeric_Types_Lift01_Lift01SupportAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01SupportAssembly_cs");
}
