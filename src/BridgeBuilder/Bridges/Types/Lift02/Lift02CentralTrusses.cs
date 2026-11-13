using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Complete measured transverse assemblies; outer side frames retain their prior map.
internal static class Lift02CentralTrusses
{
    private static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift02CentralTrusses.Data", () => HardcodedNumbers.Load("Types_Lift02_CentralTrusses"));
    internal static MovableStructureModel.Plan Complete(MovableStructureModel.Plan plan, int lod, float delta)
    {
        var positions = plan.Positions.Take(plan.SourcePositions.Length).ToArray();
        var frames = plan.FrameTransforms.ToDictionary(p => p.Key, p => p.Value);
        int assemblies = Data.F32.Length;
        int start = Numeric_Types_Lift02_Lift02CentralTrusses_cs.Values.I32[1] * assemblies;
        for (int n = Numeric_Types_Lift02_Lift02CentralTrusses_cs.Values.I32[2]; n < lod * assemblies; n++)
            start += Data.I32[n];
        for (int assembly = Numeric_Types_Lift02_Lift02CentralTrusses_cs.Values.I32[3]; assembly < assemblies; assembly++)
        {
            int count = Data.I32[lod * assemblies + assembly];
            float scale = (Data.F32[assembly] + delta) / Data.F32[assembly];
            var frame = Matrix4x4.identity;
            frame.m00 = scale;
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX,
                new[] { plan.SourcePositions, positions }, Array.Empty<float[]>(), new[] { scale },
                first: start, count: count, indices: new[] { Data.I32 });
            for (int n = start; n < start + count; n++)
            {
                int vertex = Data.I32[n];
                frames[vertex] = frame;
            }

            start += count;
        }

        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices, plan.RemoveTriangles, plan.AddedVertices, plan.Fragments, plan.InteriorVertices, plan.Positions.Skip(positions.Length).Select(p => p.x).ToArray(), plan.ReconstructsAuthoredPositions, plan.UvCorrections, plan.PreparedDomains, frames.Select(p => new KeyValuePair<int, Matrix4x4>(p.Key, p.Value)).ToArray(), plan.SourceCopies);
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02CentralTrusses_cs
internal static class Numeric_Types_Lift02_Lift02CentralTrusses_cs
{
    static Numeric_Types_Lift02_Lift02CentralTrusses_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02CentralTrusses_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02CentralTrusses_cs"));
}
