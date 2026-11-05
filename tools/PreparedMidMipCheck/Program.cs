using BridgeBuilder.Runtime;

if (args.Length == 2 && args[0] == "--catalog")
{
    foreach (var file in Directory.EnumerateFiles(args[1], "*.MidMips", SearchOption.AllDirectories).OrderBy(p => p))
    {
        string cache = File.ReadAllText(file + ".cid").Trim();
        foreach (var surface in BridgeMidMipReferences.Read(File.ReadAllBytes(file)))
        {
            Console.WriteLine($"        \"{surface}\" => \"{cache}\",");
            if (BridgePreparedMaterialFiles.CacheFor(surface) != cache)
                throw new Exception("Forward cache catalogue differs for " + surface);
        }
    }
    return;
}

if (args.Length != 3) throw new ArgumentException("Usage: PreparedMidMipCheck <candidate-style-root> <original-style-root> <VT-fixtures>");
int required = 0, zero = 0, negative = 0;
string[] Refs(byte[] bytes) => BridgeSurfaceReferences.Read(bytes, (_, _) => true);
int Levels(string cid)
{
    var path = Path.Combine(args[2], cid + ".VTTexture");
    if (!File.Exists(path)) return 0;
    using var stream = File.OpenRead(path);
    return BridgeMidMipReferences.RequiredLevels(stream, false);
}
void MustFail(Action action)
{
    try { action(); }
    catch (InvalidDataException) { negative++; return; }
    catch (EndOfStreamException) { negative++; return; }
    throw new Exception("Malformed/missing MidMips input was accepted.");
}
foreach (var file in Directory.GetFiles(args[0], "*.Surface", SearchOption.AllDirectories))
{
    string relative = Path.GetRelativePath(args[0], Path.GetDirectoryName(file)!);
    if (relative != "Paint" && !relative.StartsWith("Lanes" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
    int levels = Refs(File.ReadAllBytes(file)).Select(Levels).DefaultIfEmpty().Max();
    var inputs = BridgePreparedMaterialFiles.Read(args[0], relative, Path.GetFileName(file), Refs, _ => { }, Levels);
    var caches = inputs.Where(i => Path.GetExtension(i.Path) == ".MidMips").ToArray();
    if (levels == 0)
    {
        if (caches.Length != 0) throw new Exception("Zero-level source gained a cache dependency.");
        zero++;
    }
    else
    {
        if (caches.Length != 1) throw new Exception("Expected one unambiguous native cache membership.");
        required++;
        MustFail(() => BridgePreparedMaterialFiles.Read(args[1], relative, Path.GetFileName(file), Refs, _ => { }, Levels));
        var bytes = File.ReadAllBytes(caches[0].Path);
        var cid = File.ReadAllText(file + ".cid").Trim();
        if (!BridgeMidMipReferences.Read(bytes).Contains(cid)) throw new Exception("Cache omitted exact Surface identity.");
    }
}
var common = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0]))!, "Shared", "MidMips");
var sample = Directory.GetFiles(common, "*.MidMips", SearchOption.AllDirectories).First();
var content = File.ReadAllBytes(sample);
MustFail(() => BridgeMidMipReferences.Read(content[..^1]));
var wrongLevels = (byte[])content.Clone(); BitConverter.GetBytes(0).CopyTo(wrongLevels, 4);
MustFail(() => BridgeMidMipReferences.Read(wrongLevels));
// The current skinned paint descriptor has no VT carrier. The historical
// positive-level paint fixture remains the control for its original cache.
bool currentPaint = File.ReadAllText(Path.Combine(args[0], "Paint", "SkinnedPaint.Surface.cid")).Trim()
    == "eceaaa678f9efb4eec32f1d5e3ad1d10";
int expectedRequired = currentPaint ? 21 : 22;
if (required != expectedRequired || zero != 27 - expectedRequired || negative != expectedRequired + 2) throw new Exception($"Coverage differs: {required}/{zero}/{negative}");
Console.WriteLine($"PASS source-linked prepared MidMips preflight: required={required}; zero-level/empty={zero}; missing/truncated/header negatives={negative}");
