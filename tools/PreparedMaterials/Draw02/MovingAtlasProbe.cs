using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

public static class MovingAtlasProbe
{
    [Serializable] private sealed class Point { public int lod,domain; public float x,z,u; }
    [Serializable] private sealed class Input { public Point[] points; }
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var input=JsonUtility.FromJson<Input>(File.ReadAllText(root+"/artifacts/material-remap/Draw02/near-cells/moving-atlas-candidates.json"));
            var native=new Colossal.IO.AssetDatabase.SurfaceAsset(); native.id.guid="8a28ab8d9526e258efbcb81c3bbc936b";
            int checks=0;
            foreach(var delta in new[]{-2f,0f,2f,5f})
            foreach(var yaw in new[]{0f,31f,90f})
            for(var lod=0;lod<2;lod++)
            {
                var pose=new Pose(new Vector3(3f,2f,-7f),Quaternion.Euler(0f,yaw,0f));
                var fallback=new MovableRoadGeometry.Partition(0);
                var level=Draw02MovingAtlasAppearance.Create(lod,native,delta,pose,
                    new[]{new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one)},
                    new[]{MovableRoadAppearance.Type.None},fallback,fallback);
                foreach(var p in input.points)
                {
                    if(p.lod!=lod)continue;
                    var target=new Vector3(p.x*(6.0999755859375f+delta)/6.0999755859375f,0f,p.z);
                    var road=Quaternion.Inverse(pose.rotation)*(target-pose.position);
                    var partition=level.Carriageway;
                    while(partition.Negative!=null)
                        partition=partition.Frame.MultiplyPoint3x4(road).z<0?partition.Negative:partition.Positive;
                    var part=Math.Abs(p.z)<4?0:Math.Abs(p.z)<76?1:2;
                    var expected=1+p.domain*3+part;
                    if(partition.Domain!=expected)throw new Exception($"Wrong domain LOD{lod}, native {p.domain}, {p.x:R},{p.z:R}: {partition.Domain}!={expected}");
                    var patch=level.Patches[expected];
                    if(!patch.SourceUv || patch.Surface!=native || patch.SourceAtlasRepeat!=(part==1) || patch.RepeatU || patch.RepeatV!=(part==1))throw new Exception("Source identity/axis changed.");
                    var coordinate=new Vector4(road.x,road.y,road.z,1f);
                    if(Math.Abs(Vector4.Dot(patch.U,coordinate)-p.u)*4096>.01f)throw new Exception("Transformed finite U differs from measured affine projection.");
                    checks++;
                }
            }
            var result=$"PASS Draw02MovingAtlasAppearance: {checks} native face interiors across two LODs, four widths and three poses; original Surface, finite U, repeated V only in proved 4..76m range; terminal domains finite.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/moving-atlas-projection-check.txt",result);
            Debug.Log(result);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
