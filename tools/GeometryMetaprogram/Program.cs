using System.Buffers.Binary;
using Colossal.AssetPipeline.Native;

if (args.Length is not (1 or 3 or 4))
{
    Console.Error.WriteLine(
        "Usage: GeometryMetaprogram <geometry> [<lod1 geometry> <lod2 geometry> [output.cs]]");
    return 2;
}

var geometry = GeometryFile.Read(File.ReadAllBytes(args[0]));
Console.WriteLine($"{Path.GetFileName(args[0])}: {geometry.Meshes.Count} mesh(es)");
for (var meshIndex = 0; meshIndex < geometry.Meshes.Count; meshIndex++)
{
    var mesh = geometry.Meshes[meshIndex];
    var pieces = Pieces.Of(mesh.Positions, mesh.Indices);
    Console.WriteLine(
        $"mesh {meshIndex}: {mesh.Positions.Length} vertices, {mesh.Indices.Length} indices, "
        + $"{pieces.Count} welded piece(s)");
    foreach (var piece in pieces.OrderBy(piece => piece.Left))
    {
        Console.WriteLine(
            $"  {piece.Id,4}: x {piece.Left,9:0.0000}..{piece.Right,9:0.0000}, "
            + $"y {piece.Low,9:0.0000}..{piece.High,9:0.0000}, "
            + $"z {piece.Back,9:0.0000}..{piece.Front,9:0.0000}, "
            + $"vertices {piece.Vertices,5}");
    }
}
if (args.Length >= 3)
{
    var full = geometry.Meshes.Single();
    var lod1 = GeometryFile.Read(File.ReadAllBytes(args[1])).Meshes.Single();
    var lod2 = GeometryFile.Read(File.ReadAllBytes(args[2])).Meshes.Single();
    var fullCoefficients = PortalCoefficients.FromTopology(full);
    var lod1Coefficients = PortalCoefficients.FromTopology(lod1);
    var lod2Coefficients = PortalCoefficients.FromNearestPrototype(lod2, full, fullCoefficients);
    PortalCoefficients.Report("full", fullCoefficients);
    PortalCoefficients.Report("LOD1", lod1Coefficients);
    PortalCoefficients.Report("LOD2", lod2Coefficients);
    PortalCoefficients.Emit("Full", fullCoefficients);
    PortalCoefficients.Emit("Lod1", lod1Coefficients);
    PortalCoefficients.Emit("Lod2", lod2Coefficients);
    if (args.Length == 4)
    {
        PortalCoefficients.WriteSource(
            args[3],
            ("TrussArchBridge01NetPillar Mesh", fullCoefficients),
            ("TrussArchBridge01NetPillar_LOD1 Mesh", lod1Coefficients),
            ("TrussArchBridge01NetPillar_LOD2 Mesh", lod2Coefficients));
    }
}
return 0;

internal sealed record MeshData(Position[] Positions, int[] Indices);
internal readonly record struct Position(float X, float Y, float Z);

internal sealed class GeometryFile
{
    private const uint Zstd = 1;
    private const uint Meshopt = 2;
    private const int AttributeCount = 14;
    private const int HeaderSize = 88;
    private const int SubMeshSize = 40;

    internal required IReadOnlyList<MeshData> Meshes { get; init; }

    internal static GeometryFile Read(byte[] bytes)
    {
        var offset = 0;
        var version = ReadUInt16(bytes, ref offset);
        if (version != 1) throw new InvalidDataException($"Unsupported geometry version {version}.");
        var meshCount = ReadInt32(bytes, ref offset);
        var fileFlags = ReadUInt32(bytes, ref offset);
        var headers = new MeshHeader[meshCount];
        for (var mesh = 0; mesh < meshCount; mesh++)
        {
            var start = offset;
            var formats = ReadUInt64(bytes, ref offset);
            var dimensions = ReadUInt32(bytes, ref offset);
            var attributes = new int[AttributeCount];
            for (var attribute = 0; attribute < AttributeCount; attribute++)
                attributes[attribute] = ReadInt32(bytes, ref offset);
            var indexBytes = ReadInt32(bytes, ref offset);
            var meshFlags = ReadUInt32(bytes, ref offset);
            var vertices = ReadInt32(bytes, ref offset);
            var indices = ReadInt32(bytes, ref offset);
            var subMeshes = ReadInt32(bytes, ref offset);
            if (offset - start != HeaderSize) throw new InvalidDataException("Geometry header size changed.");
            headers[mesh] = new MeshHeader(
                formats, dimensions, attributes, indexBytes, meshFlags, vertices, indices, subMeshes);
        }

        foreach (var header in headers) offset += checked(header.SubMeshes * SubMeshSize);

        var decodedIndices = new int[meshCount][];
        for (var mesh = 0; mesh < meshCount; mesh++)
        {
            var header = headers[mesh];
            var block = Slice(bytes, ref offset, header.IndexBytes);
            var compressed = (fileFlags & Zstd) != 0
                ? CompressionUtilities.Decompress(block, CompressionFormat.ZSTD)
                : block;
            var indexSize = header.VertexCount > ushort.MaxValue ? 4 : 2;
            var raw = (fileFlags & Meshopt) != 0
                ? DecodeMeshoptIndices(compressed, header.IndexCount, indexSize)
                : compressed;
            decodedIndices[mesh] = DecodeIndices(raw, header.IndexCount, indexSize);
        }

        var positions = new Position[meshCount][];
        for (var attribute = 0; attribute < AttributeCount; attribute++)
        {
            for (var mesh = 0; mesh < meshCount; mesh++)
            {
                var header = headers[mesh];
                var byteCount = header.AttributeBytes[attribute];
                if (byteCount == 0) continue;
                var block = Slice(bytes, ref offset, byteCount);
                if (attribute != 0) continue;

                var compressed = (fileFlags & Zstd) != 0
                    ? CompressionUtilities.Decompress(block, CompressionFormat.ZSTD)
                    : block;
                const int stride = sizeof(float) * 3;
                var raw = (fileFlags & Meshopt) != 0
                    ? DecodeMeshoptVertices(compressed, header.VertexCount, stride)
                    : compressed;
                positions[mesh] = DecodePositions(raw, header.VertexCount);
            }
        }

        if (offset > bytes.Length) throw new EndOfStreamException("Geometry blocks overrun the file.");
        var meshes = new MeshData[meshCount];
        for (var mesh = 0; mesh < meshCount; mesh++)
        {
            if (positions[mesh] == null) throw new InvalidDataException($"Mesh {mesh} has no position data.");
            meshes[mesh] = new MeshData(positions[mesh], decodedIndices[mesh]);
        }
        return new GeometryFile { Meshes = meshes };
    }

    private static unsafe byte[] DecodeMeshoptIndices(byte[] source, int count, int stride)
    {
        var result = new byte[checked(count * stride)];
        fixed (byte* sourcePointer = source)
        fixed (byte* resultPointer = result)
        {
            var status = NativeCompression.DecompressMeshoptIndexBuffer(
                (IntPtr)sourcePointer, source.LongLength, count, stride, (IntPtr)resultPointer);
            if (status != 0) throw new InvalidDataException($"meshopt index decode failed ({status}).");
        }
        return result;
    }

    private static unsafe byte[] DecodeMeshoptVertices(byte[] source, int count, int stride)
    {
        var result = new byte[checked(count * stride)];
        fixed (byte* sourcePointer = source)
        fixed (byte* resultPointer = result)
        {
            var status = NativeCompression.DecompressMeshoptVertexAttr(
                (IntPtr)sourcePointer, source.LongLength, count, stride, (IntPtr)resultPointer);
            if (status != 0) throw new InvalidDataException($"meshopt vertex decode failed ({status}).");
        }
        return result;
    }

    private static Position[] DecodePositions(byte[] raw, int count)
    {
        if (raw.Length < checked(count * 12)) throw new InvalidDataException("Position data is truncated.");
        var result = new Position[count];
        for (var vertex = 0; vertex < count; vertex++)
        {
            var start = vertex * 12;
            result[vertex] = new Position(
                BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(start, 4))),
                BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(start + 4, 4))),
                BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(start + 8, 4))));
        }
        return result;
    }

    private static int[] DecodeIndices(byte[] raw, int count, int stride)
    {
        if (raw.Length < checked(count * stride)) throw new InvalidDataException("Index data is truncated.");
        var result = new int[count];
        for (var index = 0; index < count; index++)
            result[index] = stride == 2
                ? BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(index * 2, 2))
                : BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(index * 4, 4));
        return result;
    }

    private static byte[] Slice(byte[] source, ref int offset, int count)
    {
        if (count < 0 || offset < 0 || offset > source.Length - count)
            throw new EndOfStreamException("Geometry block is outside the file.");
        var result = source.AsSpan(offset, count).ToArray();
        offset += count;
        return result;
    }

    private static ushort ReadUInt16(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
        offset += 2;
        return value;
    }

    private static int ReadInt32(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, 4));
        offset += 4;
        return value;
    }

    private static uint ReadUInt32(byte[] bytes, ref int offset) => unchecked((uint)ReadInt32(bytes, ref offset));

    private static ulong ReadUInt64(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, 8));
        offset += 8;
        return value;
    }

    private sealed record MeshHeader(
        ulong AttributeFormats,
        uint AttributeDimensions,
        int[] AttributeBytes,
        int IndexBytes,
        uint Flags,
        int VertexCount,
        int IndexCount,
        int SubMeshes);
}

internal readonly record struct Piece(
    int Id, float Left, float Right, float Low, float High, float Back, float Front, int Vertices);

internal static class Pieces
{
    internal static IReadOnlyList<Piece> Of(Position[] vertices, int[] indices)
    {
        var parent = Enumerable.Range(0, vertices.Length).ToArray();
        int Root(int vertex)
        {
            while (parent[vertex] != vertex)
            {
                parent[vertex] = parent[parent[vertex]];
                vertex = parent[vertex];
            }
            return vertex;
        }
        void Join(int one, int two)
        {
            var first = Root(one);
            var second = Root(two);
            if (first != second) parent[first] = second;
        }

        var welded = new Dictionary<(int X, int Y, int Z), int>();
        for (var index = 0; index < vertices.Length; index++)
        {
            var point = vertices[index];
            var key = (
                (int)MathF.Round(point.X * 1000f),
                (int)MathF.Round(point.Y * 1000f),
                (int)MathF.Round(point.Z * 1000f));
            if (welded.TryGetValue(key, out var first)) Join(index, first);
            else welded[key] = index;
        }
        for (var corner = 0; corner + 2 < indices.Length; corner += 3)
        {
            var a = indices[corner];
            var b = indices[corner + 1];
            var c = indices[corner + 2];
            if ((uint)a >= vertices.Length || (uint)b >= vertices.Length || (uint)c >= vertices.Length)
                continue;
            Join(a, b);
            Join(b, c);
        }

        var groups = Enumerable.Range(0, vertices.Length).GroupBy(Root).ToArray();
        var result = new List<Piece>(groups.Length);
        for (var id = 0; id < groups.Length; id++)
        {
            var points = groups[id].Select(index => vertices[index]).ToArray();
            result.Add(new Piece(
                id,
                points.Min(point => point.X), points.Max(point => point.X),
                points.Min(point => point.Y), points.Max(point => point.Y),
                points.Min(point => point.Z), points.Max(point => point.Z),
                points.Length));
        }
        return result;
    }
}

internal static class PortalCoefficients
{
    private const float CentreEpsilon = 0.001f;

    internal static float[] FromTopology(MeshData mesh)
    {
        var pieces = Pieces.Of(mesh.Positions, mesh.Indices);
        var labels = LabelsOf(mesh.Positions, mesh.Indices);
        var result = new float[mesh.Positions.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var point = mesh.Positions[index];
            var piece = pieces[labels[index]];
            if (piece.Left <= CentreEpsilon && piece.Right >= -CentreEpsilon)
            {
                var width = piece.Right - piece.Left;
                var centre = (piece.Left + piece.Right) * 0.5f;
                result[index] = width > CentreEpsilon
                    ? 2f * (point.X - centre) / width
                    : 0f;
            }
            else
            {
                result[index] = point.X > 0f ? 1f : point.X < 0f ? -1f : 0f;
            }
        }
        return result;
    }

    internal static float[] FromNearestPrototype(
        MeshData mesh, MeshData prototype, IReadOnlyList<float> prototypeCoefficients)
    {
        var result = new float[mesh.Positions.Length];
        var furthest = 0f;
        var average = 0d;
        for (var index = 0; index < mesh.Positions.Length; index++)
        {
            var point = mesh.Positions[index];
            var nearest = -1;
            var distance = float.MaxValue;
            for (var candidate = 0; candidate < prototype.Positions.Length; candidate++)
            {
                var other = prototype.Positions[candidate];
                var x = point.X - other.X;
                var y = point.Y - other.Y;
                var z = point.Z - other.Z;
                var squared = x * x + y * y + z * z;
                if (squared >= distance) continue;
                distance = squared;
                nearest = candidate;
            }
            result[index] = prototypeCoefficients[nearest];
            var metres = MathF.Sqrt(distance);
            furthest = Math.Max(furthest, metres);
            average += metres;
        }
        Console.WriteLine(
            $"LOD2 nearest full-detail vertex: average {average / mesh.Positions.Length:0.0000} m, "
            + $"maximum {furthest:0.0000} m");
        return result;
    }

    internal static void Report(string name, IReadOnlyList<float> coefficients)
    {
        var translated = coefficients.Count(value => Math.Abs(Math.Abs(value) - 1f) <= CentreEpsilon);
        var stretched = coefficients.Count - translated;
        Console.WriteLine(
            $"{name} transform map: {translated} rigid-translation vertices, "
            + $"{stretched} spanning vertices; coefficient "
            + $"{coefficients.Min():0.0000}..{coefficients.Max():0.0000}");
    }

    internal static void Emit(string name, IReadOnlyList<float> coefficients)
    {
        var encoded = Encode(coefficients);
        Console.WriteLine($"{name}VertexCount={coefficients.Count}");
        Console.WriteLine($"{name}Membership={encoded.Membership}");
        Console.WriteLine($"{name}Coefficients={encoded.Coefficients}");
    }

    internal static void WriteSource(
        string path, params (string MeshName, IReadOnlyList<float> Coefficients)[] maps)
    {
        var source = new System.Text.StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("// Generated from the three TrussArchBridge01NetPillar .Geometry assets.");
        source.AppendLine("using System;");
        source.AppendLine("using System.Collections.Generic;");
        source.AppendLine();
        source.AppendLine("namespace BridgePrefabGenerator.Bridges;");
        source.AppendLine();
        source.AppendLine("internal static class TrussArch01PierData");
        source.AppendLine("{");
        source.AppendLine("    internal static readonly IReadOnlyDictionary<string, TrussArch01Geometry.PortalMap> Maps =");
        source.AppendLine("        new Dictionary<string, TrussArch01Geometry.PortalMap>(StringComparer.Ordinal)");
        source.AppendLine("        {");
        foreach (var map in maps)
        {
            var encoded = Encode(map.Coefficients);
            source.AppendLine($"            [\"{map.MeshName}\"] = new TrussArch01Geometry.PortalMap(");
            source.AppendLine($"                {map.Coefficients.Count},");
            source.AppendLine($"                \"{encoded.Membership}\",");
            source.AppendLine($"                \"{encoded.Coefficients}\"),");
        }
        source.AppendLine("        };");
        source.AppendLine("}");
        File.WriteAllText(path, source.ToString(), new System.Text.UTF8Encoding(false));
    }

    private static (string Membership, string Coefficients) Encode(
        IReadOnlyList<float> coefficients)
    {
        var membership = new byte[(coefficients.Count + 7) / 8];
        using var payload = new MemoryStream();
        using var writer = new BinaryWriter(payload);
        for (var index = 0; index < coefficients.Count; index++)
        {
            var coefficient = coefficients[index];
            if (Math.Abs(Math.Abs(coefficient) - 1f) <= CentreEpsilon) continue;
            membership[index >> 3] |= (byte)(1 << (index & 7));
            writer.Write(coefficient);
        }
        writer.Flush();
        return (Convert.ToBase64String(membership), Convert.ToBase64String(payload.ToArray()));
    }

    private static int[] LabelsOf(Position[] vertices, int[] indices)
    {
        var parent = Enumerable.Range(0, vertices.Length).ToArray();
        int Root(int vertex)
        {
            while (parent[vertex] != vertex)
            {
                parent[vertex] = parent[parent[vertex]];
                vertex = parent[vertex];
            }
            return vertex;
        }
        void Join(int one, int two)
        {
            var first = Root(one);
            var second = Root(two);
            if (first != second) parent[first] = second;
        }

        var welded = new Dictionary<(int X, int Y, int Z), int>();
        for (var index = 0; index < vertices.Length; index++)
        {
            var point = vertices[index];
            var key = (
                (int)MathF.Round(point.X * 1000f),
                (int)MathF.Round(point.Y * 1000f),
                (int)MathF.Round(point.Z * 1000f));
            if (welded.TryGetValue(key, out var first)) Join(index, first);
            else welded[key] = index;
        }
        for (var corner = 0; corner + 2 < indices.Length; corner += 3)
        {
            var a = indices[corner];
            var b = indices[corner + 1];
            var c = indices[corner + 2];
            if ((uint)a >= vertices.Length || (uint)b >= vertices.Length || (uint)c >= vertices.Length)
                continue;
            Join(a, b);
            Join(b, c);
        }

        var numbered = new Dictionary<int, int>();
        var labels = new int[vertices.Length];
        for (var index = 0; index < labels.Length; index++)
        {
            var root = Root(index);
            if (!numbered.TryGetValue(root, out var label))
            {
                label = numbered.Count;
                numbered[root] = label;
            }
            labels[index] = label;
        }
        return labels;
    }
}
