using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native PedestrianDrawBridge01 mesh 0, Geometry SHA-256:
// 491d8dae6420e0b5b00fbf0014ff74a8aff44c45a1bd48ab40708978888d7f05.
// Fixed units: faces 1035/1036 and 13243/13244. Moving: 1091/1092 and 13299/13300.
// Both leaves retain the same negative longitudinal UV gradient; their origins differ.
internal static class PedestrianDraw01RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        const float movingWidth = 3.68646240234375f;
        const float movingLength = 9.05517578125f - 2.2608642578125f;
        Patch Fixed(float minV, float end) => new(
            new Vector2(.3837890625f, minV),
            new Vector2(.5712890625f, .282470703125f),
            frame * new Vector4(-.25f, 0f, 0f, 1f),
            frame * new Vector4(0f, 0f, -.25f, end / 4f), repeat: true);
        Patch Moving(float end) => new(
            new Vector2(.3837890625f, .09490966796875f),
            new Vector2(.5712890625f, .440673828125f),
            frame * new Vector4(-1f / movingWidth, 0f, 0f, 1f),
            frame * new Vector4(0f, 0f, -1f / movingLength, end / movingLength), repeat: true);
        return new[]
        {
            Fixed(.0948486328125f, 68f), Fixed(.09490966796875f, -64f),
            Moving(9.05517578125f), Moving(-2.2608642578125f)
        };
    }

    internal static Partition Regions(Partition physical, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            if (node.Bone == 0)
                return new Partition(pose, new Partition(1), new Partition(0));
            return new Partition(node.Bone + 1);
        }
        return Convert(physical);
    }
}
