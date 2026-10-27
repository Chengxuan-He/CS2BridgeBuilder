using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw02SupportAssembly
{
    // Sources are privately reconstructed from the recorded Draw02 descriptors, not
    // borrowed prototype Prefabs. The factory owns the resulting persistent renders.
    internal static RenderPrefab? Create(TowerFactory factory, string name, RenderPrefab root, bool column, float halfWidthDelta)
    {
        var children = root.GetComponent<LodProperties>().m_LodMeshes;
        var originals = new[]
        {
            root,
            children[Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[0]],
            children[Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[1]]
        };
        var levels = new MovableRoadAssets.Level[originals.Length];
        for (var lod = Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[2]; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = originals[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[3] || meshes[Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[4]].subMeshCount != Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[5])
                    throw new InvalidOperationException("Draw02 support requires its recorded single native surface slot.");
                var mesh = meshes[Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[6]];
                var plan = Draw02SupportWideningData.CreatePlan(column, lod, mesh.vertices, mesh.GetIndices(Numeric_Types_Draw02_Draw02SupportAssembly_cs.Values.I32[7]), halfWidthDelta);
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
                // Preserve native authored Y/Z stacking reach; do not replace it with
                // compressed vertex extents or a motion-swept bounding box.
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

// Embedded numerical arrays: Types_Draw02_Draw02SupportAssembly_cs
internal static class Numeric_Types_Draw02_Draw02SupportAssembly_cs
{
    static Numeric_Types_Draw02_Draw02SupportAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02SupportAssembly_cs");
}
