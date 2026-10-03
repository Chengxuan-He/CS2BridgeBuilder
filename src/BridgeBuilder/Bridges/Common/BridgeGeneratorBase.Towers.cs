using Colossal.Mathematics;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal abstract partial class BridgeGeneratorBase
{

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
    protected virtual bool FitTower(
        NetGeometryPrefab target,
        BridgeStyle style,
        float deckWidth,
        BridgeStyleVariant variant,
        BridgeTowers.Tower? chosen,
        float? primarySourceRoadWidth)
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
            var road = AllTowersUseReferenceDeck && primarySourceRoadWidth.HasValue
                ? primarySourceRoadWidth.Value
                : primary
                ? primarySourceRoadWidth
                    ?? chosen?.Road
                    ?? (WidthFollowsSidewalks
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
    protected virtual void CopyPlacement(NetGeometryPrefab target, BridgeStyle style)
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

}
