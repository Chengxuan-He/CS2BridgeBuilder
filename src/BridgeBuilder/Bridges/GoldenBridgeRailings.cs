using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>An actual outer road section edge, measured from the target road prefab at generation.</summary>
internal readonly struct RoadEdge
{
    internal RoadEdge(float outerBoundary, float innerBoundary, bool isSidewalk)
        : this(outerBoundary, outerBoundary, innerBoundary, isSidewalk)
    {
    }

    internal RoadEdge(
        float outerBoundary,
        float sidewalkOuterBoundary,
        float sidewalkInnerBoundary,
        bool isSidewalk)
    {
        OuterBoundary = Math.Max(0f, outerBoundary);
        SidewalkOuterBoundary = Math.Max(0f, sidewalkOuterBoundary);
        InnerBoundary = Math.Max(0f, sidewalkInnerBoundary);
        IsSidewalk = isSidewalk;
    }

    /// <summary>The road surface edge, measured from x=0.</summary>
    internal float OuterBoundary { get; }

    /// <summary>The edge of the outermost sidewalk that is farthest from x=0.</summary>
    internal float SidewalkOuterBoundary { get; }

    /// <summary>The edge of that same sidewalk that is nearest x=0.</summary>
    internal float InnerBoundary { get; }
    internal bool IsSidewalk { get; }
    internal float SidewalkWidth => IsSidewalk
        ? Math.Max(0f, SidewalkOuterBoundary - InnerBoundary)
        : 0f;
}

/// <summary>
/// Finds the golden suspension bridge's two boundary-facing railing edges and places only its
/// removable inner railing.
/// </summary>
internal static class GoldenBridgeRailings
{
    private const float Bucket = 0.25f;
    /// <summary>The golden bridge's gap between the road surface and the sidewalk platform.</summary>
    internal const float RoadSurfaceGap = 1f;
    internal readonly struct Band
    {
        internal Band(float from, float to)
        {
            From = from;
            To = to;
        }

        internal float From { get; }
        internal float To { get; }

        internal bool Covers(float absoluteX) => absoluteX >= From && absoluteX <= To;
    }

    internal readonly struct Layout
    {
        internal Layout(Band inner, Band outer)
        {
            Inner = inner;
            Outer = outer;
        }

        /// <summary>The only band that may be moved or removed.</summary>
        internal Band Inner { get; }

        /// <summary>The deck-edge outer railing, present on both sides of the bridge.</summary>
        internal Band Outer { get; }
    }

    internal readonly struct Plan
    {
        internal Plan(
            Layout layout,
            float side,
            RoadEdge roadEdge,
            float innerEdgeBefore,
            float outerEdgeAfter)
        {
            Layout = layout;
            Side = Math.Sign(side);
            RoadEdge = roadEdge;
            InnerEdgeBefore = innerEdgeBefore;
            OuterEdgeAfter = outerEdgeAfter;
        }

        internal Layout Layout { get; }
        internal float Side { get; }
        internal RoadEdge RoadEdge { get; }
        internal float SidewalkWidth => RoadEdge.SidewalkWidth;
        /// <summary>The inner railing edge nearest the road centre, before it is fitted.</summary>
        internal float InnerEdgeBefore { get; }

        /// <summary>The outer railing edge nearest the road boundary, after deck widening.</summary>
        internal float OuterEdgeAfter { get; }
        internal bool Remove => !RoadEdge.IsSidewalk || SidewalkWidth <= 0f;
        internal float RailingGap => Math.Max(0f, SidewalkWidth - RoadSurfaceGap);
        internal float RoadOuterBoundary => RoadEdge.OuterBoundary;
        internal float SidewalkInnerBoundary => RoadEdge.InnerBoundary;
        internal float OuterInset => RoadOuterBoundary - OuterEdgeAfter;
        internal float InnerTarget => OuterEdgeAfter - RailingGap;
        internal float Shift => Remove ? 0f : Side * (InnerTarget - InnerEdgeBefore);
    }

    /// <summary>The occupied absolute-X bands at railing height, from the centre outwards.</summary>
    internal static IReadOnlyList<Band> BandsOf(float3[] vertices, float low, float high)
    {
        var occupied = new SortedSet<int>();
        foreach (var vertex in vertices)
        {
            if (vertex.y <= low || vertex.y >= high) continue;
            occupied.Add((int)Math.Floor(Math.Abs(vertex.x) / Bucket));
        }

        var bands = new List<Band>();
        var start = int.MinValue;
        var previous = int.MinValue;
        foreach (var slot in occupied)
        {
            if (start == int.MinValue)
            {
                start = previous = slot;
                continue;
            }

            if (slot == previous + 1)
            {
                previous = slot;
                continue;
            }

            bands.Add(new Band(start * Bucket, (previous + 1) * Bucket));
            start = previous = slot;
        }

        if (start != int.MinValue)
            bands.Add(new Band(start * Bucket, (previous + 1) * Bucket));

        return bands;
    }

    /// <summary>
    /// Selects the deck-edge outer railing as the band whose widened outer edge is closest to the
    /// target road prefab's measured outer boundary, ignoring suspension structure beyond it, and
    /// computes the inner railing from the two corresponding boundary-facing mesh edges. The
    /// outermost edge of the outer railing and the innermost edge of the inner railing span the target
    /// road's sidewalk width less the golden bridge's one-metre road-surface gap; using either band's
    /// centre or the inner band's outward edge changes that span.
    /// </summary>
    internal static bool TryPlan(
        IReadOnlyList<Band> bands,
        float3[] source,
        float3[] moved,
        float low,
        float high,
        RoadEdge roadEdge,
        float side,
        out Plan plan)
    {
        plan = default;
        if (bands.Count < 2 || source.Length != moved.Length || Math.Abs(side) < 0.5f)
            return false;

        var outerAt = -1;
        var outerEdgeAfter = 0f;
        var closest = float.MaxValue;
        for (var index = 1; index < bands.Count; index++)
        {
            if (!TryEdge(
                    bands[index], source, moved, low, high, side, outermost: true,
                    out var candidateEdge))
                continue;

            var distance = Math.Abs(candidateEdge - roadEdge.OuterBoundary);
            if (distance >= closest) continue;
            closest = distance;
            outerAt = index;
            outerEdgeAfter = candidateEdge;
        }

        if (outerAt <= 0) return false;

        var layout = new Layout(bands[outerAt - 1], bands[outerAt]);
        if (!TryEdge(
                layout.Inner, source, source, low, high, side, outermost: false,
                out var innerEdgeBefore))
            return false;

        plan = new Plan(
            layout,
            side,
            roadEdge,
            innerEdgeBefore,
            outerEdgeAfter);
        return true;
    }

    private static bool TryEdge(
        Band band,
        float3[] source,
        float3[] positioned,
        float low,
        float high,
        float side,
        bool outermost,
        out float edge)
    {
        var minimum = float.MaxValue;
        var maximum = float.MinValue;
        for (var index = 0; index < source.Length; index++)
        {
            var vertex = source[index];
            if (vertex.y <= low || vertex.y >= high || Math.Sign(vertex.x) != Math.Sign(side))
                continue;

            if (!band.Covers(Math.Abs(vertex.x))) continue;
            var absoluteX = Math.Abs(positioned[index].x);
            minimum = Math.Min(minimum, absoluteX);
            maximum = Math.Max(maximum, absoluteX);
        }

        if (minimum == float.MaxValue)
        {
            edge = 0f;
            return false;
        }

        edge = outermost ? maximum : minimum;
        return true;
    }
}
