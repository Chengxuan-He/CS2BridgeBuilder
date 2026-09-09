using BridgeBuilder.Settings;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The bridge styles the player can choose from, and the prefabs that provide them.
///
/// The list itself is fixed - see <see cref="BridgeStyleDefinitions"/> - so the dropdown always reads
/// as a set of named styles whether or not a world has been scanned yet. What discovery contributes
/// is the variants: which registered prefabs provide each style and how wide each one was authored,
/// which is what the width fitting needs. A style with no variants yet is still listed, and simply
/// says it is not available.
///
/// Bridges from asset packs that match none of the named styles are appended as they are found, so
/// installing a pack still widens the list rather than being ignored.
/// </summary>
internal static class BridgeStyleCatalog
{
    private static readonly object Gate = new();
    private static List<BridgeStyle> _styles = CreateNamedStyles();
    private static bool _scanned;

    internal static IReadOnlyList<BridgeStyle> Styles
    {
        get { lock (Gate) return _styles; }
    }

    /// <summary>
    /// Whether a world has been scanned yet. Before that, no style has variants, and marking them all
    /// as unavailable would be misleading rather than informative - they are unbound, not missing.
    /// </summary>
    internal static bool Scanned
    {
        get { lock (Gate) return _scanned; }
    }

    internal static BridgeStyle? Find(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        lock (Gate)
        {
            return _styles.FirstOrDefault(style => string.Equals(style.Id, id, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// The style an export will actually use: the stored one, or the first installed one when nothing
    /// has been chosen. Falls back to the first entry so that a caller always has something to name,
    /// even before a world has been scanned.
    /// </summary>
    internal static BridgeStyle? Resolve(string? id)
    {
        var chosen = Find(id);
        if (chosen != null) return chosen;

        lock (Gate)
        {
            return _styles.FirstOrDefault(style => style.IsInstalled) ?? _styles.FirstOrDefault();
        }
    }

    /// <summary>The named styles, with no variants attached yet.</summary>
    private static List<BridgeStyle> CreateNamedStyles()
    {
        return BridgeStyleDefinitions.All
            .Select(definition => new BridgeStyle(
                definition.Id,
                definition.NameSuffix,
                () => UiStringCatalog.Current.StyleName(definition.Id),
                definition.Clearance))
            .ToList();
    }

    /// <summary>
    /// Re-binds every style to the prefabs currently registered. Cheap enough to run on every scan:
    /// one pass over the prefab catalogue plus a width measurement per donor.
    /// </summary>
    /// <param name="generated">
    /// Names this mod has written. They are excluded as donors: a generated bridge already wears a
    /// style, and letting it back in as a source for that same style closes a loop where each run
    /// copies structure onto structure.
    /// </param>
    internal static void Rebuild(PrefabSystem prefabSystem, ICollection<string> generated)
    {
        var named = CreateNamedStyles();
        var byId = named.ToDictionary(style => style.Id, StringComparer.Ordinal);
        var extras = new Dictionary<string, BridgeStyle>(StringComparer.Ordinal);
        var sources = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        // Every net that some other prefab hangs underneath itself. Collected before anything is
        // offered, because a lower deck is not a bridge to build from - it is half of one.
        //
        // ExtradosedBridge01 Train Track is the case that named this. Its name contains
        // "extradosedbridge01", so it lands in that style; it carries no AuxiliaryNets of its own, so
        // it passed the single-deck filter; and it carries no structure at all, because the structure
        // belongs to the deck above it. Asking for a single deck bridge of that style therefore built
        // one with no towers rather than refusing, which is the opposite of the intended behaviour and
        // reported itself only as "the tower it derives from ('') is not installed".
        var lowerDecks = new HashSet<PrefabBase>(ReferenceEqualityComparer<PrefabBase>.Instance);
        foreach (var prefab in PrefabCatalog.GetAll(prefabSystem).OfType<NetGeometryPrefab>())
        {
            AuxiliaryNets? nets;
            try
            {
                nets = prefab.GetComponent<AuxiliaryNets>();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var entry in nets?.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
            {
                if (entry?.m_Prefab != null) lowerDecks.Add(entry.m_Prefab);
            }
        }

        var bridgeCapable = 0;
        var donors = 0;
        foreach (var prefab in PrefabCatalog.GetAll(prefabSystem).OfType<NetGeometryPrefab>())
        {
            Bridge? bridge;
            try
            {
                bridge = prefab.GetComponent<Bridge>();
            }
            catch (Exception)
            {
                // A prefab whose components fail to resolve is a broken asset, not a bridge style.
                continue;
            }

            if (bridge == null) continue;
            if (generated.Contains(prefab.name)) continue;

            // Somebody's lower deck. See above.
            if (lowerDecks.Contains(prefab)) continue;

            // Bridge Expansion Pack content is skipped where the base game covers it, which is what
            // it was folded into the game for: offering both shows the player the same bridge twice,
            // and deriving from the pack's copy binds a generated bridge to an asset that can be
            // uninstalled while the vanilla one cannot.
            //
            // Except where the base game covers nothing. Every double deck suspension bridge installed
            // is the pack's; the game's own suspension bridges are all single deck. Skipping those too
            // does not remove a duplicate, it removes the only archetype there is for two decks, and
            // rule 11 then refuses to build one - correctly, and for a reason the exclusion created.
            //
            // A different width is not a capability the game lacks: generating any width from a
            // narrower archetype is what this mod is. A second deck is, because it is a different
            // arrangement rather than the same one stretched.
            if (BridgeStyleDefinitions.IsSupersededPack(prefab.name)
                && prefab.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?.Length is not > 0)
            {
                continue;
            }

            bridgeCapable++;

            var definition = BridgeStyleDefinitions.Match(prefab.name);
            BridgeStyle? style;
            if (definition != null)
            {
                style = byId[definition.Id];
            }
            else
            {
                // Not one of the named styles. Only worth offering if it brings structure of its own -
                // every ordinary road carries a Bridge component too, since that is how any road can
                // be elevated, and listing all of those would bury the real styles.
                if (!HasStructure(prefab, bridge)) continue;

                // Quays, piers and dams are bridge-capable nets that are not bridges: they are built
                // against a shore rather than spanning anything, and offering them as styles fills the
                // list with entries no one would pick.
                if (bridge.m_BuildStyle == BridgeBuildStyle.Quay) continue;

                var family = FamilyOf(prefab.name);
                if (!extras.TryGetValue(family, out style))
                {
                    var label = SpaceOut(family);
                    style = new BridgeStyle(family, family, () => label);
                    extras[family] = style;
                }
            }

            donors++;
            style.Add(new BridgeStyleVariant(prefab, bridge, NetWidth.Of(prefab)));

            if (!sources.TryGetValue(style.Id, out var seen))
            {
                seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                sources[style.Id] = seen;
            }

            seen.Add(SourceOf(prefab));
        }

        var ordered = named
            .Concat(extras.Values.OrderBy(style => style.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        foreach (var style in ordered)
        {
            style.Source = sources.TryGetValue(style.Id, out var seen)
                ? string.Join(", ", seen.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                : string.Empty;
        }

        lock (Gate)
        {
            _styles = ordered;
            _scanned = true;
        }

        Report(ordered, bridgeCapable, donors);
    }

    /// <summary>
    /// Whether an unnamed bridge-capable prefab brings a look of its own. Any one of these is enough
    /// on purpose: the game and the packs do not agree on where the towers live - above the deck, as
    /// anchored sub objects, or driven by fixed-length spans - and requiring a particular one would
    /// quietly drop whole families of real bridges.
    /// </summary>
    private static bool HasStructure(NetGeometryPrefab prefab, Bridge bridge)
    {
        if (bridge.m_FixedSegments is { Length: > 0 }) return true;
        if (prefab.GetComponent<OverheadNetSections>() != null) return true;
        if (prefab.GetComponent<NetSubObjects>() != null) return true;
        if (prefab.GetComponent<MoveableBridge>() != null) return true;
        return false;
    }

    /// <summary>
    /// Groups an unnamed pack's per width prefabs into one family, handling both conventions: the
    /// game numbers its variants ("…01", "…02") and packs spell theirs out after a separator.
    /// </summary>
    internal static string FamilyOf(string name)
    {
        var separator = name.IndexOf(" - ", StringComparison.Ordinal);
        var family = separator > 0 ? name.Substring(0, separator) : name;
        return TrimVariantNumber(family).Trim();
    }

    /// <summary>Drops a trailing variant number: "SuspensionBridge01" and "Lift Bridge 5" alike.</summary>
    private static string TrimVariantNumber(string name)
    {
        var end = name.Length;
        while (end > 0 && char.IsDigit(name[end - 1])) end--;
        // Only a suffix of one or two digits is a variant number. Anything longer is part of the name
        // - a year, a road number - and cutting it would merge styles that are not the same style.
        if (end == name.Length || name.Length - end > 2 || end == 0) return name;
        return name.Substring(0, end).TrimEnd(' ', '_', '-');
    }

    /// <summary>"TrussArchBridge" becomes "Truss Arch Bridge"; text that is already spaced is left alone.</summary>
    private static string SpaceOut(string name)
    {
        if (name.Contains(' ')) return name;

        var builder = new StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(name[index - 1])) builder.Append(' ');
            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Which pack a donor came from, as far as the asset database will say. Only used to tell the
    /// player what an exported bridge will depend on, so an unknown source is not an error.
    /// </summary>
    private static string SourceOf(PrefabBase prefab)
    {
        try
        {
            var path = prefab.asset?.path;
            if (!string.IsNullOrEmpty(path))
            {
                var parts = path!.Replace('\\', '/').Split('/');
                if (parts.Length >= 2) return parts[parts.Length - 2];
            }

            // Built-in prefabs have no asset path. There is no way to read back which pack a built-in
            // bridge belongs to, so say only what is certain.
            return prefab.isBuiltin ? "Base game" : "Unknown";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Writes what discovery saw into the log. This is the one place that can explain a style list the
    /// player did not expect, and it distinguishes the two ways it goes wrong: a style with no
    /// variants at all, versus variants that were found but grouped under the wrong style.
    /// </summary>
    private static void Report(List<BridgeStyle> styles, int bridgeCapable, int donors)
    {
        var installed = styles.Count(style => style.IsInstalled);
        ModHost.Log.Info(
            $"Bridge styles: {installed} of {styles.Count} available, from {donors} donor prefab(s) "
            + $"out of {bridgeCapable} bridge-capable prefab(s); {BridgeMeasurements.Count} recorded widths");
        foreach (var style in styles)
        {
            if (!style.IsInstalled)
            {
                ModHost.Log.Info($"  [{style.Id}] not available - nothing registered provides it");
                continue;
            }

            ModHost.Log.Info(
                $"  [{style.Id}] clearance {style.AuthoredClearance?.ToString() ?? "averaged"}m from {style.Source}: "
                + string.Join(", ", style.Variants.Select(variant =>
                    $"{variant.Name} road {variant.RoadWidth:0.#}m tower {variant.StructureWidth:0.#}m (+{variant.Clearance:0.#}m)")));

            // One line per donor naming its towers individually. The aggregate above says how wide the
            // widest is; this says what they are, which is what a list of towers has to be built from.
            foreach (var variant in style.Variants)
            {
                ModHost.Log.Info($"    towers of {variant.Name}: {variant.DescribeTowers()}");
            }
        }
    }
}
