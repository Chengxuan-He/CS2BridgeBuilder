using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw01SupportAssembly
{
    internal static RenderPrefab? Create(TowerFactory factory, string name, RenderPrefab source, bool footing, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, name, source, footing, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateAsync(TowerFactory factory, string name, RenderPrefab source, bool footing, float halfWidthDelta)
    {
        await GenerationWork.Next("support PedestrianDraw01SupportAssembly.Create");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var originals = new[]
        {
            source,
            lods[Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[1]],
            lods[Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[2]]
        };
        var levels = new MovableRoadAssets.Level[Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[3]];
        for (var lod = Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[4]; lod < levels.Length; lod++)
        {
            var original = originals[lod];
            var expected = "PedestrianDrawBridge01Pillar" + (footing ? "Base" : "") + (lod == Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[5] ? "" : "_LOD" + lod) + " Mesh";
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                var supportInput0 = meshes[Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[9]].vertices;
                var supportInput1 = meshes[Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[10]].GetIndices(Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values.I32[11]);
                var plan = await GenerationWork.Background("support structure plan", () => PedestrianDraw01SupportGeometry.Create(footing, lod, supportInput0, supportInput1, halfWidthDelta));
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

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs.Values", () => HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01SupportAssembly_cs"));
}
