using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// One bridge the exporter can copy a look from: a net prefab that already carries a
/// <see cref="Bridge"/> component, together with the width it was authored for.
/// The donor is referenced, never cloned. Its towers and cables stay owned by whichever mod or
/// vanilla pack shipped them, which is also why an exported bridge keeps needing that pack.
/// </summary>
internal sealed class BridgeStyleVariant
{
    internal BridgeStyleVariant(NetGeometryPrefab donor, Bridge bridge, float width)
    {
        Donor = donor;
        Bridge = bridge;
        Width = width;

        // Recorded numbers win over measured ones. Measuring at runtime depends on which geometry
        // assets happen to be loaded and on a section rule the game's own bridges do not follow, so a
        // prefab that was scanned once uses that answer and only an unscanned one measures itself.
        if (BridgeMeasurements.TryGet(donor.name, out var recordedRoad, out var recordedTower))
        {
            RoadWidth = recordedRoad;
            StructureWidth = recordedTower;
        }
        else
        {
            RoadWidth = NetWidth.RoadSurfaceOf(donor);
            StructureWidth = MeasureStructure(donor);
        }
    }

    internal NetGeometryPrefab Donor { get; }

    internal Bridge Bridge { get; }

    /// <summary>Metres. 0 when the donor's sections could not be measured.</summary>
    internal float Width { get; }

    internal string Name => Donor.name;

    internal OverheadNetSections? Overhead => Donor.GetComponent<OverheadNetSections>();

    internal NetSubObjects? SubObjects => Donor.GetComponent<NetSubObjects>();

    /// <summary>
    /// Whether the donor carries a second deck of its own. Such a bridge places its towers and cables
    /// around two levels, so its structure only reads correctly on something that also has two.
    /// </summary>
    internal bool IsDoubleDeck => Donor.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?.Length > 0;

    /// <summary>
    /// The arrangement this bridge hangs its lower deck by, or null when it has none.
    ///
    /// The whole double deck arrangement is the archetype's: how far below the deck the second one
    /// runs, and whether it runs the other way. Both are read from here rather than chosen, because a
    /// double deck bridge's towers, cables and portals are drawn around two decks at one particular
    /// separation - move them apart and the lower deck passes through structure that was modelled to
    /// clear it.
    /// </summary>
    internal AuxiliaryNetInfo? LowerDeck =>
        Donor.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?.FirstOrDefault(info => info != null);

    /// <summary>
    /// Where the archetype puts its second deck, in metres along y. Negative is below, positive above.
    /// Zero when it has none.
    ///
    /// Not adjustable. It was a setting, clamped between four and twenty-four metres, and a bridge
    /// built at any value but the archetype's is a bridge whose structure was drawn for a separation
    /// it no longer has.
    /// </summary>
    internal float DeckSpacing
    {
        get
        {
            var lower = LowerDeck;
            return lower == null ? 0f : lower.m_Position.y;
        }
    }

    /// <summary>
    /// How wide the structure above the deck actually is, in metres. 0 when nothing can be measured.
    ///
    /// Reported rather than used for selection - see <see cref="BridgeStyle.Nearest"/> for why the
    /// donor's own deck width is the better selector - but it is what tells the player whether the
    /// towers will clear their road, so it has to be a real measurement.
    /// </summary>
    internal float StructureWidth { get; }

    /// <summary>
    /// The carriageway this bridge carries, without its own railings and deck edges. This is the
    /// number a road is comparable to.
    /// </summary>
    internal float RoadWidth { get; }

    /// <summary>
    /// How much wider the tower is than the road it spans - the margin its author left. Measured
    /// against the carriageway, not the whole net, or it answers a question nobody asked.
    /// </summary>
    internal float Clearance => StructureWidth > 0f && RoadWidth > 0f ? StructureWidth - RoadWidth : 0f;

    /// <summary>
    /// Measures the structure from its geometry, not from where its parts are placed.
    ///
    /// A pylon is a single object sitting at the centre of the deck - every position in the reference
    /// pack's suspension bridges is exactly (0, 0, 0) - and the width that matters is the span of its
    /// mesh. Reading the offsets instead, as an earlier version did, measured the towers of a 38 m
    /// bridge as 26.5 m and would have measured them as zero if the overhead sections had not
    /// happened to be there.
    /// </summary>
    private static float MeasureStructure(NetGeometryPrefab donor)
    {
        var width = 0f;

        var overhead = donor.GetComponent<OverheadNetSections>();
        if (overhead?.m_Sections != null)
        {
            foreach (var section in overhead.m_Sections)
            {
                if (section?.m_Section == null) continue;
                width = Math.Max(width, (Math.Abs(section.m_Offset.x) * 2f) + NetWidth.Of(section.m_Section));
            }
        }

        var subObjects = donor.GetComponent<NetSubObjects>();
        if (subObjects?.m_SubObjects != null)
        {
            foreach (var info in subObjects.m_SubObjects)
            {
                if (info?.m_Object == null) continue;
                var span = MeasureObject(info.m_Object);
                if (span <= 0f) continue;
                width = Math.Max(width, (Math.Abs(info.m_Position.x) * 2f) + span);
            }
        }

        return width;
    }


    /// <summary>Whether this donor hangs the named tower along its deck.</summary>
    internal bool CarriesTower(string towerName)
    {
        var subObjects = Donor.GetComponent<NetSubObjects>();
        if (subObjects?.m_SubObjects == null) return false;

        foreach (var info in subObjects.m_SubObjects)
        {
            if (info?.m_Object == null) continue;
            if (string.Equals(info.m_Object.name, towerName, StringComparison.Ordinal)) return true;
        }

        return false;
    }
    /// <summary>
    /// Every tower this donor actually carries, named and measured one by one.
    ///
    /// The aggregate width is the widest of them, which is the number selection needs but tells you
    /// nothing about what is in there. This is what a tower list has to be built from: the objects
    /// themselves - a pylon, a pillar, a portal - not the bridge they happen to belong to.
    /// </summary>
    internal string DescribeTowers()
    {
        var parts = new List<string>();

        var overhead = Donor.GetComponent<OverheadNetSections>();
        if (overhead?.m_Sections != null)
        {
            foreach (var section in overhead.m_Sections)
            {
                if (section?.m_Section == null) continue;
                parts.Add($"section {section.m_Section.name}={NetWidth.Of(section.m_Section):0.#}");
            }
        }

        var subObjects = Donor.GetComponent<NetSubObjects>();
        if (subObjects?.m_SubObjects != null)
        {
            foreach (var info in subObjects.m_SubObjects)
            {
                if (info?.m_Object == null) continue;
                parts.Add($"object {info.m_Object.name}={MeasureObject(info.m_Object):0.#}@{info.m_Position.x:0.#}");
            }
        }

        return parts.Count == 0 ? "none" : string.Join(" | ", parts);
    }

    /// <summary>The lateral span of an object's meshes, or 0 when it has none that can be measured.</summary>
    private static float MeasureObject(ObjectPrefab prefab)
    {
        if (prefab is not ObjectGeometryPrefab geometry || geometry.m_Meshes == null) return 0f;

        var width = 0f;
        foreach (var mesh in geometry.m_Meshes)
        {
            if (mesh?.m_Mesh is not RenderPrefab render) continue;
            try
            {
                var bounds = render.bounds;
                width = Math.Max(width, (Math.Abs(mesh.m_Position.x) * 2f) + (bounds.max.x - bounds.min.x));
            }
            catch (Exception)
            {
                // A mesh whose geometry asset is not loaded cannot be measured. Skipping it costs an
                // accurate number in the report; throwing would cost the whole style list.
            }
        }

        return width;
    }
}

/// <summary>
/// A style as the player picks it - "Suspension Bridge" - rather than one of the per width prefabs
/// the donor pack ships. Bridge packs author a separate prefab for every lane count, and asking the
/// player to match those by hand would defeat the point of converting an arbitrary custom road.
/// </summary>
internal sealed class BridgeStyle
{
    private readonly List<BridgeStyleVariant> _variants = new();

    internal BridgeStyle(string id, string nameSuffix, Func<string> displayName, int? clearance = null)
    {
        Id = id;
        NameSuffix = nameSuffix;
        _displayName = displayName;
        AuthoredClearance = clearance;
    }

    /// <summary>
    /// The style's measured margin over the road, in metres. Null for a family discovered in a pack
    /// that matches no named style - nothing has been measured for it, so it averages its own variants.
    /// </summary>
    internal int? AuthoredClearance { get; }

    private readonly Func<string> _displayName;

    /// <summary>Stable across sessions: this is what the setting stores.</summary>
    internal string Id { get; }

    /// <summary>
    /// The untranslated suffix an exported asset carries. Read from the definition rather than from
    /// <see cref="DisplayName"/> so that switching language never renames an asset.
    /// </summary>
    internal string NameSuffix { get; }

    /// <summary>
    /// Resolved on every read, because the player can change language while the options page is open
    /// and the style list is only rebuilt when a world is loaded.
    /// </summary>
    internal string DisplayName => _displayName();

    /// <summary>
    /// Where the variants came from, so the UI can say what an export will depend on. Empty until a
    /// world has been scanned, since that is when donors are found.
    /// </summary>
    internal string Source { get; set; } = string.Empty;

    /// <summary>
    /// False when the style is named here but nothing providing it is registered - either because no
    /// world has been scanned yet, or because the content that ships it is not installed.
    /// </summary>
    internal bool IsInstalled => _variants.Count > 0;

    internal IReadOnlyList<BridgeStyleVariant> Variants => _variants;

    internal void Add(BridgeStyleVariant variant) => _variants.Add(variant);

    /// <summary>
    /// The variant to build from, for a road of the given width.
    ///
    /// Selection is on the road each tower was built for, not on how wide the tower itself is. A tower
    /// always reaches further than the road it straddles, so comparing a road against a tower's own
    /// span is comparing two different things - which is how a 42 m road was once told a 34 m tower
    /// would fit. Rounded up: the narrowest tower built for a road at least this wide, and the widest
    /// there is when none of them reach.
    /// </summary>
    internal BridgeStyleVariant? Nearest(float width, bool forRoad = true)
    {
        return Select(width, forRoad).Variant;
    }

    /// <summary>
    /// The tower this style would use for a road of the given width, or null when it has no list.
    /// The generator derives from this one, so that a road matching its recorded width reproduces it
    /// exactly.
    /// </summary>
    internal BridgeTowers.Tower? TowerFor(float width)
    {
        return Select(width).Tower;
    }

    /// <summary>
    /// The bridge to copy and the tower to derive, decided together.
    ///
    /// They have to be one decision. The cables and deck props belong to the donor and were placed to
    /// meet the legs of whichever tower that donor carries; the generated tower is derived from the
    /// tower this returns. If those are two different towers then no single sideways shift can put the
    /// cables back onto the legs, and the bridge comes out with its cables over the carriageway. This
    /// used to be two walks of the tower table handed two different widths - the road's declared width
    /// for the donor, its measured width for the tower - which is exactly how they came apart. Deciding
    /// once removes the possibility instead of keeping two answers in step by hand.
    /// </summary>
    internal Selection Select(float width, bool forRoad = true, bool doubleDeck = false,
        Func<BridgeStyleVariant, bool>? allow = null)
    {
        // A double deck bridge is built from a double deck archetype, not from a single deck one with
        // a second net hung underneath it. Those are different bridges: a double deck archetype's
        // towers, portals and cables are drawn around two decks at one particular separation, and its
        // AuxiliaryNets entry is where that separation is recorded. Building the lower deck as a
        // bridge of its own produced two structures that had never been designed to stand together.
        //
        // So the candidates are filtered rather than merely preferred. With none, the style has no
        // double deck version and generation fails - see BridgeComposer - because there is nothing to
        // follow and inventing an arrangement is what this was.
        // Filtered both ways, not only one. Asked for two decks, only double deck archetypes will do;
        // asked for one, only single deck ones will. The second half was a preference before - a
        // penalty in Taste - so a style whose archetypes are all double deck handed one over anyway
        // and the lower deck came out as structure the bridge had no road for. ExtradosedBridge01 and
        // 02 are that case: both carry a second deck of their own, and a single deck bridge cannot be
        // made from either.
        // An optional caller filter remains for constraints belonging to a particular operation. It
        // must not be used to exclude a track from the main slot of an A-pylon bridge: main/auxiliary
        // are ownership roles in AuxiliaryNets, and a track is a valid main network.
        var eligible = _variants
            .Where(candidate => candidate.IsDoubleDeck == doubleDeck)
            .Where(candidate => allow == null || allow(candidate))
            .ToList();

        // The list names towers; a tower is only usable through a donor that carries it - and the same
        // tower is usually carried by several. Taking whichever came first is how a single deck bridge
        // ended up wearing a double deck one's structure, so the donors are ordered and the least wrong
        // one wins.
        BridgeStyleVariant? DonorFor(string towerName) => eligible
            .Where(candidate => candidate.CarriesTower(towerName))
            .OrderBy(candidate => Disqualifying(candidate, forRoad))
            .ThenBy(candidate => Taste(candidate, doubleDeck))
            .FirstOrDefault();

        var tower = BridgeTowers.Select(Id, width, towerName => DonorFor(towerName) != null);
        if (tower.HasValue)
        {
            var variant = DonorFor(tower.Value.Name);
            if (variant != null) return new Selection(variant, tower);
        }

        // Either the type has no list at all, or nothing on it is installed.
        return new Selection(Ranked(width + TypicalClearance(), forRoad, eligible, doubleDeck), null);
    }

    /// <summary>One decision: which bridge to copy from, and which of its towers to derive from.</summary>
    internal readonly struct Selection
    {
        internal Selection(BridgeStyleVariant? variant, BridgeTowers.Tower? tower)
        {
            Variant = variant;
            Tower = tower;
        }

        internal BridgeStyleVariant? Variant { get; }

        internal BridgeTowers.Tower? Tower { get; }

        /// <summary>
        /// How much wider this road is than the one everything on this bridge was authored for.
        ///
        /// One number for the whole bridge - the tower's vertices, the cables, the deck props - so they
        /// all travel the same distance and stay attached to each other. Zero at the tower's own width,
        /// which is what makes a bridge over a 20 m road come out as the game's own four lane
        /// suspension bridge rather than as something merely derived from it.
        /// </summary>
        internal float ExtraFor(float deckWidth, string? styleId = null)
        {
            if (Tower.HasValue) return deckWidth - Tower.Value.Road;

            // No portal was selected. For a style whose structure is overhead there is none to
            // select - a through arch is spanned by its arch and its only object is a support under
            // the deck - but the road that design was drawn for was measured all the same, and that
            // is what the widening is against. Falling straight through to the ranked variant's own
            // width measures against whichever bridge the ranking happened to turn up: the through
            // arch came out 2 m short that way, on a road it was never compared to.
            var recorded = BridgeTowers.RoadOf(styleId);
            if (recorded > 0f) return deckWidth - recorded;

            return Variant != null && Variant.RoadWidth > 0f ? deckWidth - Variant.RoadWidth : 0f;
        }
    }

    /// <summary>
    /// The fallback for a type with no tower list: a family found in a pack that matches no named
    /// style. Ordered so that fit outranks preference - a preference that overrules fit is a bug.
    /// </summary>
    private BridgeStyleVariant? Ranked(
        float target, bool forRoad, IReadOnlyList<BridgeStyleVariant> eligible, bool doubleDeck)
    {
        BridgeStyleVariant? best = null;
        var bestKey = Key.Worst;
        foreach (var variant in eligible)
        {
            var tower = variant.StructureWidth;
            if (tower <= 0f) continue;

            var difference = Math.Abs(tower - target);
            var key = new Key(
                Disqualifying(variant, forRoad),
                tower + CoverTolerance >= target ? 0 : 1,
                (int)Math.Ceiling(difference / BandMetres),
                Taste(variant, doubleDeck),
                difference);
            if (key.CompareTo(bestKey) >= 0) continue;
            bestKey = key;
            best = variant;
        }

        // Nothing measurable to rank: better a variant chosen blind than refusing to build at all.
        return best ?? (eligible.Count > 0 ? eligible[0] : null);
    }

    /// <summary>
    /// The margin between a tower and the road it spans, averaged across this style.
    ///
    /// Clearance is tower width minus road width, and it is a property of the style rather than of any
    /// one bridge in it - an author builds a family to a consistent margin - so the mean across every
    /// variant that can be measured is what the target uses.
    ///
    /// The mean is then floored. Individual measurements can come out at or below zero, because a net's
    /// width counts shoulders and railings the towers stand inside of: the game's own
    /// SuspensionBridge03 measures -1.6 m that way. A target at or below the road width would let a
    /// 40 m road take a 40 m tower - a tower the road exactly fills rather than one it passes through -
    /// so the floor is what stops the arithmetic producing a bridge nobody would call fitted.
    /// </summary>
    private float TypicalClearance()
    {
        // A named style uses its recorded number. Averaging live measurements was what produced a
        // target below the road width, because a few prefabs measure as though their towers were
        // narrower than the road they span.
        if (AuthoredClearance.HasValue) return AuthoredClearance.Value;

        var clearances = _variants
            .Where(variant => variant.StructureWidth > 0f && variant.RoadWidth > 0f)
            .Select(variant => variant.Clearance)
            // Only positive margins are data. A tower measuring narrower than the road it was authored
            // for is not a design decision - the game's own SuspensionBridge03 reports -1.6 m - it is
            // this measurement failing on that prefab, and averaging failures in drags the target below
            // the road width, which is how a 40 m road was offered a 40 m tower.
            .Where(clearance => clearance > 0f)
            .ToList();
        var measured = clearances.Count == 0 ? 0f : clearances.Sum() / clearances.Count;
        return Math.Max(measured, MinimumClearance);
    }

    private static bool Builtin(BridgeStyleVariant variant)
    {
        try
        {
            return variant.Donor.isBuiltin;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// The least a tower may exceed the road by. A tower merely as wide as the road is not a tower the
    /// road passes through, so the margin is a floor and not an average.
    /// </summary>
    private const float MinimumClearance = 4f;

    /// <summary>Float noise only. A tower short of the target does not count as covering it.</summary>
    private const float CoverTolerance = 0.05f;

    /// <summary>Two towers within this much of each other fit equally well.</summary>
    private const float BandMetres = 2f;

    private readonly struct Key : IComparable<Key>
    {
        internal static readonly Key Worst = new(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, float.MaxValue);

        private readonly int _kind;
        private readonly int _cover;
        private readonly int _band;
        private readonly int _origin;
        private readonly float _exact;

        internal Key(int kind, int cover, int band, int origin, float exact)
        {
            _kind = kind;
            _cover = cover;
            _band = band;
            _origin = origin;
            _exact = exact;
        }

        public int CompareTo(Key other)
        {
            if (_kind != other._kind) return _kind.CompareTo(other._kind);
            if (_cover != other._cover) return _cover.CompareTo(other._cover);
            if (_band != other._band) return _band.CompareTo(other._band);
            if (_origin != other._origin) return _origin.CompareTo(other._origin);
            return _exact.CompareTo(other._exact);
        }
    }
    /// <summary>
    /// Reasons a variant is the wrong shape whatever it measures. These outrank fit, because a tower
    /// that fits a road it was never built for is still the wrong tower.
    ///
    /// Carrying the wrong traffic counts here. A pack's railway suspension bridge is often a road
    /// prefab underneath, so the net's type alone does not catch it - its name does, and a bridge
    /// built to carry trains has its towers and portals placed for a railway rather than a highway.
    /// </summary>
    private static int Disqualifying(BridgeStyleVariant variant, bool forRoad)
    {
        var penalty = 0;
        // No structure at all produces a bare bridge: correct in every measurement, visibly missing
        // its towers.
        if (variant.StructureWidth <= 0f) penalty += 8;
        if (variant.Donor is RoadPrefab != forRoad) penalty += 4;
        if (forRoad && CarriesRails(variant.Donor.name)) penalty += 4;
        return penalty;
    }

    /// <summary>Whether the donor's name says it was built for rails rather than for a road.</summary>
    private static bool CarriesRails(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name!.IndexOf("train", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("subway", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("tram", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("track", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Preferences, applied only between variants that fit about equally well.
    ///
    /// These used to outrank width, and that is how a two-lane bridge's tower ended up on a six-lane
    /// road: the four-lane variant that nearly fitted was passed over for being a pack's rather than
    /// the game's own, and the three-lane one for carrying a second deck. A preference that overrules
    /// fit is not a preference, it is a bug.
    /// </summary>
    private static int Taste(BridgeStyleVariant variant, bool doubleDeck)
    {
        var penalty = 0;

        // A second deck is a penalty when one was not asked for - a double deck archetype places its
        // structure around two levels and only reads correctly on something that has two - and not a
        // penalty when it was, because then it is the only thing that will do.
        if (variant.IsDoubleDeck && !doubleDeck) penalty += 2;
        if (!Builtin(variant)) penalty += 1;
        return penalty;
    }
}
