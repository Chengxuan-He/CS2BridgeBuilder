using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class Draw02FarCandidateProbe
{
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
            var candidate=root+"/artifacts/material-remap/shared/outputs/candidate-31EA53D5347CFE9A2DF5D9607395425A4BA1DDD18C10746E028527A811889253";
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var name=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(candidate,name);if(!File.Exists(p))p=Path.Combine(managed,name);return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var type=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            string digest;using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(candidate+"/BridgeBuilder.dll"))).Replace("-","");
            if(digest!="31EA53D5347CFE9A2DF5D9607395425A4BA1DDD18C10746E028527A811889253")throw new Exception("Candidate binary changed.");
            var assembly=Assembly.LoadFrom(candidate+"/BridgeBuilder.dll");
            var id=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);
            var hashType=id.GetField("guid").FieldType;
            var hash=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"436d1828c36d1336cf115c85def2c62c"});
            var asset=Activator.CreateInstance(type,true);
            type.BaseType.GetField("m_Id",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(asset,Activator.CreateInstance(id,new[]{hash}));
            // Metadata fixture from the unchanged source bytes. Resolving the VT
            // graph requires the game's asset database and is tested separately.
            // The actual candidate output path below only reads these header fields.
            var header=File.ReadAllBytes(root+"/artifacts/material-remap/Draw02/far-native.Surface");
            type.GetField("m_Version",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(asset,BitConverter.ToUInt16(header,0));
            type.GetField("m_MaterialTemplateHash",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(asset,BitConverter.ToInt32(header,2));
            type.GetField("m_IsVTMaterial",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(asset,header[6]!=0);
            var far=assembly.GetType("BridgeBuilder.Bridges.Draw02FarAppearance",true).GetMethod("Create",BindingFlags.NonPublic|BindingFlags.Static);
            var atlas=assembly.GetType("BridgeBuilder.Bridges.MovableSurfaceAtlas",true);
            var inputType=atlas.GetNestedType("Input",BindingFlags.NonPublic);
            var geometry=assembly.GetType("BridgeBuilder.Bridges.MovableRoadGeometry",true);
            var vertex=geometry.GetNestedType("Vertex",BindingFlags.NonPublic);
            var triangle=geometry.GetNestedType("Triangle",BindingFlags.NonPublic);
            var bound=geometry.GetNestedType("BoundTriangle",BindingFlags.NonPublic);
            object New(Type t,params object[] args)=>Activator.CreateInstance(t,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,args,null);
            object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(o);
            void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(o,value);
            var ownerType=assembly.GetType("BridgeBuilder.Bridges.MovableRoadMaterial",true);
            var owner=FormatterServices.GetUninitializedObject(ownerType);
            Set(owner,"<StyleId>k__BackingField","Draw02");
            var region=assembly.GetType("BridgeBuilder.Bridges.MovableNativeDecals",true).GetNestedType("Region",BindingFlags.NonPublic);
            var paint=Array.CreateInstance(region,0);
            int verified=0;
            foreach(float delta in new[]{-2f,0f,2f,5f})foreach(float yaw in new[]{0f,31f,90f})
            {
                var pose=new Pose(new Vector3(3f,2f,-7f),Quaternion.Euler(0f,yaw,0f));
                var level=far.Invoke(null,new[]{asset,(object)delta,pose});
                var patches=(Array)Get(level,"Patches");var faces=Array.CreateInstance(bound,32);
                var points=new Vector3[32][];
                for(int domain=0;domain<32;domain++)
                {
                    int half=(domain%16)/8,side=(domain%8)/4,part=domain%4;
                    float x=(side==0?-1f:1f)*(domain<16?3f:8f+delta),z=(half==0?-1f:1f)*new[]{4f,30f,70f,85f}[part];
                    var p=new[]{new Vector3(x,0,z),new Vector3(x,0,z+.1f),new Vector3(x+.1f,0,z)};
                    points[domain]=p.Select(v=>Quaternion.Inverse(pose.rotation)*(v-pose.position)).ToArray();
                    var v=points[domain].Select(pos=>New(vertex,pos,Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white)).ToArray();
                    var tri=New(triangle,v[0],v[1],v[2],0,0,domain);
                    faces.SetValue(New(bound,tri,(uint)domain),domain);
                }
                var input=New(inputType,false);Set(input,"Faces",faces);Set(input,"Patches",patches);Set(input,"SupportedTypes",Get(level,"SupportedTypes"));Set(input,"Native",asset);
                var assets=Array.CreateInstance(type,1);assets.SetValue(asset,0);Set(input,"Materials",assets);
                var inputs=Array.CreateInstance(inputType,1);inputs.SetValue(input,0);
                object[] arguments={inputs,owner,paint,null};
                var result=(Array)atlas.GetMethod("Compose",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,arguments);
                var output=(Array)result.GetValue(0);var surfaces=(Array)((Array)arguments[3]).GetValue(0);
                if(output.Length!=32||surfaces.Length!=1||!ReferenceEquals(surfaces.GetValue(0),asset))throw new Exception("Candidate created new geometry/materials for original far UVs.");
                for(int domain=0;domain<32;domain++)
                {
                    var patch=patches.GetValue(domain);var u=(Vector4)Get(patch,"U");var w=(Vector4)Get(patch,"V");var tri=output.GetValue(domain);
                    for(int corner=0;corner<3;corner++)
                    {
                        var v=Get(tri,new[]{"A","B","C"}[corner]);var position=(Vector3)Get(v,"Position");var uv=(Vector4)Get(v,"UV");var point=points[domain][corner];
                        var p4=new Vector4(point.x,point.y,point.z,1);
                        if(position!=point||uv.x!=Vector4.Dot(p4,u)||uv.y!=Vector4.Dot(p4,w))throw new Exception("Actual candidate output differs from its finite source mapping.");
                        var tangent=(Vector4)Get(v,"Tangent");if(!float.IsFinite(tangent.x)||Math.Abs(tangent.w)!=1f)throw new Exception("Invalid actual candidate tangent.");
                        verified++;
                    }
                }
            }
            var message="PASS frozen candidate "+digest+": actual Draw02FarAppearance.Create -> MovableSurfaceAtlas.Compose; "+verified+" output vertices, 384 triangles; 4 widths and 3 poses; original Surface object retained; no new material/texture, no triangle subdivision; UVs equal actual per-LOD finite projection. Surface metadata is hydrated from unchanged original header bytes; VT graph resolution, selected mixed-layer composition, native rendering and persistence are separate checks.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/far-candidate-output-check.txt",message);Debug.Log(message);
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
