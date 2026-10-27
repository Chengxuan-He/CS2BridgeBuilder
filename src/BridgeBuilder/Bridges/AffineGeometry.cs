using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BridgeBuilder.Bridges;

/// <summary>
/// CPU execution of sparse affine matrix rows. Keep multiply and add separate, in
/// authored order: changing to fused operations changes measured float results.
/// Callers own disjoint output ranges and snapshot engine objects before scheduling.
/// </summary>
internal static class AffineGeometry
{
    [ThreadStatic] private static bool _worker;
    internal static void Groups(int count, Action<int> body)
    {
        if (_worker || count <= 1 || Environment.ProcessorCount <= 1)
        {
            for (int i = 0; i < count; i++) body(i);
            return;
        }
        Parallel.For(0, count, new ParallelOptions
        { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var previous = _worker;
            _worker = true;
            try { body(i); }
            finally { _worker = previous; }
        });
    }
    private static class ScatterCache<T>
    {
        internal static readonly ConditionalWeakTable<T[], int[]> Rows = new();
    }
    private static readonly ConditionalWeakTable<int[], KeyValuePair<int, int>[]> IndexCounts = new();
    internal static void Indices(int[] indices, Action<int> body)
    {
        var groups = IndexCounts.GetValue(indices, data =>
        {
            var counts = new Dictionary<int, int>();
            foreach (int index in data)
                counts[index] = counts.TryGetValue(index, out int count) ? count + 1 : 1;
            var result = new KeyValuePair<int, int>[counts.Count];
            ((ICollection<KeyValuePair<int, int>>)counts).CopyTo(result, 0);
            return result;
        });
        For(0, groups.Length, i =>
        {
            for (int repeat = 0; repeat < groups[i].Value; repeat++) body(groups[i].Key);
        });
    }

    // Measured tables are immutable. Preserve sequential last-writer semantics
    // for repeated indices; concurrent workers never write the same vertex.
    internal static void Scatter<T>(T[] rows, Func<T, int> destination, Action<T> body)
    {
        var selected = ScatterCache<T>.Rows.GetValue(rows, data =>
        {
            var last = new Dictionary<int, int>();
            for (int i = 0; i < data.Length; i++) last[destination(data[i])] = i;
            var entries = new int[last.Count];
            last.Values.CopyTo(entries, 0);
            Array.Sort(entries);
            return entries;
        });
        For(0, selected.Length, i => body(rows[selected[i]]));
    }
    internal static float AddScaled(float origin, float coefficient, float parameter)
        => origin + coefficient * parameter;

    internal static float Translate(float origin, float translation) => origin + translation;
    internal static float Scale(float coordinate, float scale) => coordinate * scale;

    // One row of [C | origin] multiplied by [p0 p1 p2 1]^T. The same
    // representation applies independently to X, Y and Z; no axis is forced fixed.
    internal readonly struct Row
    {
        internal readonly float Origin, C0, C1, C2;
        private readonly byte _terms;
        private Row(float origin, float c0, byte terms)
        { Origin = origin; C0 = c0; C1 = C2 = 0f; _terms = terms; }
        internal Row(float origin, float c0, float c1, float c2)
        { Origin = origin; C0 = c0; C1 = c1; C2 = c2; _terms = 3; }
        internal static Row Constant(float value) => new(value, 0f, 0);
        internal static Row Affine(float origin, float coefficient) => new(origin, coefficient, 1);
        internal static Row Linear(float coefficient) => new(0f, coefficient, 4);
        internal float Evaluate(float p0, float p1, float p2)
            => _terms == 0 ? Origin : _terms == 1 ? AddScaled(Origin, C0, p0) :
                _terms == 4 ? Scale(C0, p0) : ((Origin + C0 * p0) + C1 * p1) + C2 * p2;
    }

    // Homogeneous parameter matrix: [X; Y; Z] * [p0 p1 p2 1]^T.
    // Sparse rows omit zero operations so unchanged axes retain their float bits.
    internal readonly struct Matrix
    {
        private readonly Row _x, _y, _z;
        internal Matrix(Row x, Row y, Row z) { _x = x; _y = y; _z = z; }
        internal void Evaluate(float p0, float p1, float p2, out float x, out float y, out float z)
        {
            x = _x.Evaluate(p0, p1, p2);
            y = _y.Evaluate(p0, p1, p2);
            z = _z.Evaluate(p0, p1, p2);
        }
    }

    internal static void For(int from, int to, Action<int> body)
    {
        const int batchSize = 2048;
        int count = to - from;
        if (_worker || count <= batchSize || Environment.ProcessorCount <= 1)
        {
            for (int i = from; i < to; i++) body(i);
            return;
        }
        // Schedule contiguous batches, never one task per vertex. No engine API
        // calls or shared counters belong inside this numerical kernel.
        int batches = (count + batchSize - 1) / batchSize;
        Groups(batches, batch =>
        {
            int first = from + batch * batchSize;
            int last = Math.Min(to, first + batchSize);
            for (int i = first; i < last; i++) body(i);
        });
    }
}
