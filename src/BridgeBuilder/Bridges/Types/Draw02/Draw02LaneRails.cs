using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Exact original orange separator membership. Runtime translates recorded faces;
// original UVs, material slots and packed bone/frame channels remain native.
internal static class Draw02LaneRails
{
    internal static int SourceLod(int lod) => Math.Min(lod, 1);
    internal static int Part(int lod, bool low, bool carOnLeft, bool positiveLeaf)
        => SourceLod(lod) * 6 + (low ? 0 : carOnLeft ? 4 : 2) + (positiveLeaf ? 1 : 0);

    internal static Vector2 Extent(int part)
    {
        var values = Numeric_Types_Draw02_Draw02LaneRails_cs.Values.F32;
        return new Vector2(values[part * 3 + 1], values[part * 3 + 2]);
    }

    internal static float Anchor(int part)
        => Numeric_Types_Draw02_Draw02LaneRails_cs.Values.F32[part * 3];

    internal static MovableStructureModel.Plan Create(int part, float targetX,
        Vector3[] source, int[] indices)
        => Create(part, _ => targetX, source, indices);

    internal static MovableStructureModel.Plan Create(int part, Func<float, float> targetX,
        Vector3[] source, int[] indices)
    {
        var data = Numeric_Types_Draw02_Draw02LaneRails_cs.Values;
        int header = part * 4;
        if (source.Length != data.I32[header] || indices.Length / 3 != data.I32[header + 1])
            throw new InvalidOperationException("Draw02 separator source differs from its measured native mesh.");
        var positions = (Vector3[])source.Clone();
        var removed = new bool[data.I32[header + 1]];
        Array.Fill(removed, true);
        var used = new bool[source.Length];
        int start = data.I32[header + 2], count = data.I32[header + 3];
        float anchor = data.F32[part * 3];
        var vertices = new List<int>();
        var targets = new List<float>();
        for (int i = 0; i < count; i++)
        {
            int face = data.I32[start + i];
            removed[face] = false;
            for (int corner = 0; corner < 3; corner++)
            {
                int vertex = indices[face * 3 + corner];
                if (used[vertex]) continue;
                used[vertex] = true;
                vertices.Add(vertex);
                targets.Add(targetX(source[vertex].z));
            }
        }
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.TargetTranslation,
            new[] { source, positions }, new[] { targets.ToArray() }, new[] { anchor },
            count: vertices.Count, indices: new[] { vertices.ToArray() });
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { removed });
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02LaneRails_cs
internal static class Numeric_Types_Draw02_Draw02LaneRails_cs
{
    static Numeric_Types_Draw02_Draw02LaneRails_cs() { }
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02LaneRails_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02LaneRails_cs"));
}
