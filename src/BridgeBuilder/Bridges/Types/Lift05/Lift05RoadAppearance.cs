using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native LiftBridge05 mesh 0 UV0: fixed unit faces 557/558, moving 1366/1367.
// Clean U strips exclude the baked rail tracks in the complete native islands.
internal static class Lift05RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[8];
        for (var moving = 0; moving < 2; moving++)
        for (var half = 0; half < 2; half++)
        for (var side = 0; side < 2; side++)
        {
            var sideSign = side == 0 ? -1f : 1f;
            var halfSign = half == 0 ? -1f : 1f;
            var min = moving == 0 ? new Vector2(.41015625f, .402099609375f)
                : new Vector2(.052734375f, .61865234375f);
            var max = moving == 0 ? new Vector2(.4375f, .5546875f)
                : new Vector2(.083984375f, .76806640625f);
            var nativeMin = moving == 0 ? .3662109375f : .01251220703125f;
            var nativeMax = moving == 0 ? .480712890625f : .12432861328125f;
            var direction = moving == 0 ? -sideSign * halfSign : sideSign;
            var gradient = direction * (nativeMax - nativeMin) / 3f;
            var origin = moving == 0 && sideSign * halfSign > 0f ? nativeMax : nativeMin;
            var u = frame * new Vector4(gradient / (max.x - min.x), 0f, 0f,
                (origin - min.x) / (max.x - min.x));
            var v = frame * (moving == 0
                ? new Vector4(0f, 0f, -halfSign / 4f, 21f)
                : new Vector4(0f, 0f, -.25f, 8f));
            result[moving * 4 + half * 2 + side] = new Patch(min, max, u, v, repeat: true);
        }
        return result;
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Sides(int offset)
        {
            var seam = Matrix4x4.identity;
            seam.m00 = 0f; seam.m02 = -1f; seam.m20 = 1f; seam.m22 = 0f;
            return new Partition(seam * pose, new Partition(offset), new Partition(offset + 1));
        }
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            var offset = node.Bone switch
            {
                0 => 0, 2 => 4,
                _ => throw new InvalidOperationException("Lift05 road appearance requires fixed or road bone ownership.")
            };
            return new Partition(pose, Sides(offset), Sides(offset + 2));
        }
        return Convert(Lift05RoadPartitions.Carriageway);
    }
}
