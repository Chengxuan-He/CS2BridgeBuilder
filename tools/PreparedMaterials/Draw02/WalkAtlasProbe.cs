using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

public static class WalkAtlasProbe
{
    [Serializable] private sealed class Point { public int lod,domain; public float x,z,u; }
    [Serializable] private sealed class Input { public Point[] points; }
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var input=JsonUtility.FromJson<Input>(File.ReadAllText(root+"/artifacts/material-remap/Draw02/near-cells/walk-atlas-candidates.json"));
            var native=new Colossal.IO.AssetDatabase.SurfaceAsset(); native.id.guid="8a28ab8d9526e258efbcb81c3bbc936b";
            var source=HardcodedNumbers.Load("Types_Draw02_Draw02WalkAtlasAppearance_cs").F32;
            int checks=0;
            foreach(var delta in new[]{-2f,0f,2f,5f})
            foreach(var yaw in new[]{0f,31f,90f})
            for(var lod=0;lod<2;lod++)
            {
                var pose=new Pose(new Vector3(3f,2f,-7f),Quaternion.Euler(0f,yaw,0f));
                var layout=new MovableSelectedRoadLayout { LeftSidewalk=new Vector2(-9.5f-delta,-6.5999755859375f),RightSidewalk=new Vector2(6.5999755859375f,9.5f+delta) };
                var fallback=new MovableRoadGeometry.Partition(0);
                var original=new MovableRoadAppearance.Level(native,new[]{new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one)},
                    new[]{MovableRoadAppearance.Type.None},fallback,fallback);
                var level=Draw02WalkAtlasAppearance.Apply(lod,original,layout,pose);
                foreach(var p in input.points)
                {
                    if(p.lod!=lod)continue;
                    var row=(lod*4+p.domain)*14;
                    var span=p.domain%2==0?layout.LeftSidewalk:layout.RightSidewalk;
                    var targetX=span.x+(p.x-source[row])/(source[row+1]-source[row])*(span.y-span.x);
                    var road=Quaternion.Inverse(pose.rotation)*(new Vector3(targetX,0f,p.z)-pose.position);
                    var partition=level.Sidewalk;
                    while(partition.Negative!=null)partition=partition.Frame.MultiplyPoint3x4(road).z<0?partition.Negative:partition.Positive;
                    var part=Math.Abs(p.z)<4?0:Math.Abs(p.z)<76?1:2;
                    var expected=1+p.domain*3+part;
                    if(partition.Domain!=expected)throw new Exception("Wrong sidewalk source domain.");
                    var patch=level.Patches[expected];
                    if(!patch.SourceUv || patch.Surface!=native || patch.SourceAtlasRepeat!=(part==1) || patch.RepeatU || patch.RepeatV!=(part==1))throw new Exception("Source identity/axis changed.");
                    if(Math.Abs(Vector4.Dot(patch.U,new Vector4(road.x,road.y,road.z,1f))-p.u)*4096>.02f)throw new Exception("Finite sidewalk U changed.");
                    checks++;
                }
            }
            var result=$"PASS Draw02WalkAtlasAppearance: {checks} native face interiors across two LODs, four widths and three poses; original Surface, finite U, original V repeat only in 4..76m; finite terminals.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/walk-atlas-projection-check.txt",result);Debug.Log(result);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
namespace BridgeBuilder.Bridges
{
    internal sealed class MovableSelectedRoadLayout
    {
        internal Vector2 LeftSidewalk,RightSidewalk;
    }
}
