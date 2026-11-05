using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Offline complete-assembly and longitudinal-member memberships, in native vertex order.
// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02TrussMapping_cs
// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02UpperTruss_cs
internal static class PedestrianDraw02TrussMapping
{
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02TrussMapping_cs");
    private static readonly HardcodedNumbers Upper = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02UpperTruss_cs");

    internal static MovableStructureModel.Plan Apply(int lod, MovableStructureModel.Plan plan, float delta)
    {
        if ((uint)lod > 1 || !float.IsFinite(delta)) throw new ArgumentException("Invalid PedestrianDraw02 truss input.");
        int start = lod == 0 ? 0 : 3 + Data.I32[1] + Data.I32[2];
        int f = lod == 0 ? 0 : 1 + Data.I32[2];
        int count = Data.I32[start], assemblyCount = Data.I32[start + 1], ribCount = Data.I32[start + 2];
        if (plan.SourcePositions.Length != count) throw new InvalidOperationException("PedestrianDraw02 truss source mismatch.");
        var positions = new Vector3[count];
        Array.Copy(plan.Positions, positions, count);
        float scale = (Data.F32[f] + delta) / Data.F32[f];
        var matrix = Matrix4x4.identity;
        matrix.m00 = scale;
        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        AffineGeometry.For(0, assemblyCount, row =>
        {
            int i = Data.I32[start + 3 + row];
            positions[i].x = AffineGeometry.Scale(plan.SourcePositions[i].x, scale);
        });
        for (int row = 0; row < assemblyCount; row++)
            frames.Add(new KeyValuePair<int, Matrix4x4>(Data.I32[start + 3 + row], matrix));
        // The three overhead bays are distinct from the six below-deck bays.
        // Their plates and collars share the complete overhead reach, not their own span.
        int upperStart = lod == 0 ? 0 : 2 + Upper.I32[1];
        int upperCount = Upper.I32[upperStart + 1];
        float upperScale = (Upper.F32[lod] + delta) / Upper.F32[lod];
        var upperMatrix = Matrix4x4.identity;
        upperMatrix.m00 = upperScale;
        AffineGeometry.For(0, upperCount, row =>
        {
            int i = Upper.I32[upperStart + 2 + row];
            positions[i].x = AffineGeometry.Scale(plan.SourcePositions[i].x, upperScale);
        });
        for (int row = 0; row < upperCount; row++)
            frames.Add(new KeyValuePair<int, Matrix4x4>(Upper.I32[upperStart + 2 + row], upperMatrix));
        AffineGeometry.For(0, ribCount, row =>
        {
            int i = Data.I32[start + 3 + assemblyCount + row];
            positions[i].x = AffineGeometry.AddScaled(plan.SourcePositions[i].x, Data.F32[f + 1 + row], delta);
        });
        var constructed = new float[plan.ConstructedCount];
        for (int i = 0; i < constructed.Length; i++) constructed[i] = plan.Positions[count + i].x;
        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices,
            plan.RemoveTriangles, plan.AddedVertices, plan.Fragments, plan.InteriorVertices, constructed,
            plan.ReconstructsAuthoredPositions, plan.UvCorrections, plan.PreparedDomains, frames.ToArray(), plan.SourceCopies);
    }
}
