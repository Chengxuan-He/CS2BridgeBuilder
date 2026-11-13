using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw02ApproachAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory, ContentPrefab content, float halfWidthDelta) =>
        GenerationWork.Synchronous(async () => { await ApplyAsync(target, factory, content, halfWidthDelta); return true; });

    internal static async System.Threading.Tasks.Task ApplyAsync(NetGeometryPrefab target, TowerFactory factory, ContentPrefab content, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw02ApproachAssembly.Apply");
        using var sources = new Draw02ApproachSources(content);
        var pieces = new NetPiecePrefab[sources.Roots.Length];
        var layoutRemainder = Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.F32[0];
        for (var family = Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.I32[0]; family < pieces.Length; family++)
        {
            var root = (NetPiecePrefab)sources.Roots[family];
            var originals = new[]
            {
                (RenderPrefab)root
            }.Concat(root.GetComponent<LodProperties>()?.m_LodMeshes ?? Array.Empty<RenderPrefab>()).ToArray();
            var levels = new MovableRoadAssets.Level[originals.Length];
            for (var lod = Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.I32[1]; lod < levels.Length; lod++)
            {
                Mesh[]? meshes = null;
                try
                {
                    var original = originals[lod];
                    meshes = PrivateGeometryReader.Read(original);
                    var supportInput0 = meshes[Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.I32[5]].vertices;
                    var supportInput1 = meshes[Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.I32[6]].GetIndices(Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.I32[7]);
                    var plan = await GenerationWork.Background("support structure plan", () => Draw02ApproachGeometry.Create(family, lod, supportInput0, supportInput1, halfWidthDelta));
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

            var generated = await factory.CreateMovableRenderGraphAsync(target.name + " Fixed Approach " + family, levels) as NetPiecePrefab ?? throw new InvalidOperationException("Draw02 approach render creation failed.");
            // Only the inner strips gain width. Native lateral layout translates all
            // remaining parts rigidly and keeps both centre seams at the origin.
            if (family < Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.I32[8])
            {
                // Families 0/1 are mutually exclusive edge/node variants of one strip.
                var signedWidth = root.m_Width + halfWidthDelta;
                generated.m_Width = Mathf.Max(Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values.F32[1], signedWidth);
                layoutRemainder = signedWidth - generated.m_Width;
            }

            pieces[family] = generated;
        }

        Draw02ApproachLayout.Apply(target, factory, pieces, layoutRemainder);
        factory.ExpandMovableNetworkAppearance();
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02ApproachAssembly_cs
internal static class Numeric_Types_Draw02_Draw02ApproachAssembly_cs
{
    static Numeric_Types_Draw02_Draw02ApproachAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02ApproachAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02ApproachAssembly_cs"));
}
