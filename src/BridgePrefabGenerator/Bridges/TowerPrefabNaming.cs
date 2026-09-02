using System;
using System.Globalization;

namespace BridgePrefabGenerator.Bridges;

/// <summary>Names the tower prefab owned by one generated bridge.</summary>
internal static class TowerPrefabNaming
{
    /// <summary>The design-and-width part shared by the structures belonging to one bridge.</summary>
    internal static string Prefix(string styleId, float deckWidth) =>
        string.Format(CultureInfo.InvariantCulture, "{0}-{1:0.#}", styleId, deckWidth);

    /// <summary>
    /// A tower belongs to one bridge, even when another bridge uses the same design at the same width.
    /// Secondary structures retain their source name after the bridge-owned name, matching the golden
    /// suspension bridge's existing pylon-and-pier convention.
    /// </summary>
    internal static string ForBridge(
        string styleId,
        float deckWidth,
        string bridgeName,
        string sourceTowerName,
        bool primary)
    {
        var owner = string.IsNullOrWhiteSpace(bridgeName) ? "UnnamedBridge" : bridgeName.Trim();
        var name = string.Format(
            CultureInfo.InvariantCulture,
            "{0}-{1}",
            Prefix(styleId, deckWidth),
            owner);

        return primary
            ? name
            : string.Format(CultureInfo.InvariantCulture, "{0} {1}", name, sourceTowerName);
    }
}
