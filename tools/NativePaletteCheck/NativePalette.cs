using System.Numerics;

namespace BridgeBuilder.Bridges;

// Native Default shader's fixed instance-color expression, in linear color space.
// Alpha controls smoothness and coating; it is not surface transparency.
internal static class NativePalette
{
    internal static Vector4 Factor(Vector3 control, Vector4 first, Vector4 second, Vector4 third)
    {
        float rg = control.X + control.Y, rgb = rg + control.Z;
        var mixed = first + control.Y * rg * (second - first);
        mixed += control.Z * rgb * (third - mixed);
        return Vector4.One + System.Math.Min(rgb, 1f) * (mixed - Vector4.One);
    }

    internal static bool RemovesCoating(Vector3 control, Vector4 first, Vector4 second, Vector4 third)
    {
        var contribution = control * new Vector3(1f - first.W, 1f - second.W, 1f - third.W);
        return Vector3.Dot(contribution, contribution) != 0f;
    }
}
