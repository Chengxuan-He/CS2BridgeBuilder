using System;
using System.IO;
using System.Security.Cryptography;

namespace BridgeBuilder.Bridges;

// Immutable measured arrays, owned by each generated data class for its assembly lifetime.
// The resource is manager input only; generated native assets never reference this loader.
internal sealed class HardcodedNumbers
{
    internal readonly int[] I32;
    internal readonly uint[] U32;
    internal readonly long[] I64;
    internal readonly ulong[] U64;
    internal readonly float[] F32;
    internal readonly double[] F64;

    private HardcodedNumbers(BinaryReader reader, int[] counts)
    {
        I32 = new int[counts[0]]; for (var i = 0; i < I32.Length; i++) I32[i] = reader.ReadInt32();
        U32 = new uint[counts[1]]; for (var i = 0; i < U32.Length; i++) U32[i] = reader.ReadUInt32();
        I64 = new long[counts[2]]; for (var i = 0; i < I64.Length; i++) I64[i] = reader.ReadInt64();
        U64 = new ulong[counts[3]]; for (var i = 0; i < U64.Length; i++) U64[i] = reader.ReadUInt64();
        F32 = new float[counts[4]]; for (var i = 0; i < F32.Length; i++) F32[i] = reader.ReadSingle();
        F64 = new double[counts[5]]; for (var i = 0; i < F64.Length; i++) F64[i] = reader.ReadDouble();
    }

    internal static HardcodedNumbers Load(string name)
    {
        using var stream = typeof(HardcodedNumbers).Assembly.GetManifestResourceStream(
            "BridgeBuilder.Numbers." + name + ".bin")
            ?? throw new InvalidDataException("Missing embedded numerical resource: " + name);
        return Decode(stream, name);
    }

    internal static HardcodedNumbers Decode(byte[] bytes, string name)
    {
        using var stream = new MemoryStream(bytes, false);
        return Decode(stream, name);
    }

    private static HardcodedNumbers Decode(Stream stream, string name)
    {
        if (stream.Length < 64) throw new InvalidDataException("Truncated numerical resource: " + name);
        using var sha = SHA256.Create();
        var block = new byte[65536];
        long remaining = stream.Length - 32;
        while (remaining > 0)
        {
            int read = stream.Read(block, 0, (int)Math.Min(block.Length, remaining));
            if (read == 0) throw new InvalidDataException("Truncated numerical resource: " + name);
            sha.TransformBlock(block, 0, read, block, 0);
            remaining -= read;
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        foreach (byte value in sha.Hash!)
            if (stream.ReadByte() != value)
                throw new InvalidDataException("Numerical resource checksum mismatch: " + name);
        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt32() != 0x314e4242 || reader.ReadInt32() != 1)
            throw new InvalidDataException("Unsupported numerical resource: " + name);
        var counts = new int[6];
        long expected = 64;
        for (var i = 0; i < counts.Length; i++)
        {
            counts[i] = reader.ReadInt32();
            if (counts[i] < 0) throw new InvalidDataException("Negative numerical array length: " + name);
            expected += (long)counts[i] * (i == 2 || i == 3 || i == 5 ? 8 : 4);
        }
        if (expected != stream.Length) throw new InvalidDataException("Numerical array lengths mismatch: " + name);
        return new HardcodedNumbers(reader, counts);
    }
}
