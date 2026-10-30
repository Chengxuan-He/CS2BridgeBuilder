using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;

// Measured grating/solid silhouette domains, independently for LOD0 and LOD1.
// Repeating interiors use one offline source-atlas unit with continuous V.
// Only actual appearance boundaries and finite terminals partition receivers.
internal static class Draw02MovingAtlasAppearance
{
    internal static MovableRoadAppearance.Level Create(int lod, SurfaceAsset native,
        float halfDelta, Pose pose, Patch[] otherPatches, Type[] otherSupport,
        Partition otherCarriageway, Partition sidewalk)
    {
        if (lod < 0 || lod > 1 || native.id.guid.ToString() != "8a28ab8d9526e258efbcb81c3bbc936b")
            throw new InvalidOperationException("Draw02 moving atlas requires its measured near or middle Surface.");
        var data = Numeric_Types_Draw02_Draw02MovingAtlasAppearance_cs.Values;
        var ints = data.I32; var values = data.F32; var header = lod * 9;
        var count = ints[header]; var slabs = ints[header + 1];
        var di = ints[header + 3]; var si = ints[header + 4]; var ii = ints[header + 5];
        var df = ints[header + 6]; var sf = ints[header + 7]; var inf = ints[header + 8];
        var constants = values.Length - 4;
        var width = values[constants];
        if (!float.IsFinite(halfDelta) || width + halfDelta <= 0f)
            throw new InvalidOperationException("Draw02 moving atlas width is invalid.");
        var xScale = width / (width + halfDelta);
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        var first = otherPatches.Length;
        var outside = first + count * 3;
        var patches = new Patch[outside + 1]; var support = new Type[outside + 1];
        Array.Copy(otherPatches, patches, first); Array.Copy(otherSupport, support, first);
        // Outside the measured road-top silhouette is not prototype asphalt.
        // Preserve the selected input there; do not remove its receiver geometry.
        patches[outside] = new Patch(Vector2.zero, Vector2.one);
        support[outside] = Type.None;
        for (var domain = 0; domain < count; domain++)
        {
            var offset = df + domain * 14;
            var min = new Vector2(values[offset + 2], values[offset + 4]);
            var max = new Vector2(values[offset + 3], values[offset + 5]);
            var u = projection * new Vector4(values[offset] * xScale, 0f, 0f, values[offset + 1]);
            for (var part = 0; part < 3; part++)
            {
                var index = first + domain * 3 + part;
                var v = projection * new Vector4(0f, 0f, values[offset + 8 + part * 2], values[offset + 9 + part * 2]);
                patches[index] = part == 1
                    ? Draw02PreparedAtlas.Repeat(0, u, v, projectTangent: true)
                    : new Patch(min, max, u, v, surface: native, sourceUv: true, projectTangent: true);
                support[index] = Type.Asphalt;
            }
        }
        Partition Plane(float x, float z, float offset, Partition negative, Partition positive)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = z; matrix.m02 = -x;
            matrix.m20 = x; matrix.m22 = z; matrix.m23 = offset;
            return new Partition(matrix * transform, negative, positive);
        }
        Partition Domain(int domain)
        {
            var sign = ints[di + domain] == 1 ? 1f : -1f;
            var start = first + domain * 3;
            return Plane(0f, sign, -values[constants + 1], new Partition(start),
                Plane(0f, sign, -values[constants + 2], new Partition(start + 1), new Partition(start + 2)));
        }
        Partition Half(int bone)
        {
            var sign = bone == 1 ? 1f : -1f;
            Partition result = otherCarriageway;
            for (var slab = slabs - 1; slab >= 0; slab--)
            {
                var row = si + slab * 3;
                if (ints[row] != bone) continue;
                Partition strip = new Partition(outside);
                for (var n = ints[row + 2] - 1; n >= 0; n--)
                {
                    var interval = ints[row + 1] + n; var f = inf + interval * 4;
                    var right = Plane(xScale, -values[f + 2] * sign, -values[f + 3], Domain(ints[ii + interval]), strip);
                    strip = Plane(xScale, -values[f] * sign, -values[f + 1], new Partition(outside), right);
                }
                result = Plane(0f, sign, -values[sf + slab * 2 + 1], strip, result);
            }
            return result;
        }
        return new MovableRoadAppearance.Level(native, patches, support,
            Plane(0f, 1f, 0f, Half(2), Half(1)), sidewalk);
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02MovingAtlasAppearance_cs
internal static class Numeric_Types_Draw02_Draw02MovingAtlasAppearance_cs
{
    static Numeric_Types_Draw02_Draw02MovingAtlasAppearance_cs() { }
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02MovingAtlasAppearance_cs");
}
