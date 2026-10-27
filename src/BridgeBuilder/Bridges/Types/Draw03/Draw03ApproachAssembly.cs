using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw03ApproachAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory, ContentPrefab content, float halfWidthDelta)
    {
        using var sources = new Draw03ApproachSources(content);
        var pieces = new NetPiecePrefab[sources.Roots.Length];
        var layoutRemainder = Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.F32[0];
        for (var family = Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[0]; family < pieces.Length; family++)
        {
            var root = (NetPiecePrefab)sources.Roots[family];
            var originals = new[]
            {
                (RenderPrefab)root
            }.Concat(root.GetComponent<LodProperties>()?.m_LodMeshes ?? Array.Empty<RenderPrefab>()).ToArray();
            var levels = new MovableRoadAssets.Level[originals.Length];
            for (var lod = Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[1]; lod < levels.Length; lod++)
            {
                Mesh[]? meshes = null;
                try
                {
                    var original = originals[lod];
                    meshes = PrivateGeometryReader.Read(original);
                    if (meshes.Length != Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[2] || meshes[Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[3]].subMeshCount != Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[4])
                        throw new InvalidOperationException("Draw03 approach requires its recorded single native surface slot.");
                    var plan = Draw03ApproachGeometry.Create(family, lod, meshes[Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[5]].vertices, meshes[Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[6]].GetIndices(Numeric_Types_Draw03_Draw03ApproachAssembly_cs.Values.I32[7]), halfWidthDelta);
                    var bounds = original.bounds;
                    bounds.min.x = plan.Positions.Min(position => position.x);
                    bounds.max.x = plan.Positions.Max(position => position.x);
                    levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
                }
                finally
                {
                    PrivateGeometryReader.Release(meshes);
                }
            }

            var generated = factory.CreateMovableRenderGraph(target.name + " Fixed Approach " + family, levels) as NetPiecePrefab ?? throw new InvalidOperationException("Draw03 approach render creation failed.");
            // Match local mesh scaling with every structural strip's native width.
            // This spreads the width change across the full truss rather than its centre.
            if (Draw03Truss.IsApproachStructure(family))
            {
                generated.m_Width = root.m_Width * Draw03Truss.Scale(halfWidthDelta);
            }

            pieces[family] = generated;
        }

        Draw03ApproachLayout.Apply(target, factory, pieces, layoutRemainder);
        factory.ExpandMovableNetworkAppearance();
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03ApproachAssembly_cs
internal static class Numeric_Types_Draw03_Draw03ApproachAssembly_cs
{
    static Numeric_Types_Draw03_Draw03ApproachAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03ApproachAssembly_cs");
}
