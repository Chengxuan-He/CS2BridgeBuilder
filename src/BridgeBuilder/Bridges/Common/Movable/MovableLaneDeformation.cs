using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Closed-pose evaluation of native SG_CurvedShader vertex deformation. Equations follow
/// extracted 1.6.2f1 programs 570/4, 570/7 and 570/10; no shader or engine patch is installed.
/// Decal boxes are projection volumes and must never enter this mesh-deformation path.
/// </summary>
internal static class MovableLaneDeformation
{
    internal static MovableRoadGeometry.Vertex Evaluate(MovableLaneVisuals.Batch batch,
        MovableRoadGeometry.Vertex vertex, Func<Vector3, float>? projectedHeight)
    {
        if (batch.Decal) throw new InvalidOperationException("A decal volume requires surface projection, not mesh export.");
        if (batch.HangingSwaying)
            throw new NotSupportedException("A hanging/swaying lane requires its native dynamic attachment representation: " + batch.Source.name);

        var transform = batch.Transform;
        var scale = new Vector3(((Vector3)transform.GetColumn(0)).magnitude,
            ((Vector3)transform.GetColumn(1)).magnitude, ((Vector3)transform.GetColumn(2)).magnitude);
        var p = Vector3.Scale(vertex.Position, scale);
        var t = Mathf.Clamp01(p.z / batch.Parameters.x + .5f);
        var remainder = p.z - (t - .5f) * batch.Parameters.x;
        var curve = batch.CurveMatrix;
        Vector4 point;
        Vector3 direction;
        var uv = vertex.UV;

        if (batch.GeometryTiling && !batch.Dilated)
        {
            var period = batch.Dilation.x;
            var phase = Remainder(t / period);
            var weight = Mathf.Clamp01((.5f - Mathf.Abs(phase - .5f)) / batch.Dilation.y);
            weight = weight * weight * (3f - 2f * weight);
            var end = curve[3, 3];
            var first = Mathf.Clamp01((t - phase * period * weight) / end);
            var last = Mathf.Clamp01((t + (1f - phase) * period * weight) / end);
            var fullT = t / end;
            if (fullT >= 1.001f) { p.x = 0f; p.y = 0f; }
            fullT = Mathf.Clamp01(fullT);
            var correction = Bezier(curve, fullT).w / end - fullT;
            first -= correction; last -= correction;
            point = Vector4.LerpUnclamped(Bezier(curve, first), Bezier(curve, last), phase);
            direction = Derivative(curve, (first + last) * .5f);
            uv.y -= Mathf.Floor(uv.y);
            uv.w = fullT - correction;
        }
        else
        {
            point = Bezier(curve, t);
            direction = Derivative(curve, t);
            uv.w = t;
            if (uv.y >= 0f)
            {
                var phase = Remainder(point.w / Mathf.Max(t, .001f) * uv.y * .5f);
                uv.y = 1f - Mathf.Abs(phase * 2f - 1f);
            }
            else uv.y -= Mathf.Floor(uv.y);
            if (batch.Dilated)
            {
                var dilation = batch.Dilation.x * batch.Dilation.y;
                p.x *= dilation; p.y *= dilation;
                uv.z = 1f;
            }
        }

        // These zero-vector defaults are the native shader's authored degeneracy behavior.
        var forward = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
        var right = new Vector3(forward.z, 0f, -forward.x);
        right = right.sqrMagnitude > 0f ? right.normalized : Vector3.right;
        var up = Vector3.LerpUnclamped(Vector3.Cross(forward, right), Vector3.up, batch.Parameters.y);
        var position = (Vector3)point + forward * remainder + right * p.x + up * p.y;
        var normal = right * vertex.Normal.x + up * vertex.Normal.y + forward * vertex.Normal.z;
        var tangent = right * vertex.Tangent.x + up * vertex.Tangent.y + forward * vertex.Tangent.z;
        var basis = transform;
        basis.SetColumn(0, transform.GetColumn(0) / scale.x);
        basis.SetColumn(1, transform.GetColumn(1) / scale.y);
        basis.SetColumn(2, transform.GetColumn(2) / scale.z);
        var center = basis.MultiplyPoint3x4((Vector3)point);
        position = basis.MultiplyPoint3x4(position);
        if (batch.Parameters.y != 0f)
        {
            if (projectedHeight == null)
                throw new InvalidOperationException("A projected lane requires the evaluated selected-road receiver surface.");
            position.y += (projectedHeight(center) - center.y) * batch.Parameters.y;
        }
        normal = basis.MultiplyVector(normal).normalized;
        tangent = basis.MultiplyVector(tangent).normalized;
        return new MovableRoadGeometry.Vertex(position, normal,
            new Vector4(tangent.x, tangent.y, tangent.z, vertex.Tangent.w), uv, vertex.Color);
    }

    private static float Remainder(float value) => value - (float)Math.Truncate(value);

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
