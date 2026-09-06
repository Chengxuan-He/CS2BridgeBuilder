namespace BridgeBuilder.Bridges;

/// <summary>Derives a generated bridge's lateral change from the archetype it copies.</summary>
internal static class PrototypeBridgeSizing
{
    /// <summary>
    /// The V-shaped double-deck bridge is built on its upper road. Its lower net is an auxiliary and
    /// contributes no width: the only valid change is target upper road minus prototype upper road.
    /// A tower opening is geometry around that road, not a second measurement of the road itself.
    /// </summary>
    internal static float UpperDeckExtra(
        float targetUpperWidth, float prototypeUpperWidth, float fallback)
    {
        return targetUpperWidth > 0f && prototypeUpperWidth > 0f
            ? targetUpperWidth - prototypeUpperWidth
            : fallback;
    }
}
