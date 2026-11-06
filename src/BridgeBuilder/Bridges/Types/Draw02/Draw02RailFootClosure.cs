using System;
using System.Collections.Generic;
using UnityEngine;
using Kind = BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind;

namespace BridgeBuilder.Bridges;
// Original wall/slab contacts follow the same authored high-rail inner shell.
internal static class Draw02RailFootClosure
{
    internal static void Apply(int lod, MovableStructureModel.Plan plan, MovableSelectedRoadLayout layout, Vector3[] positions, bool[] removed, HashSet<int> replaced, List<KeyValuePair<int, Matrix4x4>> frames, List<MovableStructureModel.EdgeVertex> edges, List<MovableStructureModel.Fragment> fragments, List<float> constructedX)
    {
        if (lod == Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[0] || layout.Lanes == null || layout.Lanes.Length == Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[1])
            return;
        if (plan.InteriorVertices.Length != Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[2])
            throw new InvalidOperationException("Draw02 measured closure expects its original edge-only shell recipe.");
        var data = RailFootClosureMeasurements.Values.I32;
        var split = RailFootContactSplitMeasurements.Values;
        var cache = new Dictionary<float, MovableRoadLaneLayout.Boundary[]>();
        bool Contact(float z, int side, out float x)
        {
            if (!cache.TryGetValue(z, out var boundaries))
                cache.Add(z, boundaries = layout.LaneBoundariesAt(z));
            bool found = false;
            x = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[3];
            foreach (var boundary in boundaries)
            {
                bool valid = side > Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[4] ? (boundary.Left & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[5]) && (boundary.Right & Kind.Car) == (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[6]) : (boundary.Right & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[7]) && (boundary.Left & Kind.Car) == (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[8]);
                if (valid && (!found || (side > Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[9] ? boundary.X > x : boundary.X < x)))
                {
                    found = true;
                    x = boundary.X;
                }
            }

            return found;
        }

        var requests = new List<(int Program, float A, float B, float C)>();
        var positionWrites = new List<(int Vertex, int Row)>();
        var constructedWrites = new List<(int Index, int Row)>();
        var frameRows = new List<(int Vertex, int Row, float Lower)>();
        int Target(Vector3 source, int side, float unchangedX)
        {
            if (!Contact(source.z, side, out float boundary))
            {
                requests.Add((StructurePrograms.CopyScalar, unchangedX, default, default));
                return requests.Count - 1;
            }
            int part = Draw02LaneRails.Part(lod, false, side > Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[10], source.z > Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[11]);
            requests.Add((StructurePrograms.TargetScalar, source.x, boundary, Draw02LaneRails.Anchor(part)));
            return requests.Count - 1;
        }

        for (int side = -Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[13]; side <= Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[14]; side += Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[15])
        {
            int record = lod * Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[16] + (side > Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[17] ? Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[18] : Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[19]), h = record * Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[20];
            if (plan.SourcePositions.Length != data[h] || removed.Length != data[h + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[21]])
                throw new InvalidOperationException("Draw02 rail closure requires its measured original main mesh.");
            var active = new HashSet<int>();
            for (int group = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[22]; group <= Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[23]; group += Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[24])
                for (int i = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[25]; i < data[h + group + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[26]]; i++)
                {
                    int vertex = data[data[h + group] + i];
                    var source = plan.SourcePositions[vertex];
                    if (!Contact(source.z, side, out _))
                        continue;
                    positionWrites.Add((vertex, Target(source, side, positions[vertex].x)));
                    active.Add(vertex);
                }

            if (active.Count == Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[27])
                continue;
            int sh = record * Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[28];
            var local = new int[split.I32[sh + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[29]]];
            var sourcePoints = new Vector3[local.Length]; var originalPoints = new Vector3[local.Length];
            var edgeA = new int[local.Length]; var edgeB = new int[local.Length]; var edgeOutput = new int[local.Length];
            var fractions = new float[local.Length];
            for (int i = 0; i < local.Length; i++)
            {
                int row = split.I32[sh] + i * Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[31];
                edgeA[i] = split.I32[row]; edgeB[i] = split.I32[row + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[32]];
                fractions[i] = split.F32[split.I32[row + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[33]]]; edgeOutput[i] = i;
            }
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.EdgeXyz, new[] { plan.SourcePositions, sourcePoints },
                new[] { fractions }, Array.Empty<float>(), count: local.Length, indices: new[] { edgeA, edgeB, edgeOutput });
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.EdgeXyz, new[] { plan.Positions, originalPoints },
                new[] { fractions }, Array.Empty<float>(), count: local.Length, indices: new[] { edgeA, edgeB, edgeOutput });
            for (int i = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[30]; i < local.Length; i++)
            {
                int row = split.I32[sh] + i * Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[31];
                int a = split.I32[row], b = split.I32[row + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[32]];
                float t = split.F32[split.I32[row + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[33]]];
                var source = sourcePoints[i];
                int target = Target(source, side, originalPoints[i].x);
                local[i] = plan.SourcePositions.Length + edges.Count;
                edges.Add(new MovableStructureModel.EdgeVertex(a, b, t));
                constructedWrites.Add((constructedX.Count, target)); constructedX.Add(default);
                if (split.I32[row + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[34]] == Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[35])
                {
                    frameRows.Add((local[i], target, originalPoints[i].x));
                }
            }

            int offset = split.I32[sh + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[36]];
            var framed = new HashSet<int>();
            for (int i = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[37]; i < split.I32[sh + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[38]]; i++)
            {
                int face = split.I32[offset++], count = split.I32[offset++];
                var indices = new int[count];
                for (int k = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[39]; k < count; k++)
                {
                    int id = split.I32[offset++];
                    indices[k] = id < Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[40] ? local[-id - Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[41]] : id;
                    if (id >= Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[42] && plan.SourcePositions[id].y < split.F32[Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[43]] && framed.Add(id))
                        frameRows.Add((id, Target(plan.SourcePositions[id], side, plan.Positions[id].x), plan.Positions[id].x));
                }

                removed[face] = true;
                replaced.Add(face);
                fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[44], face, indices));
            }

            // Original vertical slab faces already carried by the rail have one owner.
            for (int i = Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[45]; i < data[h + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[46]]; i++)
            {
                int face = data[data[h + Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[47]] + i];
                removed[face] = true;
                replaced.Add(face);
            }
        }
        var rows = requests.ToArray();
        var targets = StructureRows.Evaluate(Array.ConvertAll(rows, r => r.Program), Array.ConvertAll(rows, r => r.A),
            Array.ConvertAll(rows, r => r.B), Array.ConvertAll(rows, r => r.C));
        foreach (var write in positionWrites) positions[write.Vertex].x = targets[write.Row];
        foreach (var write in constructedWrites) constructedX[write.Index] = targets[write.Row];
        var frameData = frameRows.ToArray();
        float span = split.F32[Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.I32[12]] - Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values.F32[0];
        var shears = StructureRows.Evaluate(Array.ConvertAll(frameData, _ => StructurePrograms.ShearScalar),
            Array.ConvertAll(frameData, r => targets[r.Row]), Array.ConvertAll(frameData, r => r.Lower), Array.ConvertAll(frameData, _ => span));
        for (int i = 0; i < frameData.Length; i++)
        {
            var frame = Matrix4x4.identity; frame.m01 = shears[i];
            frames.Add(new KeyValuePair<int, Matrix4x4>(frameData[i].Vertex, frame));
        }
    }
}

internal static class RailFootClosureMeasurements
{
    static RailFootClosureMeasurements()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.RailFootClosureMeasurements.Values", () => HardcodedNumbers.Load("Types_Draw02_RailFootClosureMeasurements"));
}

internal static class RailFootContactSplitMeasurements
{
    static RailFootContactSplitMeasurements()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.RailFootContactSplitMeasurements.Values", () => HardcodedNumbers.Load("Types_Draw02_RailFootContactSplitMeasurements"));
}

// Embedded numerical arrays: Types_Draw02_Draw02RailFootClosure_cs
internal static class Numeric_Types_Draw02_Draw02RailFootClosure_cs
{
    static Numeric_Types_Draw02_Draw02RailFootClosure_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02RailFootClosure_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02RailFootClosure_cs"));
}
