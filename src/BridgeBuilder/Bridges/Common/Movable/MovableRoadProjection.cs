using System;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Projection changes sampling coordinates, never receiver topology or attributes.
internal static class MovableRoadProjection
{
    internal static Vector4 Tangent(Vector3 normal, Vector4 u, Vector4 v)
    {
        var direction = Vector3.Cross((Vector3)v, normal);
        float determinant = Vector3.Dot((Vector3)u, direction);
        if (!float.IsFinite(determinant) || determinant == 0f)
            throw new InvalidOperationException("Evidenced projected tangent has a singular receiver frame.");
        direction = (direction / determinant).normalized;
        return new Vector4(direction.x, direction.y, direction.z, determinant > 0 ? 1 : -1);
    }

    internal static Vertex Prepared(Vertex vertex, MovableExistingRoadSurface.Patch patch)
    {
        if (!patch.PreparedUnit) throw new ArgumentException("Completed offline material mapping required.");
        var p = new Vector4(vertex.Position.x, vertex.Position.y, vertex.Position.z, 1);
        var uv = vertex.UV;
        uv.x = Vector4.Dot(p, patch.U); uv.y = Vector4.Dot(p, patch.V);
        if (!float.IsFinite(uv.x) || !float.IsFinite(uv.y) ||
            (!patch.RepeatU && !(uv.x > 0f && uv.x < 1f && uv.x >= patch.FiniteSamplingMin.x && uv.x <= patch.FiniteSamplingMax.x)) ||
            (!patch.RepeatV && !(uv.y > 0f && uv.y < 1f && uv.y >= patch.FiniteSamplingMin.y && uv.y <= patch.FiniteSamplingMax.y)))
            throw new InvalidOperationException("Offline unit finite axis escaped its guarded native sampling range.");
        var tangent = patch.Tangent ?? vertex.Tangent;
        if (patch.ProjectTangent) tangent = Tangent(vertex.Normal, patch.U, patch.V);
        if (!float.IsFinite(tangent.x) || !float.IsFinite(tangent.y) ||
            !float.IsFinite(tangent.z) || !float.IsFinite(tangent.w))
            throw new InvalidOperationException("Offline unit has a non-finite tangent frame.");
        return new Vertex(vertex.Position, vertex.Normal, tangent, uv, vertex.Color);
    }
}
