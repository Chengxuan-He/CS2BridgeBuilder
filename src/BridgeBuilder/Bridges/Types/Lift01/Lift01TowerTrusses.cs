using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01TowerTrusses
{
    // Original transverse tower and upper gantry assemblies, including their internal side
    // diagonals and centre connection plates. No runtime part identification.
    private static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift01TowerTrusses.Data", () => HardcodedNumbers.Load("Types_Lift01_TowerTrussMap"));
    internal static MovableStructureModel.Plan Complete(MovableStructureModel.Plan plan, int lod, float delta)
    {
        var positions = plan.Positions.Take(plan.SourcePositions.Length).ToArray();
        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        for (var assembly = Numeric_Types_Lift01_Lift01TowerTrusses_cs.Values.I32[2]; assembly < Numeric_Types_Lift01_Lift01TowerTrusses_cs.Values.I32[3]; assembly++)
        {
            var group = lod * Numeric_Types_Lift01_Lift01TowerTrusses_cs.Values.I32[4] + assembly;
            var start = Numeric_Types_Lift01_Lift01TowerTrusses_cs.Values.I32[5];
            for (var previous = Numeric_Types_Lift01_Lift01TowerTrusses_cs.Values.I32[6]; previous < group; previous++)
                start += Data.I32[previous];
            var count = Data.I32[group];
            var halfSpan = Data.F32[assembly];
            var scale = (halfSpan + delta) / halfSpan;
            var frame = Matrix4x4.identity;
            frame.m00 = scale;
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX,
                new[] { plan.SourcePositions, positions }, Array.Empty<float[]>(), new[] { scale },
                first: start, count: count, indices: new[] { Data.I32 });
            for (var n = start; n < start + count; n++)
                frames.Add(new KeyValuePair<int, Matrix4x4>(Data.I32[n], frame));
        }

        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices, plan.RemoveTriangles, plan.AddedVertices, plan.Fragments, plan.InteriorVertices, plan.Positions.Skip(positions.Length).Select(p => p.x).ToArray(), plan.ReconstructsAuthoredPositions, plan.UvCorrections, plan.PreparedDomains, frames.ToArray(), plan.SourceCopies);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01TowerTrusses_cs
internal static class Numeric_Types_Lift01_Lift01TowerTrusses_cs
{
    static Numeric_Types_Lift01_Lift01TowerTrusses_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01TowerTrusses_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01TowerTrusses_cs"));
}
