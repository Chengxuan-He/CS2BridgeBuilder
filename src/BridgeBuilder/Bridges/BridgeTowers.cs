
using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The towers each bridge type can use, in ascending order of the road they were built for.
///
/// Two widths per entry, and the difference between them is the point.
///
/// <c>Road</c> is the carriageway the tower was designed to span. It is what a road is compared
/// against, and it is what makes the result checkable: generate at exactly this width and the shift is
/// zero, so the mesh comes out identical to the tower it was derived from. Ask for a suspension bridge
/// over a 23 m road and you get 4LaneSuspensionBridgePillar, unchanged.
///
/// <c>Mesh</c> is how wide the tower itself is - the span of its geometry, legs included. It is
/// reported rather than matched against, because a tower always reaches further than the road it
/// straddles and comparing a road to it directly is how a 42 m road was once told a 34 m tower fitted.
///
/// Both are whole metres from a scan of a running game; they cannot be read any other way, since mesh
/// bounds only exist once the geometry asset is loaded. Rail and track bridges are absent - their
/// portals are placed for a railway - as are entries whose towers could not be measured, and anything
/// carried only by double deck bridges, which is second-deck structure rather than a tower.
///
/// The suspension road widths are corrected figures, not raw ones, and the correction is exactly two
/// metres: 14/18/22/26 in <see cref="BridgeMeasurements"/> against 12/16/20/24 here, uniformly. Two of
/// them were checked against the game - a 20 m road takes the four-lane pillar, a 16 m road the
/// three-lane one - and the whole family was brought onto that scale.
///
/// Two metres is not a fudge. It is the side section at each end of every road, the strip that blends
/// the net into the terrain beside it, which the scan counted as carriageway and a tower does not have
/// to span. <see cref="SectionNames.IsSide"/> now excludes it at the source, so new measurements come
/// out on this scale already. The number was arrived at twice from different directions - once from
/// the game, once from the section list - which is the only reason it is trusted.
///
/// Every other family was measured the same way, by TowerSelfTest, against the bridges that carry each
/// tower. Before that they held raw scanned numbers, and most were wrong: the golden entry said 34,
/// which is the blue five-lane tower's width and belongs to nothing in that family.
///
/// Entries whose mesh came back narrower than their road are marked as supports rather than removed. A
/// column standing under a deck is not a portal the road passes through, so its width is not a road
/// width and selection passes over it - but it was measured, and a measurement that is deleted only
/// gets taken again. Two of them had been deleted once already for exactly that reason.
///
/// A listed tower whose prefab is not installed is skipped. Correcting a number here is a one-line
/// edit, and TowerSelfTest re-checks the identity property against whatever it says.
/// </summary>
internal static class BridgeTowers
{
    /// <summary>One tower: its name, how wide its mesh is, and the road it was built for.</summary>
    internal readonly struct Tower
    {
        internal Tower(string name, int mesh, int road, bool verified = false, bool support = false)
        {
            Name = name;
            Mesh = mesh;
            Road = road;
            Verified = verified;
            Support = support;
        }

        internal string Name { get; }
        internal int Mesh { get; }
        internal int Road { get; }

        /// <summary>
        /// Whether this is a column under the deck rather than a portal the road passes through.
        ///
        /// It is the measurement that says so: a support does not span its road, so its width is not a
        /// road width and deriving a tower from it produces one sized from a number that never meant
        /// what it was read as. They are listed rather than deleted because they were measured, and a
        /// measurement that disappears gets taken again; selection simply passes over them.
        /// </summary>
        internal bool Support { get; }

        /// <summary>
        /// Whether this road width has been checked against the running game, or is only what the
        /// scan happened to report.
        ///
        /// The distinction is not bookkeeping. An unverified number is not merely imprecise - it can
        /// belong to a different tower entirely, and then the bridge is built to the wrong width and
        /// nothing downstream can tell. The tests hold verified numbers to the scan they came from;
        /// unverified ones are used, but they are used knowing that.
        /// </summary>
        internal bool Verified { get; }
    }

    private static readonly Dictionary<string, Tower[]> Table =
        new(StringComparer.Ordinal)
        {
            ["CableStayed"] = new[]
            {
                // Left at 3. The metre came off the eight-lane tower, which is the one that was
                // looked at; this is a 6 m footbridge pylon and nobody has seen it. Applying the same
                // correction to it would be the inference that has now been wrong twice.
                new Tower("PedestrianBridgeCableStayedPillar Placeholder", 6, 3, verified: true),

                // Road 33, an in-game observation, and it supersedes the inference that produced 32.
                //
                // The 32 was derived rather than measured: this bridge is "XL Road Divided" and every
                // divided road came back implausibly wide - 75 m for eight lanes, 61 m for six - so its
                // own road width says nothing. Its tower and cable section do: 42 m and 35 m. Against
                // 32 those give ten metres of tower overhang and three of cable margin, which is
                // exactly what the blue suspension family has at every size, and no other width made
                // both come out right. It was a good inference and it was a metre out.
                //
                // At 33 the overhang is 9 and the margin 2, so this family is not the blue one after
                // all - which is the same thing the golden family turned out to be, and there the
                // inference from ten metres of overhang cost more than a metre. A constant measured on
                // one family is that family's; rule 9. MEASURED ROAD 75 m, unusable.
                new Tower("8LaneCableStayedBridgePillar Placeholder", 42, 33, verified: true),
            },
            // One key per pylon, split out of a single "Extradosed" entry. The pylon is what a
            // cable-stayed design is: a road fitted to a V pylon cannot wear an A pylon's cables, and
            // filing all three together meant the width fit chose between designs.
            //
            // The roads are the audit's, which is to say the narrowest carriageway among the bridges
            // that ship carrying each pylon: five carry the V pylon and every one of them is 40 m.
            //
            // They were 20/18/18 - three steps down from these, each step an in-game reading of a
            // bridge that looked too narrow, recorded as a road correction. Twenty metres of
            // accumulated correction, and it showed: converting a 40 m road, the V pylon was told the
            // parts had been drawn for 20 and widened everything by 20 more, which put the stay cables
            // ten metres beyond each edge of the deck, hanging over nothing.
            //
            // Two things let that run. The readings were taken while the legs were being scaled
            // instead of carried and the top deck was not reaching the legs, so a bridge that looked
            // too narrow was often a bridge drawn wrongly at the right width - the corrections were
            // paying for faults that are fixed now. And the note here said no independent check was
            // available, which was not true: the audit is one, it had been printing "table says road
            // 20 m; measured road 40 m" on every export, and nothing acted on it. The self test cannot
            // catch this - it reproduces the tower at the table's own road width, so a wrong table is
            // a test that passes.
            ["Extradosed01"] = new[]
            {
                new Tower("ExtradosedBridge01NetPillar", 53, 40, verified: true),
            },
            ["Extradosed02"] = new[]
            {
                new Tower("ExtradosedBridge02NetPillar", 46, 38, verified: true),
            },
            ["Extradosed03"] = new[]
            {
                new Tower("ExtradosedBridge03NetPillar", 56, 38, verified: true),
            },
            ["ExtradosedLarge"] = new[]
            {
                // A single column, 9 m across, standing on the centre line of a 61 m road with the
                // carriageway either side of it. Narrower than its road by design, which is why the
                // support flag fits: the flag means "not something the road runs through", and this is
                // exactly that. The style is listed in NotDerived as well, so nothing tries to widen a
                // column against a road width it was never drawn against - the game's own asset is
                // used unchanged.
                new Tower("6LaneExtradosedBridgePillar Placeholder", 9, 61, verified: true, support: true),
            },
            ["CoveredWood"] = new[]
            {
                // The housing is the structure and the pillar under it is a support: 3.3 m of pier
                // beneath a path the covered section spans at 9.5 m.
                //
                // Road 6, corrected by 10 from the 16 the deck scan returned, and this one does have a
                // check behind it: the housing is the envelope the path runs through, and 9.5 m cannot
                // enclose a 16 m path. Against 6 it stands 1.75 m clear each side, which is what a
                // covered bridge does.
                new Tower("PedestrianBridgeCoveredWood01NetPillar", 3, 6, verified: true, support: true),
            },
            ["Grand"] = new[]
            {
                // Grand Bridge carries both, and they are different things: the pylon is the portal the
                // road passes through, the pillar is what stands under it. Only the first is a tower.
                // Roads corrected by 10, from what the deck scan returned to what the structure
                // straddles - the same correction the golden family and the through arch needed, and
                // for the same reason: NetWidth sums the deck, and the deck is not the carriageway the
                // structure was drawn around. At the scanned widths the generated pylons came out
                // about ten metres narrow.
                //
                // Unlike those two this one has no independent check behind it. The golden family was
                // settled by a 33.8 m envelope that cannot enclose a 50 m road, and the through arch
                // by a 12.35 m opening that cannot carry a 20 m road. The grand bridge's section is
                // 21 m and wider than either reading of its road, so it discriminates between them not
                // at all. This is an in-game observation written down as one.
                // 12, from 9. The correction to 9 overshot by 3 - a narrower road makes a wider tower,
                // so three metres of tower came off by putting three metres of road back on.
                new Tower("GrandBridgePylon Placeholder", 34, 12, verified: true),
                new Tower("GrandBridgePillar Placeholder", 44, 12, verified: true),
                new Tower("BXP GrandBridgePillarB Placeholder", 44, 24, verified: true),
            },
            ["Lift"] = new[]
            {
                new Tower("LiftBridge01", 41, 24, verified: true),
                new Tower("LiftBridge05", 26, 24, verified: true),
                new Tower("LiftBridge02", 42, 30, verified: true),
                new Tower("LiftBridge04", 24, 36, verified: true, support: true),
                new Tower("LiftBridge03", 93, 60, verified: true),
            },
            ["Suspension"] = new[]
            {
                new Tower("2LaneSuspensionBridgePillar Placeholder", 22, 12, verified: true),
                new Tower("3LaneSuspensionBridgePillar Placeholder", 26, 16, verified: true),
                new Tower("4LaneSuspensionBridgePillar Placeholder", 30, 20, verified: true),
                new Tower("5LaneSuspensionBridgePillar Placeholder", 34, 24, verified: true),
            },
            ["SuspensionGolden"] = new[]
            {
                // SuspensionBridge03 and 04 carry both, and the pylon is the portal. The 34 recorded
                // here before belonged to the blue five-lane tower and to nothing in this family.
                //
                // Road 32, corrected from the 50 the scan returned. Fifty is what NetWidth sums across
                // this bridge's deck; it is not the carriageway the pylon straddles. The cable section
                // settles it without needing the game: it measures 33.8 m and it is the envelope the
                // road runs between, and 33.8 m cannot envelope a 50 m road. Against 32 it stands
                // 1.8 m outside the carriageway, which is what an envelope does.
                //
                // The pylon is 50.4 m across, so this family's overhang is 50.4 - 32 = 18.4 m. That is
                // its own number, not the blue family's 10 - see BridgeCables.Overhang - and the two
                // are different designs, which is the whole reason they are separate styles.
                //
                // Kept rather than replaced silently, per rule 2, and with what was tried: 50 was read
                // as measured and 40 was guessed from the blue family's overhang. Forty was worse than
                // fifty in the game and that was taken as confirming fifty; it confirmed only that 40
                // was not the answer. Both readings were of the deck. Only 32 is of the road.
                //
                // Fifty is also what collapsed the stiffening truss: a 16 m deck against a 50 m road
                // asked a 33.8 m truss to lose 34 m, and it was written 0 m across. Against 32 the
                // same deck asks it to lose 16, and it keeps 17.8.
                // 27, from 32. Thirty-two came from the pylon measuring 50.4 over what looked like a
                // 32 m carriageway; five metres of tower came on by taking five metres of road off,
                // so the overhang is 23.4 rather than 18.4. The 33.8 m section still encloses 27 with
                // 3.4 m clear each side, so the envelope check that ruled out 50 still holds.
                // 24, from 27, from 32, from the scan's 50. The overhang is 26.4 - see
                // BridgeCables.GoldenOverhang - and the 33.8 m section still encloses 24 with 4.9 m
                // clear each side, so the envelope check that ruled out 50 still holds at every step.
                new Tower("SuspensionBridge03NetPylon", 50, 24, verified: true),
                new Tower("SuspensionBridge03NetPillar", 50, 24, verified: true),
            },
            // The blue arch-above design, split out of TrussArch because the road runs under the arch
            // here and over it there - see BridgeStyleDefinitions.
            ["TrussArch01"] = new[]
            {
                // The live prototype audit identifies this as a support under a 20 m road. Earlier
                // code overruled that measured road with the support's 12.24 m opening and recorded
                // 10 m instead. An opening in a support is not a second measurement of the road. That
                // made a 40 m export widen every overhead truss member by 30 m rather than the 20 m
                // difference from TrussArchBridge01 itself, and the openwork mesh tore apart.
                //
                // Keep the prototype's measured relationship verbatim: 18.4 m support, 20 m road.
                // Support stays true so it is never mistaken for a portal when selecting a donor;
                // RoadOf still supplies the 20 m prototype datum to the overhead structure.
                new Tower("TrussArchBridge01NetPillar", 18, 20, verified: true, support: true),
            },
            // The green arch-above design is its own prototype. It uses the same target-minus-prototype
            // calculation and the same member-topology widening rule as blue, while retaining the 24 m
            // road measured from TrussArchBridge03 itself rather than borrowing blue's 20 m datum.
            ["TrussArch03"] = new[]
            {
                new Tower("TrussArchBridge03NetPillar", 18, 24, verified: true, support: true),
            },
            ["TrussArch"] = new[]
            {
                new Tower("2LaneTrussArchBridgePillar Placeholder", 14, 12, verified: true),

                // TrussArchBridge01NetPillar is not here any more. It is the arch-above design's
                // structure and is recorded under TrussArch01, against the prototype road it was
                // actually carried by. Filed under both keys it made an unrelated arch-below road
                // select the arch-above frame - the measurement is not lost, it is under the family
                // it belongs to.
                //
                // 03 is now recorded under its own green style. 02 remains the measured support in
                // this general family until it too becomes a separately selectable prototype.
                new Tower("TrussArchBridge02NetPillar", 30, 38, verified: true, support: true),
            },
        };

    /// <summary>The towers listed for a type, narrowest road first. Empty when the type has none.</summary>
    internal static IReadOnlyList<Tower> For(string styleId)
    {
        return Table.TryGetValue(styleId, out var towers) ? towers : Array.Empty<Tower>();
    }

    /// <summary>A road within this of a tower's own width is that tower's width.</summary>
    internal const float CoverTolerance = 0.05f;

    /// <summary>
    /// The one tower a road of this width uses - the single answer both the donor bridge and the
    /// generated mesh are chosen from.
    ///
    /// It exists because there used to be two walks of this table, one to pick the bridge to copy from
    /// and one to pick the tower to derive, and they were handed different widths. They disagreed: the
    /// donor came back carrying the five lane pylon, so its cables were spaced for that, while the
    /// tower was derived from the four lane one. The cables kept the wider spacing and the tower did
    /// not, which put the cables out over the carriageway instead of down either side of it. One walk,
    /// one answer, and the two cannot drift apart again.
    ///
    /// <paramref name="hasDonor"/> reports whether a bridge carrying that tower is actually installed.
    /// A tower nothing carries cannot be used, so it is passed over rather than returned and then
    /// failed on. Passing a predicate rather than the prefabs keeps this testable without a game.
    /// </summary>
    internal static Tower? Select(string styleId, float width, Func<string, bool>? hasDonor = null)
    {
        Tower? widest = null;
        foreach (var tower in For(styleId))
        {
            // A support is a column under the deck. Its width is not a road width, so widening a tower
            // from it produces a portal sized by a number that never meant that.
            if (tower.Support) continue;
            if (hasDonor != null && !hasDonor(tower.Name)) continue;

            widest = tower;
            if (tower.Road >= width - CoverTolerance) return tower;
        }

        // Nothing listed was built for a road this wide; the widest there is comes closest.
        return widest;
    }


    /// <summary>Every type that has a tower list, for the self test to walk.</summary>
    internal static IEnumerable<string> Styles => Table.Keys;

    /// <summary>
    /// Whether a named object is one of this style's structures.
    ///
    /// Used to tell structure the generated tower did not take over from ordinary props: a bridge that
    /// keeps one of its own towers beside a generated one wears two widths at once, and the report has
    /// to be able to say which object that was.
    /// </summary>
    internal static bool IsTower(string styleId, string? objectName)
    {
        if (objectName == null || !Table.TryGetValue(styleId, out var towers)) return false;

        foreach (var tower in towers)
        {
            if (string.Equals(tower.Name, objectName, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>The road a named structure of this style was authored for, or null when not recorded.</summary>
    internal static float? RoadFor(string styleId, string? objectName)
    {
        if (objectName == null || !Table.TryGetValue(styleId, out var towers)) return null;

        foreach (var tower in towers)
        {
            if (string.Equals(tower.Name, objectName, StringComparison.Ordinal)) return tower.Road;
        }

        return null;
    }

    /// <summary>
    /// Styles whose structure is not derived, and why.
    ///
    /// The through-arch styles do not belong here: their support is not widened, but their separate
    /// net-section truss is. TrussArch01's live prototype road is 20 m even though its support body and
    /// opening are narrower; the overhead section is widened independently from that 20 m datum. The
    /// support opening must never be reclassified as a portal or used as a road-width measurement.
    ///
    /// Most bridges are spanned by something the road passes through - a portal, a pylon - and that is
    /// an object the generator widens. A through arch is spanned by the arch, and the arch is drawn as
    /// a net section overhead, one down each side. Its only object is a pillar narrower than the road,
    /// which is a support holding the deck up rather than a structure the road runs between.
    ///
    /// Recorded rather than inferred from "all its entries are supports", because those are two
    /// different statements. All-supports can mean the portal has not been measured yet, which is a
    /// gap; this means there is no portal to measure, which is the design. Without the distinction a
    /// style with no portal falls back silently to whatever the ranking turns up, and silence is what
    /// this table exists to prevent.
    /// </summary>
    private static readonly Dictionary<string, string> Overhead =
        new(StringComparer.Ordinal)
        {
            ["CoveredWood"] = "a covered bridge is spanned by its housing, which is an overhead "
                + "section; its only object is a 3.3 m pier under the path, holding it up rather than "
                + "carrying it",
            // Not "no portal" but "nothing to widen". The single column stands on the centre line and
            // the road passes either side of it: it is narrower than its carriageway by design, so
            // there is no width relationship between the two to carry across. The game's own asset is
            // used as it is.
            ["ExtradosedLarge"] = "a single-column pylon stands on the centre line with the road either "
                + "side of it and is narrower than the carriageway by design, so there is no width to "
                + "derive - the game's own asset is used unchanged",
        };

    /// <summary>Why a style's structure is left alone, or null when it is derived like the rest.</summary>
    internal static string? NotDerivedReason(string? styleId)
    {
        if (styleId == null) return null;
        return Overhead.TryGetValue(styleId, out var reason) ? reason : null;
    }

    /// <summary>
    /// The road a style's structure was authored for, supports counted.
    ///
    /// <see cref="Select"/> passes over supports, and rightly: a support is not something to widen.
    /// But its road was measured, and for a style whose structure is overhead - a through arch, whose
    /// only object is a pillar under the deck - that measurement is the only record of what the design
    /// was drawn for. Without it the widening falls through to whichever variant the ranking turned
    /// up, and is measured against a bridge that has nothing to do with the one being built.
    ///
    /// The narrowest, matching the order <see cref="Select"/> reads the list in.
    /// </summary>
    internal static float RoadOf(string? styleId)
    {
        if (styleId == null || !Table.TryGetValue(styleId, out var towers)) return 0f;

        var narrowest = 0f;
        foreach (var tower in towers)
        {
            if (tower.Road <= 0f) continue;
            if (narrowest <= 0f || tower.Road < narrowest) narrowest = tower.Road;
        }

        return narrowest;
    }

    /// <summary>
    /// Metres added to a style's structure - its tower and its cables alike - beyond what the road
    /// gives.
    ///
    /// The golden family needs three metres more than the road accounts for. Kept apart from the road
    /// because moving the road would move the deck props and the spread report with it; this moves
    /// only what the bridge is built of.
    ///
    /// It went to the tower alone at first, on a reading of the measurement that took the cables to be
    /// right where they were. They were not, and they could not have been: the distance from the
    /// cables to the tower's outer edge is the archetype's and holds at every road width, so three
    /// metres of tower and none of cable moves that distance a metre and a half per side by
    /// construction. Whatever is added is added to both, or the two come apart - rule 5.
    /// </summary>
    private static readonly Dictionary<string, float> TowerBonus =
        new(StringComparer.Ordinal)
        {
            // TrussArchBridge01's overhead frame stands 12 m wider than the carriageway relationship
            // alone produces. This belongs to the blue prototype structure, not to the target road:
            // adding it here widens the two sides by another 6 m each while leaving the road, the
            // green TrussArchBridge03 prototype and every other bridge family unchanged.
            ["TrussArch01"] = 12f,

            // TrussArchBridge03 needs another 16 m of structural width beyond the target-minus-
            // prototype-road calculation. The green side frame includes its inner railing, so its
            // dedicated open-truss policy carries each side by 8 m without altering that clearance.
            ["TrussArch03"] = 16f,

            // The double-deck V prototype's structure needs 20 m more than its upper carriageway
            // alone accounts for. On a 16 m target the raw 16 - 40 = -24 m contraction carried the
            // prototype's 20.09 m node opening through the centre, reversing its two sides. Applying
            // this prototype allowance makes the effective contraction -4 m and keeps a 16.09 m
            // opening. This is the earlier measured 16 m allowance plus the final 4 m correction.
            ["Extradosed01"] = 20f,

            // 3, then -4, -1, +1, -2, +1, -0.5, each read in the game on the same tower: -2.5.
            ["SuspensionGolden"] = -2.5f,

            // Seen in the game on the V pylon, after its legs were being carried rather than scaled
            // and its top decoration was reaching the legs again.
            //
            // Kept, but it was read against a road width that has since moved twenty metres: the table
            // said the pylon had been drawn for an 18 m road when the bridges carrying it are 38 m, so
            // the bridge it was measured on was twenty metres wider than it should have been. Two
            // metres more than that is not two metres more than this. Worth re-reading.
            // 2, then +12, +6, -2, each read in the game after the reading before it was acted on: 18.
            ["Extradosed03"] = 18f,
        };


    /// <summary>
    /// Whether a style's archetype carries railings of its own along the deck.
    ///
    /// Recorded, because nothing in an archetype declares it. The golden family's are golden and live
    /// in its support mesh; the V pylon's are its own too. A road always brings a railing when it is
    /// elevated, so on these two the deck ends up with both, side by side and a hand's breadth apart.
    ///
    /// A style not named here keeps the road's, which is right: most bridge archetypes have none of
    /// their own and the road's railing is the only one there is.
    /// </summary>
    internal static bool BringsItsOwnRailings(string? styleId) =>
        styleId is "SuspensionGolden";

    /// <summary>Extra widening this style's towers take beyond what the road gives. Zero for most.</summary>
    internal static float BonusFor(string? styleId)
    {
        if (styleId == null) return 0f;
        return TowerBonus.TryGetValue(styleId, out var bonus) ? bonus : 0f;
    }

    /// <summary>
    /// Converts the target-minus-prototype-road difference into the one width change every piece of
    /// the bridge structure receives. Keeping this operation in one place prevents the tower and
    /// cables from taking a prototype allowance while node-bound props still use the raw road delta.
    /// </summary>
    internal static float StructureExtraFor(string? styleId, float roadExtra) =>
        roadExtra + BonusFor(styleId);
}
