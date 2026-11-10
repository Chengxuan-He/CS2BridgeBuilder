using BridgeBuilder.Runtime;

if (args.Length == 2 && args[0] == "--catalog")
{
    foreach (var file in Directory.EnumerateFiles(args[1], "*.MidMips", SearchOption.AllDirectories).OrderBy(p => p))
    {
        string cache = File.ReadAllText(file + ".cid").Trim();
        foreach (var surface in (BridgeMidMipReferences.TryRead(File.ReadAllBytes(file),out var midResult1,out var midError1)?midResult1:throw new System.IO.InvalidDataException(midError1)))
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
string[] Refs(byte[] bytes) => (BridgeSurfaceReferences.TryRead(bytes, (_, _) => true,out var surfaceRefs1,out var surfaceRefsError1)?surfaceRefs1:throw new System.IO.InvalidDataException(surfaceRefsError1));
int Levels(string cid)
{
    var path = Path.Combine(args[2], cid + ".VTTexture");
    if (!File.Exists(path)) return 0;
    using var stream = File.OpenRead(path);
    return (BridgeMidMipReferences.TryRequiredLevels(stream, false,out var midResult5,out var midError5)?midResult5:throw new System.IO.InvalidDataException(midError5));
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
    var inputs = (BridgePreparedMaterialFiles.TryRead(args[0], relative, Path.GetFileName(file),out var preparedInputs1,out var preparedError1, Refs, _ => { }, Levels)?preparedInputs1:throw new System.IO.InvalidDataException(preparedError1));
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
        MustFail(() => _ = (BridgePreparedMaterialFiles.TryRead(args[1], relative, Path.GetFileName(file),out var preparedInputs2,out var preparedError2, Refs, _ => { }, Levels)?preparedInputs2:throw new System.IO.InvalidDataException(preparedError2)));
        var bytes = File.ReadAllBytes(caches[0].Path);
        var cid = File.ReadAllText(file + ".cid").Trim();
        if (!(BridgeMidMipReferences.TryRead(bytes,out var midResult2,out var midError2)?midResult2:throw new System.IO.InvalidDataException(midError2)).Contains(cid)) throw new Exception("Cache omitted exact Surface identity.");
    }
}
var common = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0]))!, "Shared", "MidMips");
var sample = Directory.GetFiles(common, "*.MidMips", SearchOption.AllDirectories).First();
var content = File.ReadAllBytes(sample);
MustFail(() => _ = (BridgeMidMipReferences.TryRead(content[..^1],out var midResult3,out var midError3)?midResult3:throw new System.IO.InvalidDataException(midError3)));
var wrongLevels = (byte[])content.Clone(); BitConverter.GetBytes(0).CopyTo(wrongLevels, 4);
MustFail(() => _ = (BridgeMidMipReferences.TryRead(wrongLevels,out var midResult4,out var midError4)?midResult4:throw new System.IO.InvalidDataException(midError4)));
// The current skinned paint descriptor has no VT carrier. The historical
// positive-level paint fixture remains the control for its original cache.
bool currentPaint = File.ReadAllText(Path.Combine(args[0], "Paint", "SkinnedPaint.Surface.cid")).Trim()
    == "eceaaa678f9efb4eec32f1d5e3ad1d10";
int expectedRequired = currentPaint ? 21 : 22;
if (required != expectedRequired || zero != 27 - expectedRequired || negative != expectedRequired + 2) throw new Exception($"Coverage differs: {required}/{zero}/{negative}");
Console.WriteLine($"PASS source-linked prepared MidMips preflight: required={required}; zero-level/empty={zero}; missing/truncated/header negatives={negative}");
