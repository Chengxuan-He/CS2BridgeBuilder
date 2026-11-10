using System;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Projection changes sampling coordinates, never receiver topology or attributes.
internal static class MovableRoadProjection
{
    internal static bool TryTangent(Vector3 normal, Vector4 u, Vector4 v,out Vector4 result,out string? error, Vector4? original = null)
    {
        result=default;error=null;
        var direction = Vector3.Cross((Vector3)v, normal);
        float determinant = Vector3.Dot((Vector3)u, direction);
        if (determinant == 0f && original.HasValue){result=original.Value;return true;}
        if (!float.IsFinite(determinant) || determinant == 0f)
            {error="Evidenced projected tangent has a singular receiver frame.";return false;}
        direction = (direction / determinant).normalized;
        result=new Vector4(direction.x, direction.y, direction.z, determinant > 0 ? 1 : -1);return true;
    }

    internal static bool TryPrepared(Vertex vertex, MovableExistingRoadSurface.Patch patch,out Vertex result,out string? error)
    {
        result=default;error=patch.Error;if(error!=null)return false;
        if (!patch.PreparedUnit){error="Completed offline material mapping required.";return false;}
        var p = new Vector4(vertex.Position.x, vertex.Position.y, vertex.Position.z, 1);
        var uv = vertex.UV;
        uv.x = Vector4.Dot(p, patch.U); uv.y = Vector4.Dot(p, patch.V);
        if (!float.IsFinite(uv.x) || !float.IsFinite(uv.y))
            {error="Offline unit projection produced non-finite coordinates.";return false;}
        var tangent = patch.Tangent ?? vertex.Tangent;
        if (patch.ProjectTangent && !TryTangent(vertex.Normal,patch.U,patch.V,out tangent,out error,vertex.Tangent))return false;
        if (!float.IsFinite(tangent.x) || !float.IsFinite(tangent.y) ||
            !float.IsFinite(tangent.z) || !float.IsFinite(tangent.w))
            {error="Offline unit has a non-finite tangent frame.";return false;}
        result=new Vertex(vertex.Position, vertex.Normal, tangent, uv, vertex.Color);return true;
    }
}
