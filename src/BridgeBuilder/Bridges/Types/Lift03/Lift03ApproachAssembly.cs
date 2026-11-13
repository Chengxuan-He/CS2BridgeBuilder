using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift03ApproachAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory, ContentPrefab content, float halfWidthDelta) =>
        GenerationWork.Synchronous(async () => { await ApplyAsync(target, factory, content, halfWidthDelta); return true; });

    internal static async System.Threading.Tasks.Task ApplyAsync(NetGeometryPrefab target, TowerFactory factory, ContentPrefab content, float halfWidthDelta)
    {
        await GenerationWork.Next("support Lift03ApproachAssembly.Apply");
        using var sources = new Lift03ApproachSources(content);
        var root = (NetPiecePrefab)sources.Roots[Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[0]];
        var children = root.GetComponent<LodProperties>().m_LodMeshes;
        var originals = new[]
        {
            (RenderPrefab)root,
            children[Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[1]],
            children[Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[2]]
        };
        var levels = new MovableRoadAssets.Level[originals.Length];
        for (var lod = Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[3]; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = originals[lod];
                meshes = PrivateGeometryReader.Read(original);
                var supportInput0 = meshes[Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[7]].vertices;
                var supportInput1 = meshes[Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[8]].GetIndices(Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.I32[9]);
                var plan = await GenerationWork.Background("support structure plan", () => Lift03ApproachStructure.Create(lod, supportInput0, supportInput1, halfWidthDelta));
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

        var generated = await factory.CreateMovableRenderGraphAsync(target.name + " Fixed Approach", levels) as NetPiecePrefab ?? throw new InvalidOperationException("Lift03 approach render creation failed.");
        generated.m_Width = root.m_Width + Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values.F32[0] * halfWidthDelta;
        Lift03ApproachLayout.Apply(target, factory, new[] { generated }, content);
        factory.ExpandMovableNetworkAppearance();
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03ApproachAssembly_cs
internal static class Numeric_Types_Lift03_Lift03ApproachAssembly_cs
{
    static Numeric_Types_Lift03_Lift03ApproachAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift03_Lift03ApproachAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift03_Lift03ApproachAssembly_cs"));
}
