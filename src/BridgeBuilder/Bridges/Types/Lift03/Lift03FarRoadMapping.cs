using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Four exact finite charts of the original LOD2 upper road. Unlike LOD0/1,
// neither axis repeats and U continues across the centre without mirroring.
internal static class Lift03FarRoadMapping
{
    internal static Patch[] Patches(Pose roadToObject, float halfDelta)
    {
        var values = Data.Values.F32;
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[Data.Values.I32[1]];
        // Original native oct15 frame is +X/-1 on all sixteen far road faces.
        var axis = Quaternion.Inverse(roadToObject.rotation) * Vector3.right;
        var tangent = new Vector4(axis.x, axis.y, axis.z, -1);
        for (int i = 0; i < result.Length; i++)
        {
            int at = i * 11;
            // Retain the original transverse traversal as its geometry widens.
            float scale = values[at] / (values[at] + halfDelta);
            var u = frame * new Vector4(values[at + 7] * scale, 0, 0, values[at + 9]);
            var v = frame * new Vector4(0, 0, values[at + 8], values[at + 10]);
            result[i] = new Patch(new Vector2(values[at + 3], values[at + 4]),
                new Vector2(values[at + 5], values[at + 6]), u, v, sourceUv: true, tangent: tangent);
        }
        return result;
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Cut(float station, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m23 = -station;
            return new Partition(frame * pose, negative, positive);
        }
        // These are existing physical motion seams, not texture-period cuts.
        return Cut(-Lift03RoadPartitions.FixedStart, new Partition(0),
            Cut(0, new Partition(1),
                Cut(Lift03RoadPartitions.FixedStart, new Partition(2), new Partition(3))));
    }

    private static class Data
    {
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03FarRoadMapping_cs");
        static Data()
        {
            if (Values.I32.Length != 2 || Values.I32[0] != 1 || Values.I32[1] != 4 || Values.F32.Length != 44)
                throw new InvalidOperationException("Invalid Lift03 far-road mapping schema.");
        }
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03FarRoadMapping_cs
