using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Fixed network materials require their native composition context. Carried
// object materials retain their complete original descriptors and dependencies.
internal static class Lift01NativeMaterials
{
    // These three slots belong to the fixed NetPiece approach graph, including
    // its RenderPrefab LOD children. Their positive V is a native longitudinal
    // rate, not an object-atlas coordinate. The caller must additionally prove
    // the complete graph retains its native network renderer and template.
    internal static bool IsNativeNetwork(string render, int mesh, string sourceCid) =>
        (render, mesh, sourceCid) switch
        {
            ("LiftBridge01Net Mesh", 0, "368eb4e7fa7b81f6e28ee321c76dd9f2") => true,
            ("LiftBridge01Net_LOD1 Mesh", 0, "879f0a8882e3069d3d8b890a5ec15140") => true,
            ("LiftBridge01Net_LOD2 Mesh", 0, "0a9990f567fc079703b199f1fcb0c53c") => true,
            _ => false
        };

    // The shared caller retains this signature while migrating the other styles.
    // A prepared object descriptor is not original non-road material reuse.
    // Native network graphs bypass this object-only resolver through the exact
    // render/slot/CID predicate above; rejecting leakage preserves that boundary.
    internal static SurfaceAsset Resolve(SurfaceAsset original, Func<string, SurfaceAsset> load)
    {
        original.LoadHeader();
        if (original.materialTemplateHash is 17 or 33)
            throw new InvalidOperationException("Lift01 network material requires its native NetPiece render context: " + original.id);
        return original;
    }
}
