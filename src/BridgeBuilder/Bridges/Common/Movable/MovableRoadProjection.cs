using System;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Projection changes sampling coordinates, never receiver topology or attributes.
internal static class MovableRoadProjection
{
    internal static Vector4 Tangent(Vector3 normal, Vector4 u, Vector4 v, Vector4? original = null)
    {
        var direction = Vector3.Cross((Vector3)v, normal);
        float determinant = Vector3.Dot((Vector3)u, direction);
        if (determinant == 0f && original.HasValue) return original.Value;
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
        if (!float.IsFinite(uv.x) || !float.IsFinite(uv.y))
            throw new InvalidOperationException("Offline unit projection produced non-finite coordinates.");
        var tangent = patch.Tangent ?? vertex.Tangent;
        if (patch.ProjectTangent) tangent = Tangent(vertex.Normal, patch.U, patch.V, vertex.Tangent);
        if (!float.IsFinite(tangent.x) || !float.IsFinite(tangent.y) ||
            !float.IsFinite(tangent.z) || !float.IsFinite(tangent.w))
            throw new InvalidOperationException("Offline unit has a non-finite tangent frame.");
        return new Vertex(vertex.Position, vertex.Normal, tangent, uv, vertex.Color);
    }
}
