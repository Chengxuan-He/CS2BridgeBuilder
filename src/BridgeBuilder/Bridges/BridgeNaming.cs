using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BridgeBuilder.Bridges;

/// <summary>
/// What a generated bridge is called: <c>upper_lower_style</c>, or <c>upper_style</c> when there is
/// no lower deck.
///
/// The name records the whole pairing on purpose. A bridge is defined by both decks and the style, so
/// two bridges that differ in any of the three are different assets and must not overwrite each
/// other. Every part is the untranslated identifier - the name registered in Road Builder, or the
/// prefab name - because an asset that renamed itself when the player switched language would stop
/// matching its own export record and would look like a different asset to anyone it was shared with.
/// </summary>
internal static class BridgeNaming
{
    private const string Separator = "_";

    /// <summary>The name before any uniqueness suffix.</summary>
    internal static string BaseName(Deck upper, Deck? lower, BridgeStyle? style)
    {
        var parts = new List<string> { Part(upper.AssetName) };
        if (lower != null) parts.Add(Part(lower.AssetName));
        parts.Add(Part(style?.Id ?? "Bridge"));
        return NameSanitizer.MakeFileSystemSafe(string.Join(Separator, parts));
    }

    /// <summary>
    /// The name to use: what the player typed, or the generated one when they have typed nothing.
    ///
    /// A chosen name is taken as given apart from being made safe to write to disk. The generated one
    /// is what the field shows by default, and it is regenerated whenever the configuration changes -
    /// see <c>BridgeSetting</c> - so a name left alone always describes the bridge it will produce and
    /// never a bridge two settings ago.
    /// </summary>
    internal static string BaseName(Deck upper, Deck? lower, BridgeStyle? style, string? chosen)
    {
        var trimmed = (chosen ?? string.Empty).Trim();
        return trimmed.Length == 0
            ? BaseName(upper, lower, style)
            : NameSanitizer.MakeFileSystemSafe(trimmed);
    }

    /// <summary>
    /// The name to write, made unique against <paramref name="taken"/>.
    ///
    /// <paramref name="reusable"/> is the one name that may collide without being a conflict: the
    /// asset this same pairing produced last time, which a re-run is meant to replace rather than sit
    /// beside as a second copy.
    /// </summary>
    internal static string UniqueName(
        Deck upper,
        Deck? lower,
        BridgeStyle? style,
        ICollection<string> taken,
        Func<string, bool> reusable,
        string? chosen = null)
    {
        var baseName = BaseName(upper, lower, style, chosen);
        if (!taken.Contains(baseName) || reusable(baseName)) return baseName;

        for (var index = 1; index < 1000; index++)
        {
            var candidate = baseName + " (" + index.ToString(CultureInfo.InvariantCulture) + ")";
            if (!taken.Contains(candidate) || reusable(candidate)) return candidate;
        }

        // A thousand bridges from one pairing is not a case worth handling gracefully, but silently
        // overwriting the first would be worse than an obviously odd name.
        return baseName + " (overflow)";
    }

    /// <summary>The name of the second asset a two-road-deck bridge needs.</summary>
    internal static string LowerDeckName(string bridgeName) => bridgeName + Separator + "Lower";

    /// <summary>
    /// The name of the deck carried alongside a bridge, said by where it actually sits.
    ///
    /// Both happen. An archetype that hangs its second net above is built on the deck the player
    /// chose, and the road they converted is the one carried - above. Calling that prefab "Lower"
    /// because it is the carried one would be a name that contradicts the thing it names.
    /// </summary>
    /// <summary>
    /// The name of a section derived from one of the road's own - the same section without the pieces
    /// this bridge supplies itself.
    /// </summary>
    internal static string SectionName(string bridgeName, string sectionName) =>
        sectionName + Separator + bridgeName;

    internal static string CarriedDeckName(string bridgeName, bool above) =>
        above ? bridgeName + Separator + "Upper" : LowerDeckName(bridgeName);

    /// <summary>
    /// Keeps the separator meaningful: a road whose own name contains an underscore would otherwise
    /// read as two parts, and the name would no longer say which deck was which.
    /// </summary>
    private static string Part(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? "Unnamed" : trimmed.Replace(Separator, "-");
    }
}
