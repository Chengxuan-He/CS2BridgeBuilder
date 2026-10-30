using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;

// Finite cap/ramp charts retain the native Surface. Short and repeating asphalt
// share one finished offline paint-composed cycle with distinct V sampling modes.
internal static class Draw02FixedCarAtlasAppearance
{
    internal static MovableRoadAppearance.Level Apply(int lod, MovableRoadAppearance.Level level,
        float halfDelta, Pose pose)
    {
        if (lod < 0 || lod > 1) throw new ArgumentOutOfRangeException(nameof(lod));
        var values = Numeric_Types_Draw02_Draw02FixedCarAtlasAppearance_cs.Values.F32;
        var width = values[values.Length - 3];
        if (!float.IsFinite(halfDelta) || width + halfDelta <= 0f)
            throw new InvalidOperationException("Draw02 fixed carriageway width is invalid.");
        var scale = width / (width + halfDelta);
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        var first = level.Patches.Length;
        var outside = first + 32;
        var patches = new Patch[outside + 1]; var support = new Type[outside + 1];
        Array.Copy(level.Patches, patches, first); Array.Copy(level.SupportedTypes, support, first);
        patches[outside] = new Patch(Vector2.zero, Vector2.one);
        for (var domain = 0; domain < 32; domain++)
        {
            var row = (lod * 32 + domain) * 17;
            var part = domain % 16 / 4;
            var direction = Quaternion.Inverse(pose.rotation) * new Vector3(values[row + 13], values[row + 14], values[row + 15]);
            var tangent = new Vector4(direction.x, direction.y, direction.z, values[row + 16]);
            var u = projection * new Vector4(values[row + 5] * scale, 0f, 0f, values[row + 6]);
            var v = projection * new Vector4(0f, 0f, values[row + 7], values[row + 8]);
            patches[first + domain] = part >= 2
                ? Draw02FixedCarPreparedAtlas.Create(u, v, part == 3, tangent)
                : new Patch(new Vector2(values[row + 9], values[row + 10]),
                    new Vector2(values[row + 11], values[row + 12]), u, v,
                    level.Native, sourceUv: true, tangent: tangent);
            support[first + domain] = Type.Asphalt;
        }
        Partition Cut(float x, float z, float offset, Partition negative, Partition positive)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = z; matrix.m02 = -x;
            matrix.m20 = x; matrix.m22 = z; matrix.m23 = offset;
            return new Partition(matrix * transform, negative, positive);
        }
        var unsupported = new Partition(outside);
        Partition Strip(int firstDomain)
        {
            Partition result = unsupported;
            for (var lane = 3; lane >= 0; lane--)
            {
                var row = (lod * 32 + firstDomain + lane) * 17;
                result = Cut(scale, 0f, -values[row + 1], new Partition(first + firstDomain + lane), result);
            }
            var start = (lod * 32 + firstDomain) * 17;
            return Cut(scale, 0f, -values[start], unsupported, result);
        }
        Partition Half(int half)
        {
            var end = half == 0 ? 1f : -1f;
            var firstDomain = half * 16;
            var cap = Strip(firstDomain);
            var ramp = Strip(firstDomain + 4);
            var repeat = Strip(firstDomain + 12);
            var capRow = (lod * 32 + firstDomain) * 17;
            var rampRow = (lod * 32 + firstDomain + 4) * 17;
            var shortRow = (lod * 32 + firstDomain + 8) * 17;
            var repeatRow = (lod * 32 + firstDomain + 12) * 17;
            var limitedRamp = Cut(0f, end, -values[rampRow + 4], ramp, unsupported);
            // The actual short-road lower silhouette occludes the ramp. Avoid
            // extending its lane boundaries into the earlier all-ramp region.
            Partition shortRegion = unsupported;
            var earliestShort = float.PositiveInfinity;
            for (var lane = 3; lane >= 0; lane--)
            {
                var row = shortRow + lane * 17;
                earliestShort = Mathf.Min(earliestShort, Mathf.Min(
                    values[row + 2] * values[row] + values[row + 3],
                    values[row + 2] * values[row + 1] + values[row + 3]));
                var visible = Cut(-values[row + 2] * scale, end, -values[row + 3],
                    limitedRamp, new Partition(first + firstDomain + 8 + lane));
                shortRegion = Cut(scale, 0f, -values[row + 1], visible, shortRegion);
            }
            shortRegion = Cut(scale, 0f, -values[shortRow], unsupported, shortRegion);
            shortRegion = Cut(0f, end, -earliestShort, limitedRamp, shortRegion);
            var fixedRegion = Cut(0f, end, -values[capRow + 3], unsupported,
                Cut(0f, end, -values[capRow + 4], cap,
                    Cut(0f, end, -values[shortRow + 4], shortRegion,
                        Cut(0f, end, -values[repeatRow + 4], repeat, unsupported))));
            // The measured gap between the moving silhouette and fixed cap has
            // no prototype asphalt. Keep the selected material there rather than
            // allowing the legacy moving recipe to bake a fabricated appearance.
            return Cut(0f, end, -values[values.Length - 2 + lod], level.Carriageway!, fixedRegion);
        }
        return new MovableRoadAppearance.Level(level.Native, patches, support,
            Cut(0f, 1f, 0f, Half(1), Half(0)), level.Sidewalk);
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02FixedCarAtlasAppearance_cs
internal static class Numeric_Types_Draw02_Draw02FixedCarAtlasAppearance_cs
{
    static Numeric_Types_Draw02_Draw02FixedCarAtlasAppearance_cs() { }
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02FixedCarAtlasAppearance_cs");
}
