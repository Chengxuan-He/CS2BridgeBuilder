using System;
using Unity.Mathematics;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// Immutable output of the TrussArchBridge03 base metaprogram.
///
/// The full-detail mesh and its first LOD each contain two logical, disconnected half-bases. Some
/// boundary vertices sit exactly on x=0, so their sign alone cannot say which half owns them. The
/// metaprogram resolved that ownership from the archetype's triangle indices. Runtime applies only
/// the recorded answer: each complete half, including its x=0 boundary, is translated rigidly.
/// </summary>
internal static class TrussArch03BaseGeometry
{
    internal enum Level
    {
        Near,
        Lod1,
    }

    private static readonly int[] NearLeftAxis =
    {
        237, 238, 247, 248, 257, 259, 260, 263, 264,
        267, 269, 271, 272, 275, 280, 283, 285, 287,
    };

    private static readonly int[] NearRightAxis =
    {
        96, 98, 112, 114, 120, 123, 124, 127, 146,
        148, 151, 152, 276, 279, 311, 312, 314, 316,
    };

    private static readonly int[] Lod1LeftAxis =
    {
        88, 90, 92, 135, 136, 137, 138, 139, 140,
        169, 171, 172, 173, 198, 199, 200, 203, 231,
    };

    private static readonly int[] Lod1RightAxis =
    {
        81, 83, 95, 120, 122, 124, 127, 129, 142,
        147, 148, 157, 174, 180, 182, 202, 208, 235,
    };

    /// <summary>
    /// Applies x -> x + sign(part) * delta to both logical half-bases. The recorded part sign is
    /// used only for vertices authored exactly on x=0; every other vertex has the same sign as its
    /// half. No bounds, topology, non-zero threshold or family heuristic is evaluated at runtime.
    /// </summary>
    internal static bool TryTranslate(
        Level level, float3[] source, float extra, out float3[] moved, out string reason)
    {
        moved = (float3[])source.Clone();
        reason = string.Empty;

        var expectedVertices = level == Level.Near ? 400 : 284;
        var leftAxis = level == Level.Near ? NearLeftAxis : Lod1LeftAxis;
        var rightAxis = level == Level.Near ? NearRightAxis : Lod1RightAxis;
        if (source.Length != expectedVertices)
        {
            reason = $"recorded {level} archetype has {expectedVertices} vertices, source has {source.Length}";
            return false;
        }

        var axisOwners = new sbyte[source.Length];
        if (!Record(axisOwners, source, leftAxis, -1)
            || !Record(axisOwners, source, rightAxis, 1))
        {
            reason = $"recorded {level} x=0 vertex membership does not match the archetype";
            return false;
        }

        var delta = extra * 0.5f;
        for (var index = 0; index < source.Length; index++)
        {
            var x = source[index].x;
            var owner = x < -TowerWidening.CentreEpsilon
                ? -1
                : x > TowerWidening.CentreEpsilon
                    ? 1
                    : axisOwners[index];
            if (owner == 0)
            {
                reason = $"recorded {level} metadata has no owner for x=0 vertex {index}";
                moved = (float3[])source.Clone();
                return false;
            }

            moved[index].x = x + (owner * delta);
        }

        return true;
    }

    private static bool Record(sbyte[] owners, float3[] source, int[] indices, sbyte owner)
    {
        foreach (var index in indices)
        {
            if (index < 0 || index >= source.Length
                || Math.Abs(source[index].x) > TowerWidening.CentreEpsilon
                || owners[index] != 0)
            {
                return false;
            }

            owners[index] = owner;
        }

        return true;
    }
}
