using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native PedestrianDrawBridge02 mesh 0, game 1.6.2f1.
// Geometry SHA-256: 55b8fb530c4f5d80a211feecb2e82b6deb12cc8af5d76bdc3abef78b192494be.
// Fixed unit: faces 6708/6709, X=2.6348876953125..3.68646240234375, Z=68..72.
// Moving unit: faces 8556/8557, X=1.9114990234375..3.68646240234375,
// Z=2.2608642578125..9.05517578125. Rectangles are inside the authored asphalt island.
// Repetition preserves each measured UV gradient instead of stretching an atlas crop.
internal static class PedestrianDraw02RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        const float fixedWidth = 3.68646240234375f - 2.6348876953125f;
        const float movingWidth = 3.68646240234375f - 1.9114990234375f;
        const float movingLength = 9.05517578125f - 2.2608642578125f;
        Patch Fixed(float direction) => new(
            new Vector2(.81787109375f, .150634765625f),
            new Vector2(.86865234375f, .34423828125f),
            frame * new Vector4(direction / fixedWidth, 0f, 0f, -2.6348876953125f / fixedWidth),
            frame * new Vector4(0f, 0f, direction / 4f, -17f), repeat: true);
        Patch Moving(float direction) => new(
            new Vector2(.69775390625f, .0830078125f),
            new Vector2(.78369140625f, .411865234375f),
            frame * new Vector4(-direction / movingWidth, 0f, 0f, 3.68646240234375f / movingWidth),
            frame * new Vector4(0f, 0f, -direction / movingLength, 9.05517578125f / movingLength), repeat: true);
        return new[] { Fixed(1f), Fixed(-1f), Moving(1f), Moving(-1f) };
    }

    // Physical leaves carry original bone indices; appearance leaves carry patch
    // indices. Transform every seam into sampled-road space before the atlas pass.
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
