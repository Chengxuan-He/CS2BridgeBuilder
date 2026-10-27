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
        var innerCurb = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[0] + halfWidthDelta;
        if (!float.IsFinite(innerCurb) || innerCurb <= Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[1])
            throw new InvalidOperationException("The selected width collapses the native moving carriageway domain.");
        var sidewalk = retainNativePlatforms ? Cut(-Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[2], Partition.Gap, Cut(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[3], Sidewalk, Partition.Gap)) : Sidewalk;
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
            frame.m00 = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[4];
            frame.m02 = -Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[5];
            frame.m20 = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[6];
            frame.m22 = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[7];
            frame.m23 = -x;
            return new Partition(frame * pose, negative, positive);
        }

        // Native fixed sidewalk top begins at +/-78.150390625 m. Only the leaves
        // use cutout grating; the end platforms use the opaque native paving patch.
        Partition CutZ(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m23 = -z;
            return new Partition(frame * pose, negative, positive);
        }

        Partition Half(int offset)
        {
            var leftWalkway = CutZ(-Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[8], new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[0] + offset), CutZ(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[9], new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[1] + offset), new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[2] + offset)));
            var rightWalkway = CutZ(-Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[10], new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[3] + offset), CutZ(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[11], new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[4] + offset), new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[5] + offset)));
            Partition Carriageway(int moving, int fixedCap) => CutZ(-Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[12], new Partition(fixedCap + offset), CutZ(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[13], new Partition(moving + offset), new Partition(fixedCap + offset)));
            var roadway = CutX(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[14], Carriageway(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[6], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[7]), Carriageway(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[8], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[9]));
            return sidewalk ? CutX(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[15], leftWalkway, rightWalkway) : roadway;
        }

        return CutZ(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[16], Half(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[10]), Half(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[11]));
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
        frame.m00 = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[17];
        frame.m02 = -Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[18];
        frame.m20 = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[19];
        frame.m22 = Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[20];
        frame.m23 = -seam;
        return new Partition(frame, negative, positive);
    }

    internal static readonly Partition Carriageway = Create(-Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[21], -Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[22], -Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[23], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[24], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[25], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[26]);
    internal static readonly Partition Sidewalk = Create(-Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[27], -Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[28], -Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[29], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[30], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[31], Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.F32[32]);
    // In increasing Z: fixed, end gap, negative leaf, central gap, positive leaf,
    // end gap, fixed. Bone ARRAY indices 1/2 correspond to native connections 100/101.
    // Sampling supplies the finite node extent; these trees do not fabricate approaches.
    private static Partition Create(float negativeFixedEnd, float negativeLeafStart, float negativeLeafEnd, float positiveLeafStart, float positiveLeafEnd, float positiveFixedStart)
    {
        var fixedRoad = new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[12]);
        return Cut(negativeFixedEnd, fixedRoad, Cut(negativeLeafStart, Partition.Gap, Cut(negativeLeafEnd, new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[13]), Cut(positiveLeafStart, Partition.Gap, Cut(positiveLeafEnd, new Partition(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[14]), Cut(positiveFixedStart, Partition.Gap, fixedRoad))))));
    }

    private static Partition Cut(float seam, Partition negative, Partition positive)
    {
        var objectToSeam = Matrix4x4.identity;
        objectToSeam.m23 = -seam;
        return new Partition(objectToSeam, negative, positive);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01RoadPartitions_cs
internal static class Numeric_Types_Draw01_Draw01RoadPartitions_cs
{
    static Numeric_Types_Draw01_Draw01RoadPartitions_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01RoadPartitions_cs");
}
