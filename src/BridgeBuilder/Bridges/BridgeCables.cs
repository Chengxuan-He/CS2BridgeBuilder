using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The overhead section each tower's bridge carries - the cables - and how wide it is, measured in a
/// running game.
///
/// A second measurement of the same thing, which is why it is worth writing down separately. A bridge
/// states its width three times over: the road it declares, the tower that straddles it, and the cable
/// section drawn above it. The road is the one that cannot be trusted - it comes back wrong on every
/// divided bridge, 75 m for eight lanes and 61 m for six - while the other two are read from geometry
/// and agree with each other.
///
/// So they are used to check it. Across the suspension family the relationships are exact and constant:
///
///     tower   = road + 10      22/12, 26/16, 30/20, 34/24
///     cables  = road + 3       15/12, 19/16, 23/20, 27/24
///
/// Ten metres of tower standing outside the carriageway, five each side; three metres of cable, a metre
/// and a half each side. The eight-lane cable-stayed bridge has a 42 m tower and a 35 m cable section,
/// and 32 is the only road width that satisfies both - which is how its width was arrived at after its
/// own measurement came back as 75.
///
/// The margin is not universal, and neither is the arrangement. A footbridge is built to a different
/// scale and has a metre of margin rather than three. An extradosed bridge fans its cables from a low
/// pylon over the deck; a lift bridge's section is its lifting mechanism; the golden bridge's section
/// is a stiffening truss, its pieces named SupportEnd and SupportMiddle, with the main cables on the
/// pylon object instead. Those sections are narrower than their road and are right to be. Only the
/// ones marked Outer are the envelope the road runs between, and only those have to stay outside it.
/// </summary>
internal static class BridgeCables
{
    /// <summary>One measurement: the section a tower's bridge carries, and how wide it is.</summary>
    internal readonly struct Cables
    {
        internal Cables(string tower, string section, float width, bool outer = false)
        {
            Tower = tower;
            Section = section;
            Width = width;
            Outer = outer;
        }

        /// <summary>The tower in <see cref="BridgeTowers"/> whose bridge carries this section.</summary>
        internal string Tower { get; }

        internal string Section { get; }

        /// <summary>Metres across, from the section's own pieces.</summary>
        internal float Width { get; }

        /// <summary>
        /// Whether this section is the bridge's outer envelope - the main cables, running down either
        /// side of the carriageway - or structure inboard of it.
        ///
        /// The distinction is in the pieces. A suspension or cable-stayed bridge hangs its deck from
        /// cables outside the road, so its section is wider than the road and the two move together. An
        /// extradosed bridge fans its cables from a low pylon over the deck, a lift bridge's section is
        /// the lifting mechanism, and the golden bridge's section is a stiffening truss whose pieces
        /// are named SupportEnd and SupportMiddle - its main cables live on the pylon object instead.
        /// All of those are narrower than the road they belong to, correctly.
        /// </summary>
        internal bool Outer { get; }
    }

    private static readonly Cables[] Measured =
    {
        new Cables("2LaneSuspensionBridgePillar Placeholder", "2-Lane Suspension Bridge", 15f, outer: true),
        new Cables("3LaneSuspensionBridgePillar Placeholder", "3-Lane Suspension Bridge", 19f, outer: true),
        new Cables("4LaneSuspensionBridgePillar Placeholder", "4-Lane Suspension Bridge", 23f, outer: true),
        new Cables("5LaneSuspensionBridgePillar Placeholder", "5-Lane Suspension Bridge", 27f, outer: true),
        new Cables("8LaneCableStayedBridgePillar Placeholder", "8-Lane Cable Stayed Bridge 00", 35f, outer: true),
        new Cables("PedestrianBridgeCableStayedPillar Placeholder", "Cable Stayed Pedestrian Bridge", 4f, outer: true),
        new Cables("ExtradosedBridge01NetPillar", "ExtradosedBridge01 Section", 31.6f),
        new Cables("ExtradosedBridge02NetPillar", "ExtradosedBridge02 Section", 23f),
        new Cables("ExtradosedBridge03NetPillar", "ExtradosedBridge03 Section", 33.1f),
        // Outer, on the corrected road: 33.8 m of cable over a 32 m carriageway, 1.8 m clear each
        // side. It was not marked so while the road was recorded at 50, because 33.8 m cannot be the
        // envelope of a 50 m road - the contradiction that named the wrong number.
        new Cables("SuspensionBridge03NetPylon", "SuspensionBridge03 Section", 33.8f, outer: true),
        new Cables("SuspensionBridge03NetPillar", "SuspensionBridge03 Section", 33.8f, outer: true),
        // The housing a covered bridge is spanned by, and the envelope its path runs through: 9.5 m
        // over a 6 m path. Not marked so while the road was recorded at 16, for the usual reason.
        new Cables("PedestrianBridgeCoveredWood01NetPillar", "PedestrianBridgeCoveredWood01 Section",
            9.5f, outer: true),

        new Cables("GrandBridgePylon Placeholder", "Grand Bridge", 21f),
        new Cables("GrandBridgePillar Placeholder", "Grand Bridge", 21f),
        new Cables("LiftBridge01", "LiftBridge01 Section", 8f),
        new Cables("LiftBridge03", "LiftBridge03 Section", 21.2f),
        // Outer, on the corrected road: 15.4 m of arch over a 10 m carriageway, which the road runs
        // between. It was not marked so while the road was recorded at 20, because 15.4 m cannot be
        // the envelope of a 20 m road - the same contradiction from the other direction.
        new Cables("TrussArchBridge01NetPillar", "TrussArchBridge01 Section", 15.4f, outer: true),
        new Cables("TrussArchBridge02NetPillar", "TrussArchBridge02 Section", 20.8f),
        new Cables("TrussArchBridge03NetPillar", "TrussArchBridge03 Section", 14.1f),
    };

    /// <summary>Everything measured, for the tests to hold the tower table against.</summary>
    internal static IEnumerable<Cables> All => Measured;

    /// <summary>The section carried alongside a tower, or null when none was measured.</summary>
    internal static Cables? For(string towerName)
    {
        foreach (var entry in Measured)
        {
            if (string.Equals(entry.Tower, towerName, StringComparison.Ordinal)) return entry;
        }

        return null;
    }

    /// <summary>
    /// How far a suspension or cable-stayed bridge's cables hang outside the carriageway, both sides
    /// together. Constant across every size the game ships of both designs.
    /// </summary>
    internal const float Margin = 3f;

    /// <summary>
    /// How far the tower stands outside the carriageway, both sides together, for the blue suspension
    /// family - and the check that fixed a road width when its own measurement was unusable.
    ///
    /// The cable-stayed family was here too, and that is how its road came to be inferred as 32: its
    /// own measurement is unusable, and 42 m of tower with 35 m of cable gave ten and three exactly
    /// against 32. Seen in the game the tower was a metre wide. At 33 the numbers are 9 and 2, so the
    /// family has its own and the inference was a good one that was wrong.
    ///
    /// Not universal, which the doc here once claimed and the data does not. The golden family's pylon
    /// is 50.4 m across a 24 m road: an overhang of 26.4, its own number. Applying ten to it produced a
    /// guess of 40 for a road that turned out to be 24, and the guess being wrong was read at the time
    /// as confirming the 50 it replaced. Three families, three overhangs, and every attempt to carry
    /// one to another has cost a round - rule 9.
    /// </summary>
    internal const float Overhang = 10f;

    /// <summary>The golden family's, measured the same way: a 50.4 m pylon over a 24 m carriageway.</summary>
    internal const float GoldenOverhang = 26.4f;

    /// <summary>
    /// A cable piece disables texture tiling, and that is not only about textures.
    ///
    /// The archetype's piece carries <c>NetPieceTiling</c> with <c>m_DisableTextureTiling</c> set.
    /// <c>NetInitializeSystem</c> turns it into <c>NetPieceFlags.DisableTiling</c> on the composition
    /// piece, and <c>NetCompositionHelpers.CalculateCompositionPieceOffsets</c> lays the pieces of a
    /// composition out in separate groups chosen by that flag, each group packed along its own running
    /// cursor. A piece without the flag is packed among the road's own surface pieces; a piece with it
    /// is laid out in its own group, spanning the width rather than queueing beside the carriageway.
    ///
    /// The flag also decides whether the piece contributes its surface heights to the composition's
    /// edge heights - a cable sheet is seventy-odd metres tall and has no business setting the height
    /// of the road's edge.
    ///
    /// A generated piece copied the archetype's fields and none of its components, so it had neither.
    /// The cables were the right width and in the wrong place, which read as a widening fault and was
    /// not one: the mesh was fine and the composition put it somewhere else.
    /// </summary>
    internal const bool PieceDisablesTextureTiling = true;

    /// <summary>
    /// <c>NetPieceFlags.DisableTiling</c> and its neighbours, so the value above can be checked against
    /// what the flag actually is rather than trusted.
    /// </summary>
    internal const int PieceFlagDisableTiling = 16;

    internal const int PieceFlagPreserveShape = 1;

    internal const int PieceFlagBlockTraffic = 2;

    internal const int PieceFlagBlockCrosswalk = 4;

    internal const int PieceFlagSurface = 8;

    internal const int PieceFlagLowerBottomToTerrain = 32;

    /// <summary>
    /// How far each part of a tower stands outside the cables, measured from the game's own bridges.
    ///
    /// The requirement, stated as data: a generated bridge puts its tower the same distance outside its
    /// cables as the archetype does, at every width. Measured on
    /// <c>Suspension Bridge - Highway Oneway - 5 Lanes</c> and confirmed on the 4-lane, which is a
    /// different road width and gives the same three numbers to five decimals:
    ///
    ///     part         5 lanes (road 24)   4 lanes (road 20)   outside the cables
    ///     base           18.75000            16.75000            5.27667
    ///     leg            17.01078            15.01078            3.53745
    ///     top            17.15220            15.15221            3.67887
    ///     cables         13.47333            11.47333            -
    ///
    /// It holds because both are carried outward by half the extra width and neither is scaled - the
    /// tower's legs by the rigid branch of the widening rule, the cable sheet by a stretch measured from
    /// the span it actually draws. Two independent code paths that happen to agree, which is exactly the
    /// kind of property that breaks without anything reporting it.
    ///
    /// It can break for a reason that is nobody's arithmetic error. The tower archetype is chosen by
    /// width from the recorded list, and the cables come from whichever installed bridge carries that
    /// tower - and the same tower is carried by several. Let those resolve to different bridges and the
    /// distance becomes tower(A) minus cables(B), which is not this constant and never was.
    /// </summary>
    internal const float TowerBaseOutsideCables = 5.27667f;

    internal const float TowerLegOutsideCables = 3.53745f;

    internal const float TowerTopOutsideCables = 3.67887f;

    /// <summary>
    /// Which of the three a part is, by its place in the stack - base at the bottom, top at the end,
    /// legs between. A tower of one part is a placeholder, and a placeholder carries the top alone:
    /// the archetype's placeholder mesh is its top mesh, same vertex count and same reach across.
    /// </summary>
    internal static float TowerOutsideCables(int part, int parts) =>
        Suspension.For(part, parts);

    /// <summary>The five lane suspension bridge, as a <see cref="Spacing"/> - the family these came from.</summary>
    internal static Spacing Suspension =>
        new("5LaneSuspensionBridgePillar Placeholder",
            TowerBaseOutsideCables, TowerLegOutsideCables, TowerTopOutsideCables);

    /// <summary>
    /// How far the measured distance may differ before it is reported.
    ///
    /// A centimetre. The archetypes agree to a hundredth of that, and the widening moves both edges by
    /// the same half of the same number, so anything above this is a difference in kind - a tower and a
    /// cable section that came from different bridges - rather than rounding.
    /// </summary>
    internal const float SpacingTolerance = 0.01f;

    /// <summary>Whether a measured distance is the archetype's, within <see cref="SpacingTolerance"/>.</summary>
    internal static bool SpacingHolds(Spacing spacing, float measured, int part, int parts)
    {
        return Math.Abs(measured - spacing.For(part, parts)) <= SpacingTolerance;
    }

    /// <summary>
    /// How much wider than its archetype a tower has to be to stand the archetype's distance outside
    /// cables whose outer edge is at <paramref name="cableOuter"/>.
    ///
    /// Solving <c>towerOuter + extra/2 == cableOuter + distance</c>. It comes out the same whichever
    /// part is measured, because the archetype satisfies all three distances at once - a placeholder's
    /// single part is its top at 3.67887 outside cables reaching 13.47333, the replacement's legs are
    /// at 3.53745 outside the same cables, and both reduce to twice however far the cables moved.
    ///
    /// The rule this replaces was the deck's width minus the road the tower was authored for. It gives
    /// the same answer whenever the tower and the cables came from the same bridge, which is why the
    /// two agreed to five decimals on every bridge measured. It stops agreeing when they did not come
    /// from the same bridge, and they need not: the tower is chosen by width from the recorded list and
    /// the cables come from whichever installed bridge carries it. Measuring against the cables cannot
    /// drift that way, because the cables are the thing the distance is to.
    /// </summary>
    internal static float ExtraForTower(
        Spacing spacing, float cableOuter, float towerOuter, int part, int parts)
    {
        return 2f * (cableOuter + spacing.For(part, parts) - towerOuter);
    }

    /// <summary>Which part of a tower stands beside the cables: the legs, or a placeholder's only part.</summary>
    internal static int LegIndexOf(int parts) => parts >= 3 ? 1 : 0;

    /// <summary>
    /// How far each part of one tower stands outside its cables. Measured, per tower.
    /// </summary>
    internal readonly struct Spacing
    {
        internal Spacing(string tower, float baseOutside, float legOutside, float topOutside)
        {
            Tower = tower;
            Base = baseOutside;
            Leg = legOutside;
            Top = topOutside;
        }

        internal string Tower { get; }

        internal float Base { get; }

        internal float Leg { get; }

        internal float Top { get; }

        /// <summary>Which of the three a part is, by its place in the stack.</summary>
        internal float For(int part, int parts)
        {
            if (parts <= 1) return Top;
            if (part <= 0) return Base;
            return part >= parts - 1 ? Top : Leg;
        }
    }

    /// <summary>
    /// The distances, for the towers they have been measured on.
    ///
    /// Two entries, and both are here because two is what makes them a design rather than a bridge.
    /// The five lane and four lane suspension bridges are 4 m apart in road width and give the same
    /// three numbers to five decimals:
    ///
    ///     part      5 lanes (road 24)   4 lanes (road 20)   outside the cables
    ///     base        18.75000            16.75000            5.27667
    ///     leg         17.01078            15.01078            3.53745
    ///     top         17.15220            15.15221            3.67887
    ///     cables      13.47333            11.47333            -
    ///
    /// The two and three lane towers of the same family are not here, because they have not been
    /// dumped. They are almost certainly the same numbers and almost certainly is not measured: a
    /// tower with no entry is sized by the road instead, which is what every tower was sized by before
    /// these were measured and is therefore not a regression.
    ///
    /// The other families have no entry for a stronger reason than not having been dumped. Their
    /// overhead section is not the envelope the road runs between - an extradosed bridge fans its
    /// cables from a low pylon over the deck and its section is 21 m against roads of 31 and 61, a lift
    /// bridge's section is its lifting mechanism - so there is no distance of this kind to measure.
    /// <see cref="Cables.Outer"/> is the recorded test for that, and both have to hold before a tower is
    /// sized against its cables.
    /// </summary>
    private static readonly Spacing[] Spacings =
    {
        new Spacing("5LaneSuspensionBridgePillar Placeholder", 5.27667f, 3.53745f, 3.67887f),
        new Spacing("4LaneSuspensionBridgePillar Placeholder", 5.27667f, 3.53745f, 3.67887f),
    };

    /// <summary>The measured distances for a tower, or null when none were taken.</summary>
    internal static Spacing? SpacingFor(string towerName)
    {
        foreach (var entry in Spacings)
        {
            if (string.Equals(entry.Tower, towerName, StringComparison.Ordinal)) return entry;
        }

        return null;
    }

    /// <summary>
    /// Whether a tower may be sized against its cables at all.
    ///
    /// Both conditions, and each rules out a different kind of bridge. The section has to be the
    /// envelope the road runs between, or the distance is to something that does not enclose the road
    /// and sizing against it is meaningless - the extradosed and lift families fail here. And the
    /// distances have to have been measured on this tower, or the numbers used would be another
    /// family's - which is what happened when they were held as three constants and applied to
    /// everything with an overhead section.
    /// </summary>
    internal static Spacing? SizingSpacingFor(string towerName)
    {
        var cables = For(towerName);
        if (cables == null || !cables.Value.Outer) return null;
        return SpacingFor(towerName);
    }

    /// <summary>
    /// The narrowest deck this style's structure can be fitted to, or zero when nothing is recorded.
    ///
    /// A structure is widened by moving its parts, and a part cannot lose more width than it has. The
    /// golden bridge is drawn for a 50 m road and its stiffening truss is 33.8 m across; fitted to a
    /// 16 m deck it is asked to lose 34 m, and the truss collapsed to a line - the report said "0 m
    /// across" and the bridge was written anyway.
    ///
    /// So the floor is where the narrowest recorded structure runs out. Below it the answer is not a
    /// narrower bridge, it is a different design, and refusing says so.
    /// </summary>
    internal static float NarrowestDeckFor(string towerName, float authoredRoad)
    {
        var cables = For(towerName);
        if (cables == null || cables.Value.Width <= 0f) return 0f;

        // Losing the whole of it leaves nothing; a metre of margin keeps the result a structure rather
        // than a seam.
        return authoredRoad - cables.Value.Width + 1f;
    }
}
