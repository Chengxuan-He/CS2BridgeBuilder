using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw03PillarAssembly
{
    // Ordinary network supports are distinct from the mechanism's stacking supports.
    // Reconstruct native inputs privately; only factory-owned results escape this scope.
    internal static StaticObjectPrefab? Create(TowerFactory factory, string name, RenderPrefab nativeDefaultBase, ContentPrefab content, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, name, nativeDefaultBase, content, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab?> CreateAsync(TowerFactory factory, string name, RenderPrefab nativeDefaultBase, ContentPrefab content, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw03PillarAssembly.Create");
        using var sources = new Draw03PillarSources(nativeDefaultBase, content);
        var generated = new RenderPrefab[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[0]];
        var labels = new[]
        {
            "Top",
            "Column",
            "Footing"
        };
        for (var part = Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[1]; part < generated.Length; part++)
        {
            var root = sources.Roots[part];
            var children = root.GetComponent<LodProperties>().m_LodMeshes;
            var originals = new[]
            {
                root,
                children[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[2]],
                children[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[3]]
            };
            var levels = new MovableRoadAssets.Level[originals.Length];
            for (var lod = Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[4]; lod < levels.Length; lod++)
            {
                Mesh[]? meshes = null;
                try
                {
                    var original = originals[lod];
                    meshes = PrivateGeometryReader.Read(original);
                    if (meshes.Length != Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[5] || meshes[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[6]].subMeshCount != Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[7])
                        throw new InvalidOperationException("Draw03 ordinary pillar requires its recorded single native surface slot.");
                    var mesh = meshes[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[8]];
                    var supportInput0 = mesh.vertices;
                    var supportInput1 = mesh.GetIndices(Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[11]);
                    var supportInput2 = mesh.vertices;
                    var supportInput3 = mesh.GetIndices(Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[12]);
                    var plan = await GenerationWork.Background("support structure plan", () => lod == Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[9] && part != Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[10] ? Draw03FarNetPillars.Create(part, supportInput0, supportInput1, halfWidthDelta) : Draw03NetPillarParts.Create(part, lod, supportInput2, supportInput3, halfWidthDelta));
                    var bounds = original.bounds;
                    bounds.min.x -= halfWidthDelta;
                    bounds.max.x += halfWidthDelta;
                    // Native Y/Z bounds control stacking: compressed vertices may slightly
                    // exceed them, but model/submesh bounds already account for those vertices.
                    // Expand only the changed lateral extent.
                    foreach (var position in plan.Positions)
                    {
                        bounds.min.x = Math.Min(bounds.min.x, position.x);
                        bounds.max.x = Math.Max(bounds.max.x, position.x);
                    }

                    levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
                }
                finally
                {
                    PrivateGeometryReader.Release(meshes);
                }
            }

            var render = await factory.CreateMovableRenderGraphAsync($"{name} {labels[part]}", levels);
            if (render == null)
                return null;
            generated[part] = render;
        }

        return Draw03PillarSources.CreateObject(factory, name, generated[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[13]], generated[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[14]], generated[Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values.I32[15]], content);
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03PillarAssembly_cs
internal static class Numeric_Types_Draw03_Draw03PillarAssembly_cs
{
    static Numeric_Types_Draw03_Draw03PillarAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw03_Draw03PillarAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Draw03_Draw03PillarAssembly_cs"));
}
