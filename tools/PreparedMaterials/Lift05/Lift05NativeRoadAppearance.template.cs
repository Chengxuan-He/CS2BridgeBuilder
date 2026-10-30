using System;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Measured continuous unit phase; only genuine gradient, finite and motion boundaries remain.
internal static class Lift05NativeRoadAppearance
{
    private sealed class LevelData
    {
        internal readonly int[] Roots, Bands, Units, Children;
        internal readonly double[][] Patches, Planes;
        internal LevelData(int[] roots,int[] bands,int[] units,int[] children,double[][] patches,double[][] planes)
        { Roots=roots;Bands=bands;Units=units;Children=children;Patches=patches;Planes=planes; }
    }
    private static class Data
    {
        static Data() {}
        internal static readonly LevelData[] Levels=Read();
        private static LevelData[] Read()
        {
            var d=HardcodedNumbers.Load("Types_Lift05_RoadProjection");int i=0,j=0;
            if(d.I32[i++]!=2 || d.I32[i++]!=3)throw new InvalidOperationException("Invalid Lift05 continuous road schema.");
            var levels=new LevelData[3];
            for(int lod=0;lod<levels.Length;lod++)
            {
                int count=d.I32[i++],nodes=d.I32[i++];var roots=new[]{d.I32[i++],d.I32[i++],d.I32[i++]};
                var bands=new int[count];var units=new int[count];var patches=new double[count][];var planes=new double[nodes][];var children=new int[nodes*2];
                for(int p=0;p<count;p++){bands[p]=d.I32[i++];units[p]=d.I32[i++];patches[p]=new double[10];for(int k=0;k<10;k++)patches[p][k]=d.F64[j++];}
                for(int n=0;n<nodes;n++){children[n*2]=d.I32[i++];children[n*2+1]=d.I32[i++];planes[n]=new[]{d.F64[j++],d.F64[j++],d.F64[j++]};}
                levels[lod]=new LevelData(roots,bands,units,children,patches,planes);
            }
            if(i!=d.I32.Length || j!=d.F64.Length)throw new InvalidOperationException("Unconsumed Lift05 continuous road metadata.");
            return levels;
        }
    }
    private static SurfaceAsset Resolve(int unit) => BridgePreparedMaterials.RepeatUnit("Lift05",unit switch
    {
        0=>"84f30c1a451d65ae38673f25b4e8870b876e20e5977c0254a413fee964b68785",
        1=>"18d35ed1e221c7a1a1cad597b22896ce5146eb6050095b2b86fd3da7605cbd82",
        2=>"bc6c38c2c3b14e5bb5743dec2c2ccd05d02dfd30501dc5f08118b70412ccf2d7",
        3=>"40d7c50979f1502da28972cacf93d06805317986a0d1b1cc6b1faba7ada71284",
        _=>throw new InvalidOperationException("Unknown Lift05 prepared road unit.")
    });
    // Optional finished assets permit isolated native fixture injection; normal creation
    // resolves the identical packaged graph on this existing main-thread native path.
    internal static MovableRoadAppearance.Level ForLod(int lod,RenderPrefab native,Pose roadToObject,float halfDelta,SurfaceAsset[]? finishedUnits=null)
    {
        if(lod<0 || lod>=Data.Levels.Length || !(3+halfDelta>0))throw new InvalidOperationException("Invalid Lift05 road width or LOD.");
        var data=Data.Levels[lod];var surface=native.GetSurfaceAsset(0);var pose=Matrix4x4.TRS(roadToObject.position,roadToObject.rotation,Vector3.one);
        var patches=new Patch[data.Patches.Length+1];var supported=new MovableRoadAppearance.Type[patches.Length];patches[0]=new Patch(Vector2.zero,Vector2.one);
        var finished=new SurfaceAsset[4];
        if(lod<2)for(int unit=0;unit<finished.Length;unit++)finished[unit]=finishedUnits==null?Resolve(unit):finishedUnits[unit];
        double Scale(int band)=>band==1?3.0/(3.0+halfDelta):1.0;
        double Offset(int band)=>band==0?halfDelta:band==2?-halfDelta:0.0;
        for(int p=0;p<data.Patches.Length;p++)
        {
            var v=data.Patches[p];int band=data.Bands[p],unit=data.Units[p];double scale=Scale(band),offset=Offset(band);
            var u=pose.transpose*new Vector4((float)(v[0]*scale),0,(float)v[2],(float)(v[4]+v[0]*offset));
            var w=pose.transpose*new Vector4((float)(v[1]*scale),0,(float)v[3],(float)(v[5]+v[1]*offset));
            var min=new Vector2((float)v[6],(float)v[7]);var max=new Vector2((float)v[8],(float)v[9]);
            patches[p+1]=unit<0?new Patch(min,max,u,w,surface,sourceUv:true,projectTangent:true):
                new Patch(min,max,u,w,finished[unit],preparedUnit:true,repeatU:unit==0,repeatV:true,projectTangent:true).WithFiniteSampling(Vector2.zero,Vector2.one);
            supported[p+1]=MovableRoadAppearance.Type.Asphalt;
        }
        Partition Plane(double x,double z,double constant,Partition negative,Partition positive)
        {
            var matrix=Matrix4x4.identity;matrix.m00=(float)z;matrix.m02=(float)-x;matrix.m20=(float)x;matrix.m22=(float)z;matrix.m23=(float)constant;
            return new Partition(matrix*pose,negative,positive);
        }
        Partition Tree(int node,int band)
        {
            if(node<0)return new Partition(-node-1);
            var line=data.Planes[node];return Plane(line[0]*Scale(band),line[1],line[2]+line[0]*Offset(band),Tree(data.Children[node*2],band),Tree(data.Children[node*2+1],band));
        }
        var regions=Plane(1,0,3+halfDelta,Tree(data.Roots[0],0),Plane(1,0,-3-halfDelta,Tree(data.Roots[1],1),Tree(data.Roots[2],2)));
        return new MovableRoadAppearance.Level(surface,patches,supported,regions,regions);
    }
}
