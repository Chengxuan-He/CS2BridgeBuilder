namespace BridgeBuilder.Bridges;

// Measured original fixed NetPiece graph, including its non-root RenderPrefab
// LOD children. All three native descriptors use template 33. The common caller
// additionally requires a native network graph and validates builtin ownership.
internal static class Lift03NativeMaterials
{
    internal static bool IsNativeNetwork(string render, int mesh, string sourceCid) =>
        (render, mesh, sourceCid) switch
        {
            ("LiftBridge03Net Mesh", 0, "9b568ffac0e99361e5531554b45c2ea7") => true,
            ("LiftBridge03Net_LOD1 Mesh", 0, "11fec4b6bcc46091a61029632c295a1c") => true,
            ("LiftBridge03Net_LOD2 Mesh", 0, "c1c539bc6ef12cacaa0994bfcc750634") => true,
            _ => false
        };
}
