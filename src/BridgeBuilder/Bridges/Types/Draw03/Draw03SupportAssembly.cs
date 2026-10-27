using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw03SupportAssembly
{
    internal static RenderPrefab? Create(TowerFactory factory, string name, RenderPrefab source, bool footing, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[0])
            throw new InvalidOperationException("Draw03 supports require their three recorded native LODs.");
        var originals = new[]
        {
            source,
            lods[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[1]],
            lods[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[2]]
        };
        var levels = new MovableRoadAssets.Level[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[3]];
        for (var lod = Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[4]; lod < levels.Length; lod++)
        {
            var original = originals[lod];
            var expected = "DrawBridge03Pillar" + (footing ? "Base" : "") + (lod == Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[5] ? "" : "_LOD" + lod) + " Mesh";
            if (original == null || original.asset != null || original.name != expected)
                throw new InvalidOperationException("Draw03 supports require their reconstructed native render inputs.");
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[6] || meshes[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[7]].subMeshCount != Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[8])
                    throw new InvalidOperationException("Draw03 supports require their recorded single native surface slot.");
                var plan = lod == Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[9] ? Draw03FarSupports.Create(footing, meshes[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[10]].vertices, meshes[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[11]].GetIndices(Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[12]), halfWidthDelta) : Draw03DetailedSupports.Create(footing, lod, meshes[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[13]].vertices, meshes[Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[14]].GetIndices(Numeric_Types_Draw03_Draw03SupportAssembly_cs.Values.I32[15]), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
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

// Embedded numerical arrays: Types_Draw03_Draw03SupportAssembly_cs
internal static class Numeric_Types_Draw03_Draw03SupportAssembly_cs
{
    static Numeric_Types_Draw03_Draw03SupportAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03SupportAssembly_cs");
}
