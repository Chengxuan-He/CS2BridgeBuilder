using BridgeBuilder.Bridges;
using System.Reflection;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

long values = 0;
var names = Assembly.GetExecutingAssembly().GetManifestResourceNames()
    .Where(n => n.StartsWith("BridgeBuilder.Numbers.")).ToArray();
foreach (var resource in names)
{
    if(!resource.EndsWith(".bin"))throw new Exception("Unexpected resource encoding: "+resource);
    var id = resource["BridgeBuilder.Numbers.".Length..^4];
    var data = HardcodedNumbers.Load(id);
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)!;
    using var raw = new MemoryStream();stream.CopyTo(raw);raw.Position=0;
    if(args.Length>1 && !raw.ToArray().SequenceEqual(File.ReadAllBytes(Path.Combine(args[1],id+".bin"))))
        throw new Exception("Embedded bytes differ from pre-migration source: "+resource);
    using var reader = new BinaryReader(raw);
    reader.BaseStream.Position = 32;
    foreach (var x in data.I32) Equal(x, reader.ReadInt32());
    foreach (var x in data.U32) Equal(x, reader.ReadUInt32());
    foreach (var x in data.I64) Equal(x, reader.ReadInt64());
    foreach (var x in data.U64) Equal(x, reader.ReadUInt64());
    foreach (var x in data.F32) Equal(BitConverter.SingleToInt32Bits(x), reader.ReadInt32());
    foreach (var x in data.F64) Equal(BitConverter.DoubleToInt64Bits(x), reader.ReadInt64());
    values += data.I32.Length + data.U32.Length + data.I64.Length + data.U64.Length + data.F32.Length + data.F64.Length;
}
if (names.Length == 0) throw new Exception("No embedded resources.");
byte[] Fixture()
{
    using var s = new MemoryStream();
    using var w = new BinaryWriter(s);
    w.Write(0x314e4242u); w.Write(1);
    foreach (var count in new[] { 2, 2, 2, 2, 3, 3 }) w.Write(count);
    w.Write(int.MinValue); w.Write(int.MaxValue);
    w.Write(uint.MinValue); w.Write(uint.MaxValue);
    w.Write(long.MinValue); w.Write(long.MaxValue);
    w.Write(ulong.MinValue); w.Write(ulong.MaxValue);
    w.Write(-0f); w.Write(float.Epsilon); w.Write(float.MaxValue);
    w.Write(-0d); w.Write(double.Epsilon); w.Write(double.MaxValue);
    w.Flush(); w.Write(SHA256.HashData(s.ToArray())); w.Flush(); return s.ToArray();
}
var fixture = Fixture();
Parallel.For(0,16,_ => Equal(HardcodedNumbers.Decode(fixture,"parallel").I64[0],long.MinValue));
var boundary = HardcodedNumbers.Decode(fixture, "boundary");
Equal(boundary.I64[0], long.MinValue); Equal(boundary.U64[1], ulong.MaxValue);
Equal(BitConverter.SingleToInt32Bits(boundary.F32[0]), int.MinValue);
Equal(BitConverter.DoubleToInt64Bits(boundary.F64[0]), long.MinValue);
Equal(boundary.F32[1], float.Epsilon); Equal(boundary.F64[1], double.Epsilon);
Reject(() => HardcodedNumbers.Load("missing"));
Reject(() => HardcodedNumbers.Decode(fixture[..^1], "truncated"));
var corrupt = (byte[])fixture.Clone(); corrupt[32] ^= 1;
Reject(() => HardcodedNumbers.Decode(corrupt, "corrupt"));
foreach (var offset in new[] { 0, 4, 8 })
{
    var invalid = (byte[])fixture.Clone();
    BitConverter.GetBytes(-1).CopyTo(invalid, offset);
    SHA256.HashData(invalid[..^32]).CopyTo(invalid, invalid.Length - 32);
    Reject(() => HardcodedNumbers.Decode(invalid, "invalid-header"));
}
Console.WriteLine($"PASS {names.Length} embedded resources, {values} typed values, bit-exact decoding, signed/unsigned limits, negative zero, subnormals, corruption/truncation/version/count/missing rejection.");
if (args.Length != 0)
{
    using var dll = File.OpenRead(args[0]);
    using var pe = new PEReader(dll);
    var metadata = pe.GetMetadataReader();
    var embedded = metadata.ManifestResources.Select(h => metadata.GetString(metadata.GetManifestResource(h).Name))
        .Where(n => n.StartsWith("BridgeBuilder.Numbers.")).ToHashSet();
    if (!embedded.SetEquals(names)) throw new Exception("Production DLL resource set differs from the check.");
    int checkedInitializers = 0, maxBytes = 0, lazyHolders=0;
    foreach (var h in metadata.TypeDefinitions)
    {
        var type = metadata.GetTypeDefinition(h);
        var typeName = metadata.GetString(type.Name);
        if(typeName.StartsWith("Numeric_"))
        {
            if((type.Attributes & TypeAttributes.BeforeFieldInit)!=0)throw new Exception("Eager numeric holder: "+typeName);
            lazyHolders++;
        }
        if (!typeName.StartsWith("Lift") && !typeName.StartsWith("Draw") && !typeName.StartsWith("Pedestrian")) continue;
        foreach (var mh in type.GetMethods())
        {
            var method = metadata.GetMethodDefinition(mh);
            if (metadata.GetString(method.Name) != ".cctor") continue;
            int size = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!.Length;
            maxBytes = Math.Max(size, maxBytes); checkedInitializers++;
            if (size > 16384) throw new Exception("Large movable static initializer remains: " + typeName);
        }
    }
    Console.WriteLine($"PASS production DLL resource names and {checkedInitializers} movable static initializers; largest initializer IL={maxBytes} bytes.");
    Console.WriteLine($"PASS {lazyHolders} demand-initialized numeric holders; direct embedded reads and concurrent decoding.");
}
static void Equal<T>(T a, T b) where T : IEquatable<T>
{ if (!a.Equals(b)) throw new Exception($"Mismatch: {a} / {b}"); }
static void Reject(Action action)
{
    try { action(); } catch (InvalidDataException) { return; }
    throw new Exception("Invalid resource accepted.");
}
