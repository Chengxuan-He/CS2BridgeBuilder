using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// DrawBridge03 mesh 0, game 1.6.2f1. Geometry SHA-256:
// f7ecc824ad55745e791e40b18fd12f5def20657ed8ea56d373429d5a1337588f.
// Native top faces 58046/58054, 58174/58180 and their opposite-side copies.
// Clean asphalt crop: pixels [3084,4052) x [1312,1476), bottom-left UV origin.
// The original complete road island contains paint and must not be repeated.
internal static class Draw03RoadAppearance
{
    private const float FixedUnitEnd = 88.041015625f;
    private const float OuterEnd = 96f;

    internal static Patch[] Patches(MovableSelectedRoadLayout layout, Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[24];
        for (var zone = 0; zone < 3; zone++)
        for (var half = 0; half < 2; half++)
        for (var walk = 0; walk < 2; walk++)
        for (var side = 0; side < 2; side++)
        {
            var direction = half == 0 ? -1f : 1f;
            var sideSign = side == 0 ? -1f : 1f;
            var length = zone == 2 ? OuterEnd - FixedUnitEnd : 8f;
            var origin = zone == 2 ? OuterEnd : zone == 1 ? FixedUnitEnd
                : walk == 0 ? 72.015625f : 80.015625f;
            var u = frame * new Vector4(0f, 0f, -direction / length, origin / length);
            Vector2 min, max;
            Vector4 v;
            if (walk != 0)
            {
                min = new Vector2(.7529296875f, .01087188720703125f);
                max = new Vector2(.9892578125f, .1024169921875f);
                var span = side == 0 ? layout.LeftSidewalk : layout.RightSidewalk;
                var outer = side == 0 ? span.x : span.y;
                // Move the sampling origin with each selected sidewalk's outer edge;
                // retain the measured 3.10009765625 m native period independently.
                var gradient = -sideSign / 3.10009765625f;
                v = frame * new Vector4(gradient, 0f, 0f, -gradient * outer);
            }
            else
            {
                min = new Vector2(.7529296875f, .3203125f);
                max = new Vector2(.9892578125f, .3603515625f);
                // Preserve the native atlas-V/metre gradient after selecting a smaller
                // paint-free crop. The crop is not stretched across the road width.
                var width = 9.39990234375f - sideSign * direction * .0000019073486328125f;
                var gradient = -sideSign * (.385986328125f - .10833740234375f) / width;
                var crop = max.y - min.y;
                v = frame * new Vector4(gradient / crop, 0f, 0f,
                    (.385986328125f - gradient * direction * .0000019073486328125f - min.y) / crop);
            }
            result[zone * 8 + half * 4 + walk * 2 + side] = new Patch(min, max, u, v, repeat: true);
        }
        return result;
    }

    internal static Partition Regions(bool sidewalk, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Cut(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity; frame.m23 = -z;
            return new Partition(frame * pose, negative, positive);
        }
        Partition Sides(int zone, int half)
        {
            var offset = zone * 8 + half * 4 + (sidewalk ? 2 : 0);
            var frame = Matrix4x4.identity;
            frame.m00 = 0f; frame.m02 = -1f; frame.m20 = 1f; frame.m22 = 0f;
            return new Partition(frame * pose, new Partition(offset), new Partition(offset + 1));
        }
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            return node.Bone switch
            {
                0 => Cut(0f, Cut(-FixedUnitEnd, Sides(2, 0), Sides(1, 0)),
                    Cut(FixedUnitEnd, Sides(1, 1), Sides(2, 1))),
                1 => Sides(0, 1),
                2 => Sides(0, 0),
                _ => throw new InvalidOperationException("Draw03 road appearance requires its original three bones.")
            };
        }
        return Convert(sidewalk ? Draw03RoadPartitions.Sidewalk : Draw03RoadPartitions.Carriageway);
    }
}
