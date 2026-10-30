using BridgeBuilder.Bridges;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

// Domain order is the measured Draw02RoadAppearance arrangement: each leaf
// contains carriageway, walkway, fixed carriageway and fixed walkway pairs.
int checkedDomains = 0;
for (int lod = 0; lod < 3; lod++)
{
    var flags = Draw02AppearanceSupport.ForLod(lod);
    if (flags.Length != 16) throw new Exception("Missing Draw02 physical domains.");
    foreach (int domain in new[] { 0, 1, 4, 5, 8, 9, 12, 13 })
        if (flags[domain] != Type.Asphalt) throw new Exception("Carriageway admits an unsupported appearance.");
    foreach (int domain in new[] { 2, 3, 6, 7, 10, 11, 14, 15 })
        if (flags[domain] != (Type.Asphalt | Type.Sidewalk)) throw new Exception("Walkway appearance roles differ.");
    foreach (var flag in flags)
        if ((flag & (Type.WheelTrack | Type.Pathway | Type.Ballast | Type.TrackBed | (Type)64)) != 0)
            throw new Exception("Unmeasured type admitted by the prototype gate.");
    // A caller mutating one request must not alter another LOD or generation.
    flags[0] = Type.None;
    if (Draw02AppearanceSupport.ForLod(lod)[0] != Type.Asphalt)
        throw new Exception("Support state leaked between generation requests.");
    checkedDomains += flags.Length;
}
foreach (int lod in new[] { -1, 3, int.MaxValue })
{
    try { Draw02AppearanceSupport.ForLod(lod); }
    catch (ArgumentOutOfRangeException) { continue; }
    throw new Exception("Unknown Draw02 LOD accepted.");
}
Console.WriteLine($"PASS Draw02 appearance support: {checkedDomains} domains; independent asphalt/sidewalk roles; wheel-track and unknown types excluded; invalid LOD rejected; no shared mutable state. Native sampling is a separate check.");

namespace Colossal.IO.AssetDatabase { internal sealed class SurfaceAsset { } }
namespace BridgeBuilder.Bridges
{
    internal static class MovableExistingRoadSurface { internal readonly struct Patch { } }
    internal static class MovableRoadGeometry { internal sealed class Partition { } }
    internal static class Numeric_Types_Draw02_Draw02RenderAssembly_cs
    {
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02RenderAssembly_cs");
    }
}
