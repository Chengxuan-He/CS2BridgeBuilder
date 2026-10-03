using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static partial class TowerWidening
{
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

}
