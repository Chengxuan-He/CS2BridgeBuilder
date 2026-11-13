using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift02NetPillarAssembly
{
    internal static StaticObjectPrefab? Create(TowerFactory factory, string name, Lift02PillarSources sources, ContentPrefab content, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, name, sources, content, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab?> CreateAsync(TowerFactory factory, string name, Lift02PillarSources sources, ContentPrefab content, float halfWidthDelta)
    {
        await GenerationWork.Next("support Lift02NetPillarAssembly.Create");
        var roots = new RenderPrefab[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[0]];
        for (var part = Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[1]; part < roots.Length; part++)
        {
            var root = await CreatePartAsync(factory, name + " Part " + part, sources.Roots[part], part, halfWidthDelta);
            if (root == null)
                return null;
            roots[part] = root;
        }

        return Lift02PillarSources.CreateObject(factory, name, roots[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[2]], roots[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[3]], roots[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[4]], content);
    }

    private static RenderPrefab? CreatePart(TowerFactory factory, string name, RenderPrefab source, int part, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreatePartAsync(factory, name, source, part, halfWidthDelta));

    private static async System.Threading.Tasks.Task<RenderPrefab?> CreatePartAsync(TowerFactory factory, string name, RenderPrefab source, int part, float halfWidthDelta)
    {
        await GenerationWork.Next("support Lift02NetPillarAssembly.CreatePart");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var originals = new[]
        {
            source,
            lods[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[6]],
            lods[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[7]]
        };
        var levels = new MovableRoadAssets.Level[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[8]];
        for (var lod = Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[9]; lod < levels.Length; lod++)
        {
            var original = originals[lod];
            var stem = part switch
            {
                0 => "LiftBridge02NetPillarTop",
                1 => "LiftBridge02NetPillar",
                2 => "LiftBridge02NetPillarBase"};
            var expected = stem + (lod == Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[10] ? "" : "_LOD" + lod) + " Mesh";
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                var supportInput0 = meshes[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[14]].vertices;
                var supportInput1 = meshes[Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[15]].GetIndices(Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values.I32[16]);
                var plan = await GenerationWork.Background("support structure plan", () => Lift02NetPillarGeometry.Create(part, lod, supportInput0, supportInput1, halfWidthDelta));
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

        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02NetPillarAssembly_cs
internal static class Numeric_Types_Lift02_Lift02NetPillarAssembly_cs
{
    static Numeric_Types_Lift02_Lift02NetPillarAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02NetPillarAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02NetPillarAssembly_cs"));
}
