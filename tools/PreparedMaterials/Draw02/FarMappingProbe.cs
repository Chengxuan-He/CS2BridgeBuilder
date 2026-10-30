using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

public static class FarMappingProbe
{
    [Serializable] private sealed class Point { public int domain; public float x,z,u,v,loU,hiU,loV,hiV; }
    [Serializable] private sealed class Input { public Point[] points; }
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var data=JsonUtility.FromJson<Input>(File.ReadAllText(root+"/artifacts/material-remap/Draw02/far-native-candidates.json"));
            var n=HardcodedNumbers.Load("Types_Draw02_Draw02FarAppearance_cs").F32;
            int checks=0; float worst=0;
            foreach(float delta in new[]{-2f,0f,2f,5f})
            foreach(float yaw in new[]{0f,31f,90f})
            foreach(int role in new[]{0,1})
            {
                var pose=new Pose(new Vector3(3f,2f,-7f),Quaternion.Euler(0f,yaw,0f));
                var level=Draw02FarAppearance.Create(new Colossal.IO.AssetDatabase.SurfaceAsset(),delta,pose);
                foreach(float z in new[]{-85f,-70f,-30f,-1f,1f,30f,70f,85f})
                foreach(float x in new[]{-1f,1f})
                {
                    var road=Quaternion.Inverse(pose.rotation)*(new Vector3(x,0f,z)-pose.position);
                    var partition=role==0?level.Carriageway:level.Sidewalk;
                    while(partition.Negative!=null)
                        partition=partition.Frame.MultiplyPoint3x4(road).z<0f?partition.Negative:partition.Positive;
                    var part=Math.Abs(z)<8f?0:Math.Abs(z)<60f?1:Math.Abs(z)<78.72f?2:3;
                    var expected=role*16+(z<0f?0:8)+(x<0f?0:4)+part;
                    if(partition.Domain!=expected) throw new Exception("Far appearance partition selected the wrong domain.");
                    var support=level.SupportedTypes[expected];
                    var wanted=role==0?MovableRoadAppearance.Type.Asphalt:MovableRoadAppearance.Type.Asphalt|MovableRoadAppearance.Type.Sidewalk;
                    if(support!=wanted) throw new Exception("Far domain admitted the wrong appearance layers.");
                }
                foreach(var p in data.points)
                {
                    var part=p.domain%4; var sign=p.domain<8?-1f:1f;
                    var scale=1f; var offset=0f;
                    if(part==0) { var centre=role==0?n[121]:n[122];scale=(n[112]-n[120])/(n[112]-centre);offset=sign*(n[120]-scale*centre); }
                    if(part==2) { scale=(n[117]-n[113])/(n[114]-n[113]);offset=sign*n[113]*(1f-scale); }
                    if(part==3) { scale=(n[119]-n[118])/(n[116]-n[115]);offset=sign*(n[118]-scale*n[115]); }
                    var half=n[p.domain*7+6];
                    var target=new Vector3(p.x*(half+delta)/half,0f,(p.z-offset)/scale);
                    var road=Quaternion.Inverse(pose.rotation)*(target-pose.position);
                    var point=new Vector4(road.x,road.y,road.z,1f);
                    var patch=level.Patches[role*16+p.domain];
                    var u=Vector4.Dot(patch.U,point); var v=Vector4.Dot(patch.V,point);
                    if(!patch.SourceUv || patch.Repeat || patch.Surface!=level.Native) throw new Exception("Finite source identity lost.");
                    if(!(u>p.loU && u<p.hiU && v>p.loV && v<p.hiV))
                        throw new Exception($"Domain {p.domain}, role {role}, delta {delta}, yaw {yaw}: ({u:R},{v:R}) outside ({p.loU:R}..{p.hiU:R},{p.loV:R}..{p.hiV:R}).");
                    worst=Math.Max(worst,Math.Max(Math.Abs(u-p.u),Math.Abs(v-p.v))*512f);
                    checks++;
                }
            }
            var result=$"PASS actual Draw02FarAppearance with Unity matrices: {checks} candidate projections; four widths, three poses, both roles; all UV coordinates strictly inside original Float16 intervals; maximum error {worst:R} base-mip pixels.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/far-native-projection-result.txt",result);
            Debug.Log(result); EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}

namespace Colossal.IO.AssetDatabase
{
    internal sealed class SurfaceAsset
    {
        internal Identity id=new Identity();
        internal sealed class Identity { internal string guid="436d1828c36d1336cf115c85def2c62c"; }
    }
}
namespace BridgeBuilder.Bridges
{
    internal sealed class HardcodedNumbers
    {
        internal float[] F32;
        internal static HardcodedNumbers Load(string name)
        {
            var bytes=File.ReadAllBytes(Environment.GetEnvironmentVariable("DRAW02_REPO")+"/assets/HardcodedNumbers/Draw02/"+name+".bin");
            var values=new float[BitConverter.ToInt32(bytes,24)];
            Buffer.BlockCopy(bytes,32,values,0,values.Length*4);
            return new HardcodedNumbers { F32=values };
        }
    }
    internal static class MovableRoadGeometry
    {
        internal sealed class Partition
        {
            internal readonly int Domain; internal readonly Matrix4x4 Frame;
            internal readonly Partition Negative,Positive;
            internal Partition(int domain) { Domain=domain; }
            internal Partition(Matrix4x4 frame,Partition negative,Partition positive) { Frame=frame;Negative=negative;Positive=positive; }
        }
    }
}
