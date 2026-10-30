using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Offline native descriptor conversions share the original texture payloads.
internal static class Draw03NativeMaterials
{
    // Only valid when the shared caller has retained the native NetPiece graph,
    // including its RenderPrefab LOD children. These source meshes use finite
    // negative-V UVs; shader 581 retains their worldspace layer in that context.
    // This is not permission to use a network Surface on an object or mechanism.
    internal static bool IsNativeNetwork(string render, int mesh, string sourceCid)
    {
        if (mesh is not 0) return false;
        var cid = render switch
        {
            "DrawBridge03BottomFirst300cm Mesh" => "d03892b7da3a2d59c54f291688a97768",
            "DrawBridge03BottomFirst300cm_LOD1 Mesh" => "81d1460e8651ade51c7446562a728aaa",
            "DrawBridge03BottomFirst300cm_LOD2 Mesh" => "7daf1691b38921d037a72de87093e521",
            "DrawBridge03BottomSecond300cm Mesh" => "e965aed500cf3353b1bcc3056028a4f6",
            "DrawBridge03BottomSecond300cm_LOD1 Mesh" => "350e0f2d943a7ffb6b2e88e40d3eb4f6",
            "DrawBridge03BottomSecond300cm_LOD2 Mesh" => "25627b171c4bafc49229e18d08fc7939",
            "DrawBridge03BottomLast300cm Mesh" => "3c980464d19cc45e65320758ffd7e19a",
            "DrawBridge03BottomLast300cm_LOD1 Mesh" => "6704b71480669ef830c4a98b195ab105",
            "DrawBridge03BottomLast300cm_LOD2 Mesh" => "fdfd44f97b07c61262c0e6ffdaf20ec4",
            "DrawBridge03BottomEdge350cm Mesh" => "3afa75210adf6176ad365cd63bb0539e",
            "DrawBridge03BottomEdge350cm_LOD1 Mesh" => "3405c6dec952aa0ced1de6a74b9d75dd",
            "DrawBridge03BottomEdge350cm_LOD2 Mesh" => "8c0e869e9e26251f2df1f613097dff35",
            "DrawBridge03Side50cm Mesh" => "ff2d7583e73f3484d85cf180612bc11a",
            "DrawBridge03Side50cm_LOD1 Mesh" => "833b0983e437a2f3d420e86b402164f4",
            "DrawBridge03Side50cm_LOD2 Mesh" => "03da929429ede1feaa8d311e9f8a90bb",
            _ => null
        };
        return cid != null && sourceCid == cid;
    }

    internal static string? ObjectDescriptor(string sourceCid) => sourceCid switch
    {
        "03da929429ede1feaa8d311e9f8a90bb" or "8c0e869e9e26251f2df1f613097dff35"
            => "a6b3efae98e0fc2f3056ec517bb949cb",
        "3405c6dec952aa0ced1de6a74b9d75dd" or "3afa75210adf6176ad365cd63bb0539e"
            or "833b0983e437a2f3d420e86b402164f4" or "ff2d7583e73f3484d85cf180612bc11a"
            => "1b7d44bbf84bd6c880f2dde840b638f6",
        "25627b171c4bafc49229e18d08fc7939" or "7daf1691b38921d037a72de87093e521"
            or "fdfd44f97b07c61262c0e6ffdaf20ec4"
            => "b2e79001615c2697a6448f909f6ad7f3",
        "350e0f2d943a7ffb6b2e88e40d3eb4f6" or "3c980464d19cc45e65320758ffd7e19a"
            or "6704b71480669ef830c4a98b195ab105" or "81d1460e8651ade51c7446562a728aaa"
            or "d03892b7da3a2d59c54f291688a97768" or "e965aed500cf3353b1bcc3056028a4f6"
            => "eb26a16316548f7db386fdf96b019862",
        _ => null
    };

    // The caller supplies the style-scoped loader and owns its resource lifetime.
    // These descriptors retain source-atlas UVs and native texture-global settings.
    internal static SurfaceAsset Resolve(SurfaceAsset original, Func<string, SurfaceAsset> load)
    {
        var cid = ObjectDescriptor(original.id.guid.ToString());
        if (cid == null) return original;
        original.LoadHeader();
        var source33 = cid is "a6b3efae98e0fc2f3056ec517bb949cb" or "1b7d44bbf84bd6c880f2dde840b638f6";
        var wrongTemplate = source33 ? original.materialTemplateHash is not 33 : original.materialTemplateHash is not 17;
        if (!original.isBuiltin || wrongTemplate || original.isVTMaterial)
            throw new InvalidOperationException("Draw03 native network surface identity or template differs: " + original.id);
        var converted = load(cid);
        converted.LoadHeader();
        if (converted.id.guid.ToString() != cid || converted.materialTemplateHash is not 0 || converted.isVTMaterial)
            throw new InvalidOperationException("Draw03 native object descriptor differs: " + cid);
        return converted;
    }
}
