using System;
using System.IO;

namespace BridgeBuilder.Bridges;

// Source-linked integration input. The coordinator can move this reader to the
// common native-chart path; it never creates assets or discovers chart membership.
internal sealed class Lift03NativeChartReader
{
    internal readonly struct Chart
    {
        private readonly int[] _indices;
        private readonly int _triangles, _triangleCount, _boundary, _boundaryCount;
        internal readonly bool ExactRectangle, EdgesPaired;
        internal ReadOnlySpan<int> TriangleIds => new(_indices, _triangles, _triangleCount);
        internal ReadOnlySpan<int> BoundaryVertexPairs => new(_indices, _boundary, _boundaryCount * 2);

        internal Chart(int[] indices, int triangles, int triangleCount, int boundary, int boundaryCount, int flags)
        {
            _indices = indices;
            _triangles = triangles; _triangleCount = triangleCount;
            _boundary = boundary; _boundaryCount = boundaryCount;
            ExactRectangle = (flags & 1) != 0; EdgesPaired = (flags & 2) != 0;
        }
    }

    private readonly HardcodedNumbers _data;
    internal readonly Chart[] Charts;
    internal int VertexCount => _data.I32[1];
    internal int TriangleCount => _data.I32[2];
    internal ReadOnlySpan<float> SourceUv => _data.F32;
    internal ReadOnlySpan<int> TriangleVertexIds => new(_data.I32, 4, TriangleCount * 3);

    internal Lift03NativeChartReader(HardcodedNumbers data)
    {
        _data = data;
        if (data.I32.Length < 4 || data.I32[0] != 1 || data.I32[1] < 0 || data.I32[2] < 0 || data.I32[3] < 0 ||
            data.U32.Length != 0 || data.I64.Length != 0 || data.U64.Length != 0 || data.F64.Length != 0 ||
            (long)data.I32[1] * 2 != data.F32.Length || 4L + (long)data.I32[2] * 3 > data.I32.Length)
            throw new InvalidDataException("Invalid Lift03 native chart arrays.");
        foreach (float value in data.F32)
            if (!float.IsFinite(value)) throw new InvalidDataException("Nonfinite Lift03 native chart UV.");
        foreach (int vertex in TriangleVertexIds)
            if ((uint)vertex >= VertexCount) throw new InvalidDataException("Lift03 chart vertex out of range.");
        Charts = new Chart[data.I32[3]];
        int at = checked(4 + TriangleCount * 3);
        for (int i = 0; i < Charts.Length; i++)
        {
            if (at > data.I32.Length - 3) throw new InvalidDataException("Truncated Lift03 native chart.");
            int triangles = data.I32[at++], edges = data.I32[at++], flags = data.I32[at++];
            if (triangles < 0 || edges < 0 || flags < 0 || flags > 3 ||
                at + (long)triangles + (long)edges * 2 > data.I32.Length)
                throw new InvalidDataException("Invalid Lift03 native chart lengths.");
            int boundary = at + triangles;
            for (int j = at; j < boundary; j++)
                if ((uint)data.I32[j] >= TriangleCount) throw new InvalidDataException("Lift03 chart triangle out of range.");
            int end = checked(boundary + edges * 2);
            for (int j = boundary; j < end; j++)
                if ((uint)data.I32[j] >= VertexCount) throw new InvalidDataException("Lift03 chart boundary out of range.");
            Charts[i] = new Chart(data.I32, at, triangles, boundary, edges, flags);
            at = end;
        }
        if (at != data.I32.Length) throw new InvalidDataException("Trailing Lift03 native chart data.");
    }
}
