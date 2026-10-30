using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

// Source-mapper checks only; the final candidate independently tests native assets.
public static class FixedCarAllDomainsProbe
{
    [Serializable] sealed class Mapping { public Row[] rows; }
    [Serializable] sealed class Row { public int lod,end,lane,part; public float[] x,lowerZ; public float upperZ; }
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var rows=JsonUtility.FromJson<Mapping>(File.ReadAllText(root+"/artifacts/material-remap/Draw02/fixed-car/mapping-plan.json")).rows;
            var native=new Colossal.IO.AssetDatabase.SurfaceAsset();
            int positive=0,negative=0,visitsMaximum=0;
            foreach(var delta in new[]{-2f,0f,2f,5f})foreach(var yaw in new[]{0f,31f,90f})for(int lod=0;lod<2;lod++)
            {
                var pose=new Pose(new Vector3(3,2,-7),Quaternion.Euler(0,yaw,0));
                var original=new MovableRoadAppearance.Level(native,new[]{new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one)},new[]{MovableRoadAppearance.Type.None},new MovableRoadGeometry.Partition(0),new MovableRoadGeometry.Partition(0));
                var level=Draw02FixedCarAtlasAppearance.Apply(lod,original,delta,pose);
                int Count(MovableRoadGeometry.Partition n)=>n.Negative==null?1:1+Count(n.Negative)+Count(n.Positive);
                visitsMaximum=Math.Max(visitsMaximum,Count(level.Carriageway));
                var width=6.4000244140625f;
                int Domain(float x,float z)
                {
                    var p=Quaternion.Inverse(pose.rotation)*(new Vector3(x*(width+delta)/width,0,z)-pose.position);
                    var node=level.Carriageway;
                    while(node.Negative!=null)
                    {
                        if(node.Frame.determinant==0)throw new Exception("Singular partition frame.");
                        node=node.Frame.MultiplyPoint3x4(p).z<0?node.Negative:node.Positive;
                    }
                    return node.Domain;
                }
                foreach(var row in rows)
                {
                    if(row.lod!=lod)continue;
                    float x=(row.x[0]+row.x[1])*.5f;
                    float z=(row.lowerZ[0]*x+row.lowerZ[1]+row.upperZ)*.5f*row.end;
                    int wanted=1+(row.end==1?0:16)+row.part*4+row.lane;
                    int actual=Domain(x,z);if(actual!=wanted)throw new Exception($"Wrong source domain: expected {wanted}, actual {actual}.");
                    var patch=level.Patches[actual];
                    if(level.SupportedTypes[actual]!=MovableRoadAppearance.Type.Asphalt||patch.SourceAtlasRepeat||patch.RepeatU)throw new Exception("Type or transverse semantics changed.");
                    if(row.part<2) {if(!patch.SourceUv||patch.Surface!=native||patch.PreparedUnit)throw new Exception("Native finite identity changed.");}
                    else if(!patch.PreparedUnit||patch.SourceUv||patch.RepeatV!=(row.part==3))throw new Exception("Short/repeat mode changed.");
                    positive++;
                }
                foreach(var end in new[]{-1f,1f})
                {
                    foreach(var point in new[]{new Vector2(1,78.72f),new Vector2(20,84),new Vector2(1,91)})
                    {if(Domain(point.x,point.y*end)!=33)throw new Exception("Unsupported prototype gap/outer domain reached a legacy recipe.");negative++;}
                    if(Domain(1,40*end)!=0)throw new Exception("Fixed mapper captured the moving domain.");negative++;
                }
            }
            var result=$"PASS actual 64-domain source mapper: {positive} domain checks and {negative} gap/outer/moving controls; maximum expanded visits {visitsMaximum}. Private resource stub only; native resource/Compose validation is separate.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/fixed-car/all-domains-mapper-check.txt",result);Debug.Log(result);EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
