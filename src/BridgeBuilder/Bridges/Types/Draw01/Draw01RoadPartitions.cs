using System;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native DrawBridge01 main mesh 0, game 1.6.2f1, decoded binary32 boundaries.
// Geometry SHA-256: f9cc32a1e42568aa26b3248ad2cdb6fee7de23d3dfeaa0b56e7714c51e0d1abe
// Physical motion domains are recorded from the prototype, not mapped from road prefabs.
// Every generated road LOD uses the same highest-detail logical seams; the native
// far mesh's flattened proxy is not a different road or a different bone assignment.
internal static class Draw01RoadPartitions
{
    // Partition the evaluated geometry in mechanism space, independent of the source
    // section, piece, material, or lane type. The native inner curb boundary is
    // x = +/-3.4000244 m; it follows the same lateral displacement as the structure.
    // Triangles spanning this boundary are clipped, not assigned by their centroid.
    internal static Partition Surface(float halfWidthDelta, bool retainNativePlatforms = false)
    {
        var innerCurb = 3.4000244f + halfWidthDelta;
        if (!float.IsFinite(innerCurb) || innerCurb <= 0f)
            throw new InvalidOperationException("The selected width collapses the native moving carriageway domain.");
        var sidewalk = retainNativePlatforms
            ? Cut(-78.150390625f, Partition.Gap, Cut(78.150390625f, Sidewalk, Partition.Gap))
            : Sidewalk;
        return Across(-innerCurb, sidewalk, Across(innerCurb, Carriageway, sidewalk));
    }

    // The selected Sidewalk section supplies its evaluated width and side, including
    // the fixed platforms. No prototype-width strip or pedestrian-lane heuristic creates one.
    internal static Partition RoadSurface(bool sidewalk) => sidewalk ? Sidewalk : Carriageway;

    internal static Partition MaterialRegions(bool sidewalk, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition CutX(float x, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m00 = 0f; frame.m02 = -1f;
            frame.m20 = 1f; frame.m22 = 0f; frame.m23 = -x;
            return new Partition(frame * pose, negative, positive);
        }
        // Native fixed sidewalk top begins at +/-78.150390625 m. Only the leaves
        // use cutout grating; the end platforms use the opaque native paving patch.
        Partition CutZ(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity; frame.m23 = -z;
            return new Partition(frame * pose, negative, positive);
        }
        Partition Half(int offset)
        {
            var leftWalkway = CutZ(-78.150390625f, new Partition(2 + offset),
                CutZ(78.150390625f, new Partition(1 + offset), new Partition(2 + offset)));
            var rightWalkway = CutZ(-78.150390625f, new Partition(2 + offset),
                CutZ(78.150390625f, new Partition(4 + offset), new Partition(2 + offset)));
            Partition Carriageway(int moving, int fixedCap) =>
                CutZ(-83.740234375f, new Partition(fixedCap + offset),
                    CutZ(83.740234375f, new Partition(moving + offset), new Partition(fixedCap + offset)));
            var roadway = CutX(0f, Carriageway(0, 3), Carriageway(5, 6));
            return sidewalk ? CutX(0f, leftWalkway, rightWalkway) : roadway;
        }
        return CutZ(0f, Half(7), Half(0));
    }

    internal static Partition[] ForObjects(MovableNodeObjects.Result objects, float halfWidthDelta)
    {
        var partition = Surface(halfWidthDelta);
        var result = new Partition[objects.Placements.Length];
        Array.Fill(result, partition);
        return result;
    }

    private static Partition Across(float seam, Partition negative, Partition positive)
    {
        // Exact quarter-turn: seam-local Z is object X minus the measured seam.
        var frame = Matrix4x4.identity;
        frame.m00 = 0f; frame.m02 = -1f;
        frame.m20 = 1f; frame.m22 = 0f; frame.m23 = -seam;
        return new Partition(frame, negative, positive);
    }

    internal static readonly Partition Carriageway = Create(
        -83.740234375f, -83.58984375f, -0.05267906188964844f,
        0.05267906188964844f, 83.58984375f, 83.740234375f);

    internal static readonly Partition Sidewalk = Create(
        -78.150390625f, -78.05078125f, -0.03000020980834961f,
        0.03000020980834961f, 78.05078125f, 78.150390625f);

    // In increasing Z: fixed, end gap, negative leaf, central gap, positive leaf,
    // end gap, fixed. Bone ARRAY indices 1/2 correspond to native connections 100/101.
    // Sampling supplies the finite node extent; these trees do not fabricate approaches.
    private static Partition Create(float negativeFixedEnd, float negativeLeafStart,
        float negativeLeafEnd, float positiveLeafStart, float positiveLeafEnd,
        float positiveFixedStart)
    {
        var fixedRoad = new Partition(0);
        return Cut(negativeFixedEnd, fixedRoad,
            Cut(negativeLeafStart, Partition.Gap,
                Cut(negativeLeafEnd, new Partition(1),
                    Cut(positiveLeafStart, Partition.Gap,
                        Cut(positiveLeafEnd, new Partition(2),
                            Cut(positiveFixedStart, Partition.Gap, fixedRoad))))));
    }

    private static Partition Cut(float seam, Partition negative, Partition positive)
    {
        var objectToSeam = Matrix4x4.identity;
        objectToSeam.m23 = -seam;
        return new Partition(objectToSeam, negative, positive);
    }
}
