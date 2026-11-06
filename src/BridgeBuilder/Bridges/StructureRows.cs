using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace BridgeBuilder.Bridges;

// Immutable membership scheduling. Repeated indices retain their ordered writes;
// each numerical stage contains disjoint destinations and has no callback.
internal static class StructureRows
{
    private struct ScalarCoordinate
    {
        public float X, Y, Z;
        public ScalarCoordinate(float x, float y, float z) { X=x; Y=y; Z=z; }
    }
    // Prepare homogeneous scalar stages without changing the caller's row order.
    // Numerical programs address independent output rows; the caller scatters
    // those completed values in its original topology order (including repeats).
    internal static float[] Evaluate(int[] entries, params float[][] inputs)
    {
        var output = new float[entries.Length];
        var columns = new float[inputs.Length + 1][];
        Array.Copy(inputs, columns, inputs.Length);
        columns[inputs.Length] = output;
        var groups = new Dictionary<int, List<int>>();
        for (int i = 0; i < entries.Length; i++)
        {
            if (!groups.TryGetValue(entries[i], out var rows)) groups.Add(entries[i], rows = new List<int>());
            rows.Add(i);
        }
        foreach (var group in groups)
            AffineGeometry.Execute(StructurePrograms.Data, group.Key, Array.Empty<ScalarCoordinate[]>(), columns,
                Array.Empty<float>(), count: group.Value.Count, indices: new[] { group.Value.ToArray() });
        return output;
    }
    private static class Slices<T>
    {
        internal static ConditionalWeakTable<T[], Dictionary<(int First, int Count), T[]>> Cache => GenerationParameters.Get<ConditionalWeakTable<T[], Dictionary<(int First, int Count), T[]>>>(typeof(global::BridgeBuilder.Bridges.StructureRows.Slices<T>).FullName + ":Cache", () => new());
    }
    internal static T[] Slice<T>(T[] source, int first, int count)
    {
        var slices = Slices<T>.Cache.GetValue(source, _ => new Dictionary<(int, int), T[]>());
        lock (slices)
        {
            if (!slices.TryGetValue((first, count), out var result))
            {
                result = new T[count];
                Array.Copy(source, first, result, 0, count);
                slices.Add((first, count), result);
            }
            return result;
        }
    }
    private static ConditionalWeakTable<int[], int[][]> Cache => GenerationParameters.Get<ConditionalWeakTable<int[], int[][]>>("BridgeBuilder.Bridges.StructureRows.Cache", () => new());
    internal static int[][] Stages(int[] indices) => Cache.GetValue(indices, data =>
    {
        var counts = new Dictionary<int, int>();
        var stages = new List<List<int>>();
        foreach (int index in data)
        {
            int stage = counts.TryGetValue(index, out int count) ? count : 0;
            counts[index] = stage + 1;
            while (stages.Count <= stage) stages.Add(new List<int>());
            stages[stage].Add(index);
        }
        var result = new int[stages.Count][];
        for (int i = 0; i < result.Length; i++) result[i] = stages[i].ToArray();
        return result;
    });
}
