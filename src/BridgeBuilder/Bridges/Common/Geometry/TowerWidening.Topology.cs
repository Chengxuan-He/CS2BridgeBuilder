using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static partial class TowerWidening
{


    /// <summary>
    /// Which vertices belong to a piece of material that never touches the centre line, anywhere along
    /// its own height.
    ///
    /// The crossing question is asked per height, because a pylon's opening is a different number at
    /// every height. Material is not built per height: a cable plane, a railing, a leg runs vertically
    /// through many of them. Where the answer changes from one band to the next, such a piece is
    /// scaled at its bottom and carried at its top, and it comes out sheared - and its neighbours,
    /// moving by different amounts on either side of the same boundary, close the gaps between them
    /// and merge.
    ///
    /// The golden bridge's cable section is one band of crossing at deck level and sixteen of open air
    /// above it. Its cables and railings pass through that boundary, and a railing 0.18 m thick came
    /// out 1.89 m thick where it had run into the one beside it.
    ///
    /// So the question is asked of the material rather than of the height. A piece of material that
    /// stands clear of the centre over its whole extent is carried entire, whatever band any part of
    /// it falls in. Only material that does reach the centre is left to the per-height rule, where the
    /// question of what it spans between genuinely does depend on the height.
    /// </summary>
    internal static bool[] CarriedWhole(float3[] vertices, IReadOnlyList<int>? triangles) =>
        CarriedWhole(vertices, triangles, out _);

    /// <summary>
    /// As above, and how far from the centre each vertex.s own piece of material reaches at its widest.
    ///
    /// That is what tells an ornament from a member hung between the legs. Both cross the centre with
    /// air either side of them at the height in question; the ornament is part of something that runs
    /// all the way out to the legs, and the hung member ends before it gets there. Scaled against its
    /// own end, the ornament.s central spoke - half a metre wide, where the thing it belongs to is
    /// fourteen - was blown up by a factor of twenty at the heights where nothing stood beside it, and
    /// hardly at all where the arch did. It came out a diamond.
    /// </summary>
    internal static bool[] CarriedWhole(
        float3[] vertices, IReadOnlyList<int>? triangles, out float[] reach)
    {
        var carried = new bool[vertices.Length];
        reach = new float[vertices.Length];
        if (triangles == null || vertices.Length == 0) return carried;

        var parent = new int[vertices.Length];
        for (var index = 0; index < parent.Length; index++) parent[index] = index;

        int Root(int of)
        {
            while (parent[of] != of)
            {
                parent[of] = parent[parent[of]];
                of = parent[of];
            }

            return of;
        }

        void Join(int one, int two)
        {
            var first = Root(one);
            var second = Root(two);
            if (first != second) parent[first] = second;
        }

        // Welded by position first. A game mesh splits its vertices at every hard edge - they carry
        // normals and texture coordinates as well as a position - so two faces meeting along a seam
        // have four vertices there and no index in common. Joined by index alone, a shape comes apart
        // into smoothing groups rather than into pieces of material, and a leg that meets a crossbeam
        // at a sharp corner reads as separate from it.
        var welded = new Dictionary<long, int>();
        for (var index = 0; index < vertices.Length; index++)
        {
            var key = WeldKey(vertices[index]);
            if (welded.TryGetValue(key, out var first)) Join(index, first);
            else welded[key] = index;
        }

        for (var corner = 0; corner + 2 < triangles.Count; corner += 3)
        {
            var a = triangles[corner];
            var b = triangles[corner + 1];
            var c = triangles[corner + 2];
            if (a < 0 || b < 0 || c < 0) continue;
            if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length) continue;

            Join(a, b);
            Join(b, c);
        }

        // Whether each piece of material reaches the centre: either it has vertices on both sides of
        // it, or it stands on it.
        var left = new bool[vertices.Length];
        var right = new bool[vertices.Length];
        var touches = new bool[vertices.Length];

        for (var index = 0; index < vertices.Length; index++)
        {
            var root = Root(index);
            var x = vertices[index].x;
            if (x > CentreEpsilon) right[root] = true;
            else if (x < -CentreEpsilon) left[root] = true;
            else touches[root] = true;
        }

        var widest = new float[vertices.Length];
        for (var index = 0; index < vertices.Length; index++)
        {
            var root = Root(index);
            widest[root] = Math.Max(widest[root], Math.Abs(vertices[index].x));
        }

        for (var index = 0; index < vertices.Length; index++)
        {
            var root = Root(index);
            carried[index] = !touches[root] && !(left[root] && right[root]);
            reach[index] = widest[root];
        }

        return carried;
    }


    /// <summary>One connected piece of material, and where it sits.</summary>
    internal readonly struct Piece
    {
        internal Piece(
            int id, float left, float right, float low, float high, float back, float front)
        {
            Id = id;
            Left = left;
            Right = right;
            Low = low;
            High = high;
            Back = back;
            Front = front;
        }

        /// <summary>Which piece this is, as <see cref="PiecesOf"/> labelled its vertices.</summary>
        internal int Id { get; }

        /// <summary>How far it reaches to each side, signed.</summary>
        internal float Left { get; }

        internal float Right { get; }

        /// <summary>How high it stands.</summary>
        internal float Low { get; }

        internal float High { get; }

        /// <summary>Longitudinal bounds used to recognise one transverse truss assembly.</summary>
        internal float Back { get; }

        internal float Front { get; }

        /// <summary>Whether it is entirely on one side of the centre line.</summary>
        internal bool Aside => (Left > CentreEpsilon && Right > CentreEpsilon)
            || (Left < -CentreEpsilon && Right < -CentreEpsilon);

        /// <summary>Whether this import island itself touches or straddles the centre.</summary>
        internal bool CrossesCentre => Left <= CentreEpsilon && Right >= -CentreEpsilon;

        /// <summary>How far out it reaches, whichever side it is on.</summary>
        internal float Outer => Math.Max(Math.Abs(Left), Math.Abs(Right));

        /// <summary>How far in it reaches.</summary>
        internal float Inner => Math.Min(Math.Abs(Left), Math.Abs(Right));

        internal float LateralSpan => Right - Left;

        internal float VerticalSpan => High - Low;

        internal float LongitudinalSpan => Front - Back;
    }

    /// <summary>
    /// Labels each vertex with the piece of material it belongs to, and describes each piece.
    ///
    /// The same welding and the same connectivity <see cref="CarriedWhole"/> uses - a game mesh splits
    /// its vertices at every hard edge, so two faces meeting along a seam have no index in common and
    /// have to be joined by where they are.
    ///
    /// What this is for is moving one piece on its own. A railing standing at the kerb is a piece; the
    /// railing at the deck's edge is another; whether the first is there at all, and where it stands,
    /// is a question about the road underneath rather than about the archetype.
    /// </summary>
    internal static Piece[] PiecesOf(float3[] vertices, IReadOnlyList<int>? triangles, out int[] labels)
    {
        labels = new int[vertices.Length];
        if (triangles == null || vertices.Length == 0) return Array.Empty<Piece>();

        var parent = new int[vertices.Length];
        for (var index = 0; index < parent.Length; index++) parent[index] = index;

        int Root(int of)
        {
            while (parent[of] != of)
            {
                parent[of] = parent[parent[of]];
                of = parent[of];
            }

            return of;
        }

        void Join(int one, int two)
        {
            var first = Root(one);
            var second = Root(two);
            if (first != second) parent[first] = second;
        }

        var welded = new Dictionary<long, int>();
        for (var index = 0; index < vertices.Length; index++)
        {
            var key = WeldKey(vertices[index]);
            if (welded.TryGetValue(key, out var first)) Join(index, first);
            else welded[key] = index;
        }

        for (var corner = 0; corner + 2 < triangles.Count; corner += 3)
        {
            var a = triangles[corner];
            var b = triangles[corner + 1];
            var c = triangles[corner + 2];
            if (a < 0 || b < 0 || c < 0) continue;
            if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length) continue;

            Join(a, b);
            Join(b, c);
        }

        var numbered = new Dictionary<int, int>();
        var left = new List<float>();
        var right = new List<float>();
        var low = new List<float>();
        var high = new List<float>();
        var back = new List<float>();
        var front = new List<float>();

        for (var index = 0; index < vertices.Length; index++)
        {
            var root = Root(index);
            if (!numbered.TryGetValue(root, out var id))
            {
                id = left.Count;
                numbered[root] = id;
                left.Add(float.MaxValue);
                right.Add(float.MinValue);
                low.Add(float.MaxValue);
                high.Add(float.MinValue);
                back.Add(float.MaxValue);
                front.Add(float.MinValue);
            }

            labels[index] = id;
            left[id] = Math.Min(left[id], vertices[index].x);
            right[id] = Math.Max(right[id], vertices[index].x);
            low[id] = Math.Min(low[id], vertices[index].y);
            high[id] = Math.Max(high[id], vertices[index].y);
            back[id] = Math.Min(back[id], vertices[index].z);
            front[id] = Math.Max(front[id], vertices[index].z);
        }

        var pieces = new Piece[left.Count];
        for (var id = 0; id < pieces.Length; id++)
        {
            pieces[id] = new Piece(
                id, left[id], right[id], low[id], high[id], back[id], front[id]);
        }

        return pieces;
    }

    /// <summary>
    /// Finds the complete top-truss assembly starting at the parts which actually touch x=0.
    ///
    /// TrussArch01 imports one transverse truss as many islands: rods, plates, pivots and their hard
    /// edge faces. The centre-line rule is decided by that authored assembly, not by the import
    /// islands, so the centre islands seed a walk through the transverse members which physically
    /// meet them. The walk is deliberately one-way: it may leave the centre only through a member
    /// whose longest axis is x. A side arch, end plate or upright may touch a top rod, but its longest
    /// axis is longitudinal or vertical and the walk must stop there. That is the distinction the old
    /// all-pairs union lost when it absorbed hundreds of side islands into the top truss.
    /// </summary>
    private static Piece[] LogicalOpenTrussParts(Piece[] pieces)
    {
        if (pieces.Length == 0) return Array.Empty<Piece>();

        var inTopTruss = new bool[pieces.Length];
        var pending = new Queue<int>();
        for (var index = 0; index < pieces.Length; index++)
        {
            if (!pieces[index].CrossesCentre) continue;
            inTopTruss[index] = true;
            pending.Enqueue(index);
        }

        while (pending.Count > 0)
        {
            var from = pending.Dequeue();
            for (var candidate = 0; candidate < pieces.Length; candidate++)
            {
                if (inTopTruss[candidate] || !IsTopTransverseMember(pieces[candidate])) continue;
                if (!TouchesTransverseTruss(pieces[from], pieces[candidate])) continue;

                inTopTruss[candidate] = true;
                pending.Enqueue(candidate);
            }
        }

        var logical = new List<Piece>();
        for (var index = 0; index < pieces.Length; index++)
        {
            if (inTopTruss[index]) logical.Add(pieces[index]);
        }
        return logical.ToArray();
    }

    /// <summary>
    /// Whether an off-centre island can be a member of the transverse top truss. It must be x-led
    /// against both other axes. Comparing x only with z, as the previous implementation did, called
    /// a tall end plate "transverse" merely because it was thin longitudinally and stretched the
    /// entire side structure.
    /// </summary>
    private static bool IsTopTransverseMember(Piece piece) =>
        piece.LateralSpan + CentreEpsilon >= piece.LongitudinalSpan
        && piece.LateralSpan + CentreEpsilon >= piece.VerticalSpan;

    private static float AxisGap(float firstLow, float firstHigh, float secondLow, float secondHigh) =>
        firstHigh < secondLow
            ? secondLow - firstHigh
            : secondHigh < firstLow
                ? firstLow - secondHigh
                : 0f;

    private static bool TouchesTransverseTruss(Piece one, Piece two)
    {
        // A diagonal brace can be authored with a deliberate gap between its rod and the centre plate,
        // so the x-axis reach must be allowed to bridge that importer gap. The lateral member's own
        // length is valid for x only. It must never become the y/z tolerance: doing that was the old
        // all-pairs bug which joined unrelated side structure several metres above or along the span.
        var oneJoint = Math.Min(one.VerticalSpan, one.LongitudinalSpan);
        var twoJoint = Math.Min(two.VerticalSpan, two.LongitudinalSpan);
        var crossSection = Math.Max(CentreEpsilon, Math.Max(oneJoint, twoJoint));
        var lateralGap = Math.Max(
            crossSection, Math.Max(one.LateralSpan, two.LateralSpan));
        var longitudinalGap = Math.Max(
            crossSection, Math.Max(one.LongitudinalSpan, two.LongitudinalSpan));

        return AxisGap(one.Left, one.Right, two.Left, two.Right) <= lateralGap + CentreEpsilon
            && AxisGap(one.Low, one.High, two.Low, two.High) <= crossSection + CentreEpsilon
            && AxisGap(one.Back, one.Front, two.Back, two.Front) <= longitudinalGap + CentreEpsilon;
    }

    /// <summary>Where a vertex sits, to a millimetre, as a key two vertices can share.</summary>
    private static long WeldKey(float3 vertex) =>
        (((long)Math.Round(vertex.x * 1000f) & 0x1FFFFF) << 42)
        | (((long)Math.Round(vertex.y * 1000f) & 0x1FFFFF) << 21)
        | ((long)Math.Round(vertex.z * 1000f) & 0x1FFFFF);

    /// <summary>Which height band a coordinate falls in.</summary>
    private static int BandOf(float y, float low, float height, int bands)
    {
        if (bands <= 1 || height <= CentreEpsilon) return 0;

        var band = (int)((y - low) / height * bands);
        if (band < 0) return 0;
        return band >= bands ? bands - 1 : band;
    }

}
