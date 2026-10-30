using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Embedded numerical arrays: Types_PedestrianDraw01_FarRoadMapping
// Original LOD2 has eight finite walking charts. Neither its atlas regions nor
// their longitudinal direction match the near/middle repeated walking sheets.
internal static class PedestrianDraw01FarRoadMapping
{
    internal static Patch[] Patches(SurfaceAsset surface, Pose roadToObject, float halfWidthDelta)
    {
        var transform = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var values = Data.Values.F32;
        var result = new Patch[Data.Values.I32[1]];
        for (var index = 0; index < result.Length; index++)
        {
            var at = index * 16;
            var halfSpan = values[at + 1];
            var widthScale = halfSpan / (halfSpan + halfWidthDelta);
            var u = new Vector4(values[at + 8] * widthScale, values[at + 9], values[at + 10], values[at + 11]);
            var v = new Vector4(values[at + 12] * widthScale, values[at + 13], values[at + 14], values[at + 15]);
            result[index] = new Patch(new Vector2(values[at + 4], values[at + 5]),
                new Vector2(values[at + 6], values[at + 7]), transform * u, transform * v,
                surface: surface, sourceUv: true);
        }
        return result;
    }

    internal static Partition Regions(Partition physical, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Cut(float station, int negative, int positive)
        {
            var frame = Matrix4x4.identity;
            frame.m23 = -station;
            return new Partition(frame * pose, new Partition(negative), new Partition(positive));
        }
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            // Bone IDs are measured in the same source records as the chart pairs.
            var values = Data.Values;
            if (node.Bone == values.I32[2])
                return new Partition(pose, Cut(values.F32[16 + 2], 0, 1), Cut(values.F32[7 * 16 + 2], 6, 7));
            if (node.Bone == values.I32[2 + 2 * 3]) return Cut(values.F32[3 * 16 + 2], 2, 3);
            if (node.Bone == values.I32[2 + 4 * 3]) return Cut(values.F32[5 * 16 + 2], 4, 5);
            throw new InvalidOperationException("PedestrianDraw01 far walking chart has no measured bone domain.");
        }
        return Convert(physical);
    }

    private static class Data
    {
        static Data()
        {
            if (Values.I32.Length != 26 || Values.I32[0] != 1 || Values.I32[1] != 8 || Values.F32.Length != 128)
                throw new InvalidOperationException("Invalid PedestrianDraw01 far road mapping schema.");
        }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_FarRoadMapping");
    }
}
