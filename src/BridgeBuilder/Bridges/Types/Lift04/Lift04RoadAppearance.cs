using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native LiftBridge04 mesh 0, Geometry SHA-256:
// 869158c6d06c9a9717851460adf922838d45c294c4b1e692b7b96e4585422980.
// Repeating units measured at faces 4198/4199, 28426/28427,
// 10581/10582 and 33328/33329. Preserve the prototype bed's own appearance.
internal static class Lift04RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var min = new Vector2(.4697265625f, .8251953125f);
        var max = new Vector2(.62548828125f, .9814453125f);
        const float movingStart = 64.72265625f;
        const float movingLength = 68.529296875f - movingStart;
        Patch Unit(float direction, float length, float start, float lateral) => new(min, max,
            frame * new Vector4(0f, 0f, direction / length, -start / length),
            frame * new Vector4(lateral / 4f, 0f, 0f, 0f), repeat: true);
        return new[]
        {
            Unit(-1f, 4f, 92f, 1f), Unit(1f, 4f, 92f, -1f),
            Unit(-1f, movingLength, movingStart, -1f), Unit(1f, movingLength, movingStart, -1f)
        };
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            // Appearance indices are independent of the unchanged physical bones 0/2.
            var first = node.Bone == 0 ? 0 : 2;
            return new Partition(pose, new Partition(first), new Partition(first + 1));
        }
        return Convert(Lift04RoadPartitions.Carriageway);
    }
}
