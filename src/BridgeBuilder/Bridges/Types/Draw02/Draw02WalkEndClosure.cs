using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// The coarse original end cap merges carriageway and sidewalk. Restore only its
// measured upper sidewalk strip; the existing lower cap and car clearance stay.
internal static class Draw02WalkEndClosure
{
    internal static MovableStructureModel.Plan Apply(int lod, MovableStructureModel.Plan plan, MovableSelectedRoadLayout layout)
    {
        if (lod != Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[0])
            return plan;
        bool Present(int side) => side < Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[1] ? layout.LeftSidewalk.y > layout.LeftSidewalk.x : layout.RightSidewalk.y > layout.RightSidewalk.x;
        if (!Present(-Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[2]) && !Present(Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[3]))
            return plan;
        var data = WalkEndMeasurements.Values;
        if (plan.SourcePositions.Length != data.I32[Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[4]] || plan.SourceIndices[Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[5]].Length / Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[6] != data.I32[Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[7]] || plan.InteriorVertices.Length != Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[8])
            throw new InvalidOperationException("Draw02 far sidewalk cap requires its measured source plan.");
        var positions = new Vector3[plan.SourcePositions.Length];
        Array.Copy(plan.Positions, positions, positions.Length);
        var edges = new List<MovableStructureModel.EdgeVertex>(plan.AddedVertices);
        var interiors = new List<MovableStructureModel.TriangleVertex>();
        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        var xs = new List<float>();
        for (int i = Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[9]; i < plan.ConstructedCount; i++)
            xs.Add(plan.Positions[positions.Length + i].x);
        var local = new int[data.I32[Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[10]]];
        for (int kind = Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[11]; kind < Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[12]; kind++)
            for (int i = Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[13]; i < local.Length; i++)
            {
                int h = Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[14] + i * Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[15], side = data.I32[h + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[16]];
                if (data.I32[h] != kind || !Present(side))
                    continue;
                int a = data.I32[h + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[17]], b = data.I32[h + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[18]], c = data.I32[h + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[19]], k = data.I32[h + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[20]];
                float inner = side < Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[21] ? layout.LeftSidewalk.y : layout.RightSidewalk.x;
                float outer = plan.Positions[data.I32[side < Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[22] ? Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[23] : Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[24]]].x;
                local[i] = positions.Length + edges.Count + interiors.Count;
                var frame = Matrix4x4.identity;
                frame.m00 = side * (outer - inner) / Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.F32[0];
                frames.Add(new KeyValuePair<int, Matrix4x4>(local[i], frame));
                if (kind == Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[26])
                    edges.Add(new MovableStructureModel.EdgeVertex(a, b, data.F32[k]));
                else
                    interiors.Add(new MovableStructureModel.TriangleVertex(a, b, c, data.F32[k], data.F32[k + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[27]]));
                xs.Add(AffineGeometry.AddScaled(inner, data.F32[k + Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[28]], outer - inner));
            }

        var added = new Dictionary<int, List<int>>();
        int offset = data.I32[Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[29]];
        for (int i = Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[30]; i < data.I32[Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[31]]; i++)
        {
            int face = data.I32[offset++], side = data.I32[offset++], count = data.I32[offset++];
            if (Present(side))
            {
                var ids = new List<int>();
                for (int k = Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[32]; k < count; k++)
                    ids.Add(local[data.I32[offset + k]]);
                added.Add(face, ids);
            }

            offset += count;
        }

        var fragments = new List<MovableStructureModel.Fragment>();
        foreach (var fragment in plan.Fragments)
        {
            if (added.TryGetValue(fragment.Triangle, out var ids))
            {
                ids.InsertRange(Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[33], fragment.Indices);
                fragments.Add(new MovableStructureModel.Fragment(fragment.Slot, fragment.Triangle, ids.ToArray()));
                added.Remove(fragment.Triangle);
            }
            else
                fragments.Add(fragment);
        }

        if (added.Count != Numeric_Types_Draw02_Draw02WalkEndClosure_cs.Values.I32[34])
            throw new InvalidOperationException("Draw02 sidewalk strip lost its original lower end cap.");
        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices, plan.RemoveTriangles, edges.ToArray(), fragments.ToArray(), interiors.ToArray(), xs.ToArray(), uvCorrections: plan.UvCorrections, preparedDomains: plan.PreparedDomains, frameTransforms: frames.ToArray());
    }
}

internal static class WalkEndMeasurements
{
    static WalkEndMeasurements()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_WalkEndMeasurements");
}

// Embedded numerical arrays: Types_Draw02_Draw02WalkEndClosure_cs
internal static class Numeric_Types_Draw02_Draw02WalkEndClosure_cs
{
    static Numeric_Types_Draw02_Draw02WalkEndClosure_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02WalkEndClosure_cs");
}
