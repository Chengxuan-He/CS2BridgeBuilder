using Colossal.Mathematics;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Turns a road that has already been cloned into a standalone prefab into a bridge, by copying the
/// bridge specific parts of a donor onto it.
///
/// What is copied, and what deliberately is not:
///   <see cref="Bridge"/>              span length, sag, whether it may curve, which build style;
///   <see cref="MoveableBridge"/>      the opening mechanism of a draw or lift bridge;
///   <see cref="OverheadNetSections"/> the sections drawn above the deck - towers and cables;
///   <see cref="NetSubObjects"/>       the props anchored along the deck - pylons, portals.
///
/// The road's own <c>m_Sections</c> are left exactly as they are. An earlier version appended the
/// donor's elevated-only sections to get the deck edges too, and that is the one operation here that
/// changes how the net tiles across its own width: the composition system lays sections side by side
/// and computes their offsets, so sections authored for a different net at a different width are not
/// a decoration but a second, conflicting layout. Everything still copied is additive - drawn above
/// or anchored alongside - and cannot disturb the deck itself.
///
/// Towers, cables and their LODs are private derived geometry owned by the generated bridge.
/// Authored components are preserved and references to derived objects are remapped together.
/// Shared materials/effects retain their original references and content prerequisites.
/// </summary>
internal abstract partial class BridgeGeneratorBase
{
    /// <summary>Below this the towers read as fitted; above it the difference is visible on screen.</summary>
    private const float NoticeableWidthDifference = 3f;

    protected readonly ExportReport _report;
    protected readonly TowerFactory? _towers;

    internal abstract string StyleId { get; }
    internal virtual bool StructureFollowsUpperAuxiliary => false;
    protected virtual bool AllTowersUseReferenceDeck => false;
    protected virtual bool WidthFollowsSidewalks => false;

    protected BridgeGeneratorBase(ExportReport report, TowerFactory? towers = null)
    {
        _report = report;
        _towers = towers;
    }

    /// <summary>
    /// Applies <paramref name="style"/> to <paramref name="target"/>, sized for the road's own width.
    /// Returns the variant that was used, or null when the style had nothing usable in it.
    /// </summary>
    internal BridgeStyleVariant? Apply(
        NetGeometryPrefab target, BridgeStyle style, float roadWidth, BridgeOptions options,
        NetGeometryPrefab? measure = null, Func<BridgeStyleVariant, bool>? allow = null)
    {
        // A routed generator must never silently compose another bridge family.
        if (!string.Equals(style.Id, StyleId, StringComparison.Ordinal))
        {
            _report.Failed(target.name, new InvalidOperationException(
                $"Generator '{StyleId}' cannot compose selected style '{style.Id}'."));
            return null;
        }
        // Measure first, then choose. The clone is what the towers actually have to straddle, and a
        // Road Builder road's declared width does not always match the sections it ended up with -
        // so the declared width is not what anything gets selected on.
        //
        // Selecting on one width and sizing on another is not a rounding difference, it is two
        // different bridges: the donor came back carrying the five lane pylon, its cables spaced to
        // match, while the tower was derived from the four lane one. Everything downstream takes the
        // number computed here.
        // Measured the same way a donor is: carriageway only, so the two numbers mean the same thing.
        var breakdown = new List<string>();
        var measuredDeck = measure ?? target;
        var measuredRoad = measuredDeck as RoadPrefab;
        var forRoad = measuredDeck is not TrackPrefab;
        var targetWidth = measuredDeck != null
            ? WidthOf(measuredDeck, roadWidth, breakdown)
            : roadWidth;
        if (!(targetWidth > 0f) || float.IsInfinity(targetWidth))
        {
            _report.Failed(target.name, new InvalidOperationException(
                "The selected road has no valid initialized width; cached widths must not size a bridge."));
            return null;
        }
        var whiteTruss = WidthFollowsSidewalks;
        // The white truss uses the sidewalk edge for its inner structural envelope. Bridges with an
        // authored inner railing need the same complete scan even when their structure has one width:
        // an empty lane outside a sidewalk is not a sidewalk, and the two sides need not match.
        // Road input is independent of bridge family. Every factory receives
        // the outermost selected elevated footway, even when an empty strip or
        // other component lies outside it. Do not gate the scan on railing policy.
        var roadEdges = RoadEdgesOf(measuredRoad, targetWidth);
        var structureEdges = BridgeTowers.StructureEdgesFor(
            style.Id, targetWidth, roadEdges.Left, roadEdges.Right);
        var structureWidth = structureEdges.Width;
        Mod.Log.Info(string.Format(CultureInfo.InvariantCulture,
            "BridgeWidth footways owner='{0}', source='{1}', style='{2}', elevatedRoad={3:R}, "
            + "leftOuter={4:R}, leftInner={5:R}, leftWidth={6:R}, "
            + "rightOuter={7:R}, rightInner={8:R}, rightWidth={9:R}.",
            target.name, measuredDeck?.name, style.Id, targetWidth,
            roadEdges.Left.SidewalkOuterBoundary, roadEdges.Left.InnerBoundary, roadEdges.Left.SidewalkWidth,
            roadEdges.Right.SidewalkOuterBoundary, roadEdges.Right.InnerBoundary, roadEdges.Right.SidewalkWidth));
        var outwardExtension = whiteTruss
            ? NetWidth.OutwardExtensionOf(measuredRoad)
            : 0f;
        var visibleRoadWidth = targetWidth + outwardExtension;
        var outerStructureWidth = BridgeTowers.WhiteTrussArchWidths.OuterTarget(
            style.Id, visibleRoadWidth);

        // Refused rather than attempted. A bridge that is not generated is a bridge the player still
        // has; a bridge generated from an arrangement nobody has measured is one that looks built and
        // behaves as something else.
        var unsupported = BridgeStyleDefinitions.GenerationUnsupportedReason(style.Id, options.DoubleDeck);
        if (unsupported != null)
        {
            _report.Failed(target.name, new NotSupportedException(
                $"'{style.DisplayName}' cannot be generated: {unsupported}."));
            return null;
        }

        var selection = style.Select(targetWidth, forRoad, doubleDeck: options.DoubleDeck, allow);
        var variant = selection.Variant;
        if (variant == null)
        {
            _report.Failed(target.name, new InvalidOperationException(
                options.DoubleDeck
                    ? $"The bridge type '{style.DisplayName}' does not support double deck bridges. "
                        + "Nothing installed provides a double deck version of it to build from. Pick "
                        + "another type, or turn the second deck off."
                    : style.Variants.Any(variant => variant.IsDoubleDeck)
                        ? $"The bridge type '{style.DisplayName}' is a double deck design and cannot "
                            + "be built with one deck. Every archetype it has carries a second deck of "
                            + "its own. Turn the second deck on, or pick a single deck type."
                        : $"The bridge style '{style.DisplayName}' has no usable donor prefab."));
            return null;
        }

        // A donor that carries no structure builds a bridge that is a road on stilts. It used to get
        // through: the lower deck of a double deck pair matches its partner's name, carries no
        // AuxiliaryNets of its own so it passed the single deck filter, and carries no towers because
        // the structure belongs to the deck above it. The catalogue drops lower decks now; this is the
        // check on that, because a bridge with no structure reported itself only as a warning that a
        // tower named '' was not installed.
        if (!HasStructure(variant))
        {
            _report.Failed(target.name, new InvalidOperationException(
                $"'{variant.Name}' carries no structure of its own, so there is nothing to build a "
                + $"'{style.DisplayName}' from. It is a deck rather than a bridge."));
            return null;
        }

        // Asked for two decks and given an archetype with one: the filter should have caught it, so
        // reaching here means a variant claimed to be double deck and carries no arrangement.
        if (options.DoubleDeck && !variant.IsDoubleDeck)
        {
            _report.Failed(target.name, new InvalidOperationException(
                $"'{variant.Name}' was selected for a double deck bridge and carries no lower deck "
                + "arrangement of its own."));
            return null;
        }

        // Write down how the width was arrived at, not just what it came to. This number decides which
        // tower gets built and how far it is stretched, so when it is wrong the report has to say which
        // section made it wrong.
        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: deck measures {1:0.#} m - {2}",
            target.name, targetWidth, string.Join(", ", breakdown)));
        // Most archetypes have one lateral envelope. White TrussArchBridge02 is the exception: its
        // outside frame preserves the prototype's measured bridge-minus-visible-deck relationship,
        // while its inside frame follows the outermost boundary of the two outside footways.
        var chosen = selection.Tower;
        var extra = selection.ExtraFor(structureWidth, style);

        if (WidthFollowsSidewalks)
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: white truss-arch has two width targets: outer {1:0.#####} m is the {2:0.###} m "
                + "visible road width ({3:0.###} m road surface plus {4:0.###} m outward extensions) "
                + "plus the prototype bridge-minus-deck constant {5:0.#####} m "
                + "({11:0.#####} m prototype bridge minus {12:0.###} m prototype visible deck); "
                + "inner {10:0.###} m reaches "
                + "{8:0.###} m left and {9:0.###} m right from x=0. These are the outer boundaries "
                + "of the outermost {6:0.###} m left and {7:0.###} m right footways; a side without "
                + "a footway falls back to its road edge and its outer bridge railing is removed. "
                + "Empty lanes remain road sections and are not counted as footways. The prototype's "
                + "named inner and outer layers are derived "
                + "independently and every LOD uses the same layer assignment.",
                target.name, outerStructureWidth, visibleRoadWidth, targetWidth, outwardExtension,
                BridgeTowers.WhiteTrussArchWidths.PrototypeBridgeMinusDeck,
                roadEdges.Left.SidewalkWidth, roadEdges.Right.SidewalkWidth,
                structureEdges.Left, structureEdges.Right, structureWidth,
                TrussArch02Geometry.PrototypeSectionOuterWidth,
                BridgeTowers.WhiteTrussArchWidths.PrototypeVisibleDeckWidth));
        }

        // No complaint about how far the tower is being widened.
        //
        // There was one: past double the authored road it raised an error saying the tower would not
        // look like the style it came from. That fires on an eight lane road, which is the case this
        // mod exists for - the game has no suspension tower for a 64 m deck, and building one is the
        // point rather than a degradation of it. A widened tower is the same tower with its legs
        // further apart at any width, so there is no threshold past which it stops being one.

        // A deck too narrow for this style's structure. Refused rather than fitted, because a part
        // cannot lose more width than it has: at 16 m the golden bridge's 33.8 m stiffening truss was
        // asked to lose 34 and came out, as the report put it, "0 m across".
        var floor = chosen.HasValue
            ? BridgeCables.NarrowestDeckFor(chosen.Value.Name, chosen.Value.Road)
            : 0f;
        if (floor > 0f && targetWidth < floor)
        {
            _report.Failed(target.name, new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' carries a structure {1:0.#} m across drawn for a {2:0.#} m road, so it cannot be "
                + "fitted to a {3:0.#} m deck - it would have to lose more width than it has. The "
                + "narrowest deck this style can be built on is {4:0.#} m.",
                style.DisplayName, chosen!.Value.Road - floor + 1f, chosen.Value.Road, targetWidth,
                floor)));
            return null;
        }

        CopyBridge(target, style, variant, options);
        CopyPlacement(target, style);
        CopyMoveable(target, variant);
        // Each double-deck archetype records the width of its root road/deck. The root is the upper
        // road when the auxiliary is below, and the lower road when the auxiliary is above. Using
        // that same ownership role in the generated bridge makes the normal target-minus-prototype
        // calculation preserve bridgeWidth-roadWidth without a runtime audit correction.
        var prototypeDecks = variant.LowerDeck;
        var doubleDeckReference = options.DoubleDeck && prototypeDecks != null
            ? variant.RoadWidth
            : 0f;
        var followsUpperAuxiliary = options.DoubleDeck && StructureFollowsUpperAuxiliary;
        if (followsUpperAuxiliary)
        {
            // Use the recorded upper archetype road, not the lower network owning the towers.
            // The selected upper road and this reference must describe the same deck role.
            var upperPrototype = prototypeDecks?.m_Prefab;
            if (upperPrototype == null || !BridgeMeasurements.TryGet(
                    upperPrototype.name, out doubleDeckReference, out _) || doubleDeckReference <= 0f)
            {
                _report.Failed(target.name, new InvalidOperationException(
                    "The grey double suspension upper-deck archetype width is not recorded."));
                return null;
            }
        }
        var structureAllowance = doubleDeckReference > 0f
            ? 0f
            : style.ArchetypeStructureAllowance;

        // Each bridge is sized against its own cables, so the previous bridge's are forgotten
        // before this one's are built. The factory outlives a single bridge; the measurement
        // must not.
        _towers?.BeginBridge(style.Id, structureAllowance, target.name);

        float? primarySourceRoadWidth = null;
        if (chosen.HasValue && doubleDeckReference > 0f)
        {
            extra = PrototypeBridgeSizing.ReferenceDeckExtra(
                targetWidth, doubleDeckReference, extra);
            primarySourceRoadWidth = doubleDeckReference;
            var referenceLevel = followsUpperAuxiliary ? "upper auxiliary" : prototypeDecks!.m_Position.y > TowerWidening.CentreEpsilon
                ? "lower"
                : "upper";
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: double-deck structure width follows the {1} deck: {2:0.###} m target minus "
                + "{3:0.###} m on prototype '{4}' = {5:0.########} m widening. The other deck and "
                + "the road geometry are excluded from the bridge-structure width.",
                target.name, referenceLevel, targetWidth, doubleDeckReference,
                followsUpperAuxiliary ? prototypeDecks!.m_Prefab.name : variant.Name, extra));
        }

        var overheadExtra = BridgeTowers.WhiteTrussArchWidths.OverheadExtra(
            style.Id, outerStructureWidth, extra);
        ReportSpread(
            outerStructureWidth,
            outerStructureWidth - overheadExtra);

        // What the road puts at each edge, which is where the archetype's inner railing stands and
        // whether it stands at all. Measured before anything is derived, because the sections it reads
        // are the road's own and the deck railings are about to be taken off them.
        if (_towers != null)
        {
            _towers.MeasureFootways(roadEdges.Left, roadEdges.Right);
            _towers.MeasureStructureWidths(
                outerStructureWidth, structureEdges.Left, structureEdges.Right);
            _towers.MeasureStructureExtra(extra);
        }

        if (!CopyOverhead(target, variant, style.Id, overheadExtra)) return null;
        CopySubObjects(target, variant, overheadExtra);
        if (target is RoadPrefab roadTarget) RemoveDeckRailings(roadTarget, style.Id);

        // When the style has no tower wide enough, build one. Everything above still applies - the
        // span behaviour, the cables, the deck props that do fit - and only the tower is replaced,
        // because the tower is the one part whose width nothing can adjust after the fact.
        var towerWidth = WidthFollowsSidewalks
            ? outerStructureWidth
            : structureWidth;
        Mod.Log.Info(string.Format(CultureInfo.InvariantCulture,
            "BridgeWidth owner='{0}', source='{1}', style='{2}', cached={3:R}, measured={4:R}, "
            + "structure={5:R}, towerArgument={6:R}, overheadExtra={7:R}, prototype='{8}'.",
            target.name, measuredDeck?.name ?? "(unavailable)", style.Id, roadWidth, targetWidth,
            structureWidth, towerWidth, overheadExtra, variant.Name));
        var fitted = FitTower(
            target, style, towerWidth, variant, chosen, primarySourceRoadWidth);
        if (!fitted) ReportTooNarrow(target.name, towerWidth, variant.StructureWidth);

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: style '{1}' from '{2}', variant '{3}' - deck {4:0.#} m, tower {5:0.#} m "
            + "(clearance {6:0.#} m), carrying a {7:0.#} m deck. Tower covers it by {8:0.#} m.",
            target.name, style.DisplayName, style.Source, variant.Name,
            variant.Width, variant.StructureWidth, variant.Clearance, targetWidth,
            variant.StructureWidth - targetWidth));

        return variant;
    }

}
