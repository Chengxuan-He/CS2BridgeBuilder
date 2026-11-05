using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Corresponding object road appearance, not an equivalent network shader conversion.
// Exact known source identities and the complete receiver footprint gate this route.
internal static class MovableCompatibleRoad
{
    internal sealed class Binding
    {
        internal readonly string Source, Color, Normal, Mask;
        internal readonly bool Builtin;
        internal readonly MovableRoadAppearance.Type Kind;
        internal readonly SurfaceAsset Surface;
        internal readonly float Scale;
        internal Binding(string source, bool builtin, string color, string normal, string mask,
            MovableRoadAppearance.Type kind, SurfaceAsset surface, float scale)
        { Source=source; Builtin=builtin; Color=color; Normal=normal; Mask=mask; Kind=kind; Surface=surface; Scale=scale; }
        internal bool Accepts(Triangle triangle, MovableRoadAppearance.Type supported)
        {
            if ((supported & Kind) != 0) return false;
            float a=triangle.A.UV.x,b=triangle.B.UV.x,c=triangle.C.UV.x;
            if(!float.IsFinite(a)||!float.IsFinite(b)||!float.IsFinite(c))return false;
            return MovableRoadLayerIdentity.WholeReceiver(Source,Builtin,Color,Normal,Mask,Kind,
                Math.Min(a,Math.Min(b,c)),Math.Max(a,Math.Max(b,c)))==Kind && Kind!=MovableRoadAppearance.Type.None;
        }
    }

    internal static Binding? Read(SurfaceAsset source, MovableRoadMaterial owner)
    {
        if(!source.isBuiltin || source.materialTemplateHash!=17)return null;
        source.LoadProperties(true);
        try
        {
            string Id(string name)=>source.TryGetTexture(name,out var texture)&&texture!=null
                ?texture.id.guid.ToString():string.Empty;
            var kind=MovableRoadLayerIdentity.Worldspace(source.id.guid.ToString(),true,
                Id("_WorldspaceAlbedo"),Id("_WorldspaceNormalMap"),Id("_WorldspaceMaskMap"),string.Empty);
            if(kind==MovableRoadAppearance.Type.None)return null;
            // Both targets retain the actual source family's world PBR; the known
            // Ballast descriptor is prepared offline, never an Asphalt fallback.
            var target=kind==MovableRoadAppearance.Type.Ballast
                ? BridgeBuilder.Runtime.BridgePreparedMaterials.NativeSource(owner.StyleId,
                    "8b9121008e8de326e7abc88b69df080e")
                : owner.Asset;
            target.LoadProperties(true);
            try
            {
                if(!MovableSurfaceAssets.IsObjectSurface(target))return null;
                foreach(var pair in new[]{("_WorldspaceAlbedo","_BaseColorMap"),
                    ("_WorldspaceNormalMap","_NormalMap"),("_WorldspaceMaskMap","_MaskMap")})
                    if(!target.TryGetTexture(pair.Item2,out var texture)||texture==null||
                        Id(pair.Item1)!=texture.id.guid.ToString())return null;
                if(!source.TryGetFloatProperty("_WorldspaceUVScale",out var scale))
                    scale=AssetDatabase.global.resources.materialLibrary.GetMaterial(17).GetFloat("_WorldspaceUVScale");
                if(!float.IsFinite(scale))throw new InvalidOperationException("Corresponding road material has non-finite source density.");
                return new Binding(source.id.guid.ToString(),true,Id("_BaseColorMap"),Id("_NormalMap"),
                    Id("_MaskMap"),kind,target,scale);
            }
            finally {target.UnloadProperties();}
        }
        finally {source.UnloadProperties();}
    }

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
        if(determinant!=0f)tangent=MovableRoadProjection.Tangent(vertex.Normal,u,v);
        if(!float.IsFinite(tangent.x)||!float.IsFinite(tangent.y)||!float.IsFinite(tangent.z)||
            !float.IsFinite(tangent.w))
            throw new InvalidOperationException("Corresponding road tangent is invalid.");
        return new Vertex(vertex.Position,vertex.Normal,tangent,uv,vertex.Color);
    }
}
