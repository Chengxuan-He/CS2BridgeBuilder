using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// LiftBridge03 native mesh-0 Surface d5708265307c0c422270f918fb8ca8e8.
// Source atlas 4096 square, VT 8c8c7c348b3a3114bc610b7dcc7f305f.
// Upper clean asphalt excludes baked white lines; lower unit retains native ballast.
internal static class Lift03RoadAppearance
{
    internal static Patch[] Patches(Lift03RoadSampling.Deck deck, Pose roadToObject)
    {
        Vector2 min, max;
        float du, dv;
        switch (deck)
        {
            case Lift03RoadSampling.Deck.Main:
                min = new Vector2(3136f / 4096f, 40f / 4096f);
                max = new Vector2(3520f / 4096f, 1032f / 4096f);
                // Native face 23857: 124 and 124.359375 source pixels per metre.
                du = .0302734375f;
                dv = .030361175537109375f;
                break;
            case Lift03RoadSampling.Deck.Auxiliary:
                min = new Vector2(2820f / 4096f, 1048f / 4096f);
                max = new Vector2(4060f / 4096f, 2040f / 4096f);
                // Native face 27296: 249 source pixels per metre on both axes.
                du = -.060791015625f;
                dv = .060791015625f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(deck));
        }
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var uScale = du / (max.x - min.x);
        var vScale = dv / (max.y - min.y);
        Patch Unit(float xSign, float zSign) => new(min, max,
            frame * new Vector4(xSign * uScale, 0f, 0f, 0f),
            frame * new Vector4(0f, 0f, zSign * vScale, 0f), repeat: true);
        // Left-negative Z, left-positive Z, right-negative Z, right-positive Z.
        return new[] { Unit(1f, 1f), Unit(1f, -1f), Unit(-1f, 1f), Unit(-1f, -1f) };
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var lateral = Matrix4x4.Rotate(Quaternion.Euler(0f, -90f, 0f));
        return new Partition(lateral * pose,
            new Partition(pose, new Partition(0), new Partition(1)),
            new Partition(pose, new Partition(2), new Partition(3)));
    }
}
