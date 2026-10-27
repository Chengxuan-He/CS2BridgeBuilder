using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Descriptor-only native network-to-object conversion. Original texture CIDs,
// complete mip chains and octahedral tangent decoding remain unchanged.
internal static class Lift01NativeMaterials
{
    internal static string? ObjectDescriptor(string sourceCid) => sourceCid switch
    {
        "368eb4e7fa7b81f6e28ee321c76dd9f2" or "879f0a8882e3069d3d8b890a5ec15140"
            => "2d94a491d55ac415c29591597618b5ef",
        "0a9990f567fc079703b199f1fcb0c53c" => "ec895036ef4c8b94de29cb702fdd6120",
        _ => null
    };

    // The caller supplies the shared style-scoped loader and retains its lifetime.
    // Returned materials require source-atlas UVs, not normalized baked-page UVs.
    internal static SurfaceAsset Resolve(SurfaceAsset original, Func<string, SurfaceAsset> load)
    {
        var cid = ObjectDescriptor(original.id.ToString());
        if (cid == null) return original;
        original.LoadHeader();
        if (!original.isBuiltin || original.materialTemplateHash is not 33)
            throw new InvalidOperationException("Lift01 native network surface identity or template differs: " + original.id);
        var converted = load(cid);
        converted.LoadHeader();
        if (converted.id.ToString() != cid || converted.materialTemplateHash is not 0 || converted.isVTMaterial)
            throw new InvalidOperationException("Lift01 native object descriptor differs: " + cid);
        return converted;
    }
}
