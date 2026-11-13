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
        var frameRows = new List<(int Vertex, float Delta, float Dx, float Dy)>();
        var fragments = new List<MovableStructureModel.Fragment>();
        var replaced = new HashSet<int>();
        var constructedX = new List<float>();
        for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[1]; i < plan.ConstructedCount; i++)
            constructedX.Add(plan.Positions[positions.Length + i].x);
        var cache = new Dictionary<float, MovableRoadLaneLayout.Boundary[]>();
        for (int record = lod == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[3] ? Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[4] : lod * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[5]; record < (lod == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[6] ? Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[7] : lod * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[8] + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[9]); record++)
        {
            int h = record * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[10], side = record % Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[11] == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[12] ? -Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[13] : Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[14];
            int reference = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[16]];
            float bodyShift = plan.Positions[reference].x - plan.SourcePositions[reference].x;
            float Shift(Vector3 source)
            {
                if (lod == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[17] || layout.Lanes == null || layout.Lanes.Length == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[18])
                    return bodyShift;
                if (!cache.TryGetValue(source.z, out var boundaries))
                    cache.Add(source.z, boundaries = layout.LaneBoundariesAt(source.z));
                bool found = false;
                float x = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[19];
                foreach (var boundary in boundaries)
                {
                    bool valid = side > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[20] ? (boundary.Left & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[21]) && (boundary.Right & Kind.Car) == (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[22]) : (boundary.Right & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[23]) && (boundary.Left & Kind.Car) == (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[24]);
                    if (valid && (!found || (side > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[25] ? boundary.X > x : boundary.X < x)))
                    {
                        found = true;
                        x = boundary.X;
                    }
                }

                if (!found)
                    return bodyShift;
                return x - Draw02LaneRails.Anchor(Draw02LaneRails.Part(lod, false, side > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[26], source.z > Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[27]));
            }

            void Frame(int vertex, float delta, int coefficient)
            {
                float dx = data.F32[coefficient + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[28]], dy = data.F32[coefficient + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[29]];
                if (delta == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[30] || (dx == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[31] && dy == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[32]))
                    return;
                frameRows.Add((vertex, delta, dx, dy));
            }

            var moves = new List<(int Vertex, int Program, float A, float B, float C)>();
            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[33]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[34]]; i++)
            {
                int row = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[35]] + i * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[36], vertex = data.I32[row], c = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[37]];
                float shift = Shift(plan.SourcePositions[vertex]), delta = shift - bodyShift;
                bool translate = data.F32[c] == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.F32[0];
                moves.Add((vertex, translate ? StructurePrograms.AddScalar : StructurePrograms.AddProductScalar,
                    translate ? plan.SourcePositions[vertex].x : plan.Positions[vertex].x, translate ? shift : data.F32[c], delta));
                Frame(vertex, delta, c);
            }

            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[38]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[39]]; i++)
            {
                int vertex = data.I32[data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[40]] + i];
                moves.Add((vertex, StructurePrograms.AddScalar, plan.SourcePositions[vertex].x, Shift(plan.SourcePositions[vertex]), default));
            }

            var moveRows = moves.ToArray();
            var moved = StructureRows.Evaluate(Array.ConvertAll(moveRows, r => r.Program),
                Array.ConvertAll(moveRows, r => r.A), Array.ConvertAll(moveRows, r => r.B), Array.ConvertAll(moveRows, r => r.C));
            for (int i = 0; i < moveRows.Length; i++) positions[moveRows[i].Vertex].x = moved[i];

            var local = new int[data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[41]]];
            var sourcePoints = new Vector3[local.Length];
            var originalPoints = new Vector3[local.Length];
            var edgeA = new int[local.Length]; var edgeB = new int[local.Length]; var edgeOutput = new int[local.Length];
            var edgeFractions = new float[local.Length];
            for (int i = 0; i < local.Length; i++)
            {
                int row = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[43]] + i * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[44];
                edgeA[i] = data.I32[row]; edgeB[i] = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[45]];
                edgeOutput[i] = i; edgeFractions[i] = data.F32[data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[46]]];
            }
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.EdgeXyz, new[] { plan.SourcePositions, sourcePoints },
                new[] { edgeFractions }, Array.Empty<float>(), count: local.Length, indices: new[] { edgeA, edgeB, edgeOutput });
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.EdgeXyz, new[] { plan.Positions, originalPoints },
                new[] { edgeFractions }, Array.Empty<float>(), count: local.Length, indices: new[] { edgeA, edgeB, edgeOutput });
            var constructedPrograms = new int[local.Length]; var constructedOrigins = new float[local.Length];
            var constructedTerms = new float[local.Length]; var constructedDeltas = new float[local.Length];
            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[42]; i < local.Length; i++)
            {
                int row = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[43]] + i * Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[44], a = data.I32[row], b = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[45]], c = data.I32[row + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[46]];
                float t = data.F32[c];
                var source = sourcePoints[i];
                float shift = Shift(source), delta = shift - bodyShift, originalX = originalPoints[i].x;
                local[i] = positions.Length + edges.Count;
                edges.Add(new MovableStructureModel.EdgeVertex(a, b, t));
                bool translate = data.F32[c + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[47]] == Numeric_Types_Draw02_Draw02EndClosure_cs.Values.F32[0];
                constructedPrograms[i] = translate ? StructurePrograms.AddScalar : StructurePrograms.AddProductScalar;
                constructedOrigins[i] = translate ? source.x : originalX;
                constructedTerms[i] = translate ? shift : data.F32[c + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[47]];
                constructedDeltas[i] = delta;
                Frame(local[i], delta, c + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[48]);
            }
            constructedX.AddRange(StructureRows.Evaluate(constructedPrograms, constructedOrigins, constructedTerms, constructedDeltas));

            int offset = data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[49]];
            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[50]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[51]]; i++)
            {
                int face = data.I32[offset++], count = data.I32[offset++];
                var indices = new int[count];
                for (int k = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[52]; k < count; k++)
                {
                    int id = data.I32[offset++];
                    indices[k] = id < Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[53] ? local[-id - Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[54]] : id;
                }

                removed[face] = true;
                replaced.Add(face);
                fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[55], face, indices));
            }

            for (int i = Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[56]; i < data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[57]]; i++)
            {
                int face = data.I32[data.I32[h + Numeric_Types_Draw02_Draw02EndClosure_cs.Values.I32[58]] + i];
                removed[face] = false;
                replaced.Add(face);
            }
        }

        var frameData = frameRows.ToArray();
        var xx = StructureRows.Evaluate(Array.ConvertAll(frameData, _ => StructurePrograms.AddProductScalar),
            Array.ConvertAll(frameData, _ => Numeric_Types_Draw02_Draw02EndClosure_cs.Values.F32[0]),
            Array.ConvertAll(frameData, r => r.Dx), Array.ConvertAll(frameData, r => r.Delta));
        var xy = StructureRows.Evaluate(Array.ConvertAll(frameData, _ => StructurePrograms.ProductScalar),
            Array.ConvertAll(frameData, r => r.Dy), Array.ConvertAll(frameData, r => r.Delta), new float[frameData.Length]);
        for (int i = 0; i < frameData.Length; i++)
        {
            var frame = Matrix4x4.identity; frame.m00 = xx[i]; frame.m01 = xy[i];
            frames.Add(new KeyValuePair<int, Matrix4x4>(frameData[i].Vertex, frame));
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

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.EndClosureMeasurements.Values", () => HardcodedNumbers.Load("Types_Draw02_EndClosureMeasurements"));
}

// Embedded numerical arrays: Types_Draw02_Draw02EndClosure_cs
internal static class Numeric_Types_Draw02_Draw02EndClosure_cs
{
    static Numeric_Types_Draw02_Draw02EndClosure_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02EndClosure_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02EndClosure_cs"));
}
