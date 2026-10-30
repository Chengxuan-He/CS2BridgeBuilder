using UnityEngine;
using BridgeBuilder.Runtime;

namespace BridgeBuilder.Bridges;
// Independent finite endpoint composition. The source projection is unchanged;
// only the measured embedded-paint region uses the defined source donor.
internal static class Draw01FiniteAsphalt
{
    internal static MovableExistingRoadSurface.Patch Create(Vector4 u, Vector4 v, Matrix4x4 frame, Vector2 sourceMin, Vector2 sourceMax, Vector4? tangent = null)
    {
        var nativeMin = new Vector2(Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[0], Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[1]);
        var nativeMax = new Vector2(Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[2], Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[3]);
        var contentMin = new Vector2(Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[4], Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[5]);
        var contentMax = new Vector2(Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[6], Numeric_Types_Draw01_Draw01FiniteAsphalt_cs.Values.F32[7]);
        var scale = new Vector2((contentMax.x - contentMin.x) / (nativeMax.x - nativeMin.x), (contentMax.y - contentMin.y) / (nativeMax.y - nativeMin.y));
        u *= scale.x;
        u.w += contentMin.x - nativeMin.x * scale.x;
        v *= scale.y;
        v.w += contentMin.y - nativeMin.y * scale.y;
        var min = contentMin + Vector2.Scale(sourceMin - nativeMin, scale);
        var max = contentMin + Vector2.Scale(sourceMax - nativeMin, scale);
        return new MovableExistingRoadSurface.Patch(min, max, frame.transpose * u, frame.transpose * v, BridgePreparedMaterials.RepeatUnit("Draw01", "76122db93bfeb1e5e1717bd555d11f2aa2f8e534c44f7724ddea9738b763f614"), preparedUnit: true, tangent: tangent, projectTangent: !tangent.HasValue).WithFiniteSampling(Vector2.zero, Vector2.one);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01FiniteAsphalt_cs
internal static class Numeric_Types_Draw01_Draw01FiniteAsphalt_cs
{
    static Numeric_Types_Draw01_Draw01FiniteAsphalt_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01FiniteAsphalt_cs");
}
