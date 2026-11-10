using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Receiver-local source color, not a projected shader on an incompatible object.
// Consume covered receiver pieces once so adjacent/overlapping projection segments
// cannot emit duplicate coplanar colored faces. No texture expansion is involved.
internal static class MovableProjectedColor
{
    internal static bool TryBuild(IReadOnlyList<Triangle> receivers,
        IReadOnlyList<MovableNativeDecals.Region> regions, IReadOnlyDictionary<SurfaceAsset,int> slots,
        Partition partition, int bones, int materialCount, out BoundTriangle[] result, out string? error)
        => TryBuildMapped(receivers,regions,slots,partition,bones,materialCount,null,out result,out error);

    internal static bool TryBuildMapped(IReadOnlyList<Triangle> receivers,
        IReadOnlyList<MovableNativeDecals.Region> regions, IReadOnlyDictionary<SurfaceAsset,int> slots,
        Partition partition,int bones,int materialCount,IReadOnlyDictionary<SurfaceAsset,MovableMaterialCompatibility.Mapping>? mappings,
        out BoundTriangle[] result,out string? error,float clearance=.0005f)
    {
        result=Array.Empty<BoundTriangle>();error=null;
        try
        {
            var pending=receivers.ToList();var colored=new List<Triangle>();
            foreach(var region in regions)
            {
                if(region.SourceColor==null)continue;
                if(!slots.TryGetValue(region.SourceColor,out var slot))
                {error="Projected source color has no realized material slot.";return false;}
                var next=new List<Triangle>();
                foreach(var face in pending)
                {
                float a=Vector3.Dot(face.A.Position-region.Center,region.Up),b=Vector3.Dot(face.B.Position-region.Center,region.Up),c=Vector3.Dot(face.C.Position-region.Center,region.Up);
                if(Math.Min(a,Math.Min(b,c))>region.Half.y || Math.Max(a,Math.Max(b,c)) < -region.Half.y)
                {next.Add(face);continue;}
                foreach(var part in MovableRoadCoverage.SplitColors(new BoundTriangle(face,0),new[]{region}))
                {
                    if(!part.Colored){next.Add(part.Face.Geometry);continue;}
                    Vertex Map(Vertex vertex)
                    {
                        var normal=vertex.Normal.normalized;
                        var tangent=region.Right-normal*Vector3.Dot(region.Right,normal);
                        if(tangent.sqrMagnitude==0)tangent=new Vector3(vertex.Tangent.x,vertex.Tangent.y,vertex.Tangent.z);
                        tangent.Normalize();
                        var mapped=new Vertex(vertex.Position,normal,new Vector4(tangent.x,tangent.y,tangent.z,vertex.Tangent.w),region.SourceUv(vertex.Position),Color.white);
                        if(mappings!=null&&mappings.TryGetValue(region.SourceColor,out var mapping))mapped=mapping.Apply(mapped);
                        return new Vertex(mapped.Position+normal*clearance,mapped.Normal,mapped.Tangent,mapped.UV,mapped.Color);
                    }
                    var triangle=part.Face.Geometry;
                    colored.Add(new Triangle(Map(triangle.A),Map(triangle.B),Map(triangle.C),slot,triangle.Occurrence,triangle.SourceTriangle));
                }
                }
                pending=next;
            }
            if(colored.Count==0)return true;
            return TryBind(colored,partition,bones,materialCount,out result,out error);
        }
        catch(Exception exception){result=Array.Empty<BoundTriangle>();error="Projected color receiver assembly: "+exception;return false;}
    }
}
