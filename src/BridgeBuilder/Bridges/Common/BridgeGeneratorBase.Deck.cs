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
    /// The carriageway a bridge is sized against, and how it was arrived at.
    ///
    /// Normally the prefab being built. It is a separate argument because the selected root deck is
    /// established before its clone is composed. For a double deck whose auxiliary hangs above, that
    /// root is the chosen lower road/track; for an auxiliary below it is the converted upper road.
    /// </summary>
    internal static float WidthOf(
        NetGeometryPrefab prefab, float fallback, List<string>? breakdown = null)
    {
        var width = NetWidth.RoadSurfaceOf(prefab, breakdown);
        // A selectable source network must use its initialized composition, never
        // an old catalog value when that composition is temporarily unavailable.
        if (prefab is RoadPrefab or TrackPrefab or PathwayPrefab
            && prefab.GetComponent<Bridge>() == null) return width;
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
    /// Selected elevated component slots are laid out across the road in order. All styles scan
    /// inward only as far as x=0 for the first actual sidewalk on each side, so an empty lane is not
    /// mistaken for one and a one-sided sidewalk is not mirrored onto the other side.
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
        RoadPrefab? target, float fallbackWidth)
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
        var counted = new List<(string Name, float Start, float Width)>();
        var total = 0f;
        foreach (var allocation in NetWidth.SourceSections(target!))
        {
            var width = allocation.Width;
            if (width <= 0f) continue;

            counted.Add((allocation.Name, total, width));
            total += width;
        }

        var outerBoundary = total > 0f ? total * 0.5f : Math.Max(0f, fallbackWidth * 0.5f);
        if (counted.Count == 0)
        {
            return (
                new RoadEdge(outerBoundary, outerBoundary, isSidewalk: false),
                new RoadEdge(outerBoundary, outerBoundary, isSidewalk: false));
        }

        static RoadEdge OutermostSidewalkOf(
            IReadOnlyList<(string Name, float Start, float Width)> entries,
            float outer,
            bool positive)
        {
            if (positive)
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    if (entry.Start >= outer) break;
                    if (!SectionNames.IsSidewalk(entry.Name)) continue;

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
                    if (!SectionNames.IsSidewalk(entry.Name)) continue;

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
        return (
            OutermostSidewalkOf(counted, outerBoundary, positive: false),
            OutermostSidewalkOf(counted, outerBoundary, positive: true));
    }

    private void RemoveDeckRailings(RoadPrefab target, string? styleId)
    {
        if (BridgeStyleDefinitions.RoadRailingsOf(styleId) != RoadRailingPolicy.EndsAndNodesOnly)
            return;

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
            "{0}: the road's own white railing now draws only at nodes and dead ends - {1}. The "
            + "measured archetype for style '{2}' has no ordinary road railing along its span; joins "
            + "and turnarounds retain the road railing so their open ends remain protected.",
            target.name, string.Join(", ", removed.Distinct()), styleId ?? "<unknown>"));
    }

    /// <summary>
    /// How far above the deck a side piece has to reach before it is something standing on the deck
    /// rather than something holding its edge together.
    ///
    /// The shoulder's side piece tops out at 0.2 m below; the elevated edge with its railing reaches
    /// 0.5 m above. There is no case anywhere near this line, which is why a line will do.
    /// </summary>
    private const float DeckSurface = 0.25f;

}
