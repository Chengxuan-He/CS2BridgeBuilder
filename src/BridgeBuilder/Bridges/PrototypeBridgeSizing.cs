namespace BridgeBuilder.Bridges;
/// <summary>Derives a generated bridge's lateral change from the archetype it copies.</summary>
internal static class PrototypeBridgeSizing
{
    /// <summary>
    /// A double-deck bridge is sized from the road/deck in the archetype's root ownership role. That
    /// is the upper road when the auxiliary hangs below (Suspension and ExtradosedBridge01), and the
    /// lower road when the auxiliary hangs above (ExtradosedBridge02). The other deck and the tower
    /// opening are never substituted for this reference width.
    /// </summary>
    internal static float ReferenceDeckExtra(float targetReferenceWidth, float prototypeReferenceWidth, float fallback)
    {
        return targetReferenceWidth > Numeric_PrototypeBridgeSizing_cs.Values.F32[0] && prototypeReferenceWidth > Numeric_PrototypeBridgeSizing_cs.Values.F32[1] ? targetReferenceWidth - prototypeReferenceWidth : fallback;
    }
}

// Embedded numerical arrays: PrototypeBridgeSizing_cs
internal static class Numeric_PrototypeBridgeSizing_cs
{
    static Numeric_PrototypeBridgeSizing_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("PrototypeBridgeSizing_cs");
}
