using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using BridgeBuilder.Runtime;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Continuous offline units share pixels across physical transverse domains.
// Only finite terminals and measured physical boundaries remain in the tree.
internal static class Lift03SourceRoadMapping
{
    internal static MovableRoadAppearance.Level Create(Lift03RoadSampling.Deck deck,
        int lod, SurfaceAsset source, Pose roadToObject, float halfDelta, SurfaceAsset? preparedSurface = null)
    {
        if ((uint)lod >= 2u) throw new ArgumentOutOfRangeException(nameof(lod));
        var values = Data.Values;
        int caseBase = (lod * 2 + (deck == Lift03RoadSampling.Deck.Main ? 0 : 1)) * 4;
        var patches = new List<Patch>();
        var prepared = preparedSurface ?? BridgePreparedMaterials.RepeatUnit("Lift03", deck == Lift03RoadSampling.Deck.Main
            ? "9c5ba0c317f4d9ae207c5509a5e191a8c31c224c2305a43e1507bbf726488523"
            : "32247dd719ab358e466505dd8f3fd0702a8f0db566be3e870bf3a7a523489e2c");
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var projection = pose.transpose;
        float width = -values.F32[values.I32[values.I32[2 + caseBase * 2]] * 35 + 2];
        float scale = width / (width + halfDelta);
        if (!(scale > 0f) || !float.IsFinite(scale)) throw new InvalidOperationException("Invalid Lift03 finite road width.");
        Partition Plane(float a, float b, float c, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m00 = b; frame.m02 = -a;
            frame.m20 = a; frame.m22 = b; frame.m23 = c;
            return new Partition(frame * pose, negative, positive);
        }
        Partition Measured(int p, int offset, int xSign, int zSign, Partition negative, Partition positive)
            => Plane(values.F32[p + offset] * xSign * scale, values.F32[p + offset + 1] * zSign,
                values.F32[p + offset + 2], negative, positive);
        Partition Transverse(int start, int count, int xSign, Partition[] leaves)
        {
            if (count == 1) return leaves[0];
            if (count != 2) throw new InvalidOperationException("Invalid Lift03 transverse source domains.");
            float seam = values.F32[(start + 1) * 35 + 2] / scale;
            return xSign > 0 ? Plane(1, 0, -seam, leaves[0], leaves[1])
                : Plane(1, 0, seam, leaves[1], leaves[0]);
        }
        Partition Side(int at, int domain, int xSign, int zSign)
        {
            int row = at + domain * 3, start = values.I32[row], count = values.I32[row + 1];
            bool repeat = values.I32[row + 2] != 0;
            var leaves = new Partition[count];
            for (int band = 0; band < count; band++)
            {
                int p = (start + band) * 35;
                Partition Chart(int coefficient)
                {
                    var u = projection * new Vector4(values.F32[p + coefficient] * xSign * scale, 0,
                        values.F32[p + coefficient + 1] * zSign, values.F32[p + coefficient + 2]);
                    var v = projection * new Vector4(values.F32[p + coefficient + 3] * xSign * scale, 0,
                        values.F32[p + coefficient + 4] * zSign, values.F32[p + coefficient + 5]);
                    var leaf = new Partition(patches.Count);
                    var patch = new Patch(new Vector2(values.F32[p + 4], values.F32[p + 6]),
                        new Vector2(values.F32[p + 5], values.F32[p + 7]), u, v,
                        surface: repeat ? prepared : source, sourceUv: !repeat,
                        preparedUnit: repeat, repeatU: false, repeatV: repeat,
                        projectTangent: true);
                    // Source-grid Extract writes every texel of every mip, including
                    // the finite-axis guard. This declares authored coverage only;
                    // it does not change content UVs or certify filter equivalence.
                    patches.Add(repeat ? patch.WithFiniteSampling(Vector2.zero, Vector2.one) : patch);
                    return leaf;
                }
                var first = Chart(8);
                if (values.F32[p + 20] != 0 || values.F32[p + 21] != 0)
                {
                    var rest = Chart(14);
                    if (values.F32[p + 32] != 0 || values.F32[p + 33] != 0)
                        rest = Measured(p, 32, xSign, zSign, rest, Chart(26));
                    first = Measured(p, 20, xSign, zSign, first, rest);
                }
                leaves[band] = first;
            }
            return Transverse(start, count, xSign, leaves);
        }
        Partition Boundary(int at, int next, int xSign, int zSign, Partition before, Partition after)
        {
            int row = at + next * 3, start = values.I32[row], count = values.I32[row + 1];
            int first = start * 35;
            bool same = true;
            for (int i = 1; i < count; i++) for (int j = 0; j < 3; j++)
                same &= values.F32[first + 23 + j] == values.F32[(start + i) * 35 + 23 + j];
            if (same) return Measured(first, 23, xSign, zSign, before, after);
            var leaves = new Partition[count];
            for (int i = 0; i < count; i++) leaves[i] = Measured((start + i) * 35, 23, xSign, zSign, before, after);
            return Transverse(start, count, xSign, leaves);
        }
        Partition Quadrant(int xSign, int zSign)
        {
            int header = 2 + (caseBase + (zSign > 0 ? 2 : 0) + (xSign < 0 ? 1 : 0)) * 2;
            int at = values.I32[header], domains = values.I32[header + 1];
            var result = Side(at, domains - 1, xSign, zSign);
            for (int i = domains - 2; i >= 0; i--)
                result = Boundary(at, i + 1, xSign, zSign, Side(at, i, xSign, zSign), result);
            return result;
        }
        var regions = Plane(0, 1, 0,
            Plane(1, 0, 0, Quadrant(1, -1), Quadrant(-1, -1)),
            Plane(1, 0, 0, Quadrant(1, 1), Quadrant(-1, 1)));
        var supported = new MovableRoadAppearance.Type[patches.Count];
        for (int i = 0; i < supported.Length; i++) supported[i] = deck == Lift03RoadSampling.Deck.Main
            ? MovableRoadAppearance.Type.Asphalt : MovableRoadAppearance.Type.Ballast;
        return new MovableRoadAppearance.Level(source, patches.ToArray(), supported, regions, regions);
    }
    private static class Data
    {
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03SourceRoadMapping_cs");
        static Data()
        {
            if (Values.I32.Length < 34 || Values.I32[0] != 6 || Values.I32[1] != 16 || Values.F32.Length % 35 != 0)
                throw new InvalidOperationException("Invalid Lift03 source-road mapping schema.");
        }
    }
}
// Embedded numerical arrays: Types_Lift03_Lift03SourceRoadMapping_cs
