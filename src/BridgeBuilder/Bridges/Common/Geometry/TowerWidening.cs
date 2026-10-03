using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Widens a tower by moving its vertices apart, without changing anything else about it.
///
/// The rule is one line: every vertex moves outward, along x, by half the extra width, in whichever
/// direction it already lies. Vertices on the centre line do not move.
///
/// <code>
///   x' = x + sign(x) * delta / 2
/// </code>
///
/// Three properties follow from that, and they are the reasons it is this and not a scale.
///
/// It is the identity when the tower is already the right width. Delta is zero, no vertex moves, and
/// the result is the original mesh vertex for vertex - which is the standard this has to meet: a
/// generated tower for a road the game already has a bridge for must be that bridge's tower.
///
/// It does not deform the legs. A scale would thicken them in proportion; a translation carries each
/// leg across rigidly, so a widened tower has the same legs as the one it came from, just further
/// apart. The beam between them is the only thing that changes length, which is what widening a
/// portal physically means.
///
/// It leaves normals correct. Translation does not rotate anything, so every face keeps the normal it
/// had. Only the beam's texture stretches, along the one axis it was already tiled on.
///
/// It takes plain vertex arrays rather than a mesh on purpose: nothing here touches the engine, so the
/// rule can be tested without a running game, which is the only place the property above can be
/// checked cheaply enough to check on every build.
/// </summary>
internal static partial class TowerWidening
{
    /// <summary>A vertex closer than this to the centre line is on it, and stays put.</summary>
    internal const float CentreEpsilon = 0.001f;

    /// <summary>
    /// A copy of <paramref name="vertices"/> widened by <paramref name="extra"/> metres. The input is
    /// never modified, so a caller can compare before against after.
    /// </summary>
    internal static float3[] Widen(float3[] vertices, float extra)
    {
        var result = new float3[vertices.Length];
        Array.Copy(vertices, result, vertices.Length);
        if (Math.Abs(extra) < CentreEpsilon) return result;

        for (var index = 0; index < result.Length; index++)
        {
            result[index].x = Spread(result[index].x, extra);
        }

        return result;
    }

    /// <summary>
    /// One coordinate carried outward by half of <paramref name="extra"/>, away from the centre line.
    ///
    /// Everything that has to stay attached to the tower moves through here, not just the tower's own
    /// vertices: the cables, the deck props, anything the donor bridge placed out to either side. They
    /// were authored to meet the legs of a particular tower, so they only keep meeting them if they
    /// travel the same distance in the same direction. Moving the legs by a translation while moving
    /// the cables by a scale is what left the cables hanging over the carriageway instead of down
    /// either side of it - two rules for one bridge.
    /// </summary>
    internal static float Spread(float x, float extra)
    {
        if (Math.Abs(x) <= CentreEpsilon) return x;

        var shift = extra * 0.5f;
        return x + (x > 0f ? shift : -shift);
    }

    /// <summary>
    /// A portal widened properly: the legs carried apart, the span between them stretched to follow.
    ///
    /// <see cref="Spread"/> alone is right for anything that belongs to one leg and wrong for anything
    /// crossing between them, and a portal is both. Its legs stand outside the carriageway and its
    /// crossbeams run from one to the other, straight through the centre line - and a rule built on
    /// sign(x) has a discontinuity exactly there. Every vertex left of centre jumps one way, every
    /// vertex right of it jumps the other, and the beams that spanned the middle are torn open by
    /// precisely <paramref name="extra"/> metres. Small shifts look like nothing; large ones look like
    /// the tower has come apart, which is why it appeared to have a width beyond which it broke.
    ///
    /// <paramref name="inner"/> is where the legs begin - half the road, since the road is what passes
    /// between them. Outside it a vertex belongs to a leg and moves rigidly, so the legs keep their
    /// shape and thickness; inside it a vertex belongs to the span and moves in proportion, so the
    /// beams stay attached at both ends. The two agree at the boundary, so nothing tears anywhere.
    ///
    /// At the authored width the shift is zero and the ratio is one: the tower comes back unchanged,
    /// which is the property everything else here is held to.
    /// </summary>
    internal static float3[] Widen(float3[] vertices, float extra, float inner)
    {
        var result = new float3[vertices.Length];
        Array.Copy(vertices, result, vertices.Length);
        if (Math.Abs(extra) < CentreEpsilon) return result;

        // Nothing sensible to divide by: fall back to carrying the halves apart, which is what this
        // did before the span was accounted for.
        if (inner <= CentreEpsilon) return Widen(vertices, extra);

        var shift = extra * 0.5f;
        var ratio = (inner + shift) / inner;

        for (var index = 0; index < result.Length; index++)
        {
            var x = result[index].x;
            if (Math.Abs(x) < inner)
            {
                result[index].x = x * ratio;
                continue;
            }

            // Carried outward, and never past the centre. A leg brought in by more than it stands out
            // lands on the far side and the two legs swap - the same inversion the ratio guard catches
            // on the other branch, arrived at by the other arithmetic. Both stop at zero.
            var moved = x + (x > 0f ? shift : -shift);
            result[index].x = x > 0f ? Math.Max(0f, moved) : Math.Min(0f, moved);
        }

        return result;
    }

    /// <summary>
    /// Applies and enforces the contract's exact side-part mapping. TrussArch01's authored base is
    /// never allowed to enter a scale/profile path: every non-zero coordinate moves by the same signed
    /// delta and x=0 remains x=0.
    /// </summary>
    internal static float3[] WidenRigidBase(float3[] vertices, float extra)
    {
        return Widen(vertices, extra);
    }

    /// <summary>
    /// A copy of <paramref name="vertices"/> stretched from <paramref name="width"/> to
    /// <paramref name="width"/> plus <paramref name="extra"/>, every coordinate moving in proportion.
    ///
    /// The rule for a continuous surface, and the opposite of what <see cref="Widen"/> does. A portal is
    /// two legs with nothing between them, so its halves are carried apart rigidly and the gap in the
    /// middle simply grows. A net piece is a sheet spanning its whole width - the cables, the hangers
    /// and whatever ties them together are one surface - and carrying its halves apart tears it: every
    /// vertex left of centre jumps one way, every vertex right of it jumps the other, and the triangles
    /// that straddled the centre line are stretched across the gap that opens between them. On screen
    /// that is not cables in the wrong place; it is long shards lying over the carriageway.
    ///
    /// Stretching moves the far edge exactly as far as the rigid rule would, so the cables still land
    /// where the widened tower's legs are, and everything between simply spreads. At the authored width
    /// the ratio is one and nothing moves at all, which is the same standard the rigid rule is held to.
    /// </summary>
    internal static float3[] Stretch(float3[] vertices, float width, float extra)
    {
        var result = new float3[vertices.Length];
        Array.Copy(vertices, result, vertices.Length);
        if (width <= CentreEpsilon || Math.Abs(extra) < CentreEpsilon) return result;

        var ratio = (width + extra) / width;
        for (var index = 0; index < result.Length; index++)
        {
            result[index].x *= ratio;
        }

        return result;
    }

    /// <summary>How far the vertices reach across x: the width the shape spans.</summary>
    internal static float WidthOf(float3[] vertices)
    {
        if (vertices.Length == 0) return 0f;

        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var vertex in vertices)
        {
            min = Math.Min(min, vertex.x);
            max = Math.Max(max, vertex.x);
        }

        return max - min;
    }

    /// <summary>
    /// The box a set of points occupies, over the vertices one submesh actually indexes.
    ///
    /// Needed because a generated mesh does not get its bounds computed for it. The asset pipeline
    /// builds the Unity mesh with <c>Mesh.SetSubMesh(index, descriptor, flags: 15)</c> - and 15
    /// includes <c>DontRecalculateBounds</c> - then sets <c>mesh.bounds</c> to the union of the
    /// descriptors' own bounds. So a descriptor constructed as
    /// <c>new SubMeshDescriptor(start, count, Triangles)</c>, which leaves that field at its default,
    /// produces a mesh that declares itself a zero-size box at the origin.
    ///
    /// Every mesh this mod generated declared exactly that. The vertices were right, the indices were
    /// right, the vertex layout was right, and each mesh said it occupied no space - which is why
    /// reasoning about the geometry never found anything: the geometry was never the part that was
    /// wrong. The game's own importers set this field; ours did not.
    ///
    /// Computed over the vertices the submesh indexes rather than all of them, because that is what
    /// the descriptor describes, and from the widened positions rather than the source, because those
    /// are what get written.
    /// </summary>
    internal static void ExtentOf(
        float3[] points, IReadOnlyList<int> indices, int from, int count,
        out float3 min, out float3 max)
    {
        min = default;
        max = default;
        if (points.Length == 0 || count <= 0) return;

        var started = false;
        for (var step = 0; step < count; step++)
        {
            var index = indices[from + step];
            if (index < 0 || index >= points.Length) continue;

            var point = points[index];
            if (!started)
            {
                min = point;
                max = point;
                started = true;
                continue;
            }

            min = new float3(
                Math.Min(min.x, point.x), Math.Min(min.y, point.y), Math.Min(min.z, point.z));
            max = new float3(
                Math.Max(max.x, point.x), Math.Max(max.y, point.y), Math.Max(max.z, point.z));
        }
    }

    /// <summary>The lowest and highest vertex index a run of indices refers to.</summary>
    internal static void IndexRangeOf(
        IReadOnlyList<int> indices, int from, int count, out int first, out int used)
    {
        first = 0;
        used = 0;
        if (count <= 0) return;

        var low = int.MaxValue;
        var high = int.MinValue;
        for (var step = 0; step < count; step++)
        {
            var index = indices[from + step];
            if (index < low) low = index;
            if (index > high) high = index;
        }

        if (low > high) return;
        first = low;
        used = (high - low) + 1;
    }

    /// <summary>
    /// The widest clear span across the centre line, found by slicing the shape into height bands.
    ///
    /// This is the number bounds cannot give: a bounding box has an outer face and no inner one, so the
    /// gap a portal's legs leave between them is absent from every dump that prints extents. It is also
    /// not the smallest distance from the centre line to any vertex, which is the obvious thing to
    /// reach for and gives zero here. A tower's repeatable segment is two legs *and* a crossbeam - the
    /// rungs of the ladder repeat with it - and the crossbeam runs straight through the middle.
    ///
    /// So the shape is sliced across its height and each band measured on its own. The bands holding a
    /// crossbeam report a span of nearly nothing; the bands between them hold legs alone and report the
    /// real gap. The widest is the answer, which is why this takes the maximum rather than the minimum.
    ///
    /// A band with vertices on one side of the centre line only is skipped rather than counted as a
    /// huge span - it has no facing pair to measure between.
    /// </summary>
    internal static float ClearSpanOf(float3[] vertices, int bands)
    {
        if (vertices.Length == 0 || bands <= 0) return 0f;

        var low = float.MaxValue;
        var high = float.MinValue;
        foreach (var vertex in vertices)
        {
            low = Math.Min(low, vertex.y);
            high = Math.Max(high, vertex.y);
        }

        var height = high - low;
        if (height <= CentreEpsilon) bands = 1;

        var right = new float[bands];
        var left = new float[bands];
        for (var band = 0; band < bands; band++)
        {
            right[band] = float.MaxValue;
            left[band] = float.MaxValue;
        }

        foreach (var vertex in vertices)
        {
            var band = bands == 1 ? 0 : (int)((vertex.y - low) / height * bands);
            if (band < 0) band = 0;
            if (band >= bands) band = bands - 1;

            if (vertex.x > CentreEpsilon) right[band] = Math.Min(right[band], vertex.x);
            else if (vertex.x < -CentreEpsilon) left[band] = Math.Min(left[band], -vertex.x);
        }

        var widest = 0f;
        for (var band = 0; band < bands; band++)
        {
            if (right[band] == float.MaxValue || left[band] == float.MaxValue) continue;
            widest = Math.Max(widest, right[band] + left[band]);
        }

        return widest;
    }

}
