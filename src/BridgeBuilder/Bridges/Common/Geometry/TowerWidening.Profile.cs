using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static partial class TowerWidening
{
    /// <summary>
    /// Where a shape stands relative to the centre line, height by height.
    ///
    /// Built once and then applied to every mesh it covers, which is what lets a scope wider than one
    /// mesh be answered consistently. A tower's part is its own scope. A section's pieces are one
    /// scope between them: they are one structure seen at different points along the span, and a
    /// feature appearing in more than one of them has to move the same way in each. The golden
    /// bridge's cables run through an end piece and a middle piece; measured separately the end opens
    /// wider, because it carries the anchorage and not because its cables sit further apart, and the
    /// same pair of cables was scaled in one piece and carried in the other.
    /// </summary>
    /// <summary>
    /// Where a shape stands relative to the centre line, height by height.
    ///
    /// For each height it records how far out the material that reaches the centre extends - the span
    /// of the crossing member there, zero where nothing crosses. That is what both branches of the
    /// rule need: a crossing member is scaled against its own outer end, and everything beyond that
    /// end is clear of the centre and is carried.
    ///
    /// Built once and applied to every mesh it covers, which is what lets a scope wider than one mesh
    /// be answered consistently. A tower's part is its own scope, together with its levels of detail.
    /// A section's pieces are one scope between them: they are one structure seen at different points
    /// along the span, and a feature appearing in more than one of them has to move the same way in
    /// each. The golden bridge's cables run through an end piece and a middle piece; measured
    /// separately the end opens wider, because it carries the anchorage and not because its cables sit
    /// further apart, and the same pair of cables was scaled in one piece and carried in the other.
    /// </summary>
    internal sealed class Profile
    {
        private readonly float _low;
        private readonly float _height;
        private readonly float[] _span;
        private readonly float[] _outer;
        private readonly List<KeyValuePair<float, float>>?[] _carried;

        private Profile(
            float low,
            float height,
            float[] span,
            float[] outer,
            List<KeyValuePair<float, float>>?[] carried)
        {
            _low = low;
            _height = height;
            _span = span;
            _outer = outer;
            _carried = carried;
        }

        /// <summary>How far out the whole scope reaches, at any height.</summary>
        internal float Outer { get; private set; }

        /// <summary>
        /// Authored left and right reaches of the complete logical transverse assembly. These are
        /// measured from the highest-detail archetype scope and reused by every mesh and LOD; an
        /// individual face, pivot or connector is never allowed to invent a bridge-wide scale.
        /// </summary>
        internal float OpenTrussLeftReach { get; private set; }

        internal float OpenTrussRightReach { get; private set; }

        /// <summary>
        /// Full-detail islands belonging to complete transverse truss groups which cross x=0. Their
        /// prototype footprints carry the same part decision into every level of detail.
        /// </summary>
        private Piece[] OpenTrussLogicalParts { get; set; } = Array.Empty<Piece>();

        /// <summary>
        /// Measured inner face of an open-truss side assembly. The green side arch and its inner
        /// railing lie outside this boundary and are translated together; the top beam crosses x=0
        /// and is stretched up to it. This is prototype geometry, never a fixed road-width constant.
        /// </summary>
        internal float OpenTrussBoundary { get; private set; }

        /// <summary>The profile of one shape, measured from its vertices alone.</summary>
        internal static Profile Of(float3[] vertices) => Of(new[] { vertices });

        /// <summary>The profile of everything in one scope, measured from vertices alone.</summary>
        internal static Profile Of(IReadOnlyList<float3[]> shapes) => Of(shapes, null);

        /// <summary>
        /// The profile of everything in one scope, using the triangles where they are given.
        ///
        /// The triangles are what tell one piece of material from another. Vertices alone say how
        /// close the shape comes to the centre at a height, which is enough to know whether anything
        /// crosses; they cannot say where the crossing member ends and a leg begins, because both are
        /// only numbers on the same line. An edge between two vertices is material between them, so
        /// walking the edges outward from the centre finds where the material stops being continuous.
        ///
        /// Without them the crossing member is taken to reach as far as the scope does, which is right
        /// for a sheet spanning the full width and wrong for anything narrower. That is how the golden
        /// bridge's top deck came out short: it spans to about 12 m between legs standing at 26, and
        /// scaling it against the legs' reach moved its ends less than half as far as the legs went,
        /// tearing a gap either side of it that grew with every metre of road.
        /// </summary>
        internal static Profile Of(
            IReadOnlyList<float3[]> shapes, IReadOnlyList<IReadOnlyList<int>?>? triangles)
        {
            var low = float.MaxValue;
            var high = float.MinValue;
            var outer = 0f;
            var any = false;

            foreach (var shape in shapes)
            {
                foreach (var vertex in shape ?? Array.Empty<float3>())
                {
                    low = Math.Min(low, vertex.y);
                    high = Math.Max(high, vertex.y);
                    outer = Math.Max(outer, Math.Abs(vertex.x));
                    any = true;
                }
            }

            if (!any)
            {
                return new Profile(
                    0f, 0f, new[] { 0f }, new[] { 0f },
                    new List<KeyValuePair<float, float>>?[1]);
            }

            var height = high - low;
            var bands = height <= CentreEpsilon ? 1 : SpanBands;

            // Measure the whole transverse top truss from the highest-detail prototype. It may be
            // represented by many import islands, but it is one logical group across x=0 and every
            // island uses the group's full reach. No centre fitting is measured or transformed as a
            // stand-alone substitute for the truss.
            var openTrussLeftReach = 0f;
            var openTrussRightReach = 0f;
            var openTrussBoundary = 0f;
            var openTrussLogicalParts = new List<Piece>();
            for (var shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                var shape = shapes[shapeIndex] ?? Array.Empty<float3>();
                if (shape.Length == 0) continue;

                openTrussBoundary = Math.Max(
                    openTrussBoundary, ClearSpanOf(shape, SpanBands) * 0.5f);

                var indices = triangles != null && shapeIndex < triangles.Count
                    ? triangles[shapeIndex]
                    : null;
                if (indices == null) continue;

                var logical = LogicalOpenTrussParts(PiecesOf(shape, indices, out _));
                foreach (var component in logical)
                {
                    openTrussLogicalParts.Add(component);
                    openTrussLeftReach = Math.Max(
                        openTrussLeftReach, Math.Max(0f, -component.Left));
                    openTrussRightReach = Math.Max(
                        openTrussRightReach, Math.Max(0f, component.Right));
                }
            }

            // Closest approach and furthest reach per band, which is all the vertices can say.
            var closest = new float[bands];
            var reach = new float[bands];
            for (var band = 0; band < bands; band++) closest[band] = float.MaxValue;

            foreach (var shape in shapes)
            {
                foreach (var vertex in shape ?? Array.Empty<float3>())
                {
                    var band = BandOf(vertex.y, low, height, bands);
                    var distance = Math.Abs(vertex.x);
                    closest[band] = Math.Min(closest[band], distance);
                    reach[band] = Math.Max(reach[band], distance);
                }
            }

            var span = new float[bands];
            for (var band = 0; band < bands; band++)
            {
                span[band] = closest[band] <= CentreEpsilon ? reach[band] : 0f;
            }

            // How thick the outermost run of material is at each height. Nothing else can see whether
            // a wing was carried or scaled: both put its outer edge in the same place, and only its
            // thickness says which happened.
            var thickness = new float[bands];
            var carried = new List<KeyValuePair<float, float>>?[bands];
            if (triangles != null)
                WalkEdges(shapes, triangles, low, height, bands, span, thickness, carried);

            return new Profile(low, height, span, thickness, carried)
            {
                Outer = outer,
                OpenTrussLeftReach = openTrussLeftReach,
                OpenTrussRightReach = openTrussRightReach,
                OpenTrussBoundary = openTrussBoundary,
                OpenTrussLogicalParts = openTrussLogicalParts.ToArray()
            };
        }

        /// <summary>
        /// Reuses the full-detail top-truss classification for the current mesh or LOD. A matching
        /// island takes the complete assembly's affine stretch; nonmatching side material is carried.
        /// </summary>
        internal bool OpenTrussPartCrossesCentre(Piece piece)
        {
            // A real x=0 part is always a stretching part. This check precedes every footprint
            // heuristic and therefore cannot be overridden by axis or LOD classification.
            if (piece.CrossesCentre) return true;
            if (!IsTopTransverseMember(piece)) return false;

            foreach (var authored in OpenTrussLogicalParts)
            {
                if (TouchesTransverseTruss(piece, authored)) return true;
            }
            return false;
        }

        /// <summary>
        /// How far out the material that reaches the centre extends at this height, or zero where
        /// nothing reaches it.
        /// </summary>
        internal float SpanAt(float y) => _span[Band(y)];

        /// <summary>How thick the outermost run of material is at this height, or zero if unknown.</summary>
        internal float OuterThicknessAt(float y) => _outer[Band(y)];

        /// <summary>
        /// Whether material at this height and this distance from the centre belongs to a piece that
        /// never touches the centre, and so is carried whole.
        ///
        /// Asked of the profile rather than of the mesh in hand, because the profile is shared by a
        /// part and every level of detail of it. A coarse mesh welds together what a fine one models
        /// separately, so asked of itself it answers differently - and the two levels then widen
        /// differently, which is a bridge that changes shape as the camera pulls back. Levels of detail
        /// stand in for each other; they do not get their own opinion about what the material is.
        /// </summary>
        internal bool CarriedAt(float y, float distance)
        {
            var ranges = _carried[Band(y)];
            if (ranges == null) return false;

            foreach (var range in ranges)
            {
                if (distance >= range.Key - CentreEpsilon && distance <= range.Value + CentreEpsilon)
                    return true;
            }

            return false;
        }

        private int Band(float y) => BandOf(y, _low, _height, _span.Length);

        /// <summary>
        /// Works out, for each height, how far the material that spans the centre reaches - and where
        /// the legs begin, so that material attached to them stretches to meet them and the legs
        /// themselves are left alone.
        ///
        /// Three things happen at a height, and the merged intervals of the triangles' edges tell them
        /// apart:
        ///
        /// Nothing stands on the centre. Everything there is one side or the other - legs, cables,
        /// railings - and is carried.
        ///
        /// Something stands on the centre and stops short of the legs. It is its own member with a gap
        /// either side, so it is scaled against its own outer end and the gap it was drawn with is the
        /// gap it keeps. A walkway slung between the legs is this.
        ///
        /// Something stands on the centre and runs into the legs. It is attached, so it is scaled
        /// against the leg's inner face and arrives there exactly, while the leg is carried and keeps
        /// its thickness. The golden bridge's top ornament is this: a fan of ribs springing from an
        /// arch, and the arch meets the legs.
        ///
        /// The third case is the reason the legs have to be found, and the reason they cannot be found
        /// at that height alone: where the ornament meets the leg they are one piece of material and
        /// no lateral measurement separates them. What separates them is that a leg is also there
        /// above the ornament and below it, where nothing stands on the centre - so the leg's inner
        /// face is read from the nearest height that has legs and nothing else, and the shape is asked
        /// about itself rather than about the road.
        ///
        /// A shape with no such height is a sheet spanning the full width at every height - the
        /// suspension cables are one - and is scaled about the centre entire.
        /// </summary>
        private static void WalkEdges(
            IReadOnlyList<float3[]> shapes,
            IReadOnlyList<IReadOnlyList<int>?> triangles,
            float low,
            float height,
            int bands,
            float[] span,
            float[] thickness,
            List<KeyValuePair<float, float>>?[] carriedRanges)
        {
            var covered = new List<KeyValuePair<float, float>>?[bands];
            var carriedCovered = new List<KeyValuePair<float, float>>?[bands];

            // How far the material that reaches the centre at each height runs, taken as a whole piece
            // rather than as whatever is visible at that one height.
            var centreReach = new float[bands];

            for (var index = 0; index < shapes.Count; index++)
            {
                var vertices = shapes[index];
                var indices = index < triangles.Count ? triangles[index] : null;
                if (vertices == null || indices == null) continue;

                // Which of this shape's vertices belong to a piece that never touches the centre. Every
                // shape in the scope contributes what it can see: a fine mesh knows the ornament is
                // separate from the leg where a coarse one has welded them, and the union is what both
                // are then widened by.
                var whole = CarriedWhole(vertices, indices, out var pieceReach);

                for (var corner = 0; corner + 2 < indices.Count; corner += 3)
                {
                    var a = indices[corner];
                    var b = indices[corner + 1];
                    var c = indices[corner + 2];
                    if (a < 0 || b < 0 || c < 0) continue;
                    if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length) continue;

                    // A triangle, not three separate edges. A horizontal slab cuts a triangle in a
                    // segment bounded by its edges, so the three of them together say how far the
                    // material reaches at that height - where each on its own says only where one line
                    // is. Taken separately, a solid face leaves nothing at its intermediate heights
                    // but a few isolated points: its two vertical sides and wherever its diagonal
                    // happens to be, with the material between them unrecorded.
                    var low3 = Math.Min(vertices[a].y, Math.Min(vertices[b].y, vertices[c].y));
                    var high3 = Math.Max(vertices[a].y, Math.Max(vertices[b].y, vertices[c].y));
                    var isCarried = whole[a] && whole[b] && whole[c];

                    var firstBand = BandOf(low3, low, height, bands);
                    var lastBand = BandOf(high3, low, height, bands);
                    for (var band = firstBand; band <= lastBand; band++)
                    {
                        // Signed first, folded after. A cross-section running from one side of the
                        // centre to the other covers the centre, and folding each edge to its distance
                        // before taking the union loses that: a vertical edge just past the middle
                        // reads as material standing 0.08 m clear of it, when the triangle it belongs
                        // to plainly crosses.
                        var leftMost = float.MaxValue;
                        var rightMost = float.MinValue;
                        for (var side = 0; side < 3; side++)
                        {
                            var one = vertices[indices[corner + side]];
                            var two = vertices[indices[corner + ((side + 1) % 3)]];
                            if (BandOf(Math.Max(one.y, two.y), low, height, bands) < band) continue;
                            if (BandOf(Math.Min(one.y, two.y), low, height, bands) > band) continue;

                            var (edgeLeft, edgeRight) = Within(one, two, band, low, height, bands);
                            leftMost = Math.Min(leftMost, edgeLeft);
                            rightMost = Math.Max(rightMost, edgeRight);
                        }

                        if (leftMost > rightMost) continue;

                        var from = leftMost < -CentreEpsilon && rightMost > CentreEpsilon
                            ? 0f
                            : Math.Min(Math.Abs(leftMost), Math.Abs(rightMost));
                        var to = Math.Max(Math.Abs(leftMost), Math.Abs(rightMost));

                        (covered[band] ??= new List<KeyValuePair<float, float>>())
                            .Add(new KeyValuePair<float, float>(from, to));
                        if (from <= CentreEpsilon)
                        {
                            centreReach[band] = Math.Max(centreReach[band], pieceReach[a]);
                        }
                        if (isCarried)
                        {
                            (carriedCovered[band] ??= new List<KeyValuePair<float, float>>())
                                .Add(new KeyValuePair<float, float>(from, to));
                        }
                    }
                }
            }

            // The carried material at each height, merged into ranges so a level of detail can be
            // asked about a place rather than about its own topology.
            for (var band = 0; band < bands; band++)
            {
                var pieces = carriedCovered[band];
                if (pieces == null || pieces.Count == 0) continue;

                pieces.Sort((left, right) => left.Key.CompareTo(right.Key));
                var mergedCarried = new List<KeyValuePair<float, float>>();
                var start = pieces[0].Key;
                var end = pieces[0].Value;
                for (var at = 1; at < pieces.Count; at++)
                {
                    if (pieces[at].Key <= end + CentreEpsilon)
                    {
                        end = Math.Max(end, pieces[at].Value);
                        continue;
                    }

                    mergedCarried.Add(new KeyValuePair<float, float>(start, end));
                    start = pieces[at].Key;
                    end = pieces[at].Value;
                }

                mergedCarried.Add(new KeyValuePair<float, float>(start, end));
                carriedRanges[band] = mergedCarried;
            }

            // The merged runs at each height, outermost last.
            var runs = new List<KeyValuePair<float, float>>?[bands];
            for (var band = 0; band < bands; band++)
            {
                var intervals = covered[band];
                if (intervals == null || intervals.Count == 0) continue;

                intervals.Sort((left, right) => left.Key.CompareTo(right.Key));
                var merged = new List<KeyValuePair<float, float>>();
                var from = intervals[0].Key;
                var to = intervals[0].Value;
                for (var at = 1; at < intervals.Count; at++)
                {
                    if (intervals[at].Key <= to + CentreEpsilon)
                    {
                        to = Math.Max(to, intervals[at].Value);
                        continue;
                    }

                    merged.Add(new KeyValuePair<float, float>(from, to));
                    from = intervals[at].Key;
                    to = intervals[at].Value;
                }

                merged.Add(new KeyValuePair<float, float>(from, to));
                runs[band] = merged;

                // Only material standing clear of the centre has a thickness worth keeping. Where the
                // outermost run reaches the centre it is the spanning member itself, and its extent
                // changes with the widening by design - reporting that as a leg that lost its shape is
                // what the V pylon.s own top did when it was brought in.
                var outermost = merged[merged.Count - 1];
                thickness[band] = outermost.Key > CentreEpsilon ? outermost.Value - outermost.Key : 0f;
            }

            // Where the legs stand: read at the heights that have legs and nothing else, which is to
            // say the heights where nothing reaches the centre.
            var legInner = new float[bands];
            var known = new bool[bands];
            for (var band = 0; band < bands; band++)
            {
                var merged = runs[band];
                if (merged == null || merged.Count == 0) continue;
                if (merged[0].Key <= CentreEpsilon) continue;

                // The innermost material at a height where nothing crosses: that is the face the road
                // passes, whatever else stands outside it. Reading the outermost run.s start instead
                // gave 26 where the leg's inner face was 22, on a band whose only edges were the two
                // vertical ones - runs of no width, the outer of which starts at the outer face.
                legInner[band] = merged[0].Key;
                known[band] = true;
            }

            for (var band = 0; band < bands; band++)
            {
                var merged = runs[band];
                if (merged == null || merged.Count == 0) continue;

                // Nothing on the centre: all of it is carried.
                if (merged[0].Key > CentreEpsilon)
                {
                    span[band] = 0f;
                    continue;
                }

                var outerStart = merged[merged.Count - 1].Key;
                var reach = merged[merged.Count - 1].Value;
                var leg = Nearest(legInner, known, band, bands);

                // The material on the centre here belongs to something that runs out as far as the
                // legs. It is attached to them, so it is scaled against the leg's inner face and
                // arrives there exactly, while the leg is carried and keeps its thickness.
                //
                // This is the golden bridge's top ornament - a fan of ribs on an arch, air between
                // every rib. At the heights between ribs it looks like a narrow member standing alone,
                // and scaling it against its own end blew its central spoke up twentyfold at those
                // heights and hardly at all where the arch stood beside it: a diamond. What it looks
                // like at one height says nothing; what the piece it belongs to reaches says
                // everything.
                if (leg > CentreEpsilon
                    && (outerStart < leg - CentreEpsilon || centreReach[band] >= leg - CentreEpsilon))
                {
                    span[band] = leg;
                    continue;
                }

                // The outermost run is the leg itself, or there is no leg to speak of, and the
                // material on the centre stops with air beyond it. It is its own member: scaled
                // against its own end, and the gap it was drawn with is the gap it keeps. A walkway
                // slung between the legs is this.
                //
                // Its own end, and not its end at this height. A member reaches different distances at
                // different heights - an ornament's central spoke stands alone where the ribs beside
                // it leave air, and stands beside the arch lower down - so scaling it against what it
                // happens to reach here gives it a different ratio at every height and it comes out a
                // kite. How far the piece of material reaches is a fact about the member; how far it
                // reaches at one height is a fact about where you cut it.
                if (merged.Count > 1)
                {
                    span[band] = Math.Max(merged[0].Value, centreReach[band]);
                    continue;
                }

                // One run from the centre to the outer edge with no leg anywhere in the shape: a sheet
                // spanning the full width, scaled about the centre entire. The suspension cables are
                // one of these.
                span[band] = reach;
            }
        }

        /// <summary>
        /// Where an edge runs, across the bridge, while it is inside one band - measured from where it
        /// actually is at those heights rather than from where its ends are, and signed.
        /// </summary>
        private static (float From, float To) Within(
            float3 one, float3 two, int band, float low, float height, int bands)
        {
            var step = bands <= 1 || height <= CentreEpsilon ? height : height / bands;
            var bottom = low + (band * step);
            var top = bottom + step;

            var lowY = Math.Min(one.y, two.y);
            var highY = Math.Max(one.y, two.y);
            var from = Math.Max(bottom, lowY);
            var to = Math.Min(top, highY);
            if (to < from) to = from;

            // A level edge is at both of its ends at once - it has a lateral extent rather than a
            // position - so interpolating it would discard everything but one end of it.
            var level = Math.Abs(two.y - one.y) <= CentreEpsilon;
            var atFrom = level ? one.x : AtHeight(one, two, from);
            var atTo = level ? two.x : AtHeight(one, two, to);

            // Signed, and left to the caller to fold: where this edge runs is a fact about the edge,
            // and whether the material it belongs to reaches the centre is a fact about the triangle.
            return (Math.Min(atFrom, atTo), Math.Max(atFrom, atTo));
        }

        /// <summary>Where an edge is, across the bridge, at one height along it.</summary>
        private static float AtHeight(float3 one, float3 two, float y)
        {
            var rise = two.y - one.y;
            if (Math.Abs(rise) <= CentreEpsilon) return one.x;

            var along = (y - one.y) / rise;
            if (along < 0f) along = 0f;
            else if (along > 1f) along = 1f;

            return one.x + ((two.x - one.x) * along);
        }

        /// <summary>The nearest height that has legs and nothing across the centre, searched both ways.</summary>
        private static float Nearest(float[] values, bool[] known, int band, int bands)
        {
            for (var away = 0; away < bands; away++)
            {
                var below = band - away;
                if (below >= 0 && known[below]) return values[below];

                var above = band + away;
                if (above < bands && known[above]) return values[above];
            }

            return 0f;
        }
    }



}
