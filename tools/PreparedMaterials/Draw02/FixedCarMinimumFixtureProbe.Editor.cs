using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Runs the actual fixed cap/ramp finite production path from a frozen candidate.
// AssetDatabase registration and selected mixed-layer routing are separate checks.
public static class Draw02FixedCarMinimumFixtureProbe
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
            void DumpPartition(object partition) {
                var ids=new System.Collections.Generic.Dictionary<object,int>();
                var nodes=new System.Collections.Generic.List<object>();
                int Visit(object node) {if(node==null)return -1;if(ids.TryGetValue(node,out var found))return found;var index=nodes.Count;ids.Add(node,index);nodes.Add(node);Visit(Get(node,"Negative"));Visit(Get(node,"Positive"));return index;}
                Visit(partition);var rows=new System.Collections.Generic.List<string>();
                foreach(var node in nodes){var matrix=(Matrix4x4)Get(node,"ObjectToSeam");var numbers=Enumerable.Range(0,16).Select(i=>matrix[i/4,i%4].ToString("R",System.Globalization.CultureInfo.InvariantCulture));rows.Add(ids[node]+","+Visit(Get(node,"Negative"))+","+Visit(Get(node,"Positive"))+","+Get(node,"Bone")+","+string.Join(",",numbers));}
                File.WriteAllLines(root+"/artifacts/material-remap/Draw02/fixed-car/candidate-area-partition.csv",rows);
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
            foreach(var key in new[]{"e5b04c743e8060755c092c47231ff40578fe0e9d9f984cab7dd985fe76a8e2c3","dbcf4b520d9cfe7b5bdced376806be978d10a364de44116f867dbe5a9d0ba541","75dffef619cd682850620b2937e8d63cd406eaa564fffbfd8f241e2e1e048ea3"}) {
                var name="Draw02/RepeatUnits/"+key+"/Repeat.Surface";
                var identity=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{File.ReadAllText(root+"/assets/Materials/"+name+".cid").Trim()});
                var asset=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(asset,Activator.CreateInstance(identifier,new[]{identity}));Header(asset,root+"/assets/Materials/"+name);catalogue.Add(name,asset);
            }
            var geometry=T("MovableRoadGeometry");var partitionType=geometry.GetNestedType("Partition",F);
            var vertexType=geometry.GetNestedType("Vertex",F);var triangleType=geometry.GetNestedType("Triangle",F);
            var patchType=T("MovableExistingRoadSurface").GetNestedType("Patch",F);
            var appearanceType=T("MovableRoadAppearance").GetNestedType("Type",F);
            var fallback=New(partitionType,0);var oldPatches=Array.CreateInstance(patchType,16);
            for(int i=0;i<16;i++)oldPatches.SetValue(New(patchType,Vector2.zero,Vector2.one),i);
            var oldSupport=Array.CreateInstance(appearanceType,16);
            var prepared=T("MovableRoadProjection").GetMethod("Prepared",F);
            var inputType=T("MovableSurfaceAtlas").GetNestedType("Input",F);
            var boundType=geometry.GetNestedType("BoundTriangle",F);
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));Set(owner,"<StyleId>k__BackingField","Draw02");
            var paint=Array.CreateInstance(T("MovableNativeDecals").GetNestedType("Region",F),0);
            int relativeFailures=0;var geometryFolder=root+"/artifacts/material-remap/Draw02/fixed-car/geometry-"+hash;Directory.CreateDirectory(geometryFolder);
            int triangles=0,vertices=0,cases=0;double maxAreaError=0,minArea=double.PositiveInfinity;float maxPhase=0,maxU=0;
            for(int kind=0;kind<2;kind++)for(int lod=0;lod<2;lod++)foreach(float delta in new[]{-2f,0f,2f,5f})foreach(float yaw in new[]{0f,31f,90f})
            foreach(float tilt in new[]{-35f,0f,35f})foreach(int half in new[]{0,1})
            {

                if(kind!=1||lod!=0||delta!=-2f||yaw!=31f||tilt!=-35f||half!=1)continue;
                var pose=new Pose(new Vector3(3f,2f,-7f),Quaternion.Euler(0,yaw,0));
                var levelType=T("MovableRoadAppearance").GetNestedType("Level",F);
                var originalLevel=New(levelType,surface,oldPatches,oldSupport,fallback,fallback);
                var level=T("Draw02FixedCarAtlasAppearance").GetMethod("Apply",F).Invoke(null,new[]{(object)lod,originalLevel,delta,pose});
                var patches=(Array)Get(level,"Patches");var support=(Array)Get(level,"SupportedTypes");
                var sign=half==0?1f:-1f;float scale=(6.4000244140625f+delta)/6.4000244140625f;
                float za=kind==0?78.8f:79.95f,zb=kind==0?79.9f:79.975f;
                var points=new[]{new Vector3(-5.5f*scale,0,sign*za),new Vector3(-5.5f*scale,0,sign*zb),new Vector3(5.5f*scale,0,sign*za)};
                if(sign<0f){var swap=points[1];points[1]=points[2];points[2]=swap;}
                var inverse=Quaternion.Inverse(pose.rotation);
                var n=inverse*(Quaternion.Euler(tilt,0,0)*Vector3.up);
                object V(Vector3 world)
                {
                    var p=inverse*(world-pose.position);
                    return New(vertexType,p,n,new Vector4(0,0,1,1),new Vector4(0,0,p.x*.01f,p.z*.001f),new Color(.3f,.4f,.5f,.6f));
                }
                var input=Array.CreateInstance(triangleType,1);input.SetValue(New(triangleType,V(points[0]),V(points[1]),V(points[2]),0,7,123),0);
                if(cases==0)Debug.Log("FixedCar candidate: entering production TryBind.");
                object[] bind={input,Get(level,"Carriageway"),patches.Length,1,null,null,false,null,null};
                if(!(bool)geometry.GetMethod("TryBind",F).Invoke(null,bind))throw new Exception($"Production appearance partition failed: {bind[5]}; kind={kind} lod={lod} delta={delta} yaw={yaw} tilt={tilt} half={half}");
                if(cases==0)Debug.Log("FixedCar candidate: TryBind completed.");
                var bound=(Array)bind[4];double area=0;var triangleDump=new System.Collections.Generic.List<string>();
                foreach(var entry in bound)
                {
                    var domain=Convert.ToInt32(Get(entry,"Bone"));var patch=patches.GetValue(domain);
                    if(Convert.ToInt32(support.GetValue(domain))!=1)throw new Exception("Support admitted an unrelated type.");
                    if((bool)Get(patch,"PreparedUnit")||!(bool)Get(patch,"SourceUv")||(bool)Get(patch,"SourceAtlasRepeat")||(bool)Get(patch,"RepeatU")||(bool)Get(patch,"RepeatV")||!ReferenceEquals(Get(patch,"Surface"),surface))throw new Exception("Actual mapper did not preserve finite original Surface.");
                    var original=Get(entry,"Geometry");
                    var one=Array.CreateInstance(boundType,1);one.SetValue(entry,0);
                    var materialArray=Array.CreateInstance(surfaceType,1);materialArray.SetValue(surface,0);
                    var atlasInput=New(inputType,false);Set(atlasInput,"Faces",one);Set(atlasInput,"Materials",materialArray);Set(atlasInput,"Native",surface);Set(atlasInput,"Patches",patches);Set(atlasInput,"SupportedTypes",support);
                    var atlasInputs=Array.CreateInstance(inputType,1);atlasInputs.SetValue(atlasInput,0);
                    if(cases==0)Debug.Log("FixedCar candidate: entering Compose.");
                    object[] compose={atlasInputs,owner,paint,null};
                    var levels=(Array)T("MovableSurfaceAtlas").GetMethod("Compose",F).Invoke(null,compose);
                    if(cases==0)Debug.Log("FixedCar candidate: Compose completed.");
                    var output=(Array)levels.GetValue(0);
                    if(output.Length!=1||!ReferenceEquals(((Array)((Array)compose[3]).GetValue(0)).GetValue(0),Get(patch,"Surface")))throw new Exception("Production Compose added period triangles or replaced the prepared Surface.");
                    var u=(Vector4)Get(patch,"U");var v=(Vector4)Get(patch,"V");var lo=(Vector2)Get(patch,"Min");var hi=(Vector2)Get(patch,"Max");
                    foreach(var triangle in output)
                    {
                        var physical=Array.CreateInstance(boundType,1);physical.SetValue(New(boundType,triangle,(uint)0),0);
                        object[] modelArgs={"Draw02PrivateFixedFiniteCheck",physical,1,3,null,null};
                        if(!(bool)T("MovableRoadModel").GetMethod("TryBuild",F).Invoke(null,modelArgs))throw new Exception("Native model encoder rejected candidate: "+modelArgs[5]);
                        var model=modelArgs[4];Mesh mesh=null;
                        try {
                            var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();
                            mesh=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
                            var coordinates=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(0,coordinates);
                            if(mesh.vertexCount!=3||mesh.triangles.Length!=3||coordinates.Count!=3)throw new Exception("Native upload changed triangle count.");
                            for(int k=0;k<3;k++)if(coordinates[k]!=(Vector4)Get(Get(triangle,new[]{"A","B","C"}[k]),"UV"))throw new Exception("Native uploaded UV differs from continuous projection.");
                            var attribute=UnityEngine.Rendering.VertexAttribute.BlendIndices;
                            if(mesh.GetVertexAttributeFormat(attribute)!=UnityEngine.Rendering.VertexAttributeFormat.UInt32)throw new Exception("Native road bone format changed.");
                            int stream=mesh.GetVertexAttributeStream(attribute),stride=mesh.GetVertexBufferStride(stream),offset=mesh.GetVertexAttributeOffset(attribute);
                            using var meshData=Mesh.AcquireReadOnlyMeshData(mesh);
                            var bytes=meshData[0].GetVertexData<byte>(stream).ToArray();
                            for(int k=0;k<3;k++)if(BitConverter.ToUInt32(bytes,k*stride+offset)!=(uint)0)throw new Exception("Native upload changed physical bone.");
                        } finally {if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);((IDisposable)model).Dispose();}
                        if((int)Get(triangle,"Material")!=0||(int)Get(triangle,"Occurrence")!=7||(int)Get(triangle,"SourceTriangle")!=123)throw new Exception("Source provenance changed.");
                        var positions=new Vector3[3];int c=0;
                        foreach(var name in new[]{"A","B","C"})
                        {
                            var vertex=Get(triangle,name);var p=(Vector3)Get(vertex,"Position");positions[c++]=p;
                            var uv=(Vector4)Get(vertex,"UV");var t=(Vector4)Get(vertex,"Tangent");var normal=(Vector3)Get(vertex,"Normal");
                            var p4=new Vector4(p.x,p.y,p.z,1);var expectedU=Vector4.Dot(u,p4);var phase=(Vector4.Dot(v,p4)-uv.y)/(hi.y-lo.y);
                            maxU=Math.Max(maxU,Math.Abs(expectedU-uv.x));maxPhase=Math.Max(maxPhase,Math.Abs(phase));
                            if(Math.Abs(expectedU-uv.x)>2e-6f||Math.Abs(phase)>2e-5f)throw new Exception("Source phase or finite U changed.");
                            if(Math.Abs(Vector3.Dot(normal,(Vector3)t))>2e-6f||Math.Abs(((Vector3)t).magnitude-1f)>2e-6f||Math.Abs(t.w)!=1f)throw new Exception("Projected tangent is not a valid receiver frame.");
                            if((normal-n).magnitude>2e-6f||Math.Abs(uv.z-p.x*.01f)>2e-6f||Math.Abs(uv.w-p.z*.001f)>2e-6f||(Color)Get(vertex,"Color")!=new Color(.3f,.4f,.5f,.6f))throw new Exception("Interpolated selected attributes changed.");
                            vertices++;
                        }
                        triangleDump.Add(string.Join(";",positions.Select(pos=>pos.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+pos.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+pos.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
                        double a=Vector3.Cross(positions[1]-positions[0],positions[2]-positions[0]).magnitude*.5;
                        area+=a;minArea=Math.Min(minArea,a);triangles++;
                    }
                }
                var receiver=input.GetValue(0);
                var inputPoints=new[]{"A","B","C"}.Select(name=>(Vector3)Get(Get(receiver,name),"Position")).ToArray();
                double expectedArea=Vector3.Cross(inputPoints[1]-inputPoints[0],inputPoints[2]-inputPoints[0]).magnitude*.5;
                maxAreaError=Math.Max(maxAreaError,Math.Abs(area-expectedArea)/expectedArea);
                var geometryText=string.Join(";",inputPoints.Select(pos=>pos.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+pos.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+pos.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+"\n"+string.Join("\n",triangleDump);
                DumpPartition(Get(level,"Carriageway"));
                File.WriteAllText(root+"/artifacts/material-remap/Draw02/fixed-car/candidate-area-counterexample.txt",geometryText);
                var geometryPath=geometryFolder+"/case-"+kind+"-"+lod+"-"+delta+"-"+yaw+"-"+half+".txt";
                if(File.Exists(geometryPath)&&File.ReadAllText(geometryPath)!=geometryText)throw new Exception("Identical geometry inputs changed with receiver normal.");
                File.WriteAllText(geometryPath,geometryText);
                if(Math.Abs(area-expectedArea)>expectedArea*1e-4)relativeFailures++;
                cases++;
            }
            var report=$"TRANSPORT CHECKS completed for frozen candidate {hash}: actual fixed cap/ramp Apply -> TryBind -> Compose -> native model upload; {cases} cases, {triangles} triangles/{vertices} vertices; minimum original09 fixture only: lod0, delta -2, yaw31, negative end, receiver tilt -35. Maximum relative area error {maxAreaError:R}; minimum triangle area {minArea:R}; phase error {maxPhase:R}; finite U error {maxU:R}. Original Surface, exact constant tangent, uploaded Float32 UV and fixed bone 0, provenance and selected attributes preserved. Asphalt-only flags verified. No period triangles. Original relative-area threshold failures: {relativeFailures}; strict rounding-cell/topology/absolute-bound audit remains separate. Full Prepare, selected-input mixed gate, native AssetDatabase registration/render/persistence remain separate.";
            File.WriteAllText(root+"/artifacts/material-remap/Draw02/fixed-car/minimum-fixture-check.txt",report);Debug.Log(report);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
