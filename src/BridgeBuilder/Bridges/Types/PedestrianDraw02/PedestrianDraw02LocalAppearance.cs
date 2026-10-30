using System;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

namespace BridgeBuilder.Bridges;

// Embedded numerical arrays: Types_PedestrianDraw02_LocalAppearance
// Local authored outline rules, grouped by source and width-affine mapping.
internal static class PedestrianDraw02LocalAppearance
{
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_PedestrianDraw02_LocalAppearance");

    internal static Partition Create(Patch[] source, Pose roadToObject, float delta, out Patch[] compact)
    {
        var i=Data.I32;var f=Data.F32;
        if(i[0]!=2)throw new InvalidOperationException(nameof(PedestrianDraw02LocalAppearance));
        compact=new Patch[i[4]];
        for(int n=0;n<compact.Length;n++)compact[n]=source[i[5+n]];
        return Build(roadToObject,delta,false);
    }

    // Physical Split only selects the original bone: it does not transform UV,
    // position or source frame at its leaves. Equal bone leaves therefore have
    // identical physical behavior; appearance retains its distinct projections.
    internal static Partition Physical(float delta) => Build(new Pose(Vector3.zero,Quaternion.identity),delta,true);

    private static Partition Build(Pose roadToObject,float delta,bool physical)
    {
        var i=Data.I32;var f=Data.F32;
        if(i[0]!=2)throw new InvalidOperationException(nameof(PedestrianDraw02LocalAppearance));
        var pose=Matrix4x4.TRS(roadToObject.position,roadToObject.rotation,Vector3.one);
        Partition Plane(float nx,float nz,float c,Partition low,Partition high)
        {
            var matrix=Matrix4x4.identity;
            matrix.m00=nz;matrix.m02=-nx;matrix.m20=nx;matrix.m22=nz;matrix.m23=c;
            return new Partition(matrix*pose,low,high);
        }
        Partition Node(int index)
        {
            if(index<0)return index==-1?Partition.Gap:new Partition(physical?i[5+i[4]-index-2]:-index-2);
            int at=5+i[4]*2+index*3,offset=i[at]*8;
            var low=Node(i[at+1]);var high=Node(i[at+2]);
            if(physical && ((low.IsGap&&high.IsGap) || (low.Bone>=0&&low.Bone==high.Bone)))return low;
            float ax=AffineGeometry.AddScaled(f[offset],f[offset+2],delta),az=f[offset+1];
            float bx=AffineGeometry.AddScaled(f[offset+3],f[offset+5],delta),bz=f[offset+4];
            float nx=az-bz,nz=bx-ax;
            float length=MathF.Sqrt(nx*nx+nz*nz);
            if(!(length>0))throw new InvalidOperationException(nameof(PedestrianDraw02LocalAppearance));
            nx/=length;nz/=length;
            if(nx*f[offset+6]+nz*f[offset+7]<0){nx=-nx;nz=-nz;}
            return Plane(nx,nz,-nx*ax-nz*az,low,high);
        }
        int bounds=i[3]*8;
        var result=Node(i[1]);
        result=Plane(0,1,-f[bounds+3],result,Partition.Gap);
        result=Plane(0,1,-f[bounds+2],Partition.Gap,result);
        result=Plane(1,0,-f[bounds+1]-delta,result,Partition.Gap);
        return Plane(1,0,-f[bounds]+delta,Partition.Gap,result);
    }
}
