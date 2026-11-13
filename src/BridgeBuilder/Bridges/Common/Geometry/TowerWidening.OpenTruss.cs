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
    internal static float3[] WidenOpenTruss(float3[] source, IReadOnlyList<int>? triangles, float extra, out TrussWideningFacts facts) => WidenOpenTruss(source, triangles, extra, false, Profile.Of(new[] { source }, new IReadOnlyList<int>? [] { triangles }), out facts);
    /// <param name = "preserveOuterAssemblies">
    /// Carry a side-connected assembly rigidly as soon as it reaches the transverse structure's
    /// authored boundary. Used by the green through arch, whose inner railing and outer arch are one
    /// assembly and therefore must retain their source clearance.
    /// </param>
    internal static float3[] WidenOpenTruss(float3[] source, IReadOnlyList<int>? triangles, float extra, bool preserveOuterAssemblies, out TrussWideningFacts facts) => WidenOpenTruss(source, triangles, extra, preserveOuterAssemblies, Profile.Of(new[] { source }, new IReadOnlyList<int>? [] { triangles }), out facts);
    /// <summary>
    /// Widens an open truss using one classification measured from the full-detail archetype. The
    /// caller passes the same profile to every LOD, so a small centre fitting never invents its own
    /// bridge-wide scale and a coarse mesh never votes differently from the mesh it represents.
    /// </summary>
    internal static float3[] WidenOpenTruss(float3[] source, IReadOnlyList<int>? triangles, float extra, bool preserveOuterAssemblies, Profile profile, out TrussWideningFacts facts)
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
        var leftRatio = leftReach > CentreEpsilon ? (leftReach + shift) / leftReach : 1f;
        var rightRatio = rightReach > CentreEpsilon ? (rightReach + shift) / rightReach : 1f;
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
                    if (!component.CrossesCentre)
                        floating++;
                }
                else
                    rigid++;
            }

            var positive = new List<int>();
            var negative = new List<int>();
            var stretchLeft = new List<int>();
            var stretchRight = new List<int>();
            for (int index = 0; index < moved.Length; index++)
            {
                var component = components[labels[index]];
                var x = source[index].x;
                if (translated[component.Id])
                {
                    var centre = (component.Left + component.Right) * 0.5f;
                    if (centre >= 0f) positive.Add(index); else negative.Add(index);
                }
                else
                {
                    if (Math.Abs(x) <= CentreEpsilon) continue;
                    if (x < 0f) stretchLeft.Add(index); else stretchRight.Add(index);
                }
            }
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.PositiveHalfX, new[] { source, moved },
                Array.Empty<float[]>(), new[] { extra }, count: positive.Count, indices: new[] { positive.ToArray() });
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.NegativeHalfX, new[] { source, moved },
                Array.Empty<float[]>(), new[] { extra }, count: negative.Count, indices: new[] { negative.ToArray() });
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX, new[] { source, moved },
                Array.Empty<float[]>(), new[] { leftRatio }, count: stretchLeft.Count, indices: new[] { stretchLeft.ToArray() });
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX, new[] { source, moved },
                Array.Empty<float[]>(), new[] { rightRatio }, count: stretchRight.Count, indices: new[] { stretchRight.ToArray() });
        }

        facts = default;
        return moved;
    }

    private static float3[] WidenProfiledOpenTruss(float3[] source, IReadOnlyList<int>? triangles, float extra, Profile profile, out TrussWideningFacts facts)
    {
        var moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        var components = PiecesOf(source, triangles, out var labels);

        var shift = extra * 0.5f;
        var boundary = profile.OpenTrussBoundary;

        var ratio = boundary > CentreEpsilon ? (boundary + shift) / boundary : 1f;
        var rigid = 0;
        var spanning = 0;
        var mixed = 0;
        foreach (var component in components)
        {
            if (component.Right <= -boundary + CentreEpsilon || component.Left >= boundary - CentreEpsilon)
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
        var negative = new List<int>();
        var positive = new List<int>();
        var stretching = new List<int>();
        for (int index = 0; index < source.Length; index++)
        {
            var x = source[index].x;
            if (x <= -boundary)
                negative.Add(index);
            else if (x >= boundary)
                positive.Add(index);
            else
                stretching.Add(index);
        }
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.NegativeHalfX, new[] { source, moved },
            Array.Empty<float[]>(), new[] { extra }, count: negative.Count, indices: new[] { negative.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.PositiveHalfX, new[] { source, moved },
            Array.Empty<float[]>(), new[] { extra }, count: positive.Count, indices: new[] { positive.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaleX, new[] { source, moved },
            Array.Empty<float[]>(), new[] { ratio }, count: stretching.Count, indices: new[] { stretching.ToArray() });
        facts = default;
        return moved;
    }

    internal readonly struct TrussWideningFacts
    {
        internal TrussWideningFacts(int pieces, int rigidPieces, int spanningPieces, int floatingPieces, int degenerateBefore, int degenerateAfter, int flippedTriangles, float leftStructuralReach, float rightStructuralReach, float leftScale, float rightScale, float measuredWidthChange, bool finite, bool contractSatisfied)
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
