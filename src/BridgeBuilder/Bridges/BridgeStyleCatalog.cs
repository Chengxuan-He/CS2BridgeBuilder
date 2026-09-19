using BridgeBuilder.Settings;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The bridge styles the player can choose from, and the prefabs that provide them.
///
/// The list itself is fixed - see <see cref="BridgeStyleDefinitions"/> - so the dropdown always reads
/// as a set of named styles whether or not a world has been scanned yet. What discovery contributes
/// is the variants: which registered prefabs provide each style and how wide each one was authored,
/// which is what the width fitting needs. A style with no available variants remains in the internal
/// catalogue for lookup, but is not offered in the scanned UI lists.
///
/// Unknown pack prefabs never create new styles implicitly: an installed prefab
/// is not evidence that this mod implements generation for its design.
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
        // A stored unsupported/removed ID must fail, never silently build a
        // different bridge through the default style.
        if (!string.IsNullOrEmpty(id)) return Find(id);

        lock (Gate)
        {
            return _styles.FirstOrDefault(style => style.IsInstalled) ?? _styles.FirstOrDefault();
        }
    }

    /// <summary>The named styles, with no variants attached yet.</summary>
    private static List<BridgeStyle> CreateNamedStyles()
    {
        return BridgeStyleDefinitions.All
            .Where(definition => BridgeStyleDefinitions.CanGenerate(definition.Id))
            .Select(definition => new BridgeStyle(
                definition.Id,
                definition.NameSuffix,
                () => UiStringCatalog.Current.StyleName(definition.Id),
                definition.Clearance,
                definition.ArchetypeStructureAllowance))
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
            // Except where the base game covers no equivalent design. The blue double-deck suspension
            // bridge is supplied by the pack; the base game's numbered SuspensionBridge02 is the
            // separate grey double-deck design. Skipping the pack entry would therefore remove the
            // only blue two-level archetype rather than a duplicate.
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
            if (definition == null || !byId.TryGetValue(definition.Id, out var style)) continue;

            var variant = new BridgeStyleVariant(prefab, bridge, NetWidth.Of(prefab), style.Id == "GoldenGateDouble");
            if (!BridgeStyleDefinitions.SupportsDeckMode(style.Id, variant.IsDoubleDeck)) continue;
            if (!BridgeStyleDefinitions.AcceptsSource(style.Id, variant.Source.IsBaseGame)) continue;
            style.Add(variant);

            if (variant.IsAvailable)
            {
                donors++;
            }
            else
            {
                ModHost.Log.Info(
                    $"  [{style.Id}] rejected donor '{prefab.name}' from {variant.Source.Label}: "
                    + "the owning DLC/mod prerequisite is unavailable");
            }
        }

        var ordered = named;
        foreach (var style in ordered)
        {
            style.BindSingleSource();
        }
        donors = ordered.Sum(style => style.Variants.Count(variant => variant.IsAvailable));

        lock (Gate)
        {
            _styles = ordered;
            _scanned = true;
        }

        Report(ordered, bridgeCapable, donors);
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
                var unavailable = style.Variants
                    .Where(variant => !variant.IsAvailable)
                    .Select(variant => variant.Source.Label)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(source => source, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                ModHost.Log.Info(unavailable.Count == 0
                    ? $"  [{style.Id}] not available - nothing registered provides it"
                    : $"  [{style.Id}] not available - prerequisite unavailable: "
                        + string.Join(", ", unavailable));
                continue;
            }

            ModHost.Log.Info(
                $"  [{style.Id}] clearance {style.AuthoredClearance?.ToString() ?? "averaged"}m from {style.Source}: "
                + string.Join(", ", style.Variants.Where(variant => variant.IsAvailable).Select(variant =>
                    $"{variant.Name} road {variant.RoadWidth:0.#}m tower {variant.StructureWidth:0.#}m (+{variant.Clearance:0.#}m)")));

            // One line per donor naming its towers individually. The aggregate above says how wide the
            // widest is; this says what they are, which is what a list of towers has to be built from.
            foreach (var variant in style.Variants.Where(variant => variant.IsAvailable))
            {
                ModHost.Log.Info($"    towers of {variant.Name}: {variant.DescribeTowers()}");
            }
        }
    }
}
