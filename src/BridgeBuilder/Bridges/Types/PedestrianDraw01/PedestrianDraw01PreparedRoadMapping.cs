using System;
using BridgeBuilder.Runtime;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Embedded numerical arrays: Types_PedestrianDraw01_PreparedRoadMapping
// Native x=0 UV boundaries remain distinct. Each finished half-width unit is
// shared by both LODs; its finite U guard and longitudinal repeat are independent.
internal static class PedestrianDraw01PreparedRoadMapping
{
    private static readonly string[] Keys = {
        "9a7f4b58ff26c496ab8b4441fd5614c707c5880717fcd52c43a81cce887db6dc",
        "0654d55f6457bbf76e4a452b45560f6480e62d24eebd602a74bc62fade7089ae",
        "9a7f4b58ff26c496ab8b4441fd5614c707c5880717fcd52c43a81cce887db6dc",
        "9a7f4b58ff26c496ab8b4441fd5614c707c5880717fcd52c43a81cce887db6dc",
        "19e7aa9db90b4ad913271ca9a959209161a0d865e65082b8c3171e2c8accb650",
        "19e7aa9db90b4ad913271ca9a959209161a0d865e65082b8c3171e2c8accb650",
        "19e7aa9db90b4ad913271ca9a959209161a0d865e65082b8c3171e2c8accb650",
        "19e7aa9db90b4ad913271ca9a959209161a0d865e65082b8c3171e2c8accb650",
    };

    internal static Patch[] Patches(Pose roadToObject, float halfWidthDelta)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var values = Data.Values.F32;
        var result = new Patch[Keys.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var at = index * 9;
            var halfWidth = values[at] + halfWidthDelta;
            if (!(halfWidth > 0))
                throw new InvalidOperationException("PedestrianDraw01 walking unit has invalid finite width.");
            var u = new Vector4(values[at + 1] / halfWidth, 0, 0, values[at + 2]);
            var v = new Vector4(0, 0, values[at + 3], values[at + 4]);
            var tangent = Quaternion.Inverse(roadToObject.rotation) * new Vector3(values[at + 5], values[at + 6], values[at + 7]);
            result[index] = new Patch(Vector2.zero, Vector2.one, frame * u, frame * v,
                surface: BridgePreparedMaterials.RepeatUnit("PedestrianDraw01", Keys[index]),
                preparedUnit: true, repeatU: false, repeatV: true,
                tangent: new Vector4(tangent.x, tangent.y, tangent.z, values[at + 8]));
        }
        return result;
    }

    internal static Partition Regions(Partition appearance, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var transverse = Matrix4x4.identity;
        transverse.m00 = 0;
        transverse.m02 = -1;
        transverse.m20 = 1;
        transverse.m22 = 0;
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam, Convert(node.Negative), Convert(node.Positive));
            return new Partition(transverse * pose, new Partition(node.Bone * 2), new Partition(node.Bone * 2 + 1));
        }
        return Convert(appearance);
    }

    private static class Data
    {
        static Data()
        {
            if (Values.I32.Length != 2 || Values.I32[0] != 1 || Values.I32[1] != Keys.Length || Values.F32.Length != Keys.Length * 9)
                throw new InvalidOperationException("Invalid PedestrianDraw01 prepared walking mapping schema.");
        }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PreparedRoadMapping");
    }
}
