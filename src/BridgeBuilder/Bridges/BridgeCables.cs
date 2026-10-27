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

        /// <summary>The tower in <see cref = "BridgeTowers"/> whose bridge carries this section.</summary>
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

    private static readonly Cables[] Measured = ReadPackedMeasured();
    /// <summary>Everything measured, for the tests to hold the tower table against.</summary>
    internal static IEnumerable<Cables> All => Measured;

    /// <summary>The section carried alongside a tower, or null when none was measured.</summary>
    internal static Cables? For(string towerName)
    {
        foreach (var entry in Measured)
        {
            if (string.Equals(entry.Tower, towerName, StringComparison.Ordinal))
                return entry;
        }

        return null;
    }

    /// <summary>
    /// How far a suspension or cable-stayed bridge's cables hang outside the carriageway, both sides
    /// together. Constant across every size the game ships of both designs.
    /// </summary>
    internal static float Margin => Numeric_BridgeCables_cs.Values.F32[19];
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
    internal static float Overhang => Numeric_BridgeCables_cs.Values.F32[20];
    /// <summary>The golden family's, measured the same way: a 50.4 m pylon over a 24 m carriageway.</summary>
    internal static float GoldenOverhang => Numeric_BridgeCables_cs.Values.F32[21];

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
    internal static int PieceFlagDisableTiling => Numeric_BridgeCables_cs.Values.I32[0];
    internal static int PieceFlagPreserveShape => Numeric_BridgeCables_cs.Values.I32[1];
    internal static int PieceFlagBlockTraffic => Numeric_BridgeCables_cs.Values.I32[2];
    internal static int PieceFlagBlockCrosswalk => Numeric_BridgeCables_cs.Values.I32[3];
    internal static int PieceFlagSurface => Numeric_BridgeCables_cs.Values.I32[4];
    internal static int PieceFlagLowerBottomToTerrain => Numeric_BridgeCables_cs.Values.I32[5];
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
    internal static float TowerBaseOutsideCables => Numeric_BridgeCables_cs.Values.F32[22];
    internal static float TowerLegOutsideCables => Numeric_BridgeCables_cs.Values.F32[23];
    internal static float TowerTopOutsideCables => Numeric_BridgeCables_cs.Values.F32[24];

    /// <summary>
    /// Which of the three a part is, by its place in the stack - base at the bottom, top at the end,
    /// legs between. A tower of one part is a placeholder, and a placeholder carries the top alone:
    /// the archetype's placeholder mesh is its top mesh, same vertex count and same reach across.
    /// </summary>
    internal static float TowerOutsideCables(int part, int parts) => Suspension.For(part, parts);
    /// <summary>The five lane suspension bridge, as a <see cref = "Spacing"/> - the family these came from.</summary>
    internal static Spacing Suspension => new("5LaneSuspensionBridgePillar Placeholder", TowerBaseOutsideCables, TowerLegOutsideCables, TowerTopOutsideCables);
    /// <summary>
    /// How far the measured distance may differ before it is reported.
    ///
    /// A centimetre. The archetypes agree to a hundredth of that, and the widening moves both edges by
    /// the same half of the same number, so anything above this is a difference in kind - a tower and a
    /// cable section that came from different bridges - rather than rounding.
    /// </summary>
    internal static float SpacingTolerance => Numeric_BridgeCables_cs.Values.F32[25];

    /// <summary>Whether a measured distance is the archetype's, within <see cref = "SpacingTolerance"/>.</summary>
    internal static bool SpacingHolds(Spacing spacing, float measured, int part, int parts)
    {
        return Math.Abs(measured - spacing.For(part, parts)) <= SpacingTolerance;
    }

    /// <summary>
    /// How much wider than its archetype a tower has to be to stand the archetype's distance outside
    /// cables whose outer edge is at <paramref name = "cableOuter"/>.
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
    internal static float ExtraForTower(Spacing spacing, float cableOuter, float towerOuter, int part, int parts)
    {
        return Numeric_BridgeCables_cs.Values.F32[26] * (cableOuter + spacing.For(part, parts) - towerOuter);
    }

    /// <summary>Which part of a tower stands beside the cables: the legs, or a placeholder's only part.</summary>
    internal static int LegIndexOf(int parts) => parts >= Numeric_BridgeCables_cs.Values.I32[6] ? Numeric_BridgeCables_cs.Values.I32[7] : Numeric_BridgeCables_cs.Values.I32[8];
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
            if (parts <= Numeric_BridgeCables_cs.Values.I32[9])
                return Top;
            if (part <= Numeric_BridgeCables_cs.Values.I32[10])
                return Base;
            return part >= parts - Numeric_BridgeCables_cs.Values.I32[11] ? Top : Leg;
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
    /// <see cref = "Cables.Outer"/> is the recorded test for that, and both have to hold before a tower is
    /// sized against its cables.
    /// </summary>
    private static readonly Spacing[] Spacings = ReadPackedSpacings();
    /// <summary>The measured distances for a tower, or null when none were taken.</summary>
    internal static Spacing? SpacingFor(string towerName)
    {
        foreach (var entry in Spacings)
        {
            if (string.Equals(entry.Tower, towerName, StringComparison.Ordinal))
                return entry;
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
        if (cables == null || !cables.Value.Outer)
            return null;
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
        if (cables == null || cables.Value.Width <= Numeric_BridgeCables_cs.Values.F32[33])
            return Numeric_BridgeCables_cs.Values.F32[34];
        // Losing the whole of it leaves nothing; a metre of margin keeps the result a structure rather
        // than a seam.
        return authoredRoad - cables.Value.Width + Numeric_BridgeCables_cs.Values.F32[35];
    }

    private static Cables[] ReadPackedMeasured()
    {
        var d = Numeric_BridgeCables_cs.Values;
        var values = new Cables[19];
        for (int i = 0; i < 1; i++)
            values[0 + i] = new Cables("2LaneSuspensionBridgePillar Placeholder", "2-Lane Suspension Bridge", d.F32[0 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[1 + i] = new Cables("3LaneSuspensionBridgePillar Placeholder", "3-Lane Suspension Bridge", d.F32[1 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[2 + i] = new Cables("4LaneSuspensionBridgePillar Placeholder", "4-Lane Suspension Bridge", d.F32[2 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[3 + i] = new Cables("5LaneSuspensionBridgePillar Placeholder", "5-Lane Suspension Bridge", d.F32[3 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[4 + i] = new Cables("8LaneCableStayedBridgePillar Placeholder", "8-Lane Cable Stayed Bridge 00", d.F32[4 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[5 + i] = new Cables("PedestrianBridgeCableStayedPillar Placeholder", "Cable Stayed Pedestrian Bridge", d.F32[5 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[6 + i] = new Cables("ExtradosedBridge01NetPillar", "ExtradosedBridge01 Section", d.F32[6 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[7 + i] = new Cables("ExtradosedBridge02NetPillar", "ExtradosedBridge02 Section", d.F32[7 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[8 + i] = new Cables("ExtradosedBridge03NetPillar", "ExtradosedBridge03 Section", d.F32[8 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[9 + i] = new Cables("SuspensionBridge03NetPylon", "SuspensionBridge03 Section", d.F32[9 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[10 + i] = new Cables("SuspensionBridge03NetPillar", "SuspensionBridge03 Section", d.F32[10 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[11 + i] = new Cables("PedestrianBridgeCoveredWood01NetPillar", "PedestrianBridgeCoveredWood01 Section", d.F32[11 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[12 + i] = new Cables("GrandBridgePylon Placeholder", "Grand Bridge", d.F32[12 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[13 + i] = new Cables("GrandBridgePillar Placeholder", "Grand Bridge", d.F32[13 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[14 + i] = new Cables("LiftBridge01", "LiftBridge01 Section", d.F32[14 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[15 + i] = new Cables("LiftBridge03", "LiftBridge03 Section", d.F32[15 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[16 + i] = new Cables("TrussArchBridge01NetPillar", "TrussArchBridge01 Section", d.F32[16 + i * 1], outer: true);
        for (int i = 0; i < 1; i++)
            values[17 + i] = new Cables("TrussArchBridge02NetPillar", "TrussArchBridge02 Section", d.F32[17 + i * 1]);
        for (int i = 0; i < 1; i++)
            values[18 + i] = new Cables("TrussArchBridge03NetPillar", "TrussArchBridge03 Section", d.F32[18 + i * 1]);
        return values;
    }

    private static Spacing[] ReadPackedSpacings()
    {
        var d = Numeric_BridgeCables_cs.Values;
        var values = new Spacing[2];
        for (int i = 0; i < 1; i++)
            values[0 + i] = new Spacing("5LaneSuspensionBridgePillar Placeholder", d.F32[27 + i * 3], d.F32[28 + i * 3], d.F32[29 + i * 3]);
        for (int i = 0; i < 1; i++)
            values[1 + i] = new Spacing("4LaneSuspensionBridgePillar Placeholder", d.F32[30 + i * 3], d.F32[31 + i * 3], d.F32[32 + i * 3]);
        return values;
    }
}

// Embedded numerical arrays: BridgeCables_cs
internal static class Numeric_BridgeCables_cs
{
    static Numeric_BridgeCables_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("BridgeCables_cs");
}
