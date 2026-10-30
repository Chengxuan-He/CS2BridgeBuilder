using BridgeBuilder.Runtime;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

namespace BridgeBuilder.Bridges;

// Three shared original-atlas V units; finite views retain their own U affine.
// Filtering validation is recorded separately; native file validity is not a quality pass.
internal static class Draw02PreparedAtlas
{
    static Draw02PreparedAtlas() { }
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Draw02_Draw02PreparedAtlas_cs");
    internal static Patch Repeat(int unit, Vector4 u, Vector4 v, Vector4? tangent = null, bool projectTangent = false)
    {
        var key = unit switch
        {
            0 => "e5b04c743e8060755c092c47231ff40578fe0e9d9f984cab7dd985fe76a8e2c3",
            1 => "dbcf4b520d9cfe7b5bdced376806be978d10a364de44116f867dbe5a9d0ba541",
            2 => "75dffef619cd682850620b2937e8d63cd406eaa564fffbfd8f241e2e1e048ea3",
            _ => throw new System.ArgumentOutOfRangeException(nameof(unit))
        };
        var f = Data.F32; var row = unit * 8;
        var min = new Vector2(f[row + 4], f[row + 5]);
        var max = new Vector2(f[row + 6], f[row + 7]);
        var sx = (max.x - min.x) / (f[row + 2] - f[row]);
        var sy = (max.y - min.y) / (f[row + 3] - f[row + 1]);
        u *= sx; u.w += min.x - f[row] * sx;
        v *= sy; v.w += min.y - f[row + 1] * sy;
        return new Patch(min, max, u, v, BridgePreparedMaterials.RepeatUnit("Draw02", key),
            preparedUnit: true, repeatU: false, repeatV: true, tangent: tangent, projectTangent: projectTangent)
            // The source-grid extractor populated both finite-U guards at every mip.
            // This declares real sampling coverage, not a different UV projection.
            .WithFiniteSampling(Vector2.zero, Vector2.one);
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02PreparedAtlas_cs
