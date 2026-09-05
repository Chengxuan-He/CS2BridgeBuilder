using System.Buffers.Binary;
using Colossal.AssetPipeline.Native;

if (args.Length == 4 && string.Equals(args[0], "--compare", StringComparison.Ordinal))
{
    var source = GeometryFile.Read(File.ReadAllBytes(args[1])).Meshes.Single();
    var derived = GeometryFile.Read(File.ReadAllBytes(args[2])).Meshes.Single();
    if (!float.TryParse(
            args[3], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var extra))
        throw new ArgumentException($"Invalid widening '{args[3]}'.");
    GeometryComparison.Report(source, derived, extra);
    return 0;
}

if (args.Length == 5 && string.Equals(args[0], "--section", StringComparison.Ordinal))
{
    var full = GeometryFile.Read(File.ReadAllBytes(args[1])).Meshes.Single();
    var lod1 = GeometryFile.Read(File.ReadAllBytes(args[2])).Meshes.Single();
    var lod2 = GeometryFile.Read(File.ReadAllBytes(args[3])).Meshes.Single();
    var fullCoefficients = SectionCoefficients.FromPrototype(full);
    // The full-detail archetype decides once. LOD connector bodies inherit the same stretch; LOD2's
    // welded-piece reconciliation below prevents a nearby side arch from inheriting only part of it.
    var lod1Coefficients = SectionCoefficients.FromNearestPrototype(lod1, full, fullCoefficients, false);
    var lod2Coefficients = SectionCoefficients.FromNearestPrototype(lod2, full, fullCoefficients, true);
    PortalCoefficients.Report("section full", fullCoefficients);
    PortalCoefficients.Report("section LOD1", lod1Coefficients);
    PortalCoefficients.Report("section LOD2", lod2Coefficients);
    SectionCoefficients.WriteSource(
        args[4],
        ("TrussArchBridge03Net Mesh", fullCoefficients),
        ("TrussArchBridge03Net_LOD1 Mesh", lod1Coefficients),
        ("TrussArchBridge03Net_LOD2 Mesh", lod2Coefficients));
    return 0;
}

if (args.Length is not (1 or 3 or 4))
{
    Console.Error.WriteLine(
        "Usage: GeometryMetaprogram <geometry> [<lod1 geometry> <lod2 geometry> [output.cs]]\n"
        + "       GeometryMetaprogram --compare <source geometry> <derived geometry> <extra>\n"
        + "       GeometryMetaprogram --section <full> <lod1> <lod2> <output.cs>");
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
        + $"{pieces.Count} welded piece(s), x "
        + $"{mesh.Positions.Min(point => point.X):0.000000}.."
        + $"{mesh.Positions.Max(point => point.X):0.000000}");
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

internal static class GeometryComparison
{
    private const float Epsilon = 0.001f;

    internal static void Report(MeshData source, MeshData derived, float extra)
    {
        if (source.Positions.Length != derived.Positions.Length)
            throw new InvalidDataException(
                $"Vertex counts differ: source {source.Positions.Length}, derived {derived.Positions.Length}.");

        var shift = extra * 0.5f;
        var labels = Pieces.LabelsOf(source.Positions, source.Indices);
        var pieces = Pieces.Of(source.Positions, source.Indices);
        var kinds = new Dictionary<int, string>();
        var maxY = 0f;
        var maxZ = 0f;
        var maxXError = 0f;
        for (var index = 0; index < source.Positions.Length; index++)
        {
            var before = source.Positions[index];
            var after = derived.Positions[index];
            maxY = Math.Max(maxY, Math.Abs(after.Y - before.Y));
            maxZ = Math.Max(maxZ, Math.Abs(after.Z - before.Z));

            var rigid = before.X > 0f ? before.X + shift : before.X < 0f ? before.X - shift : before.X;
            var displacement = after.X - before.X;
            var kind = Math.Abs(after.X - rigid) <= Epsilon
                ? "rigid"
                : Math.Abs(displacement) <= Epsilon
                    ? "fixed"
                    : "affine";
            var id = labels[index];
            if (kinds.TryGetValue(id, out var prior) && !string.Equals(prior, kind, StringComparison.Ordinal))
                kinds[id] = "mixed";
            else if (!kinds.ContainsKey(id))
                kinds[id] = kind;

            maxXError = Math.Max(maxXError, Math.Abs(displacement));
        }

        Console.WriteLine(
            $"vertices {source.Positions.Length}; pieces {pieces.Count}; "
            + $"max |dy| {maxY:0.000000}; max |dz| {maxZ:0.000000}; max |dx| {maxXError:0.000000}");
        foreach (var group in kinds.GroupBy(pair => pair.Value).OrderBy(group => group.Key))
            Console.WriteLine($"{group.Key}: {group.Count()} piece(s)");

        foreach (var piece in pieces.Where(piece => kinds[piece.Id] != "rigid").OrderBy(piece => piece.Back).ThenBy(piece => piece.Low))
        {
            var movedPoints = Enumerable.Range(0, labels.Length)
                .Where(index => labels[index] == piece.Id)
                .Select(index => derived.Positions[index])
                .ToArray();
            var coefficients = Enumerable.Range(0, labels.Length)
                .Where(index => labels[index] == piece.Id)
                .Select(index => shift == 0f
                    ? 0f
                    : (derived.Positions[index].X - source.Positions[index].X) / shift)
                .ToArray();
            Console.WriteLine(
                $"{kinds[piece.Id],6} {piece.Id,4}: x {piece.Left,9:0.0000}..{piece.Right,9:0.0000}, "
                + $"derived {movedPoints.Min(point => point.X),9:0.0000}.."
                + $"{movedPoints.Max(point => point.X),9:0.0000}, "
                + $"coefficient {coefficients.Min(),7:0.0000}..{coefficients.Max(),7:0.0000}, "
                + $"y {piece.Low,9:0.0000}..{piece.High,9:0.0000}, "
                + $"z {piece.Back,9:0.0000}..{piece.Front,9:0.0000}, vertices {piece.Vertices,5}");
        }

        foreach (var joint in pieces.Where(piece =>
                     Math.Min(Math.Abs(piece.Left), Math.Abs(piece.Right)) >= 4.60f
                     && Math.Max(Math.Abs(piece.Left), Math.Abs(piece.Right)) <= 7.31f
                     && piece.Low >= 6.0f
                     && piece.Right - piece.Left >= 0.60f
                     && piece.Right - piece.Left <= 1.60f
                     && piece.High - piece.Low <= 2.50f
                     && piece.Front - piece.Back <= 2.50f))
        {
            var neighbours = pieces
                .Where(piece => piece.Id != joint.Id)
                .Select(piece => (Piece: piece, Distance: BoxDistance(joint, piece)))
                .OrderBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.Piece.Id)
                .Take(8)
                .ToArray();
            Console.WriteLine(
                $"joint {joint.Id}: nearest "
                + string.Join(", ", neighbours.Select(candidate =>
                    $"{candidate.Piece.Id}/{kinds[candidate.Piece.Id]}/"
                    + $"{candidate.Piece.Vertices}v/{candidate.Distance:0.0000}m")));
        }
    }

    private static float BoxDistance(Piece one, Piece two)
    {
        static float Gap(float firstLow, float firstHigh, float secondLow, float secondHigh) =>
            firstHigh < secondLow ? secondLow - firstHigh
            : secondHigh < firstLow ? firstLow - secondHigh
            : 0f;
        var x = Gap(one.Left, one.Right, two.Left, two.Right);
        var y = Gap(one.Low, one.High, two.Low, two.High);
        var z = Gap(one.Back, one.Front, two.Back, two.Front);
        return MathF.Sqrt(x * x + y * y + z * z);
    }
}

internal static class SectionCoefficients
{
    private const float Epsilon = 0.001f;

    internal static float[] FromPrototype(MeshData mesh)
    {
        var pieces = Pieces.Of(mesh.Positions, mesh.Indices).ToArray();
        var labels = Pieces.LabelsOf(mesh.Positions, mesh.Indices);
        var parent = Enumerable.Range(0, pieces.Length).ToArray();
        int Root(int item)
        {
            while (parent[item] != item)
            {
                parent[item] = parent[parent[item]];
                item = parent[item];
            }
            return item;
        }
        void Join(int one, int two)
        {
            var first = Root(one);
            var second = Root(two);
            if (first != second) parent[first] = second;
        }

        var seeds = pieces.Select(CrossesCentre).ToArray();
        // The TrussArchBridge03 deck base is the unique x=0-crossing island that runs continuously
        // along the complete archetype section. Its contract is x' = x + sign(x) * delta. Earlier
        // code classified every crossing island whose low point was below y=0 as base material; that
        // also captured the transverse truss below the deck and split it instead of widening it.
        // Select the one complete-length crossing island here, offline, and emit only exact vertex
        // coefficients to runtime.
        var deckBase = pieces
            .Where(CrossesCentre)
            .OrderByDescending(piece => piece.Front - piece.Back)
            .First();
        var rigidBase = pieces.Select(piece => piece.Id == deckBase.Id).ToArray();
        Console.WriteLine(
            $"section prototype: deck base island {deckBase.Id}, x "
            + $"{deckBase.Left:0.0000}..{deckBase.Right:0.0000}, y "
            + $"{deckBase.Low:0.0000}..{deckBase.High:0.0000}, z "
            + $"{deckBase.Back:0.0000}..{deckBase.Front:0.0000}, "
            + $"vertices {deckBase.Vertices}");
        // Only a laterally led island can join other islands into one transverse member. A
        // longitudinal deck strip may itself cross x=0, but using it as a connector merges every
        // floor beam along the 128 m span into one false part and gives all of them the end portal's
        // denominator. Such a crossing strip is retained below as its own logical part.
        // The near-detail X braces are imported as separate half-members: each half stops at the
        // centre plate instead of containing an x=0 vertex of its own. Classify the *logical* top
        // truss here, offline, by admitting only a centre-approaching diagonal which touches an
        // actual centre-crossing island. This joins both brace halves to that centre-crossing seed,
        // so the whole top truss obeys the x=0 rule. Outer side arches approach neither the centre
        // nor its plates and remain rigid translations.
        var centreBraces = pieces.Select((piece, index) =>
            !rigidBase[index]
            && IsCentreApproachingDiagonal(piece)
            && pieces.Any(seed =>
                seeds[seed.Id]
                && !rigidBase[seed.Id]
                && Touches(piece, seed))).ToArray();
        // The immutable full-detail TrussArchBridge01Net prototype uses several distinct meshes for
        // the compact riveted side joints. Vertex count is not their identity: even mirrored or end
        // variants of the same joint use different counts. Identify the complete family here,
        // offline, from its measured prototype coordinate envelope and compact three-axis extent,
        // then require topology to connect it to a centre-crossing truss below. Only the emitted
        // vertex membership and coefficients enter the runtime assembly.
        var rivetedSideCandidates = pieces.Select(IsRivetedSideJoint).ToArray();
        var rivetedStyles = pieces
            .Where(piece => rivetedSideCandidates[piece.Id])
            .GroupBy(piece => piece.Vertices)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}v={group.Count()}")
            .ToArray();
        var coordinateEnvelopeStyles = pieces
            .Where(IsRivetedCoordinateEnvelope)
            .GroupBy(piece => piece.Vertices)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}v={group.Count()}")
            .ToArray();
        var candidates = pieces.Select((piece, index) =>
            !rigidBase[index]
            && (IsTransverse(piece)
                || centreBraces[index]
                || rivetedSideCandidates[index]
                || (CrossesCentre(piece) && !IsLongitudinal(piece)))).ToArray();
        Console.WriteLine(
            $"section prototype: admitted {centreBraces.Count(value => value)} "
            + "centre-approaching diagonal brace island(s) and "
            + $"{rivetedSideCandidates.Count(value => value)} riveted side-connector candidate(s) "
            + $"({string.Join(", ", rivetedStyles)})");
        Console.WriteLine(
            "section prototype: compact side coordinate envelope contains "
            + string.Join(", ", coordinateEnvelopeStyles));
        for (var one = 0; one < pieces.Length; one++)
        {
            if (!candidates[one]) continue;
            for (var two = one + 1; two < pieces.Length; two++)
            {
                if (!candidates[two]) continue;
                // A diagonal half-brace and its near-detail riveted joint may join only one another
                // or a centre-crossing member. Letting either act as a general proximity connector
                // would pull the nearby side arch into the stretching group even though that arch
                // never crosses x=0.
                var specialOne = centreBraces[one] || rivetedSideCandidates[one];
                var specialTwo = centreBraces[two] || rivetedSideCandidates[two];
                if (specialOne || specialTwo)
                {
                    var permitted =
                        (centreBraces[one] && (seeds[two] || rivetedSideCandidates[two]))
                        || (centreBraces[two] && (seeds[one] || rivetedSideCandidates[one]))
                        || (rivetedSideCandidates[one]
                            && (seeds[two] || centreBraces[two] || rivetedSideCandidates[two]))
                        || (rivetedSideCandidates[two]
                            && (seeds[one] || centreBraces[one] || rivetedSideCandidates[one]));
                    if (!permitted) continue;
                }
                if (Touches(pieces[one], pieces[two])) Join(one, two);
            }
        }

        var transverseGroups = Enumerable.Range(0, pieces.Length)
            .Where(index => candidates[index])
            .GroupBy(Root)
            .Where(group => group.Any(index => seeds[index]))
            .Select(group => group.ToArray())
            .ToArray();
        var longitudinalCrossings = Enumerable.Range(0, pieces.Length)
            .Where(index => seeds[index] && !candidates[index] && !rigidBase[index])
            .Select(index => new[] { index });
        var groups = transverseGroups.Concat(longitudinalCrossings).ToArray();
        var groupForPiece = Enumerable.Repeat(-1, pieces.Length).ToArray();
        var leftReach = new float[groups.Length];
        var rightReach = new float[groups.Length];
        for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            foreach (var pieceIndex in groups[groupIndex])
            {
                groupForPiece[pieceIndex] = groupIndex;
                leftReach[groupIndex] = Math.Max(leftReach[groupIndex], Math.Max(0f, -pieces[pieceIndex].Left));
                rightReach[groupIndex] = Math.Max(rightReach[groupIndex], Math.Max(0f, pieces[pieceIndex].Right));
            }

            var members = groups[groupIndex].Select(index => pieces[index]).ToArray();
            Console.WriteLine(
                $"section group {groupIndex}: {members.Length} island(s), "
                + $"reach {-leftReach[groupIndex]:0.0000}..{rightReach[groupIndex]:0.0000}, "
                + $"y {members.Min(piece => piece.Low):0.0000}..{members.Max(piece => piece.High):0.0000}, "
                + $"z {members.Min(piece => piece.Back):0.0000}..{members.Max(piece => piece.Front):0.0000}");
            foreach (var member in members.OrderBy(piece => piece.Left).ThenBy(piece => piece.Low))
            {
                Console.WriteLine(
                    $"  island {member.Id}: x {member.Left:0.0000}..{member.Right:0.0000}, "
                    + $"y {member.Low:0.0000}..{member.High:0.0000}, "
                    + $"z {member.Back:0.0000}..{member.Front:0.0000}");
            }
        }

        var ungroupedRivetedJoints = pieces
            .Where(piece => rivetedSideCandidates[piece.Id] && groupForPiece[piece.Id] < 0)
            .ToArray();
        Console.WriteLine(
            $"section prototype: {rivetedSideCandidates.Count(value => value) - ungroupedRivetedJoints.Length}/"
            + $"{rivetedSideCandidates.Count(value => value)} coordinate-identified riveted side joints "
            + "belong to a centre-crossing truss group");
        if (ungroupedRivetedJoints.Length != 0)
        {
            Console.Error.WriteLine(
                "section prototype: ungrouped riveted side joints: "
                + string.Join(", ", ungroupedRivetedJoints.Select(piece => piece.Id)));
        }

        var result = new float[mesh.Positions.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var point = mesh.Positions[index];
            var piece = pieces[labels[index]];
            var groupIndex = groupForPiece[piece.Id];
            if (groupIndex < 0)
            {
                if (rigidBase[piece.Id])
                {
                    result[index] = point.X > 0f ? 1f : point.X < 0f ? -1f : 0f;
                    continue;
                }

                var centre = (piece.Left + piece.Right) * 0.5f;
                result[index] = centre > 0f ? 1f : centre < 0f ? -1f : 0f;
                continue;
            }

            if (rivetedSideCandidates[piece.Id])
            {
                // A riveted side joint bridges two different mappings. Its inner edge belongs to
                // the centre-crossing brace and must retain that brace's group coefficient; its
                // outer edge meets a rigidly translated side member and must receive the complete
                // +/-1 coefficient. Interpolate between those two measured prototype edges. Scaling
                // the joint only by the group left its outer edge behind; scaling it about x=0 by
                // its own reach pulled its inner edge away from the brace.
                var inner = Math.Min(Math.Abs(piece.Left), Math.Abs(piece.Right));
                var outer = Math.Max(Math.Abs(piece.Left), Math.Abs(piece.Right));
                var reach = point.X < 0f ? leftReach[groupIndex] : rightReach[groupIndex];
                var direction = point.X < 0f ? -1f : point.X > 0f ? 1f : 0f;
                var innerCoefficient = reach > Epsilon ? inner / reach : 0f;
                var progress = outer - inner > Epsilon
                    ? Math.Clamp((Math.Abs(point.X) - inner) / (outer - inner), 0f, 1f)
                    : 1f;
                result[index] = direction * (innerCoefficient + (1f - innerCoefficient) * progress);
                continue;
            }

            // Every other welded island in one logical transverse truss uses the same prototype
            // span, so the brace and crossbeam remain one continuous assembly.
            result[index] = point.X < 0f && leftReach[groupIndex] > Epsilon
                ? point.X / leftReach[groupIndex]
                : point.X > 0f && rightReach[groupIndex] > Epsilon
                    ? point.X / rightReach[groupIndex]
                    : 0f;
        }

        Console.WriteLine(
            $"section prototype: {groups.Length} logical centre-crossing group(s), "
            + $"{groups.Sum(group => group.Length)} stretching island(s), "
            + $"{pieces.Length - groups.Sum(group => group.Length)} rigid side island(s)");
        var jointOuterCoefficients = pieces
            .Where(piece => rivetedSideCandidates[piece.Id] && groupForPiece[piece.Id] >= 0)
            .SelectMany(piece => Enumerable.Range(0, mesh.Positions.Length)
                .Where(index => labels[index] == piece.Id)
                .Where(index => Math.Abs(
                    Math.Abs(mesh.Positions[index].X)
                    - Math.Max(Math.Abs(piece.Left), Math.Abs(piece.Right))) <= Epsilon)
                .Select(index => Math.Abs(result[index])))
            .ToArray();
        var jointInnerErrors = pieces
            .Where(piece => rivetedSideCandidates[piece.Id] && groupForPiece[piece.Id] >= 0)
            .SelectMany(piece => Enumerable.Range(0, mesh.Positions.Length)
                .Where(index => labels[index] == piece.Id)
                .Where(index => Math.Abs(
                    Math.Abs(mesh.Positions[index].X)
                    - Math.Min(Math.Abs(piece.Left), Math.Abs(piece.Right))) <= Epsilon)
                .Select(index =>
                {
                    var point = mesh.Positions[index];
                    var groupIndex = groupForPiece[piece.Id];
                    var reach = point.X < 0f ? leftReach[groupIndex] : rightReach[groupIndex];
                    var expected = reach > Epsilon ? point.X / reach : 0f;
                    return Math.Abs(result[index] - expected);
                }))
            .ToArray();
        Console.WriteLine(
            $"section prototype: {rivetedSideCandidates.Count(value => value) - ungroupedRivetedJoints.Length} "
            + "stretching riveted side-joint island(s), outer-edge coefficient "
            + $"{jointOuterCoefficients.Min():0.0000}..{jointOuterCoefficients.Max():0.0000}, "
            + $"maximum inner-edge brace mismatch {jointInnerErrors.Max():0.000000}");
        return result;
    }

    internal static float[] FromNearestPrototype(
        MeshData mesh,
        MeshData prototype,
        IReadOnlyList<float> prototypeCoefficients,
        bool reconcileWeldedPieces)
    {
        var tree = new PositionTree(prototype.Positions);
        var result = new float[mesh.Positions.Length];
        var maximum = 0f;
        var total = 0d;
        for (var index = 0; index < result.Length; index++)
        {
            var nearest = tree.Nearest(mesh.Positions[index], out var squaredDistance);
            var coefficient = prototypeCoefficients[nearest];
            if (Math.Abs(Math.Abs(coefficient) - 1f) <= Epsilon)
            {
                result[index] = coefficient;
            }
            else
            {
                var source = prototype.Positions[nearest];
                result[index] = Math.Abs(source.X) > Epsilon
                    ? coefficient * (mesh.Positions[index].X / source.X)
                    : 0f;
            }
            var distance = MathF.Sqrt(squaredDistance);
            maximum = Math.Max(maximum, distance);
            total += distance;
        }
        Console.WriteLine(
            $"section LOD nearest full-detail vertex: average {total / mesh.Positions.Length:0.0000} m, "
            + $"maximum {maximum:0.0000} m");
        if (reconcileWeldedPieces) ReconcileWeldedPieces(mesh, result);
        return result;
    }

    private static void ReconcileWeldedPieces(MeshData mesh, float[] coefficients)
    {
        var labels = Pieces.LabelsOf(mesh.Positions, mesh.Indices);
        var pieces = Pieces.Of(mesh.Positions, mesh.Indices);
        var verticesByPiece = Enumerable.Range(0, labels.Length)
            .GroupBy(index => labels[index])
            .ToDictionary(group => group.Key, group => group.ToArray());
        var madeRigid = 0;
        var madeSpanning = 0;
        foreach (var piece in pieces)
        {
            var vertices = verticesByPiece[piece.Id];
            var rigidVotes = vertices.Count(index =>
                Math.Abs(Math.Abs(coefficients[index]) - 1f) <= Epsilon);
            var spanningVotes = vertices.Length - rigidVotes;
            if (rigidVotes == 0 || spanningVotes == 0) continue;

            // LOD2 welds authored surfaces which are separate in the full-detail archetype. Nearest
            // vertex inheritance can consequently give different transforms to vertices in one
            // triangle. The full-detail map still makes the classification: its votes decide whether
            // this coarse island represents rigid side material or a spanning member. LOD topology
            // only supplies that representation's own endpoint, so a spanning island continues to
            // meet the side material after both have moved by the same half-width delta.
            if (rigidVotes > spanningVotes)
            {
                foreach (var index in vertices)
                {
                    var x = mesh.Positions[index].X;
                    coefficients[index] = x > 0f ? 1f : x < 0f ? -1f : 0f;
                }
                Console.WriteLine(
                    $"section LOD welded island {piece.Id}: x {piece.Left:0.0000}..{piece.Right:0.0000}, "
                    + $"y {piece.Low:0.0000}..{piece.High:0.0000}, "
                    + $"z {piece.Back:0.0000}..{piece.Front:0.0000}, "
                    + $"{rigidVotes} rigid/{spanningVotes} spanning votes -> rigid");
                madeRigid++;
                continue;
            }

            if (piece.Left <= Epsilon && piece.Right >= -Epsilon)
            {
                var leftReach = Math.Max(0f, -piece.Left);
                var rightReach = Math.Max(0f, piece.Right);
                foreach (var index in vertices)
                {
                    var x = mesh.Positions[index].X;
                    coefficients[index] = x < 0f && leftReach > Epsilon
                        ? x / leftReach
                        : x > 0f && rightReach > Epsilon
                            ? x / rightReach
                            : 0f;
                }
                Console.WriteLine(
                    $"section LOD welded island {piece.Id}: x {piece.Left:0.0000}..{piece.Right:0.0000}, "
                    + $"y {piece.Low:0.0000}..{piece.High:0.0000}, "
                    + $"z {piece.Back:0.0000}..{piece.Front:0.0000}, "
                    + $"{rigidVotes} rigid/{spanningVotes} spanning votes -> spanning");
                madeSpanning++;
            }
        }
        Console.WriteLine(
            $"section LOD welded reconciliation: {madeRigid} inherited side island(s) made rigid, "
            + $"{madeSpanning} inherited centre-crossing island(s) made spanning");
    }

    internal static void WriteSource(
        string path, params (string MeshName, IReadOnlyList<float> Coefficients)[] maps)
    {
        var source = new System.Text.StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("// Generated from the TrussArchBridge03Net full-detail archetype and its two LODs.");
        source.AppendLine("using System;");
        source.AppendLine("using System.Collections.Generic;");
        source.AppendLine();
        source.AppendLine("namespace BridgePrefabGenerator.Bridges;");
        source.AppendLine();
        source.AppendLine("internal static class TrussArch03SectionData");
        source.AppendLine("{");
        source.AppendLine("    internal static readonly IReadOnlyDictionary<string, TrussArch03Geometry.PortalMap> Maps =");
        source.AppendLine("        new Dictionary<string, TrussArch03Geometry.PortalMap>(StringComparer.Ordinal)");
        source.AppendLine("        {");
        foreach (var map in maps)
        {
            var encoded = PortalCoefficients.EncodeForSource(map.Coefficients);
            source.AppendLine($"            [\"{map.MeshName}\"] = new TrussArch03Geometry.PortalMap(");
            source.AppendLine($"                {map.Coefficients.Count},");
            source.AppendLine($"                \"{encoded.Membership}\",");
            source.AppendLine($"                \"{encoded.Coefficients}\"),");
        }
        source.AppendLine("        };");
        source.AppendLine("}");
        File.WriteAllText(path, source.ToString(), new System.Text.UTF8Encoding(false));
    }

    private static bool CrossesCentre(Piece piece) =>
        piece.Left <= Epsilon && piece.Right >= -Epsilon;

    private static bool IsRivetedSideJoint(Piece piece)
    {
        if (!IsRivetedCoordinateEnvelope(piece)) return false;
        var lateral = piece.Right - piece.Left;
        var vertical = piece.High - piece.Low;
        var longitudinal = piece.Front - piece.Back;
        return lateral >= 0.60f
            && vertical <= 2.50f
            && longitudinal <= 2.50f;
    }

    private static bool IsRivetedCoordinateEnvelope(Piece piece)
    {
        if (CrossesCentre(piece)) return false;
        var inner = Math.Min(Math.Abs(piece.Left), Math.Abs(piece.Right));
        var outer = Math.Max(Math.Abs(piece.Left), Math.Abs(piece.Right));
        var lateral = piece.Right - piece.Left;
        var longitudinal = piece.Front - piece.Back;
        return inner >= 4.60f
            && outer <= 7.31f
            && piece.Low >= 6.0f
            && lateral <= 1.60f
            && longitudinal <= 2.50f;
    }

    private static bool IsTransverse(Piece piece)
    {
        var lateral = piece.Right - piece.Left;
        var vertical = piece.High - piece.Low;
        var longitudinal = piece.Front - piece.Back;
        // A side-arch diagonal can have nearly equal x/z spans and sits at the outer edge; treating
        // that as a transverse brace contaminates the end-frame reach. A true transverse member is
        // led decisively by x in this archetype. This inference remains offline and is emitted only
        // as exact vertex membership.
        return lateral > 1.1f * longitudinal + Epsilon && lateral + Epsilon >= vertical;
    }

    private static bool IsCentreApproachingDiagonal(Piece piece)
    {
        var lateral = piece.Right - piece.Left;
        var vertical = piece.High - piece.Low;
        var longitudinal = piece.Front - piece.Back;
        var distanceFromCentre = Math.Min(Math.Abs(piece.Left), Math.Abs(piece.Right));
        return !CrossesCentre(piece)
            && longitudinal > lateral + Epsilon
            && lateral + Epsilon >= vertical
            && distanceFromCentre <= lateral * 0.25f + Epsilon;
    }

    private static bool IsLongitudinal(Piece piece)
    {
        var lateral = piece.Right - piece.Left;
        var vertical = piece.High - piece.Low;
        var longitudinal = piece.Front - piece.Back;
        // A small centre plate may be a little longer in z than x and still belong to one transverse
        // truss station. Only the 128 m deck strips are longitudinal connectors; keep those from
        // joining every station into one part. This ratio is an offline archetype inspection rule and
        // is deliberately not emitted into the runtime assembly.
        return longitudinal > 4f * Math.Max(lateral, vertical) + Epsilon;
    }

    private static float Gap(float firstLow, float firstHigh, float secondLow, float secondHigh) =>
        firstHigh < secondLow ? secondLow - firstHigh
        : secondHigh < firstLow ? firstLow - secondHigh
        : 0f;

    private static bool Touches(Piece one, Piece two)
    {
        var oneLateral = one.Right - one.Left;
        var oneVertical = one.High - one.Low;
        var oneLongitudinal = one.Front - one.Back;
        var twoLateral = two.Right - two.Left;
        var twoVertical = two.High - two.Low;
        var twoLongitudinal = two.Front - two.Back;
        var oneJoint = Math.Min(oneVertical, oneLongitudinal);
        var twoJoint = Math.Min(twoVertical, twoLongitudinal);
        var crossSection = Math.Max(Epsilon, Math.Max(oneJoint, twoJoint));
        var lateralGap = Math.Max(crossSection, Math.Max(oneLateral, twoLateral));
        return Gap(one.Left, one.Right, two.Left, two.Right) <= lateralGap + Epsilon
            && Gap(one.Low, one.High, two.Low, two.High) <= crossSection + Epsilon
            // A member's z length must not authorize joining a different station along the bridge.
            // The member cross-section is the only valid importer-gap allowance on this axis.
            && Gap(one.Back, one.Front, two.Back, two.Front) <= crossSection + Epsilon;
    }

    private sealed class PositionTree
    {
        private readonly Position[] _positions;
        private readonly Node? _root;

        internal PositionTree(Position[] positions)
        {
            _positions = positions;
            var indices = Enumerable.Range(0, positions.Length).ToArray();
            _root = Build(indices, 0, indices.Length, 0);
        }

        internal int Nearest(Position point, out float squaredDistance)
        {
            var best = -1;
            squaredDistance = float.MaxValue;
            Search(_root, point, ref best, ref squaredDistance);
            return best;
        }

        private Node? Build(int[] indices, int start, int count, int depth)
        {
            if (count <= 0) return null;
            var axis = depth % 3;
            Array.Sort(indices, start, count, Comparer<int>.Create((one, two) =>
                Coordinate(_positions[one], axis).CompareTo(Coordinate(_positions[two], axis))));
            var middle = start + count / 2;
            return new Node(
                indices[middle], axis,
                Build(indices, start, middle - start, depth + 1),
                Build(indices, middle + 1, start + count - middle - 1, depth + 1));
        }

        private void Search(Node? node, Position point, ref int best, ref float bestDistance)
        {
            if (node == null) return;
            var candidate = _positions[node.Index];
            var distance = Squared(point, candidate);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node.Index;
            }

            var difference = Coordinate(point, node.Axis) - Coordinate(candidate, node.Axis);
            var first = difference < 0f ? node.Left : node.Right;
            var second = difference < 0f ? node.Right : node.Left;
            Search(first, point, ref best, ref bestDistance);
            if (difference * difference < bestDistance) Search(second, point, ref best, ref bestDistance);
        }

        private static float Coordinate(Position point, int axis) =>
            axis == 0 ? point.X : axis == 1 ? point.Y : point.Z;

        private static float Squared(Position one, Position two)
        {
            var x = one.X - two.X;
            var y = one.Y - two.Y;
            var z = one.Z - two.Z;
            return x * x + y * y + z * z;
        }

        private sealed record Node(int Index, int Axis, Node? Left, Node? Right);
    }
}

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

    internal static int[] LabelsOf(Position[] vertices, int[] indices)
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
        var encoded = EncodeForSource(coefficients);
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
            var encoded = EncodeForSource(map.Coefficients);
            source.AppendLine($"            [\"{map.MeshName}\"] = new TrussArch01Geometry.PortalMap(");
            source.AppendLine($"                {map.Coefficients.Count},");
            source.AppendLine($"                \"{encoded.Membership}\",");
            source.AppendLine($"                \"{encoded.Coefficients}\"),");
        }
        source.AppendLine("        };");
        source.AppendLine("}");
        File.WriteAllText(path, source.ToString(), new System.Text.UTF8Encoding(false));
    }

    internal static (string Membership, string Coefficients) EncodeForSource(
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
