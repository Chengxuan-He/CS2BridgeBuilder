using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Offline descriptor-only conversion preserves the original texture identities.
internal static class Draw01NativeMaterials
{
    internal static string? ObjectDescriptor(string sourceCid) => sourceCid switch
    {
        "16b28f9bc6abc54fd73acbdfa5f14978" or "45f6fe14df0150267b86e281c8d17ec2"
            or "8f424eb10e5caa7edce9770e4350d542" => "1d6c92d008a952b1db4e1cc6cf7a1160",
        _ => null
    };

    internal static SurfaceAsset Resolve(SurfaceAsset original, Func<string, SurfaceAsset> load)
    {
        var cid = ObjectDescriptor(original.id.ToString());
        if (cid == null) return original;
        original.LoadHeader();
        if (!original.isBuiltin || original.materialTemplateHash is not 17)
            throw new InvalidOperationException("Draw01 native network surface identity or template differs: " + original.id);
        var converted = load(cid);
        converted.LoadHeader();
        if (converted.id.ToString() != cid || converted.materialTemplateHash is not 0 || converted.isVTMaterial)
            throw new InvalidOperationException("Draw01 native object descriptor differs: " + cid);
        return converted;
    }
}
