using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Pure coordinate projection used by geometry regression fixtures. It does not
// authorize material replacement or adaptation of a network descriptor.
internal static class MovableCompatibleRoad
{
    internal static Vertex Project(Vertex vertex, Matrix4x4 frame, float scale)
    {
        var u=frame.GetRow(0)*scale; var v=frame.GetRow(2)*scale;
        var p=new Vector4(vertex.Position.x,vertex.Position.y,vertex.Position.z,1);
        var uv=vertex.UV; uv.x=Vector4.Dot(p,u); uv.y=Vector4.Dot(p,v);
        if(!float.IsFinite(uv.x)||!float.IsFinite(uv.y))throw new InvalidOperationException("Corresponding road projection is non-finite.");
        var tangent=vertex.Tangent;
        float determinant=Vector3.Dot((Vector3)u,Vector3.Cross((Vector3)v,vertex.Normal));
        // A vertical receiver can have singular XZ projection. Preserve its valid
        // authored frame; do not remove the face or normalize its geometric cross.
        if(determinant!=0f && !MovableRoadProjection.TryTangent(vertex.Normal,u,v,out tangent,out var cause))throw new InvalidOperationException(cause);
        if(!float.IsFinite(tangent.x)||!float.IsFinite(tangent.y)||!float.IsFinite(tangent.z)||
            !float.IsFinite(tangent.w))
            throw new InvalidOperationException("Corresponding road tangent is invalid.");
        return new Vertex(vertex.Position,vertex.Normal,tangent,uv,vertex.Color);
    }
}
