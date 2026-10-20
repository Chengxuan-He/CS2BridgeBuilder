using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native LiftBridge01 mesh 0, game 1.6.2f1, SHA-256
// cc34bfcb478f5621513c458120bacfb395e21548e3eee2e753dca97e138b5714.
// Central floor unit: X=-2..0, Z=87.779296875..91.779296875,
// faces 11144/11145. Moving units use the same atlas rectangle, with a
// separate longitudinal origin. Rails and sleepers are separate geometry.
internal static class Lift01RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[8];
        for (var moving = 0; moving < 2; moving++)
        for (var half = 0; half < 2; half++)
        for (var side = 0; side < 2; side++)
        {
            var index = moving * 4 + half * 2 + side;
            result[index] = new Patch(
                new Vector2(.57421875f, .260009765625f),
                new Vector2(.6796875f, .469970703125f),
                frame * new Vector4(side == 0 ? -.5f : .5f, 0f, 0f, 0f),
                frame * new Vector4(0f, 0f, half == 0 ? -.25f : .25f,
                    moving == 0 ? 91.779296875f / 4f : 19f), repeat: true);
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
                0 => 0,
                4 => 4,
                _ => throw new InvalidOperationException("Lift01 floor appearance requires fixed or road bone ownership.")
            };
            return new Partition(pose, Sides(offset + 2), Sides(offset));
        }
        return Convert(Lift01RoadPartitions.Carriageway);
    }
}
