using System;
using System.Collections.Generic;
using UnityEngine;
using Kind = BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind;

namespace BridgeBuilder.Bridges;
// Measured original inner upper wall, adjoining panel endpoints and leaf end caps.
internal static class Draw02EndClosure
{
    internal static MovableStructureModel.Plan Apply(int lod, MovableStructureModel.Plan plan, MovableSelectedRoadLayout layout)
    {
        var data = EndClosureMeasurements.Values;
        var positions = new Vector3[plan.SourcePositions.Length];
        Array.Copy(plan.Positions, positions, positions.Length);
        var removed = (bool[])plan.RemoveTriangles[Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[0]].Clone();
        var edges = new List<MovableStructureModel.EdgeVertex>(plan.AddedVertices);
        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        var fragments = new List<MovableStructureModel.Fragment>();
        var replaced = new HashSet<int>();
        var constructedX = new List<float>();
        for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[1]; i < plan.ConstructedCount; i++)
            constructedX.Add(plan.Positions[positions.Length + i].x);
        if (plan.InteriorVertices.Length != Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[2])
            throw new InvalidOperationException("Draw02 end closure requires its measured edge-only source recipe.");
        var cache = new Dictionary<float, MovableRoadLaneLayout.Boundary[]>();
        for (int record = lod == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[3] ? Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[4] : lod * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[5]; record < (lod == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[6] ? Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[7] : lod * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[8] + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[9]); record++)
        {
            int h = record * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[10], side = record % Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[11] == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[12] ? -Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[13] : Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[14];
            if (positions.Length != data.I32[h] || removed.Length != data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[15]])
                throw new InvalidOperationException("Draw02 end closure source differs from its measured prototype.");
            int reference = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[16]];
            float bodyShift = plan.Positions[reference].x - plan.SourcePositions[reference].x;
            float Shift(Vector3 source)
            {
                if (lod == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[17] || layout.Lanes == null || layout.Lanes.Length == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[18])
                    return bodyShift;
                if (!cache.TryGetValue(source.z, out var boundaries))
                    cache.Add(source.z, boundaries = layout.LaneBoundariesAt(source.z));
                bool found = false;
                float x = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[20];
                foreach (var boundary in boundaries)
                {
                    bool valid = side > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[21] ? (boundary.Left & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[22]) && (boundary.Right & Kind.Car) == (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[23]) : (boundary.Right & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[24]) && (boundary.Left & Kind.Car) == (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[25]);
                    if (valid && (!found || (side > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[26] ? boundary.X > x : boundary.X < x)))
                    {
                        found = true;
                        x = boundary.X;
                    }
                }

                if (!found)
                    return bodyShift;
                return x - Draw02LaneRails.Anchor(Draw02LaneRails.Part(lod, false, side > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[28], source.z > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[29]));
            }

            void Frame(int vertex, float delta, int coefficient)
            {
                float dx = data.F32[coefficient + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[30]], dy = data.F32[coefficient + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[31]];
                if (delta == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[32] || (dx == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[33] && dy == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[34]))
                    return;
                var matrix = Matrix4x4.identity;
                matrix.m00 = AffineGeometry.AddScaled(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.F32[0], dx, delta);
                matrix.m01 = dy * delta;
                frames.Add(new KeyValuePair<int, Matrix4x4>(vertex, matrix));
            }

            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[35]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[36]]; i++)
            {
                int row = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[37]] + i * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[38], vertex = data.I32[row], c = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[39]];
                float shift = Shift(plan.SourcePositions[vertex]), delta = shift - bodyShift;
                positions[vertex].x = data.F32[c] == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.F32[0]
                    ? AffineGeometry.Translate(plan.SourcePositions[vertex].x, shift)
                    : AffineGeometry.AddScaled(plan.Positions[vertex].x, data.F32[c], delta);
                Frame(vertex, delta, c);
            }

            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[40]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[41]]; i++)
            {
                int vertex = data.I32[data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[42]] + i];
                positions[vertex].x = AffineGeometry.Translate(plan.SourcePositions[vertex].x, Shift(plan.SourcePositions[vertex]));
            }

            var local = new int[data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[43]]];
            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[44]; i < local.Length; i++)
            {
                int row = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[45]] + i * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[46], a = data.I32[row], b = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[47]], c = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[48]];
                float t = data.F32[c];
                var source = Vector3.LerpUnclamped(plan.SourcePositions[a], plan.SourcePositions[b], t);
                float shift = Shift(source), delta = shift - bodyShift, originalX = Mathf.LerpUnclamped(plan.Positions[a].x, plan.Positions[b].x, t);
                local[i] = positions.Length + edges.Count;
                edges.Add(new MovableStructureModel.EdgeVertex(a, b, t));
                constructedX.Add(data.F32[c + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[49]] == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.F32[0]
                    ? AffineGeometry.Translate(source.x, shift)
                    : AffineGeometry.AddScaled(originalX, data.F32[c + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[49]], delta));
                Frame(local[i], delta, c + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[50]);
            }

            int offset = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[51]];
            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[52]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[53]]; i++)
            {
                int face = data.I32[offset++], count = data.I32[offset++];
                var indices = new int[count];
                for (int k = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[54]; k < count; k++)
                {
                    int id = data.I32[offset++];
                    indices[k] = id < Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[55] ? local[-id - Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[56]] : id;
                }

                removed[face] = true;
                replaced.Add(face);
                fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[57], face, indices));
            }

            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[58]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[59]]; i++)
            {
                int face = data.I32[data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[60]] + i];
                removed[face] = false;
                replaced.Add(face);
            }
        }

        foreach (var fragment in plan.Fragments)
            if (!replaced.Contains(fragment.Triangle))
                fragments.Add(fragment);
        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices, new[] { removed }, edges.ToArray(), fragments.ToArray(), plan.InteriorVertices, constructedX.ToArray(), uvCorrections: plan.UvCorrections, preparedDomains: plan.PreparedDomains, frameTransforms: frames.ToArray());
    }
}

internal static class EndClosureMeasurements
{
    static EndClosureMeasurements()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_EndClosureMeasurements");
}

// Embedded numerical arrays: Types_Draw02_Draw02EndClosure_cs
internal static class Numeric_Types_Draw02_Draw02EndClosure_cs
{
    static Numeric_Types_Draw02_Draw02EndClosure_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02EndClosure_cs");
}
