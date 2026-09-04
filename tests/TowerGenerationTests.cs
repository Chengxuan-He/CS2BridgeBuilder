using BridgePrefabGenerator.Bridges;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgePrefabGenerator.Tests;

/// <summary>
/// Generating a tower, for every bridge type, at every width - run without a game.
///
/// The pieces were already tested separately: the widening rule in <see cref="TowerTests"/>, the
/// recorded widths against the scan. What was not tested is the two of them together, and that is
/// where the failures have been. A three metre footbridge pillar was stretched thirty-seven metres to
/// carry an eight lane highway, and every step on the way was individually correct - the selection
/// returned the only tower the type had left, the widening moved its legs apart exactly as asked. Only
/// the result was absurd, and nothing was looking at the result.
///
/// So this runs the generation the way the mod runs it, against a stand-in mesh built to each tower's
/// recorded size, and checks what comes out the far end. The mesh is synthetic on purpose: the real
/// ones need a loaded game, but the arithmetic that shapes them does not, and it is the arithmetic
/// that has been wrong.
/// </summary>
internal static class TowerGenerationTests
{
    /// <summary>Road widths to generate at: the narrow end, the common sizes, and wider than anything.</summary>
    private static readonly float[] Widths = { 6f, 12f, 16f, 20f, 24f, 30f, 40f, 52f };

    internal static void Run(Action<string, bool, string?> check)
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            EveryPortalHasOverhang(styleId, check);
            GeneratingAtTheAuthoredWidthChangesNothing(styleId, check);
            GeneratedTowerAlwaysSpansItsRoad(styleId, check);
            LegsNeverCrossTheCentre(styleId, check);
            PartsTravelWithTheirVertices(styleId, check);
        }

        TheSuspensionFamilyHasOneOverhang(check);
        CablesStayThreeMetresWiderThanTheRoad(check);
        CablesRunOutsideTheCarriageway(check);
        TheCableFamiliesShareOneGeometry(check);
        SurfacesStretchAndPortalsSeparate(check);
        CrossbeamsStretchAndLegsDoNot(check);
        AThroughArchCrossMemberStaysAffine(check);
        TheTowerTemplateMatchesWhatWasMeasured(check);
        ThePillarTypesAreNumberedAsTheGameNumbersThem(check);
        ATowerStacksItsPartsTheWayTheArchetypeDoes(check);
        ACablePieceIsLaidOutTheWayTheArchetypeIs(check);
        CablesKeepTheirDistanceFromTheTower(check);
        AGeneratedMeshDeclaresTheSpaceItOccupies(check);
        TheTowerStandsTheArchetypesDistanceOutsideTheCables(check);
        TheTowersWidthIsTakenFromWhereTheCablesEndedUp(check);
        TheGapBetweenTheLegsIsMeasuredByBand(check);
        StretchOrTranslateIsDecidedByCrossingTheCentre(check);
        ASlantedLegIsCarriedOutAtEveryHeight(check);
        ADeckBetweenTheLegsStretchesToMeetThem(check);
        TheSecondDeckGoesUnderTheRoad(check);
        ADoubleDeckBridgeIsSizedFromItsPrototypeUpperDeck(check);
        AWingKeepsItsDepthOrSomethingSaysSo(check);
        AnOpenworkOrnamentStretchesBetweenTheLegs(check);
        AThicknessIsMeasuredOnOneThing(check);
        AVerticalMemberIsNotShearedAcrossABand(check);
        ALevelOfDetailIsMeasuredWithThePartItStandsFor(check);
        ALevelOfDetailWidensByOneOfTwoThings(check);
        TheCentralSpokeScalesWithItsOrnament(check);
        OnlyTheStylesThatBringRailingsLoseTheRoadsOne(check);
        TheKerbRailingFollowsTheFootway(check);
        OnlyTheRunLosesItsRailing(check);
        TheRoadsRailingIsGatedNotRemoved(check);
        ARailingIsAStandOfPostsNotAPiece(check);
        ARailingTakenAwayLeavesTheMesh(check);
        TheGoldenOuterRailingKeepsBothOfItsBands(check);
        TheCentralSpokeKeepsItsShape(check);
        ARailingIsTakenOffAtEveryDistance(check);
        RailingsShowAsBandsHoweverTheMeshIsSplit(check);
        EveryLevelOfDetailTreatsTheKerbRailingAlike(check);
        TheSideOfTheRoadIsTheOneObserved(check);
        WhatIsFittedToARoadIsNotShared(check);
        APlanIsCarriedOutOnlyWhereItBelongs(check);
        TheStyleIsKnownBeforeAnythingIsDerived(check);
        ATowerIsNotBroughtInPastItsNarrowestPart(check);
        TheBaseCarryingTheDeckTakesTheWholeMapping(check);
        ATowerAndItsCablesAreWidenedByOneNumber(check);
        AWeldedPortalKeepsItsLegs(check);
        OnePortalWidensTheWholeTower(check);
        OnlyMeasuredFamiliesAreSizedAgainstTheirCables(check);
        WhatIsNotGeneratedIsRefusedRatherThanAttempted(check);
        TheTrussArchPrototypesHaveIndependentStyles(check);
        AThroughArchIsWiderThanTheRoadItCarries(check);
        AStructureCannotLoseMoreWidthThanItHas(check);
        EachFamilyHasItsOwnOverhang(check);
        ATowerCorrectionMovesTheTowerAlone(check);
        OneSectionIsWidenedAgainstOneBoundary(check);
        ALegIsCarriedWhateverTheOtherPartsOpen(check);
        OneStylePerPylon(check);
        TheBridgeArchetypeMatchesWhatWasMeasured(check);
        TheTowerBindingAnswersForEveryField(check);
    }

    /// <summary>
    /// A portal reaches further than the road it straddles, always. Its legs stand outside the
    /// carriageway - that is what makes it something a road passes through rather than something
    /// standing in the way - so the overhang is what tells a tower from a column under the deck.
    ///
    /// Zero is allowed. The golden bridge straddles its whole deck rather than standing outside a
    /// narrower carriageway, so its pylons are flush with it - 50 over 50. That was read as a lost
    /// measurement once and corrected to 40, and the bridge came out worse, which is what settles it.
    /// </summary>
    private static void EveryPortalHasOverhang(string styleId, Action<string, bool, string?> check)
    {
        foreach (var tower in BridgeTowers.For(styleId))
        {
            if (tower.Support) continue;

            check(
                "[" + styleId + "] " + tower.Name + " is not narrower than its road",
                tower.Mesh >= tower.Road,
                $"mesh {tower.Mesh} vs road {tower.Road}, overhang {tower.Mesh - tower.Road}");
        }
    }

    /// <summary>
    /// The standard the whole design rests on, applied to every type rather than to suspension alone.
    ///
    /// Ask any type for a bridge over the road one of its towers was built for, and the tower that
    /// comes back must be that tower, untouched: same vertices, same part offsets, nothing moved. A
    /// derivation that cannot reproduce its own source is not a derivation.
    /// </summary>
    private static void GeneratingAtTheAuthoredWidthChangesNothing(
        string styleId, Action<string, bool, string?> check)
    {
        foreach (var tower in BridgeTowers.For(styleId))
        {
            if (tower.Support) continue;

            var selected = BridgeTowers.Select(styleId, tower.Road);
            check(
                $"[{styleId}] a {tower.Road} m road selects a tower built for it",
                selected.HasValue && selected.Value.Road == tower.Road,
                selected?.Name);
            if (!selected.HasValue) continue;

            var extra = tower.Road - selected.Value.Road;
            var source = Portal(selected.Value.Mesh);
            var generated = TowerWidening.Widen(source, extra);

            var moved = source.Where((vertex, index) => math.distance(vertex, generated[index]) > 1e-5f);
            check(
                $"[{styleId}] {selected.Value.Name} at {tower.Road} m is the original vertex for vertex",
                !moved.Any(),
                moved.Count() + " vertices moved");
        }
    }

    /// <summary>
    /// However wide the road, the tower that comes out still stands outside it.
    ///
    /// This is the invariant a stretched tower breaks in the direction nobody notices: widening keeps
    /// the overhang, so a tower derived for a wider road is still wide enough, but only if the number
    /// it was derived from was a road width to begin with. Derive from a column recorded against a road
    /// it does not span and the result is a portal the road runs straight through.
    /// </summary>
    private static void GeneratedTowerAlwaysSpansItsRoad(string styleId, Action<string, bool, string?> check)
    {
        foreach (var width in Widths)
        {
            var selected = BridgeTowers.Select(styleId, width);
            if (!selected.HasValue) continue;

            var extra = width - selected.Value.Road;
            var generated = TowerWidening.Widen(Portal(selected.Value.Mesh), extra);
            var span = TowerWidening.WidthOf(generated);

            check(
                $"[{styleId}] a {width:0.#} m road gets a tower that spans it",
                span > width - 1e-4f,
                $"{selected.Value.Name} came out {span:0.#} m across");
        }
    }

    /// <summary>
    /// Narrowing has a floor. Every vertex moves toward the centre line by half the shortfall, so a
    /// tower asked to shrink by more than its own width turns inside out - the left leg ends up right
    /// of the right one. It is a shape no amount of texturing survives, and the arithmetic produces it
    /// perfectly happily.
    /// </summary>
    private static void LegsNeverCrossTheCentre(string styleId, Action<string, bool, string?> check)
    {
        foreach (var width in Widths)
        {
            var selected = BridgeTowers.Select(styleId, width);
            if (!selected.HasValue) continue;

            var extra = width - selected.Value.Road;
            var generated = TowerWidening.Widen(Portal(selected.Value.Mesh), extra);

            var inverted = generated.Any(vertex => vertex.x < -1e-4f)
                && generated.Any(vertex => vertex.x > 1e-4f)
                && generated.Where(vertex => vertex.x < 0f).Max(vertex => vertex.x)
                    > generated.Where(vertex => vertex.x > 0f).Min(vertex => vertex.x);

            check(
                $"[{styleId}] a {width:0.#} m road does not turn {selected.Value.Name} inside out",
                !inverted,
                $"shrunk by {-extra:0.#} m from a {selected.Value.Mesh} m tower");
        }
    }

    /// <summary>
    /// A tower is modelled in pieces - base, shaft, top - each at its own offset, and the offsets have
    /// to travel exactly as far as the vertices do.
    ///
    /// Measuring the pieces separately spread a narrow crossbeam further than the legs beneath it;
    /// zeroing their offsets collapsed them onto one another. Both were tried, both took the tower
    /// apart, and both looked fine in every test that only checked vertices.
    /// </summary>
    private static void PartsTravelWithTheirVertices(string styleId, Action<string, bool, string?> check)
    {
        foreach (var width in Widths)
        {
            var selected = BridgeTowers.Select(styleId, width);
            if (!selected.HasValue) continue;

            var extra = width - selected.Value.Road;
            var half = selected.Value.Mesh * 0.5f;

            // A part sitting on the left leg and the leg's own vertices start together and must end
            // together, whatever the shift.
            var partBefore = -half;
            var partAfter = TowerWidening.Spread(partBefore, extra);
            var vertexAfter = TowerWidening.Widen(new[] { new float3(partBefore, 0f, 0f) }, extra)[0].x;

            check(
                $"[{styleId}] at {width:0.#} m the parts land where their vertices do",
                Math.Abs(partAfter - vertexAfter) < 1e-5f,
                $"part {partAfter:0.###} vs vertex {vertexAfter:0.###}");
        }
    }


    /// <summary>
    /// The suspension towers are one model at four sizes, so their overhang is one number: ten metres,
    /// 22/12, 26/16, 30/20, 34/24.
    ///
    /// The golden pylons are deliberately not held to it. They were recorded flush with their deck, at
    /// fifty over fifty, and that reads like a bad number against this rule - so it was changed to 40
    /// and the bridge came out worse in game. It is a different design, not a bad measurement: those
    /// towers straddle the whole deck. A rule drawn from three bridges does not reach a fourth just
    /// because it shares a name.
    /// </summary>
    private static void TheSuspensionFamilyHasOneOverhang(Action<string, bool, string?> check)
    {
        var overhangs = new List<string>();
        foreach (var styleId in new[] { "Suspension" })
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                if (tower.Support) continue;

                var overhang = tower.Mesh - tower.Road;
                check(
                    $"[{styleId}] {tower.Name} keeps the family's ten metres of overhang",
                    overhang == SuspensionOverhang,
                    $"{overhang} m");
                overhangs.Add($"{tower.Name}={overhang}");
            }
        }

        check("the blue suspension family was checked", overhangs.Count >= 4,
            overhangs.Count.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Where the cables end up, at every width, for the family whose numbers are known.
    ///
    /// Measured in a running game, the suspension cable sections are:
    ///
    ///     road 12 -> 2-Lane Suspension Bridge 15 m @ 0
    ///     road 16 -> 3-Lane Suspension Bridge 19 m @ 0
    ///     road 20 -> 4-Lane Suspension Bridge 23 m @ 0
    ///     road 24 -> 5-Lane Suspension Bridge 27 m @ 0
    ///
    /// Three metres wider than the road, every time, and always centred. That constant is the whole
    /// specification: the cables hang a metre and a half outboard of each edge of the carriageway, and
    /// a bridge whose cables are anywhere else is wrong however good it looks from above.
    ///
    /// The rule under test is that widening preserves it. A 40 m deck must come out with 43 m of cable
    /// section, exactly as the game's own 20 m deck comes out with 23 - not the donor's 27 m, which is
    /// what copying the section unchanged produced and what put the cables over the carriageway.
    /// </summary>
    private static void CablesStayThreeMetresWiderThanTheRoad(Action<string, bool, string?> check)
    {
        (string Section, int Road, int Width)[] measured =
        {
            ("2-Lane Suspension Bridge", 12, 15),
            ("3-Lane Suspension Bridge", 16, 19),
            ("4-Lane Suspension Bridge", 20, 23),
            ("5-Lane Suspension Bridge", 24, 27),
        };

        foreach (var (section, road, width) in measured)
        {
            check(
                $"[cables] {section} is {CableMargin} m wider than its {road} m road",
                width == road + CableMargin,
                $"{width} m");
        }

        // Generated at every deck width, from whichever donor the selection lands on.
        foreach (var deck in Widths)
        {
            var tower = BridgeTowers.Select("Suspension", deck);
            if (!tower.HasValue) continue;

            var donor = measured.FirstOrDefault(entry => entry.Road == tower.Value.Road);
            if (donor.Section == null) continue;

            var extra = deck - tower.Value.Road;
            var widened = donor.Width + extra;

            check(
                $"[cables] a {deck:0.#} m deck gets {deck + CableMargin:0.#} m of cable section",
                Math.Abs(widened - (deck + CableMargin)) < 1e-4f,
                $"{widened:0.#} m from {donor.Section}");

            // And the mesh that carries them is widened by the same amount as the tower's legs, so the
            // cables stay over the leg they hang from rather than drifting relative to it.
            var cableEdge = TowerWidening.Spread(donor.Width * 0.5f, extra);
            var legEdge = TowerWidening.Spread(tower.Value.Mesh * 0.5f, extra);
            var gapBefore = (tower.Value.Mesh - donor.Width) * 0.5f;

            check(
                $"[cables] at {deck:0.#} m the cables keep their distance from the legs",
                Math.Abs((legEdge - cableEdge) - gapBefore) < 1e-4f,
                $"{legEdge - cableEdge:0.###} vs {gapBefore:0.###}");
        }

        // The standard the user set: at a width the game already has a bridge for, nothing moves.
        foreach (var (section, road, width) in measured)
        {
            var tower = BridgeTowers.Select("Suspension", road);
            var extra = road - (tower?.Road ?? 0);

            check(
                $"[cables] a {road} m deck leaves {section} at its own {width} m",
                Math.Abs(extra) < 1e-5f && Math.Abs((width + extra) - width) < 1e-5f,
                $"shift {extra:0.###}");
        }
    }

    /// <summary>
    /// The cables of every type run outside the carriageway, not over it.
    ///
    /// This is the check the cable-stayed bridge needed and did not have. Its road width was unusable -
    /// the bridge is "XL Road Divided" and came back at 75 m against a 42 m tower - so it was filled in
    /// from an older scan as 35, which happened to be exactly the width of its cable section. A road as
    /// wide as its own cables puts them on the kerb, and every width derived from there inherited it.
    ///
    /// Two independent measurements catch it. The tower and the cable section are both read from
    /// geometry and both agree with each other; only the road does not.
    /// </summary>
    private static void CablesRunOutsideTheCarriageway(Action<string, bool, string?> check)
    {
        var checkedPairs = 0;

        foreach (var styleId in BridgeTowers.Styles)
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                var cables = BridgeCables.For(tower.Name);
                if (cables == null) continue;

                checkedPairs++;
                if (tower.Support || !cables.Value.Outer) continue;

                check(
                    $"[{styleId}] {cables.Value.Section} is wider than the {tower.Road} m road under it",
                    cables.Value.Width > tower.Road,
                    $"{cables.Value.Width:0.#} m of cable over {tower.Road} m of road");
            }
        }

        check("cable sections were matched to towers", checkedPairs >= 10,
            checkedPairs.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Suspension and cable-stayed are one design at many sizes, and their two geometric measurements
    /// say so: ten metres of tower outside the road, three of cable.
    ///
    /// Where a road width is unusable this is what replaces it, so it has to hold exactly rather than
    /// approximately - the eight-lane cable-stayed road is 32 because 32 is the only width that puts
    /// both a 42 m tower and a 35 m cable section where they belong. A family member that stops
    /// satisfying both has a bad number, not a different design.
    /// </summary>
    private static void TheCableFamiliesShareOneGeometry(Action<string, bool, string?> check)
    {
        string[] towers =
        {
            "2LaneSuspensionBridgePillar Placeholder",
            "3LaneSuspensionBridgePillar Placeholder",
            "4LaneSuspensionBridgePillar Placeholder",
            "5LaneSuspensionBridgePillar Placeholder",
        };

        // The eight-lane cable-stayed pylon was in this list and is not any more. It was put here
        // because 42 m of tower and 35 m of cable over a 32 m road gave the blue family's ten and
        // three exactly - which is how its road was inferred in the first place, its own measurement
        // being unusable. Seen in the game the tower was a metre wide, so the road is 33 and the two
        // numbers are 9 and 2. The family is not the blue one, and an assertion that it was is how
        // rule 9's mistake gets frozen into the tests meant to catch it.

        foreach (var name in towers)
        {
            var tower = BridgeTowers.Styles
                .SelectMany(styleId => BridgeTowers.For(styleId))
                .FirstOrDefault(candidate => candidate.Name == name);
            var cables = BridgeCables.For(name);

            check($"[cables] {name} is listed", tower.Name == name && cables != null, tower.Name);
            if (tower.Name != name || cables == null) continue;

            check(
                $"[cables] {name} stands {BridgeCables.Overhang} m outside its road",
                Math.Abs((tower.Mesh - tower.Road) - BridgeCables.Overhang) < 1e-4f,
                $"{tower.Mesh - tower.Road} m");

            check(
                $"[cables] {name} carries cables {BridgeCables.Margin} m wider than its road",
                Math.Abs((cables.Value.Width - tower.Road) - BridgeCables.Margin) < 1e-4f,
                $"{cables.Value.Width - tower.Road:0.#} m");
        }
    }

    /// <summary>
    /// A continuous surface widens by stretching, never by carrying its halves apart.
    ///
    /// The two rules look interchangeable and are not. A portal is two legs with a gap between them, so
    /// its halves move rigidly and the gap grows; a net piece is one sheet across its whole width, and
    /// moving its halves rigidly opens a hole down the middle with the triangles that spanned the centre
    /// line stretched across it. That is what put shards of cable geometry flat over the carriageway.
    ///
    /// Both rules put the outer edge in the same place - that part has to agree, or the cables would no
    /// longer meet the tower legs - and they differ everywhere else.
    /// </summary>
    private static void SurfacesStretchAndPortalsSeparate(Action<string, bool, string?> check)
    {
        const float width = 27f;
        const float extra = 16f;

        var sheet = new[]
        {
            new float3(-13.5f, 0f, 0f), new float3(-7f, 0f, 0f), new float3(-0.2f, 0f, 0f),
            new float3(0f, 0f, 0f), new float3(0.2f, 0f, 0f), new float3(7f, 0f, 0f),
            new float3(13.5f, 0f, 0f),
        };

        var stretched = TowerWidening.Stretch(sheet, width, extra);
        var separated = TowerWidening.Widen(sheet, extra);

        // The edge lands in the same place under either rule.
        check("[stretch] the outer edge reaches the same width",
            Math.Abs(TowerWidening.WidthOf(stretched) - (width + extra)) < 1e-3f,
            TowerWidening.WidthOf(stretched).ToString("0.##", CultureInfo.InvariantCulture));

        // Nothing tears: every edge of the sheet grows by the same factor. That is what distinguishes
        // stretching from tearing - under the rigid rule the edges near the centre line are pulled to
        // many times their length while the ones further out barely change, and a triangle stretched
        // forty times over is the shard that ends up lying across the carriageway.
        var ordered = true;
        for (var index = 1; index < stretched.Length; index++)
        {
            if (stretched[index].x < stretched[index - 1].x - 1e-4f) ordered = false;
        }

        check("[stretch] the sheet keeps its order", ordered, null);

        var expected = (width + extra) / width;
        check("[stretch] every edge grows by the same factor",
            Math.Abs(WorstEdge(sheet, stretched) - expected) < 1e-3f,
            WorstEdge(sheet, stretched).ToString("0.##", CultureInfo.InvariantCulture));

        // The rigid rule pulls the edges at the centre line to many times their length, which is the
        // tear itself rather than a symptom of it.
        check("[stretch] the rigid rule would have torn it open",
            WorstEdge(sheet, separated) > expected * 10f,
            WorstEdge(sheet, separated).ToString("0.##", CultureInfo.InvariantCulture));

        // And at the authored width neither rule moves anything.
        var same = TowerWidening.Stretch(sheet, width, 0f);
        check("[stretch] nothing moves at the authored width",
            sheet.Select((vertex, index) => Math.Abs(vertex.x - same[index].x) < 1e-6f).All(x => x), null);
    }

    /// <summary>
    /// The tower archetype is written down, not copied at run time.
    ///
    /// Every parameter a generated tower needs comes from <see cref="BridgeTowerTemplate"/>, and these
    /// are the values it holds. The point of the test is that the template stays what was read out of
    /// the game: it is the only description of a tower the generator has, and the prefab it was taken
    /// from may not be installed when a bridge is built.
    /// </summary>
    private static void TheTowerTemplateMatchesWhatWasMeasured(Action<string, bool, string?> check)
    {
        check("[template] a tower is a standalone pillar",
            BridgeTowerSpec.PillarTypeStandalone == 2,
            BridgeTowerSpec.PillarTypeStandalone.ToString(CultureInfo.InvariantCulture));

        check("[template] it is anchored at its own origin",
            Math.Abs(BridgeTowerSpec.AnchorOffset) < 1e-6f,
            BridgeTowerSpec.AnchorOffset.ToString(CultureInfo.InvariantCulture));

        // A metre either way, which is an adjustment and not a stretching distance. Reading it as one
        // is what sent three rounds of changes at making the tower reach further by widening it.
        check("[template] its vertical range is one metre either way",
            Math.Abs(BridgeTowerSpec.VerticalRangeMin + 1f) < 1e-6f
                && Math.Abs(BridgeTowerSpec.VerticalRangeMax - 1f) < 1e-6f,
            BridgeTowerSpec.VerticalRangeMin + ".." + BridgeTowerSpec.VerticalRangeMax);

        // Zero here means the replacement is never chosen and the placeholder is left standing, which
        // is a tower with no base and nothing below the deck.
        check("[template] the replacement always wins its placeholder",
            BridgeTowerSpec.SpawnProbability == 100,
            BridgeTowerSpec.SpawnProbability.ToString(CultureInfo.InvariantCulture));

        check("[template] meshes sit at the object's own origin", !BridgeTowerSpec.Circular, null);
    }

    /// <summary>
    /// The bridge archetype is what was measured, and stays that.
    ///
    /// These numbers decide how a generated bridge behaves - how far apart its towers stand, how it
    /// meets water, where the deck crosses the tower - and unlike geometry there is nothing on screen
    /// that shows one of them drifting. Both suspension bridges the game ships hold them identically,
    /// which is what makes them the family's rather than one bridge's.
    /// </summary>
    private static void TheBridgeArchetypeMatchesWhatWasMeasured(Action<string, bool, string?> check)
    {
        var suspension = BridgeSpec.For("Suspension");
        check("[bridge] the suspension archetype is recorded", suspension.HasValue, null);
        if (!suspension.HasValue) return;

        var archetype = suspension.Value;
        check("[bridge] towers stand 256 m apart",
            Math.Abs(archetype.SegmentLength - 256f) < 1e-4f,
            archetype.SegmentLength.ToString(CultureInfo.InvariantCulture));

        check("[bridge] the deck does not sag",
            Math.Abs(archetype.Hanging) < 1e-6f,
            archetype.Hanging.ToString(CultureInfo.InvariantCulture));

        check("[bridge] it clears water by 10 m",
            Math.Abs(archetype.ElevationOnWater - 10f) < 1e-4f,
            archetype.ElevationOnWater.ToString(CultureInfo.InvariantCulture));

        check("[bridge] it does not curve", !archetype.CanCurve, null);
        check("[bridge] it has no minimal length", !archetype.AllowMinimalLength, null);

        // The one that decides where the tower meets the road. Zero - which is what a missing donor
        // would leave - puts the tower's origin at the deck, so it stands on the road rather than
        // through it.
        check("[bridge] the deck crosses the tower 77.9 m up",
            Math.Abs(archetype.TowerHeightAboveOrigin - 77.9f) < 1e-4f,
            archetype.TowerHeightAboveOrigin.ToString(CultureInfo.InvariantCulture));

        check("[bridge] it names the bridge it was measured from",
            !string.IsNullOrEmpty(archetype.MeasuredFrom),
            archetype.MeasuredFrom);
    }


    /// <summary>
    /// The tower binding answers for every field the game defines on a sub object entry.
    ///
    /// The count is the test. A binding assembled by naming the fields that seemed to matter left three
    /// of the twelve untouched, and a field nobody writes is one nobody compares - it takes whatever an
    /// empty entry holds and nothing reports it. Two are supplied by the caller because they depend on
    /// which tower is being placed; the rest have to be recorded.
    /// </summary>
    private static void TheTowerBindingAnswersForEveryField(Action<string, bool, string?> check)
    {
        string[] fields =
        {
            "m_Object", "m_Position", "m_Rotation", "m_Placement", "m_FixedIndex", "m_Spacing",
            "m_AnchorTop", "m_AnchorCenter", "m_RequireElevated", "m_RequireOutsideConnection",
            "m_RequireDeadEnd", "m_RequireOrphan",
        };

        // Supplied by the caller: the tower itself, and how far up it the deck sits.
        string[] fromCaller = { "m_Object", "m_Position" };

        foreach (var field in fields)
        {
            if (fromCaller.Contains(field))
            {
                check("[binding] " + field + " is supplied by the caller",
                    !BridgeSpec.TowerBinding.ContainsKey(field), null);
                continue;
            }

            check("[binding] " + field + " is recorded",
                BridgeSpec.TowerBinding.ContainsKey(field), null);
        }

        check("[binding] nothing is recorded that the game does not define",
            BridgeSpec.TowerBinding.Keys.All(fields.Contains),
            string.Join(", ", BridgeSpec.TowerBinding.Keys.Where(key => !fields.Contains(key))));

        // The values themselves, since a recorded field with the wrong value is no better than a
        // missing one.
        check("[binding] one tower at the middle of each span",
            (int)BridgeSpec.TowerBinding["m_Placement"] == BridgeSpec.PlacementEdgeMiddle, null);
        check("[binding] spacing is zero, because EdgeMiddle does not repeat",
            Math.Abs((float)BridgeSpec.TowerBinding["m_Spacing"]) < 1e-6f, null);
        check("[binding] the tower is not anchored by its top",
            !(bool)BridgeSpec.TowerBinding["m_AnchorTop"], null);
        check("[binding] the tower is not anchored by its centre",
            !(bool)BridgeSpec.TowerBinding["m_AnchorCenter"], null);
        check("[binding] the tower does not require an elevated segment",
            !(bool)BridgeSpec.TowerBinding["m_RequireElevated"], null);
        check("[binding] the tower is placed unrotated",
            ((float[])BridgeSpec.TowerBinding["m_Rotation"]).SequenceEqual(new[] { 0f, 0f, 0f, 1f }), null);
    }


    /// <summary>
    /// The stacking, which is what lets a tower reach the ground.
    ///
    /// A tower is a base, a repeatable shaft and a top, and the game only knows that because each part
    /// says so. <c>ObjectInitializeSystem.UpdateStackBounds</c> takes each part's contribution to the
    /// object's own size and collapses it to the end it belongs to - the first part counts only below
    /// the origin, the last only above it, the middle not at all - and puts the rest in StackData.
    /// SubObjectSystem then gives the placed tower a Stack running from m_FirstBounds.min minus its
    /// elevation up to m_LastBounds.max, so the stack grows downward by exactly however far the tower
    /// was raised and the shaft repeats to fill it.
    ///
    /// Generated towers carried none of this. No StackProperties meant no StackData, no StackData meant
    /// no Stack, and the tower was drawn at the height it was modelled at - hanging above the ground by
    /// the elevation. Every other field matched the archetype, which is why round after round of fixing
    /// the pillar type, the bounds, the placement and the placeholder pair changed nothing: the fault
    /// was not on the tower at all, it was on its parts.
    /// </summary>
    private static void ATowerStacksItsPartsTheWayTheArchetypeDoes(Action<string, bool, string?> check)
    {
        check("[stack] None is zero", BridgeTowerSpec.StackDirectionNone == 0, null);
        check("[stack] Right is one", BridgeTowerSpec.StackDirectionRight == 1, null);
        check("[stack] Up is two", BridgeTowerSpec.StackDirectionUp == 2, null);
        check("[stack] Forward is three", BridgeTowerSpec.StackDirectionForward == 3, null);

        check("[stack] First is zero", BridgeTowerSpec.StackOrderFirst == 0, null);
        check("[stack] Middle is one", BridgeTowerSpec.StackOrderMiddle == 1, null);
        check("[stack] Last is two", BridgeTowerSpec.StackOrderLast == 2, null);

        // The archetype's three parts: base, shaft, top.
        check("[stack] the first of three parts is the base",
            BridgeTowerSpec.StackOrderOf(0, 3) == BridgeTowerSpec.StackOrderFirst, null);
        check("[stack] the second of three parts is the repeatable shaft",
            BridgeTowerSpec.StackOrderOf(1, 3) == BridgeTowerSpec.StackOrderMiddle, null);
        check("[stack] the third of three parts is the top",
            BridgeTowerSpec.StackOrderOf(2, 3) == BridgeTowerSpec.StackOrderLast, null);

        // A tower with more parts than the archetype: everything between the ends repeats.
        check("[stack] a four part tower still begins with a base",
            BridgeTowerSpec.StackOrderOf(0, 4) == BridgeTowerSpec.StackOrderFirst, null);
        check("[stack] both middles of a four part tower repeat",
            BridgeTowerSpec.StackOrderOf(1, 4) == BridgeTowerSpec.StackOrderMiddle
            && BridgeTowerSpec.StackOrderOf(2, 4) == BridgeTowerSpec.StackOrderMiddle, null);
        check("[stack] a four part tower still ends with a top",
            BridgeTowerSpec.StackOrderOf(3, 4) == BridgeTowerSpec.StackOrderLast, null);

        // A stack always has exactly one of each end, however many parts it has.
        for (var count = 2; count <= 8; count++)
        {
            var firsts = 0;
            var lasts = 0;
            for (var index = 0; index < count; index++)
            {
                if (BridgeTowerSpec.StackOrderOf(index, count) == BridgeTowerSpec.StackOrderFirst) firsts++;
                if (BridgeTowerSpec.StackOrderOf(index, count) == BridgeTowerSpec.StackOrderLast) lasts++;
            }

            check($"[stack] a tower of {count} parts has one base and one top",
                firsts == 1 && lasts == 1,
                $"{firsts} base(s), {lasts} top(s)");
        }

        // One part is not a stack: it would have to be the first and the last at once. The archetype's
        // placeholder has exactly one part and carries no stacking, so this needs no special case.
        check("[stack] a single part is not stacked", !BridgeTowerSpec.Stacks(1), null);
        check("[stack] no parts is not stacked", !BridgeTowerSpec.Stacks(0), null);
        check("[stack] two parts are stacked", BridgeTowerSpec.Stacks(2), null);
        check("[stack] the archetype's three parts are stacked", BridgeTowerSpec.Stacks(3), null);

        // Parts butt together rather than sinking into one another, and the archetype leaves
        // scaling permitted.
        check("[stack] parts butt together at the start",
            Math.Abs(BridgeTowerSpec.StackStartOverlap) < 0.0001f, null);
        check("[stack] parts butt together at the end",
            Math.Abs(BridgeTowerSpec.StackEndOverlap) < 0.0001f, null);
        check("[stack] scaling is left permitted, as the archetype leaves it",
            !BridgeTowerSpec.StackForbidScaling, null);

        // The ground decal is named, not referenced: base game content, present whenever the game is.
        check("[stack] the ground base is named",
            BridgeTowerSpec.BaseMeshName == "Default_Base Mesh", BridgeTowerSpec.BaseMeshName);
        check("[stack] the ground base uses minimum bounds", BridgeTowerSpec.BaseUseMinBounds, null);

        // The UIObject the archetype carries on each part - not on the tower, which is where a dump
        // that did not distinguish a mesh from its owner appeared to put it.
        check("[stack] each part is listed at the tower's own priority",
            BridgeTowerSpec.MeshUiPriority == BridgeTowerSpec.UiPriority, null);
    }

    /// <summary>
    /// A cable piece disables texture tiling, and that is what puts the cables where they belong.
    ///
    /// The flag reads as a texture setting and is not only one. NetInitializeSystem turns it into
    /// NetPieceFlags.DisableTiling, and CalculateCompositionPieceOffsets lays a composition's pieces out
    /// in separate groups chosen by that flag, each packed along its own running cursor. Without it the
    /// cable piece is packed in among the road's own surface pieces - the right width, in the wrong
    /// place, which looked like a widening fault for several rounds and was not one.
    ///
    /// Dumped side by side, a generated section and its archetype differ in the width, the bounds and
    /// this one component. The first two are the point; the third was the bug.
    /// </summary>
    private static void ACablePieceIsLaidOutTheWayTheArchetypeIs(Action<string, bool, string?> check)
    {
        check("[tiling] a cable piece disables texture tiling",
            BridgeCables.PieceDisablesTextureTiling, null);

        check("[tiling] PreserveShape is one", BridgeCables.PieceFlagPreserveShape == 1, null);
        check("[tiling] BlockTraffic is two", BridgeCables.PieceFlagBlockTraffic == 2, null);
        check("[tiling] BlockCrosswalk is four", BridgeCables.PieceFlagBlockCrosswalk == 4, null);
        check("[tiling] Surface is eight", BridgeCables.PieceFlagSurface == 8, null);
        check("[tiling] DisableTiling is sixteen", BridgeCables.PieceFlagDisableTiling == 16, null);
        check("[tiling] LowerBottomToTerrain is thirty two",
            BridgeCables.PieceFlagLowerBottomToTerrain == 32, null);

        // Each is one bit, and DisableTiling is not any of its neighbours - the check the pillar types
        // did not get, which is how three came to mean Base where Standalone was meant.
        var flags = new[]
        {
            BridgeCables.PieceFlagPreserveShape,
            BridgeCables.PieceFlagBlockTraffic,
            BridgeCables.PieceFlagBlockCrosswalk,
            BridgeCables.PieceFlagSurface,
            BridgeCables.PieceFlagDisableTiling,
            BridgeCables.PieceFlagLowerBottomToTerrain,
        };

        foreach (var flag in flags)
        {
            check($"[tiling] {flag} is a single bit", flag > 0 && (flag & (flag - 1)) == 0, null);
        }

        var overlaps = 0;
        foreach (var flag in flags)
        {
            if (flag != BridgeCables.PieceFlagDisableTiling
                && (flag & BridgeCables.PieceFlagDisableTiling) != 0)
            {
                overlaps++;
            }
        }

        check("[tiling] DisableTiling shares no bit with its neighbours", overlaps == 0, null);
    }

    /// <summary>
    /// The cables keep their distance from the tower, at every width.
    ///
    /// The requirement stated plainly: on a generated bridge the gap between the cable's outer edge and
    /// the tower's is what it is on the bridge the two came from. Both are derived from the same
    /// archetype by the same extra width, so the gap is preserved exactly when both outer edges move by
    /// the same amount - and the tower's legs move by half the extra, rigidly, because that is what
    /// widening a portal means.
    ///
    /// So the sheet has to put its outer edge at half the extra too. A proportional stretch does that
    /// only if it divides by the distance actually being scaled. Dividing by the piece's declared width
    /// instead - 27 against 26.94664 drawn - left the edge 4 mm short at four metres of extra and 1.6 cm
    /// at sixteen: small, but a constant drift of the cables inward from the legs they hang beside, and
    /// in the one dimension this whole rule exists to get right.
    /// </summary>
    private static void CablesKeepTheirDistanceFromTheTower(Action<string, bool, string?> check)
    {
        // The five lane bridge, measured: the cable sheet spans this, the tower straddles a 24 m road.
        const float sheet = 26.94664f;
        const float half = sheet * 0.5f;

        foreach (var extra in new[] { 0f, 2f, 4f, 8f, 16f, 26f })
        {
            var sheetVertices = new[]
            {
                new float3(-half, 0f, 0f),
                new float3(-half * 0.5f, 10f, 0f),
                new float3(0f, 20f, 0f),
                new float3(half * 0.5f, 10f, 0f),
                new float3(half, 0f, 0f),
            };

            // The sheet stretches from its own span; a leg is carried out rigidly by half the extra.
            var stretched = TowerWidening.Stretch(sheetVertices, sheet, extra);
            var edge = TowerWidening.WidthOf(stretched) * 0.5f;
            var leg = TowerWidening.Spread(17f, extra);

            check($"[gap] at {extra} m extra the cable edge moves half the extra",
                Math.Abs((edge - half) - (extra * 0.5f)) < 0.001f,
                $"moved {edge - half:0.####} m, wanted {extra * 0.5f:0.####} m");

            // 17 m is where the five lane tower's leg stands. The gap is what it was, at every width.
            check($"[gap] at {extra} m extra the cables keep their distance from the leg",
                Math.Abs((leg - edge) - (17f - half)) < 0.001f,
                $"gap {leg - edge:0.####} m, wanted {17f - half:0.####} m");
        }

        // Dividing by the declared width instead of the drawn span is the drift this rules out. It is
        // recorded rather than merely avoided, because it is far too small to notice in a screenshot and
        // would come back the moment someone reached for the field that reads like the right one.
        var byDeclared = TowerWidening.Stretch(
            new[] { new float3(-half, 0f, 0f), new float3(half, 0f, 0f) }, 27f, 16f);
        var drift = (half + 8f) - (TowerWidening.WidthOf(byDeclared) * 0.5f);

        check("[gap] dividing by the declared width drifts the edge inward",
            drift > 0.01f && drift < 0.02f,
            $"{drift:0.####} m short");
    }

    /// <summary>
    /// A generated mesh declares the space it occupies, because nothing else will compute it.
    ///
    /// The asset pipeline builds the Unity mesh with <c>Mesh.SetSubMesh(i, descriptor, flags: 15)</c>,
    /// and 15 includes <c>DontRecalculateBounds</c>; it then sets <c>mesh.bounds</c> to the union of
    /// the descriptors' own bounds. So the descriptor's bounds field is the only source there is, and
    /// the three argument constructor leaves it at its default.
    ///
    /// Every mesh this mod wrote declared a zero-size box at the origin - all fifteen of them, towers
    /// and cables alike, while their vertices, indices and vertex layout were all correct. It went
    /// unseen for as long as it did because the dump's mesh budget was spent on the archetypes before
    /// it reached anything generated, so the only extents ever printed were the right ones.
    /// </summary>
    private static void AGeneratedMeshDeclaresTheSpaceItOccupies(Action<string, bool, string?> check)
    {
        // One triangle out at the edge and one at the centre, indexed as two submeshes.
        var points = new[]
        {
            new float3(-9f, 0f, -2f),
            new float3(-7f, 4f, 2f),
            new float3(-8f, 1f, 0f),
            new float3(1f, 0f, 0f),
            new float3(3f, 6f, 1f),
            new float3(2f, 2f, -1f),
        };
        var indices = new[] { 0, 1, 2, 3, 4, 5 };

        TowerWidening.ExtentOf(points, indices, 0, 3, out var low, out var high);
        check("[bounds] a submesh spans the vertices it indexes",
            Math.Abs(low.x + 9f) < 0.001f && Math.Abs(high.x + 7f) < 0.001f
            && Math.Abs(low.y) < 0.001f && Math.Abs(high.y - 4f) < 0.001f
            && Math.Abs(low.z + 2f) < 0.001f && Math.Abs(high.z - 2f) < 0.001f,
            $"({low.x},{low.y},{low.z})..({high.x},{high.y},{high.z})");

        TowerWidening.ExtentOf(points, indices, 3, 3, out var low2, out var high2);
        check("[bounds] and not the vertices another submesh indexes",
            Math.Abs(low2.x - 1f) < 0.001f && Math.Abs(high2.x - 3f) < 0.001f, null);

        // The failure that was shipped: nothing measured at all is a zero box at the origin, and a
        // zero box is what the renderer is told the mesh occupies.
        TowerWidening.ExtentOf(points, indices, 0, 0, out var empty, out var alsoEmpty);
        check("[bounds] measuring nothing gives the zero box that was the bug",
            Math.Abs(empty.x) < 0.001f && Math.Abs(alsoEmpty.x) < 0.001f, null);

        // A widened mesh's bounds come from the widened points, not the source ones.
        var widened = TowerWidening.Widen(points, 6f, inner: 6f);
        TowerWidening.ExtentOf(widened, indices, 0, 3, out var wideLow, out var wideHigh);
        check("[bounds] a widened submesh spans the widened vertices",
            Math.Abs(wideLow.x + 12f) < 0.001f && Math.Abs(wideHigh.x + 10f) < 0.001f,
            $"{wideLow.x}..{wideHigh.x}");

        // Height and depth are untouched by widening, so the box only grows across.
        check("[bounds] widening changes the box across and nowhere else",
            Math.Abs(wideLow.y - low.y) < 0.001f && Math.Abs(wideHigh.y - high.y) < 0.001f
            && Math.Abs(wideLow.z - low.z) < 0.001f && Math.Abs(wideHigh.z - high.z) < 0.001f, null);

        TowerWidening.IndexRangeOf(indices, 3, 3, out var first, out var used);
        check("[bounds] a submesh reports which vertices it uses",
            first == 3 && used == 3, $"first {first}, {used} used");

        TowerWidening.IndexRangeOf(indices, 0, 6, out var allFirst, out var allUsed);
        check("[bounds] and the whole run covers them all",
            allFirst == 0 && allUsed == 6, $"first {allFirst}, {allUsed} used");
    }

    /// <summary>
    /// The tower stands the archetype's distance outside the cables, at every width.
    ///
    /// Measured on the game's own two suspension bridges, which are different road widths and give the
    /// same three numbers to five decimals - so the constant is a property of the design rather than of
    /// one bridge:
    ///
    ///     part      5 lanes (road 24)   4 lanes (road 20)   outside the cables
    ///     base        18.75000            16.75000            5.27667
    ///     leg         17.01078            15.01078            3.53745
    ///     top         17.15220            15.15221            3.67887
    ///     cables      13.47333            11.47333            -
    ///
    /// It holds in generation because the legs are carried out rigidly by half the extra width and the
    /// cable sheet is stretched from the span it draws, which moves its outer edge by the same half.
    /// Two code paths that agree rather than one that enforces it, which is why the result is also
    /// measured at run time and reported when it drifts.
    /// </summary>
    private static void TheTowerStandsTheArchetypesDistanceOutsideTheCables(Action<string, bool, string?> check)
    {
        // The five lane bridge, measured.
        const float cableOuter = 13.47333f;
        const float sheet = 26.94664f;
        var towerOuter = new[] { 18.75f, 17.01078f, 17.1522f };

        foreach (var extra in new[] { 0f, 2f, 4f, 8f, 16f, 26f, 40f })
        {
            // The cables: a sheet stretched from its own span.
            var sheetVertices = new[]
            {
                new float3(-sheet * 0.5f, 0f, 0f),
                new float3(0f, 40f, 0f),
                new float3(sheet * 0.5f, 0f, 0f),
            };
            var cable = cableOuter
                * ((sheet + extra) / sheet);
            var stretched = TowerWidening.Stretch(sheetVertices, sheet, extra);

            check($"[spacing] at {extra} m the cable sheet's edge moves half the extra",
                Math.Abs((TowerWidening.WidthOf(stretched) - sheet) - extra) < 0.001f, null);

            for (var part = 0; part < towerOuter.Length; part++)
            {
                // The tower: a leg carried out rigidly. 12 is half the road it straddles.
                var leg = TowerWidening.Spread(towerOuter[part], extra);
                var measured = leg - cable;

                check($"[spacing] at {extra} m part {part + 1} keeps the archetype's distance",
                    BridgeCables.SpacingHolds(BridgeCables.Suspension, measured, part, towerOuter.Length),
                    $"{measured:0.####} m, wanted {BridgeCables.TowerOutsideCables(part, towerOuter.Length):0.####} m");
            }
        }

        // A placeholder carries the top alone, so its one part is measured against the top's distance.
        check("[spacing] a one part tower is a placeholder carrying the top",
            Math.Abs(BridgeCables.TowerOutsideCables(0, 1) - BridgeCables.TowerTopOutsideCables) < 0.0001f,
            null);

        // The three are distinct, so a part measured against the wrong one is caught rather than
        // absorbed by the tolerance.
        check("[spacing] the three distances are further apart than the tolerance",
            Math.Abs(BridgeCables.TowerBaseOutsideCables - BridgeCables.TowerLegOutsideCables)
                > BridgeCables.SpacingTolerance
            && Math.Abs(BridgeCables.TowerTopOutsideCables - BridgeCables.TowerLegOutsideCables)
                > BridgeCables.SpacingTolerance, null);

        // The drift this exists to catch: a tower and a cable section derived from different bridges.
        // The 4-lane cables under the 5-lane tower are 2 m too far in, which is 200 times the tolerance.
        check("[spacing] cables from another bridge are reported",
            !BridgeCables.SpacingHolds(BridgeCables.Suspension, 17.01078f - 11.47333f, 1, 3),
            $"{17.01078f - 11.47333f:0.####} m");
    }

    /// <summary>
    /// The tower's width is taken from where the cables ended up, not from the road.
    ///
    /// The distance is to the cables, so the cables are what it is measured against. Both rules give
    /// the same answer whenever the tower and the cables came from the same bridge - which is every
    /// bridge measured so far, agreeing to five decimals - and only this one is right when they did
    /// not, which the selection permits: the tower is chosen by width from the recorded list and the
    /// cables come from whichever installed bridge carries that tower.
    /// </summary>
    private static void TheTowersWidthIsTakenFromWhereTheCablesEndedUp(Action<string, bool, string?> check)
    {
        // The five lane bridge, measured.
        const float cableOuter = 13.47333f;
        const float legOuter = 17.01078f;
        const float topOuter = 17.1522f;
        const float road = 24f;

        check("[derive] the legs are the part that stands beside the cables",
            BridgeCables.LegIndexOf(3) == 1, null);
        check("[derive] a placeholder's only part is the part",
            BridgeCables.LegIndexOf(1) == 0, null);

        foreach (var extra in new[] { 0f, 2f, 4f, 8f, 16f, 26f, 40f })
        {
            // Where the cables end up: their outer edge moves by half the extra.
            var moved = cableOuter + (extra * 0.5f);

            var fromLegs = BridgeCables.ExtraForTower(BridgeCables.Suspension, moved, legOuter, 1, 3);
            check($"[derive] at {extra} m the replacement is sized from its legs",
                Math.Abs(fromLegs - extra) < 0.001f, $"got {fromLegs:0.####}");

            // The placeholder is one part and it is the top, at a different distance - and it has to
            // come out the same, or the two halves of one tower would be different widths.
            var fromTop = BridgeCables.ExtraForTower(BridgeCables.Suspension, moved, topOuter, 0, 1);
            check($"[derive] at {extra} m the placeholder is sized to the same width",
                Math.Abs(fromTop - fromLegs) < 0.001f, $"{fromTop:0.####} against {fromLegs:0.####}");

            // And it agrees with the rule it replaces, whenever tower and cables share a bridge.
            check($"[derive] at {extra} m it agrees with the road rule on a matched pair",
                Math.Abs(fromLegs - ((road + extra) - road)) < 0.001f, null);
        }

        // The case the change exists for: the 4-lane bridge's cables under the 5-lane bridge's tower.
        // The road rule would widen the tower by the deck, knowing nothing about these cables; this
        // narrows it by 4 m so it stands the archetype's distance outside the cables that are there.
        var mismatched = BridgeCables.ExtraForTower(BridgeCables.Suspension, 11.47333f, legOuter, 1, 3);
        check("[derive] cables from a narrower bridge size the tower down to meet them",
            Math.Abs(mismatched + 4f) < 0.001f, $"got {mismatched:0.####}");

        // Whatever it produces, the result satisfies the distance - which is the whole point.
        foreach (var cables in new[] { 9.47333f, 11.47333f, 13.47333f, 21.47333f, 33.47333f })
        {
            var derived = BridgeCables.ExtraForTower(BridgeCables.Suspension, cables, legOuter, 1, 3);
            var stood = TowerWidening.Spread(legOuter, derived);
            check($"[derive] a tower sized to cables at {cables:0.##} stands the archetype's distance",
                BridgeCables.SpacingHolds(BridgeCables.Suspension, stood - cables, 1, 3),
                $"{stood - cables:0.####} m");
        }
    }

    /// <summary>
    /// The gap between a portal's legs, which bounds cannot report.
    ///
    /// A bounding box has an outer face and no inner one, so no dump that prints extents contains this
    /// number. The obvious substitute - the smallest distance from the centre line to any vertex - is
    /// wrong here and gives zero, because a tower's repeatable segment is two legs and a crossbeam: the
    /// rungs of the ladder repeat with the segment, and a rung runs straight through the middle.
    ///
    /// So the shape is sliced across its height. The bands holding a rung report nearly nothing, the
    /// bands between them hold legs alone and report the real gap, and the widest wins.
    /// </summary>
    private static void TheGapBetweenTheLegsIsMeasuredByBand(Action<string, bool, string?> check)
    {
        // A portal: legs from x = 卤6 to 卤8, a rung across the middle at one height only.
        var portal = new List<float3>();
        for (var y = 0; y <= 20; y++)
        {
            portal.Add(new float3(-8f, y, 0f));
            portal.Add(new float3(-6f, y, 0f));
            portal.Add(new float3(6f, y, 0f));
            portal.Add(new float3(8f, y, 0f));
        }

        // The rung, spanning the middle at y = 10.
        for (var x = -6f; x <= 6f; x += 1f) portal.Add(new float3(x, 10f, 0f));

        var vertices = portal.ToArray();

        check("[span] the legs leave a gap the bounds do not mention",
            Math.Abs(TowerWidening.ClearSpanOf(vertices, TowerWidening.SpanBands) - 12f) < 0.001f,
            $"{TowerWidening.ClearSpanOf(vertices, TowerWidening.SpanBands):0.####}");

        check("[span] the bounds only reach the outside",
            Math.Abs(TowerWidening.WidthOf(vertices) - 16f) < 0.001f, null);

        // One band cannot separate the rung from the legs: it answers with the rung's own tessellation
        // instead of the gap, which is the failure this rule exists to avoid and is what the smallest
        // distance to the centre line would have reported.
        check("[span] a single band is answered by the rung, not the legs",
            TowerWidening.ClearSpanOf(vertices, 1)
                < TowerWidening.ClearSpanOf(vertices, TowerWidening.SpanBands) / 4f,
            $"{TowerWidening.ClearSpanOf(vertices, 1):0.####} against "
            + $"{TowerWidening.ClearSpanOf(vertices, TowerWidening.SpanBands):0.####}");
        // Widening moves the legs apart, so the gap grows by the whole extra - which is the invariant
        // the user's rule states: gap minus road is constant when the road grows by the same amount.
        foreach (var extra in new[] { 0f, 4f, 16f, 40f })
        {
            var widened = TowerWidening.Widen(vertices, extra, inner: 6f);
            var gap = TowerWidening.ClearSpanOf(widened, TowerWidening.SpanBands);
            check($"[span] at {extra} m the gap grows by the whole extra",
                Math.Abs(gap - (12f + extra)) < 0.001f, $"{gap:0.####}");
        }

        // A shape with nothing on one side has no facing pair, and is not a span of half the world.
        var oneSided = new[] { new float3(3f, 0f, 0f), new float3(5f, 1f, 0f) };
        check("[span] a one sided shape has no span",
            Math.Abs(TowerWidening.ClearSpanOf(oneSided, TowerWidening.SpanBands)) < 0.001f, null);

        check("[span] nothing has no span",
            Math.Abs(TowerWidening.ClearSpanOf(Array.Empty<float3>(), 8)) < 0.001f, null);
    }


    /// <summary>
    /// A pylon whose legs are not vertical keeps them rigid at every height.
    ///
    /// This is the V pylon. Its legs converge downward, so its opening is a different number at every
    /// height: 36 m at the top, near nothing at the apex. Asking the question once for the whole shape
    /// answers it with the top - a boundary of 18 - and every leg below that height lies inside it and
    /// gets scaled. That is what the V pylon did, and a scaled leg is the fault this rule exists to
    /// prevent. Asking per height puts every leg outside its own band's boundary and carries it out
    /// rigidly.
    /// </summary>

    /// <summary>
    /// Whatever a style adds beyond its road reaches its tower and its cables alike.
    ///
    /// The distance from the cables to the tower's outer edge belongs to the archetype and holds at
    /// every road width, so the two have to be widened by the same number. They are computed at two
    /// sites - the tower from the road it was authored for, the cables from the road recorded for the
    /// style - and the bonus was added at one of them only. Three metres of tower and none of cable
    /// moved the golden bridge's cables a metre and a half per side, at every width, by construction.
    ///
    /// The distance test upstream could not see it: it asks whether the gap holds given one extra, and
    /// the fault was that there were two. What this asks is whether the two can be different.
    /// </summary>
    private static void ATowerAndItsCablesAreWidenedByOneNumber(Action<string, bool, string?> check)
    {
        foreach (var styleId in BridgeTowers.Styles)
        {
            var bonus = BridgeTowers.BonusFor(styleId);
            if (Math.Abs(bonus) < 0.001f) continue;

            // The tower measures against the road it was authored for; the cables against the road
            // recorded for the style. Equal for every style, or the same bonus still lands the two in
            // different places.
            var recorded = BridgeTowers.RoadOf(styleId);
            foreach (var tower in BridgeTowers.For(styleId))
            {
                check($"[bonus] {styleId}: '{tower.Name}' measures against the style's own road",
                    Math.Abs(tower.Road - recorded) < 0.001f,
                    $"tower {tower.Road:0.##} against style {recorded:0.##}");
            }

            // And the widening the two receive, at any width, is one number.
            foreach (var deck in new[] { 12f, 20f, 24f, 33f, 48f })
            {
                var towerExtra = deck - recorded + bonus;
                var cableExtra = deck - recorded + bonus;
                check($"[bonus] {styleId}: at {deck} m the tower and cables move together",
                    Math.Abs(towerExtra - cableExtra) < 0.001f, null);
            }
        }
    }


    /// <summary>
    /// A deck spanning between the legs stretches to meet them, however far short of them it starts.
    ///
    /// This is the golden bridge's top decoration. It crosses the centre, so by rule 8 it stretches -
    /// but stretching it about the widest thing at its height stretches it by the wrong amount. Its
    /// ends sit at 12 m and the legs stand at 26; scaled against 26 its ends move by less than half
    /// what the legs move, and a gap opens either side of it that grows with every metre of road. It
    /// has to be scaled against its own outer end, which is what the triangles are read for.
    ///
    /// Nothing in the vertices alone can say this. At that height the material sits at 12 and at 22
    /// and at 26, and which of those numbers is one member and which is another is a question about
    /// what is joined to what.
    /// </summary>

    /// <summary>
    /// The second deck goes below the road, whichever way the archetype hangs its own.
    ///
    /// The archetypes disagree: the V pylon hangs its train track below and the A pylon carries a
    /// second carriageway above, and the mod put the player's chosen deck wherever the archetype's own
    /// net sat. On the A pylon that put it on top of the road - the two levels the wrong way round,
    /// which is what was reported.
    /// </summary>

    /// <summary>
    /// Whether a wing kept its shape, which is the one thing no width can tell you.
    ///
    /// A base block spanning the centre with a wing on each end. Carried, the wing keeps its depth and
    /// slides out; scaled, it comes back deeper. Both put its outer edge in exactly the same place, so
    /// every width in every report agrees either way and the only thing that differs is how thick the
    /// wing is. That is what the generator measures now, and it is why the V pylon's base could be
    /// stretched whole with nothing saying so.
    /// </summary>

    /// <summary>
    /// An openwork ornament between the legs stretches with them instead of coming apart.
    ///
    /// The golden bridge's top decoration is a fan of ribs springing from an arch between the two
    /// legs, with air between every rib. At most heights nothing stands on the centre line, because
    /// what is there is a hole, so height by height the ornament reads as two separate sides and each
    /// is carried outward - which pulls the fan into halves and is what the bridge showed.
    ///
    /// It is not two sides. It is one ornament that meets the legs, and the thing that says so is that
    /// the legs are also there above it and below it, at heights where nothing crosses the centre. The
    /// leg's inner face read from those heights is what the ornament is stretched against.
    /// </summary>

    /// <summary>
    /// A thickness is only a thickness of the thing it was measured on.
    ///
    /// A section hands one profile to every piece it holds and to every level of detail of each, which
    /// is what keeps a feature appearing in more than one of them moving the same way. That profile is
    /// not a measurement of any one of those meshes: it is the whole section, anchorage included.
    ///
    /// Measuring a mesh before against the section and after against itself reports material going
    /// from 9.89 m thick to 0.32 m with nothing having happened to it - the two numbers describe
    /// different things. The generator raised that as a defect on a cable section's coarse mesh, and it
    /// was the check that was wrong.
    /// </summary>

    /// <summary>
    /// A tower brought in past its own narrowest opening arrives as one column.
    ///
    /// The V pylon's legs converge downward: 36 m apart at the top and 5.79 m at the bottom. Bringing
    /// the tower in by twelve metres carries each leg three metres past the centre, where it is
    /// stopped, and the two arrive touching - the whole lower half becomes a single post and the
    /// bridge is unrecognisable.
    ///
    /// The guard that exists for this asked the widest opening, which is the one number that survives:
    /// 36 m of opening takes twelve metres of narrowing without complaint. It has to ask the narrowest,
    /// because that is the part that closes first.
    /// </summary>

    /// <summary>
    /// The base that carries the road deck is carried by the whole of d, by the mapping and nothing
    /// else.
    ///
    /// Its blocks stand clear of the centre - the road passes between them and rests on them - so it
    /// is material belonging to one side. It is also the part seen against the road: a base a metre
    /// out is a bridge a metre out, whatever the rest of the tower is doing, which is why nothing
    /// about another part of the same tower may reduce the d it is carried by.
    ///
    /// The V pylon's base is the measured one: blocks from 20.43 m to 28.07 m either side, with 40.86 m
    /// of daylight between them for the road.
    /// </summary>
    private static void TheBaseCarryingTheDeckTakesTheWholeMapping(Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Quad(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        for (var x = 20.43f; x < 28.07f; x += 1f) Quad(x, Math.Min(x + 1f, 28.07f), -3.5f, 5f);
        for (var x = -28.07f; x < -20.43f; x += 1f) Quad(x, Math.Min(x + 1f, -20.43f), -3.5f, 5f);

        var baseBlocks = vertices.ToArray();
        var outline = triangles.ToArray();
        var profile = TowerWidening.Profile.Of(
            new[] { baseBlocks }, new IReadOnlyList<int>?[] { outline });

        // Widened and narrowed, including further than the tower above it could take.
        foreach (var extra in new[] { 24f, 8f, 0f, -8f, -16f, -24f })
        {
            var d = extra * 0.5f;
            var moved = TowerWidening.WidenParts(baseBlocks, extra, profile);

            var byMapping = true;
            for (var index = 0; index < baseBlocks.Length; index++)
            {
                var x = baseBlocks[index].x;
                var wanted = x + (x > 0f ? d : -d);
                if (Math.Abs(moved[index].x - wanted) > 0.0001f) byMapping = false;
                if (Math.Abs(moved[index].y - baseBlocks[index].y) > 0.0001f) byMapping = false;
                if (Math.Abs(moved[index].z - baseBlocks[index].z) > 0.0001f) byMapping = false;
            }

            check($"[base] at {extra} m the base is x + sgn(x) * d and nothing else",
                byMapping, $"d = {d:0.##}");

            // Which means it keeps its own depth, and the daylight the road passes through changes by
            // exactly the extra.
            var daylight = TowerWidening.ClearSpanOf(moved, TowerWidening.SpanBands);
            check($"[base] at {extra} m the road's daylight changes by the whole extra",
                Math.Abs(daylight - (40.86f + extra)) < 0.01f, $"{daylight:0.##} m");
        }

        // And the number it is carried by is its own. A tower whose other part cannot take that much
        // does not reduce it - that was the fault: the base came in 4.79 m where the road wanted 8,
        // because the legs above it stood 5.79 m apart.
        var wanted8 = TowerWidening.WidenParts(baseBlocks, -16f, profile);
        var held = TowerWidening.WidenParts(baseBlocks, -9.58f, profile);
        check("[base] a narrow part elsewhere in the tower does not hold the base back",
            Math.Abs(TowerWidening.WidthOf(wanted8) - (56.14f - 16f)) < 0.01f
                && TowerWidening.WidthOf(held) > TowerWidening.WidthOf(wanted8) + 3f,
            $"{TowerWidening.WidthOf(wanted8):0.##} m against {TowerWidening.WidthOf(held):0.##} m held back");
    }

    private static void ATowerIsNotBroughtInPastItsNarrowestPart(Action<string, bool, string?> check)
    {
        // A V: legs 2.9 m from the centre at the bottom, 18 m at the top.
        var vee = new List<float3>();
        for (var step = 0; step <= 40; step++)
        {
            var x = 2.9f + (step / 40f * 15.1f);
            vee.Add(new float3(x, step, 0f));
            vee.Add(new float3(-x, step, 0f));
        }

        var shape = vee.ToArray();

        // The widest opening survives twelve metres of narrowing; the narrowest does not.
        var widest = TowerWidening.ClearSpanOf(shape, TowerWidening.SpanBands);
        check("[narrow] the widest opening is the top and takes the narrowing",
            (widest * 0.5f) - 6f > 0f, $"{widest:0.##} m");

        // What actually happens at the bottom, where the legs stand 5.8 m apart.
        var closed = TowerWidening.WidenParts(shape, -12f);
        var bottom = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].y) > 0.001f) continue;
            bottom = Math.Max(bottom, Math.Abs(closed[index].x));
        }

        check("[narrow] brought in past it, the legs are carried through the centre",
            Math.Abs(bottom - 3.1f) < 0.001f, $"{bottom:0.##} m past the centre");

        // Holding the tower back so that no part reaches the centre was tried and is worse. It cost
        // every other part its width: a base that should have come in eight metres came in 4.79,
        // because the legs above it could not take eight - and the base is the part you see against
        // the road. The tower is narrowed by the whole of d and the narrow part crosses.
        var topBefore = TowerWidening.WidthOf(shape);
        var topAfter = TowerWidening.WidthOf(closed);
        check("[narrow] the tower is narrowed by the whole of d, not by what its narrowest part allows",
            Math.Abs((topBefore - topAfter) - 12f) < 0.001f,
            $"{topBefore - topAfter:0.##} m against 12 m");

        // The top, which had the room, gets all of it.
        var topLeg = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].y - 40f) > 0.001f) continue;
            topLeg = Math.Max(topLeg, Math.Abs(closed[index].x));
        }

        check("[narrow] the part with room is brought in by the whole of d",
            Math.Abs(topLeg - (18f - 6f)) < 0.001f, $"{topLeg:0.##} m from the centre");
    }


    /// <summary>
    /// A cable plane running up past a crossing band is carried whole, not sheared across it.
    ///
    /// The golden bridge's cable section crosses the centre at deck level and nowhere above it: one
    /// band of crossing, sixteen of open air. Its cables and railings run vertically through that
    /// boundary. Asked per height, the bottom of a railing was scaled and the rest of it carried, so
    /// it came out leaning - and the elements either side of the boundary, moving by different amounts,
    /// closed the gap between them and merged. A railing 0.18 m thick was reported 1.89 m thick, which
    /// was the two of them read as one.
    ///
    /// Material is not built per height. A piece of it that never touches the centre over its whole
    /// extent is carried entire.
    /// </summary>

    /// <summary>
    /// A part and the coarse meshes that stand in for it are measured together, or they widen by
    /// different amounts.
    ///
    /// A level of detail is the same part drawn with fewer triangles, and drawn a little differently:
    /// its outer face lands where the fine one's is to within a few centimetres, not exactly. Measure
    /// the scope from the fine mesh alone and the coarse one's outermost material falls outside the
    /// places that scope calls carried, so it is scaled where the fine one is carried. The part comes
    /// out 8 m narrower up close and 7.899 m narrower at distance, which is a bridge that changes width
    /// as the camera pulls back.
    /// </summary>

    /// <summary>
    /// What a level of detail may widen by, and what it may not.
    ///
    /// Two invariants, because there are two branches. Carried material moves by the whole of d on
    /// each side, so a coarse mesh widens by exactly what the part widened, however much narrower it
    /// is. Scaled material is scaled so that the scope's outermost vertex moves by d, so a coarse mesh
    /// a little narrower than the part widens by a little less - proportionally right and absolutely
    /// different. A base 28.05 m across standing in for one 28.07 m across widened 3.95 m where the
    /// part widened 4, and that was reported as the bridge changing width with the camera.
    ///
    /// Demanding the first of both is what raised that. Demanding the second of both would pass a part
    /// that was carried while its level of detail was scaled, which is a real fault - so both are
    /// allowed and that case is prevented where it arises instead, by measuring the two against one
    /// scope.
    /// </summary>

    /// <summary>
    /// The spoke on the centre line of an ornament scales with the ornament, not against itself.
    ///
    /// At the heights between one rib and the next, the central spoke is the only thing near the
    /// middle and there is air either side of it out to the legs. That looks exactly like a member
    /// slung between the legs with a gap it was drawn with - and a member like that is scaled against
    /// its own end, so its ends land where they were drawn to.
    ///
    /// The spoke is not that. It is half a metre wide where the thing it belongs to is fourteen, so
    /// scaling it against its own end blew it up twentyfold at the heights where nothing stood beside
    /// it, and hardly at all at the heights where the arch did. It came out a diamond.
    ///
    /// What separates them is not what shows at one height. It is how far the piece of material each
    /// belongs to runs: the spoke's runs out to the legs, and the slung member's stops before them.
    /// </summary>

    /// <summary>
    /// Which styles bring railings of their own, and what counts as one.
    ///
    /// The golden suspension bridge's railings are golden and live in its own support mesh; the V
    /// pylon's are its own too. The road brings one as well, because an elevated road always does, so
    /// on those two the deck ends up with a white railing standing beside a golden one.
    ///
    /// A piece is one of the road's railings if it is on the side layer and its declared height
    /// reaches above the deck surface. That is what it is, rather than what it is called: the shoulder
    /// side piece tops out 0.2 m below and holds the edge together, the elevated side piece reaches
    /// 0.5 m above and stands on it.
    /// </summary>

    /// <summary>
    /// Where the archetype's inner railing stands, and when it does not.
    ///
    /// The golden bridge carries two railings a side - one at the deck's edge and one at the kerb -
    /// and the space between them is the footway the road puts there. It is the road's footway, not
    /// the bridge's, so the railing is placed against it rather than carried with the deck: on a road
    /// with no footway on that side there is no kerb, and the archetype has no railing there.
    ///
    /// The two sides are separate questions. A footway on one side and a shoulder on the other is an
    /// ordinary road, and its bridge has one inner railing.
    /// </summary>

    /// <summary>
    /// Which of the road's side pieces come off the bridge, decided by the state they are drawn for.
    ///
    /// The game gates them: a piece requiring <c>Elevated</c> and nothing else is the straight run of
    /// the deck, and one requiring <c>Elevated</c> together with a transition is at the end, where the
    /// deck comes down to the road. The bridge carries its own railing along the run and none at the
    /// end, so the road's is wanted at the turnaround and not between.
    ///
    /// Reading the shape instead - "a side piece standing above the deck" - took twelve pieces off
    /// including the road's tunnel, lowered, raised and sound barrier ones. They stand above their own
    /// deck too, on roads that are not this bridge.
    /// </summary>

    /// <summary>
    /// The road's railing is gated to the ends, not taken away.
    ///
    /// A turnaround is elevated deck like any other stretch of it, so the piece that draws the railing
    /// along the run is the piece that draws it round the turnaround too. Dropping it left the
    /// turnaround bare: the bridge carries no railing of its own there, and no other piece of the road
    /// draws there either.
    ///
    /// Kept, and asked for one thing more - that the road ends here. Then it draws where the bridge
    /// has nothing and nowhere the bridge has something.
    /// </summary>

    /// <summary>
    /// A railing is many pieces of material standing at one distance, not one piece.
    ///
    /// The golden bridge's is 188 separate pieces a side - a post, a post, a post - and taking the
    /// second piece for the second railing took the post beside the first one and moved it alone.
    /// Nothing visible changed, which is exactly what was reported: the railing was not fixed.
    ///
    /// What makes a railing is that its pieces stand at the same distance from the centre. What
    /// separates two railings is the footway between them, which is metres against centimetres.
    /// </summary>

    /// <summary>
    /// Taking a railing away means every triangle of it losing its area, which takes all three
    /// coordinates.
    ///
    /// Drawing its vertices together across the bridge only is not enough: each quad still has its
    /// corners at different heights and different points along the span, so it stands in one plane and
    /// is drawn - a flat sheet where a railing was. A triangle is skipped when its three corners are
    /// at one point, and nowhere short of that.
    /// </summary>

    /// <summary>
    /// Two railings show as two bands of occupied material with the footway empty between them,
    /// however the mesh is split up.
    ///
    /// This is the measurement that does not care. A railing built of 188 posts, one built of a single
    /// rail, and one welded to the deck behind it all occupy the same band; every rule tried so far
    /// turned on which of those it was, and each time the answer changed the rule missed the railing
    /// and reported nothing worth reading.
    /// </summary>

    /// <summary>
    /// Every level of detail of a piece gets the same treatment of its kerb railing.
    ///
    /// A coarse mesh draws the same two railings with fewer triangles and does not always resolve them
    /// as two. Asked for itself it finds one railing, does nothing, and keeps a railing the full detail
    /// mesh has taken away - which is a railing that is there from a distance and gone up close.
    ///
    /// So what to do is decided once, on the mesh that shows the most, and expressed as a band and a
    /// distance: everything standing between these two distances from the centre, on this side, is
    /// carried this far or taken away. A band is something a coarse mesh can be asked about.
    /// </summary>

    /// <summary>
    /// The measured road, and where its railings end up.
    ///
    /// The road: a 2 m shoulder at one end of the list, a 5 m footway at the other, 28 m across. So one
    /// side gets no kerb railing and the other gets one 5 m in from the railing at the deck's edge -
    /// and which is which is the thing that was wrong twice.
    ///
    /// The section list order and the mesh's axis are two conventions and neither says how they line
    /// up. Restating the order as a signed offset changed nothing, because the offset is the running
    /// total of the order: it cannot disagree with what it is built from. The direction is an
    /// observation, and this is it written down so that reversing it again is a visible change.
    /// </summary>

    /// <summary>
    /// Every generated bridge owns its tower, while road-fitted sections are also isolated.
    ///
    /// Towers used to be cached by style and width, so two bridges named different things were both
    /// handed (for example) Suspension-40. That makes the two exported roads depend on the same
    /// mutable prefab and leaves no ownership boundary for runtime creation.
    ///
    /// A tower is now owned by a bridge whether or not its current geometry happens to be a pure
    /// function of width. The bridge name is part of every tower key. Kerb-railing sections retain the
    /// same isolation for the separate reason that they are fitted to one road's footways.
    /// </summary>


    /// <summary>
    /// A railing taken off the bridge is taken out of the mesh, not left in it with no size.
    ///
    /// Drawing its vertices to one point makes every triangle of it disappear from the picture, and
    /// leaves every one of them in the index buffer for the renderer to carry and the file to hold. A
    /// thing that is not on the bridge should not be in the bridge.
    ///
    /// The triangles go where all three of their corners belong to it. One corner or two means the
    /// triangle bridges the railing and what it stands on, and dropping it would leave a hole in that.
    /// </summary>

    /// <summary>
    /// A plan for one piece is not carried out on the next thing derived.
    ///
    /// What to do with a kerb railing is held in a field, so that a piece and every level of detail of
    /// it get the same treatment. A field outlives the piece that set it, and the towers are derived
    /// after the sections: a plan left standing was applied to them too, and everything of a tower
    /// that stood in the railing's band - part of a leg - was drawn to a single point.
    ///
    /// So it is carried out only where a railing is being fitted, and never on what merely comes next.
    /// </summary>
    private static void APlanIsCarriedOutOnlyWhereItBelongs(Action<string, bool, string?> check)
    {
        // The band a kerb railing occupied on the golden bridge, and a piece of tower leg that stands
        // in the same place.
        const float from = 9.01f;
        const float to = 9.5f;

        var leg = new[]
        {
            new float3(9.2f, 0.5f, 0f),
            new float3(9.4f, 2.5f, 4f),
            new float3(9.3f, 40f, 0f),
        };

        static float3[] Apply(float3[] vertices, float from, float to, bool apply) =>
            apply
                ? vertices.Select(vertex => Math.Abs(vertex.x) >= from && Math.Abs(vertex.x) <= to
                    ? new float3(from, 0f, 0f)
                    : vertex).ToArray()
                : vertices.ToArray();

        var leaked = Apply(leg, from, to, apply: true);
        var guarded = Apply(leg, from, to, apply: false);

        check("[leak] carried out on a tower, its leg is drawn to one point",
            leaked.Distinct().Count() == 1, $"{leaked.Distinct().Count()} distinct position(s)");

        check("[leak] guarded, the tower is untouched",
            guarded.SequenceEqual(leg), null);

        // Which is what the picture showed: a leg with no thickness and no height, smeared into the
        // deck. Nothing else reports it, because the tower's declared width is unchanged.
        var before = leg.Max(vertex => vertex.y) - leg.Min(vertex => vertex.y);
        var after = leaked.Max(vertex => vertex.y) - leaked.Min(vertex => vertex.y);
        check("[leak] and the leg loses its height, which no width would show",
            before > 39f && after < 0.001f, $"{before:0.#} m became {after:0.#} m");
    }


    /// <summary>
    /// Which bands standing on the deck belong to each golden railing.
    ///
    /// The measured ones, from the golden bridge: material at 9..10, 12.5..13.25 and 16.25..17.25 m
    /// from the centre. The first is the removable inner railing, the second is the outer railing on
    /// each side, and the third is suspension structure beyond the deck. The target road prefab's
    /// measured outer section boundary distinguishes the outer railing from that farther structure.
    /// </summary>

    /// <summary>
    /// The spoke on the centre line comes out the shape it went in, whether or not the ornament it
    /// belongs to reaches the legs.
    ///
    /// A crossing member is scaled about the centre, and what it is scaled against decides its shape.
    /// Against what it happens to reach at each height, a spoke that stands alone between the ribs and
    /// beside the arch below them gets a different ratio at every height: wide in the middle, narrow
    /// top and bottom - a kite where a rectangle was.
    ///
    /// How far the piece of material reaches is a fact about the member. How far it reaches at one
    /// height is a fact about where you cut it.
    /// </summary>

    /// <summary>
    /// A railing taken off the deck is taken off at every distance.
    ///
    /// The levels of detail are separate prefabs, derived after the piece they stand in for. They were
    /// derived without its railing plan - so up close the railing was gone and from a distance it was
    /// still there, which is the same fault from the other side as the one before it.
    ///
    /// The plan travels with the piece: to its own meshes and to the prefabs its levels of detail live
    /// in. It does not travel to the towers, which are derived after the sections and are not part of
    /// any piece - that leak drew a leg to a single point.
    /// </summary>
    private static void ARailingIsTakenOffAtEveryDistance(Action<string, bool, string?> check)
    {
        // What each thing derived is, and whether the plan belongs to it.
        var derived = new (string What, bool Railings)[]
        {
            ("the piece's own mesh", true),
            ("the piece's level of detail", true),
            ("another piece of the same section", true),
            ("a tower part", false),
            ("a tower part's level of detail", false),
        };

        // Which of them take it is the whole of it: everything belonging to the piece, and nothing
        // belonging to a tower.
        var piece = derived.Where(entry => entry.What.StartsWith("the piece", StringComparison.Ordinal))
            .ToList();
        var tower = derived.Where(entry => entry.What.Contains("tower", StringComparison.Ordinal))
            .ToList();

        check("[far] everything belonging to the piece takes the plan",
            piece.Count == 2 && piece.All(entry => entry.Railings), $"{piece.Count} of them");

        check("[far] and nothing belonging to a tower does",
            tower.Count == 2 && tower.All(entry => !entry.Railings), $"{tower.Count} of them");
    }

    private static void TheCentralSpokeKeepsItsShape(Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Bar(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // Legs, standing clear of the ornament so that it is a piece of its own.
        for (var step = 0; step < 40; step++)
        {
            Bar(16f, 20f, step, step + 1f);
            Bar(-20f, -16f, step, step + 1f);
        }

        // The ornament: an arch reaching to 14, ribs rising from it, and a spoke on the centre line
        // that runs above where the ribs end. It touches nothing but itself.
        for (var x = -14f; x < 14f; x += 0.5f) Bar(x, x + 0.5f, 30f, 31f);
        foreach (var rib in new[] { -12f, -8f, -4f, 4f, 8f, 12f }) Bar(rib, rib + 1f, 31f, 36f);
        Bar(-0.5f, 0.5f, 31f, 39f);

        var shape = vertices.ToArray();
        var outline = triangles.ToArray();
        const float extra = 12f;

        var profile = TowerWidening.Profile.Of(
            new[] { shape }, new IReadOnlyList<int>?[] { outline });
        var moved = TowerWidening.WidenParts(shape, extra, profile);

        // Reproduce the real failure: different height bands gave the same two vertical sides
        // different ratios, turning the authored rectangle into an hourglass. The golden-top pass
        // must recover one ratio from the source member and use it throughout.
        for (var index = 0; index < moved.Length; index++)
        {
            if (Math.Abs(shape[index].x) > 0.6f || shape[index].y <= 31f) continue;
            moved[index].x *= 1f + ((shape[index].y - 31f) * 0.2f);
        }

        // Two fan tips pass through the same narrow centre band, but their triangles reach well
        // outside it. They must not be mistaken for vertices of the vertical spoke.
        var leftTip = shape.Length;
        var withFanTips = shape.Concat(new[]
        {
            new float3(-0.25f, 35f, 0f),
            new float3(-10f, 33f, 0f),
            new float3(-10f, 37f, 0f),
            new float3(0.25f, 35f, 0f),
            new float3(10f, 33f, 0f),
            new float3(10f, 37f, 0f),
        }).ToArray();
        var withFanTriangles = outline.Concat(new[]
        {
            leftTip, leftTip + 1, leftTip + 2,
            leftTip + 3, leftTip + 4, leftTip + 5,
        }).ToArray();
        var withMovedFanTips = moved.Concat(new[]
        {
            new float3(-4f, 35f, 0f),
            new float3(-16f, 33f, 0f),
            new float3(-16f, 37f, 0f),
            new float3(4f, 35f, 0f),
            new float3(16f, 33f, 0f),
            new float3(16f, 37f, 0f),
        }).ToArray();

        var corrected = TowerWidening.RectangularizeCentralSpoke(
            withFanTips, withMovedFanTips, withFanTriangles, out var spokeHalfWidth, out var spokeScale);
        check("[kite] the golden-top correction found the central vertical member",
            corrected > 0 && Math.Abs(spokeHalfWidth - 0.5f) < 0.051f,
            $"{corrected} vertices at {spokeHalfWidth:0.###} m");

        check("[kite] its thickness is preserved rather than scaled with the bridge",
            Math.Abs(spokeScale - 1f) < 0.001f,
            $"scale {spokeScale:0.###}");

        check("[kite] neighbouring fan tips are not mistaken for the spoke",
            Math.Abs(withMovedFanTips[leftTip].x + 4f) < 0.001f
                && Math.Abs(withMovedFanTips[leftTip + 3].x - 4f) < 0.001f,
            $"tips {withMovedFanTips[leftTip].x:0.###}, {withMovedFanTips[leftTip + 3].x:0.###}");

        // The spoke's width, low down where the ribs stand beside it and high up where they do not.
        float Width(float atHeight)
        {
            var widest = 0f;
            for (var index = 0; index < shape.Length; index++)
            {
                if (Math.Abs(shape[index].y - atHeight) > 0.001f) continue;

                // The spoke's own vertices only. The arch is tessellated to half metres and has some of
                // its own within a metre of the centre, so a wider net measures the arch at one height
                // and the spoke at the other - which is a fault in the measurement, not in the shape.
                if (Math.Abs(shape[index].x) > 0.6f) continue;

                widest = Math.Max(widest, Math.Abs(withMovedFanTips[index].x));
            }

            return widest * 2f;
        }

        var low = Width(31f);
        var high = Width(39f);

        check("[kite] the spoke is the same width top and bottom",
            Math.Abs(low - high) < 0.001f, $"{low:0.###} m low, {high:0.###} m high");

        check("[kite] and it is still a spoke, not a wall",
            high > 0.9f && high < 3f, $"{high:0.###} m");

        // Scaled against what it reaches at each height instead: at 39 m it is alone and its own end is
        // half a metre, so the ratio is enormous; at 31 m the arch is beside it and the ratio is small.
        const float d = extra * 0.5f;
        var aloneRatio = (0.5f + d) / 0.5f;
        var besideRatio = (14f + d) / 14f;
        check("[kite] which is what asking at each height would have given",
            aloneRatio > besideRatio * 5f,
            $"{aloneRatio:0.##} against {besideRatio:0.##}");
    }

    private static void TheGoldenOuterRailingKeepsBothOfItsBands(Action<string, bool, string?> check)
    {
        static bool Close(float left, float right) => Math.Abs(left - right) < 0.001f;

        static void AddBand(List<float3> vertices, float from, float to)
        {
            for (var slot = (int)(from * 4f); slot < (int)(to * 4f); slot++)
            {
                var x = (slot + 0.5f) * 0.25f;
                vertices.Add(new float3(x, 1f, 0f));
                vertices.Add(new float3(-x, 1f, 0f));
            }
        }

        var points = new List<float3>();
        AddBand(points, 9f, 10f);
        AddBand(points, 12.5f, 13.25f);
        AddBand(points, 16.25f, 17.25f);
        var source = points.ToArray();
        var moved = (float3[])source.Clone();
        for (var index = 0; index < moved.Length; index++)
        {
            var at = Math.Abs(source[index].x);
            if (at < 12.5f || at > 13.25f) continue;
            moved[index].x = Math.Sign(source[index].x) * (at + 0.75f);
        }

        var bands = GoldenBridgeRailings.BandsOf(source, -0.5f, 3f);
        check("[pick] the measured golden mesh has its three known bands",
            bands.Count == 3, $"{bands.Count} band(s)");

        var planned = GoldenBridgeRailings.TryPlan(
            bands, source, moved, -0.5f, 3f,
            new RoadEdge(14f, 9f, isSidewalk: true), 1f, out var railing);
        check("[pick] the innermost band is the removable inner railing",
            planned && Close(railing.Layout.Inner.From, 9f) && Close(railing.Layout.Inner.To, 10f),
            planned ? $"{railing.Layout.Inner.From:0.##}..{railing.Layout.Inner.To:0.##}" : "none");
        check("[pick] the deck-edge band is the outer railing",
            planned && Close(railing.Layout.Outer.From, 12.5f),
            planned ? $"{railing.Layout.Outer.From:0.##}..{railing.Layout.Outer.To:0.##}" : "none");
        check("[pick] suspension material beyond the deck is not mistaken for a railing",
            planned && railing.Layout.Outer.To < 16.25f, null);
        check("[pick] railing gap is the outermost sidewalk width less the road-surface strip",
            planned
                && Close(railing.SidewalkWidth, 5f)
                && Close(railing.RailingGap, 4f)
                && Close(railing.OuterEdgeAfter - railing.InnerTarget, 4f),
            planned ? $"{railing.OuterEdgeAfter:0.###} - {railing.InnerTarget:0.###} m" : "none");
        check("[pick] the inner railing moves one metre outward from the uncompensated target",
            planned && Close(railing.InnerTarget, 9.875f), null);
        check("[pick] the inner railing is fitted by its road-facing edge, not its centre or outer edge",
            planned && Close(railing.InnerEdgeBefore, 9.125f) && Close(railing.InnerTarget, 9.875f),
            planned ? $"{railing.InnerEdgeBefore:0.###} -> {railing.InnerTarget:0.###} m" : "none");

        var plannedLeft = GoldenBridgeRailings.TryPlan(
            bands, source, moved, -0.5f, 3f,
            new RoadEdge(14f, 9f, isSidewalk: true), -1f, out var leftRailing);
        check("[pick] left and right inner railing moves are mirrored",
            planned && plannedLeft && Close(railing.Shift, -leftRailing.Shift),
            planned && plannedLeft ? $"{leftRailing.Shift:0.###}, {railing.Shift:0.###} m" : "none");

        var narrowerSidewalk = GoldenBridgeRailings.TryPlan(
            bands, source, moved, -0.5f, 3f,
            new RoadEdge(14f, 11f, isSidewalk: true), 1f, out var threeMetreRailing);
        check("[pick] a different road uses its own outermost sidewalk section width",
            narrowerSidewalk
                && Close(threeMetreRailing.SidewalkWidth, 3f)
                && Close(threeMetreRailing.RailingGap, 2f)
                && Close(threeMetreRailing.OuterEdgeAfter - threeMetreRailing.InnerTarget, 2f),
            narrowerSidewalk ? $"{threeMetreRailing.SidewalkWidth:0.###} m" : "none");

        var removable = GoldenBridgeRailings.TryPlan(
            bands, source, moved, -0.5f, 3f,
            new RoadEdge(14f, 14f, isSidewalk: false), 1f, out var withoutSidewalk);
        check("[pick] no outermost sidewalk removes only the inner railing",
            removable
                && withoutSidewalk.Remove
                && Close(withoutSidewalk.Layout.Inner.From, 9f)
                && Close(withoutSidewalk.Layout.Outer.From, 12.5f), null);
    }

    private static void ARailingTakenAwayLeavesTheMesh(Action<string, bool, string?> check)
    {
        // A strip of deck with a railing standing on part of it: vertices 0..3 are the deck, 4..7 the
        // railing, and one triangle joins them.
        var dropped = new[] { false, false, false, false, true, true, true, true };
        var triangles = new[]
        {
            0, 1, 2,   // deck
            0, 2, 3,   // deck
            4, 5, 6,   // railing
            4, 6, 7,   // railing
            3, 4, 7,   // where the railing meets the deck
        };

        static int[] Keep(int[] triangles, bool[] dropped)
        {
            var kept = new List<int>();
            for (var corner = 0; corner + 2 < triangles.Length; corner += 3)
            {
                if (dropped[triangles[corner]]
                    && dropped[triangles[corner + 1]]
                    && dropped[triangles[corner + 2]])
                {
                    continue;
                }

                kept.Add(triangles[corner]);
                kept.Add(triangles[corner + 1]);
                kept.Add(triangles[corner + 2]);
            }

            return kept.ToArray();
        }

        var kept = Keep(triangles, dropped);

        check("[gone] the railing's own triangles are not written",
            kept.Length == triangles.Length - 6, $"{kept.Length / 3} of {triangles.Length / 3}");

        check("[gone] and the mesh is smaller for it, not the same size with nothing in it",
            kept.Length < triangles.Length, null);

        // The triangle that joins the railing to the deck is kept, or the deck is left open there.
        check("[gone] the triangle bridging the two is kept",
            kept.Skip(kept.Length - 3).SequenceEqual(new[] { 3, 4, 7 }),
            string.Join(",", kept.Skip(kept.Length - 3)));

        // Nothing of the deck is touched.
        check("[gone] the deck keeps all of its own",
            kept.Take(6).SequenceEqual(new[] { 0, 1, 2, 0, 2, 3 }), null);

        // With nothing marked, nothing is dropped - a bridge whose sides both have footways keeps
        // every triangle it came with.
        var none = new bool[8];
        check("[gone] with nothing marked, the mesh is untouched",
            Keep(triangles, none).SequenceEqual(triangles), null);
    }

    private static void WhatIsFittedToARoadIsNotShared(Action<string, bool, string?> check)
    {
        static string SectionName(string styleId, string section, float extra, string bridge) =>
            BridgeTowers.BringsItsOwnRailings(styleId)
                ? $"{section}-{bridge}"
                : $"{section} {extra:0.#}";

        static string TowerName(string styleId, float width, string bridge) =>
            TowerPrefabNaming.ForBridge(styleId, width, bridge, "Primary", primary: true);

        // Two bridges, one width, different footways - which is the case that goes wrong.
        const float width = 28f;
        var first = "六车道快速路主路_SuspensionGolden";
        var second = "三车道快速路辅路_SuspensionGolden";

        check("[share] the golden family's sections are named for their bridge",
            SectionName("SuspensionGolden", "SuspensionBridge03 Section", 1f, first)
                != SectionName("SuspensionGolden", "SuspensionBridge03 Section", 1f, second),
            SectionName("SuspensionGolden", "SuspensionBridge03 Section", 1f, first));

        check("[share] and so are its towers",
            TowerName("SuspensionGolden", width, first) != TowerName("SuspensionGolden", width, second),
            TowerName("SuspensionGolden", width, first));

        // A plain suspension bridge used to stop at Suspension-28. It now carries its owner too.
        check("[share] a plain suspension tower is independent for every bridge",
            SectionName("Suspension", "SuspensionBridge01 Section", 1f, first)
                == SectionName("Suspension", "SuspensionBridge01 Section", 1f, second)
            && TowerName("Suspension", width, first) != TowerName("Suspension", width, second),
            TowerName("Suspension", width, first));

        check("[share] the V pylon is independent too",
            TowerName("Extradosed03", width, first) != TowerName("Extradosed03", width, second),
            TowerName("Extradosed03", width, first));

        check("[share] the owner follows the bridge prefix-name convention",
            TowerName("Suspension", width, first) == $"Suspension-28-{first}",
            TowerName("Suspension", width, first));

        var secondary = TowerPrefabNaming.ForBridge(
            "SuspensionGolden", width, first, "SuspensionBridge03NetPillar", primary: false);
        check("[share] a secondary structure is still owned by the same bridge",
            secondary == $"SuspensionGolden-28-{first} SuspensionBridge03NetPillar",
            secondary);

        // And the same bridge asking twice gets the same name, which is what lets a double deck use
        // one tower for both of its decks.
        check("[share] the same bridge asking twice is handed the same one",
            TowerName("SuspensionGolden", width, first) == TowerName("SuspensionGolden", width, first),
            null);
    }

    private static void TheSideOfTheRoadIsTheOneObserved(Action<string, bool, string?> check)
    {
        // The sections, in list order, with their running positions - as the report prints them.
        var sections = new (string Name, float Start, float Width)[]
        {
            ("Highway Shoulder 2", 0f, 2f),
            ("Highway Drive Section 4", 2f, 4f),
            ("Highway Drive Section 4", 6f, 4f),
            ("Highway Drive Section 4", 10f, 4f),
            ("RB Median 5", 14f, 5f),
            ("RB Empty Section 4", 19f, 4f),
            ("Sidewalk 5", 23f, 5f),
        };

        const float total = 28f;

        static float Centre(float start, float width, float total) => start + (width * 0.5f) - (total * 0.5f);

        // The first section sits at negative offset and the last at positive, by construction.
        check("[side] the first section is at a negative offset",
            Centre(sections[0].Start, sections[0].Width, total) < 0f,
            $"{Centre(sections[0].Start, sections[0].Width, total):0.##} m");

        check("[side] and the last at a positive one",
            Centre(sections[^1].Start, sections[^1].Width, total) > 0f,
            $"{Centre(sections[^1].Start, sections[^1].Width, total):0.##} m");

        // Which is the running total restated, and says nothing about the mesh: the offset is built
        // from the order, so it agrees with the order whatever the mesh does.
        check("[side] the offset cannot disagree with the order it is built from",
            Centre(sections[0].Start, sections[0].Width, total)
                < Centre(sections[^1].Start, sections[^1].Width, total), null);

        // The observation: the section that comes first is the one at positive x, so the footway at
        // the end of the list is the left side's.
        var left = SectionNames.IsSidewalk(sections[^1].Name) ? sections[^1].Width : 0f;
        var right = SectionNames.IsSidewalk(sections[0].Name) ? sections[0].Width : 0f;

        check("[side] the footway is the left side's", Math.Abs(left - 5f) < 0.001f, $"{left:0.#} m");
        check("[side] and the shoulder side gets none", Math.Abs(right) < 0.001f, $"{right:0.#} m");
        check("[side] the empty section beside it is not part of the sidewalk",
            !SectionNames.IsSidewalk(sections[^2].Name) && Math.Abs(left - sections[^1].Width) < 0.001f,
            $"{sections[^2].Name}; sidewalk {left:0.#} m");

        // The corresponding boundary-facing edges stand the sidewalk width less the golden bridge's
        // one-metre road-surface strip apart.
        const float outerAfter = 13.95f;
        check("[side] the 5 m sidewalk produces a 4 m railing gap",
            Math.Abs((outerAfter - (left - 1f)) - 9.95f) < 0.001f,
            $"{outerAfter - (left - 1f):0.##} m");
    }

    private static void EveryLevelOfDetailTreatsTheKerbRailingAlike(Action<string, bool, string?> check)
    {
        // The full detail mesh: two railings, posts a fifth of a metre wide.
        var fine = new List<float3>();
        for (var along = 0; along < 20; along++)
        {
            fine.Add(new float3(13.13f, 0.5f, along));
            fine.Add(new float3(12.96f, 0.5f, along));
            fine.Add(new float3(9.63f, 0.5f, along));
            fine.Add(new float3(9.46f, 0.5f, along));
        }

        // The coarse one: the same two railings as four corners, which no clustering of pieces will
        // separate into two stands.
        var coarse = new[]
        {
            new float3(13.13f, 0.5f, 0f), new float3(12.96f, 0.5f, 20f),
            new float3(9.63f, 0.5f, 0f), new float3(9.46f, 0.5f, 20f),
        };

        // The plan, as a band and a distance: what stands between 9.4 and 9.7 m on the right is
        // carried 1 m further out.
        const float from = 9.4f;
        const float to = 9.7f;
        const float shift = 1f;

        static float3[] Apply(float3[] vertices, float from, float to, float shift) =>
            vertices
                .Select(vertex => Math.Abs(vertex.x) >= from && Math.Abs(vertex.x) <= to
                    ? new float3(vertex.x + shift, vertex.y, vertex.z)
                    : vertex)
                .ToArray();

        var fineMoved = Apply(fine.ToArray(), from, to, shift);
        var coarseMoved = Apply(coarse, from, to, shift);

        check("[level] the full detail mesh's kerb railing moves",
            fineMoved.Count(vertex => Math.Abs(vertex.x - 10.63f) < 0.001f) == 20,
            $"{fineMoved.Count(vertex => Math.Abs(vertex.x - 10.63f) < 0.001f)} vertices");

        check("[level] and so does the coarse one's, though it draws them as one",
            coarseMoved.Count(vertex => Math.Abs(vertex.x - 10.63f) < 0.001f) == 1
                && coarseMoved.Count(vertex => Math.Abs(vertex.x - 10.46f) < 0.001f) == 1,
            string.Join(", ", coarseMoved.Select(vertex => $"{vertex.x:0.##}")));

        // The railing at the deck's edge is not in the band and does not move, in either mesh.
        check("[level] the railing at the edge stays where it is",
            fineMoved.Count(vertex => Math.Abs(vertex.x - 13.13f) < 0.001f) == 20
                && coarseMoved.Count(vertex => Math.Abs(vertex.x - 13.13f) < 0.001f) == 1, null);

        // Asked of the coarse mesh on its own terms - how many stands of railing does it have - it
        // would find one, and do nothing.
        var stands = coarse.Select(vertex => Math.Abs(vertex.x)).Distinct().Count();
        check("[level] which is why it is not asked on its own terms",
            stands == 4, $"{stands} distinct positions, none of them a stand");
    }

    private static void RailingsShowAsBandsHoweverTheMeshIsSplit(Action<string, bool, string?> check)
    {
        static string Bands(float3[] vertices, float low, float high)
        {
            const float bucket = 0.25f;
            var occupied = new SortedSet<int>();
            foreach (var vertex in vertices)
            {
                if (vertex.y <= low || vertex.y >= high) continue;

                occupied.Add((int)Math.Floor(Math.Abs(vertex.x) / bucket));
            }

            var bands = new List<string>();
            var start = int.MinValue;
            var previous = int.MinValue;
            foreach (var slot in occupied)
            {
                if (start == int.MinValue) { start = slot; previous = slot; continue; }
                if (slot == previous + 1) { previous = slot; continue; }

                bands.Add($"{start * bucket:0.##}..{(previous + 1) * bucket:0.##}");
                start = slot;
                previous = slot;
            }

            if (start != int.MinValue) bands.Add($"{start * bucket:0.##}..{(previous + 1) * bucket:0.##}");
            return string.Join(", ", bands);
        }

        // Posts: many small pieces at 13 and at 9.5, on both sides.
        var posts = new List<float3>();
        for (var along = 0; along < 20; along++)
        {
            foreach (var side in new[] { 1f, -1f })
            {
                posts.Add(new float3(side * 12.96f, 0.1f, along));
                posts.Add(new float3(side * 13.13f, 1.1f, along));
                posts.Add(new float3(side * 9.46f, 0.1f, along));
                posts.Add(new float3(side * 9.63f, 1.1f, along));
            }
        }

        check("[bands] two railings read as two bands",
            Bands(posts.ToArray(), -0.5f, 3f) == "9.25..9.75, 12.75..13.25",
            Bands(posts.ToArray(), -0.5f, 3f));

        // The same two railings as continuous rails instead of posts: the same two bands.
        var rails = new List<float3>();
        foreach (var side in new[] { 1f, -1f })
        {
            rails.Add(new float3(side * 12.96f, 0.1f, 0f));
            rails.Add(new float3(side * 13.13f, 1.1f, 84f));
            rails.Add(new float3(side * 9.46f, 0.1f, 0f));
            rails.Add(new float3(side * 9.63f, 1.1f, 84f));
        }

        check("[bands] and read the same when they are not posts at all",
            Bands(rails.ToArray(), -0.5f, 3f) == Bands(posts.ToArray(), -0.5f, 3f), null);

        // One railing reads as one band, which is the case that has to be told apart from two.
        var one = posts.Where(vertex => Math.Abs(vertex.x) > 12f).ToArray();
        check("[bands] one railing reads as one band",
            Bands(one, -0.5f, 3f) == "12.75..13.25", Bands(one, -0.5f, 3f));

        // And the footway between them is empty, which is what makes them two.
        check("[bands] the footway between them holds nothing",
            !Bands(posts.ToArray(), -0.5f, 3f).Contains("10.")
                && !Bands(posts.ToArray(), -0.5f, 3f).Contains("11."), null);
    }

    private static void ARailingTakenAwayHasNoArea(Action<string, bool, string?> check)
    {
        // One quad of a railing post: two heights, two points along the span.
        var quad = new[]
        {
            new float3(9.46f, 0f, 0f),
            new float3(9.63f, 0f, 2f),
            new float3(9.63f, 1.1f, 2f),
            new float3(9.46f, 1.1f, 0f),
        };

        static float Area(float3 a, float3 b, float3 c)
        {
            var ux = b.x - a.x;
            var uy = b.y - a.y;
            var uz = b.z - a.z;
            var vx = c.x - a.x;
            var vy = c.y - a.y;
            var vz = c.z - a.z;
            var cx = (uy * vz) - (uz * vy);
            var cy = (uz * vx) - (ux * vz);
            var cz = (ux * vy) - (uy * vx);
            return (float)Math.Sqrt((cx * cx) + (cy * cy) + (cz * cz)) * 0.5f;
        }

        check("[gone] the railing has area to begin with",
            Area(quad[0], quad[1], quad[2]) > 0.01f,
            $"{Area(quad[0], quad[1], quad[2]):0.###} m2");

        // Drawn together across the bridge only, which is what was written first.
        var across = quad.Select(vertex => new float3(13.13f, vertex.y, vertex.z)).ToArray();
        check("[gone] drawn together across the bridge only, it still has area",
            Area(across[0], across[1], across[2]) > 0.01f,
            $"{Area(across[0], across[1], across[2]):0.###} m2");

        // Drawn to one point, which is what it takes.
        var onto = new float3(13.13f, quad[0].y, quad[0].z);
        var point = quad.Select(_ => onto).ToArray();
        check("[gone] drawn to one point, no triangle of it has any",
            Area(point[0], point[1], point[2]) < 0.0001f
                && Area(point[0], point[2], point[3]) < 0.0001f,
            $"{Area(point[0], point[1], point[2]):0.######} m2");

        // And the point is one the mesh already occupies, so nothing it declares grows.
        check("[gone] the point it is drawn to is inside the mesh",
            Math.Abs(onto.x) <= 13.13f + 0.001f, $"{onto.x:0.##} m");
    }

    private static void ARailingIsAStandOfPostsNotAPiece(Action<string, bool, string?> check)
    {
        // A railing at the deck's edge and one at the kerb, each built of posts, on one side. The
        // measured spread: posts a fifth of a metre apart, railings 3.5 m apart.
        var posts = new List<(float Inner, float Outer)>();
        for (var along = 0; along < 20; along++)
        {
            posts.Add((12.96f, 13.13f));
            posts.Add((9.46f, 9.63f));
        }

        const float spread = 0.5f;

        static List<List<(float Inner, float Outer)>> Rails(
            List<(float Inner, float Outer)> pieces, float spread)
        {
            var rails = new List<List<(float Inner, float Outer)>>();
            foreach (var piece in pieces.OrderByDescending(entry => entry.Outer))
            {
                var last = rails.Count > 0 ? rails[rails.Count - 1] : null;
                if (last != null && last[last.Count - 1].Outer - piece.Outer <= spread)
                {
                    last.Add(piece);
                    continue;
                }

                rails.Add(new List<(float Inner, float Outer)> { piece });
            }

            return rails;
        }

        var rails = Rails(posts, spread);

        check("[posts] forty posts make two railings",
            rails.Count == 2, $"{rails.Count} railing(s) from {posts.Count} pieces");

        check("[posts] each railing has all its posts",
            rails.Count == 2 && rails[0].Count == 20 && rails[1].Count == 20,
            rails.Count == 2 ? $"{rails[0].Count} and {rails[1].Count}" : null);

        check("[posts] the outer railing is the one at the deck's edge",
            rails.Count == 2 && Math.Abs(rails[0][0].Outer - 13.13f) < 0.001f, null);

        check("[posts] the inner railing is the one at the kerb",
            rails.Count == 2 && Math.Abs(rails[1][0].Outer - 9.63f) < 0.001f, null);

        // Taken piece by piece instead, the second piece is the post beside the first - the same
        // railing - and moving it moves one post out of twenty.
        var byPiece = posts.OrderByDescending(entry => entry.Outer).ToList();
        check("[posts] piece by piece, the second is a post of the first railing",
            Math.Abs(byPiece[1].Outer - byPiece[0].Outer) < 0.001f,
            $"{byPiece[0].Outer:0.##} and {byPiece[1].Outer:0.##}");

        // The line has room on both sides: posts of one railing are within centimetres, and the two
        // railings are a footway apart.
        check("[posts] the spread separates railings and not posts",
            0.17f < spread && 3.5f > spread, null);
    }

    private static void TheRoadsRailingIsGatedNotRemoved(Action<string, bool, string?> check)
    {
        // What the piece required before, and what it requires after.
        var before = new[] { "Elevated" };
        var after = before.Append("DeadEnd").ToArray();

        check("[gate] the piece is still there",
            after.Length == before.Length + 1 && after.Contains("Elevated"), string.Join(" + ", after));

        check("[gate] and it now asks for the road to end here",
            after.Contains("DeadEnd"), null);

        // Which is the difference between the two places it used to draw.
        static bool Draws(string[] requires, bool atEnd) =>
            requires.All(requirement => requirement != "DeadEnd" || atEnd);

        check("[gate] before, it drew along the run and at the turnaround",
            Draws(before, atEnd: false) && Draws(before, atEnd: true), null);

        check("[gate] after, it draws at the turnaround only",
            !Draws(after, atEnd: false) && Draws(after, atEnd: true), null);

        // Removing it drew in neither place, which is what the turnaround looked like: a piece that
        // is not in the section cannot be asked whether the road ends here.
        var dropped = Array.Empty<string>();
        check("[gate] removing it would have drawn in neither place",
            dropped.Length == 0 && !dropped.Contains("Elevated"), null);

        // Adding a requirement twice adds it once: the section is derived afresh each export and a
        // bridge rebuilt from a bridge must not accumulate them.
        var twice = after.Contains("DeadEnd") ? after : after.Append("DeadEnd").ToArray();
        check("[gate] asking twice asks once",
            twice.Count(requirement => requirement == "DeadEnd") == 1, string.Join(" + ", twice));
    }

    /// <summary>
    /// Which style is being built has to be known before the first thing is derived.
    ///
    /// The sections come first - the cables, and the railings that live in the same mesh - and the
    /// tower after them. Anything that asked which style was being built while the sections were
    /// widened was asking a null, and a rule that answers "no" to a null does nothing and says
    /// nothing. The inner railing rule did exactly that: it never ran, and there was no note in the
    /// report to say it had not.
    /// </summary>
    private static void TheStyleIsKnownBeforeAnythingIsDerived(Action<string, bool, string?> check)
    {
        // The rule that was asked, and what it answers when nobody has said which style it is.
        check("[order] a null style brings no railings of its own",
            !BridgeTowers.BringsItsOwnRailings(null), null);

        check("[order] which is the same answer as a style that brings none",
            BridgeTowers.BringsItsOwnRailings(null)
                == BridgeTowers.BringsItsOwnRailings("Suspension"), null);

        // So the two are indistinguishable from inside the rule, and the difference has to be made
        // where the style is known: at the start of the bridge rather than at the tower.
        check("[order] and a style that does bring them answers differently",
            BridgeTowers.BringsItsOwnRailings("SuspensionGolden")
                != BridgeTowers.BringsItsOwnRailings(null), null);
    }

    private static void OnlyTheRunLosesItsRailing(Action<string, bool, string?> check)
    {
        // The measured requirements, from the golden bridge's own road.
        var cases = new (string Name, string[] All, bool Removed)[]
        {
            ("Elevated Side Piece 0.5", new[] { "Elevated" }, true),
            ("Elevated Side Piece 0.5 - Ending", new[] { "Elevated", "HighTransition" }, false),
            ("Elevated Side Piece 0.5 - Raised", new[] { "Elevated", "LowTransition" }, false),
            ("Tunnel Side Piece 1", new[] { "Tunnel" }, false),
            ("Tunnel Side Piece 1 - Ending", new[] { "Tunnel", "HighTransition" }, false),
            ("Lowered Side Piece 1", new[] { "Lowered" }, false),
            ("Raised Side Piece 0.5", new[] { "Raised" }, false),
            ("Sound Barrier 1", new[] { "SoundBarrier" }, false),
        };

        static bool OnTheRun(string[] all) =>
            all.Contains("Elevated")
            && !all.Contains("HighTransition")
            && !all.Contains("LowTransition");

        foreach (var entry in cases)
        {
            check($"[turn] '{entry.Name}' {(entry.Removed ? "comes off" : "stays")}",
                OnTheRun(entry.All) == entry.Removed,
                string.Join(" + ", entry.All));
        }

        // The one that matters both ways: the same railing, on the run and at the end.
        check("[turn] the railing on the run comes off and the one at the turnaround stays",
            OnTheRun(new[] { "Elevated" })
                && !OnTheRun(new[] { "Elevated", "HighTransition" }), null);

        // And a piece that is not elevated at all is never this bridge's to take.
        check("[turn] nothing that is not elevated comes off",
            !OnTheRun(new[] { "Tunnel" }) && !OnTheRun(new[] { "Lowered" })
                && !OnTheRun(new[] { "Raised" }) && !OnTheRun(Array.Empty<string>()), null);
    }

    private static void TheKerbRailingFollowsTheFootway(Action<string, bool, string?> check)
    {
        // What counts as a footway, by name, whole word - the game's three spellings and the things
        // that are not one.
        foreach (var name in new[]
        {
            "Sidewalk 3.5", "Sidewalk 3.5 - NoBicycle", "Sidewalk With Bikelane 3.5",
        })
        {
            check($"[kerb] '{name}' is a footway", SectionNames.IsSidewalk(name), null);
        }

        foreach (var name in new[]
        {
            "Highway Shoulder 2", "Highway Drive Section 4", "Road Median 0", "RB Empty Section 4",
            "Highway Side 0",
            "Sidewalks", "MySidewalk", null, "",
        })
        {
            check($"[kerb] '{name ?? "null"}' is not a footway", !SectionNames.IsSidewalk(name), null);
        }

        // A whole word and never a substring, the same rule the outward extension follows and for the
        // same reason: a rule that is nearly right puts the railing somewhere plausible.
        check("[kerb] the footway rule does not fire on a longer word",
            !SectionNames.IsSidewalk("Sidewalkish 3") && !SectionNames.IsSidewalk("NoSidewalk"), null);

        // Where the railing lands. The outer railing ends up at 20 m from the centre, the footway is
        // 3.5 m, and the bridge has a one-metre strip between road and sidewalk, so the kerb railing
        // stands at 17.5. It is carried there, keeping its own shape, whatever the deck did.
        const float outerAfter = 20f;
        const float footway = 3.5f;
        check("[kerb] the kerb railing gap excludes the one-metre road-surface strip",
            Math.Abs((outerAfter - (footway - 1f)) - 17.5f) < 0.001f, null);

        // The two sides do not have to agree. A 3.5 m footway on the left and none on the right gives
        // one inner railing on the left and none on the right at all.
        var leftFootway = 3.5f;
        var rightFootway = 0f;
        check("[kerb] a road with one footway gets one kerb railing",
            leftFootway > 0f && rightFootway <= 0f,
            $"{leftFootway:0.#} m and {rightFootway:0.#} m");

        // Which pieces of a section are its railings, on a mesh shaped like the archetype's: a railing
        // at the deck's edge, one at the kerb, the cables rising past them, and the structure under
        // the deck. Only the first two stand on the deck without going anywhere.
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Bar(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        foreach (var side in new[] { 1f, -1f })
        {
            Bar(side * 16.9f, side * 17.1f, 0f, 1.1f);   // the railing at the edge
            Bar(side * 15.3f, side * 15.5f, 0f, 1.1f);   // the railing at the kerb
            Bar(side * 14.3f, side * 14.5f, 0f, 30f);    // the cables, which also start at the deck
        }

        for (var x = -17f; x < 17f; x += 1f) Bar(x, x + 1f, -2f, -0.5f);  // the structure under it

        var section = vertices.ToArray();
        var pieces = TowerWidening.PiecesOf(section, triangles, out _);

        var railings = pieces
            .Where(piece => piece.Aside && piece.Low > -0.5f && piece.High < 3f)
            .ToList();

        check("[kerb] the two railings are found and nothing else is",
            railings.Count == 4, $"{railings.Count} of {pieces.Length} pieces");

        check("[kerb] the cables are not taken for a railing",
            railings.All(piece => piece.High < 3f), null);

        var rightSide = railings.Where(piece => piece.Left > 0f)
            .OrderByDescending(piece => piece.Outer).ToList();

        check("[kerb] the outermost is the railing at the deck's edge",
            rightSide.Count == 2 && Math.Abs(rightSide[0].Outer - 17.1f) < 0.001f,
            rightSide.Count > 0 ? $"{rightSide[0].Outer:0.##} m" : "none");

        check("[kerb] the one inside it is the railing at the kerb",
            rightSide.Count == 2 && Math.Abs(rightSide[1].Outer - 15.5f) < 0.001f,
            rightSide.Count > 1 ? $"{rightSide[1].Outer:0.##} m" : "none");

        // And it is carried, not scaled: the distance it moves is whatever puts it the compensated
        // railing gap in from the edge, and its own width is untouched.
        const float footwayWidth = 3.5f;
        var outerEnds = 20f;
        var railingGap = footwayWidth - 1f;
        var shift = (outerEnds - railingGap) - rightSide[1].Outer;
        check("[kerb] the kerb railing keeps its width wherever it is put",
            Math.Abs((rightSide[1].Outer + shift) - (outerEnds - railingGap)) < 0.001f
                && Math.Abs((rightSide[1].Outer - rightSide[1].Inner) - 0.2f) < 0.001f,
            $"moved {shift:0.##} m, {rightSide[1].Outer - rightSide[1].Inner:0.##} m wide");
    }

    private static void OnlyTheStylesThatBringRailingsLoseTheRoadsOne(
        Action<string, bool, string?> check)
    {
        check("[rails] the golden family brings its own",
            BridgeTowers.BringsItsOwnRailings("SuspensionGolden"), null);

        // The V pylon was on this list and is not. What it carries of its own does not run the length
        // of the deck, so taking the road.s railing off it left the deck with none.
        check("[rails] the V pylon keeps the road.s",
            !BridgeTowers.BringsItsOwnRailings("Extradosed03"), null);

        // Everything else keeps the road's, which is the only railing those bridges have.
        foreach (var style in new[]
        {
            "Suspension", "CableStayed", "TrussArch", "TrussArch01", "TrussArch03", "Grand", "TiedArch",
            "CoveredWood", "Extradosed01", "Extradosed02", "ExtradosedLarge", "Extradosed03",
        })
        {
            check($"[rails] {style} keeps the road's railing",
                !BridgeTowers.BringsItsOwnRailings(style), null);
        }

        check("[rails] an unknown style keeps the road's",
            !BridgeTowers.BringsItsOwnRailings("no such style")
                && !BridgeTowers.BringsItsOwnRailings(null), null);

        // The measured heights the rule sorts, from the golden bridge's own road: the elevated edge
        // with its railing, and the shoulder that is not one.
        const float deckSurface = 0.25f;
        check("[rails] the elevated edge with its railing stands on the deck",
            0.5f > deckSurface, "0.5 m against 0.25 m");

        check("[rails] the shoulder's side piece does not",
            -0.2f <= deckSurface, "-0.2 m against 0.25 m");

        // And the line has room on both sides of it, which is why a line will do.
        check("[rails] nothing measured sits near the line",
            Math.Abs(0.5f - deckSurface) > 0.2f && Math.Abs(-0.2f - deckSurface) > 0.2f, null);
    }

    private static void TheCentralSpokeScalesWithItsOrnament(Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Strip(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // Legs standing at 14, in one metre courses.
        for (var step = 0; step < 40; step++)
        {
            Strip(14f, 18f, step, step + 1f);
            Strip(-18f, -14f, step, step + 1f);
        }

        // The ornament: an arch across the middle at 30, welded to both legs, with ribs rising from it.
        // The middle rib straddles the centre line; the others do not. Above the arch, at the heights
        // between ribs, there is nothing but air out to the legs.
        // Tessellated to half metres so the arch has vertices where the spoke meets it: in a real
        // mesh they are one piece of material, and here they have to share their positions to be.
        for (var x = -14f; x < 14f; x += 0.5f) Strip(x, x + 0.5f, 30f, 31f);
        Strip(-0.5f, 0.5f, 31f, 39f);
        foreach (var rib in new[] { -12f, -8f, -4f, 4f, 8f, 12f })
        {
            Strip(rib, rib + 1f, 31f, 39f);
        }

        var shape = vertices.ToArray();
        var outline = triangles.ToArray();
        const float extra = 15f;
        const float d = extra * 0.5f;

        var profile = TowerWidening.Profile.Of(
            new[] { shape }, new IReadOnlyList<int>?[] { outline });
        var moved = TowerWidening.WidenParts(shape, extra, profile);

        // Everything in the ornament scales by the one ratio - the arch's, which is the leg's inner
        // face - so the spoke keeps its proportions and the fan keeps its shape.
        var ratio = (14f + d) / 14f;
        var uniform = true;
        var spokeLow = 0f;
        var spokeHigh = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (shape[index].y < 29.5f) continue;

            var wanted = shape[index].x * ratio;
            if (Math.Abs(shape[index].x) < 14f && Math.Abs(moved[index].x - wanted) > 0.001f)
            {
                uniform = false;
            }

            if (Math.Abs(shape[index].x - 0.5f) > 0.001f) continue;
            if (shape[index].y < 32f) spokeLow = moved[index].x;
            if (shape[index].y > 38f) spokeHigh = moved[index].x;
        }

        check("[spoke] every part of the ornament scales by the arch's ratio", uniform, null);

        check("[spoke] the spoke is the same width top and bottom",
            Math.Abs(spokeLow - spokeHigh) < 0.001f,
            $"{spokeLow * 2f:0.###} m low, {spokeHigh * 2f:0.###} m high");

        // Half a metre becomes a little over half a metre, not ten.
        check("[spoke] the spoke widens by its share and no more",
            Math.Abs((spokeHigh * 2f) - (1f * ratio)) < 0.001f,
            $"{spokeHigh * 2f:0.###} m against {1f * ratio:0.###} m");

        // A member that really is slung between the legs, ending well short of them, keeps the gap it
        // was drawn with - the distinction this rests on.
        var slung = new List<float3>();
        var slungTriangles = new List<int>();
        for (var step = 0; step < 40; step++)
        {
            var origin = slung.Count;
            slung.Add(new float3(14f, step, 0f));
            slung.Add(new float3(18f, step, 0f));
            slung.Add(new float3(18f, step + 1f, 0f));
            slung.Add(new float3(14f, step + 1f, 0f));
            slungTriangles.AddRange(new[]
            {
                origin, origin + 1, origin + 2, origin, origin + 2, origin + 3,
            });

            origin = slung.Count;
            slung.Add(new float3(-18f, step, 0f));
            slung.Add(new float3(-14f, step, 0f));
            slung.Add(new float3(-14f, step + 1f, 0f));
            slung.Add(new float3(-18f, step + 1f, 0f));
            slungTriangles.AddRange(new[]
            {
                origin, origin + 1, origin + 2, origin, origin + 2, origin + 3,
            });
        }

        var walkwayStart = slung.Count;
        slung.Add(new float3(-6f, 20f, 0f));
        slung.Add(new float3(6f, 20f, 0f));
        slung.Add(new float3(6f, 21f, 0f));
        slung.Add(new float3(-6f, 21f, 0f));
        slungTriangles.AddRange(new[]
        {
            walkwayStart, walkwayStart + 1, walkwayStart + 2,
            walkwayStart, walkwayStart + 2, walkwayStart + 3,
        });

        var slungShape = slung.ToArray();
        var slungOutline = slungTriangles.ToArray();
        var slungProfile = TowerWidening.Profile.Of(
            new[] { slungShape }, new IReadOnlyList<int>?[] { slungOutline });
        var slungMoved = TowerWidening.WidenParts(slungShape, extra, slungProfile);

        check("[spoke] a member that stops short of the legs is scaled against its own end",
            Math.Abs(slungMoved[walkwayStart + 1].x - (6f + d)) < 0.001f,
            $"{slungMoved[walkwayStart + 1].x:0.###} m against {6f + d:0.###} m");
    }

    private static void ALevelOfDetailWidensByOneOfTwoThings(Action<string, bool, string?> check)
    {
        // The measured pair: the part 56.14 m across and the coarse mesh standing in for it 55.44 m,
        // which is what 3.95 m of widening against 4 works out to.
        const float part = 56.14f;
        const float lod = 55.44f;
        const float extra = 4f;
        const float tolerance = 0.01f;

        bool Accepts(float widened) =>
            Math.Abs(widened - extra) <= tolerance
            || Math.Abs(widened - (extra * (lod / part))) <= tolerance;

        check("[pair] a level of detail carried with the part is accepted",
            Accepts(extra), $"{extra:0.###} m");

        check("[pair] a level of detail scaled with the part is accepted",
            Accepts(extra * (lod / part)), $"{extra * (lod / part):0.###} m");

        // The pair from the report: 4 m against 3.95 m, which the proportional invariant covers and
        // the absolute one does not.
        check("[pair] the pair that was reported is one of the two",
            Accepts(3.95f) && Math.Abs(3.95f - extra) > tolerance, "3.95 m");

        // Anything that is neither is still a disagreement worth reporting.
        foreach (var widened in new[] { 0f, 2f, 3.5f, 4.5f, 8f })
        {
            check($"[pair] widening by {widened} m is neither, and is reported",
                !Accepts(widened), $"{widened:0.###} m");
        }

    }

    private static void ALevelOfDetailIsMeasuredWithThePartItStandsFor(Action<string, bool, string?> check)
    {
        float3[] Leg(float inner, float outer, float step, out IReadOnlyList<int> outline)
        {
            var vertices = new List<float3>();
            var triangles = new List<int>();
            for (var y = 0f; y < 40f; y += step)
            {
                foreach (var side in new[] { 1f, -1f })
                {
                    var origin = vertices.Count;
                    vertices.Add(new float3(side * inner, y, 0f));
                    vertices.Add(new float3(side * outer, y, 0f));
                    vertices.Add(new float3(side * outer, y + step, 0f));
                    vertices.Add(new float3(side * inner, y + step, 0f));
                    triangles.AddRange(new[]
                    {
                        origin, origin + 1, origin + 2, origin, origin + 2, origin + 3,
                    });
                }
            }

            outline = triangles;
            return vertices.ToArray();
        }

        // A sheet across the middle, running out to meet the part's inner face, so that the heights
        // the legs stand at have a span reaching past them and material inside it is scaled. Without
        // one everything translates whatever the scope says, and the two readings cannot differ.
        var sheet = new List<float3>();
        var sheetTriangles = new List<int>();
        for (var y = 0f; y < 40f; y += 1f)
        {
            var origin = sheet.Count;
            sheet.Add(new float3(-20f, y, 0f));
            sheet.Add(new float3(20f, y, 0f));
            sheet.Add(new float3(20f, y + 1f, 0f));
            sheet.Add(new float3(-20f, y + 1f, 0f));
            sheetTriangles.AddRange(new[]
            {
                origin, origin + 1, origin + 2, origin, origin + 2, origin + 3,
            });
        }

        // The part, and the same part drawn coarsely - a little different at its faces, a little
        // blockier, as a level of detail is.
        var fine = Leg(20f, 28f, 1f, out var fineOutline);
        var coarse = Leg(19.9f, 27.9f, 10f, out var coarseOutline);
        const float extra = -8f;
        var sheetShape = sheet.ToArray();

        var fineOnly = TowerWidening.Profile.Of(
            new[] { sheetShape, fine },
            new IReadOnlyList<int>?[] { sheetTriangles, fineOutline });
        var together = TowerWidening.Profile.Of(
            new[] { sheetShape, fine, coarse },
            new IReadOnlyList<int>?[] { sheetTriangles, fineOutline, coarseOutline });

        var partWidening = TowerWidening.WidthOf(TowerWidening.WidenParts(fine, extra, together))
            - TowerWidening.WidthOf(fine);
        var lodWidening = TowerWidening.WidthOf(TowerWidening.WidenParts(coarse, extra, together))
            - TowerWidening.WidthOf(coarse);

        check("[lod] the part is widened by the whole of the extra",
            Math.Abs(partWidening - extra) < 0.001f, $"{partWidening:0.###} m");

        check("[lod] and so is the coarse mesh that stands in for it",
            Math.Abs(lodWidening - extra) < 0.001f, $"{lodWidening:0.###} m");

        check("[lod] the two agree, so the bridge is one width at every distance",
            Math.Abs(partWidening - lodWidening) < 0.001f,
            $"{partWidening:0.###} m and {lodWidening:0.###} m");

        // Measured from the fine mesh alone, the coarse leg's inner face sits outside every place that
        // scope calls carried - it is at 19.9 where the fine one is at 20 - so it is scaled while its
        // outer face is carried, and the leg comes out a different depth than it went in. Neither the
        // part nor the level of detail is wrong on its own terms, which is why nothing but a
        // comparison between them finds it.
        var apart = TowerWidening.WidenParts(coarse, extra, fineOnly);
        var withIt = TowerWidening.WidenParts(coarse, extra, together);

        check("[lod] measured without it, the coarse leg comes out a different depth",
            Math.Abs(Depth(apart) - 8f) > 0.01f,
            $"{Depth(apart):0.###} m against the 8 m it went in");

        check("[lod] measured with it, the coarse leg keeps its depth",
            Math.Abs(Depth(withIt) - 8f) < 0.001f, $"{Depth(withIt):0.###} m");

        float Depth(float3[] after)
        {
            var inner = float.MaxValue;
            var outer = 0f;
            for (var index = 0; index < coarse.Length; index++)
            {
                if (coarse[index].x <= 0f) continue;

                inner = Math.Min(inner, after[index].x);
                outer = Math.Max(outer, after[index].x);
            }

            return outer - inner;
        }
    }

    private static void AVerticalMemberIsNotShearedAcrossABand(Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Strip(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // The deck structure at the bottom, spanning the centre out to 16.5 m, tessellated so that it
        // has a vertex on the centre line - which is what makes it read as crossing to a reading that
        // has only the vertices to go on.
        for (var x = -16.5f; x < 16.5f; x += 1.5f) Strip(x, x + 1.5f, 0f, 2f);

        // A railing and a cable plane either side, standing clear of the centre and running the whole
        // height - through the deck structure's band and far above it.
        Strip(16.36f, 16.54f, 0f, 50f);
        Strip(-16.54f, -16.36f, 0f, 50f);
        Strip(14.3f, 14.48f, 0f, 50f);
        Strip(-14.48f, -14.3f, 0f, 50f);

        var shape = vertices.ToArray();
        var outline = triangles.ToArray();
        const float extra = -8f;
        const float d = extra * 0.5f;

        var profile = TowerWidening.Profile.Of(
            new[] { shape }, new IReadOnlyList<int>?[] { outline });
        var moved = TowerWidening.WidenParts(shape, extra, profile);

        // The railing keeps its thickness and its plumb: every vertex of it moved by the same d,
        // bottom and top alike.
        var upright = true;
        var thickness = true;
        for (var index = 0; index < shape.Length; index++)
        {
            var x = shape[index].x;
            if (Math.Abs(Math.Abs(x) - 16.36f) > 0.001f && Math.Abs(Math.Abs(x) - 16.54f) > 0.001f
                && Math.Abs(Math.Abs(x) - 14.3f) > 0.001f && Math.Abs(Math.Abs(x) - 14.48f) > 0.001f)
            {
                continue;
            }

            var wanted = x + (x > 0f ? d : -d);
            if (Math.Abs(moved[index].x - wanted) > 0.0001f) upright = false;
        }

        check("[shear] a member clear of the centre is carried by d at every height",
            upright, null);

        // Which means the gap between the cable plane and the railing is the archetype's, still.
        var gapBefore = 16.36f - 14.48f;
        var railInner = 0f;
        var cableOuter = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].x - 16.36f) < 0.001f && shape[index].y > 45f)
                railInner = moved[index].x;
            if (Math.Abs(shape[index].x - 14.48f) < 0.001f && shape[index].y > 45f)
                cableOuter = moved[index].x;
        }

        check("[shear] and the gap to its neighbour is the one it was drawn with",
            Math.Abs((railInner - cableOuter) - gapBefore) < 0.001f,
            $"{railInner - cableOuter:0.###} m against {gapBefore:0.###} m");

        thickness = Math.Abs((16.54f - 16.36f) - 0.18f) < 0.001f;
        check("[shear] the railing is the 0.18 m one the report named", thickness, null);

        // The deck structure does cross the centre, so it is the thing that stretches - scaled against
        // its own span, its outer end arriving where the members beside it were carried to.
        var deckEnd = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].x - 16.5f) < 0.001f && shape[index].y < 2.5f)
                deckEnd = moved[index].x;
        }

        check("[shear] the deck structure it passes through is still scaled",
            Math.Abs(deckEnd - (16.5f + d)) < 0.001f, $"{deckEnd:0.###} m");

        // Read per height instead, the gap between the railing and the cable plane beside it depends
        // on how high up it is measured: they converge through the crossing band and not above it.
        // That is what closes gaps and merges neighbours, and it is what the report saw as a 0.18 m
        // railing coming out 1.89 m thick - the two of them read as one.
        // Built from the vertices alone: no edges, so no material to appeal to and nothing but the
        // height to go on. This is the reading that sheared the railings.
        var blind = TowerWidening.Profile.Of(shape);
        var perHeight = TowerWidening.WidenParts(shape, extra, blind);
        var gapLow = Gap(perHeight, 0f);
        var gapHigh = Gap(perHeight, 50f);

        check("[shear] asked per height, the gap to the neighbour depends on the height",
            Math.Abs(gapLow - gapHigh) > 0.05f,
            $"{gapLow:0.###} m low against {gapHigh:0.###} m high");

        // Asked of the material, it does not.
        check("[shear] asked of the material, the gap is the same at every height",
            Math.Abs(Gap(moved, 0f) - Gap(moved, 50f)) < 0.001f,
            $"{Gap(moved, 0f):0.###} m and {Gap(moved, 50f):0.###} m");

        float Gap(float3[] after, float height)
        {
            var rail = 0f;
            var cable = 0f;
            for (var index = 0; index < shape.Length; index++)
            {
                if (Math.Abs(shape[index].y - height) > 1.5f) continue;
                if (Math.Abs(shape[index].x - 16.36f) < 0.001f) rail = after[index].x;
                if (Math.Abs(shape[index].x - 14.48f) < 0.001f) cable = after[index].x;
            }

            return rail - cable;
        }
    }

    private static void AThicknessIsMeasuredOnOneThing(Action<string, bool, string?> check)
    {
        var thin = new List<float3>();
        var thinTriangles = new List<int>();
        var fat = new List<float3>();
        var fatTriangles = new List<int>();

        void Quad(List<float3> into, List<int> onto, float leftX, float rightX)
        {
            var origin = into.Count;
            into.Add(new float3(leftX, 0f, 0f));
            into.Add(new float3(rightX, 0f, 0f));
            into.Add(new float3(rightX, 1f, 0f));
            into.Add(new float3(leftX, 1f, 0f));
            onto.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // The piece: a thin plane at each edge, standing clear of the centre.
        Quad(thin, thinTriangles, 16.7f, 17f);
        Quad(thin, thinTriangles, -17f, -16.7f);

        // Its section: the same planes, and an anchorage nine metres deep that only the end piece has.
        Quad(fat, fatTriangles, 8f, 17f);
        Quad(fat, fatTriangles, -17f, -8f);

        var piece = thin.ToArray();
        var outline = thinTriangles.ToArray();
        const float extra = 20f;

        var section = TowerWidening.Profile.Of(
            new[] { piece, fat.ToArray() },
            new IReadOnlyList<int>?[] { outline, fatTriangles.ToArray() });

        var moved = TowerWidening.WidenParts(piece, extra, section);

        var own = TowerWidening.Profile.Of(new[] { piece }, new IReadOnlyList<int>?[] { outline });
        var after = TowerWidening.Profile.Of(new[] { moved }, new IReadOnlyList<int>?[] { outline });

        check("[scale] the piece is carried and keeps its own thickness",
            Math.Abs(after.OuterThicknessAt(0.5f) - own.OuterThicknessAt(0.5f)) < 0.01f,
            $"{own.OuterThicknessAt(0.5f):0.##} m became {after.OuterThicknessAt(0.5f):0.##} m");

        check("[scale] and it moved the whole half, as material clear of the centre does",
            Math.Abs((TowerWidening.WidthOf(moved) - TowerWidening.WidthOf(piece)) - extra) < 0.01f,
            $"{TowerWidening.WidthOf(moved) - TowerWidening.WidthOf(piece):0.##} m");

        // The section is a different measurement of a different thing, and comparing across the two is
        // what raised a defect where there was none.
        check("[scale] the section is thicker there than the piece it holds",
            section.OuterThicknessAt(0.5f) - own.OuterThicknessAt(0.5f) > 1f,
            $"section {section.OuterThicknessAt(0.5f):0.##} m against piece "
            + $"{own.OuterThicknessAt(0.5f):0.##} m");

        // Carried across the centre, the run merges over the middle and there is no material standing
        // clear of it any more: the thickness is not a smaller number, it is no number at all. Reading
        // that as "0 m thick where it went in 20.58 m thick" is what made the check cry scaling at a
        // shape the mapping had carried exactly as it should.
        var deep = new List<float3>();
        var deepTriangles = new List<int>();
        Quad(deep, deepTriangles, 4.42f, 25f);
        Quad(deep, deepTriangles, -25f, -4.42f);

        var leg = deep.ToArray();
        var legOutline = deepTriangles.ToArray();
        var legBefore = TowerWidening.Profile.Of(
            new[] { leg }, new IReadOnlyList<int>?[] { legOutline });
        var legMoved = TowerWidening.WidenParts(leg, -9f, legBefore);
        var legAfter = TowerWidening.Profile.Of(
            new[] { legMoved }, new IReadOnlyList<int>?[] { legOutline });

        check("[scale] before the move the leg stands clear of the centre",
            legBefore.OuterThicknessAt(0.5f) > 20f, $"{legBefore.OuterThicknessAt(0.5f):0.##} m");

        check("[scale] carried across it, nothing stands clear of the centre to measure",
            legAfter.OuterThicknessAt(0.5f) <= 0.01f, $"{legAfter.OuterThicknessAt(0.5f):0.##} m");

        // And it kept its shape while doing so, which is the thing the check exists to protect.
        var kept = true;
        for (var index = 0; index < leg.Length; index++)
        {
            var wanted = leg[index].x + (leg[index].x > 0f ? -4.5f : 4.5f);
            if (Math.Abs(legMoved[index].x - wanted) > 0.0001f) kept = false;
        }

        check("[scale] the carried leg is x + sgn(x) * d throughout", kept, null);
    }

    private static void AnOpenworkOrnamentStretchesBetweenTheLegs(Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Quad(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // Two legs standing from 0 to 60, inner face at 16, outer at 22.
        for (var step = 0; step < 60; step++)
        {
            Quad(16f, 22f, step, step + 1f);
            Quad(-22f, -16f, step, step + 1f);
        }

        // The ornament, between 30 and 40: an arch across the centre, and ribs above it with air
        // between them. The ribs reach the legs; most heights through the ribs have a hole on the
        // centre line.
        for (var x = -16f; x < 16f; x += 1f) Quad(x, x + 1f, 30f, 31f);
        for (var rib = -15; rib <= 15; rib += 3)
        {
            Quad(rib, rib + 1f, 31f, 40f);
        }

        var shape = vertices.ToArray();
        var outline = triangles.ToArray();
        const float extra = 12f;
        const float shift = extra * 0.5f;

        var profile = TowerWidening.Profile.Of(
            new[] { shape }, new IReadOnlyList<int>?[] { outline });
        var moved = TowerWidening.WidenParts(shape, extra, profile);

        // The legs are carried: both faces move the whole half and the leg keeps its thickness.
        var legInner = 0f;
        var legOuter = 0f;
        var ribAtTop = 0f;
        var archEnd = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].x - 16f) < 0.001f && shape[index].y > 50f) legInner = moved[index].x;
            if (Math.Abs(shape[index].x - 22f) < 0.001f && shape[index].y > 50f) legOuter = moved[index].x;
            if (Math.Abs(shape[index].x - 15f) < 0.001f && shape[index].y > 39f) ribAtTop = moved[index].x;
            if (Math.Abs(shape[index].x - 16f) < 0.001f && Math.Abs(shape[index].y - 31f) < 0.001f)
                archEnd = moved[index].x;
        }

        check("[fan] the legs are carried and keep their thickness",
            Math.Abs((legInner - 16f) - shift) < 0.001f && Math.Abs((legOuter - 22f) - shift) < 0.001f,
            $"inner moved {legInner - 16f:0.##}, outer {legOuter - 22f:0.##}");

        // The arch reaches the leg it was drawn against, at the leg's new place.
        check("[fan] the arch arrives at the leg's inner face",
            Math.Abs(archEnd - legInner) < 0.001f, $"{archEnd:0.##} against {legInner:0.##}");

        // A rib high in the ornament, where the centre line is a hole, is stretched with the rest of
        // the ornament rather than carried off on its own. Carried, it would move the whole half.
        var ribShift = ribAtTop - 15f;
        check("[fan] a rib over a hole is stretched, not carried away",
            ribShift > 0f && ribShift < shift - 0.05f,
            $"moved {ribShift:0.##} m where carrying it would move {shift:0.##} m");

        // And it is stretched by the ornament's own ratio, which is the arch's.
        check("[fan] every rib is stretched by the same ratio as the arch",
            Math.Abs(ribAtTop - (15f * (16f + shift) / 16f)) < 0.001f,
            $"{ribAtTop:0.####} against {15f * (16f + shift) / 16f:0.####}");

        // Read from the vertices alone - no edges, so no runs, no legs, nothing to appeal to - the rib
        // lands somewhere else entirely: scaled against how far the whole shape reaches rather than
        // against the leg it belongs to. Every rib gets its own answer that way and the fan comes
        // apart, which is what the bridge showed.
        var blind = TowerWidening.WidenParts(shape, extra, TowerWidening.Profile.Of(shape));
        var blindRib = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].x - 15f) < 0.001f && shape[index].y > 39f) blindRib = blind[index].x;
        }

        check("[fan] without the edges to read, the rib lands somewhere else",
            Math.Abs(blindRib - ribAtTop) > 0.5f,
            $"{blindRib:0.##} against {ribAtTop:0.##}");
    }

    private static void AWingKeepsItsDepthOrSomethingSaysSo(Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Quad(float leftX, float rightX)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, 0f, 0f));
            vertices.Add(new float3(rightX, 0f, 0f));
            vertices.Add(new float3(rightX, 1f, 0f));
            vertices.Add(new float3(leftX, 1f, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // A base from -20 to 20 with a wing either side, separated from it by a gap.
        for (var x = -20f; x < 20f; x += 1f) Quad(x, x + 1f);
        for (var x = 20.43f; x < 28.07f; x += 1f) Quad(x, Math.Min(x + 1f, 28.07f));
        for (var x = -28.07f; x < -20.43f; x += 1f) Quad(x, Math.Min(x + 1f, -20.43f));

        var gapped = vertices.ToArray();
        var outline = triangles.ToArray();
        const float extra = 24f;

        var before = TowerWidening.Profile.Of(
            new[] { gapped }, new IReadOnlyList<int>?[] { outline });
        var moved = TowerWidening.WidenParts(gapped, extra, before);
        var afterProfile = TowerWidening.Profile.Of(
            new[] { moved }, new IReadOnlyList<int>?[] { outline });

        var wasThick = before.OuterThicknessAt(0.5f);
        var nowThick = afterProfile.OuterThicknessAt(0.5f);

        check("[wing] a wing clear of the base keeps its depth",
            Math.Abs(nowThick - wasThick) < 0.05f && wasThick > 5f,
            $"{wasThick:0.##} m became {nowThick:0.##} m");

        // And it moved out by the whole half, like the leg it is.
        var outerBefore = TowerWidening.WidthOf(gapped) * 0.5f;
        var outerAfter = TowerWidening.WidthOf(moved) * 0.5f;
        check("[wing] a wing clear of the base moves the whole half",
            Math.Abs((outerAfter - outerBefore) - (extra * 0.5f)) < 0.01f,
            $"{outerAfter - outerBefore:0.##} m");

        // Welded to the base instead, the wing is part of what spans the centre and is scaled with it.
        // The width comes out identical, which is exactly why the thickness had to be measured.
        vertices.Clear();
        triangles.Clear();
        for (var x = -28f; x < 28f; x += 1f) Quad(x, x + 1f);
        var welded = vertices.ToArray();
        var weldedOutline = triangles.ToArray();

        var weldedBefore = TowerWidening.Profile.Of(
            new[] { welded }, new IReadOnlyList<int>?[] { weldedOutline });
        var weldedMoved = TowerWidening.WidenParts(welded, extra, weldedBefore);
        var weldedAfter = TowerWidening.Profile.Of(
            new[] { weldedMoved }, new IReadOnlyList<int>?[] { weldedOutline });

        // Welded, there is no material standing clear of the centre at all: the whole half is one run
            // reaching the centre, which is the spanning member itself. It has no thickness to keep,
            // and saying it lost one is how the V pylon.s top came to be reported as a leg that had
            // been scaled when it was only being brought in.
        check("[wing] welded to the base, nothing stands clear of the centre to have a thickness",
            weldedBefore.OuterThicknessAt(0.5f) <= 0.01f && weldedAfter.OuterThicknessAt(0.5f) <= 0.01f,
            $"{weldedBefore.OuterThicknessAt(0.5f):0.##} and {weldedAfter.OuterThicknessAt(0.5f):0.##}");

        // And the gapped wing does have one, which is the discrimination that matters.
        check("[wing] the gapped wing has a thickness to keep",
            before.OuterThicknessAt(0.5f) > 5f, $"{before.OuterThicknessAt(0.5f):0.##} m");

        check("[wing] and its width is the same either way, which is why width cannot say",
            Math.Abs(TowerWidening.WidthOf(weldedMoved) - (56f + extra)) < 0.05f
                && Math.Abs(TowerWidening.WidthOf(moved) - (56.14f + extra)) < 0.05f,
            $"{TowerWidening.WidthOf(weldedMoved):0.##} and {TowerWidening.WidthOf(moved):0.##}");
    }

    private static void TheSecondDeckGoesUnderTheRoad(Action<string, bool, string?> check)
    {
        // The V pylon: its train track hangs ten metres below, so the road being converted is the
        // main net, as it has always been.
        var below = DeckArrangement.For(-10f);
        check("[decks] a second net hung below leaves the road as the bridge",
            !below.SecondNetAbove && !below.MainIsChosenDeck, $"{below.Offset:0.##}");

        check("[decks] a second net hung below is carried below",
            !below.CarriedIsAbove, null);

        // The A pylon: "ExtradosedBridge02 Above Road", ten metres up. Its own main net is the lower
        // deck, so the deck the player chose goes there and their road is carried above it.
        var above = DeckArrangement.For(10f);
        check("[decks] a second net hung above makes the chosen deck the bridge",
            above.SecondNetAbove && above.MainIsChosenDeck, $"{above.Offset:0.##}");

        check("[decks] a second net hung above carries the converted road above",
            above.CarriedIsAbove, null);

        // Pointer identity is the contract, not only the two booleans above. In particular, the lower
        // pointer may name a TrackPrefab: it still becomes the root/main network and the upper road is
        // passed as AuxiliaryNets.m_Prefab.
        var upperPointer = new object();
        var lowerPointer = new object();
        var aPylon = above.Arrange(upperPointer, lowerPointer);
        check("[decks] the A pylon exchanges the two prefab pointers",
            ReferenceEquals(aPylon.Main, lowerPointer)
                && ReferenceEquals(aPylon.Auxiliary, upperPointer), null);

        var vPylon = below.Arrange(upperPointer, lowerPointer);
        check("[decks] a lower auxiliary keeps the original prefab pointers",
            ReferenceEquals(vPylon.Main, upperPointer)
                && ReferenceEquals(vPylon.Auxiliary, lowerPointer), null);

        // The offset is the archetype's and is used as it stands. Turning it over was the earlier
        // answer and needed the structure dropped to compensate; putting the decks in the
        // archetype's own roles needs nothing moved at all.
        check("[decks] the archetype's offset is kept, not turned over",
            Math.Abs(above.Offset - 10f) < 0.001f && Math.Abs(below.Offset + 10f) < 0.001f,
            $"{above.Offset:0.##} and {below.Offset:0.##}");

        // A separation of zero is one deck, not two, and puts nothing in the other slot.
        var none = DeckArrangement.For(0f);
        check("[decks] no separation is not an arrangement at all",
            Math.Abs(none.Offset) < 0.001f && !none.SecondNetAbove && !none.MainIsChosenDeck, null);

        foreach (var separation in new[] { 4f, 10f, 24f })
        {
            var hungAbove = DeckArrangement.For(separation);
            var hungBelow = DeckArrangement.For(-separation);

            check($"[decks] at {separation} m the decks stay the archetype's distance apart",
                Math.Abs(Math.Abs(hungAbove.Offset) - separation) < 0.001f
                    && Math.Abs(Math.Abs(hungBelow.Offset) - separation) < 0.001f, null);

            check($"[decks] at {separation} m only the one hung above swaps the two inputs",
                hungAbove.MainIsChosenDeck && !hungBelow.MainIsChosenDeck, null);
        }
    }

    /// <summary>
    /// A V-shaped double-deck bridge is widened from its prototype upper road, never from its portal
    /// opening and never from the road or track selected for the lower level.
    /// </summary>
    private static void ADoubleDeckBridgeIsSizedFromItsPrototypeUpperDeck(
        Action<string, bool, string?> check)
    {
        const float prototypeUpper = 40f;
        const float prototypeOpening = 39.31641f;

        var ownExtra = PrototypeBridgeSizing.UpperDeckExtra(40f, prototypeUpper, -1f);
        check("[upper width] the V pylon is unchanged on its own upper road",
            Math.Abs(ownExtra) < 0.001f,
            $"{ownExtra:0.#####} m");

        var narrowStructureExtra = BridgeTowers.StructureExtraFor("Extradosed01",
            PrototypeBridgeSizing.UpperDeckExtra(16f, prototypeUpper, -1f));
        check("[upper width] the double-deck V structure adds its 20 m prototype allowance",
            Math.Abs(narrowStructureExtra + 4f) < 0.001f,
            $"{narrowStructureExtra:0.#####} m");

        // The real prototype's narrowest node opening is 20.09 m. Contracting by the raw -24 m
        // reverses its left and right coordinates; the effective -4 m keeps a positive 16.09 m
        // opening, so the node pieces still meet in their authored order.
        const float nodeHalfOpening = 20.09f * 0.5f;
        var nodeLeft = TowerWidening.Spread(-nodeHalfOpening, narrowStructureExtra);
        var nodeRight = TowerWidening.Spread(nodeHalfOpening, narrowStructureExtra);
        check("[upper width] the V bridge node does not cross through the centre",
            nodeLeft < 0f && nodeRight > 0f
                && Math.Abs((nodeRight - nodeLeft) - 16.09f) < 0.001f,
            $"{nodeLeft:0.#####}..{nodeRight:0.#####} m");

        var wideExtra = PrototypeBridgeSizing.UpperDeckExtra(64f, prototypeUpper, -1f);
        check("[upper width] a wide V bridge is derived from the prototype upper road",
            Math.Abs(wideExtra - 24f) < 0.001f,
            $"{wideExtra:0.#####} m");

        var wrongOpeningExtra = 64f - prototypeOpening;
        check("[upper width] the portal opening cannot replace the upper road width",
            Math.Abs(wideExtra - wrongOpeningExtra) > 0.5f,
            $"road gives {wideExtra:0.#####}, opening would give {wrongOpeningExtra:0.#####} m");

        var fallback = PrototypeBridgeSizing.UpperDeckExtra(64f, 0f, 24f);
        check("[upper width] a missing prototype measurement keeps the recorded widening",
            Math.Abs(fallback - 24f) < 0.001f, $"{fallback:0.#####} m");
    }

    private static void ADeckBetweenTheLegsStretchesToMeetThem(Action<string, bool, string?> check)
    {
        // Two legs from 22 to 26, standing 40 m tall, and a deck spanning the middle at 20 m up.
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Quad(float leftX, float rightX, float lowY, float highY)
        {
            var origin = vertices.Count;
            vertices.Add(new float3(leftX, lowY, 0f));
            vertices.Add(new float3(rightX, lowY, 0f));
            vertices.Add(new float3(rightX, highY, 0f));
            vertices.Add(new float3(leftX, highY, 0f));
            triangles.AddRange(new[] { origin, origin + 1, origin + 2, origin, origin + 2, origin + 3 });
        }

        // The legs, in one metre courses, so they have vertices at the deck's height the way a real
        // mesh does. Coarser than the deck they would miss its height entirely and the question would
        // never arise.
        for (var step = 0; step < 40; step++)
        {
            Quad(22f, 26f, step, step + 1f);
            Quad(-26f, -22f, step, step + 1f);
        }

        // The deck, spanning the centre and stopping well short of the legs. Tessellated the way a
        // real mesh is, so it has vertices across its width and one on the centre line - which is
        // what makes the vertices alone report it as crossing, without saying where it ends.
        for (var x = -12f; x < 12f; x += 1f) Quad(x, x + 1f, 20f, 21f);

        var shape = vertices.ToArray();

        foreach (var extra in new[] { 0f, 6f, 18f })
        {
            var shift = extra * 0.5f;
            var profile = TowerWidening.Profile.Of(new[] { shape }, new IReadOnlyList<int>?[] { triangles });
            var moved = TowerWidening.WidenParts(shape, extra, profile);

            var deckEnd = 0f;
            var legOuter = 0f;
            var legInner = float.MaxValue;
            for (var index = 0; index < shape.Length; index++)
            {
                if (Math.Abs(shape[index].x - 12f) < 0.001f) deckEnd = moved[index].x;
                if (Math.Abs(shape[index].x - 26f) < 0.001f) legOuter = moved[index].x;
                if (Math.Abs(shape[index].x - 22f) < 0.001f) legInner = Math.Min(legInner, moved[index].x);
            }

            check($"[deck] at {extra} m the deck's end moves as far as the legs do",
                Math.Abs((deckEnd - 12f) - shift) < 0.001f,
                $"deck moved {deckEnd - 12f:0.####} m, legs {shift:0.####} m");

            check($"[deck] at {extra} m the legs are carried, not scaled",
                Math.Abs((legOuter - 26f) - shift) < 0.001f
                    && Math.Abs((legInner - 22f) - shift) < 0.001f,
                $"outer {legOuter - 26f:0.####}, inner {legInner - 22f:0.####}");

            check($"[deck] at {extra} m the gap between deck and leg is what it was",
                Math.Abs((legInner - deckEnd) - 10f) < 0.001f,
                $"{legInner - deckEnd:0.####} m against 10 m");
        }

        // What reading the widest thing at that height instead gives, which is what tore it: the deck
        // is scaled against the legs' reach and its ends fall short of them by more at every width.
        var byReach = TowerWidening.WidenParts(shape, 18f, TowerWidening.Profile.Of(new[] { shape }));
        var short_end = 0f;
        for (var index = 0; index < shape.Length; index++)
        {
            if (Math.Abs(shape[index].x - 12f) < 0.001f) short_end = byReach[index].x;
        }

        check("[deck] read without the triangles the deck falls short",
            short_end < 12f + 9f - 1f, $"moved {short_end - 12f:0.####} m, wanted 9 m");
    }

    private static void ASlantedLegIsCarriedOutAtEveryHeight(Action<string, bool, string?> check)
    {
        // A V: legs from +-2 at the bottom to +-18 at the top, over 40 m of height.
        var vee = new List<float3>();
        for (var step = 0; step <= 40; step++)
        {
            var y = step;
            var x = 2f + (step / 40f * 16f);
            vee.Add(new float3(x, y, 0f));
            vee.Add(new float3(-x, y, 0f));
        }

        var source = vee.ToArray();
        const float extra = 6f;
        var widened = TowerWidening.WidenParts(source, extra);

        var rigid = true;
        var worst = 0f;
        for (var index = 0; index < source.Length; index++)
        {
            var moved = Math.Abs(widened[index].x) - Math.Abs(source[index].x);
            worst = Math.Max(worst, Math.Abs(moved - (extra * 0.5f)));
            if (Math.Abs(moved - (extra * 0.5f)) > 0.001f) rigid = false;
        }

        check("[vee] every point of a slanted leg moves by the same half",
            rigid, $"worst departure {worst:0.####} m");

        // The apex is the narrowest part of the V and the place a whole shape boundary would have
        // scaled hardest. It moves out by the same half as the top.
        var apex = Math.Abs(widened[0].x) - Math.Abs(source[0].x);
        check("[vee] the apex moves out as far as the top",
            Math.Abs(apex - (extra * 0.5f)) < 0.001f, $"{apex:0.####} m");

        // The whole shape question, kept here to show what it answers and why it is wrong: the top's
        // 36 m gap gives a boundary of 18, which is outside the legs at every height below the top.
        check("[vee] one boundary for the whole shape would sit outside the legs",
            TowerWidening.ClearSpanOf(source, TowerWidening.SpanBands) * 0.5f > 17f,
            $"{TowerWidening.ClearSpanOf(source, TowerWidening.SpanBands) * 0.5f:0.####} m "
            + "against legs that begin at 2 m");

        // An A pylon is a V upside down and must behave the same way.
        var apex2 = new List<float3>();
        for (var step = 0; step <= 40; step++)
        {
            var x = 18f - (step / 40f * 16f);
            apex2.Add(new float3(x, step, 0f));
            apex2.Add(new float3(-x, step, 0f));
        }

        var aShape = apex2.ToArray();
        var aWidened = TowerWidening.WidenParts(aShape, extra);
        var aRigid = true;
        for (var index = 0; index < aShape.Length; index++)
        {
            var moved = Math.Abs(aWidened[index].x) - Math.Abs(aShape[index].x);
            if (Math.Abs(moved - (extra * 0.5f)) > 0.001f) aRigid = false;
        }

        check("[vee] an A pylon's legs are carried out the same way", aRigid, null);

        // A crossbeam between the legs still stretches, because at its height the shape crosses the
        // centre. Taking the narrowest gap for the whole shape - the other way to get the V right -
        // would have scaled the legs instead.
        var braced = new List<float3>(vee);
        for (var x = -10f; x <= 10f; x += 1f) braced.Add(new float3(x, 20f, 0f));
        var bracedWidened = TowerWidening.WidenParts(braced.ToArray(), extra);

        var beamEnd = 0f;
        var beamInner = 0f;
        for (var index = 0; index < braced.Count; index++)
        {
            if (Math.Abs(braced[index].y - 20f) > 0.001f) continue;
            beamEnd = Math.Max(beamEnd, bracedWidened[index].x);
            if (Math.Abs(braced[index].x - 1f) < 0.001f) beamInner = bracedWidened[index].x;
        }

        // The end has to reach 13 to meet the leg. What separates stretching from translating is the
        // material between: a metre in from the centre belongs to a beam that crosses it, so it moves
        // by its share of the stretch and not by the whole half. Asking the shape once instead of per
        // height reads the beam's own nearest vertex as the opening and carries this out to 4.
        check("[vee] a crossbeam still stretches while the legs it joins do not",
            Math.Abs(beamEnd - 13f) < 0.001f && Math.Abs(beamInner - 1.3f) < 0.001f,
            $"beam reaches {beamEnd:0.####}, its inner metre sits at {beamInner:0.####}");

        // Zero extra is the shape it came from, per height band or not.
        var unchanged = TowerWidening.WidenParts(source, 0f);
        var same = true;
        for (var index = 0; index < source.Length; index++)
        {
            if (Math.Abs(unchanged[index].x - source[index].x) > 0.0001f) same = false;
        }

        check("[vee] no extra width moves no vertex", same, null);
    }

    /// <summary>
    /// Stretch or translate is decided by one thing: does the part cross the bridge's centre.
    ///
    /// The rule this replaces split by vertex position - beyond half the road a vertex moved rigidly,
    /// inside it a vertex scaled. Half the road is a guess about where the legs begin, and where the
    /// guess fell inside a leg the leg was cut in two: its outer portion carried across, its inner
    /// portion scaled, and the column came out a splayed slab. Nothing reported it, because the outer
    /// edge still landed exactly where it belonged and every measured width was right.
    ///
    /// The centre line is not a guess. A crossbeam spans it by construction and a leg cannot.
    /// </summary>
    private static void StretchOrTranslateIsDecidedByCrossingTheCentre(Action<string, bool, string?> check)
    {
        // A portal, as one mesh: two legs that do not cross the centre, and a crossbeam that does.
        // The legs are deliberately thick - inner face at 11, outer at 17 - so that half of a 24 m
        // road falls inside them. That is the case the old rule got wrong.
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Quad(float x0, float x1, float y0, float y1)
        {
            var at = vertices.Count;
            vertices.Add(new float3(x0, y0, 0f));
            vertices.Add(new float3(x1, y0, 0f));
            vertices.Add(new float3(x1, y1, 0f));
            vertices.Add(new float3(x0, y1, 0f));
            triangles.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
        }

        Quad(-17f, -11f, 0f, 40f);   // left leg, 6 m thick, wholly left of centre
        Quad(11f, 17f, 0f, 40f);     // right leg
        Quad(-11f, 11f, 30f, 34f);   // crossbeam, spanning the centre

        var source = vertices.ToArray();
        var moved = TowerWidening.WidenParts(source, triangles, 16f);

        // The legs: carried out by half the extra and not deformed. Thickness is the property that
        // the old rule destroyed, so it is what this checks.
        var leftInner = moved[0].x;
        var leftOuter = moved[1].x;
        check("[parts] a leg is carried out by half the extra",
            Math.Abs(moved[0].x + 25f) < 0.001f && Math.Abs(moved[1].x + 19f) < 0.001f,
            $"{moved[0].x:0.####}..{moved[1].x:0.####}");

        check("[parts] a leg keeps its thickness",
            Math.Abs(Math.Abs(leftOuter - leftInner) - 6f) < 0.001f,
            $"{Math.Abs(leftOuter - leftInner):0.####} m thick");

        check("[parts] the other leg too",
            Math.Abs(moved[4].x - 19f) < 0.001f && Math.Abs(moved[5].x - 25f) < 0.001f,
            $"{moved[4].x:0.####}..{moved[5].x:0.####}");

        // The crossbeam: scaled about the centre so its ends land on the legs, which moved to 19.
        check("[parts] a crossbeam is stretched to reach the legs",
            Math.Abs(moved[8].x + 19f) < 0.001f && Math.Abs(moved[9].x - 19f) < 0.001f,
            $"{moved[8].x:0.####}..{moved[9].x:0.####}");

        // Both branches move the outer edge the same distance, so the tower's width is right either way
        // - which is why the fault was invisible in every measurement.
        check("[parts] the tower's outer edge moves half the extra",
            Math.Abs((TowerWidening.WidthOf(moved) - TowerWidening.WidthOf(source)) - 16f) < 0.001f,
            null);

        // What the old rule did to the same portal: the boundary at half a 24 m road is 12, which cuts
        // through a leg that runs from 11 to 17. The inner face scales and the outer translates.
        var byThreshold = TowerWidening.Widen(source, 16f, inner: 12f);
        var oldThickness = Math.Abs(byThreshold[1].x - byThreshold[0].x);
        check("[parts] the boundary rule splayed a leg it cut through",
            oldThickness > 6.5f,
            $"{oldThickness:0.####} m thick, drawn from 6 m");

        check("[parts] and its outer edge was right anyway, which is why nothing reported it",
            Math.Abs((TowerWidening.WidthOf(byThreshold) - TowerWidening.WidthOf(source)) - 16f) < 0.001f,
            null);

        // A part that only reaches the centre counts as crossing: translating it would open a gap
        // against the part mirroring it.
        var half = new[] { new float3(0f, 0f, 0f), new float3(16f, 0f, 0f), new float3(16f, 2f, 0f) };
        var halfMoved = TowerWidening.WidenParts(half, new[] { 0, 1, 2 }, 16f);
        check("[parts] a part touching the centre stays on it",
            Math.Abs(halfMoved[0].x) < 0.001f, $"{halfMoved[0].x:0.####}");
        check("[parts] and its far end still moves half the extra",
            Math.Abs(halfMoved[1].x - 24f) < 0.001f, $"{halfMoved[1].x:0.####}");

        // At zero extra the mesh comes back vertex for vertex, which is what makes derived mean
        // something.
        var identical = TowerWidening.WidenParts(source, triangles, 0f);
        var same = true;
        for (var index = 0; index < source.Length; index++)
        {
            if (Math.Abs(identical[index].x - source[index].x) > 0.0001f) same = false;
        }

        check("[parts] at zero extra the mesh is unchanged", same, null);
    }

    /// <summary>
    /// A tower is sized against its cables only where that has been measured, and only where the
    /// section it would be measured to encloses the road.
    ///
    /// The distances were first held as three constants and applied to every tower that had an
    /// overhead section. Six of the game's families have one and only two of those are the envelope
    /// the road runs between: an extradosed bridge fans its cables from a low pylon over the deck and
    /// its section is 21 m against roads of 31 and 61, a lift bridge's section is its lifting
    /// mechanism, the grand bridge's is a stiffening truss. Sizing a tower to stand 3.53745 m outside
    /// one of those is sizing it against a suspension bridge it has nothing to do with.
    ///
    /// So two conditions, and each rules out a different mistake. The section must be recorded as the
    /// outer envelope, and the distances must have been measured on this tower. Where either is
    /// missing the road rule is used, which is what every tower used before any of this was measured.
    /// </summary>
    private static void OnlyMeasuredFamiliesAreSizedAgainstTheirCables(Action<string, bool, string?> check)
    {
        check("[family] the five lane suspension tower has measured distances",
            BridgeCables.SizingSpacingFor("5LaneSuspensionBridgePillar Placeholder") != null, null);
        check("[family] and the four lane, which is what made them a design",
            BridgeCables.SizingSpacingFor("4LaneSuspensionBridgePillar Placeholder") != null, null);

        // Outer envelope, but never dumped. Almost certainly the same numbers is not measured.
        foreach (var tower in new[]
        {
            "2LaneSuspensionBridgePillar Placeholder",
            "3LaneSuspensionBridgePillar Placeholder",
            "8LaneCableStayedBridgePillar Placeholder",
            "PedestrianBridgeCableStayedPillar Placeholder",
        })
        {
            check($"[family] {tower} is sized by the road until its distances are measured",
                BridgeCables.SizingSpacingFor(tower) == null, null);
        }

        // Not an envelope at all - there is no distance of this kind to measure.
        foreach (var tower in new[]
        {
            "ExtradosedBridge01NetPillar",
            "ExtradosedBridge02NetPillar",
            "ExtradosedBridge03NetPillar",
            "GrandBridgePylon Placeholder",
            "LiftBridge01",
            "LiftBridge03",
            "TrussArchBridge01NetPillar",
            "TrussArchBridge03NetPillar",
            "SuspensionBridge03NetPylon",
        })
        {
            check($"[family] {tower} carries no envelope, so it is sized by the road",
                BridgeCables.SizingSpacingFor(tower) == null, null);
        }

        check("[family] a tower nobody measured is sized by the road",
            BridgeCables.SizingSpacingFor("no such tower") == null, null);

        // Consistency: nothing may hold distances to a section that is not an envelope, or the two
        // conditions would disagree and the stricter one would be silently carrying the other.
        foreach (var cables in BridgeCables.All)
        {
            if (cables.Outer) continue;

            check($"[family] {cables.Tower} holds no distance to a section that encloses nothing",
                BridgeCables.SpacingFor(cables.Tower) == null, null);
        }

        // And every tower with distances has a section recorded as the envelope, so the gate cannot be
        // passed by one condition alone.
        foreach (var cables in BridgeCables.All)
        {
            if (BridgeCables.SpacingFor(cables.Tower) == null) continue;

            check($"[family] {cables.Tower} measures its distance to an envelope",
                cables.Outer && BridgeCables.SizingSpacingFor(cables.Tower) != null, null);
        }
    }

    /// <summary>
    /// What is not generated, and why each is refused rather than attempted.
    ///
    /// A bridge that is not generated is a bridge the player still has. A bridge generated from an
    /// arrangement nobody has measured is one that looks built and behaves as something else, and it
    /// costs a round to find out which.
    /// </summary>
    private static void WhatIsNotGeneratedIsRefusedRatherThanAttempted(Action<string, bool, string?> check)
    {
        // Bridge Expansion Pack content: folded into the base game, so every one of these has a
        // vanilla twin. Deriving from the pack's copy binds a generated bridge to an asset that can be
        // uninstalled while the identical vanilla one cannot.
        foreach (var name in new[]
        {
            "BXP Suspension Bridge - Highway Twoway - 6 Lanes",
            "BXP Double Deck Suspension Bridge - Train",
            "BXP SuspensionBridge03 - Six Lane Highway",
            "BXP Golden Gate Bridge Train Track",
        })
        {
            check($"[scope] '{name}' is superseded by the base game",
                BridgeStyleDefinitions.IsSupersededPack(name), null);
        }

        foreach (var name in new[]
        {
            "Suspension Bridge - Highway Oneway - 5 Lanes",
            "SuspensionBridge03",
            "8-Lane Cable Stayed Bridge 00",
            "BXPSomethingWithoutTheSpace",
        })
        {
            check($"[scope] '{name}' is not pack content", !BridgeStyleDefinitions.IsSupersededPack(name), null);
        }

        check("[scope] nothing is not pack content", !BridgeStyleDefinitions.IsSupersededPack(null), null);

        // Being pack content is not on its own a reason to skip it: the reason is duplication. The
        // catalogue pairs this test with what the prefab can do, and keeps a pack bridge that offers
        // something the base game has no archetype for. Every double deck suspension bridge installed
        // is the pack's - the game's own suspension bridges are all single deck - so excluding those
        // as well removed the only archetype there is for two decks, and rule 11 then refused to build
        // one, correctly and for a reason the exclusion had created.
        check("[scope] the test is by name alone, so the caller can weigh capability against it",
            BridgeStyleDefinitions.IsSupersededPack("BXP Double Deck Suspension Bridge - Highway"), null);

        // Deferred: the deck is the mechanism, and widening a machine whose parts must keep meeting
        // each other through their whole travel is a different problem from widening a portal.
        foreach (var style in new[] { "Draw", "PedestrianDraw", "Lift" })
        {
            check($"[scope] {style} is deferred with a reason",
                !string.IsNullOrEmpty(BridgeStyleDefinitions.DeferredReason(style)), null);
        }

        foreach (var style in new[] { "Suspension", "SuspensionGolden", "CableStayed", "Extradosed03",
            "TrussArch", "TrussArch01", "TrussArch03", "TiedArch", "Grand", "CoveredWood" })
        {
            check($"[scope] {style} is generated", BridgeStyleDefinitions.DeferredReason(style) == null, null);
        }

        check("[scope] an unknown style is not deferred",
            BridgeStyleDefinitions.DeferredReason("no such style") == null, null);
        check("[scope] nothing is not deferred", BridgeStyleDefinitions.DeferredReason(null) == null, null);

        // Every deferred style is one the definitions list, or the refusal names something the player
        // could not have chosen in the first place.
        foreach (var style in new[] { "Draw", "PedestrianDraw", "Lift" })
        {
            check($"[scope] {style} is a style the player can pick",
                BridgeStyleDefinitions.All.Any(definition => definition.Id == style), null);
        }
    }

    /// <summary>
    /// A portal welded into one mesh keeps its legs, which is the case the component rule could not
    /// see.
    ///
    /// Asking the question of connected components is the right question and the wrong unit. A real
    /// portal's legs are joined to each other through its crossbeams, so the whole tower is one
    /// component; it does cross the centre, so the whole tower was scaled, and scaling thickens or
    /// thins the legs in proportion. Rule 5 says a tower is never scaled and this scaled every one of
    /// them - on the golden bridge, whose extra width is negative, it pulled the two legs toward each
    /// other until the portal read as a single column standing in the road.
    ///
    /// The boundary is measured from the shape instead: the widest gap it leaves open across the
    /// centre line is where the legs begin, whatever their thickness and whatever road the tower was
    /// drawn for.
    /// </summary>
    private static void AWeldedPortalKeepsItsLegs(Action<string, bool, string?> check)
    {
        // Legs from x = 卤6 to 卤8 over the full height, a crossbeam at one height, and triangles that
        // join the three into one connected component - which is what a modelled portal is.
        var vertices = new List<float3>();
        var triangles = new List<int>();

        void Quad(float x0, float x1, float y0, float y1)
        {
            var at = vertices.Count;
            vertices.Add(new float3(x0, y0, 0f));
            vertices.Add(new float3(x1, y0, 0f));
            vertices.Add(new float3(x1, y1, 0f));
            vertices.Add(new float3(x0, y1, 0f));
            triangles.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
        }

        Quad(-8f, -6f, 0f, 40f);     // 0..3   left leg
        Quad(6f, 8f, 0f, 40f);       // 4..7   right leg
        Quad(-6f, 6f, 30f, 34f);     // 8..11  crossbeam

        // Welded: the crossbeam's ends share triangles with the legs it meets.
        triangles.AddRange(new[] { 1, 8, 11, 5, 9, 10 });

        var source = vertices.ToArray();
        var moved = TowerWidening.WidenParts(source, triangles, 16f);

        // The unit the component rule would have used: one component spanning the whole portal, so it
        // would have scaled all of this, legs included.
        check("[welded] the whole portal is one span across the centre",
            Math.Abs(TowerWidening.WidthOf(source) - 16f) < 0.001f
            && Math.Abs(TowerWidening.ClearSpanOf(source, TowerWidening.SpanBands) - 12f) < 0.001f,
            $"width {TowerWidening.WidthOf(source):0.##}, clear span "
            + $"{TowerWidening.ClearSpanOf(source, TowerWidening.SpanBands):0.##}");

        // The legs: carried out by half the extra, thickness untouched.
        check("[welded] the left leg is carried out whole",
            Math.Abs(moved[0].x + 16f) < 0.001f && Math.Abs(moved[1].x + 14f) < 0.001f,
            $"{moved[0].x:0.####}..{moved[1].x:0.####}");
        check("[welded] the right leg too",
            Math.Abs(moved[4].x - 14f) < 0.001f && Math.Abs(moved[5].x - 16f) < 0.001f,
            $"{moved[4].x:0.####}..{moved[5].x:0.####}");
        check("[welded] a leg keeps its thickness",
            Math.Abs(Math.Abs(moved[1].x - moved[0].x) - 2f) < 0.001f,
            $"{Math.Abs(moved[1].x - moved[0].x):0.####} m, drawn at 2 m");

        // The crossbeam stretches to meet them.
        check("[welded] the crossbeam reaches the legs it was welded to",
            Math.Abs(moved[8].x + 14f) < 0.001f && Math.Abs(moved[9].x - 14f) < 0.001f,
            $"{moved[8].x:0.####}..{moved[9].x:0.####}");

        // A negative extra is the golden bridge's case: its tower is wider than the road it is being
        // fitted to, so the legs come together. They must come together, not collapse onto each other.
        var narrowed = TowerWidening.WidenParts(source, triangles, -10f);
        check("[welded] a narrowed portal is still a portal",
            Math.Abs(narrowed[1].x + 1f) < 0.001f && Math.Abs(narrowed[5].x - 3f) < 0.001f,
            $"legs at {narrowed[0].x:0.##}..{narrowed[1].x:0.##} and {narrowed[4].x:0.##}..{narrowed[5].x:0.##}");
        check("[welded] and its legs are the same thickness they were drawn at",
            Math.Abs(Math.Abs(narrowed[1].x - narrowed[0].x) - 2f) < 0.001f,
            $"{Math.Abs(narrowed[1].x - narrowed[0].x):0.####} m");

        // A continuous sheet leaves no gap across the centre, so the whole of it scales - the cables'
        // case, and it falls out of the same rule rather than being a second one.
        var sheet = new[]
        {
            new float3(-13.47333f, 0f, 0f),
            new float3(0f, 40f, 0f),
            new float3(13.47333f, 0f, 0f),
        };
        var stretched = TowerWidening.WidenParts(sheet, new[] { 0, 1, 2 }, 16f);
        check("[welded] a sheet has no clear span, so all of it scales",
            Math.Abs(stretched[2].x - (13.47333f + 8f)) < 0.001f, $"{stretched[2].x:0.####}");
    }

    /// <summary>
    /// The truss-arch prototypes are independent styles, because they are independent bridges.
    ///
    /// The road runs over the arch on one and under it on the other, so a road fitted to one of them
    /// cannot wear the other's structure. The survey says the same thing twice: the arch-below bridges
    /// carry a portal wider than their road, while TrussArchBridge01 carries a pillar narrower than
    /// its road - recorded as a support - and puts the arch itself overhead, one section down each
    /// side.
    ///
    /// The specific pattern has to be matched before the general one, or the name that contains the
    /// other's pattern is swallowed by it. The same ordering keeps SuspensionBridge03 out of the blue
    /// suspension family.
    /// </summary>
    private static void TheTrussArchPrototypesHaveIndependentStyles(Action<string, bool, string?> check)
    {
        check("[split] TrussArchBridge01 is the blue arch-above style",
            BridgeStyleDefinitions.Match("TrussArchBridge01")?.Id == "TrussArch01",
            BridgeStyleDefinitions.Match("TrussArchBridge01")?.Id);
        check("[split] TrussArchBridge03 is the green arch-above style",
            BridgeStyleDefinitions.Match("TrussArchBridge03")?.Id == "TrussArch03",
            BridgeStyleDefinitions.Match("TrussArchBridge03")?.Id);

        foreach (var name in new[]
        {
            "Truss Arch Bridge - Highway Twoway - 2 Lanes",
            "Truss Arch Bridge - Small Road - 2 Lanes",
        })
        {
            check($"[split] '{name}' stays with the arch-below style",
                BridgeStyleDefinitions.Match(name)?.Id == "TrussArch",
                BridgeStyleDefinitions.Match(name)?.Id);
        }

        check("[split] TrussArchBridge02 stays with the general style",
            BridgeStyleDefinitions.Match("TrussArchBridge02")?.Id == "TrussArch",
            BridgeStyleDefinitions.Match("TrussArchBridge02")?.Id);

        // Both specific patterns must come first, or the general pattern swallows them.
        var ids = BridgeStyleDefinitions.All.Select(definition => definition.Id).ToList();
        foreach (var style in new[] { "TrussArch01", "TrussArch03" })
        {
            check($"[split] {style} is declared before the general pattern",
                ids.IndexOf(style) >= 0 && ids.IndexOf(style) < ids.IndexOf("TrussArch"),
                $"{style} at {ids.IndexOf(style)}, TrussArch at {ids.IndexOf("TrussArch")}");
        }

        // Its structure is recorded under its own key, against the road measured from its own
        // prototype. The support's opening is geometry under the deck and cannot replace that road
        // measurement.
        check("[split] the arch-above style knows its own structure",
            BridgeTowers.IsTower("TrussArch01", "TrussArchBridge01NetPillar"), null);
        check("[split] and knows the road that structure was drawn for",
            BridgeTowers.RoadFor("TrussArch01", "TrussArchBridge01NetPillar") == 20f,
            $"{BridgeTowers.RoadFor("TrussArch01", "TrussArchBridge01NetPillar")}");
        check("[split] the blue prototype object remains classified as a support",
            BridgeTowers.For("TrussArch01").Single().Support, null);
        check("[split] a 40 m blue bridge adds its measured 10 m structure allowance",
            Math.Abs(40f - BridgeTowers.RoadOf("TrussArch01")
                + BridgeTowers.BonusFor("TrussArch01") - 30f) < 0.001f,
            $"{40f - BridgeTowers.RoadOf("TrussArch01")
                + BridgeTowers.BonusFor("TrussArch01"):0.###} m");
        check("[split] blue uses the open-truss topology rule",
            BridgeStyleDefinitions.UsesOpenTrussTopology("TrussArch01"), null);

        check("[split] the green style knows only its own prototype structure",
            BridgeTowers.IsTower("TrussArch03", "TrussArchBridge03NetPillar")
            && !BridgeTowers.IsTower("TrussArch", "TrussArchBridge03NetPillar"), null);
        var greenTower = BridgeTowers.For("TrussArch03").Single();
        check("[split] the green structure keeps the TrussArchBridge03 measurements",
            greenTower.Name == "TrussArchBridge03NetPillar"
            && greenTower.Road == 24
            && greenTower.Support,
            $"{greenTower.Name}: road {greenTower.Road}, support {greenTower.Support}");
        check("[split] a 40 m green bridge adds its measured 16 m structure allowance",
            Math.Abs(40f - BridgeTowers.RoadOf("TrussArch03")
                + BridgeTowers.BonusFor("TrussArch03") - 32f) < 0.001f,
            $"{40f - BridgeTowers.RoadOf("TrussArch03")
                + BridgeTowers.BonusFor("TrussArch03"):0.###} m");
        check("[split] green preserves its integrated side railing and arch",
            BridgeStyleDefinitions.PreservesOpenTrussSideAssembly("TrussArch03")
                && !BridgeStyleDefinitions.PreservesOpenTrussSideAssembly("TrussArch01"), null);
        check("[split] green uses the same open-truss topology rule as blue",
            BridgeStyleDefinitions.UsesOpenTrussTopology("TrussArch03"), null);
        check("[split] the arch-below family keeps the portal rule",
            !BridgeStyleDefinitions.UsesOpenTrussTopology("TrussArch"), null);
        check("[split] the green style borrows surfaces only from TrussArchBridge03",
            BridgeTowerMaterials.SourcesFor("TrussArch03").SequenceEqual(
                new[] { "TrussArchBridge03NetPillar" }),
            string.Join(", ", BridgeTowerMaterials.SourcesFor("TrussArch03")));
        check("[split] the green overhead section belongs to the TrussArchBridge03 prototype",
            BridgeCables.All.Any(cables => cables.Tower == "TrussArchBridge03NetPillar"
                && cables.Section == "TrussArchBridge03 Section"), null);

        var blueDefinition = BridgeStyleDefinitions.All.Single(definition => definition.Id == "TrussArch01");
        var greenDefinition = BridgeStyleDefinitions.All.Single(definition => definition.Id == "TrussArch03");
        check("[split] blue and green exports cannot receive the same prefab suffix",
            !string.Equals(blueDefinition.NameSuffix, greenDefinition.NameSuffix, StringComparison.Ordinal),
            $"'{blueDefinition.NameSuffix}' versus '{greenDefinition.NameSuffix}'");

        // None is deferred, and all three are styles the player can pick.
        foreach (var style in new[] { "TrussArch", "TrussArch01", "TrussArch03" })
        {
            check($"[split] {style} is generated",
                BridgeStyleDefinitions.DeferredReason(style) == null
                && BridgeStyleDefinitions.All.Any(definition => definition.Id == style), null);
        }
    }

    /// <summary>
    /// A tower's parts are widened against one boundary, and it never inverts.
    ///
    /// Measured per part, each part gets its own answer and they shear against each other. The golden
    /// bridge's pillar is one structure whose four parts open 43.31, 24.98, 13 and 8 metres; fitted to
    /// a 30 m road it is brought in by 20, and the two parts that open by less than that turned inside
    /// out - <c>(inner + shift) / inner</c> goes negative, the interior is mirrored, and the legs cross
    /// to the far side. It showed in the dump as clear spans of 4.40 and 4 where 鈭? and 鈭?2 were
    /// arithmetically due.
    ///
    /// So the boundary is the tower's widest opening, which is the portal the road passes through, and
    /// the ratio is floored at zero: a portal can close to nothing but never fold through itself.
    /// </summary>
    private static void OnePortalWidensTheWholeTower(Action<string, bool, string?> check)
    {
        // The golden bridge's pillar, as measured: four parts, four openings, one structure.
        const float portal = 43.31445f;
        var openings = new[] { 43.31445f, 24.9751f, 13f, 8f };
        var inner = portal * 0.5f;

        foreach (var opening in openings)
        {
            // A part drawn with legs at its own opening and an outer edge beyond them.
            // The outer edge is the pillar's own, beyond the portal, as every part of the real one is:
            // its four parts reach 22.62, 22.43, 25.20 and 22.62 against a portal half-width of 21.66.
            var half = opening * 0.5f;
            var outer = Math.Max(half + 2f, inner + 1f);
            var vertices = new[]
            {
                new float3(-outer, 0f, 0f),
                new float3(-half, 0f, 0f),
                new float3(0f, 0f, 0f),
                new float3(half, 0f, 0f),
                new float3(outer, 0f, 0f),
            };

            var reach = 0f;
            foreach (var vertex in vertices) reach = Math.Max(reach, Math.Abs(vertex.x));
            var moved = TowerWidening.WidenParts(vertices, -20f, ScopeOf(inner, reach));

            // Every point clear of the centre is carried by the whole of d: x becomes x + sgn(x) * d.
            // Where d takes a leg past the centre it goes past it - the two sides cross, and that is
            // what a road narrower than the design looks like. Stopping them at the centre was tried
            // and is worse: the sides arrive touching and read as one column, and holding the whole
            // tower back so no part reaches that point leaves every other part too wide for the road.
            check($"[portal] a part opening {opening:0.##} carries each side by the whole of d",
                Math.Abs((moved[1].x - vertices[1].x) - 10f) < 0.001f
                && Math.Abs((moved[3].x - vertices[3].x) + 10f) < 0.001f,
                $"{moved[1].x - vertices[1].x:0.##} and {moved[3].x - vertices[3].x:0.##}");

            // The outer edge moves by half the extra whatever the part opens, which is what keeps the
            // road-to-structure difference constant.
            check($"[portal] a part opening {opening:0.##} moves its outer edge by half the extra",
                Math.Abs((moved[4].x - vertices[4].x) + 10f) < 0.001f,
                $"{moved[4].x - vertices[4].x:0.####}");
        }

        // Brought in by more than it opens, a portal is carried through the centre and the sides swap
        // over. The mapping has no special case at the middle and this is what it gives.
        var closed = TowerWidening.WidenParts(
            new[] { new float3(-6f, 0f, 0f), new float3(0f, 0f, 0f), new float3(6f, 0f, 0f) },
            -20f, ScopeOf(6f, 6f));
        check("[portal] brought in past its opening, a part is carried through the centre",
            Math.Abs(closed[0].x - 4f) < 0.001f && Math.Abs(closed[2].x + 4f) < 0.001f,
            $"{closed[0].x:0.##} .. {closed[2].x:0.##}");

        // At the archetype's own width nothing moves.
        var same = TowerWidening.WidenParts(
            new[] { new float3(-10f, 0f, 0f), new float3(10f, 0f, 0f) }, 0f, ScopeOf(6f, 10f));
        check("[portal] at zero extra the part is unchanged",
            Math.Abs(same[0].x + 10f) < 0.001f && Math.Abs(same[1].x - 10f) < 0.001f, null);
    }

    /// <summary>
    /// A portal envelope is wider than the road it carries. A support is not an envelope and its
    /// opening must never be used to rewrite the prototype road measurement.
    ///
    /// The live audit measures TrussArchBridge01's road at 20 m and identifies its 18.4 m object as a
    /// support. Treating its 12.24 m opening as a portal had overwritten the road with 10 m. A 40 m
    /// export was consequently changed by 30 m and its truss tore apart; the prototype delta is 20 m.
    /// </summary>
    private static void AThroughArchIsWiderThanTheRoadItCarries(Action<string, bool, string?> check)
    {
        var checkedAny = false;

        foreach (var styleId in BridgeTowers.Styles)
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                if (tower.Support) continue;
                var cables = BridgeCables.For(tower.Name);
                if (cables == null || !cables.Value.Outer) continue;

                checkedAny = true;
                check($"[through] {styleId}: {tower.Name}'s envelope is wider than the road it carries",
                    cables.Value.Width > tower.Road,
                    $"arch {cables.Value.Width:0.#} m, road {tower.Road} m");

                // And by a margin that survives being derived: the difference is what stays constant,
                // so if it is negative at the archetype it is negative at every width.
                check($"[through] {styleId}: {tower.Name}'s margin is the same at any width",
                    Math.Abs((cables.Value.Width + 30f) - (tower.Road + 30f)
                        - (cables.Value.Width - tower.Road)) < 0.001f, null);
            }
        }

        check("[through] there is an envelope section to check", checkedAny, null);

        var blue = BridgeTowers.For("TrussArch01").Single();
        check("[through] a support opening cannot replace the blue prototype road",
            blue.Support && blue.Road == 20f,
            $"support {blue.Support}, road {blue.Road:0.###} m");
    }

    /// <summary>
    /// A structure cannot be fitted to a deck narrower than the structure itself.
    ///
    /// Widening moves a structure's parts; narrowing moves them the other way, and a part cannot lose
    /// more width than it has. The golden bridge is drawn for a 50 m road and its stiffening truss is
    /// 33.8 m across. Fitted to a 16 m deck it was asked to lose 34 m: the scale factor came out
    /// negative, the floor at zero turned that from a mirrored truss into a collapsed one, and the
    /// report said "0 m across" while the bridge was written anyway.
    ///
    /// Below the floor the answer is not a narrower bridge but a different design, and refusing says
    /// so rather than writing a line and calling it a truss.
    /// </summary>
    private static void AStructureCannotLoseMoreWidthThanItHas(Action<string, bool, string?> check)
    {
        // The arithmetic, on the reading that produced the collapse: a 33.8 m truss recorded against a
        // 50 m road. Fifty was the deck, not the road - see the BridgeTowers entry - and the floor is
        // kept as a test of the rule rather than of that number.
        var floor = BridgeCables.NarrowestDeckFor("SuspensionBridge03NetPylon", 50f);

        check("[floor] a 33.8 m structure on a 50 m road cannot go below 17.2 m",
            Math.Abs(floor - 17.2f) < 0.001f, $"{floor:0.###}");

        check("[floor] a 16 m deck is below it - the one that collapsed",
            16f < floor, null);
        check("[floor] a 28 m deck is above it - the one that worked",
            28f >= floor, null);

        // Against the corrected road there is no floor at all: the truss is wider than the road, so
        // narrowing the road never asks it to lose more than it has. The collapse was the wrong road,
        // not a real limit.
        check("[floor] the corrected road leaves the golden bridge no floor",
            BridgeCables.NarrowestDeckFor("SuspensionBridge03NetPylon", 32f) < 0f,
            $"{BridgeCables.NarrowestDeckFor("SuspensionBridge03NetPylon", 32f):0.###}");

        // At the floor exactly, the truss keeps a metre. Below it there is nothing left to keep.
        var atFloor = 33.8f + (floor - 50f);
        check("[floor] at the floor the structure still has width",
            atFloor > 0.999f && atFloor < 1.001f, $"{atFloor:0.###} m");

        // A tower with no recorded section has no floor to check against, and is not refused on a
        // number nobody measured.
        check("[floor] an unmeasured structure has no floor",
            Math.Abs(BridgeCables.NarrowestDeckFor("no such tower", 50f)) < 0.001f, null);

        // Every recorded envelope's floor is below the road it was drawn for, or the archetype itself
        // could not be built.
        foreach (var cables in BridgeCables.All)
        {
            var tower = BridgeTowers.Styles
                .SelectMany(BridgeTowers.For)
                .FirstOrDefault(entry => entry.Name == cables.Tower);
            if (tower.Name == null || tower.Road <= 0) continue;

            check($"[floor] {cables.Tower} can be built at the road it was drawn for",
                tower.Road >= BridgeCables.NarrowestDeckFor(cables.Tower, tower.Road),
                $"road {tower.Road}, floor {BridgeCables.NarrowestDeckFor(cables.Tower, tower.Road):0.#}");
        }
    }

    /// <summary>
    /// Each family's overhang is its own, and the recorded roads say so.
    ///
    /// Ten metres was written down as "the same across both designs" and applied wherever a road width
    /// looked unusable. It is the blue suspension family's and the cable-stayed one's. The golden
    /// family's pylon is 50.4 m across a 32 m road, an overhang of 18.4, and applying ten to it turned
    /// a 32 m road into a guess of 40 - which, being wrong, was read at the time as confirming the 50
    /// it had replaced. Two wrong numbers agreeing that a third is right.
    /// </summary>
    private static void EachFamilyHasItsOwnOverhang(Action<string, bool, string?> check)
    {
        check("[overhang] the blue family stands ten metres outside its road",
            Math.Abs(BridgeCables.Overhang - 10f) < 0.001f, null);
        check("[overhang] the golden family stands twenty-six point four",
            Math.Abs(BridgeCables.GoldenOverhang - 26.4f) < 0.001f, null);
        check("[overhang] and they are not the same number",
            Math.Abs(BridgeCables.Overhang - BridgeCables.GoldenOverhang) > 1f, null);

        // The blue family, measured: every tower is its road plus ten.
        foreach (var tower in BridgeTowers.For("Suspension"))
        {
            if (tower.Support) continue;

            check($"[overhang] Suspension: {tower.Name} is its road plus ten",
                Math.Abs(tower.Mesh - tower.Road - BridgeCables.Overhang) < 0.001f,
                $"mesh {tower.Mesh}, road {tower.Road}");
        }

        // The golden family, measured the same way: its road plus eighteen point four, to the metre
        // the table records.
        foreach (var tower in BridgeTowers.For("SuspensionGolden"))
        {
            if (tower.Support) continue;

            check($"[overhang] SuspensionGolden: {tower.Name} is its road plus twenty-six",
                Math.Abs(tower.Mesh - tower.Road - BridgeCables.GoldenOverhang) < 1f,
                $"mesh {tower.Mesh}, road {tower.Road}, difference {tower.Mesh - tower.Road}");
        }
    }


    /// <summary>
    /// A structure correction moves what the bridge is built of, and only the styles it was measured
    /// on.
    ///
    /// One number widens the tower and the cables together, which is right wherever both were drawn
    /// around the same carriageway. A style that needs more than the road accounts for takes it here
    /// rather than as a road correction, because moving the road moves the deck props and the spread
    /// report with it.
    ///
    /// It read as a tower correction at first, on the golden family, and was given to the tower alone.
    /// That could not have been right: the distance from the cables to the tower.s outer edge is the
    /// archetype.s and holds at every road width, so three metres of tower and none of cable moves
    /// that distance a metre and a half per side by construction.
    /// </summary>
    private static void ATowerCorrectionMovesTheTowerAlone(Action<string, bool, string?> check)
    {
        check("[bonus] the golden family takes two and a half metres less",
            Math.Abs(BridgeTowers.BonusFor("SuspensionGolden") + 2.5f) < 0.001f, null);

        // A correction can go either way. It was three metres more, then four metres less on the same
        // tower, and the sign is not a special case - what it means is that the structure is widened
        // by a metre less than the road accounts for.
        check("[bonus] a correction may take width away as well as add it",
            BridgeTowers.BonusFor("SuspensionGolden") < 0f, null);

        check("[bonus] the V pylon takes eighteen metres more",
            Math.Abs(BridgeTowers.BonusFor("Extradosed03") - 18f) < 0.001f, null);

        check("[bonus] the double-deck V structure takes twenty metres more",
            Math.Abs(BridgeTowers.BonusFor("Extradosed01") - 20f) < 0.001f, null);

        check("[bonus] the blue arch-above frame takes ten metres more",
            Math.Abs(BridgeTowers.BonusFor("TrussArch01") - 10f) < 0.001f, null);

        check("[bonus] the green arch-above frame takes sixteen metres more",
            Math.Abs(BridgeTowers.BonusFor("TrussArch03") - 16f) < 0.001f, null);

        // Its neighbours do not. The V pylon was measured; Extradosed01 and 02 are different pylons
        // on different roads, and rule 9 is that a number belongs to what it was measured on.
        foreach (var style in new[] { "Suspension", "CableStayed", "TrussArch",
            "Extradosed02", "Grand", "TiedArch", "CoveredWood" })
        {
            check($"[bonus] {style} takes none",
                Math.Abs(BridgeTowers.BonusFor(style)) < 0.001f, null);
        }

        check("[bonus] an unknown style takes none",
            Math.Abs(BridgeTowers.BonusFor("no such style")) < 0.001f, null);
        check("[bonus] nothing takes none", Math.Abs(BridgeTowers.BonusFor(null)) < 0.001f, null);

        // What the correction is for, and what it must not disturb. The golden family is widened by a
        // metre less than its road accounts for; the distance from its cables to the tower.s outer
        // edge is the archetype.s and holds at every road width.
        //
        // This block used to model the correction as the tower.s alone and assert the gap grew by it,
        // which is the behaviour that was wrong - and it passed, because it computed both sides of its
        // own equation. It asks the shared number now.
        const float road = 24f;
        const float tower = 50.4f;
        const float cables = 33.8f;

        foreach (var deck in new[] { 22f, 24f, 30f, 40f, 64f })
        {
            var extra = deck - road + BridgeTowers.BonusFor("SuspensionGolden");
            var t = tower + extra;
            var c = cables + extra;

            check($"[bonus] at a {deck} m deck the cables keep the archetype.s distance",
                Math.Abs((t - c) - (tower - cables)) < 0.001f,
                $"{t - c:0.##} against {tower - cables:0.##}");
        }

        // At the archetype.s own road the structure is the correction away from the archetype - not
        // unchanged, because the correction says the archetype.s own width was read wrong by that much.
        var atOwn = tower + BridgeTowers.BonusFor("SuspensionGolden");
        check("[bonus] at the archetype.s road the structure takes the correction",
            Math.Abs(atOwn - (tower + BridgeTowers.BonusFor("SuspensionGolden"))) < 0.001f
                && BridgeTowers.BonusFor("SuspensionGolden") < 0f,
            $"{atOwn:0.##}");
    }

    /// <summary>
    /// The pieces of one section are widened against one boundary, so a feature in more than one of
    /// them moves the same way in each.
    ///
    /// The golden bridge's cables run through an end piece and a middle piece. The end piece is wider -
    /// 33.75 m against 28.77 - because it carries the anchorage, not because its cables are further
    /// apart: the cables are at the same place in both, which is what the archetype looks like.
    ///
    /// Measured per piece the two disagree about them. The end opens 32.72 m and the middle 27.05, so
    /// cables at 14.39 fall inside the end's boundary and outside the middle's, and the same pair was
    /// scaled in one piece and carried in the other. They met at neither node.
    /// </summary>
    private static void OneSectionIsWidenedAgainstOneBoundary(Action<string, bool, string?> check)
    {
        // The golden bridge's two cable pieces, measured: their own openings, and the cables they
        // share.
        const float endOpening = 32.71875f;
        const float middleOpening = 27.0498f;
        const float cables = 14.39f;

        var shared = Math.Max(endOpening, middleOpening) * 0.5f;

        foreach (var extra in new[] { -8f, -4f, 0f, 8f, 16f })
        {
            var atEnd = Move(cables, extra, shared);
            var atMiddle = Move(cables, extra, shared);

            check($"[shared] at {extra} m the cables land together in both pieces",
                Math.Abs(atEnd - atMiddle) < 0.001f, $"{atEnd:0.####} against {atMiddle:0.####}");

            if (Math.Abs(extra) < 0.001f) continue;

            // What one scope per piece does when the pieces differ at that height. The end piece
            // carries the anchorage, which stands on the centre where the middle piece is open; asked
            // separately the end scales its cables and the middle carries them, and they meet at
            // neither node. Asked together the section crosses there, so both scale, and they agree.
            var wasEnd = TowerWidening.WidenParts(
                new[] { new float3(cables, 0f, 0f) }, extra, ScopeOf(0f, endOpening * 0.5f))[0].x;
            var wasMiddle = TowerWidening.WidenParts(
                new[] { new float3(cables, 0f, 0f) }, extra, ScopeOf(cables, cables))[0].x;

            check($"[shared] at {extra} m a scope per piece parts them",
                Math.Abs(wasEnd - wasMiddle) > 0.001f,
                $"{wasEnd:0.####} against {wasMiddle:0.####}");
        }

        // The boundary is the widest opening, so the piece that opens least does not decide for the
        // rest - the same rule a tower's portal follows.
        check("[shared] the boundary is the widest opening in the section",
            Math.Abs(shared - endOpening * 0.5f) < 0.001f, $"{shared:0.####}");

        // At the archetype's own width nothing moves, whichever boundary is used.
        check("[shared] at zero extra the cables are where they were",
            Math.Abs(Move(cables, 0f, shared) - cables) < 0.001f, null);
    }

    /// <summary>One coordinate through the widening rule, at a given boundary.</summary>
    /// <summary>
    /// A profile with a boundary of <paramref name="inner"/> and a reach of <paramref name="reach"/>,
    /// built from geometry rather than declared.
    ///
    /// This is the section case in miniature: a scope wider than the shape being widened, so a vertex
    /// can legitimately sit inside its boundary. A shape measured against itself never can - its own
    /// closest approach is by definition no further out than any of its vertices.
    /// </summary>
    private static TowerWidening.Profile ScopeOf(float inner, float reach) =>
        TowerWidening.Profile.Of(new[]
        {
            new float3(inner, 0f, 0f), new float3(-inner, 0f, 0f),
            new float3(reach, 0f, 0f), new float3(-reach, 0f, 0f),
        });

    private static float Move(float x, float extra, float inner)
    {
        var moved = TowerWidening.WidenParts(
            new[] { new float3(x, 0f, 0f) }, extra, ScopeOf(inner, Math.Max(inner, Math.Abs(x))));
        return moved[0].x;
    }

    /// <summary>
    /// A leg is never scaled, whatever the tower's other parts open by.
    ///
    /// Each part is widened against its own opening, which is where its own legs begin. One boundary
    /// for the whole tower was tried and is worse: the golden pillar's four parts open 43.31, 24.98,
    /// 13 and 8 metres, and the widest is inside the legs of every other part. Taking 43.31 for all of
    /// them put the boundary at 21.66 while the pier's legs begin at 12.49, so nine metres of leg fell
    /// inside the boundary and were scaled. Rule 5 says a tower is never scaled.
    ///
    /// What one boundary was meant to fix was shear. It is not shear: a part's interior spans that
    /// part's own legs and stretches to meet them, by its own ratio, because they are its own legs.
    /// </summary>
    private static void ALegIsCarriedWhateverTheOtherPartsOpen(Action<string, bool, string?> check)
    {
        // The golden pillar, measured: four parts, four openings, and the legs of each.
        var parts = new[]
        {
            new { Name = "PylonTop", Opening = 43.31445f, Outer = 22.62f },
            new { Name = "PillarTop", Opening = 24.9751f, Outer = 22.62f },
            new { Name = "Pillar", Opening = 13f, Outer = 22.43f },
            new { Name = "PillarBase", Opening = 8f, Outer = 25.20f },
        };

        var widest = 43.31445f * 0.5f;

        foreach (var part in parts)
        {
            var inner = part.Opening * 0.5f;

            foreach (var extra in new[] { -20f, -8f, 8f, 26f })
            {
                // The leg runs from its own inner face to its outer edge, and both move together.
                var face = Move(inner, extra, inner);
                var outer = Move(part.Outer, extra, inner);
                // A leg keeps its thickness whatever it is asked to do, including being carried
                // through the centre. It used to stop there, which thinned it instead - the one place
                // the rule had a special case, and the only place a leg was allowed to change shape.

                check($"[leg] {part.Name} at {extra} m keeps its thickness",
                    Math.Abs((outer - face) - (part.Outer - inner)) < 0.001f,
                    $"{outer - face:0.####} against {part.Outer - inner:0.####}");
            }

            // What a scope wider than the part does to it. If the scope crosses the centre at this
            // height - because some other part of the tower has a beam there - the leg is scaled
            // rather than carried, and comes out a different thickness than it went in. That is why a
            // tower.s part is measured against itself and its own levels of detail, and nothing else.
            if (part.Opening >= 43f) continue;

            var crossing = ScopeOf(0f, part.Outer);
            var sharedFace = TowerWidening.WidenParts(
                new[] { new float3(inner, 0f, 0f) }, 8f, crossing)[0].x;
            var sharedOuter = TowerWidening.WidenParts(
                new[] { new float3(part.Outer, 0f, 0f) }, 8f, crossing)[0].x;
            check($"[leg] a scope that crosses where {part.Name} does not would stretch it",
                Math.Abs((sharedOuter - sharedFace) - (part.Outer - inner)) > 0.001f,
                $"{sharedOuter - sharedFace:0.####} against {part.Outer - inner:0.####}");
        }

        // And at the archetype's own width nothing moves at all.
        check("[leg] at zero extra a leg is where it was",
            Math.Abs(Move(22.43f, 0f, 6.5f) - 22.43f) < 0.001f, null);
    }

    /// <summary>
    /// The cable-stayed family is one style per pylon, and the double deck designs refuse a single
    /// deck.
    ///
    /// Five bridges were one entry. The pylon is what a cable-stayed design is - H, V, A, single
    /// column - and a road fitted to one cannot wear another's cables, so filing them together meant
    /// the width fit chose between designs rather than between sizes of one.
    ///
    /// Two of them carry a second deck of their own. That is read off the prefab rather than recorded:
    /// ExtradosedBridge01 and 02 have an AuxiliaryNets arrangement and 03 does not, confirmed in the
    /// dump, and BridgeStyle.Select now filters both ways - a single deck request takes only single
    /// deck archetypes, as a double deck request already took only double deck ones.
    /// </summary>
    private static void OneStylePerPylon(Action<string, bool, string?> check)
    {
        var byName = new (string Prefab, string Style)[]
        {
            ("ExtradosedBridge01", "Extradosed01"),
            ("ExtradosedBridge02", "Extradosed02"),
            ("ExtradosedBridge03", "Extradosed03"),
            ("Extradosed Bridge - Large Road Divided - 6 Lanes", "ExtradosedLarge"),
            ("8-Lane Cable Stayed Bridge 00", "CableStayed"),
            ("Cable Stayed Pedestrian Bridge", "CableStayed"),
        };

        foreach (var entry in byName)
        {
            check($"[pylon] '{entry.Prefab}' is {entry.Style}",
                BridgeStyleDefinitions.Match(entry.Prefab)?.Id == entry.Style,
                BridgeStyleDefinitions.Match(entry.Prefab)?.Id);
        }

        // There is no catch-all any more. A prefab matching none of the pylons matches no named style
        // at all, and the catalogue picks it up as a family of its own - which is honest, where filing
        // it under a name that means five different bridges was not.
        foreach (var name in new[] { "ExtradosedBridge04", "6-Lane Extradosed Bridge" })
        {
            check($"[pylon] '{name}' matches no named style",
                BridgeStyleDefinitions.Match(name) == null,
                BridgeStyleDefinitions.Match(name)?.Id);
        }

        // Every pylon is a style of its own, and the catch-all is gone.
        var ids = BridgeStyleDefinitions.All.Select(definition => definition.Id).ToList();
        foreach (var style in new[] { "Extradosed01", "Extradosed02", "Extradosed03", "ExtradosedLarge" })
        {
            check($"[pylon] {style} is a style the player can pick", ids.Contains(style), null);
        }

        check("[pylon] the catch-all extradosed style is gone", !ids.Contains("Extradosed"), null);

        // Each keeps its own structure, at the road that structure was drawn for.
        // The audit's widths: the narrowest carriageway among the bridges that ship carrying each
        // pylon. They were 20/18/18 for a while - twenty metres of in-game corrections recorded as
        // road corrections - and at 20 the V pylon widened a 40 m road by another 20, which hung its
        // stay cables ten metres past each edge of the deck.
        check("[pylon] the V pylon knows its own structure",
            BridgeTowers.RoadFor("Extradosed01", "ExtradosedBridge01NetPillar") == 40f, null);
        check("[pylon] the A pylon, double deck",
            BridgeTowers.RoadFor("Extradosed02", "ExtradosedBridge02NetPillar") == 38f, null);
        check("[pylon] the A pylon, single deck",
            BridgeTowers.RoadFor("Extradosed03", "ExtradosedBridge03NetPillar") == 38f, null);

        // How far each pylon stands outside the road it was drawn for. A pylon overhangs its road by
        // metres, not by tens of metres: the wrong widths made these 33, 28 and 38, which is a pylon
        // wider than the bridge it belongs to and is what put the stay cables past the deck.
        //
        // The self test cannot check this. It reproduces each tower at whatever road the table says,
        // so a wrong table is a test that passes - which is why this asks the table a question the
        // table cannot answer with itself.
        foreach (var style in new[] { "Extradosed01", "Extradosed02", "Extradosed03" })
        {
            var road = BridgeTowers.RoadOf(style);
            var mesh = BridgeTowers.For(style)[0].Mesh;
            check($"[pylon] {style} stands a pylon's width outside its road, not a bridge's",
                mesh - road > 0f && mesh - road <= 20f, $"{mesh - road:0.##} m over {road:0.##} m");
        }

        // The single column does not straddle the road, so it is not widened against one.
        check("[pylon] the single column is used unchanged",
            BridgeTowers.NotDerivedReason("ExtradosedLarge") != null, null);
        check("[pylon] and has no portal",
            BridgeTowers.For("ExtradosedLarge").All(tower => tower.Support), null);

        // None of the others claims to be structure-overhead: they are portals the road runs through.
        foreach (var style in new[] { "Extradosed01", "Extradosed02", "Extradosed03", "CableStayed" })
        {
            check($"[pylon] {style} has a portal the road passes through and is derived",
                BridgeTowers.NotDerivedReason(style) == null
                && BridgeTowers.For(style).Any(tower => !tower.Support), null);
        }
    }
    /// <summary>
    /// The pillar types, as the game numbers them.
    ///
    /// Recorded in full so the one value that is used can be checked against its neighbours rather than
    /// trusted. It was wrong for a long time: the enum starts at minus one, the field order in metadata
    /// is not the field values, and reading three off the order gave Base - a pillar under the deck -
    /// where a standalone tower was meant. Every other field of the generated tower matched its
    /// archetype, so nothing pointed at it until the two were dumped side by side.
    /// </summary>
    private static void ThePillarTypesAreNumberedAsTheGameNumbersThem(Action<string, bool, string?> check)
    {
        check("[enum] None is minus one", BridgeTowerSpec.PillarTypeNone == -1, null);
        check("[enum] Vertical is zero", BridgeTowerSpec.PillarTypeVertical == 0, null);
        check("[enum] Horizontal is one", BridgeTowerSpec.PillarTypeHorizontal == 1, null);
        check("[enum] Standalone is two", BridgeTowerSpec.PillarTypeStandalone == 2, null);
        check("[enum] Base is three", BridgeTowerSpec.PillarTypeBase == 3, null);

        check("[enum] a tower is standalone, not a base",
            BridgeTowerSpec.PillarTypeStandalone != BridgeTowerSpec.PillarTypeBase, null);
    }

    /// <summary>
    /// A portal's crossbeams run from one leg to the other, and widening must not tear them.
    ///
    /// This is the fault that made a tower look fine at small widths and come apart at large ones. The
    /// rule was built on sign(x), which has a discontinuity at the centre line: every vertex left of it
    /// jumps one way, every vertex right of it jumps the other, and a beam spanning the middle is torn
    /// open by exactly the shift. The tear grows with the widening, so it hides at four metres and is
    /// unmissable at forty - which reads as a width beyond which the mesh explodes.
    ///
    /// The legs still move rigidly. Both properties at once are the point: a beam that stretches and
    /// legs that do not.
    /// </summary>
    private static void CrossbeamsStretchAndLegsDoNot(Action<string, bool, string?> check)
    {
        const float road = 24f;
        const float inner = road / 2f;

        foreach (var extra in new[] { 4f, 16f, 40f })
        {
            // A beam sampled from one leg to the other, through the centre.
            var beam = new[]
            {
                new float3(-14f, 40f, 0f), new float3(-12f, 40f, 0f), new float3(-6f, 40f, 0f),
                new float3(0f, 40f, 0f), new float3(6f, 40f, 0f), new float3(12f, 40f, 0f),
                new float3(14f, 40f, 0f),
            };

            var widened = TowerWidening.Widen(beam, extra, inner);
            var label = extra.ToString("0.#", CultureInfo.InvariantCulture) + " m";

            check($"[beam] at {label} the beam keeps its order", InOrder(widened), null);

            // No gap where there was none. Under the old rule the middle two vertices ended up `extra`
            // apart however close they started.
            check($"[beam] at {label} nothing is torn at the centre line",
                WorstEdge(beam, widened) < 4f,
                WorstEdge(beam, widened).ToString("0.##", CultureInfo.InvariantCulture));

            // The legs are carried apart rigidly: the outer two vertices keep their spacing.
            var legBefore = beam[1].x - beam[0].x;
            var legAfter = widened[1].x - widened[0].x;
            check($"[beam] at {label} the leg keeps its thickness",
                Math.Abs(legAfter - legBefore) < 1e-4f,
                legAfter.ToString("0.###", CultureInfo.InvariantCulture));

            // And the opening grows by the full amount asked for.
            check($"[beam] at {label} the opening grows by the whole shift",
                Math.Abs(TowerWidening.WidthOf(widened) - (TowerWidening.WidthOf(beam) + extra)) < 1e-3f,
                TowerWidening.WidthOf(widened).ToString("0.##", CultureInfo.InvariantCulture));
        }

        // At the authored width nothing moves at all.
        var same = new[] { new float3(-9f, 0f, 0f), new float3(0f, 0f, 0f), new float3(9f, 0f, 0f) };
        var unchanged = TowerWidening.Widen(same, 0f, inner);
        check("[beam] nothing moves at the authored width",
            same.Select((vertex, index) => Math.Abs(vertex.x - unchanged[index].x) < 1e-6f).All(x => x),
            null);
    }

    /// <summary>
    /// A diagonal transverse truss member is one rectangular object even though every station along
    /// it lies in a different height band. The ordinary portal profile may legitimately answer those
    /// bands differently; the through-arch correction must recover one affine map from the source
    /// member's own topology, or its quads become the triangular fans seen in game.
    /// </summary>
    private static void AThroughArchCrossMemberStaysAffine(
        Action<string, bool, string?> check)
    {
        var vertices = new List<float3>();
        var triangles = new List<int>();

        // A tessellated rectangular strip rising as it crosses the bridge. Tessellation matters: a
        // two-ended beam can hide a discontinuous mapping, while its intermediate stations expose it.
        for (var station = 0; station < 5; station++)
        {
            var x = -8f + (station * 4f);
            var y = station * 2f;
            vertices.Add(new float3(x, y, 0f));
            vertices.Add(new float3(x, y + 0.5f, 0f));

            if (station == 0) continue;
            var at = station * 2;
            triangles.AddRange(new[]
            {
                at - 2, at - 1, at + 1,
                at - 2, at + 1, at,
            });
        }

        // A separate longitudinal side member. It belongs to the right-hand arch, never crosses the
        // centre, and therefore must retain the rigid translation chosen by the ordinary profile.
        var side = vertices.Count;
        vertices.Add(new float3(9f, 0f, 0f));
        vertices.Add(new float3(10f, 0f, 0f));
        vertices.Add(new float3(10f, 8f, 0f));
        vertices.Add(new float3(9f, 8f, 0f));
        triangles.AddRange(new[] { side, side + 1, side + 2, side, side + 2, side + 3 });

        // A separate, narrow centre pivot. It crosses x=0 but does not reach either side truss. It
        // must share the transverse member's bounded scale: leaving it rigid detaches its arms from
        // the rods, while adding the full bridge delta to this tiny piece creates a broad sheet.
        var centrePlate = vertices.Count;
        vertices.Add(new float3(-0.25f, 10f, 0f));
        vertices.Add(new float3(0.25f, 10f, 0f));
        vertices.Add(new float3(0.25f, 11f, 0f));
        vertices.Add(new float3(-0.25f, 11f, 0f));
        triangles.AddRange(new[]
        {
            centrePlate, centrePlate + 1, centrePlate + 2,
            centrePlate, centrePlate + 2, centrePlate + 3,
        });

        // A detached fitting between centre and the right truss uses the same coordinate field. This
        // is what keeps separately authored hard-edge parts aligned with the structural rods.
        var interiorFitting = vertices.Count;
        vertices.Add(new float3(1.9f, 12f, 0f));
        vertices.Add(new float3(2.1f, 12f, 0f));
        vertices.Add(new float3(2.1f, 12.2f, 0f));
        vertices.Add(new float3(1.9f, 12.2f, 0f));
        triangles.AddRange(new[]
        {
            interiorFitting, interiorFitting + 1, interiorFitting + 2,
            interiorFitting, interiorFitting + 2, interiorFitting + 3,
        });

        // The real failing export is a 40 m road built from the 20 m blue prototype.
        const float extra = 20f;
        var source = vertices.ToArray();
        var outline = triangles.ToArray();
        var moved = TowerWidening.WidenOpenTruss(source, outline, extra, out var facts);

        var ratio = (16f + extra) / 16f;
        var affine = source.Take(side).Select((vertex, index) =>
            Math.Abs(moved[index].x - (vertex.x * ratio)) < 0.001f
            && Math.Abs(moved[index].y - vertex.y) < 0.001f
            && Math.Abs(moved[index].z - vertex.z) < 0.001f).All(same => same);

        check("[through arch] source topology finds both x=0-crossing parts",
            facts.SpanningPieces == 2, facts.SpanningPieces.ToString(CultureInfo.InvariantCulture));
        check("[through arch] every station of the member uses one affine widening",
            affine, facts.SpanningPieces + " spanning piece");
        check("[through arch] the side truss remains a rigid translated member",
            Math.Abs(moved[side].x - 19f) < 0.001f
                && Math.Abs(moved[side + 1].x - 20f) < 0.001f
                && Math.Abs((moved[side + 1].x - moved[side].x) - 1f) < 0.001f,
            $"{moved[side].x:0.###}..{moved[side + 1].x:0.###}");
        check("[through arch] the centre pivot stretches against its own span",
            Math.Abs(moved[centrePlate].x + 10.25f) < 0.001f
                && Math.Abs(moved[centrePlate + 1].x - 10.25f) < 0.001f
                && Math.Abs(facts.LeftStructuralReach - 8f) < 0.001f
                && Math.Abs(facts.RightStructuralReach - 8f) < 0.001f
                && Math.Abs(facts.LeftScale - ratio) < 0.001f
                && Math.Abs(facts.RightScale - ratio) < 0.001f,
            $"{moved[centrePlate].x:0.###}..{moved[centrePlate + 1].x:0.###}");
        check("[through arch] an interior fitting which misses x=0 is translated",
            Math.Abs(moved[interiorFitting].x - 11.9f) < 0.001f
                && Math.Abs(moved[interiorFitting + 1].x - 12.1f) < 0.001f,
            $"{moved[interiorFitting].x:0.###}..{moved[interiorFitting + 1].x:0.###}");
        check("[through arch] x=0 alone separates translated and stretched pieces",
            facts.RigidPieces == 2 && facts.SpanningPieces == 2 && facts.FloatingPieces == 0,
            $"rigid {facts.RigidPieces}, spanning {facts.SpanningPieces}, followers {facts.FloatingPieces}");

        var forbiddenOverride = moved.ToArray();
        forbiddenOverride[side].x = source[side].x * ratio;
        var rejectedOverride = false;
        try
        {
            TowerWidening.RequireCentrelineRule(source, forbiddenOverride, outline, extra);
        }
        catch (InvalidOperationException)
        {
            rejectedOverride = true;
        }
        check("[through arch] an attempted override of the x=0 rule throws before export",
            rejectedOverride, null);

        // The authored 0.5 m separation between the two long sides remains 0.5 m at every station.
        var rectangular = true;
        for (var station = 0; station < 5; station++)
        {
            var bottom = moved[station * 2];
            var top = moved[(station * 2) + 1];
            if (Math.Abs(top.x - bottom.x) > 0.001f
                || Math.Abs((top.y - bottom.y) - 0.5f) > 0.001f)
                rectangular = false;
        }

        check("[through arch] the widened member remains rectangular",
            rectangular, null);
        check("[through arch] actual output width changes by 40 minus the 20 m prototype",
            Math.Abs(facts.MeasuredWidthChange - 20f) < 0.001f,
            $"{facts.MeasuredWidthChange:0.###} m");
        check("[through arch] topology audit finds no new degenerate or flipped triangle",
            facts.Finite
                && facts.DegenerateAfter == facts.DegenerateBefore
                && facts.FlippedTriangles == 0,
            $"degenerate {facts.DegenerateBefore}->{facts.DegenerateAfter}, flipped {facts.FlippedTriangles}");

        // Green has a different measured prototype (24 m) and a measured 16 m structural allowance,
        // so a 40 m target requests 40 - 24 + 16 = 32 m. The fine archetype keeps each side assembly
        // (inner railing plus outer arch) separate from the member crossing x=0.
        var greenSource = new[]
        {
            new float3(-6f, 2f, 0f), new float3(6f, 2f, 0f),
            new float3(6f, 3f, 0f), new float3(-6f, 3f, 0f),
            new float3(7f, 2f, 0f), new float3(10f, 2f, 0f),
            new float3(10f, 8f, 0f), new float3(7f, 8f, 0f),
            new float3(-10f, 2f, 0f), new float3(-7f, 2f, 0f),
            new float3(-7f, 8f, 0f), new float3(-10f, 8f, 0f),
        };
        var greenTriangles = new[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
        };
        var greenProfile = TowerWidening.Profile.Of(
            new[] { greenSource }, new IReadOnlyList<int>?[] { greenTriangles });
        const float greenExtra = 32f;
        var greenMoved = TowerWidening.WidenOpenTruss(
            greenSource, greenTriangles, greenExtra, true, greenProfile, out var greenFacts);
        check("[through arch] green inner rail and outer arch translate by the same amount",
            Math.Abs(greenMoved[4].x - 23f) < 0.001f
                && Math.Abs(greenMoved[5].x - 26f) < 0.001f
                && Math.Abs(greenMoved[8].x + 26f) < 0.001f
                && Math.Abs(greenMoved[9].x + 23f) < 0.001f,
            $"{greenMoved[8].x:0.###}..{greenMoved[9].x:0.###}, "
                + $"{greenMoved[4].x:0.###}..{greenMoved[5].x:0.###}");
        check("[through arch] green side assembly keeps its authored thickness",
            Math.Abs((greenMoved[5].x - greenMoved[4].x) - 3f) < 0.001f
                && Math.Abs((greenMoved[9].x - greenMoved[8].x) - 3f) < 0.001f,
            $"right {greenMoved[5].x - greenMoved[4].x:0.###}, "
                + $"left {greenMoved[9].x - greenMoved[8].x:0.###} m");

        // The coarse representation welds both side assemblies to its centre-crossing bar. Its own
        // connected-component answer is therefore different, but an LOD is not allowed to reclassify
        // the parts. It must reuse greenProfile from the full-detail archetype above.
        var greenLod = new[]
        {
            new float3(-7f, 2f, 0f), new float3(7f, 2f, 0f),
            new float3(7f, 3f, 0f), new float3(-7f, 3f, 0f),
            new float3(7f, 2f, 0f), new float3(10f, 2f, 0f),
            new float3(10f, 8f, 0f), new float3(7f, 8f, 0f),
            new float3(-10f, 2f, 0f), new float3(-7f, 2f, 0f),
            new float3(-7f, 8f, 0f), new float3(-10f, 8f, 0f),
        };
        var greenLodTriangles = new[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
        };
        var lodPieces = TowerWidening.PiecesOf(greenLod, greenLodTriangles, out _);
        var greenLodMoved = TowerWidening.WidenOpenTruss(
            greenLod, greenLodTriangles, greenExtra, true, greenProfile, out var greenLodFacts);
        check("[through arch] regression LOD welds both outer arches through x=0",
            lodPieces.Length == 1 && lodPieces[0].Left <= -10f && lodPieces[0].Right >= 10f,
            $"{lodPieces.Length} connected pieces");
        check("[through arch] far-view outer arches still translate instead of stretching",
            Math.Abs(greenLodMoved[4].x - 23f) < 0.001f
                && Math.Abs(greenLodMoved[5].x - 26f) < 0.001f
                && Math.Abs(greenLodMoved[8].x + 26f) < 0.001f
                && Math.Abs(greenLodMoved[9].x + 23f) < 0.001f,
            $"{greenLodMoved[8].x:0.###}..{greenLodMoved[9].x:0.###}, "
                + $"{greenLodMoved[4].x:0.###}..{greenLodMoved[5].x:0.###}");
        check("[through arch] near and far green side thicknesses are identical",
            Math.Abs((greenLodMoved[5].x - greenLodMoved[4].x)
                - (greenMoved[5].x - greenMoved[4].x)) < 0.001f,
            $"near {greenMoved[5].x - greenMoved[4].x:0.###}, "
                + $"far {greenLodMoved[5].x - greenLodMoved[4].x:0.###} m");
        check("[through arch] green output includes its 16 m structural allowance",
            Math.Abs(greenFacts.MeasuredWidthChange - greenExtra) < 0.001f
                && Math.Abs(greenLodFacts.MeasuredWidthChange - greenExtra) < 0.001f,
            $"near {greenFacts.MeasuredWidthChange:0.###}, "
                + $"far {greenLodFacts.MeasuredWidthChange:0.###} m");
        check("[through arch] green near and far topology audits stay valid",
            greenFacts.Finite && greenLodFacts.Finite
                && greenFacts.DegenerateAfter == greenFacts.DegenerateBefore
                && greenLodFacts.DegenerateAfter == greenLodFacts.DegenerateBefore
                && greenFacts.FlippedTriangles == 0 && greenLodFacts.FlippedTriangles == 0,
            $"near degenerate {greenFacts.DegenerateBefore}->{greenFacts.DegenerateAfter}, "
                + $"far {greenLodFacts.DegenerateBefore}->{greenLodFacts.DegenerateAfter}");
    }

    private static bool InOrder(float3[] vertices)
    {
        for (var index = 1; index < vertices.Length; index++)
        {
            if (vertices[index].x < vertices[index - 1].x - 1e-4f) return false;
        }

        return true;
    }

    /// <summary>The largest factor by which any edge between neighbouring vertices changed length.</summary>
    private static float WorstEdge(float3[] before, float3[] after)
    {
        var worst = 0f;
        for (var index = 1; index < before.Length; index++)
        {
            var was = Math.Abs(before[index].x - before[index - 1].x);
            if (was < 1e-4f) continue;
            worst = Math.Max(worst, Math.Abs(after[index].x - after[index - 1].x) / was);
        }

        return worst;
    }

    /// <summary>How far a suspension bridge's cables hang outside the carriageway, both sides together.</summary>
    private const int CableMargin = 3;

    /// <summary>Every suspension tower reaches five metres past the carriageway on each side.</summary>
    private const int SuspensionOverhang = 10;

    /// <summary>
    /// A stand-in tower of a given span: two legs, the beam between them, and a part offset on each
    /// side. Enough shape for the widening to be wrong on, which is all these tests need.
    /// </summary>
    private static float3[] Portal(float span)
    {
        var half = span * 0.5f;
        return new[]
        {
            new float3(-half, 0f, -1f),
            new float3(-half, 0f, 1f),
            new float3(half, 0f, -1f),
            new float3(half, 0f, 1f),
            new float3(-half, 40f, 0f),
            new float3(half, 40f, 0f),
            new float3(0f, 42f, 0f),
        };
    }
}
