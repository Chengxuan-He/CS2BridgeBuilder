using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Runs frozen production mappers, appearance partitioning and the real atlas-wrap
// helper. This is deliberately separate from selected-layer Compose and VT load.
public static class Draw02AtlasCandidateProbe
{
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var hash=Environment.GetEnvironmentVariable("DRAW02_CANDIDATE_HASH");
            if(hash==null||hash.Length!=64)throw new Exception("A frozen candidate SHA is required.");
            var candidate=root+"/artifacts/material-remap/shared/outputs/candidate-"+hash;
            string actualHash;using(var sha=SHA256.Create())actualHash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(candidate+"/BridgeBuilder.dll"))).Replace("-","");
            if(actualHash!=hash)throw new Exception("Candidate binary changed.");
            var managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var name=new AssemblyName(e.Name).Name+".dll";var path=Path.Combine(candidate,name);if(!File.Exists(path))path=Path.Combine(managed,name);return File.Exists(path)?Assembly.LoadFrom(path):null;};
            var assembly=Assembly.LoadFrom(candidate+"/BridgeBuilder.dll");
            const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
            Type T(string name)=>assembly.GetType("BridgeBuilder.Bridges."+name,true);
            object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
            object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
            void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
            var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);
            var guidType=identifier.GetField("guid").FieldType;
            var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"8a28ab8d9526e258efbcb81c3bbc936b"});
            var surface=New(surfaceType);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,New(identifier,cid));
            var geometry=T("MovableRoadGeometry");var partitionType=geometry.GetNestedType("Partition",F);
            var vertexType=geometry.GetNestedType("Vertex",F);var triangleType=geometry.GetNestedType("Triangle",F);
            var patchType=T("MovableExistingRoadSurface").GetNestedType("Patch",F);
            var appearanceType=T("MovableRoadAppearance").GetNestedType("Type",F);
            var fallback=New(partitionType,0);var oldPatches=Array.CreateInstance(patchType,16);
            for(int i=0;i<16;i++)oldPatches.SetValue(New(patchType,Vector2.zero,Vector2.one),i);
            var oldSupport=Array.CreateInstance(appearanceType,16);
            var wrap=T("MovableRoadAtlasWrap").GetMethod("Apply",F);
            int triangles=0,vertices=0,cases=0;double maxAreaError=0,minArea=double.PositiveInfinity;float maxPhase=0,maxU=0;
            for(int lod=0;lod<2;lod++)foreach(float delta in new[]{-2f,0f,2f,5f})foreach(float yaw in new[]{0f,31f,90f})
            foreach(float tilt in new[]{-35f,0f,35f})foreach(int half in new[]{0,1})
            {
                var pose=new Pose(new Vector3(3f,2f,-7f),Quaternion.Euler(0,yaw,0));
                var level=T("Draw02MovingAtlasAppearance").GetMethod("Create",F).Invoke(null,new[]{(object)lod,surface,delta,pose,oldPatches,oldSupport,fallback,fallback});
                var patches=(Array)Get(level,"Patches");var support=(Array)Get(level,"SupportedTypes");
                var sign=half==0?1f:-1f;float scale=(6.0999755859375f+delta)/6.0999755859375f;
                // A strip strictly inside an original grating feature crosses the
                // true repeated cells without manufacturing appearance boundaries.
                var points=new[]{new Vector3(-5.27f*scale,0,sign*4.5f),new Vector3(-5.27f*scale,0,sign*75.5f),new Vector3(-5.23f*scale,0,sign*4.5f)};
                if(sign<0f){var swap=points[1];points[1]=points[2];points[2]=swap;}
                var inverse=Quaternion.Inverse(pose.rotation);
                var n=inverse*(Quaternion.Euler(tilt,0,tilt*.5f)*Vector3.up);
                object V(Vector3 world)
                {
                    var p=inverse*(world-pose.position);
                    return New(vertexType,p,n,new Vector4(0,0,1,1),new Vector4(0,0,p.x*.01f,p.z*.001f),new Color(.3f,.4f,.5f,.6f));
                }
                var input=Array.CreateInstance(triangleType,1);input.SetValue(New(triangleType,V(points[0]),V(points[1]),V(points[2]),0,7,123),0);
                object[] bind={input,Get(level,"Carriageway"),patches.Length,1,null,null,false,null,null};
                if(!(bool)geometry.GetMethod("TryBind",F).Invoke(null,bind))throw new Exception("Production appearance partition failed: "+bind[5]);
                var bound=(Array)bind[4];double area=0;
                foreach(var entry in bound)
                {
                    var domain=Convert.ToInt32(Get(entry,"Bone"));var patch=patches.GetValue(domain);
                    if(Convert.ToInt32(support.GetValue(domain))!=1)throw new Exception("Carriageway support admitted an unrelated type.");
                    if(!(bool)Get(patch,"SourceAtlasRepeat")||!(bool)Get(patch,"ProjectTangent")||!ReferenceEquals(Get(patch,"Surface"),surface))throw new Exception("Actual mapper did not select verified source/tangent mode.");
                    var output=(Array)wrap.Invoke(null,new[]{Get(entry,"Geometry"),patch});
                    var u=(Vector4)Get(patch,"U");var v=(Vector4)Get(patch,"V");var lo=(Vector2)Get(patch,"Min");var hi=(Vector2)Get(patch,"Max");
                    foreach(var triangle in output)
                    {
                        if((int)Get(triangle,"Material")!=0||(int)Get(triangle,"Occurrence")!=7||(int)Get(triangle,"SourceTriangle")!=123)throw new Exception("Source provenance changed.");
                        var positions=new Vector3[3];int c=0;
                        foreach(var name in new[]{"A","B","C"})
                        {
                            var vertex=Get(triangle,name);var p=(Vector3)Get(vertex,"Position");positions[c++]=p;
                            var uv=(Vector4)Get(vertex,"UV");var t=(Vector4)Get(vertex,"Tangent");var normal=(Vector3)Get(vertex,"Normal");
                            var p4=new Vector4(p.x,p.y,p.z,1);var expectedU=Vector4.Dot(u,p4);var phase=(Vector4.Dot(v,p4)-uv.y)/(hi.y-lo.y);
                            maxU=Math.Max(maxU,Math.Abs(expectedU-uv.x));maxPhase=Math.Max(maxPhase,Math.Abs(phase-Mathf.Round(phase)));
                            if(uv.y<lo.y-1e-6f||uv.y>hi.y+1e-6f||Math.Abs(expectedU-uv.x)>2e-6f||Math.Abs(phase-Mathf.Round(phase))>2e-5f)throw new Exception("Source phase or finite U changed.");
                            if(Math.Abs(Vector3.Dot(normal,(Vector3)t))>2e-6f||Math.Abs(((Vector3)t).magnitude-1f)>2e-6f||Math.Abs(t.w)!=1f)throw new Exception("Projected tangent is not a valid receiver frame.");
                            if((normal-n).magnitude>2e-6f||Math.Abs(uv.z-p.x*.01f)>2e-6f||Math.Abs(uv.w-p.z*.001f)>2e-6f||(Color)Get(vertex,"Color")!=new Color(.3f,.4f,.5f,.6f))throw new Exception("Interpolated selected attributes changed.");
                            vertices++;
                        }
                        double a=Vector3.Cross(positions[1]-positions[0],positions[2]-positions[0]).magnitude*.5;
                        area+=a;minArea=Math.Min(minArea,a);triangles++;
                    }
                }
                double expectedArea=Vector3.Cross(points[1]-points[0],points[2]-points[0]).magnitude*.5;
                maxAreaError=Math.Max(maxAreaError,Math.Abs(area-expectedArea)/expectedArea);
                if(Math.Abs(area-expectedArea)>expectedArea*1e-4)throw new Exception("Atlas wrap changed selected geometry area.");
                cases++;
            }
            var report=$"PASS frozen candidate {hash}: actual Create -> TryBind -> AtlasWrap; {cases} long-strip cases, {triangles} triangles/{vertices} vertices, two LODs/four widths/three yaw/three tilted normals/two leaves. Maximum relative area error {maxAreaError:R}; minimum triangle area {minArea:R}; phase error {maxPhase:R}; finite U error {maxU:R}. Original Surface/provenance/interpolated attributes and projected orthonormal tangent preserved. Per-domain support flags verified; selected-input Compose gate, VT/render/persistence remain separate.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/atlas-candidate-output-check.txt",report);Debug.Log(report);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
