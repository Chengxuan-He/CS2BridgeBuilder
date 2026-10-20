using System;
using Game.Rendering;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// CPU evaluation of the non-terrain vertex path in native NetCompositionMeshLit (581-106).
// Matrix selectors, synchronization and UV distance correction follow the inspected DXBC.
// This constructs selected-road geometry; it does not modify shaders or classify mechanism parts.
internal sealed class MovableCompositionDeformation
{
    private readonly float4x4[] _matrices;
    private readonly float4[] _sync;
    private readonly float3x4 _transform;
    private readonly float3x3 _directions, _normals;
    private readonly float3 _origin;
    private static readonly float4 DefaultSync = new(.2f, .4f, .6f, .8f);

    internal MovableCompositionDeformation(BatchDataHelpers.CompositionParameters parameters, float3 origin)
    {
        _matrices = new[] { parameters.m_CompositionMatrix0, parameters.m_CompositionMatrix1,
            parameters.m_CompositionMatrix2, parameters.m_CompositionMatrix3, parameters.m_CompositionMatrix4,
            parameters.m_CompositionMatrix5, parameters.m_CompositionMatrix6, parameters.m_CompositionMatrix7 };
        _sync = new[] { parameters.m_CompositionSync0, parameters.m_CompositionSync1,
            parameters.m_CompositionSync2, parameters.m_CompositionSync3 };
        _transform = parameters.m_TransformMatrix;
        _directions = new float3x3(_transform.c0, _transform.c1, _transform.c2);
        _normals = math.transpose(math.inverse(_directions));
        _origin = origin;
    }

    internal MovableRoadGeometry.Vertex Evaluate(Vector3 position, Vector3 normal, Vector4 tangent,
        Vector4 texture, Color32 selector)
    {
        if (selector.r > 7 || selector.g > 7 || selector.b != 0)
            throw new InvalidOperationException("Composition vertex requires an unknown matrix or terrain-sampling path.");
        var left = _matrices[selector.r];
        var right = _matrices[selector.g];
        var p = (float3)position;
        if (!math.all(math.isfinite(p)) || !math.all(math.isfinite((float4)texture)))
            throw new InvalidOperationException("Composition vertex contains non-finite packed attributes.");

        // Alpha's low three bits select consecutive lateral synchronization groups. Its high
        // bit forces the endpoint-width blend. Intermediate bits do not affect this shader path.
        var group = selector.a & 7;
        var sourceSync = group < 2 ? DefaultSync : _sync[math.min(group - 2, 3)];
        var endSync = group < 1 ? DefaultSync : _sync[math.min(group - 1, 3)];
        var startWidth = math.length(left.c0.xz - right.c0.xz);
        var endWidth = math.length(left.c3.xz - right.c3.xz);
        var z = math.saturate(p.z * .5f);
        var blend = math.max(2f * z * z * (3f - 2f * z), selector.a >= 128 ? 1f : 0f);
        var destinationSync = math.lerp(startWidth * sourceSync, endWidth * endSync, blend) /
            math.max(math.lerp(startWidth, endWidth, blend), 1E-06f);
        var lateral = Synchronize(p.x, sourceSync, destinationSync);

        // Mapping distance follows UV.z, including native skew correction. Geometry follows the
        // synchronized position.x; these are distinct coordinates and must not be conflated.
        // A node curve can have coincident endpoint controls. Such an endpoint contributes
        // no skew correction; normalizing its zero tangent would poison even a zero UV rate.
        var startDirection = math.normalizesafe(math.lerp(left.c1.xz - left.c0.xz,
            right.c1.xz - right.c0.xz, texture.z));
        var endDirection = math.normalizesafe(math.lerp(left.c2.xz - left.c3.xz,
            right.c2.xz - right.c3.xz, texture.z));
        var skew = (lateral - texture.z) * math.saturate(math.dot(startDirection, -endDirection));
        var startCorrection = math.dot(right.c0.xz - left.c0.xz, startDirection) * skew;
        var endCorrection = math.dot(right.c3.xz - left.c3.xz, -endDirection) * skew;
        var mapping = math.lerp(new float4(left.c0.w, left.c1.w, left.c2.w, left.c3.w),
            new float4(right.c0.w, right.c1.w, right.c2.w, right.c3.w), texture.z);
        mapping += math.lerp(new float4(startCorrection), new float4(endCorrection),
            new float4(0f, 1f / 3f, 2f / 3f, 1f));

        var t = p.z;
        var u = 1f - t;
        var weights = new float4(u * u * u, 3f * t * u * u, 3f * t * t * u, t * t * t);
        var derivative = 3f * new float4(-u * u, (1f - 3f * t) * u,
            (2f - 3f * t) * t, t * t);
        var leftPoint = math.mul(left, weights).xyz;
        var rightPoint = math.mul(right, weights).xyz;
        var point = math.lerp(leftPoint, rightPoint, lateral);
        point.y += p.y;
        var leftTangent = math.mul(left, derivative).xyz;
        var rightTangent = math.mul(right, derivative).xyz;
        var fallback = math.normalizesafe(leftTangent + rightTangent, new float3(0f, 0f, 1f));
        var forward = math.normalizesafe(math.lerp(leftTangent, rightTangent, lateral), fallback);
        var across = math.normalizesafe(new float3(forward.z, 0f, -forward.x), new float3(1f, 0f, 0f));
        var up = math.cross(forward, across);
        var n = across * normal.x + up * normal.y + forward * normal.z;
        var direction = across * tangent.x + up * tangent.y + forward * tangent.z;
        n = math.normalize(math.mul(_normals, n));
        direction = math.normalize(math.mul(_directions, direction));
        texture.y = texture.y >= 0f ? math.dot(mapping, weights) * texture.y : math.frac(texture.y);
        texture.z = 0f;
        return new MovableRoadGeometry.Vertex(math.mul(_transform, new float4(point, 1f)) - _origin,
            n, new Vector4(direction.x, direction.y, direction.z, tangent.w), texture, Color.white);
    }

    private static float Synchronize(float x, float4 source, float4 target)
    {
        // Native epsilon ordering prevents overlapping authored synchronization breakpoints.
        var k2 = math.min(source.z, source.y);
        var k3 = math.max(k2 + 1E-06f, source.z);
        var k1 = math.min(k2 - 1E-06f, source.x);
        var k4 = math.max(k3 + 1E-06f, source.w);
        var k0 = math.min(k1 - 1E-06f, 0f);
        var k5 = math.max(k4 + 1E-06f, 1f);
        var a = math.saturate((new float3(x) - new float3(k0, k1, k2)) /
            (new float3(k1, k2, k3) - new float3(k0, k1, k2)));
        var b = math.saturate((new float2(x) - new float2(k3, k4)) /
            (new float2(k4, k5) - new float2(k3, k4)));
        return math.dot(a, new float3(target.x, target.y - target.x, target.z - target.y)) +
            math.dot(b, new float2(target.w - target.z, 1f - target.w));
    }
}
