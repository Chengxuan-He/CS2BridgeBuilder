using System;
using System.IO;


namespace BridgeBuilder.Bridges;

// Immutable measured arrays, owned by the active construction/metadata scope.
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
        => GenerationParameters.Get("BBN:" + name, () => Read(name));

    private static HardcodedNumbers Read(string name)
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
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        reader.ReadUInt32(); reader.ReadInt32();
        var counts = new int[6];
        for (var i = 0; i < counts.Length; i++) counts[i] = reader.ReadInt32();
        return new HardcodedNumbers(reader, counts);
    }
}
