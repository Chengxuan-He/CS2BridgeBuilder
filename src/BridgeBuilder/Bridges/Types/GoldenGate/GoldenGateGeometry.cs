namespace BridgeBuilder.Bridges;

/// <summary>
/// Exact Golden Gate archetype identities, shared by the native and BXP decks.
/// End and middle cable meshes contain only left/right cable and hanger assemblies:
/// the captured full-detail clear spans are 25.01709 m and 24.69287 m respectively.
/// Neither assembly reaches x=0. They translate, including every mesh returned by the
/// geometry asset, and never participate in the support mesh's railing remapping.
/// These two archetype prefabs have no separate LodProperties meshes.
/// </summary>
internal static class GoldenGateGeometry
{
    internal static bool IsCable(string? styleId, string meshName) =>
        styleId is "GoldenGate" or "GoldenGateDouble"
        && meshName is "Golden Gate Bridge End Cables" or "Golden Gate Bridge Middle Cables";
}
