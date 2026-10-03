using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static partial class TowerWidening
{
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

        }

        var contractSatisfied = RequireCentrelineRule(
            source, moved, components, labels, extra, leftReach, rightReach);
        if (!contractSatisfied)
            Array.Copy(source, moved, source.Length);

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
            finite,
            contractSatisfied);
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
            facts = RejectedTrussFacts(source, triangles, components.Length, profile.OpenTrussBoundary);
            return moved;
        }

        var shift = extra * 0.5f;
        var boundary = profile.OpenTrussBoundary;
        if (Math.Abs(extra) >= CentreEpsilon && boundary <= CentreEpsilon)
        {
            facts = RejectedTrussFacts(source, triangles, components.Length, boundary);
            return moved;
        }

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

        var contractSatisfied = RequireProfiledCentrelineRule(
            source, moved, boundary, ratio, extra);
        if (!contractSatisfied)
            Array.Copy(source, moved, source.Length);

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
            WidthOf(moved) - WidthOf(source), finite, contractSatisfied);
        return moved;
    }

    private static bool RequireProfiledCentrelineRule(
        float3[] source,
        float3[] moved,
        float boundary,
        float ratio,
        float extra)
    {
        if (source.Length != moved.Length)
            return false;

        var shift = extra * 0.5f;
        for (var index = 0; index < source.Length; index++)
        {
            var x = source[index].x;
            var expected = x <= -boundary
                ? x - shift
                : x >= boundary
                    ? x + shift
                    : x * ratio;

            if (Math.Abs(moved[index].x - expected) > CentreEpsilon)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Enforces AGENTS.md rule 8 against an already transformed open-truss mesh.
    ///
    /// Kept internal so the regression suite can prove that a proposed override is rejected rather
    /// than merely producing a different-looking mesh.
    /// </summary>
    internal static bool RequireCentrelineRule(
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? triangles,
        float extra)
    {
        var components = PiecesOf(source, triangles, out var labels);
        var profile = Profile.Of(
            new[] { source }, new IReadOnlyList<int>?[] { triangles });
        return RequireCentrelineRule(
            source, moved, components, labels, extra,
            profile.OpenTrussLeftReach, profile.OpenTrussRightReach);
    }

    private static bool RequireCentrelineRule(
        float3[] source,
        float3[] moved,
        IReadOnlyList<Piece> components,
        IReadOnlyList<int> labels,
        float extra,
        float leftReach,
        float rightReach)
    {
        if (source.Length != moved.Length)
            return false;
        if (Math.Abs(extra) < CentreEpsilon || source.Length == 0) return true;
        if (components.Count == 0 || labels.Count != source.Length)
            return false;

        if (leftReach <= CentreEpsilon || rightReach <= CentreEpsilon)
            return false;

        var shift = extra * 0.5f;
        var leftRatio = Math.Max(0f, (leftReach + shift) / leftReach);
        var rightRatio = Math.Max(0f, (rightReach + shift) / rightReach);
        for (var index = 0; index < source.Length; index++)
        {
            var componentId = labels[index];
            if (componentId < 0 || componentId >= components.Count)
                return false;
            var component = components[componentId];
            var onLeft = component.Right < -CentreEpsilon;
            var onRight = component.Left > CentreEpsilon;
            var reachesLeft = component.Left <= -leftReach + CentreEpsilon;
            var reachesRight = component.Right >= rightReach - CentreEpsilon;
            var whollyInLeftSide = onLeft && reachesLeft
                && component.Inner >= (leftReach * 0.5f) - CentreEpsilon;
            var whollyInRightSide = onRight && reachesRight
                && component.Inner >= (rightReach * 0.5f) - CentreEpsilon;
            var translated = whollyInLeftSide || whollyInRightSide;

            var x = source[index].x;
            float expected;
            if (translated)
            {
                expected = x + (component.Left > CentreEpsilon ? shift : -shift);
            }
            else if (Math.Abs(x) <= CentreEpsilon)
            {
                expected = x;
            }
            else
            {
                expected = x < 0f ? x * leftRatio : x * rightRatio;
            }

            if (Math.Abs(moved[index].x - expected) > CentreEpsilon)
                return false;
        }

        return true;
    }

    private static TrussWideningFacts RejectedTrussFacts(
        float3[] source,
        IReadOnlyList<int>? triangles,
        int pieces,
        float structuralReach)
    {
        var finite = true;
        foreach (var vertex in source)
        {
            if (!float.IsNaN(vertex.x) && !float.IsInfinity(vertex.x)
                && !float.IsNaN(vertex.y) && !float.IsInfinity(vertex.y)
                && !float.IsNaN(vertex.z) && !float.IsInfinity(vertex.z))
                continue;

            finite = false;
            break;
        }

        var degenerate = DegenerateTriangles(source, triangles);
        return new TrussWideningFacts(
            pieces, 0, 0, 0,
            degenerate, degenerate, 0,
            structuralReach, structuralReach,
            1f, 1f, 0f, finite, false);
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
            bool finite,
            bool contractSatisfied)
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
            ContractSatisfied = contractSatisfied;
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
        internal bool ContractSatisfied { get; }
    }

}
