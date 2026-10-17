using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// DrawBridge01_LOD2 mesh 0, game 1.6.2f1. These frames are derived from the
// original faces 22, 25, 350 and 353, not from the generated near-detail mesh.
// UV axes follow each original leaf/side. Interior rectangles exclude baked
// prototype paint and atlas gutters; cropping retains the measured UV/metre scale.
internal static class Draw01RoadLodProjection
{
    internal static void Far(MovableExistingRoadSurface.Patch[] patches, Matrix4x4 frame,
        SurfaceAsset surface)
    {
        const float length = 68.16016f - .02999878f;
        const float width = 2.8856812f;
        Set(0, 398, 405,
            new Vector4(0, 0, (.1270751953125f - .728515625f) / length,
                .728515625f - (.1270751953125f - .728515625f) / length * .02999878f),
            new Vector4((.8017578125f - .7763671875f) / width, 0,
                (.8017578125f - .7998046875f) / length,
                .7998046875f - (.8017578125f - .7998046875f) / length * .02999878f));
        Set(5, 319, 325,
            new Vector4(0, 0, (.0955810546875f - .697265625f) / length,
                .697265625f - (.0955810546875f - .697265625f) / length * .02999878f),
            new Vector4((.640625f - .615234375f) / width, 0, 0, .615234375f));
        Set(7, 368, 373,
            new Vector4(0, 0, -(.638671875f - .037261962890625f) / length,
                .037261962890625f - (.638671875f - .037261962890625f) / length * .02999878f),
            new Vector4((.736328125f - .7109375f) / width, 0, 0, .736328125f));
        Set(12, 434, 438,
            new Vector4((.11529541015625f - .1153564453125f) / width, 0,
                -(.1153564453125f - .716796875f) / length,
                .716796875f - (.1153564453125f - .716796875f) / length * .02999878f),
            new Vector4((.83984375f - .865234375f) / width, 0,
                -(.865234375f - .86328125f) / length,
                .86328125f - (.865234375f - .86328125f) / length * .02999878f));

        void Set(int region, int firstRow, int lastRow, Vector4 u, Vector4 v)
        {
            var min = new Vector2(40f / 512f, firstRow / 512f);
            var max = new Vector2(349f / 512f, lastRow / 512f);
            // The shared resolver repeats only at atlas boundaries. The old far
            // mesh's long UV axis therefore produces long panels, not 4 m near tiles.
            u.w -= min.x; u /= max.x - min.x;
            v.w -= min.y; v /= max.y - min.y;
            patches[region] = new MovableExistingRoadSurface.Patch(min, max,
                frame * u, frame * v, surface);
        }
    }
}
