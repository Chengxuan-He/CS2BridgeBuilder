using CS2Mods.Shared.Infrastructure;
using System.Collections.Generic;

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

    private static string Safe(string? value) =>
        NameSanitizer.MakeFileSystemSafe(value).Replace('.', '_');

    /// <summary>The name before any uniqueness suffix.</summary>
    internal static string BaseName(Deck upper, Deck? lower, BridgeStyle? style)
    {
        var parts = new List<string> { Part(upper.AssetName) };
        if (lower != null) parts.Add(Part(lower.AssetName));
        parts.Add(Part(style?.Id ?? "Bridge"));
        return Safe(string.Join(Separator, parts));
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
        bridgeName + Separator + sectionName;

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
