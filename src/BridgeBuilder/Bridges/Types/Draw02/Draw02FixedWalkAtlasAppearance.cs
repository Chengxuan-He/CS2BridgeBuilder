using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;

// Fixed walk: independent cap, ramp, finite short walk and original 4 m atlas
// traversal. LOD1's triangular ramp boundary is its actual material silhouette.
internal static class Draw02FixedWalkAtlasAppearance
{
    internal static MovableRoadAppearance.Level Apply(int lod, MovableRoadAppearance.Level level,
        MovableSelectedRoadLayout layout, Pose pose)
    {
        if (lod < 0 || lod > 1) throw new ArgumentOutOfRangeException(nameof(lod));
        var values = Numeric_Types_Draw02_Draw02FixedWalkAtlasAppearance_cs.Values.F32;
        var constants = values.Length - 6;
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        var first = level.Patches.Length; var outside = first + 16;
        var patches = new Patch[outside + 1]; var support = new Type[outside + 1];
        Array.Copy(level.Patches, patches, first); Array.Copy(level.SupportedTypes, support, first);
        patches[outside] = new Patch(Vector2.zero, Vector2.one);
        var scales = new float[4]; var offsets = new float[4];
        for (var sideDomain = 0; sideDomain < 4; sideDomain++)
        {
            var side = sideDomain % 2 == 0 ? -1f : 1f;
            var span = side < 0f ? layout.LeftSidewalk : layout.RightSidewalk;
            if (!(span.y > span.x)) span = side < 0f
                ? new Vector2(-values[constants + 1], -values[constants])
                : new Vector2(values[constants], values[constants + 1]);
            var scale = (values[constants + 1] - values[constants]) / (span.y - span.x);
            var inner = side < 0f ? -span.y : span.x;
            var offset = values[constants] - scale * inner;
            scales[sideDomain] = scale; offsets[sideDomain] = offset;
            for (var part = 0; part < 4; part++)
            {
                var domain = sideDomain * 4 + part; var row = (lod * 16 + domain) * 12;
                var u = projection * new Vector4(values[row] * scale, 0f, 0f,
                    values[row + 1] + values[row] * side * offset);
                var v = projection * new Vector4(0f, 0f, values[row + 2], values[row + 3]);
                patches[first + domain] = part == 3
                    ? Draw02PreparedAtlas.Repeat(2, u, v, projectTangent: true)
                    : new Patch(new Vector2(values[row + 4], values[row + 5]),
                    new Vector2(values[row + 6], values[row + 7]), u, v, level.Native,
                    sourceUv: true, projectTangent: true);
                support[first + domain] = Type.Asphalt | Type.Sidewalk;
            }
        }
        Partition Cut(float x, float z, float offset, Partition negative, Partition positive)
        {
            var matrix = Matrix4x4.identity; matrix.m20 = x; matrix.m22 = z; matrix.m23 = offset;
            matrix.m00 = z; matrix.m02 = -x;
            return new Partition(matrix * transform, negative, positive);
        }
        Partition Domain(int sideDomain, int part)
        {
            var side = sideDomain % 2 == 0 ? -1f : 1f;
            var end = sideDomain < 2 ? 1f : -1f;
            var domain = sideDomain * 4 + part; var row = (lod * 16 + domain) * 12;
            var hit = new Partition(first + domain); var miss = new Partition(outside);
            return Cut(side * scales[sideDomain], 0f, offsets[sideDomain] - values[row + 9], miss,
                Cut(side * scales[sideDomain], -values[row + 10] * end,
                    offsets[sideDomain] - values[row + 11], hit, miss));
        }
        Partition Side(int sideDomain)
        {
            var sign = sideDomain < 2 ? 1f : -1f;
            return Cut(0f, sign, -values[constants + 2], Domain(sideDomain, 0),
                Cut(0f, sign, -values[constants + 3], Domain(sideDomain, 1),
                Cut(0f, sign, -values[constants + 4], Domain(sideDomain, 2),
                Cut(0f, sign, -values[constants + 5], Domain(sideDomain, 3), new Partition(outside)))));
        }
        return new MovableRoadAppearance.Level(level.Native, patches, support, level.Carriageway,
            Cut(0f, 1f, 0f, Cut(1f, 0f, 0f, Side(2), Side(3)), Cut(1f, 0f, 0f, Side(0), Side(1))));
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02FixedWalkAtlasAppearance_cs
internal static class Numeric_Types_Draw02_Draw02FixedWalkAtlasAppearance_cs
{
    static Numeric_Types_Draw02_Draw02FixedWalkAtlasAppearance_cs() { }
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02FixedWalkAtlasAppearance_cs");
}
