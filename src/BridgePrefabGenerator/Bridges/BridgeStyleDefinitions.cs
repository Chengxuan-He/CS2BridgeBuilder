using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// One named bridge style the player can pick, and how to recognise the prefabs that provide it.
/// </summary>
internal sealed class BridgeStyleDefinition
{
    internal BridgeStyleDefinition(string id, string nameSuffix, int clearance, params string[] patterns)
    {
        Id = id;
        NameSuffix = nameSuffix;
        Clearance = clearance;
        Patterns = patterns;
    }

    /// <summary>Stored in the settings file and never translated.</summary>
    internal string Id { get; }

    /// <summary>
    /// What an exported asset is called after the road name. Deliberately not the translated name:
    /// an asset that renamed itself when the player switched language would stop matching its own
    /// export record, and would look like a different asset to anyone it was shared with.
    /// </summary>
    internal string NameSuffix { get; }

    /// <summary>
    /// How much wider than the road this kind of bridge is built, in whole metres.
    ///
    /// A measured number, not a guess: it is the mean margin between tower and road across every
    /// variant of this style that could be measured, taken from a real scan. Zero means the style has
    /// no structure that straddles the road at all - a bascule or lift bridge raises its deck rather
    /// than standing over it, and a covered footbridge is a roof - so demanding a margin of them would
    /// rule out every variant they have.
    /// </summary>
    internal int Clearance { get; }

    /// <summary>Normalised fragments; a prefab belongs to this style if its name contains any of them.</summary>
    internal IReadOnlyList<string> Patterns { get; }
}

/// <summary>
/// The fixed list of bridge styles.
///
/// These are the styles the base game ships - suspension, extradosed, truss arch, the bascule and
/// lift bridges, the pedestrian ones - named once here rather than inferred from whatever happens to
/// be registered. Two reasons it is a fixed list and not a discovered one:
///
/// The options page is built when the mod loads, long before any world exists, and discovery needs a
/// world. A discovered list is therefore empty exactly when the player first opens the page, which is
/// the worst possible moment to show nothing.
///
/// And prefab names are identifiers, not labels. "TrussArchBridge02" is not what the style is called
/// in any language; naming it here lets every entry be translated properly.
///
/// Discovery still runs, and still decides which variant of a style fits a given road - and any
/// bridge from an asset pack that matches none of these patterns is added to the list as it is found,
/// so installing a pack still widens the choice.
/// </summary>
internal static class BridgeStyleDefinitions
{
    /// <summary>
    /// What a fresh install starts on. Suspension is the one style every player who has the bridge
    /// content will have, and starting on a concrete style keeps the dropdown from opening on a value
    /// that matches none of its entries.
    /// </summary>
    internal const string Default = "Suspension";

    /// <summary>
    /// Whether a style's overhead section is an open, member-built truss.
    ///
    /// The blue, white and green bridges are different prototypes and keep their own measured road
    /// widths, materials and donors. They nevertheless require the same mesh rule: side members
    /// translate as rigid bodies and members crossing the centre use one affine transform for their
    /// whole connected component. Keeping this classification here prevents one colour silently
    /// falling back to the height-band portal rule.
    /// </summary>
    internal static bool UsesOpenTrussTopology(string? styleId) =>
        styleId is "TrussArch01" or "TrussArch02" or "TrussArch03";

    /// <summary>
    /// Whether an open-truss prototype authors its inner railing and outer arch as one side assembly.
    /// Such an assembly must be carried rigidly; scaling across its x extent changes the authored
    /// railing-to-arch clearance. TrussArchBridge03 is built this way, while the blue prototype's
    /// inward rods and centre pivots need the shared transverse scale instead.
    /// </summary>
    internal static bool PreservesOpenTrussSideAssembly(string? styleId) => styleId == "TrussArch03";

    /// <summary>
    /// Order matters: the first matching entry wins, so the narrower patterns come first. Without
    /// that, "PedestrianDrawBridge01" would be filed under the road-carrying bascule bridges.
    ///
    /// The number after the name is the style's clearance in whole metres - how much wider than the
    /// road its towers are built.
    ///
    /// Each is measured against that style's own deck sections, not against the whole net. The net
    /// counts railings and edges the tower stands inside of, and measuring that way gave the golden
    /// suspension bridges the same 8 m margin as the pale ones - so a 42 m road was told a 50 m golden
    /// tower would span it, when that tower was built around a 34 m deck and needs 17 m of margin.
    /// A style whose towers do not straddle the road at all takes 0: a bascule or lift bridge raises
    /// its deck rather than standing over it, so demanding a margin would rule out every variant.
    /// The comment on each line is the measurement and how many samples agreed.
    /// </summary>
    internal static readonly IReadOnlyList<BridgeStyleDefinition> All = new[]
    {
        new BridgeStyleDefinition("PedestrianDraw", "Pedestrian Bascule Bridge", 0, "pedestriandrawbridge"),
        new BridgeStyleDefinition("CoveredWood", "Covered Wooden Bridge", 0, "pedestrianbridgecoveredwood", "coveredwood"),
        // The game ships two suspension designs and they are different colours: 01 and 02 are the pale
        // steel ones, 03 and 04 the golden pair, and the packs recolour along the same numbering. The
        // number is therefore not a size but an identity, and asking for a suspension bridge should not
        // decide the colour by which happened to fit. Golden goes first: it is the narrower rule.
        // Golden shares the plain style's margin - same author, same structure, one usable sample of
        // its own, and that sample's road width is the one the scan got wrong.
        new BridgeStyleDefinition("SuspensionGolden", "Golden Suspension Bridge", 17, // 16.6 over 3
            "suspensionbridge03", "suspensionbridge04", "goldengate"),
        // Only the highway suspension bridges. The vanilla SuspensionBridge01..04 are separate designs
        // that happen to share the principle, and folding them in here is what let a two-lane bridge
        // stand in for a six-lane road and a golden tower answer a request for a pale one. They are
        // still discovered - they simply arrive as their own family rather than as this style.
        new BridgeStyleDefinition("Suspension", "Suspension Bridge", 7,          // 7.3 over 8
            "suspensionbridgehighway", "suspensionhighway"),
        // The cable-stayed family, one style per pylon. They were one entry and are five bridges: the
        // pylon is what a cable-stayed design is, and a road fitted to one of them cannot wear
        // another's. Declared before the general patterns, so the specific name wins - the same
        // ordering that keeps SuspensionBridge03 out of the blue suspension family.
        //
        // 01 and 02 carry a second deck of their own - AuxiliaryNets, confirmed in the dump - and 03
        // does not. That is not recorded here: BridgeStyle.Select reads it off the variant, so asking
        // for a single deck bridge from a double deck archetype refuses on what the prefab is rather
        // than on what a table remembers about it.
        new BridgeStyleDefinition("Extradosed01", "Extradosed Bridge", 23, "extradosedbridge01"),
        new BridgeStyleDefinition("Extradosed02", "Extradosed Bridge", 23, "extradosedbridge02"),
        new BridgeStyleDefinition("Extradosed03", "Extradosed Bridge", 23, "extradosedbridge03"),
        new BridgeStyleDefinition(
            "ExtradosedLarge", "Extradosed Bridge", 23, "extradosedbridgelargeroaddivided"),
        // No catch-all "Extradosed" style. It offered a choice between designs rather than between
        // sizes of one - the pylon is what a cable-stayed bridge is - and every design that has been
        // looked at now has a style of its own above. A prefab matching none of them is picked up by
        // the catalogue as a family of its own rather than filed under a name that means five bridges.
        new BridgeStyleDefinition("CableStayed", "Cable-Stayed Bridge", 3,       // 3.1 over 4
            "cablestayed", "cablestay"),
        // Before the general truss arch pattern, so the specific name wins - the same ordering that
        // keeps SuspensionBridge03 out of the blue suspension family.
        //
        // Two designs share the name and they are not the same bridge. The road runs over the arch on
        // one and under it on the other, so a road fitted to one of them cannot wear the other's
        // structure. The data says the same thing twice: the arch-below bridges carry a portal wider
        // than their road, while TrussArchBridge01 carries a pillar narrower than its road - already
        // recorded as a support - and puts the arch itself overhead, one section down each side.
        //
        // 01, 02 and 03 are separately selectable arch-above prototypes. Keep every exact pattern before
        // the general truss-arch pattern: each generated bridge must copy the complete section, tower
        // and material family of the prototype the player selected, rather than sharing a donor with
        // another colour.
        new BridgeStyleDefinition("TrussArch01", "Truss Arch Bridge 01", 3, "trussarchbridge01"),
        new BridgeStyleDefinition("TrussArch02", "Truss Arch Bridge 02", 3, "trussarchbridge02"),
        new BridgeStyleDefinition("TrussArch03", "Truss Arch Bridge 03", 3, "trussarchbridge03"),
        new BridgeStyleDefinition("TrussArch", "Truss Arch Bridge", 3,           // 2.5 over 18
            "trussarchbridge", "trussarch"),
        new BridgeStyleDefinition("TiedArch", "Tied Arch Bridge", 1,             // 1.3 over 3
            "tiedarch"),
        new BridgeStyleDefinition("Grand", "Grand Bridge", 23,                   // 23.2 over 2
            "grandbridge"),
        // Moveable bridges lift their deck instead of standing over it, so no sample of either had a
        // structure wider than its road. Demanding a margin would rule out every variant they have.
        new BridgeStyleDefinition("Draw", "Bascule Bridge", 0, "drawbridge", "bascule"),
        new BridgeStyleDefinition("Lift", "Lift Bridge", 0, "liftbridge"),
    };

    /// <summary>The style a prefab belongs to, or null when it is not one of the named styles.</summary>
    internal static BridgeStyleDefinition? Match(string prefabName)
    {
        var normalised = Normalise(prefabName);
        if (normalised.Length == 0) return null;

        return All.FirstOrDefault(definition =>
            definition.Patterns.Any(pattern => normalised.Contains(pattern)));
    }

    /// <summary>
    /// Folds away everything the two naming conventions disagree about, so one pattern matches both.
    /// The game writes "SuspensionBridge01" and packs write "BXP Suspension Bridge - 6 Lanes"; lower
    /// cased and stripped of spaces and punctuation, both contain "suspensionbridge", while a pattern
    /// that names a design by number still finds it.
    /// </summary>
    private static string Normalise(string? name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;

        var builder = new System.Text.StringBuilder(name!.Length);
        foreach (var character in name)
        {
            // Digits are kept. Dropping them turned "SuspensionBridge03" into "suspensionbridge", so a
            // pattern naming the third design could never match one - which is why the golden bridges
            // stayed filed under the plain suspension style no matter how the rule was written.
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    /// <summary>
    /// The prefix of a pack whose bridges the base game now ships itself.
    ///
    /// Bridge Expansion Pack. Its content was folded into the game, so every BXP bridge has a vanilla
    /// twin of the same design. Keeping both offers the player the same bridge twice, and deriving from
    /// the pack's copy binds a generated bridge to an asset that can be uninstalled while the identical
    /// vanilla one cannot.
    /// </summary>
    internal const string SupersededPackPrefix = "BXP ";

    /// <summary>
    /// Whether a prefab belongs to a pack the base game has absorbed - by name alone.
    ///
    /// Being pack content is not on its own a reason to skip it. The reason is duplication, so the
    /// caller pairs this with what the prefab can do: a pack bridge offering something the base game
    /// has no archetype for - a second deck - is kept, because skipping it removes the capability
    /// rather than a duplicate of it.
    /// </summary>
    internal static bool IsSupersededPack(string? name) =>
        name != null && name.StartsWith(SupersededPackPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Styles whose generation is deferred, and why.
    ///
    /// A bascule bridge and a lift bridge are not a deck with structure over it: the deck itself is the
    /// mechanism, split into leaves that rotate or a span that rises between towers, and widening it
    /// means widening a machine whose parts have to keep meeting each other through the whole of their
    /// travel. That is a different problem from widening a portal, and none of it has been measured.
    ///
    /// They are refused rather than attempted. A bridge that is not generated is a bridge the player
    /// still has; a bridge generated from an arrangement nobody has measured is one that looks built
    /// and behaves as something else.
    /// </summary>
    private static readonly Dictionary<string, string> Deferred =
        new(StringComparer.Ordinal)
        {
            ["Draw"] = "a bascule bridge's deck is its mechanism - leaves that rotate - and widening "
                + "one has not been worked out",
            ["PedestrianDraw"] = "a bascule bridge's deck is its mechanism - leaves that rotate - and "
                + "widening one has not been worked out",
            ["Lift"] = "a lift bridge's deck is its mechanism - a span that rises between towers - and "
                + "widening one has not been worked out",
        };

    /// <summary>Why a style is deferred, or null when it is not.</summary>
    internal static string? DeferredReason(string? styleId)
    {
        if (styleId == null) return null;
        return Deferred.TryGetValue(styleId, out var reason) ? reason : null;
    }

}
