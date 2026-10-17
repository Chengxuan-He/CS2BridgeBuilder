using System;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Native curved-decal receiver coordinates, evaluated on the selected road rather than exporting
/// a decal volume as road geometry. Follows CurvedDecalShader's six-step closest-curve search and
/// endpoint blending. The appearance baker samples these coordinates before road bone binding.
/// </summary>
internal static class MovableDecalProjection
{
    internal readonly struct Sample
    {
        internal readonly Vector2 UV;
        internal readonly float EndFade, HeightFade;
        internal readonly Vector3 Forward;
        internal Sample(Vector2 uv, float endFade, float heightFade, Vector3 forward)
        { UV = uv; EndFade = endFade; HeightFade = heightFade; Forward = forward; }

        // Native 572/24 applies the end fade as an exponent independently to albedo,
        // normal and mask opacity. Multiplying the two spatial fades loses that behavior.
        // Camera-distance fade is intentionally excluded from persistent appearance.
        internal float ChannelOpacity(float materialOpacity)
        {
            var transmission = Mathf.Clamp(1f - materialOpacity * HeightFade,
                (1f - EndFade) * .001f, 1f);
            return 1f - Mathf.Pow(transmission, EndFade);
        }
    }

    internal static bool TryProject(MovableLaneVisuals.Batch batch, Vector3 receiver, out Sample sample)
    {
        sample = default;
        if (!batch.Decal) throw new InvalidOperationException("Only decal batches may enter decal projection.");
        // BuildTransformMatrix scales the authored mesh, not a unit cube. Native
        // CurvedDecalShader DBufferMesh (572/60) clips with the curve-relative side,
        // height and endpoint tests below; a local +/-0.5 test truncates real decals.
        var p = receiver - (Vector3)batch.Transform.GetColumn(3);
        var matrix = batch.CurveMatrix;
        var a = matrix.GetColumn(0); var b = matrix.GetColumn(1);
        var c = matrix.GetColumn(2); var d = matrix.GetColumn(3);
        var left = XZ(a); var right = XZ(d);
        Vector4 center;
        Vector3 forward;
        Vector2 uv;
        // Native 572/24 compares endpoint X/Z components exactly and uses a local
        // projection when they coincide; this is an authored shader branch, not a fallback.
        if (a.x == d.x && a.z == d.z)
        {
            center = a;
            forward = Vector3.forward;
            var delta = p - (Vector3)a;
            var longitudinal = delta.z / batch.Size.z;
            var sign = longitudinal > 0f ? 1f : longitudinal < 0f ? -1f : 0f;
            uv = new Vector2(delta.x / batch.Size.x + .5f, longitudinal + .5f + sign * .5f);
        }
        else
        {
            var lo = 0f; var hi = 1f; var t = .5f;
            for (var iteration = 0; iteration < 6; iteration++)
            {
                var chord = right - left;
                var factor = Mathf.Clamp(Vector2.Dot(XZ(p) - left, chord) / chord.sqrMagnitude, .1f, .9f);
                t = Mathf.LerpUnclamped(lo, hi, factor);
                var at = Bezier(matrix, t);
                var derivative = Derivative(matrix, t);
                if (Vector2.Dot(XZ(derivative), XZ(p) - XZ(at)) <= 0f) { hi = t; right = XZ(at); }
                else { lo = t; left = XZ(at); }
            }
            center = Bezier(matrix, t);
            forward = Derivative(matrix, t).normalized;
            var middleUV = Coordinates(p, center, XZ(forward).normalized, batch.Size);
            var endUV = Coordinates(p, d, (XZ(d) - XZ(c)).normalized, batch.Size);
            var startUV = Coordinates(p, a, (XZ(b) - XZ(a)).normalized, batch.Size);
            var endWeight = Mathf.Min(Smooth(b.w, c.w, middleUV.y),
                Smooth(Mathf.LerpUnclamped(c.w, d.w, .7f), d.w, endUV.y));
            var startWeight = Mathf.Min(Smooth(c.w, b.w, middleUV.y),
                Smooth(Mathf.LerpUnclamped(b.w, a.w, .7f), a.w, startUV.y));
            uv = Vector2.LerpUnclamped(Vector2.LerpUnclamped(middleUV, endUV, endWeight), startUV, startWeight);
        }
        var progress = uv.y / d.w;
        var widthScale = Mathf.Lerp(batch.Scale.x, batch.Scale.z, Mathf.Clamp01(progress));
        var side = (uv.x - .5f) / widthScale;
        var vertical = (p.y - center.y - batch.Size.w) / batch.Size.y;
        var endMargin = (.5f - Mathf.Abs(progress - .5f)) * batch.Size.z * d.w;
        if (.5f - Mathf.Abs(side) < 0f || .5f - Mathf.Abs(vertical) < 0f || endMargin + batch.SmoothingDistance < 0f)
            return false;
        var endFade = batch.SmoothingDistance == 0f ? 1f
            : Smooth01((endMargin + batch.SmoothingDistance) / (2f * batch.SmoothingDistance));
        var heightFade = Smooth01((.5f - Mathf.Abs(vertical)) * 4f);
        var properties = batch.Source.GetComponent<DecalProperties>();
        if (properties == null) throw new InvalidOperationException("A decal batch has no DecalProperties.");
        var area = properties.m_TextureArea;
        uv = new Vector2(Mathf.LerpUnclamped(area.min.x, area.max.x, side + .5f),
            Mathf.LerpUnclamped(area.min.y, area.max.y, uv.y));
        sample = new Sample(uv, endFade, heightFade, forward);
        return true;
    }

    private static Vector2 Coordinates(Vector3 p, Vector4 center, Vector2 forward, Vector4 size)
    {
        var delta = XZ(p) - XZ(center);
        return new Vector2(Vector2.Dot(new Vector2(forward.y, -forward.x), delta) / size.x + .5f,
            Vector2.Dot(forward, delta) / size.z + center.w);
    }

    private static Vector2 XZ(Vector3 v) => new(v.x, v.z);
    private static Vector2 XZ(Vector4 v) => new(v.x, v.z);
    private static float Smooth(float a, float b, float value) => Smooth01((value - a) / (b - a));
    private static float Smooth01(float value)
    { var t = Mathf.Clamp01(value); return t * t * (3f - 2f * t); }
    private static Vector4 Bezier(Matrix4x4 matrix, float t)
    {
        var s = 1f - t;
        return matrix.GetColumn(0) * (s * s * s) + matrix.GetColumn(1) * (3f * s * s * t)
            + matrix.GetColumn(2) * (3f * s * t * t) + matrix.GetColumn(3) * (t * t * t);
    }
    private static Vector3 Derivative(Matrix4x4 matrix, float t)
    {
        var s = 1f - t;
        return (Vector3)((matrix.GetColumn(1) - matrix.GetColumn(0)) * (3f * s * s)
            + (matrix.GetColumn(2) - matrix.GetColumn(1)) * (6f * s * t)
            + (matrix.GetColumn(3) - matrix.GetColumn(2)) * (3f * t * t));
    }
}
