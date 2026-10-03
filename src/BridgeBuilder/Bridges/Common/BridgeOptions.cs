using Game.Prefabs;

namespace BridgeBuilder.Bridges;

/// <summary>What the player asked for on the options page, as the composer needs it.</summary>
internal sealed class BridgeOptions
{
    /// <summary>Null keeps the donor's own build style, which is what the pack author intended.</summary>
    internal BridgeBuildStyle? BuildStyle { get; set; }

    internal bool DoubleDeck { get; set; }

    internal string? LowerDeckId { get; set; }

    internal bool LowerDeckOpposite { get; set; } = true;

    internal float DeckSpacing { get; set; } = 8f;
}
