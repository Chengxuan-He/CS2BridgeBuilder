using BridgeBuilder.Runtime;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

namespace BridgeBuilder.Bridges;

// One complete four-metre source cycle, rephased offline so the finite short
// interval stays inside the same texture. Only the repeat role wraps V.
internal static class Draw02FixedCarPreparedAtlas
{
    static Draw02FixedCarPreparedAtlas() { }
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Draw02_Draw02FixedCarPreparedAtlas_cs");
    internal static Patch Create(Vector4 u, Vector4 v, bool repeatV, Vector4 tangent)
    {
        var f = Data.F32;
        var min = new Vector2(f[4], f[5]); var max = new Vector2(f[6], f[7]);
        var sx = (max.x - min.x) / (f[2] - f[0]);
        var sy = (max.y - min.y) / (f[3] - f[1]);
        u *= sx; u.w += min.x - f[0] * sx;
        v *= sy; v.w += min.y - f[1] * sy + f[8] * (max.y - min.y);
        return new Patch(min, max, u, v, BridgePreparedMaterials.RepeatUnit("Draw02",
            "9c8457b9077bb0b478f487fe21ddc3515dbcda3bb3c69e5d27f1cd4e7f99b9e3"),
            preparedUnit: true, repeatU: false, repeatV: repeatV, tangent: tangent)
            .WithFiniteSampling(Vector2.zero, Vector2.one);
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02FixedCarPreparedAtlas_cs
