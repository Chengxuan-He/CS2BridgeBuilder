using CS2Mods.Shared.Infrastructure;
using System;
using System.Globalization;

namespace BridgeBuilder.Bridges;

/// <summary>Names the tower prefab owned by one generated bridge.</summary>
internal static class TowerPrefabNaming
{
    /// <summary>
    /// Makes every mod-owned prefab and geometry name safe for the asset database. A period is legal
    /// in a Windows file name but not in an extension-free <c>AssetDataPath</c>: the database reads
    /// everything after it as an extension and rejects the generated asset.
    /// </summary>
    internal static string Safe(string? value)
    {
        var safe = NameSanitizer.MakeFileSystemSafe(value).Replace('.', '_');
        return safe.Length == 0 ? "UnnamedAsset" : safe;
    }

    /// <summary>The design-and-width part shared by the structures belonging to one bridge.</summary>
    internal static string Prefix(string styleId, float deckWidth) =>
        Safe(string.Format(CultureInfo.InvariantCulture, "{0}-{1:0.#}", styleId, deckWidth));

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

        return Safe(primary
            ? name
            : string.Format(CultureInfo.InvariantCulture, "{0} {1}", name, sourceTowerName));
    }
}
