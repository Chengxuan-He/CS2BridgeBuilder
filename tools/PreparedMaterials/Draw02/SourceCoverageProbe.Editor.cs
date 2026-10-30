using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Runs all measured near/mid source faces through frozen production appearance mappers.
// AssetDatabase registration and selected mixed-layer routing are separate checks.
public static class Draw02SourceCoverageProbe
{
    [Serializable] sealed class Coverage {public Face[] faces;}
    [Serializable] sealed class Face {public int lod,role,mode,face;public string kind;public float[] positions;}
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var hash=Environment.GetEnvironmentVariable("DRAW02_CANDIDATE_HASH");
            bool noneSeed=Environment.GetEnvironmentVariable("DRAW02_NONE_SEED")=="true";
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
            if(noneSeed) {
                var retiredKeys=new[]{"d6e1b593dfbb1c928471674b41fceb536c555ee2b7e76993a0e94464a09428a8","66b06f046fc341d42ad3bda4e19d126f2da37a0675098d12c1df45d6a75a4a7d","6fe8104278ede99ba9d088d14b82f5a8c5e8fea04ff346d600d0854e9c58a957","4ceca541dd3558d9055d7340612abadb128343767e1c2c1f376a4510fce4adcc","851b4c15e7f05da15bd71c82d8f0fb686fadd95722ab926d8f66856861cf1c44"};
                foreach(var key in retiredKeys)
                    if(T("MovableRepeatUnits").GetMethod("For",F).Invoke(null,new object[]{"Draw02",key})!=null)
                        throw new Exception("Frozen candidate retains a retired alias.");
                Debug.Log("PASS frozen candidate: all five retired aliases are unmapped.");
            }
            var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);
            var guidType=identifier.GetField("guid").FieldType;
            var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"8a28ab8d9526e258efbcb81c3bbc936b"});
            var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
            void Header(object value,string path) {
                var header=File.ReadAllBytes(path);
                surfaceType.GetField("m_Version",F).SetValue(value,BitConverter.ToUInt16(header,0));
                surfaceType.GetField("m_MaterialTemplateHash",F).SetValue(value,BitConverter.ToInt32(header,2));
                surfaceType.GetField("m_IsVTMaterial",F).SetValue(value,header[6]!=0);
                if(header[6]==0) {
                    using var reader=new BinaryReader(new MemoryStream(header));
                    surfaceType.GetMethod("ReadHeader",F).Invoke(value,new object[]{reader});
                    if((int)surfaceType.GetProperty("materialTemplateHash",F).GetValue(value)!=1 ||
                        (bool)surfaceType.GetProperty("isVTMaterial",F).GetValue(value) ||
                        !System.Text.Encoding.UTF8.GetString(header).Contains("_TANGENTSPACE_OCTO"))
                        throw new Exception("Prepared native cutout/packed-tangent header changed.");
                }
            }
            Header(surface,root+"/artifacts/agent-material-env/Draw02/repeat-units-v2-trial/inputs/source/8a28ab8d9526e258efbcb81c3bbc936b.Surface");
                        // Resource registration is a separate native game test. Seed only
            // identity fixtures for verified development Surface files.
            var preparedMaterials=assembly.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);
            preparedMaterials.GetMethod("Initialize",F).Invoke(null,new object[]{root+"/assets"});
            var catalogue=(System.Collections.IDictionary)preparedMaterials.GetField("Surfaces",F).GetValue(null);
            foreach(var key in new[]{"e5b04c743e8060755c092c47231ff40578fe0e9d9f984cab7dd985fe76a8e2c3","dbcf4b520d9cfe7b5bdced376806be978d10a364de44116f867dbe5a9d0ba541","75dffef619cd682850620b2937e8d63cd406eaa564fffbfd8f241e2e1e048ea3","9c8457b9077bb0b478f487fe21ddc3515dbcda3bb3c69e5d27f1cd4e7f99b9e3"}) {
                var name="Draw02/RepeatUnits/"+key+"/Repeat.Surface";
                var identity=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{File.ReadAllText(root+"/assets/Materials/"+name+".cid").Trim()});
                var asset=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(asset,Activator.CreateInstance(identifier,new[]{identity}));Header(asset,root+"/assets/Materials/"+name);catalogue.Add(name,asset);
            }
            var geometry=T("MovableRoadGeometry");var partitionType=geometry.GetNestedType("Partition",F);
            var vertexType=geometry.GetNestedType("Vertex",F);var triangleType=geometry.GetNestedType("Triangle",F);
            var patchType=T("MovableExistingRoadSurface").GetNestedType("Patch",F);
            var data=JsonUtility.FromJson<Coverage>(File.ReadAllText(root+"/artifacts/material-remap/Draw02/source-face-coverage-input.json"));
            int cases=0,inputs=0,outputs=0;var failures=new System.Collections.Generic.List<string>();
            foreach(float delta in new[]{-2f,0f,2f,5f})foreach(float yaw in new[]{0f,31f,90f})for(int lod=0;lod<2;lod++)
            {
                var pose=new Pose(new Vector3(3,2,-7),Quaternion.Euler(0,yaw,0));var inverse=Quaternion.Inverse(pose.rotation);
                var layout=FormatterServices.GetUninitializedObject(T("MovableSelectedRoadLayout"));
                Set(layout,"LeftSidewalk",new Vector2(-9.5f-delta,-6.5999755859375f));Set(layout,"RightSidewalk",new Vector2(6.5999755859375f,9.5f+delta));
                var oldPatches=T("Draw02RoadAppearance").GetMethod("Patches",F).Invoke(null,new[]{layout,(object)pose});
                var oldSupport=T("Draw02AppearanceSupport").GetMethod("ForLod",F).Invoke(null,new object[]{lod});
                var car=T("Draw02RoadAppearance").GetMethod("Regions",F).Invoke(null,new object[]{false,pose});
                var walk=T("Draw02RoadAppearance").GetMethod("Regions",F).Invoke(null,new object[]{true,pose});
                if(noneSeed) {
                    var seed=Array.CreateInstance(patchType,1);
                    seed.SetValue(New(patchType,Vector2.zero,Vector2.one),0);oldPatches=seed;
                    oldSupport=Array.CreateInstance(T("MovableRoadAppearance").GetNestedType("Type",F),1);
                    car=New(partitionType,0);walk=car;
                }
                var level=T("Draw02MovingAtlasAppearance").GetMethod("Create",F).Invoke(null,new[]{(object)lod,surface,delta,pose,oldPatches,oldSupport,car,walk});
                level=T("Draw02FixedCarAtlasAppearance").GetMethod("Apply",F).Invoke(null,new[]{(object)lod,level,delta,pose});
                level=T("Draw02FixedWalkAtlasAppearance").GetMethod("Apply",F).Invoke(null,new[]{(object)lod,level,layout,pose});
                level=T("Draw02WalkAtlasAppearance").GetMethod("Apply",F).Invoke(null,new[]{(object)lod,level,layout,pose});
                var patches=(Array)Get(level,"Patches");var support=(Array)Get(level,"SupportedTypes");
                for(int role=0;role<2;role++)
                {
                    var faces=data.faces.Where(f=>f.lod==lod&&f.role==role).ToArray();var input=Array.CreateInstance(triangleType,faces.Length);
                    for(int i=0;i<faces.Length;i++)
                    {
                        var source=faces[i];var vertices=new object[3];
                        for(int v=0;v<3;v++)
                        {
                            float x=source.positions[v*3],y=source.positions[v*3+1],z=source.positions[v*3+2];
                            if(source.mode==0)x*=(6.0999755859375f+delta)/6.0999755859375f;
                            else if(source.mode==1)x*=(6.4000244140625f+delta)/6.4000244140625f;
                            else {var sign=x<0?-1f:1f;x=sign*(6.5999755859375f+(Math.Abs(x)-6.5999755859375f)*(2.9000244140625f+delta)/2.9000244140625f);}
                            var p=inverse*(new Vector3(x,y,z)-pose.position);
                            vertices[v]=New(vertexType,p,inverse*Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white);
                        }
                        input.SetValue(New(triangleType,vertices[0],vertices[1],vertices[2],0,0,source.face),i);
                    }
                    object[] bind={input,Get(level,role==0?"Carriageway":"Sidewalk"),patches.Length,1,null,null,false,null,null};
                    if(!(bool)geometry.GetMethod("TryBind",F).Invoke(null,bind))throw new Exception("Source-face binding failed: "+bind[5]);
                    var bound=(Array)bind[4];var represented=new System.Collections.Generic.HashSet<int>();
                    foreach(var entry in bound)
                    {
                        var domain=Convert.ToInt32(Get(entry,"Bone"));var triangle=Get(entry,"Geometry");var face=(int)Get(triangle,"SourceTriangle");represented.Add(face);
                        var patch=patches.GetValue(domain);int types=Convert.ToInt32(support.GetValue(domain));
                        bool complete=(bool)Get(patch,"PreparedUnit")||(bool)Get(patch,"SourceUv");
                        if(domain<(noneSeed?1:16)||types!=(role==0?1:3)||!complete)
                        {
                            var source=faces.Single(f=>f.face==face);
                            var center=((Vector3)Get(Get(triangle,"A"),"Position")+(Vector3)Get(Get(triangle,"B"),"Position")+(Vector3)Get(Get(triangle,"C"),"Position"))/3;
                            failures.Add($"lod={lod},delta={delta},yaw={yaw},role={role},kind={source.kind},sourceFace={face},domain={domain},support={types},complete={complete},center={center:R}");
                        }
                    }
                    foreach(var lost in faces.Where(f=>!represented.Contains(f.face)))failures.Add("Missing source face "+lod+"/"+lost.face);
                    inputs+=faces.Length;outputs+=bound.Length;cases++;
                }
            }
            var report=$"Frozen {hash}: {cases} actual source-face TryBind batches, {inputs} measured source-face uses, {outputs} appearance-bound output triangles; uncovered/legacy findings={failures.Count}. Seed={(noneSeed?"None":"legacy negative detector")}. This is full measured near/mid face routing, not image quality or cold loading.";
            File.WriteAllLines(root+"/artifacts/material-remap/Draw02/source-face-coverage-"+hash+".txt",new[]{report}.Concat(failures));Debug.Log(report);
            if(failures.Count>0)Debug.Log(string.Join("\n",failures.Take(12)));EditorApplication.Exit(failures.Count==0?0:1);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
