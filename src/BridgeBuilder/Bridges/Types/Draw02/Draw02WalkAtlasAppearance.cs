using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;

// Original moving sidewalk atlas traversal. Its finite transverse chart is
// stretched over the selected sidewalk; its longitudinal density stays native.
internal static class Draw02WalkAtlasAppearance
{
    internal static MovableRoadAppearance.Level Apply(int lod, MovableRoadAppearance.Level level,
        MovableSelectedRoadLayout layout, Pose pose)
    {
        if (lod < 0 || lod > 1) throw new ArgumentOutOfRangeException(nameof(lod));
        var values = Numeric_Types_Draw02_Draw02WalkAtlasAppearance_cs.Values.F32;
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        var first = level.Patches.Length;
        var patches = new Patch[first + 12]; var support = new Type[first + 12];
        Array.Copy(level.Patches, patches, first); Array.Copy(level.SupportedTypes, support, first);
        for (var domain = 0; domain < 4; domain++)
        {
            var row = (lod * 4 + domain) * 14;
            var span = domain % 2 == 0 ? layout.LeftSidewalk : layout.RightSidewalk;
            // Empty occurrences never reach this partition; avoid creating an
            // invalid projection merely because the selected road has no walk.
            if (!(span.y > span.x)) span = new Vector2(values[row], values[row + 1]);
            var min = new Vector2(values[row + 2], values[row + 3]);
            var max = new Vector2(values[row + 4], values[row + 5]);
            var gradient = (max.x - min.x) / (span.y - span.x);
            if (domain % 2 == 0) gradient = -gradient;
            var origin = domain % 2 == 0 ? span.y : span.x;
            var u = projection * new Vector4(gradient, 0f, 0f, min.x - gradient * origin);
            // Each original side/leaf has one constant packed native tangent.
            // The minute UV fit skew at the centre is not its authored frame.
            var direction = Quaternion.Inverse(pose.rotation) * (Vector3.right * Mathf.Sign(gradient));
            var handedness = (domain < 2 ? 1f : -1f) * Mathf.Sign(gradient);
            for (var part = 0; part < 3; part++)
            {
                var index = first + domain * 3 + part;
                var v = projection * new Vector4(0f, 0f, values[row + 8 + part * 2], values[row + 9 + part * 2]);
                var tangent = new Vector4(direction.x, direction.y, direction.z, handedness);
                patches[index] = part == 1
                    ? Draw02PreparedAtlas.Repeat(1, u, v, tangent: tangent)
                    : new Patch(min, max, u, v, level.Native, sourceUv: true, tangent: tangent);
                support[index] = Type.Asphalt | Type.Sidewalk;
            }
        }
        Partition Cut(float x, float z, float offset, Partition negative, Partition positive)
        {
            var matrix = Matrix4x4.identity; matrix.m20 = x; matrix.m22 = z; matrix.m23 = offset;
            matrix.m00 = z; matrix.m02 = -x;
            return new Partition(matrix * transform, negative, positive);
        }
        Partition Domain(int domain)
        {
            var sign = domain < 2 ? 1f : -1f;
            var firstDomain = first + domain * 3;
            var row = (lod * 4 + domain) * 14;
            return Cut(0f, sign, -values[values.Length - 2], new Partition(firstDomain),
                Cut(0f, sign, -values[values.Length - 1], new Partition(firstDomain + 1),
                Cut(0f, sign, -values[row + 7], new Partition(firstDomain + 2), level.Sidewalk!)));
        }
        var positive = Cut(1f, 0f, 0f, Domain(0), Domain(1));
        var negative = Cut(1f, 0f, 0f, Domain(2), Domain(3));
        return new MovableRoadAppearance.Level(level.Native, patches, support, level.Carriageway,
            Cut(0f, 1f, 0f, negative, positive));
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02WalkAtlasAppearance_cs
internal static class Numeric_Types_Draw02_Draw02WalkAtlasAppearance_cs
{
    static Numeric_Types_Draw02_Draw02WalkAtlasAppearance_cs() { }
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02WalkAtlasAppearance_cs");
}
