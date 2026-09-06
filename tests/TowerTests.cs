using BridgeBuilder.Bridges;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgeBuilder.Tests;

/// <summary>
/// The tower rules, checked without a running game.
///
/// Everything here exercises plain arithmetic over vertex arrays and a hand-recorded table, which is
/// the part that can be got wrong silently. The one thing it cannot check is that the game's own
/// meshes come back unchanged - that needs the meshes, and those only exist once the game has loaded
/// them - so TowerSelfTest repeats the same comparison in the game against real geometry. This runs
/// first and takes a second; that one confirms it against the real thing.
/// </summary>
internal static class TowerTests
{
    private static int _failures;

    private static int Main()
    {
        Console.WriteLine("Tower tests");
        Console.WriteLine(new string('-', 60));

        IdentityAtZeroShift();
        WidensSymmetrically();
        LeavesHeightAndDepthAlone();
        LeavesTheCentreLineAlone();
        WideningIsReversible();
        DoesNotModifyTheInput();
        SuspensionPicksTheFourLaneTowerAtTwentyMetres();
        SuspensionPicksTheThreeLaneTowerAtSixteenMetres();
        EveryTowerListIsOrderedByRoadWidth();
        EveryTowerIsLabelledByItsMeasurement();
        CablesMatchTheFourLanePillarAtTwentyMetres();
        CablesMatchTheThreeLanePillarAtSixteenMetres();
        EveryTowerIsItsOwnAnswer();
        VerifiedRoadWidthsAgreeWithTheScan();
        EveryWidthIsMeasured();
        EveryTypeStillHasAPortal();
        CablesAndLegsMoveTogether();
        SpreadIsTheRuleWidenUses();
        SideSectionsAreTheOutwardExtension();
        RoadSurfaceSectionsAreNotSides();
        TheSixLaneDualCarriagewayMeasuresForty();
        TowerGenerationTests.Run(Check);

        Console.WriteLine(new string('-', 60));
        Console.WriteLine(_failures == 0 ? "All tests passed." : $"{_failures} test(s) FAILED.");
        return _failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// The property the whole design rests on: asked for the width a tower already is, the rule must
    /// return that tower untouched.
    /// </summary>
    private static void IdentityAtZeroShift()
    {
        var vertices = Portal();
        var result = TowerWidening.Widen(vertices, 0f);

        Check(
            "identity at zero shift",
            vertices.Length == result.Length
                && vertices.Zip(result, (a, b) => math.distance(a, b) < 1e-6f).All(same => same));
    }

    private static void WidensSymmetrically()
    {
        var vertices = Portal();
        var result = TowerWidening.Widen(vertices, 8f);

        // A leg at -10 goes to -14, its opposite at +10 to +14: the opening grows by the full 8 m.
        Check("left leg moved out by half", math.abs(result[0].x - (-14f)) < 1e-5f);
        Check("right leg moved out by half", math.abs(result[2].x - 14f) < 1e-5f);
        Check("width grew by the full shift", math.abs(TowerWidening.WidthOf(result) - 28f) < 1e-5f);
    }

    private static void LeavesHeightAndDepthAlone()
    {
        var vertices = Portal();
        var result = TowerWidening.Widen(vertices, 6f);

        var moved = vertices.Zip(result, (a, b) => math.abs(a.y - b.y) + math.abs(a.z - b.z)).Sum();
        Check("height and depth untouched", moved < 1e-5f);
    }

    private static void LeavesTheCentreLineAlone()
    {
        var vertices = new[] { new float3(0f, 5f, 0f), new float3(0.0005f, 5f, 0f) };
        var result = TowerWidening.Widen(vertices, 10f);

        Check("centre vertex stays", math.abs(result[0].x) < 1e-6f);
        Check("near-centre vertex stays", math.abs(result[1].x - 0.0005f) < 1e-6f);
    }

    private static void WideningIsReversible()
    {
        var vertices = Portal();
        var there = TowerWidening.Widen(vertices, 7.5f);
        var back = TowerWidening.Widen(there, -7.5f);

        Check(
            "widening then narrowing returns the original",
            vertices.Zip(back, (a, b) => math.distance(a, b) < 1e-5f).All(same => same));
    }

    /// <summary>The caller compares before against after, so the rule must not write into its input.</summary>
    private static void DoesNotModifyTheInput()
    {
        var vertices = Portal();
        var first = vertices[0].x;
        TowerWidening.Widen(vertices, 12f);

        Check("input array untouched", math.abs(vertices[0].x - first) < 1e-6f);
    }

    private static void SuspensionPicksTheFourLaneTowerAtTwentyMetres()
    {
        var tower = TowerFor("Suspension", 20f);
        Check(
            "a 20 m road takes the four-lane pillar",
            tower?.Name == "4LaneSuspensionBridgePillar Placeholder",
            tower?.Name);
        Check("and needs no widening", tower is { Road: 20 }, tower?.Road.ToString());
    }

    private static void SuspensionPicksTheThreeLaneTowerAtSixteenMetres()
    {
        var tower = TowerFor("Suspension", 16f);
        Check(
            "a 16 m road takes the three-lane pillar",
            tower?.Name == "3LaneSuspensionBridgePillar Placeholder",
            tower?.Name);
        Check("and needs no widening", tower is { Road: 16 }, tower?.Road.ToString());
    }

    /// <summary>Selection walks the list and stops at the first wide enough, so order is load-bearing.</summary>
    private static void EveryTowerListIsOrderedByRoadWidth()
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            var roads = BridgeTowers.For(styleId).Select(tower => tower.Road).ToArray();
            var sorted = roads.OrderBy(road => road).ToArray();
            Check($"[{styleId}] listed narrowest road first", roads.SequenceEqual(sorted));
        }
    }

    /// <summary>
    /// The measurement decides what an entry is, and the label has to agree with it.
    ///
    /// A portal spans the road it stands on; a support does not, because it is a column under the deck
    /// rather than something the road passes through. Equal counts as spanning - a tower whose legs sit
    /// exactly at the road edge is still a portal, and the golden pylon measures that way.
    ///
    /// Both directions are checked. A support labelled as a portal gets stretched from a width that is
    /// not a road width; a portal labelled as a support is silently dropped from selection and its
    /// bridge type loses a tower it had.
    /// </summary>
    private static void EveryTowerIsLabelledByItsMeasurement()
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                var spans = tower.Mesh >= tower.Road;
                Check(
                    "[" + styleId + "] " + tower.Name + (tower.Support ? " is a support" : " spans its road"),
                    spans != tower.Support,
                    "mesh " + tower.Mesh + " vs road " + tower.Road);
            }
        }
    }

    /// <summary>
    /// The property that keeps a suspension bridge looking like one: whatever hangs off the tower has
    /// to travel exactly as far as the tower legs do.
    ///
    /// A cable authored 1 m outboard of a leg stays 1 m outboard of it at any deck width. Under the
    /// scale this replaced it did not - the gap grew with the ratio, which is how the cables ended up
    /// over the carriageway instead of down either side of it.
    /// </summary>
    private static void CablesAndLegsMoveTogether()
    {
        const float leg = 12f;
        const float cable = 13f;

        foreach (var extra in new[] { 0f, 4f, 16f, 28f, -4f })
        {
            var movedLeg = TowerWidening.Spread(leg, extra);
            var movedCable = TowerWidening.Spread(cable, extra);

            Check(
                "cable stays 1 m outboard of the leg at " + extra.ToString(CultureInfo.InvariantCulture) + " m extra",
                Math.Abs((movedCable - movedLeg) - (cable - leg)) < 1e-4f,
                (movedCable - movedLeg).ToString(CultureInfo.InvariantCulture));
        }

        Check("nothing moves at the authored width", Math.Abs(TowerWidening.Spread(cable, 0f) - cable) < 1e-6f);
        Check("the centre line never moves", Math.Abs(TowerWidening.Spread(0f, 20f)) < 1e-6f);

        // A prop on the left goes left, not towards the middle.
        Check("the left side moves left", TowerWidening.Spread(-12f, 16f) < -12f);
        Check("the right side moves right", TowerWidening.Spread(12f, 16f) > 12f);
    }

    /// <summary>
    /// The mesh and the props must be moved by one rule, not two that agree today. This checks the
    /// vertex path and the prop path against each other rather than against a constant.
    /// </summary>
    private static void SpreadIsTheRuleWidenUses()
    {
        var vertices = Portal();
        var widened = TowerWidening.Widen(vertices, 9f);

        var same = true;
        for (var index = 0; index < vertices.Length; index++)
        {
            if (Math.Abs(widened[index].x - TowerWidening.Spread(vertices[index].x, 9f)) > 1e-5f) same = false;
        }

        Check("widening a mesh is spreading each vertex", same);
    }

    /// <summary>Every side section Road Builder can append, by the name it appends it under.</summary>
    private static void SideSectionsAreTheOutwardExtension()
    {
        string[] sides =
        {
            "Alley Side 0", "Highway Side 0", "Train Side 0", "Subway Side 0",
            "Gravel Side 0", "Tiled Side 0", "Pavement Path Side Section 0",
        };

        foreach (var name in sides)
        {
            Check("'" + name + "' is the outward extension", SectionNames.IsSide(name));
        }
    }

    /// <summary>
    /// The trap the rule exists to avoid. A footway is road - a tower has to span it - and a substring
    /// test would drop every pavement on every road it measured, which reads as a plausible number.
    /// </summary>
    private static void RoadSurfaceSectionsAreNotSides()
    {
        string[] road =
        {
            "Sidewalk", "Sidewalk 3.5", "Sidewalk 4", "Wide Sidewalk 6",
            "Car Lane 4", "RB_Median_Piece_5", "Pavement Path 3", "Grass Median 5",
        };

        foreach (var name in road)
        {
            Check("'" + name + "' counts towards the road", !SectionNames.IsSide(name), "excluded");
        }

        Check("nothing is read out of an empty name", !SectionNames.IsSide(string.Empty));
        Check("nothing is read out of a missing name", !SectionNames.IsSide(null));
    }

    /// <summary>
    /// The road that found the bug, added up the way the game lays it out.
    ///
    /// Two footways, two empty lanes, six car lanes and a median, with a side section appended at each
    /// end: thirteen sections for eleven lanes. The road is 40 m. Counting the sides makes it 42 m and
    /// builds a 42 m tower for a 40 m road.
    /// </summary>
    private static void TheSixLaneDualCarriagewayMeasuresForty()
    {
        (string Name, float Width)[] sections =
        {
            ("Alley Side 0", 1f),
            ("Sidewalk 3.5", 3.5f), ("RB_Empty_Piece_2", 2f),
            ("Car Lane 4", 4f), ("Car Lane 4", 4f), ("Car Lane 4", 4f),
            ("RB_Median_Piece_5", 5f),
            ("Car Lane 4", 4f), ("Car Lane 4", 4f), ("Car Lane 4", 4f),
            ("RB_Empty_Piece_2", 2f), ("Sidewalk 3.5", 3.5f),
            ("Alley Side 0", 1f),
        };

        var road = 0f;
        var everything = 0f;
        foreach (var (name, width) in sections)
        {
            everything += width;
            if (!SectionNames.IsSide(name)) road += width;
        }

        Check("thirteen sections for eleven lanes", sections.Length == 13);
        Check("counting the sides gives the old wrong 42 m", Math.Abs(everything - 42f) < 1e-5f, everything.ToString(CultureInfo.InvariantCulture));
        Check("the road surface is 40 m", Math.Abs(road - 40f) < 1e-5f, road.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The cables of a 20 m suspension bridge sit where 4LaneSuspensionBridgePillar's cables sit.
    ///
    /// Not "close to" - exactly. At a road the game already has a bridge for, the bridge this mod
    /// builds must be that bridge: same tower, same cables, nothing moved. That only holds if the tower
    /// selected for 20 m is the four-lane one AND the sideways shift computed from it is zero, because
    /// every cable, hanger and deck prop is placed by that one number.
    ///
    /// The bug this pins down had both halves disagreeing: the donor bridge was chosen on the road's
    /// declared width and came back carrying the five-lane pylon, while the tower was derived from the
    /// four-lane one. The shift was zero, so the cables kept the five-lane spacing - two metres too far
    /// out on each side - over a four-lane tower.
    /// </summary>
    private static void CablesMatchTheFourLanePillarAtTwentyMetres()
    {
        CablesAreUnmoved("Suspension", 20f, "4LaneSuspensionBridgePillar Placeholder");
    }

    /// <summary>The same standard one tower down: a 16 m road is the three-lane bridge, untouched.</summary>
    private static void CablesMatchTheThreeLanePillarAtSixteenMetres()
    {
        CablesAreUnmoved("Suspension", 16f, "3LaneSuspensionBridgePillar Placeholder");
    }

    /// <summary>
    /// At the road a tower was built for, the whole bridge is that bridge: the tower is selected, the
    /// shift is zero, and everything hanging off it stays put.
    /// </summary>
    private static void CablesAreUnmoved(string styleId, float width, string expected)
    {
        var label = width.ToString("0.#", CultureInfo.InvariantCulture) + " m";
        var tower = BridgeTowers.Select(styleId, width);

        Check($"[{styleId}] {label} selects {expected}", tower?.Name == expected, tower?.Name);
        if (tower == null) return;

        var extra = width - tower.Value.Road;
        Check($"[{styleId}] {label} needs no widening", Math.Abs(extra) < 1e-5f,
            extra.ToString(CultureInfo.InvariantCulture));

        // Where a suspension bridge actually puts things: main cables outboard of the legs, hangers
        // just inside them, lighting near the centre. None of it may move.
        var moved = 0;
        foreach (var offset in new[] { -14.5f, -12.75f, -9f, -3f, 0f, 3f, 9f, 12.75f, 14.5f })
        {
            if (Math.Abs(TowerWidening.Spread(offset, extra) - offset) > 1e-6f) moved++;
        }

        Check($"[{styleId}] {label} leaves every cable and prop where the donor put it", moved == 0,
            moved + " moved");
    }

    /// <summary>
    /// The same property for every tower of every type, so the two anchors above are a rule rather
    /// than two cases that happen to work. A type whose selection at its own recorded width returns a
    /// different tower would build a bridge out of two bridges' parts.
    /// </summary>
    private static void EveryTowerIsItsOwnAnswer()
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                if (tower.Support) continue;

                var selected = BridgeTowers.Select(styleId, tower.Road);
                // Several towers can be built for the same road - a bridge carrying both a pylon and a
                // pillar of the same span, for instance - so what is required is a tower built for that
                // road, not this particular one. The shift is what has to be zero.
                Check("[" + styleId + "] a " + tower.Road + " m road selects a tower built for it",
                    selected.HasValue && selected.Value.Road == tower.Road,
                    selected?.Name + " at " + selected?.Road);
            }
        }
    }

    /// <summary>
    /// Every road width claimed as verified has to be reachable from the independent scan, which
    /// recorded the same bridges from the other side - per bridge rather than per tower.
    ///
    /// The two differ by exactly the two metres of side section the scan counted as carriageway, so a
    /// verified tower built for an N metre road must sit against a bridge measured at N+2. This is what
    /// makes the suspension family's 12/16/20/24 a measurement rather than four numbers that produced
    /// the right answer twice; a typo in any of them stops matching anything.
    /// </summary>
    private static void VerifiedRoadWidthsAgreeWithTheScan()
    {
        var scanned = new HashSet<int>();
        foreach (var entry in BridgeMeasurements.All) scanned.Add(entry.Value.Road);

        // Only the suspension family. It is the one whose widths were fixed against the game before
        // anything was measured in bulk, so it is the one that can hold the measurement honest: the
        // live figures have to land back on the numbers that were already known to be right.
        foreach (var tower in BridgeTowers.For("Suspension"))
        {
            Check(
                "[Suspension] " + tower.Name + ": " + tower.Road + " m matches a bridge scanned at "
                + (tower.Road + SideSections) + " m",
                scanned.Contains(tower.Road + SideSections));
        }

        var roads = BridgeTowers.For("Suspension").Select(tower => tower.Road).ToArray();
        Check("the suspension family is still 12/16/20/24",
            roads.SequenceEqual(new[] { 12, 16, 20, 24 }),
            string.Join("/", roads));
    }

    /// <summary>
    /// Every width in the table now comes from a measurement taken in a running game, and every one of
    /// them has to say so.
    ///
    /// An unmarked entry is a number somebody typed. That is how the golden suspension tower came to
    /// hold the blue five-lane tower's width, and nothing downstream could tell - a bridge built from
    /// it is simply the wrong size. Marking is not proof of correctness, but it separates what was read
    /// off the game from what was arrived at some other way, and only the first kind belongs here.
    /// </summary>
    private static void EveryWidthIsMeasured()
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                Check("[" + styleId + "] " + tower.Name + " is a measured width", tower.Verified);
            }
        }
    }

    /// <summary>
    /// Every type still has something to build from. Supports are kept in the table because they were
    /// measured, but selection passes over them, so a type whose entries are all supports would fall
    /// back silently to whatever the pack-family ranking turns up.
    ///
    /// Unless it has no portal by design. A through arch can be spanned by a measured overhead section
    /// rather than an object, with its only object being a pillar narrower than the road. That is
    /// recorded separately from a style whose whole structure is left unchanged: the section is still
    /// widened, while the support is copied into the generated bridge without treating it as a portal.
    /// </summary>
    private static void EveryTypeStillHasAPortal()
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            var portals = BridgeTowers.For(styleId).Count(tower => !tower.Support);
            var notDerived = BridgeTowers.NotDerivedReason(styleId);

            if (notDerived != null)
            {
                Check("[" + styleId + "] has no portal to derive",
                    portals == 0, portals.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            var measuredOverhead = BridgeTowers.For(styleId)
                .Where(tower => tower.Support)
                .Any(tower => BridgeCables.For(tower.Name) != null);
            if (portals == 0 && measuredOverhead)
            {
                Check("[" + styleId + "] has measured overhead structure instead of a portal",
                    portals == 0, portals.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            Check("[" + styleId + "] has a tower the road passes through", portals > 0,
                portals.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>A side section at each end, one metre apiece - what the scan counted and a tower does not span.</summary>
    private const int SideSections = 2;


    /// <summary>The same walk BridgeStyle does, without needing a style instance.</summary>
    private static BridgeTowers.Tower? TowerFor(string styleId, float width)
    {
        BridgeTowers.Tower? widest = null;
        foreach (var tower in BridgeTowers.For(styleId))
        {
            widest = tower;
            if (tower.Road >= width - 0.05f) return tower;
        }

        return widest;
    }

    /// <summary>A portal 20 m across the opening: two legs and a beam, eight vertices.</summary>
    private static float3[] Portal()
    {
        return new[]
        {
            new float3(-10f, 0f, -1f),
            new float3(-10f, 0f, 1f),
            new float3(10f, 0f, -1f),
            new float3(10f, 0f, 1f),
            new float3(-10f, 30f, 0f),
            new float3(10f, 30f, 0f),
            new float3(0f, 32f, 0f),
            new float3(0f, 28f, 0f),
        };
    }

    private static void Check(string what, bool passed, string? detail = null)
    {
        if (passed)
        {
            Console.WriteLine($"  pass  {what}");
            return;
        }

        _failures++;
        Console.WriteLine(
            string.Format(CultureInfo.InvariantCulture, "  FAIL  {0}{1}", what, detail == null ? string.Empty : $" (got {detail})"));
    }
}
