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
/// The donor is referenced, not copied, so the geometry keeps belonging to the content that shipped
/// it and the generated asset needs that content installed. Copying the meshes instead would produce
/// an asset this mod has no right to redistribute.
/// </summary>
internal sealed class BridgeComposer
{
    /// <summary>Below this the towers read as fitted; above it the difference is visible on screen.</summary>
    private const float NoticeableWidthDifference = 3f;

    private readonly ExportReport _report;
    private readonly TowerFactory? _towers;

    internal BridgeComposer(ExportReport report, TowerFactory? towers = null)
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
        RoadPrefab? measure = null, Func<BridgeStyleVariant, bool>? allow = null)
    {
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
        var measuredRoad = measure ?? target as RoadPrefab;
        var targetWidth = measuredRoad != null
            ? WidthOf(measuredRoad, roadWidth, breakdown)
            : roadWidth;
        var whiteTruss = BridgeTowers.WidthFollowsSidewalks(style.Id);
        var roadEdges = RoadEdgesOf(measuredRoad, targetWidth, whiteTruss);
        var structureEdges = BridgeTowers.StructureEdgesFor(
            style.Id, targetWidth, roadEdges.Left, roadEdges.Right);
        var structureWidth = structureEdges.Width;
        var outwardExtension = whiteTruss
            ? NetWidth.OutwardExtensionOf(measuredRoad)
            : 0f;
        var visibleRoadWidth = targetWidth + outwardExtension;
        var outerStructureWidth = BridgeTowers.WhiteTrussArchWidths.OuterTarget(
            style.Id, visibleRoadWidth);

        // Refused rather than attempted. A bridge that is not generated is a bridge the player still
        // has; a bridge generated from an arrangement nobody has measured is one that looks built and
        // behaves as something else.
        var deferred = BridgeStyleDefinitions.DeferredReason(style.Id);
        if (deferred != null)
        {
            _report.Failed(target.name, new NotSupportedException(
                $"'{style.DisplayName}' is not generated yet: {deferred}."));
            return null;
        }

        var selection = style.Select(targetWidth, forRoad: true, doubleDeck: options.DoubleDeck, allow);
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
        var extra = selection.ExtraFor(structureWidth, style.Id);

        if (BridgeTowers.WidthFollowsSidewalks(style.Id))
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
        // Each bridge is sized against its own cables, so the previous bridge's are forgotten
        // before this one's are built. The factory outlives a single bridge; the measurement
        // must not.
        _towers?.BeginBridge(style.Id, target.name);

        // Follow the selected archetype's deck roles. When its auxiliary net is below, the donor's
        // main prefab is its upper road. This is the V-shaped double-deck cable-stayed bridge: its
        // widening is target upper-road width minus prototype upper-road width. The portal opening is
        // clearance around that road, not its width, and the lower road or track is never consulted.
        var prototypeDecks = variant.LowerDeck;
        if (chosen.HasValue
            && options.DoubleDeck
            && prototypeDecks != null
            && prototypeDecks.m_Position.y < -TowerWidening.CentreEpsilon)
        {
            var roadExtra = PrototypeBridgeSizing.UpperDeckExtra(
                targetWidth, chosen.Value.Road, extra);
            extra = BridgeTowers.StructureExtraFor(style.Id, roadExtra);
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: V-shaped double-deck width follows the upper road: {1:0.###} m target minus "
                + "{2:0.###} m on prototype '{3}' = {4:0.###} m road difference, plus the prototype's "
                + "{5:0.###} m structure allowance = {6:0.###} m effective widening. Its lower "
                + "network keeps the auxiliary pointer and is not a width input.",
                target.name, targetWidth, chosen.Value.Road, variant.Name, roadExtra,
                BridgeTowers.BonusFor(style.Id), extra));
        }
        else
        {
            extra = BridgeTowers.StructureExtraFor(style.Id, extra);
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

        CopyOverhead(target, variant, overheadExtra);
        CopySubObjects(target, variant, overheadExtra);
        if (target is RoadPrefab roadTarget) RemoveDeckRailings(roadTarget, style.Id);

        // When the style has no tower wide enough, build one. Everything above still applies - the
        // span behaviour, the cables, the deck props that do fit - and only the tower is replaced,
        // because the tower is the one part whose width nothing can adjust after the fact.
        var towerWidth = BridgeTowers.WidthFollowsSidewalks(style.Id)
            ? outerStructureWidth
            : structureWidth;
        var fitted = FitTower(target, style, towerWidth, variant, chosen);
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


    /// <summary>
    /// Replaces the style's tower with one derived from it, sized for this deck.
    ///
    /// Always, not only when the style's own is too narrow. Deriving unconditionally is what makes the
    /// result checkable: at the width the source tower was authored for the shift is zero, so the
    /// generated mesh is that tower vertex for vertex, and a bridge over a road the game already has a
    /// bridge for gets exactly that bridge's tower. A rule that only fires sometimes could not be
    /// tested that way.
    ///
    /// The source tower is removed as the generated one goes in. Adding alongside would stand two
    /// towers at every span - the authored one at its own width and the derived one at the road's.
    /// </summary>
    private bool FitTower(
        NetGeometryPrefab target,
        BridgeStyle style,
        float deckWidth,
        BridgeStyleVariant variant,
        BridgeTowers.Tower? chosen)
    {
        if (_towers == null) return false;

        // A style whose structure is not derived. Skipped and said so, rather than falling back to
        // whatever object the ranking turns up: on a covered bridge that object is a 3.3 m pier under
        // the path, and widening it would put a support where the housing belongs.
        var notDerived = BridgeTowers.NotDerivedReason(style.Id);
        if (notDerived != null)
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: the style's own structure is used unchanged - {1}.",
                target.name, notDerived));
            return true;
        }


        var subObjects = target.AddOrGetComponent<NetSubObjects>();
        var existing = subObjects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();

        // Every one of this style's structures that the bridge names, not only the one that was chosen.
        //
        // A style can name more than one. The golden bridge carries a pylon at each end of its course
        // and a pier at every node between, both 50.4 m across; deriving only the pylon left three
        // fifty metre structures standing beside one forty metre one on the same bridge. That reads as
        // pillars the generator invented and is the opposite - they are the ones it failed to take
        // over - and it is also what made the generated tower look too narrow, because what it was
        // being compared against was the donor's own structure at the donor's own width.
        //
        // The chosen one is primary and keeps the plain name; the rest carry their archetype's name,
        // because two structures of one bridge cannot share a key that knows only the style and width.
        var sourceName = chosen?.Name ?? WidestTowerName(variant);
        var named = existing
            .Where(info => info?.m_Object != null && BridgeTowers.IsTower(style.Id, info.m_Object.name))
            .Select(info => info!.m_Object!.name)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (!named.Contains(sourceName, StringComparer.Ordinal)) named.Insert(0, sourceName);

        var derived = new Dictionary<string, ObjectPrefab>(StringComparer.Ordinal);
        foreach (var name in named)
        {
            var primary = string.Equals(name, sourceName, StringComparison.Ordinal);
            var recordedRoad = BridgeTowers.RoadFor(style.Id, name);
            var road = primary
                ? chosen?.Road
                    ?? (BridgeTowers.WidthFollowsSidewalks(style.Id)
                        ? recordedRoad ?? deckWidth
                        : deckWidth)
                : recordedRoad ?? chosen?.Road ?? deckWidth;

            var built = _towers.Create(style.Id, name, road, deckWidth, primary);
            if (built != null)
            {
                derived[name] = built;
                continue;
            }

            if (primary) return false;

            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "{0} keeps '{1}' at the donor's own width beside the generated {2:0.#} m structure, "
                + "because it could not be derived. The bridge wears structures of two widths.",
                target.name, name, deckWidth));
        }

        var tower = derived[sourceName];

        // One generated entry per entry taken over, each carrying its own arrangement across, so a
        // pylon at the course end stays at the course end and a pier at a node stays at its node.
        var kept = new List<NetSubObjectInfo>();
        var bound = new List<NetSubObjectInfo>();
        NetSubObjectInfo? replaced = null;
        foreach (var info in existing)
        {
            if (info?.m_Object != null && derived.TryGetValue(info.m_Object.name, out var replacement))
            {
                replaced ??= info;
                bound.Add(BindTower(replacement, style, info));
                continue;
            }

            if (info != null) kept.Add(info);
        }

        // Nothing to take over: the bridge had no structure of its own, so one is placed from the
        // recorded arrangement.
        if (bound.Count == 0) bound.Add(BindTower(tower, style, null));

        var spacing = replaced?.m_Spacing
            ?? (variant.Bridge.m_SegmentLength > 0f ? variant.Bridge.m_SegmentLength : 64f);

        subObjects.m_SubObjects = kept.Concat(bound).ToArray();
        subObjects.active = true;

        _report.Note(
            $"{target.name}: {(replaced == null ? "added" : $"replaced {bound.Count} entr"
                + (bound.Count == 1 ? "y" : "ies") + " naming " + derived.Count + " structure"
                + (derived.Count == 1 ? "" : "s") + " with")} a "
            + $"{deckWidth:0.#} m tower at {spacing:0.#} m spacing.");
        return true;
    }

    /// <summary>
    /// How high the bridge may be built.
    ///
    /// The archetype allows nothing below ground and up to two hundred metres above it. The road being
    /// converted brings its own range - a hundred either way, because a road may also be a tunnel - and
    /// keeping it lets the bridge be placed at heights its tower was never drawn for, and below ground
    /// where a suspension tower means nothing at all.
    /// </summary>
    private void CopyPlacement(NetGeometryPrefab target, BridgeStyle style)
    {
        var archetype = BridgeSpec.For(style.Id);
        if (!archetype.HasValue) return;

        var recorded = archetype.Value;
        var placeable = target.AddOrGetComponent<PlaceableNet>();
        placeable.m_ElevationRange = new Bounds1(recorded.ElevationMin, recorded.ElevationMax);
        placeable.m_AllowParallelMode = recorded.AllowParallelMode;
        placeable.m_XPReward = recorded.XpReward;
        placeable.active = true;

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: buildable from {1:0.#} m to {2:0.#} m, as '{3}' is.",
            target.name, recorded.ElevationMin, recorded.ElevationMax, recorded.MeasuredFrom));
    }

    /// <summary>
    /// The entry that anchors the tower to the deck, built by walking every field the game defines and
    /// asking the archetype for each one.
    ///
    /// Written this way because the alternative kept failing in the same manner. An entry assembled by
    /// naming the fields that seemed to matter left three of the twelve untouched and one hardcoded,
    /// and a field nobody writes is a field nobody compares - it takes whatever a default-constructed
    /// entry holds and nothing says so. Reflection makes the set of fields the game's business rather
    /// than this code's, and a field the archetype has no recorded value for is reported instead of
    /// being filled in with a guess.
    /// </summary>
    private NetSubObjectInfo BindTower(ObjectPrefab tower, BridgeStyle style, NetSubObjectInfo? replaced)
    {
        var entry = new NetSubObjectInfo
        {
            m_Object = tower,
            m_Position = new float3(0f, TowerHeight(style, replaced), 0f),
        };

        var missing = new List<string>();
        var recorded = BridgeSpec.TowerBinding;

        foreach (var field in SerializedFields.Of(typeof(NetSubObjectInfo)))
        {
            if (field.Name == nameof(NetSubObjectInfo.m_Object)
                || field.Name == nameof(NetSubObjectInfo.m_Position))
            {
                continue;
            }

            // The entry being replaced is the archetype's own, so it is carried across whole. Only the
            // object it names changes, because only the object is what this mod generates.
            //
            // It used to be rebuilt from a recorded table instead, and the table held one family's
            // arrangement. Every bridge therefore got the suspension bridge's EdgeMiddle - one tower at
            // the middle of every span - whatever its own arrangement was. An arch bridge's pier stands
            // at a node, between one arch and the next, so it was planted in the middle of an arch. The
            // golden bridge carries its towers at the ends of the course and got one per span instead.
            // Nothing reported it, because a recorded value is not a missing value.
            if (replaced != null)
            {
                field.SetValue(entry, field.GetValue(replaced));
                continue;
            }

            if (!recorded.TryGetValue(field.Name, out var value))
            {
                missing.Add(field.Name);
                continue;
            }

            field.SetValue(entry, Convert(value, field.FieldType));
        }

        if (missing.Count > 0)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "The tower binding has no recorded value for {0}, and there was no entry to carry "
                + "across. Those fields keep whatever an empty entry holds, which is not the "
                + "archetype's arrangement and is how a tower ends up anchored differently from the "
                + "bridge it was derived from.",
                string.Join(", ", missing)));
        }

        // Named in the report because it is the field that decides where the structure stands, it
        // varies by family, and until now nothing said what any family used.
        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: placed as {1} at index {2}, spacing {3:0.#}{4}.",
            tower.name, entry.m_Placement, entry.m_FixedIndex, entry.m_Spacing,
            replaced == null
                ? " - from the recorded arrangement, because the bridge had no tower to replace"
                : $" - carried across from '{replaced.m_Object?.name}'"));

        return entry;
    }

    /// <summary>
    /// One recorded value as the field's own type. The archetype holds engine-free values - an enum as
    /// its number, a rotation as four components - so that the tests can read them without the game.
    /// </summary>
    private static object Convert(object value, Type type)
    {
        if (type.IsEnum) return Enum.ToObject(type, value);
        if (type == typeof(quaternion) && value is float[] { Length: 4 } q)
        {
            return new quaternion(q[0], q[1], q[2], q[3]);
        }

        return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// How far up the tower the deck sits.
    ///
    /// From the recorded archetype where there is one. Where there is not, the entry being replaced is
    /// the only thing that knows, and using it is a documented fallback rather than the rule - the same
    /// arrangement as the bridge behaviour, and reported the same way.
    /// </summary>
    private float TowerHeight(BridgeStyle style, NetSubObjectInfo? replaced)
    {
        var archetype = BridgeSpec.For(style.Id);
        if (archetype.HasValue) return archetype.Value.TowerHeightAboveOrigin;

        if (replaced != null) return replaced.m_Position.y;

        _report.Warning(string.Format(
            CultureInfo.InvariantCulture,
            "'{0}' has no recorded tower height and nothing to read one from, so its tower is anchored "
            + "at the deck. It will stand on the road rather than through it.",
            style.DisplayName));
        return 0f;
    }

    /// <summary>The name of the widest structural prop on a donor: the tower a copy is derived from.</summary>
    private static string WidestTowerName(BridgeStyleVariant variant)
    {
        var widest = string.Empty;
        var width = 0f;
        var subObjects = variant.SubObjects;
        foreach (var info in subObjects?.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
        {
            if (info?.m_Object is not ObjectGeometryPrefab geometry) continue;
            var span = geometry.m_Meshes?
                .Select(mesh => mesh?.m_Mesh as RenderPrefab)
                .Where(mesh => mesh != null)
                .Select(mesh => mesh!.bounds.max.x - mesh.bounds.min.x)
                .DefaultIfEmpty(0f)
                .Max() ?? 0f;
            if (span <= width) continue;
            width = span;
            widest = geometry.name;
        }

        return widest;
    }
    /// <summary>
    /// How much wider this deck is than the one the donor's parts were authored for.
    ///
    /// Returned as the full difference; each part moves half of it, out to its own side. This was a
    /// ratio once, and props were multiplied by it. That is wrong for anything that has to line up with
    /// the tower, and it is wrong in a way that grows with the offset: a cable 13 m out and a leg 12 m
    /// out get pushed apart by scaling, however close they started. A translation moves them together,
    /// and at the tower's own width it moves nothing at all - the same property the generated mesh is
    /// tested against.
    ///
    /// It does nothing for the tower itself: every pylon sits at (0, 0, 0) and its width lives in its
    /// mesh. That is <see cref="FitTower"/>'s problem, solved by generating one.
    /// </summary>
    private void ReportSpread(float targetWidth, float authoredWidth)
    {
        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "Deck is {0:0.#} m against the {1:0.#} m these parts were authored for: everything to the "
            + "side moves out {2:0.#} m.",
            targetWidth, authoredWidth, (targetWidth - authoredWidth) * 0.5f));
    }

    /// <summary>
    /// Says that the towers do not span the road, on a bridge that was still generated.
    ///
    /// Only reached when no tower could be built for this deck. Raised at error level so it reaches
    /// the player in the game, and carrying no failure count, because the bridge exists and is usable.
    /// </summary>
    private void ReportTooNarrow(string name, float deckWidth, float towerWidth)
    {
        if (towerWidth <= 0f || deckWidth - towerWidth <= NoticeableWidthDifference) return;

        _report.Defect(string.Format(
            CultureInfo.InvariantCulture,
            "'{0}' was generated, but its towers are too narrow for it: the deck is {1:0.#} m and the "
            + "widest tower available is {2:0.#} m, so the road hangs {3:0.#} m out past it. A tower "
            + "wide enough could not be built for this style either. Pick another style, or use the "
            + "bridge as it is.",
            name, deckWidth, towerWidth, deckWidth - towerWidth));
    }

    /// <summary>
    /// The bridge behaviour: span length, sag, how it meets water, whether it may curve.
    ///
    /// From the recorded archetype where there is one, and from the donor where there is not.
    ///
    /// The distinction matters when the donor is missing. A road can be converted with none of the
    /// style's content installed - the widths are recorded, so the tower can still be built - and a
    /// bridge that then takes its span length from a prefab that is not there gets whatever a default
    /// constructed component holds, which for m_SegmentLength is zero. Recorded values do not have that
    /// failure mode, which is the whole reason they are recorded.
    ///
    /// Only the suspension family has been measured. Falling back is reported rather than silent,
    /// because a bridge built from unmeasured numbers is as wrong as one built from invented numbers
    /// and the only difference is whether anyone knows which.
    /// </summary>
    private void CopyBridge(
        NetGeometryPrefab target, BridgeStyle style, BridgeStyleVariant variant, BridgeOptions options)
    {
        var source = variant.Bridge;
        var archetype = BridgeSpec.For(style.Id);
        var bridge = target.AddOrGetComponent<Bridge>();

        // The donor's own behaviour, whenever there is a donor.
        //
        // The recorded archetype exists for when there is not - rule 2's case, a style whose prefab is
        // not installed - and reaching for it while the prefab is in hand is the mistake rule 2's own
        // note warns about: what is present is carried, not recalled. One style's record is not one
        // style's variants. The suspension archetype records a 256 m span, measured on the single deck
        // bridge; the double deck bridge of the same style spans 320. Forcing 256 onto it put the
        // towers 256 m apart while the cables were drawn for 320, and the two disagreed at every node.
        var recorded = archetype;
        if (source != null)
        {
            bridge.m_SegmentLength = source.m_SegmentLength;
            bridge.m_Hanging = source.m_Hanging;
            bridge.m_ElevationOnWater = source.m_ElevationOnWater;
            bridge.m_CanCurve = source.m_CanCurve;
            bridge.m_AllowMinimalLength = source.m_AllowMinimalLength;

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: bridge behaviour carried from '{1}' - spans {2:0.#} m, sag {3:0.##}, {4:0.#} m "
                + "over water.",
                target.name, variant.Name, source.m_SegmentLength, source.m_Hanging,
                source.m_ElevationOnWater));

            // The record is still worth checking against, because a variant that disagrees with its
            // own style's archetype is either a different design or a mismeasurement, and either way
            // it is worth saying which bridge the numbers came from.
            if (recorded.HasValue
                && Math.Abs(recorded.Value.SegmentLength - source.m_SegmentLength) > 1f)
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: '{1}' spans {2:0.#} m where the recorded archetype '{3}' spans {4:0.#}. The "
                    + "donor's own is used, because it is the bridge this one is derived from.",
                    target.name, variant.Name, source.m_SegmentLength,
                    recorded.Value.MeasuredFrom, recorded.Value.SegmentLength));
            }
        }
        else if (recorded.HasValue)
        {
            bridge.m_SegmentLength = recorded.Value.SegmentLength;
            bridge.m_Hanging = recorded.Value.Hanging;
            bridge.m_ElevationOnWater = recorded.Value.ElevationOnWater;
            bridge.m_CanCurve = recorded.Value.CanCurve;
            bridge.m_AllowMinimalLength = recorded.Value.AllowMinimalLength;

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: bridge behaviour from the recorded archetype - spans {1:0.#} m, measured from "
                + "'{2}'. The donor carries none of its own.",
                target.name, recorded.Value.SegmentLength, recorded.Value.MeasuredFrom));
        }
        else
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': neither '{1}' nor any recorded archetype says how this style behaves as a "
                + "bridge, so it keeps whatever the road had.",
                target.name, variant.Name));
        }

        // Still from the donor: these say which content this bridge is made of rather than how it
        // behaves, and there is nothing to record them as. Guarded, because the branch above admits a
        // donor that carries no Bridge of its own.
        if (source != null)
        {
            bridge.m_WaterFlow = source.m_WaterFlow;
            bridge.m_FixedSegments = CopyFixedSegments(source);
        }

        bridge.m_BuildStyle = options.BuildStyle ?? source?.m_BuildStyle ?? bridge.m_BuildStyle;
        bridge.active = true;
        CopyAggregate(target, variant);
    }

    /// <summary>
    /// Takes the donor's aggregate type, so a placed bridge is named as a bridge.
    ///
    /// The aggregate is what joins consecutive segments into one named road, and it carries the pool
    /// the game draws that name from. A road cloned from a street keeps the street's aggregate, so
    /// the finished bridge was christened "...街" - correct for what it was cloned from, wrong for
    /// what it became. The donor is a real bridge, so its aggregate names bridges.
    ///
    /// A donor without an aggregate leaves the deck's own alone: an aggregate of null would stop the
    /// segments joining up at all, which is a worse result than an unfitting name.
    /// </summary>
    private void CopyAggregate(NetGeometryPrefab target, BridgeStyleVariant variant)
    {
        var aggregate = variant.Donor.m_AggregateType;
        if (aggregate == null)
        {
            _report.Note(
                $"{target.name}: the style has no aggregate type, so the deck keeps its own and the "
                + "placed bridge will be named after the road it came from.");
            return;
        }

        if (ReferenceEquals(target.m_AggregateType, aggregate)) return;

        var previous = target.m_AggregateType?.name ?? "none";
        target.m_AggregateType = aggregate;
        _report.Note($"{target.name}: named as '{aggregate.name}' rather than '{previous}'.");
    }

    /// <summary>
    /// Copies the style's fixed spans, whole.
    ///
    /// An earlier version kept only the spans whose set states the deck's own sections respond to,
    /// reasoning that a span nothing draws is a span laid out around nothing. On a Road Builder road
    /// that condition is met by none of them, so the array came back empty - and the towers went with
    /// it, because a suspension bridge anchors its pylons to the ends of its main span through
    /// <see cref="NetSubObjectInfo.m_FixedIndex"/>. Every prop then indexed past the end of an empty
    /// array, every prop was dropped, and the bridge came out bare.
    ///
    /// So the array is copied intact. That keeps every index the props use valid, which is what makes
    /// them safe - the danger was never the spans themselves but an index reaching past them, and a
    /// compiled job reads that index without a bounds check. The deck still builds as ordinary spans
    /// where the style would have drawn a main one, because the sections that draw it stay with the
    /// donor; the structure at least stands in the right places.
    /// </summary>
    private FixedNetSegmentInfo[] CopyFixedSegments(Bridge source)
    {
        var segments = source.m_FixedSegments;

        // Empty, not null. The archetype carries an empty array and a generated bridge carried null,
        // which is a different thing to anything that reads it without checking first - and that kind
        // of difference surfaces as a crash somewhere else rather than as a bridge that looks wrong.
        if (segments == null || segments.Length == 0) return Array.Empty<FixedNetSegmentInfo>();

        return segments.Where(segment => segment != null).Select(Copy).ToArray();
    }

    private static FixedNetSegmentInfo Copy(FixedNetSegmentInfo segment) => new()
    {
        m_CountRange = segment.m_CountRange,
        m_Length = segment.m_Length,
        m_CanCurve = segment.m_CanCurve,
        m_SetState = Copy(segment.m_SetState),
        m_UnsetState = Copy(segment.m_UnsetState),
    };

    /// <summary>
    /// Carries over the opening mechanism of a draw or lift bridge. Without this, converting one of
    /// those would produce a bridge that looks like it should open and never does - the lift geometry
    /// is in the donor's sections either way, so the component is what makes it move rather than what
    /// makes it visible.
    /// </summary>
    private static void CopyMoveable(NetGeometryPrefab target, BridgeStyleVariant variant)
    {
        var source = variant.Donor.GetComponent<MoveableBridge>();
        if (source == null) return;

        var moveable = target.AddOrGetComponent<MoveableBridge>();
        moveable.m_LiftOffsets = source.m_LiftOffsets;
        moveable.m_MovingTime = source.m_MovingTime;
        moveable.active = true;
    }





    /// <summary>
    /// Whether a donor brings structure of its own: something over the deck, or something under it
    /// that the road runs between.
    ///
    /// A lower deck brings neither - the towers belong to the deck above it - and building from one
    /// produces a road on stilts that reports nothing worse than a warning.
    /// </summary>
    private static bool HasStructure(BridgeStyleVariant variant)
    {
        if (variant.Overhead?.m_Sections?.Length > 0) return true;

        var subObjects = variant.SubObjects?.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();
        foreach (var info in subObjects)
        {
            if (info?.m_Object != null && info.m_Object.Has<PillarObject>()) return true;
        }

        return false;
    }


    /// <summary>
    /// The carriageway a bridge is sized against, and how it was arrived at.
    ///
    /// Normally the prefab being built. It is a separate argument because a double deck bridge whose
    /// second net hangs above is built on the deck the player chose, while the road they are
    /// converting is the one hung above it - and the structure still has to be sized against their
    /// road, because that is the one whose width varies. Measuring the prefab in hand would size the
    /// towers against whatever deck was picked to go underneath.
    /// </summary>
    internal static float WidthOf(RoadPrefab prefab, float fallback, List<string>? breakdown = null)
    {
        var width = NetWidth.RoadSurfaceOf(prefab, breakdown);
        if (width <= 0f) width = NetWidth.Of(prefab);
        return width > 0f ? width : fallback;
    }


    /// <summary>
    /// Leaves the road's own railing only where the road ends, for a style whose archetype brings one
    /// along the run.
    ///
    /// The golden suspension bridge carries its railings in its own support mesh - they are golden,
    /// and they are part of the bridge rather than of the road under it. The road brings a railing too,
    /// because an elevated road always does, and the two stand beside each other: a white one against
    /// a golden one, a hand's breadth apart, on a bridge where the archetype has only the golden.
    ///
    /// Found by what the piece is rather than by what it is called. A piece on the side layer whose
    /// declared height reaches above the deck surface is standing on the deck: that is a railing, or a
    /// parapet, or a barrier. One that stays below is the fascia or the shoulder, and it holds the
    /// edge together.
    ///
    /// And by the state it is drawn for. Only the piece for the elevated run is touched, and it is
    /// gated to the road's ends rather than removed - a turnaround is elevated deck like any other,
    /// the bridge carries no railing of its own there, and no other piece draws there either.
    ///
    /// Which styles bring their own is recorded rather than inferred. Nothing in an archetype says "I
    /// have railings"; it was seen on these two, and a style not on this list keeps the road's.
    /// </summary>

    /// <summary>
    /// The actual outer section boundary at each edge of the target road, and both boundaries of the
    /// outermost sidewalk selected for that side.
    ///
    /// Sections are laid out across the road in order. Ordinary styles keep their established rule
    /// of inspecting the first and last section after outward extensions are removed. The white truss
    /// scans inward only as far as x=0 for the first actual sidewalk on each side, so an empty lane is
    /// not mistaken for one and a one-sided sidewalk is not mirrored onto the other side.
    ///
    /// Which of the two is the left was got wrong twice. The list order is a convention about how the
    /// road was written down, the mesh has its own axis, and nothing in either says which way round
    /// they are - so when they disagree the side with the footway is treated as the side without, its
    /// railing is taken away, and the other keeps one it should not have. That is what was seen, and
    /// the direction below is that observation rather than a derivation.
    ///
    /// Asked of each side separately. A road with a footway on one side and a shoulder on the other is
    /// an ordinary thing, and its bridge has one inner railing.
    /// </summary>
    private static (RoadEdge Left, RoadEdge Right) RoadEdgesOf(
        RoadPrefab? target, float fallbackWidth, bool findOutermostSidewalk)
    {
        var sections = target?.m_Sections;
        if (sections == null)
        {
            var fallbackEdge = Math.Max(0f, fallbackWidth * 0.5f);
            return (
                new RoadEdge(fallbackEdge, fallbackEdge, isSidewalk: false),
                new RoadEdge(fallbackEdge, fallbackEdge, isSidewalk: false));
        }

        // Laid out across the road in order, so a section's place is where the ones before it end.
        var counted = new List<(NetSectionPrefab Section, float Start, float Width)>();
        var total = 0f;
        foreach (var info in sections)
        {
            var section = info?.m_Section;
            if (section == null) continue;
            if (SectionNames.IsSide(section.name)) continue;

            var width = NetWidth.Of(section);
            if (width <= 0f) continue;

            counted.Add((section, total, width));
            total += width;
        }

        var outerBoundary = total > 0f ? total * 0.5f : Math.Max(0f, fallbackWidth * 0.5f);
        if (counted.Count == 0)
        {
            return (
                new RoadEdge(outerBoundary, outerBoundary, isSidewalk: false),
                new RoadEdge(outerBoundary, outerBoundary, isSidewalk: false));
        }

        static RoadEdge EdgeOf(
            (NetSectionPrefab Section, float Start, float Width) entry,
            float outer)
        {
            // Empty sections, lanes and shoulders are explicitly not footways. Only the width of an
            // outermost section identified as Sidewalk is subtracted from the measured outer boundary.
            var sidewalk = SectionNames.IsSidewalk(entry.Section.name);
            return new RoadEdge(
                outer,
                sidewalk ? outer - entry.Width : outer,
                sidewalk);
        }

        static RoadEdge OutermostSidewalkOf(
            IReadOnlyList<(NetSectionPrefab Section, float Start, float Width)> entries,
            float outer,
            bool positive)
        {
            if (positive)
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    if (entry.Start >= outer) break;
                    if (!SectionNames.IsSidewalk(entry.Section.name)) continue;

                    var sidewalkOuter = Math.Max(0f, outer - entry.Start);
                    var sidewalkInner = Math.Max(
                        0f, outer - Math.Min(outer, entry.Start + entry.Width));
                    return new RoadEdge(
                        outer, sidewalkOuter, sidewalkInner, isSidewalk: true);
                }
            }
            else
            {
                for (var index = entries.Count - 1; index >= 0; index--)
                {
                    var entry = entries[index];
                    var end = entry.Start + entry.Width;
                    if (end <= outer) break;
                    if (!SectionNames.IsSidewalk(entry.Section.name)) continue;

                    var sidewalkOuter = Math.Max(0f, end - outer);
                    var sidewalkInner = Math.Max(0f, entry.Start - outer);
                    return new RoadEdge(
                        outer, sidewalkOuter, sidewalkInner, isSidewalk: true);
                }
            }

            return new RoadEdge(outer, outer, isSidewalk: false);
        }

        // Observed game convention: the first non-side-extension section is positive mesh x and the
        // last is negative mesh x. TowerFactory calls negative x "left" and positive x "right";
        // preserve that mapping here. The boundary values themselves are absolute distances.
        return findOutermostSidewalk
            ? (
                OutermostSidewalkOf(counted, outerBoundary, positive: false),
                OutermostSidewalkOf(counted, outerBoundary, positive: true))
            : (
                EdgeOf(counted[counted.Count - 1], outerBoundary),
                EdgeOf(counted[0], outerBoundary));
    }

    private void RemoveDeckRailings(RoadPrefab target, string? styleId)
    {
        if (!BridgeTowers.BringsItsOwnRailings(styleId)) return;

        // Nothing to derive a copy with, so nothing to take off: the shared section is left alone
        // rather than edited, which would take the railing off every road in the game.
        if (_towers == null) return;

        var sections = target.m_Sections;
        if (sections == null) return;

        var removed = new List<string>();
        foreach (var info in sections)
        {
            if (info == null || info.m_Section == null) continue;

            var section = info.m_Section;

            var without = _towers.WithoutDeckPieces(
                section,
                BridgeNaming.SectionName(target.name, section.name),
                DeckSurface,
                out var taken);

            if (without == null) continue;

            info.m_Section = without;
            removed.AddRange(taken);
        }

        if (removed.Count == 0) return;

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: the road's own railing now draws only where the road ends - {1}. This style carries "
            + "railings of its own along the run, and the two stood beside each other; at a turnaround "
            + "it carries none, so the road's is the only one there.",
            target.name, string.Join(", ", removed.Distinct())));
    }

    /// <summary>
    /// How far above the deck a side piece has to reach before it is something standing on the deck
    /// rather than something holding its edge together.
    ///
    /// The shoulder's side piece tops out at 0.2 m below; the elevated edge with its railing reaches
    /// 0.5 m above. There is no case anywhere near this line, which is why a line will do.
    /// </summary>
    private const float DeckSurface = 0.25f;

    private void CopyOverhead(NetGeometryPrefab target, BridgeStyleVariant variant, float extra)
    {
        // The caller has already folded the prototype's structural allowance into this number. The
        // same effective extra also goes to node-bound props, while TowerFactory independently derives
        // the identical number from target and prototype widths. Applying a bonus here again would
        // put cables one half-bonus outside the nodes and tower they are meant to meet.

        var source = variant.Overhead;
        if (source?.m_Sections == null || source.m_Sections.Length == 0) return;

        var overhead = target.AddOrGetComponent<OverheadNetSections>();
        overhead.m_Sections = source.m_Sections
            .Where(section => section?.m_Section != null)
            .Select(section => Widened(section, target.name, extra))
            .ToArray();
        overhead.active = true;
    }

    /// <summary>
    /// One overhead section fitted to this deck: the cables.
    ///
    /// Shifting the entry's offset is not enough and never was. Every cable section sits at offset zero
    /// and carries its width in a single full-width piece, so what has to change is the piece, not where
    /// the section is put. When the piece cannot be widened the entry is still copied - a bridge with
    /// cables at the wrong spacing beats a bridge with no cables - and the factory says so.
    /// </summary>
    private NetSectionInfo Widened(NetSectionInfo source, string bridgeName, float extra)
    {
        var spread = Spread(source, extra);
        if (_towers == null || Math.Abs(extra) < 0.001f) return spread;

        var widened = _towers.WidenSection(source.m_Section, bridgeName, extra);
        if (widened != null) spread.m_Section = widened;
        return spread;
    }

    /// <summary>
    /// Adds the donor's deck props - pylons, portals - on top of whatever the road already has.
    ///
    /// Entries are dropped rather than translated when their placement depends on fixed segments the
    /// copied bridge does not have. A placement like EdgeMiddleFixedSegment indexes the bridge's fixed
    /// segment array through m_FixedIndex, and an index into an array that is shorter than the donor's
    /// is read by a compiled job with no bounds check to save us.
    /// </summary>
    private void CopySubObjects(NetGeometryPrefab target, BridgeStyleVariant variant, float extra)
    {
        var source = variant.SubObjects;

        // A style that brings no props of its own leaves the road's default pillars in place. They
        // are the wrong pillars for the style, but a bridge with no supports at all would be worse.
        if (source?.m_SubObjects == null || source.m_SubObjects.Length == 0) return;

        var fixedSegments = target.GetComponent<Bridge>()?.m_FixedSegments?.Length ?? 0;
        var usable = new List<NetSubObjectInfo>();
        var dropped = 0;
        var markers = 0;
        foreach (var info in source.m_SubObjects)
        {
            if (info?.m_Object == null) continue;

            // Markers belong to the road, not to the bridge style. The donor carries an outside
            // connection of its own, and copying it leaves the generated bridge with two - its road's
            // and the donor's - where the archetype has one.
            if (info.m_Object is MarkerObjectPrefab)
            {
                markers++;
                continue;
            }

            if (NeedsFixedSegment(info.m_Placement) && info.m_FixedIndex >= fixedSegments)
            {
                dropped++;
                continue;
            }

            usable.Add(Spread(info, extra));
        }

        if (dropped > 0)
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': {1} of the style's {2} deck props were left out because they are anchored to "
                + "fixed spans this bridge does not have.",
                target.name, dropped, source.m_SubObjects.Length));
        }

        if (markers > 0)
        {
            // Not a shortfall, so not a warning. The road brought its own, and two would be one too
            // many - which is what the generated bridge had until these were left behind.
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: left the donor's {1} marker(s) behind; the road has its own.",
                target.name, markers));
        }

        var subObjects = target.AddOrGetComponent<NetSubObjects>();
        var existing = subObjects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();

        // The road's own elevated props are its default pillars - the plain columns any road grows
        // when it is raised. They are replaced, not joined: appending left the bridge standing on its
        // style's pylons and on a row of ordinary concrete pillars at the same time. Props that are
        // not gated on being elevated belong to the road at ground level and are left alone.
        var kept = existing.Where(info => info == null || !info.m_RequireElevated).ToArray();
        var replaced = existing.Length - kept.Length;
        if (replaced > 0)
        {
            _report.Note(
                $"{target.name}: replaced {replaced} default elevated prop(s) with the style's own.");
        }

        if (usable.Count == 0)
        {
            _report.Warning(
                $"'{target.name}': none of the style's {source.m_SubObjects.Length} deck prop(s) could be "
                + "used, so this bridge has no towers or pylons of its own.");
        }

        if (usable.Count == 0 && replaced == 0) return;

        subObjects.m_SubObjects = kept.Concat(usable).ToArray();
        subObjects.active = true;
    }

    /// <summary>Whether a placement reads the bridge's fixed segment array.</summary>
    private static bool NeedsFixedSegment(NetObjectPlacement placement) => placement
        is NetObjectPlacement.NodeBeforeFixedSegment
        or NetObjectPlacement.NodeBetweenFixedSegment
        or NetObjectPlacement.NodeAfterFixedSegment
        or NetObjectPlacement.EdgeMiddleFixedSegment
        or NetObjectPlacement.EdgeEndsFixedSegment
        or NetObjectPlacement.EdgeStartFixedSegment
        or NetObjectPlacement.EdgeEndFixedSegment
        or NetObjectPlacement.EdgeEndsOrNodeFixedSegment
        or NetObjectPlacement.EdgeStartOrNodeFixedSegment
        or NetObjectPlacement.EdgeEndOrNodeFixedSegment;


    private static NetSectionInfo Spread(NetSectionInfo source, float extra) => new()
    {
        m_Section = source.m_Section,
        m_RequireAll = Copy(source.m_RequireAll),
        m_RequireAny = Copy(source.m_RequireAny),
        m_RequireNone = Copy(source.m_RequireNone),
        m_HiddenLayers = source.m_HiddenLayers,
        m_Invert = source.m_Invert,
        m_Flip = source.m_Flip,
        // Marked as a median section, as the archetype marks its cables. Read from the donor because a
        // section that is not the cables - a pack with something else overhead - should keep its own.
        m_Median = source.m_Median,
        m_HalfLength = source.m_HalfLength,

        // The cables sit on the centre line and stay there. The archetype records this offset as zero
        // at every width the game ships, so what puts a wider bridge's cables further out is the
        // section being wider, not the section being moved - and moving a zero moves nothing, which is
        // why every attempt to fix the cables by shifting this did exactly nothing.
        //
        // Still passed through Spread rather than written as zero: a donor whose overhead section is
        // genuinely offset is not the archetype's arrangement, and it keeps its own.
        m_Offset = new float3(
            TowerWidening.Spread(source.m_Offset.x, extra), source.m_Offset.y, source.m_Offset.z),
    };

    private static NetSubObjectInfo Spread(NetSubObjectInfo source, float extra) => new()
    {
        m_Object = source.m_Object,
        m_Position = new float3(
            TowerWidening.Spread(source.m_Position.x, extra), source.m_Position.y, source.m_Position.z),
        m_Rotation = source.m_Rotation,
        m_Placement = source.m_Placement,
        m_FixedIndex = source.m_FixedIndex,
        m_Spacing = source.m_Spacing,
        m_AnchorTop = source.m_AnchorTop,
        m_AnchorCenter = source.m_AnchorCenter,

        // Copied, not forced. This was set to true on the reasoning that a pylon standing on a ground
        // level road would be the most obvious way for a converted bridge to look wrong - but the
        // bridge being imitated does not do that. Read out of the game, the five-lane suspension bridge
        // anchors its tower with:
        //
        //     [subobject] 5LaneSuspensionBridgePillar Placeholder at (0, 77.9, 0)
        //         placement=EdgeMiddle spacing=0 anchorTop=False anchorCenter=False requireElevated=False
        //
        // False, because a bridge prefab is only ever built elevated and the flag has nothing to add.
        // Setting it changes which props the game considers this to be, and the road's own props are
        // sorted on the same flag a few lines down - so forcing it here quietly reclassified every prop
        // the style brought.
        m_RequireElevated = source.m_RequireElevated,
        m_RequireOutsideConnection = source.m_RequireOutsideConnection,
        m_RequireDeadEnd = source.m_RequireDeadEnd,
        m_RequireOrphan = source.m_RequireOrphan,
    };

    private static NetPieceRequirements[]? Copy(NetPieceRequirements[]? source) => source?.ToArray();
}

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
