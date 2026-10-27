using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Offline network-to-object descriptors retain the original texture CIDs and mips.
internal static class Draw02NativeMaterials
{
    internal static string? ObjectDescriptor(string sourceCid) => sourceCid switch
    {
        "55697a0c744a3cfa77056b9ff0f93646" or "9b0ef24ab46511040acf280f549a3a73"
            => "e7e3a33d1499167c8c385ebfb70af78b",
        "5d98bd98700c73926bb2b3ff69c784e2" or "9f31e9f71c97263c984303131decbfb0"
            or "af48c4b16775963b7d025f9f672722f1" or "dee7bfe689bf6752861f0f6f29a6c8e2"
            => "1d6c92d008a952b1db4e1cc6cf7a1160",
        _ => null
    };

    // The shared caller owns style-local loading and asset lifetime. These
    // descriptors require original source-atlas UVs rather than baked-page UVs.
    internal static SurfaceAsset Resolve(SurfaceAsset original, Func<string, SurfaceAsset> load)
    {
        var cid = ObjectDescriptor(original.id.ToString());
        if (cid == null) return original;
        original.LoadHeader();
        if (!original.isBuiltin || original.materialTemplateHash is not 17)
            throw new InvalidOperationException("Draw02 native network surface identity or template differs: " + original.id);
        var converted = load(cid);
        converted.LoadHeader();
        if (converted.id.ToString() != cid || converted.materialTemplateHash is not 0 || converted.isVTMaterial)
            throw new InvalidOperationException("Draw02 native object descriptor differs: " + cid);
        return converted;
    }
}
