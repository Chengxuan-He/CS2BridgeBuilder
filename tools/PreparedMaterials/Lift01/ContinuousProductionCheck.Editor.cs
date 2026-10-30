using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class Lift01RoadContinuousCheck
{
    [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
    const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,I)?.GetValue(o)??o.GetType().GetProperty(n,I).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
    static object New(Type t,params object[] a)=>Activator.CreateInstance(t,I,null,a,null);
    static Array ArrayOf(Type t,IEnumerable<object> input){var a=input.ToArray();var r=Array.CreateInstance(t,a.Length);for(int i=0;i<a.Length;i++)r.SetValue(a[i],i);return r;}
    static void Require(bool b,string text){if(!b)throw new Exception(text);}
    sealed class Lookup:RealProxy
    {
        internal readonly Dictionary<string,object> Assets=new Dictionary<string,object>();
        internal Lookup(Type t):base(t){}
        public override IMessage Invoke(IMessage message)
        {
            var call=(IMethodCallMessage)message;var args=call.Args;
            try{object value;if(call.MethodName=="TryGetAsset"){value=Assets.TryGetValue(args[0].ToString(),out var found);args[1]=found;}
                else if(call.MethodName=="GetHashCode")value=17;
                else if(call.MethodName=="Equals")value=ReferenceEquals(GetTransparentProxy(),args[0]);
                else throw new NotSupportedException(call.MethodName);
                return new ReturnMessage(value,args,args.Length,call.LogicalCallContext,call);
            }catch(Exception e){return new ReturnMessage(e,call);}
        }
    }
    [Serializable]public sealed class Result{public int lod,pose,inputFaces,boundFaces,outputFaces,unsupportedFaces,nativeFaces,droppedAtRebind,splitAtRebind;public float delta;public double inputArea,boundArea,outputArea,nativeArea,droppedArea,rebindAreaChange;public string error;}
    [Serializable]public sealed class Report{public string candidateSha256,scope;public Result[] cases;}
    static double Area(object t)
    {
        Vector3 P(string n)=>(Vector3)Get(Get(t,n),"Position");var a=P("A");var b=P("B");var c=P("C");
        double x=b.x-(double)a.x,y=b.y-(double)a.y,z=b.z-(double)a.z,u=c.x-(double)a.x,v=c.y-(double)a.y,w=c.z-(double)a.z;
        return Math.Sqrt(Math.Pow(y*w-z*v,2)+Math.Pow(z*u-x*w,2)+Math.Pow(x*v-y*u,2))/2;
    }
    static bool PositiveWinding(object t)
    {
        Vector3 P(string n)=>(Vector3)Get(Get(t,n),"Position");var a=P("A");var b=P("B");var c=P("C");var normal=(Vector3)Get(Get(t,"A"),"Normal");
        double x=b.x-(double)a.x,y=b.y-(double)a.y,z=b.z-(double)a.z,u=c.x-(double)a.x,v=c.y-(double)a.y,w=c.z-(double)a.z;
        return (y*w-z*v)*normal.x+(z*u-x*w)*normal.y+(x*v-y*u)*normal.z>0;
    }
    public static void Run()
    {
        var rows=new List<Result>();string output="";string digest="";
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
            var managed=Arg("-gameManaged");var candidate=Arg("-modAssembly");var inputs=Arg("-inputs");output=Arg("-report");
            Require(LoadLibrary(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")))!=IntPtr.Zero,"Native codec unavailable");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
            using(var sha=System.Security.Cryptography.SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(candidate))).Replace("-","");
            var mod=Assembly.LoadFrom(candidate);Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
            var inputType=T("MovableSurfaceAtlas+Input");var surfaceType=inputType.GetField("Materials",I).FieldType.GetElementType();
            var databaseType=surfaceType.Assembly.GetType("Colossal.IO.AssetDatabase.AssetDatabase",true);var database=FormatterServices.GetUninitializedObject(databaseType);
            var setField=databaseType.GetField("m_Databases",I);var databases=Activator.CreateInstance(setField.FieldType);
            var lookup=new Lookup(surfaceType.Assembly.GetType("Colossal.IO.AssetDatabase.IAssetDatabaseInternal",true));
            databases.GetType().GetMethod("Add").Invoke(databases,new[]{lookup.GetTransparentProxy()});setField.SetValue(database,databases);databaseType.GetField("s_Instance",S).SetValue(null,database);
            object Surface(string file,string cid)
            {
                var value=FormatterServices.GetUninitializedObject(surfaceType);using(var r=new BinaryReader(File.OpenRead(Path.Combine(inputs,file))))surfaceType.GetMethod("ReadHeader",I).Invoke(value,new object[]{r});
                var property=surfaceType.GetProperty("id");var hashType=property.PropertyType.GetField("guid").FieldType;var hash=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid});
                property.SetValue(value,New(property.PropertyType,hash));lookup.Assets.Add(cid,value);return value;
            }
            var near=Surface(Path.Combine(Arg("-repo"),"artifacts/material-remap/Lift01/half-floor/inputs/Original.Surface"),"bcdd054871e3bcb50f793ccefc50304d");
            var far=Surface(Path.Combine(Arg("-repo"),"artifacts/material-remap/Lift01/far-finite/Original.Surface"),"e0101c8ca7e9dc14016d187d012704a4");
            var finished=Surface(Path.Combine(Arg("-preparedRoot"),"RepeatUnits/7f727eeef2ad7c28df48e6200815357a780ac5636f83f82cd8d0ba46180e5f43/Repeat.Surface"),"93c8f4081be21c30875a937d59ec3503");
            var callback=T("Lift01HalfFloorAppearance").GetMethod("Prepared",S);
            var farCallback=T("Lift01FarAppearance").GetMethod("Create",S);
            var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var geometry=T("MovableRoadGeometry");var bind=geometry.GetMethod("TryBind",S);
            var physical=T("Lift01RoadPartitions").GetField("Carriageway",S).GetValue(null);
            var frame=geometry.GetMethods(S).Single(m=>m.Name=="InFrame"&&m.ReturnType==triangle.MakeArrayType());
            Array Bind(Array source,object partition,int count,int materials=1){var a=new object[]{source,partition,count,materials,null,null,false,null,null};Require((bool)bind.Invoke(null,a),"TryBind: "+a[5]);return (Array)a[4];}
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));Set(owner,"<StyleId>k__BackingField","Lift01");
            var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
            foreach(float delta in new[]{0f,3f,12f})foreach(int poseId in new[]{0,1,2})for(int lod=0;lod<3;lod++)
            {
                var row=new Result{lod=lod,pose=poseId,delta=delta};rows.Add(row);
                try
                {
                    var pose=poseId==0?Pose.identity:new Pose(new Vector3(3,2,-7),poseId==1?Quaternion.Euler(0,31,0):Quaternion.Euler(13,31,-9));
                    var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
                    var rotation=Quaternion.Inverse(pose.rotation);var inverse=new Pose(rotation*-pose.position,rotation);float half=3+delta;
                    object V(float x,float z)=>New(vertex,new Vector3(x,9.800048828125f,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,7,9),new Color(.5f+x/1000,.5f+z/1000,.3f,.7f));
                    var source=ArrayOf(triangle,new[]{New(triangle,V(-half,-96),V(-half,96),V(half,96),0,13,101),New(triangle,V(-half,-96),V(half,96),V(half,-96),0,13,102)});
                    var motion=Bind(source,physical,5);row.inputFaces=motion.Length;row.inputArea=motion.Cast<object>().Sum(b=>Area(Get(b,"Geometry")));
                    var road=(Array)frame.Invoke(null,new object[]{ArrayOf(triangle,motion.Cast<object>().Select(b=>Get(b,"Geometry"))),inverse});
                    var level=(lod<2?callback.Invoke(null,new object[]{lod,near,finished,pose,delta}):farCallback.Invoke(null,new object[]{far,pose,delta}));var patches=(Array)Get(level,"Patches");
                    var bound=Bind(road,Get(level,"Carriageway"),patches.Length);row.boundFaces=bound.Length;row.boundArea=bound.Cast<object>().Sum(b=>Area(Get(b,"Geometry")));
                    row.unsupportedFaces=0;
                    int escaped=0;double maxEscape=0;
                    foreach(var entry in bound)
                    {
                        int domain=(int)(uint)Get(entry,"Bone");if(domain==0)continue;var patch=patches.GetValue(domain);if(!(bool)Get(patch,"PreparedUnit"))continue;
                        var min=(Vector2)Get(patch,"Min");var max=(Vector2)Get(patch,"Max");
                        foreach(string corner in new[]{"A","B","C"})
                        {var position=(Vector3)Get(Get(Get(entry,"Geometry"),corner),"Position");var point=new Vector4(position.x,position.y,position.z,1);var u=(Vector4)Get(patch,"U");var v=(Vector4)Get(patch,"V");var uv=new Vector2(Vector4.Dot(point,u),Vector4.Dot(point,v));
                         double e=!(bool)Get(patch,"RepeatU")?Math.Max(min.x-uv.x,uv.x-max.x):0;
                         if(e>0){if(escaped<2)Debug.Log($"FINITE_ESCAPE lod={lod} delta={delta} pose={poseId} domain={domain} point={point.ToString("R")} u={u.ToString("R")} uv={uv.ToString("R")} min={min.ToString("R")} max={max.ToString("R")} escape={e:R}");escaped++;maxEscape=Math.Max(maxEscape,e);}}
                    }
                    if(escaped>0)Debug.Log($"FINITE_ESCAPE_TOTAL lod={lod} delta={delta} pose={poseId} count={escaped} max={maxEscape:R}");
                    var diagnostics=new List<string>();
                    foreach(var bf in bound) {
                        int di=(int)(uint)Get(bf,"Bone");var pa=patches.GetValue(di);
                        if(!(bool)Get(pa,"PreparedUnit"))continue;
                        var uu=(Vector4)Get(pa,"U");var vv=(Vector4)Get(pa,"V");var mn=(Vector2)Get(pa,"Min");var mx=(Vector2)Get(pa,"Max");
                        foreach(string corner in new[]{"A","B","C"}) {
                            var pt=(Vector3)Get(Get(Get(bf,"Geometry"),corner),"Position");var q=new Vector4(pt.x,pt.y,pt.z,1);float ux=Vector4.Dot(uu,q),vy=Vector4.Dot(vv,q);
                            if(ux<mn.x||ux>mx.x)diagnostics.Add("domain="+di+" corner="+corner+" local="+pt.ToString("R")+" object="+matrix.MultiplyPoint3x4(pt).ToString("R")+" U="+uu.ToString("R")+" V="+vv.ToString("R")+" min="+mn.ToString("R")+" max="+mx.ToString("R")+" projected="+ux.ToString("R")+","+vy.ToString("R")+" posePosition="+pose.position.ToString("R")+" poseRotation="+pose.rotation.ToString("R"));
                        }
                    }
                    File.WriteAllLines(Path.Combine(inputs,$"finite-guard-{lod}-{delta}-{poseId}.txt"),diagnostics);
                    var input=New(inputType,false);Set(input,"Faces",bound);Set(input,"Materials",ArrayOf(surfaceType,new[]{near}));Set(input,"Native",lod==2?far:near);Set(input,"Patches",patches);Set(input,"SupportedTypes",Get(level,"SupportedTypes"));
                    var call=new object[]{ArrayOf(inputType,new[]{input}),owner,paint,null};var generated=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",S).Invoke(null,call)).GetValue(0);var surfaces=(Array)((Array)call[3]).GetValue(0);
                    row.outputFaces=generated.Length;row.outputArea=generated.Cast<object>().Sum(Area);Require(row.outputFaces==row.boundFaces,"Continuous projection changed topology.");
                    using(var writer=new BinaryWriter(File.Create(Path.Combine(inputs,$"road-capture-{lod}-{delta}-{poseId}.bin"))))
                    {
                        writer.Write(generated.Length);
                        for(int k=0;k<generated.Length;k++)
                        {
                            var t=generated.GetValue(k);var b=bound.GetValue(k);int domain=(int)(uint)Get(b,"Bone");writer.Write(domain);
                            Require(Area(t)>0&&PositiveWinding(t),"Zero or reversed output triangle");Require((int)Get(t,"Occurrence")==13,"Lost occurrence");
                            Require((int)Get(t,"SourceTriangle")== (int)Get(Get(b,"Geometry"),"SourceTriangle"),"Lost source triangle provenance");
                            Require(ReferenceEquals(surfaces.GetValue((int)Get(t,"Material")),Get(patches.GetValue(domain),"Surface")),"Wrong finite/prepared source Surface");
                            foreach(string name in new[]{"A","B","C"})
                            {
                                var v=Get(t,name);var p=(Vector3)Get(v,"Position");var uv=(Vector4)Get(v,"UV");var n=(Vector3)Get(v,"Normal");var tangent=(Vector4)Get(v,"Tangent");
                                var original=Get(Get(b,"Geometry"),name);
                                Require(p.Equals(Get(original,"Position"))&&n.Equals(Get(original,"Normal"))&&Get(v,"Color").Equals(Get(original,"Color")),"Projection changed selected position, normal or color");
                                var patch=patches.GetValue(domain);Require(!(bool)Get(patch,"SourceAtlasRepeat"),"Period cutting still active");var point=new Vector4(p.x,p.y,p.z,1);Require(Mathf.Abs(uv.x-Vector4.Dot((Vector4)Get(patch,"U"),point))<2e-5f&&Mathf.Abs(uv.y-Vector4.Dot((Vector4)Get(patch,"V"),point))<2e-5f,"Continuous/finite direct UV mismatch");
                                Require(float.IsFinite(uv.x)&&float.IsFinite(uv.y)&&uv.z==7&&uv.w==9,"Invalid UV/source channels");Require(float.IsFinite(tangent.w)&&((Vector3)tangent).sqrMagnitude>0,"Invalid native tangent");
                                writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(uv.x);writer.Write(uv.y);writer.Write(uv.z);writer.Write(uv.w);
                                writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);writer.Write(tangent.x);writer.Write(tangent.y);writer.Write(tangent.z);writer.Write(tangent.w);
                                var objectPosition=matrix.MultiplyPoint3x4(p);writer.Write(objectPosition.x);writer.Write(objectPosition.y);writer.Write(objectPosition.z);
                            }
                        }
                    }
                    var objectTriangles=(Array)frame.Invoke(null,new object[]{generated,pose});
                    var skinned=Bind(objectTriangles,physical,5,surfaces.Length);row.nativeFaces=skinned.Length;row.nativeArea=skinned.Cast<object>().Sum(b=>Area(Get(b,"Geometry")));
                    row.rebindAreaChange=row.nativeArea-objectTriangles.Cast<object>().Sum(Area);
                    foreach(var tri in objectTriangles)
                    {
                        var one=new object[]{ArrayOf(triangle,new[]{tri}),physical,5,surfaces.Length,null,null,true,null,null};
                        Require((bool)bind.Invoke(null,one),"Individual physical rebind failed: "+one[5]);int count=((Array)one[4]).Length;
                        if(count==0){row.droppedAtRebind++;row.droppedArea+=Area(tri);}else if(count>1)row.splitAtRebind++;
                    }
                    var buildCall=new object[]{"Lift01 native road verification",skinned,surfaces.Length,5,null,null};
                    Require((bool)T("MovableRoadModel").GetMethod("TryBuild",S).Invoke(null,buildCall),"Native road model: "+buildCall[5]);
                    var model=buildCall[4];Mesh upload=null;
                    try
                    {
                        upload=(Mesh)model.GetType().GetMethod("ToUnityMesh",I).Invoke(model,new object[]{true});var uv4=new List<Vector4>();upload.GetUVs(0,uv4);var positions=upload.vertices;
                        Require(positions.Length==skinned.Length*3,"Native upload vertex count changed");
                        using(var read=Mesh.AcquireReadOnlyMeshData(upload))
                        {
                            foreach(int attribute in new[]{1,2,3,13})
                            {
                                var entry=((IEnumerable)model).Cast<object>().Single(a=>(int)(VertexAttribute)Get(a,"attribute")==attribute);
                                var original=((IEnumerable)Get(entry,"data")).Cast<byte>().ToArray();
                                int attrStream=upload.GetVertexAttributeStream((VertexAttribute)attribute),attrStart=upload.GetVertexAttributeOffset((VertexAttribute)attribute),attrStride=upload.GetVertexBufferStride(attrStream);
                                var uploaded=read[0].GetVertexData<byte>(attrStream);
                                for(int v=0;v<positions.Length;v++)for(int b=0;b<4;b++)Require(uploaded[v*attrStride+attrStart+b]==original[v*4+b],"Upload changed packed native attributes");
                            }
                            int stream=upload.GetVertexAttributeStream(VertexAttribute.BlendIndices),start=upload.GetVertexAttributeOffset(VertexAttribute.BlendIndices),stride=upload.GetVertexBufferStride(stream);var bytes=read[0].GetVertexData<byte>(stream);
                            for(int k=0;k<skinned.Length;k++)
                            {
                                var boundFace=skinned.GetValue(k);uint bone=(uint)Get(boundFace,"Bone");var tri=Get(boundFace,"Geometry");int corner=0;
                                Require(Area(tri)>0&&PositiveWinding(tri),"Physical rebind introduced zero area or reversed winding");
                                foreach(string name in new[]{"A","B","C"})
                                {
                                    int v=k*3+corner++;var original=Get(tri,name);Require(positions[v].Equals(Get(original,"Position"))&&uv4[v].Equals(Get(original,"UV")),"Native upload changed exact position or Float32 UV4");
                                    var packed=BitConverter.GetBytes(bone);for(int b=0;b<4;b++)Require(bytes[v*stride+start+b]==packed[b],"Native upload changed bone ownership");
                                }
                            }
                        }
                    }
                    finally{(model as IDisposable)?.Dispose();if(upload!=null)UnityEngine.Object.DestroyImmediate(upload);}
                    Debug.Log($"Lift01 road native case lod={lod} delta={delta} pose={poseId} input={row.inputFaces} bound={row.boundFaces} output={row.outputFaces} unsupported={row.unsupportedFaces} areaDifference={row.outputArea-row.inputArea:R}");
                }catch(Exception e){row.error=e.ToString();}
            }
            Require(rows.All(r=>r.error==null),"Native Lift01 road cases failed; see report.");Debug.Log("PASS Lift01 road native projection: 27 cases.");
        }
        catch(Exception e){Debug.LogException(e);}
        finally
        {
            if(output!="")File.WriteAllText(output,JsonUtility.ToJson(new Report{candidateSha256=digest,cases=rows.ToArray(),scope="Actual Prepared near/mid and Create far, physical/appearance TryBind, prepared continuous/original finite Compose, physical rebind, MovableRoadModel.TryBuild and ToUnityMesh. Synthetic selected rectangle; isolated source descriptor lookup. Selected network wholeType gate, GPU filtering, animation and cold load require separate tests."},true));
            EditorApplication.Exit(rows.Count==27&&rows.All(r=>r.error==null)?0:1);
        }
    }
}
