using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using RepeatBand = BridgeBuilder.Bridges.MovableExistingRoadSurface.RepeatBand;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// DrawBridge02 mesh 0, game 1.6.2f1. Geometry SHA-256:
// 1f0ff1a2c0a8318e6537bd347ae064d1bb55bfa48f3cf3c2375601adcd05dd18.
// Measured top faces: 3680/3756 (grating), 3684/3760/3784 (solid strips),
// 3634/3710 (walkway), 2492/2512 (fixed road), 10701/10703 (fixed walkway).
internal static class Draw02RoadAppearance
{
    // One complete transverse period, including both halves of the solid strip.
    // Sharing this immutable recipe also shares the generated repeating material.
    private static readonly RepeatBand[] CarriagewayBands =
    {
        new(0f, .20000076293945312f / 1.5f,
            new Vector2(.602294921875f, .27392578125f), new Vector2(.6123046875f, .475830078125f)),
        new(.20000076293945312f / 1.5f, 1.29998779296875f / 1.5f,
            new Vector2(.5234375f, .27392578125f), new Vector2(.5791015625f, .475830078125f)),
        new(1.29998779296875f / 1.5f, 1f,
            new Vector2(.59228515625f, .27392578125f), new Vector2(.602294921875f, .475830078125f))
    };

    internal static Patch[] Patches(MovableSelectedRoadLayout layout, Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[16];
        for (var half = 0; half < 2; half++)
        for (var side = 0; side < 2; side++)
        {
            var offset = half * 8;
            var movingV = new Vector4(0, 0, half == 0 ? -.25f : .25f, 11f);
            // Keep the measured UV gradient (density), but anchor each sidewalk at
            // its selected-road outer edge. Asymmetric layouts need separate origins.
            var span = side == 0 ? layout.LeftSidewalk : layout.RightSidewalk;
            var gradient = side == 0 ? -.3448246832512522f : .3448246832512522f;
            var outer = side == 0 ? span.x : span.y;
            var walkU = new Vector4(gradient, 0, 0, 1f - gradient * outer);
            result[offset + side] = new Patch(new Vector2(.5234375f, .27392578125f),
                new Vector2(.5791015625f, .475830078125f), frame * new Vector4(-1f / 1.5f, 0, 0, 0),
                frame * movingV, repeat: true, bands: CarriagewayBands);
            result[offset + 2 + side] = new Patch(new Vector2(.5234375f, .48876953125f),
                new Vector2(.669921875f, .69091796875f), frame * walkU, frame * movingV, repeat: true);
            // Negative fixed road faces 24658..24661 retain negative V slope;
            // unlike the moving leaf, they are not a simple mirrored projection.
            result[offset + 4 + side] = new Patch(new Vector2(.3251953125f, .48876953125f),
                new Vector2(.47216796875f, .67236328125f), frame * new Vector4(-1f / 3f, 0, 0, side),
                frame * new Vector4(0, 0, -.25f, half == 0 ? 21.5f : -20.5f));
            result[offset + 6 + side] = new Patch(new Vector2(.339111328125f, .68505859375f),
                new Vector2(.47216796875f, .86865234375f), frame * walkU,
                frame * new Vector4(0, 0, half == 0 ? -.25f : .25f, 21.5f));
        }
        return result;
    }

    internal static Partition Regions(bool sidewalk, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Z(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity; frame.m23 = -z;
            return new Partition(frame * pose, negative, positive);
        }
        Partition Half(int offset)
        {
            var start = offset + (sidewalk ? 2 : 0);
            Partition Side(int side) => Z(-78.7421875f, new Partition(start + side + 4),
                Z(78.7421875f, new Partition(start + side), new Partition(start + side + 4)));
            var frame = Matrix4x4.identity;
            frame.m00 = 0; frame.m02 = -1; frame.m20 = 1; frame.m22 = 0;
            return new Partition(frame * pose, Side(0), Side(1));
        }
        return Z(0, Half(8), Half(0));
    }
}
