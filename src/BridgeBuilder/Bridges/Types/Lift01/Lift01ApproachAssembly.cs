using System;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01ApproachAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory, ContentPrefab content, float halfWidthDelta)
    {
        using var sources = new Lift01ApproachSources(content);
        var root = (NetPiecePrefab)sources.Roots[Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[0]];
        var children = root.GetComponent<LodProperties>().m_LodMeshes;
        var originals = new[]
        {
            (RenderPrefab)root,
            children[Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[1]],
            children[Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[2]]
        };
        var levels = new MovableRoadAssets.Level[originals.Length];
        for (var lod = Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[3]; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = originals[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[4] || meshes[Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[5]].subMeshCount != Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[6])
                    throw new InvalidOperationException("Lift01 approach requires its recorded single native surface slot.");
                var plan = Lift01ApproachStructure.Create(lod, meshes[Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[7]].vertices, meshes[Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[8]].GetIndices(Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.I32[9]), halfWidthDelta);
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

        var generated = factory.CreateMovableRenderGraph(target.name + " Fixed Approach", levels) as NetPiecePrefab ?? throw new InvalidOperationException("Lift01 approach render creation failed.");
        generated.m_Width = root.m_Width + Numeric_Types_Lift01_Lift01ApproachAssembly_cs.Values.F32[0] * halfWidthDelta;
        Lift01ApproachLayout.Apply(target, factory, new[] { generated }, content);
        factory.ExpandMovableNetworkAppearance();
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01ApproachAssembly_cs
internal static class Numeric_Types_Lift01_Lift01ApproachAssembly_cs
{
    static Numeric_Types_Lift01_Lift01ApproachAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01ApproachAssembly_cs");
}
