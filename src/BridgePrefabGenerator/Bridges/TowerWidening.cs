using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgePrefabGenerator.Bridges;

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
internal static class TowerWidening
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
        var moved = Widen(vertices, extra);
        for (var index = 0; index < vertices.Length; index++)
        {
            var expected = Spread(vertices[index].x, extra);
            if (Math.Abs(moved[index].x - expected) <= CentreEpsilon) continue;

            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Rigid base invariant rejected vertex {0}: source x={1:0.#####}, result "
                + "x={2:0.#####}, expected x + sign(x) * delta = {3:0.#####}.",
                index, vertices[index].x, moved[index].x, expected));
        }
        return moved;
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

    /// <summary>How many height bands a shape is sliced into to find its clear span.</summary>
    internal const int SpanBands = 64;

    /// <summary>
    /// Widens a mesh by the one criterion: whether a part crosses the bridge's centre.
    ///
    /// The boundary between the two answers is measured, not guessed. <see cref="ClearSpanOf"/> slices
    /// the shape across its height and finds the widest gap it leaves open across the centre line -
    /// which is where a portal's legs begin, whatever their thickness, whatever road the tower was
    /// drawn for. Outside that gap nothing crosses the centre, so it is carried out rigidly by half the
    /// extra width. Inside it the shape does cross, so it is scaled about the centre, and the two agree
    /// exactly at the boundary, so nothing tears.
    ///
    /// A shape that leaves no gap - a cable sheet is continuous from one side to the other at every
    /// height - has a clear span of zero, and then every vertex is inside and the whole thing scales.
    /// That is the right answer for a sheet and falls out of the same rule rather than being a second
    /// one a caller has to choose.
    ///
    /// Two earlier versions are worth stating, because each was right about something:
    ///
    /// The first split at half the road. Half the road is a guess about where the legs begin, and where
    /// the guess fell inside a leg the leg was cut in two - outer portion carried, inner portion scaled
    /// - and the column came out a splayed slab.
    ///
    /// The second asked the question of connected components, which is the right question and the
    /// wrong unit: a portal's legs are joined to each other by its crossbeams, so the whole tower is
    /// one component, it does cross the centre, and scaling it thickens or thins the legs in
    /// proportion. A tower is never scaled - that is rule 5 - and this scaled every one of them.
    ///
    /// At zero extra nothing moves and the result is the mesh it came from, vertex for vertex.
    /// </summary>
    internal static float3[] WidenParts(float3[] vertices, IReadOnlyList<int> triangles, float extra) =>
        WidenParts(vertices, extra, Profile.Of(new[] { vertices }, new[] { triangles }));

    /// <summary>
    /// A shape widened against a profile, with its own material told apart by its triangles.
    ///
    /// The profile is where the triangles were read: it knows where a crossing member ends and a leg
    /// begins, and which places hold material that never touches the centre. Both are properties of
    /// the scope, shared by a part and every level of detail of it, so this asks the profile about a
    /// place rather than asking each mesh about its own topology.
    /// </summary>
    internal static float3[] WidenParts(float3[] vertices, float extra, Profile profile)
    {
        var result = new float3[vertices.Length];
        Array.Copy(vertices, result, vertices.Length);
        if (Math.Abs(extra) < CentreEpsilon || vertices.Length == 0) return result;
        if (profile.Outer <= CentreEpsilon) return result;

        var shift = extra * 0.5f;

        for (var index = 0; index < result.Length; index++)
        {
            var x = result[index].x;

            // Material that stands clear of the centre over its whole extent is carried entire,
            // whatever height any part of it happens to sit at. Asked of the profile, which a part
            // shares with its levels of detail, and not of this mesh - see Profile.CarriedAt.
            if (profile.CarriedAt(result[index].y, Math.Abs(x)))
            {
                result[index].x = x + (x > 0f ? shift : -shift);
                continue;
            }

            var span = profile.SpanAt(result[index].y);

            if (span <= CentreEpsilon || Math.Abs(x) > span)
            {
                result[index].x = x + (x > 0f ? shift : -shift);
                continue;
            }

            result[index].x = x * Math.Max(0f, (span + shift) / span);
        }

        return result;
    }

    /// <summary>A shape widened against its own profile.</summary>
    internal static float3[] WidenParts(float3[] vertices, float extra) =>
        WidenParts(vertices, extra, Profile.Of(vertices));

    /// <summary>
    /// Makes every connected member crossing the centre use one affine widening along its whole
    /// length.
    ///
    /// A through-arch truss has diagonal and transverse members whose height changes as they cross the
    /// deck. The general profile deliberately answers height by height because a pylon's opening really
    /// does change with height. Applied to one of these members, however, that gives consecutive
    /// vertices different scale factors: an end is carried, a point nearer the centre is stretched,
    /// and the rectangular member becomes a fan of long triangles.
    ///
    /// The source triangle topology says which vertices are one member. A member reaching both sides
    /// of the centre is lengthened from its own authored left and right boundaries, so both ends move
    /// outward by half <paramref name="extra"/> and every point between them follows the same affine
    /// map. A side truss never crosses the centre and is left exactly as the profile moved it.
    /// </summary>
    internal static int StretchCrossingPieces(
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? triangles,
        float extra,
        out int pieces)
    {
        pieces = 0;
        if (source.Length == 0 || source.Length != moved.Length || triangles == null) return 0;
        if (Math.Abs(extra) < CentreEpsilon) return 0;

        var components = PiecesOf(source, triangles, out var labels);
        if (components.Length == 0 || labels.Length != source.Length) return 0;

        var centres = new float[components.Length];
        var ratios = new float[components.Length];
        var crossing = new bool[components.Length];
        foreach (var component in components)
        {
            if (component.Left >= -CentreEpsilon || component.Right <= CentreEpsilon) continue;

            var width = component.Right - component.Left;
            if (width <= CentreEpsilon) continue;

            centres[component.Id] = (component.Left + component.Right) * 0.5f;
            ratios[component.Id] = Math.Max(0f, (width + extra) / width);
            crossing[component.Id] = true;
            pieces++;
        }

        var corrected = 0;
        for (var index = 0; index < source.Length; index++)
        {
            var id = labels[index];
            if (id < 0 || id >= crossing.Length || !crossing[id]) continue;

            var centre = centres[id];
            var x = centre + ((source[index].x - centre) * ratios[id]);
            if (Math.Abs(moved[index].x - x) > CentreEpsilon) corrected++;
            moved[index].x = x;
        }

        return corrected;
    }

    /// <summary>
    /// Widens an open truss member by member, using its triangle topology as the source of truth.
    ///
    /// A portal is welded into one object and needs the height profile above. An open truss is the
    /// opposite: its longitudinal chords, diagonals and transverse bars are separate pieces of
    /// material. Asking a height band where a diagonal vertex belongs gives successive stations of
    /// the same bar different transforms, which turns a rectangular bar into the fans of triangles
    /// seen on TrussArchBridge01.
    ///
    /// A sign test alone is insufficient. The real mesh contains hundreds of disconnected bolts,
    /// centre pivots and rods. Giving every centre-crossing object the bridge's full extra width turns
    /// small plates into the broad sheets seen in game; carrying every small object rigidly leaves the
    /// centre pivots at their authored width and tears them away from the widened rods.
    ///
    /// The centre line is the only decision. A logical member which reaches or crosses x=0 is
    /// stretched; every side member which does not is translated rigidly by the full half-extra. A
    /// logical transverse member may be authored as several disconnected mesh islands, so all of its
    /// faces, pivots and fittings share the reach measured from the complete full-detail assembly.
    /// Giving those islands separate reaches is not a refinement of the x=0 rule: it replaces the
    /// logical member with an importer accident and blows small centre fittings into broad sheets.
    ///
    /// The index buffer is never changed, so connected-body count is preserved by construction. Facts
    /// are measured from the returned vertices, not inferred from the requested extra, following the
    /// same rule as an external mesh validator: validate the built mesh rather than its input parameter.
    /// </summary>
    internal static float3[] WidenOpenTruss(
        float3[] source,
        IReadOnlyList<int>? triangles,
        float extra,
        out TrussWideningFacts facts) =>
        WidenOpenTruss(
            source, triangles, extra, false,
            Profile.Of(new[] { source }, new IReadOnlyList<int>?[] { triangles }),
            out facts);

    /// <param name="preserveOuterAssemblies">
    /// Carry a side-connected assembly rigidly as soon as it reaches the transverse structure's
    /// authored boundary. Used by the green through arch, whose inner railing and outer arch are one
    /// assembly and therefore must retain their source clearance.
    /// </param>
    internal static float3[] WidenOpenTruss(
        float3[] source,
        IReadOnlyList<int>? triangles,
        float extra,
        bool preserveOuterAssemblies,
        out TrussWideningFacts facts)
        => WidenOpenTruss(
            source, triangles, extra, preserveOuterAssemblies,
            Profile.Of(new[] { source }, new IReadOnlyList<int>?[] { triangles }),
            out facts);

    /// <summary>
    /// Widens an open truss using one classification measured from the full-detail archetype. The
    /// caller passes the same profile to every LOD, so a small centre fitting never invents its own
    /// bridge-wide scale and a coarse mesh never votes differently from the mesh it represents.
    /// </summary>
    internal static float3[] WidenOpenTruss(
        float3[] source,
        IReadOnlyList<int>? triangles,
        float extra,
        bool preserveOuterAssemblies,
        Profile profile,
        out TrussWideningFacts facts)
    {
        if (preserveOuterAssemblies)
        {
            return WidenProfiledOpenTruss(source, triangles, extra, profile, out facts);
        }

        var moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);

        var components = PiecesOf(source, triangles, out var labels);
        var rigid = 0;
        var spanning = 0;
        var floating = 0;
        var shift = extra * 0.5f;

        // One reach for the complete transverse assembly, measured on the full-detail archetype and
        // reused by every LOD. The top beam is authored as many mesh islands (faces, joints, pivots),
        // but it is one part crossing x=0. Giving every island its own denominator is what enlarged a
        // small centre plate by the complete requested width and produced the broad sheets in game.
        var leftReach = profile.OpenTrussLeftReach;
        var rightReach = profile.OpenTrussRightReach;

        var leftRatio = leftReach > CentreEpsilon
            ? Math.Max(0f, (leftReach + shift) / leftReach)
            : 1f;
        var rightRatio = rightReach > CentreEpsilon
            ? Math.Max(0f, (rightReach + shift) / rightReach)
            : 1f;

        if (Math.Abs(extra) >= CentreEpsilon && components.Length > 0)
        {
            var translated = new bool[components.Length];
            foreach (var component in components)
            {
                // CONTRACT rule 8 is asked of the authored part, not of an import island. The blue
                // top truss is one transverse assembly which the importer split into many rods and
                // fittings. The full-detail profile groups that complete assembly first; because the
                // assembly crosses x=0, all of it takes one stretch. The centre fitting is not a
                // special case and never supplies a scale of its own. Side arches are longitudinal,
                // remain outside the group and are translated rigidly.
                var crossesCentre = profile.OpenTrussPartCrossesCentre(component);
                translated[component.Id] = !crossesCentre;
                if (crossesCentre)
                {
                    spanning++;
                    if (!component.CrossesCentre) floating++;
                }
                else rigid++;
            }

            for (var index = 0; index < moved.Length; index++)
            {
                var component = components[labels[index]];
                var x = source[index].x;
                if (translated[component.Id])
                {
                    var centre = (component.Left + component.Right) * 0.5f;
                    moved[index].x = x + (centre >= 0f ? shift : -shift);
                }
                else
                {
                    moved[index].x = Math.Abs(x) <= CentreEpsilon
                        ? x
                        : x < 0f
                            ? x * leftRatio
                            : x * rightRatio;
                }
            }

            RequireCentrelineRule(
                source, moved, components, labels, extra, profile);
        }

        var degenerateBefore = DegenerateTriangles(source, triangles);
        var degenerateAfter = DegenerateTriangles(moved, triangles);
        var flipped = FlippedTriangles(source, moved, triangles);
        var finite = true;
        foreach (var vertex in moved)
        {
            if (!float.IsNaN(vertex.x) && !float.IsInfinity(vertex.x)
                && !float.IsNaN(vertex.y) && !float.IsInfinity(vertex.y)
                && !float.IsNaN(vertex.z) && !float.IsInfinity(vertex.z))
                continue;

            finite = false;
            break;
        }

        facts = new TrussWideningFacts(
            components.Length,
            rigid,
            spanning,
            floating,
            degenerateBefore,
            degenerateAfter,
            flipped,
            leftReach,
            rightReach,
            leftRatio,
            rightRatio,
            WidthOf(moved) - WidthOf(source),
            finite);
        return moved;
    }

    private static float3[] WidenProfiledOpenTruss(
        float3[] source,
        IReadOnlyList<int>? triangles,
        float extra,
        Profile profile,
        out TrussWideningFacts facts)
    {
        var moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        var components = PiecesOf(source, triangles, out var labels);
        if (Math.Abs(extra) >= CentreEpsilon && source.Length > 0
            && (components.Length == 0 || labels.Length != source.Length))
        {
            throw new InvalidOperationException(
                "Centre-line widening invariant cannot classify the green truss without topology.");
        }

        var shift = extra * 0.5f;
        var boundary = profile.OpenTrussBoundary;
        if (Math.Abs(extra) >= CentreEpsilon && boundary <= CentreEpsilon)
            throw new InvalidOperationException(
                "Centre-line widening invariant found no measured side boundary in the green archetype.");

        var ratio = boundary > CentreEpsilon
            ? Math.Max(0f, (boundary + shift) / boundary)
            : 1f;
        var rigid = 0;
        var spanning = 0;
        var mixed = 0;

        foreach (var component in components)
        {
            if (component.Right <= -boundary + CentreEpsilon
                || component.Left >= boundary - CentreEpsilon)
                rigid++;
            else if (component.Left < -CentreEpsilon && component.Right > CentreEpsilon)
                spanning++;
            else
                mixed++;
        }

        // One continuous x-only map for the whole mesh. It is deliberately independent of height and
        // component connectivity: the top beam crosses x=0 and is lengthened, while everything past
        // the measured inner face of the side assembly receives an exact rigid translation. Both
        // formulae agree at the boundary, so a welded beam stays connected and no triangle can become
        // a fan merely because its vertices fell in different height bands.
        for (var index = 0; index < source.Length; index++)
        {
            var x = source[index].x;
            if (x <= -boundary)
                moved[index].x = x - shift;
            else if (x >= boundary)
                moved[index].x = x + shift;
            else
                moved[index].x = x * ratio;
        }

        RequireProfiledCentrelineRule(
            source, moved, boundary, ratio, extra);

        var degenerateBefore = DegenerateTriangles(source, triangles);
        var degenerateAfter = DegenerateTriangles(moved, triangles);
        var flipped = FlippedTriangles(source, moved, triangles);
        var finite = true;
        foreach (var vertex in moved)
        {
            if (!float.IsNaN(vertex.x) && !float.IsInfinity(vertex.x)
                && !float.IsNaN(vertex.y) && !float.IsInfinity(vertex.y)
                && !float.IsNaN(vertex.z) && !float.IsInfinity(vertex.z))
                continue;
            finite = false;
            break;
        }

        facts = new TrussWideningFacts(
            components.Length, rigid, spanning, mixed,
            degenerateBefore, degenerateAfter, flipped,
            boundary, boundary, ratio, ratio,
            WidthOf(moved) - WidthOf(source), finite);
        return moved;
    }

    private static void RequireProfiledCentrelineRule(
        float3[] source,
        float3[] moved,
        float boundary,
        float ratio,
        float extra)
    {
        if (source.Length != moved.Length)
            throw new InvalidOperationException(
                "Centre-line widening invariant cannot be checked because vertex counts differ.");

        var shift = extra * 0.5f;
        for (var index = 0; index < source.Length; index++)
        {
            var x = source[index].x;
            var expected = x <= -boundary
                ? x - shift
                : x >= boundary
                    ? x + shift
                    : x * ratio;

            if (Math.Abs(moved[index].x - expected) <= CentreEpsilon) continue;

            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Centre-line widening invariant rejected profiled vertex {0}: source "
                + "x={1:0.#####}, result x={2:0.#####}, expected x={3:0.#####}. The full-detail "
                + "side boundary and the same continuous transform must be used by every LOD.",
                index, x, moved[index].x, expected));
        }
    }

    /// <summary>
    /// Enforces AGENTS.md rule 8 against an already transformed open-truss mesh.
    ///
    /// Kept internal so the regression suite can prove that a proposed override is rejected rather
    /// than merely producing a different-looking mesh.
    /// </summary>
    internal static void RequireCentrelineRule(
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? triangles,
        float extra)
    {
        var components = PiecesOf(source, triangles, out var labels);
        var profile = Profile.Of(
            new[] { source }, new IReadOnlyList<int>?[] { triangles });
        RequireCentrelineRule(
            source, moved, components, labels, extra, profile);
    }

    private static void RequireCentrelineRule(
        float3[] source,
        float3[] moved,
        IReadOnlyList<Piece> components,
        IReadOnlyList<int> labels,
        float extra,
        Profile profile)
    {
        if (source.Length != moved.Length)
            throw new InvalidOperationException(
                "Centre-line widening invariant cannot be checked because vertex counts differ.");
        if (Math.Abs(extra) < CentreEpsilon || source.Length == 0) return;
        if (components.Count == 0 || labels.Count != source.Length)
            throw new InvalidOperationException(
                "Centre-line widening invariant cannot classify parts without triangle topology.");

        var leftReach = profile.OpenTrussLeftReach;
        var rightReach = profile.OpenTrussRightReach;
        if (leftReach <= CentreEpsilon || rightReach <= CentreEpsilon)
            throw new InvalidOperationException(
                "Centre-line widening invariant found no full-detail transverse assembly reach.");

        var shift = extra * 0.5f;
        var leftRatio = Math.Max(0f, (leftReach + shift) / leftReach);
        var rightRatio = Math.Max(0f, (rightReach + shift) / rightReach);
        var stretches = new bool[components.Count];
        for (var componentIndex = 0; componentIndex < components.Count; componentIndex++)
            stretches[componentIndex] = profile.OpenTrussPartCrossesCentre(
                components[componentIndex]);

        for (var index = 0; index < source.Length; index++)
        {
            var componentId = labels[index];
            if (componentId < 0 || componentId >= components.Count)
                throw new InvalidOperationException(
                    "Centre-line widening invariant found an unclassified vertex.");
            var component = components[componentId];
            var crossesCentre = stretches[componentId];
            var translated = !crossesCentre;

            var x = source[index].x;
            float expected;
            if (translated)
            {
                var centre = (component.Left + component.Right) * 0.5f;
                expected = x + (centre >= 0f ? shift : -shift);
            }
            else if (Math.Abs(x) <= CentreEpsilon)
            {
                expected = x;
            }
            else
            {
                expected = x < 0f ? x * leftRatio : x * rightRatio;
            }

            if (Math.Abs(moved[index].x - expected) <= CentreEpsilon) continue;

            var required = translated
                ? "rigid translation because the logical part does not reach x=0"
                : "the shared stretch of the complete logical top truss reaching x=0";
            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Centre-line widening invariant rejected part {0}: source x={1:0.#####}, "
                + "result x={2:0.#####}; it must use {3}, measured once from the complete "
                + "full-detail transverse assembly (expected x={4:0.#####}).",
                component.Id, x, moved[index].x, required, expected));
        }
    }

    private static int DegenerateTriangles(float3[] vertices, IReadOnlyList<int>? triangles)
    {
        if (triangles == null) return 0;

        var count = 0;
        for (var corner = 0; corner + 2 < triangles.Count; corner += 3)
        {
            var a = triangles[corner];
            var b = triangles[corner + 1];
            var c = triangles[corner + 2];
            if (a < 0 || b < 0 || c < 0
                || a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
            {
                count++;
                continue;
            }

            var normal = math.cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (math.lengthsq(normal) <= 1e-12f) count++;
        }

        return count;
    }

    private static int FlippedTriangles(
        float3[] source, float3[] moved, IReadOnlyList<int>? triangles)
    {
        if (triangles == null || source.Length != moved.Length) return 0;

        var count = 0;
        for (var corner = 0; corner + 2 < triangles.Count; corner += 3)
        {
            var a = triangles[corner];
            var b = triangles[corner + 1];
            var c = triangles[corner + 2];
            if (a < 0 || b < 0 || c < 0
                || a >= source.Length || b >= source.Length || c >= source.Length)
                continue;

            var before = math.cross(source[b] - source[a], source[c] - source[a]);
            var after = math.cross(moved[b] - moved[a], moved[c] - moved[a]);
            if (math.lengthsq(before) <= 1e-12f || math.lengthsq(after) <= 1e-12f) continue;
            if (math.dot(before, after) < 0f) count++;
        }

        return count;
    }

    internal readonly struct TrussWideningFacts
    {
        internal TrussWideningFacts(
            int pieces,
            int rigidPieces,
            int spanningPieces,
            int floatingPieces,
            int degenerateBefore,
            int degenerateAfter,
            int flippedTriangles,
            float leftStructuralReach,
            float rightStructuralReach,
            float leftScale,
            float rightScale,
            float measuredWidthChange,
            bool finite)
        {
            Pieces = pieces;
            RigidPieces = rigidPieces;
            SpanningPieces = spanningPieces;
            FloatingPieces = floatingPieces;
            DegenerateBefore = degenerateBefore;
            DegenerateAfter = degenerateAfter;
            FlippedTriangles = flippedTriangles;
            LeftStructuralReach = leftStructuralReach;
            RightStructuralReach = rightStructuralReach;
            LeftScale = leftScale;
            RightScale = rightScale;
            MeasuredWidthChange = measuredWidthChange;
            Finite = finite;
        }

        internal int Pieces { get; }
        internal int RigidPieces { get; }
        internal int SpanningPieces { get; }
        internal int FloatingPieces { get; }
        internal int DegenerateBefore { get; }
        internal int DegenerateAfter { get; }
        internal int FlippedTriangles { get; }
        internal float LeftStructuralReach { get; }
        internal float RightStructuralReach { get; }
        internal float LeftScale { get; }
        internal float RightScale { get; }
        internal float MeasuredWidthChange { get; }
        internal bool Finite { get; }
    }

    /// <summary>
    /// Rebuilds the golden top's centred vertical spoke as a constant-width rectangle.
    ///
    /// The golden bridge's top fan is openwork. At some sampled heights the centre spoke stands by
    /// itself, at others it stands beside ribs or the arch, so a height-by-height span gives its two
    /// sides different scale factors and turns it into an hourglass or a pair of broad diamonds.
    ///
    /// It is a column on the centre line: widening the bridge changes neither its centre nor its
    /// thickness. The member is therefore found as topology, not as every vertex that happens to pass
    /// through a narrow x band. That distinction matters at the tips of the neighbouring fan ribs:
    /// their vertices enter the same band, but their triangles run far outside it. Only the narrow,
    /// centred connected component with the greatest vertical reach is rebuilt, and every one of its
    /// non-centre vertices is put on one of two parallel sides at the component's authored half-width.
    /// </summary>
    internal static int RectangularizeCentralSpoke(
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? triangles,
        out float halfWidth,
        out float scale)
    {
        halfWidth = 0f;
        scale = 1f;
        if (source.Length == 0 || source.Length != moved.Length || triangles == null) return 0;

        var outer = 0f;
        var low = float.MaxValue;
        var high = float.MinValue;
        foreach (var vertex in source)
        {
            outer = Math.Max(outer, Math.Abs(vertex.x));
            low = Math.Min(low, vertex.y);
            high = Math.Max(high, vertex.y);
        }

        if (outer <= CentreEpsilon || high <= low) return 0;

        var limit = outer * 0.1f;
        var parent = new int[source.Length];
        var used = new bool[source.Length];
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

        // Build topology only from triangles wholly inside the centre band. A fan rib's tip can sit
        // inside this band, but the other two corners of its triangle plainly identify it as a rib.
        // Joining the whole mesh first joins the spoke to the arch at their shared bottom edge and
        // makes the complete fan look like one wide component.
        for (var corner = 0; corner + 2 < triangles.Count; corner += 3)
        {
            var a = triangles[corner];
            var b = triangles[corner + 1];
            var c = triangles[corner + 2];
            if (a < 0 || b < 0 || c < 0) continue;
            if (a >= source.Length || b >= source.Length || c >= source.Length) continue;
            if (Math.Abs(source[a].x) > limit
                || Math.Abs(source[b].x) > limit
                || Math.Abs(source[c].x) > limit)
                continue;

            used[a] = true;
            used[b] = true;
            used[c] = true;
            Join(a, b);
            Join(b, c);
        }

        // A rendered mesh normally duplicates a position at hard edges. Weld only copies which
        // belong to the centre-band faces; a duplicate belonging solely to a fan rib stays out.
        var welded = new Dictionary<long, int>();
        for (var index = 0; index < source.Length; index++)
        {
            if (!used[index]) continue;
            var key = WeldKey(source[index]);
            if (welded.TryGetValue(key, out var first)) Join(index, first);
            else welded[key] = index;
        }

        var components = new Dictionary<int, (float Low, float High, int Signs)>();
        for (var index = 0; index < source.Length; index++)
        {
            if (!used[index]) continue;
            var root = Root(index);
            var vertex = source[index];
            if (!components.TryGetValue(root, out var component))
                component = (vertex.y, vertex.y, 0);

            component.Low = Math.Min(component.Low, vertex.y);
            component.High = Math.Max(component.High, vertex.y);
            component.Signs |= vertex.x < -CentreEpsilon ? 1
                : vertex.x > CentreEpsilon ? 2
                : 4;
            components[root] = component;
        }

        var chosen = -1;
        var greatestSpan = 0f;
        foreach (var entry in components)
        {
            var component = entry.Value;
            var crossesCentre = (component.Signs & 3) == 3 || (component.Signs & 4) != 0;
            if (!crossesCentre) continue;

            var verticalSpan = component.High - component.Low;
            if (verticalSpan <= greatestSpan) continue;

            chosen = entry.Key;
            greatestSpan = verticalSpan;
        }

        if (chosen < 0 || greatestSpan < Math.Max(1f, (high - low) * 0.05f)) return 0;

        // The longest pair of parallel vertical edges within the chosen centre component are the
        // spoke's sides. Short runs at the bottom belong to the arch which the spoke lands on.
        const float bucket = 0.05f;
        var candidates = new Dictionary<int, (float Low, float High, int Signs)>();
        for (var index = 0; index < source.Length; index++)
        {
            if (!used[index] || Root(index) != chosen) continue;
            var vertex = source[index];
            var distance = Math.Abs(vertex.x);
            if (distance <= bucket) continue;

            var slot = (int)Math.Round(distance / bucket);
            candidates.TryGetValue(slot, out var candidate);
            if (candidate.Signs == 0) candidate = (vertex.y, vertex.y, 0);
            candidate.Low = Math.Min(candidate.Low, vertex.y);
            candidate.High = Math.Max(candidate.High, vertex.y);
            candidate.Signs |= vertex.x < 0f ? 1 : 2;
            candidates[slot] = candidate;
        }

        var memberLow = 0f;
        var memberHigh = 0f;
        var memberSpan = 0f;
        var chosenSide = -1;
        foreach (var entry in candidates)
        {
            if (entry.Value.Signs != 3) continue;
            var verticalSpan = entry.Value.High - entry.Value.Low;
            if (verticalSpan < memberSpan - bucket) continue;
            if (verticalSpan > memberSpan + bucket || entry.Key > chosenSide)
            {
                chosenSide = entry.Key;
                memberLow = entry.Value.Low;
                memberHigh = entry.Value.High;
                memberSpan = verticalSpan;
            }
        }

        if (chosenSide < 0 || memberSpan < Math.Max(1f, greatestSpan * 0.5f)) return 0;
        halfWidth = chosenSide * bucket;
        var memberLimit = halfWidth + Math.Max(bucket, halfWidth * 0.15f);

        // No scale is deliberate. This is the thickness of a vertical column, not the span between
        // the tower's legs. Widening the tower must not turn a 2.7 m column into the 12.9 m member the
        // previous pass reported on a 64 m road.
        scale = 1f;
        var changed = 0;
        for (var index = 0; index < source.Length; index++)
        {
            var vertex = source[index];
            if (!used[index] || Root(index) != chosen) continue;
            if (vertex.y < memberLow - bucket || vertex.y > memberHigh + bucket) continue;
            if (Math.Abs(vertex.x) <= CentreEpsilon || Math.Abs(vertex.x) > memberLimit) continue;

            var corrected = vertex.x < 0f ? -halfWidth : halfWidth;
            if (Math.Abs(moved[index].x - corrected) <= CentreEpsilon) continue;
            moved[index].x = corrected;
            changed++;
        }

        return changed;
    }



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
            // position - so interpolating it would throw away everything but one end of it.
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
