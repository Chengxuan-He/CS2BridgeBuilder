using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Offline complete-assembly and longitudinal-member memberships, in native vertex order.
// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02TrussMapping_cs
// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02UpperTruss_cs
internal static class PedestrianDraw02TrussMapping
{
    private static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.PedestrianDraw02TrussMapping.Data", () => HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02TrussMapping_cs"));
    private static HardcodedNumbers Upper => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.PedestrianDraw02TrussMapping.Upper", () => HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02UpperTruss_cs"));

    internal static MovableStructureModel.Plan Apply(int lod, MovableStructureModel.Plan plan, float delta)
    {
        int start = lod == 0 ? 0 : 3 + Data.I32[1] + Data.I32[2];
        int f = lod == 0 ? 0 : 1 + Data.I32[2];
        int count = Data.I32[start], assemblyCount = Data.I32[start + 1], ribCount = Data.I32[start + 2];
        var positions = new Vector3[count];
        Array.Copy(plan.Positions, positions, count);
        float scale = (Data.F32[f] + delta) / Data.F32[f];
        var matrix = Matrix4x4.identity;
        matrix.m00 = scale;
        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX, new[] { plan.SourcePositions, positions },
            Array.Empty<float[]>(), new[] { scale }, first: start + 3, count: assemblyCount, indices: new[] { Data.I32 });
        for (int row = 0; row < assemblyCount; row++)
            frames.Add(new KeyValuePair<int, Matrix4x4>(Data.I32[start + 3 + row], matrix));
        // The three overhead bays are distinct from the six below-deck bays.
        // Their plates and collars share the complete overhead reach, not their own span.
        int upperStart = lod == 0 ? 0 : 2 + Upper.I32[1];
        int upperCount = Upper.I32[upperStart + 1];
        float upperScale = (Upper.F32[lod] + delta) / Upper.F32[lod];
        var upperMatrix = Matrix4x4.identity;
        upperMatrix.m00 = upperScale;
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX, new[] { plan.SourcePositions, positions },
            Array.Empty<float[]>(), new[] { upperScale }, first: upperStart + 2, count: upperCount, indices: new[] { Upper.I32 });
        for (int row = 0; row < upperCount; row++)
            frames.Add(new KeyValuePair<int, Matrix4x4>(Upper.I32[upperStart + 2 + row], upperMatrix));
        var ribs = StructureRows.Slice(Data.I32, start + 3 + assemblyCount, ribCount);
        var weights = StructureRows.Slice(Data.F32, f + 1, ribCount);
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.RowDelta, new[] { plan.SourcePositions, positions },
            new[] { weights }, new[] { delta }, count: ribCount, indices: new[] { ribs });
        var constructed = new float[plan.ConstructedCount];
        for (int i = 0; i < constructed.Length; i++) constructed[i] = plan.Positions[count + i].x;
        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices,
            plan.RemoveTriangles, plan.AddedVertices, plan.Fragments, plan.InteriorVertices, constructed,
            plan.ReconstructsAuthoredPositions, plan.UvCorrections, plan.PreparedDomains, frames.ToArray(), plan.SourceCopies);
    }
}
