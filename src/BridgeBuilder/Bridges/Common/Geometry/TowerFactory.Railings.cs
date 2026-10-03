using Colossal.AssetPipeline;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{

    /// <summary>
    /// A level of detail must be widened by what the mesh above it was widened by.
    ///
    /// Not "to the same width". A coarse mesh rounds off what a fine one draws, so the two are not the
    /// same width in the archetype either - the extradosed tower's top is 1.03 m wider than its second
    /// level - and demanding they match reported that difference as a fault every time. What has to
    /// hold is that both moved by the same amount, which is what an un-derived level fails: it does
    /// not move at all.
    /// </summary>

    /// <summary>
    /// What to do with the kerb railing on one side: the band it occupies, and where it goes.
    ///
    /// Worked out once, from the mesh that shows the most, and then applied to every mesh of the same
    /// piece. A level of detail draws the same railing with fewer triangles and does not always
    /// resolve the two of them as separate stands - so asked for itself it finds one railing, does
    /// nothing, and keeps a railing the full detail mesh has taken away. That is a railing which is
    /// there from a distance and gone up close, which is what the bridge showed.
    /// </summary>
    private readonly struct KerbPlan
    {
        internal KerbPlan(float side, float from, float to, float shift, bool remove, float3 onto)
        {
            Side = side;
            From = from;
            To = to;
            Shift = shift;
            Remove = remove;
            Onto = onto;
        }

        /// <summary>Which side of the centre this is, as a sign.</summary>
        internal float Side { get; }

        /// <summary>The band the kerb railing occupies, as distances from the centre.</summary>
        internal float From { get; }

        internal float To { get; }

        /// <summary>How far it is carried, when it is kept.</summary>
        internal float Shift { get; }

        /// <summary>Whether it is taken away instead.</summary>
        internal bool Remove { get; }

        /// <summary>The point it is drawn to when it is taken away.</summary>
        internal float3 Onto { get; }

        /// <summary>Whether a vertex belongs to the railing this plan is about.</summary>
        internal bool Covers(float3 vertex) =>
            Math.Sign(vertex.x) == Math.Sign(Side)
            && Math.Abs(vertex.x) >= From - TowerWidening.CentreEpsilon
            && Math.Abs(vertex.x) <= To + TowerWidening.CentreEpsilon;
    }

    private List<KerbPlan>? PlanKerbRailings(
        string name,
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? outline,
        float extra)
    {
        if (source.Length != moved.Length || source.Length == 0) return null;
        if (!BridgeTowers.BringsItsOwnRailings(_styleId)) return null;
        if (!_roadEdges.HasValue) return null;

        var bands = GoldenBridgeRailings.BandsOf(source, -RailingFoot, RailingHead);

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: material between {1:0.##} m and {2:0.##} m above the deck stands at {3}. "
            + "Road width {4:0.##} m; outermost sidewalk widths read from the road prefab: left "
            + "{5:0.##} m, right {6:0.##} m.",
            name,
            -RailingFoot,
            RailingHead,
            bands.Count == 0
                ? "nothing"
                : string.Join(", ", bands.Select(band => string.Format(
                    CultureInfo.InvariantCulture, "{0:0.##}..{1:0.##}", band.From, band.To)))
                + " m from the centre",
            _roadEdges.Value.Left.OuterBoundary + _roadEdges.Value.Right.OuterBoundary,
            _roadEdges.Value.Left.SidewalkWidth,
            _roadEdges.Value.Right.SidewalkWidth));

        if (bands.Count < 2)
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: nothing stands at a kerb here - the deck carries {1} band(s) of railing.",
                name, bands.Count));
            return null;
        }

        var plans = new List<KerbPlan>();
        foreach (var side in new[] { -1f, 1f })
        {
            var edge = side < 0f ? _roadEdges.Value.Left : _roadEdges.Value.Right;
            var footway = edge.SidewalkWidth;
            if (!GoldenBridgeRailings.TryPlan(
                    bands,
                    source,
                    moved,
                    -RailingFoot,
                    RailingHead,
                    edge,
                    side,
                    out var railing))
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: the authored suspension railing layout could not be read; leaving its railings as authored.",
                    name));
                continue;
            }

            var kerb = railing.Layout.Inner;

            if (railing.Remove)
            {
                // No footway on this side, so no railing at its kerb. Every vertex of it is drawn to
                // one point, so every triangle of it has no area and none is rasterised - all three
                // coordinates, or each quad still stands in a plane and is drawn as a sheet.
                var onto = float3.zero;
                var found = false;
                for (var index = 0; index < source.Length; index++)
                {
                    var at = Math.Abs(source[index].x);
                    if (Math.Sign(source[index].x) != Math.Sign(side)) continue;
                    if (at < kerb.From || at > kerb.To) continue;

                    onto = new float3(side * railing.OuterEdgeAfter, moved[index].y, moved[index].z);
                    found = true;
                    break;
                }

                if (!found) continue;

                plans.Add(new KerbPlan(side, kerb.From, kerb.To, 0f, remove: true, onto));
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: no railing at the {1} kerb - that side of the road has no footway. Taking "
                    + "away what stands at {2:0.##}..{3:0.##} m.",
                    name, side < 0f ? "left" : "right", kerb.From, kerb.To));
                continue;
            }

            // Match the two boundary-facing railing edges: the outer railing's outer edge and the
            // inner railing's road-facing edge. Every caller uses the complete sidewalk width.
            var shift = railing.Shift;
            plans.Add(new KerbPlan(side, kerb.From, kerb.To, shift, remove: false, float3.zero));

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: the {1} boundary-facing railing edges are {2:0.###} m apart: the road prefab's "
                + "{3:0.###} m outermost sidewalk, with no fixed deduction. "
                + "Road edge {4:0.###} m, outer railing edge {5:0.###} m; sidewalk inner edge "
                + "{6:0.###} m, inner railing edge {7:0.###} -> {8:0.###} m. What stands at "
                + "{9:0.##}..{10:0.##} m is carried {11:0.###} m, where the deck moved "
                + "{12:0.###} m.",
                name,
                side < 0f ? "left" : "right",
                railing.RailingGap,
                railing.SidewalkWidth,
                railing.RoadOuterBoundary,
                railing.OuterEdgeAfter,
                railing.SidewalkInnerBoundary,
                railing.InnerEdgeBefore,
                railing.InnerTarget,
                kerb.From,
                kerb.To,
                shift,
                extra * 0.5f));
        }

        return plans;
    }

    /// <summary>
    /// Carries out a plan on one mesh, by where its vertices are.
    ///
    /// By position and not by piece, so that a level of detail which does not resolve the two railings
    /// apart is still treated the same as the mesh it stands in for.
    /// </summary>
    private static bool[]? ApplyKerbPlans(
        IReadOnlyList<KerbPlan> plans, float3[] source, float3[] moved, bool[]? protectedSupport = null)
    {
        if (source.Length != moved.Length) return null;

        bool[]? dropped = null;
        for (var index = 0; index < source.Length; index++)
        {
            // Offline archetype membership: railing x bands also overlap the truss below.
            // Never overwrite or remove those already-transformed structural vertices.
            if (protectedSupport != null && protectedSupport[index]) continue;
            foreach (var plan in plans)
            {
                if (!plan.Covers(source[index])) continue;

                if (plan.Remove)
                {
                    // Marked, not moved. What is taken off the bridge is taken out of the index
                    // buffer when the mesh is written; moving it anywhere leaves it in the file.
                    dropped ??= new bool[source.Length];
                    dropped[index] = true;
                }
                else
                {
                    moved[index] = new float3(
                        source[index].x + plan.Shift, moved[index].y, moved[index].z);
                }

                break;
            }
        }

        return dropped;
    }

    /// <summary>How far below deck level a railing may start, and how high it may reach.</summary>
    private const float RailingFoot = 0.5f;

    private const float RailingHead = 3f;

}
