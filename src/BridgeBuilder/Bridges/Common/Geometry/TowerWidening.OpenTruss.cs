using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static partial class TowerWidening
{
    /// <summary>
    /// Widens an open truss using one classification measured from the full-detail archetype. The
    /// caller passes the same profile to every LOD, so a small centre fitting never invents its own
    /// bridge-wide scale and a coarse mesh never votes differently from the mesh it represents.
    /// </summary>
    internal static float3[] WidenOpenTruss(float3[] source, IReadOnlyList<int>? triangles, float extra, bool preserveOuterAssemblies, Profile profile)
    {
        if (preserveOuterAssemblies)
        {
            return WidenProfiledOpenTruss(source, triangles, extra, profile);
        }

        var moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        var components = PiecesOf(source, triangles, out var labels);
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

        return moved;
    }

    private static float3[] WidenProfiledOpenTruss(float3[] source, IReadOnlyList<int>? triangles, float extra, Profile profile)
    {
        var moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        var shift = extra * 0.5f;
        var boundary = profile.OpenTrussBoundary;

        var ratio = boundary > CentreEpsilon ? (boundary + shift) / boundary : 1f;
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
        return moved;
    }

}
