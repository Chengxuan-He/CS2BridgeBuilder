using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Measured LiftBridge02 mesh 0 roadbed top: 212 faces, starting at 83248.
// Original 4096px atlas, 4m longitudinal unit and 0.42156982421875m lateral strip.
internal static class Lift02RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var min = new Vector2(.92041015625f, .046112060546875f);
        var max = new Vector2(.9990234375f, .05438232421875f);
        var result = new Patch[2];
        for (var side = 0; side < 2; side++)
        {
            var sign = side == 0 ? 1f : -1f;
            result[side] = new Patch(min, max,
                frame * new Vector4(0f, 0f, -.25f, 26.5f),
                frame * new Vector4(sign / .42156982421875f, 0f, 0f,
                    4.2095947265625f / .42156982421875f), repeat: true);
        }
        return result;
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var side = Matrix4x4.identity;
        side.m00 = 0f; side.m02 = -1f; side.m20 = 1f; side.m22 = 0f;
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            return new Partition(side * pose, new Partition(0), new Partition(1));
        }
        return Convert(Lift02RoadPartitions.Carriageway);
    }
}
