using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class Lift04ContinuousReplay
{
    const BindingFlags I=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const BindingFlags S=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static object Get(object o,string name)=>o.GetType().GetField(name,I).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,I).SetValue(o,value);
    static object New(Type t,params object[] values)=>Activator.CreateInstance(t,I,null,values,null);
    static Array ArrayOf(Type type,IEnumerable<object> values){var v=values.ToArray();var a=Array.CreateInstance(type,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
    static string Arg(string key){var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,key)+1];}
    sealed class Lookup : RealProxy
    {
        public readonly Dictionary<string,object> Assets=new Dictionary<string,object>();
        public Lookup(Type type):base(type){}
        public override IMessage Invoke(IMessage message)
        {
            var call=(IMethodCallMessage)message;var args=call.Args;
            try
            {
                object value;
                if(call.MethodName=="TryGetAsset"){value=Assets.TryGetValue(args[0].ToString(),out var asset);args[1]=asset;}
                else if(call.MethodName=="GetHashCode")value=17;
                else if(call.MethodName=="Equals")value=ReferenceEquals(GetTransparentProxy(),args[0]);
                else throw new NotSupportedException("Isolated lookup: "+call.MethodName);
                return new ReturnMessage(value,args,args.Length,call.LogicalCallContext,call);
            }
            catch(Exception e){return new ReturnMessage(e,call);}
        }
    }
    [Serializable] public sealed class Case
    {
        public int lod,pose,physicalFaces,boundFaces,outputFaces,domains,extraFacesAtOriginalWidth,wrongSurface,wrongProvenance,invalidUv,physicalRebindSplits,physicalRebindFailures,preComposeRebindFailures,preComposeRebindSplits;
        public float halfDelta,minOutputArea,maxStationDrift,maxPlaneDrift,maxColorError,maxTangentLengthError;
        public double boundArea,outputArea;
        public string error;
        public Tiny[] smallestFaces;
        public Guard[] finiteViolations;
    }
    [Serializable] public sealed class Guard { public int domain;public VertexFixture vertex;public Vector4 u,v;public Vector2 min,max;public float projectedU,projectedV;public Pose pose;public string failure; }
    [Serializable] public sealed class Tiny { public float area;public int sourceTriangle;public Vector3[] objectPositions;public Vector4[] uv; }
    [Serializable] public sealed class VertexFixture { public Vector3 position,normal;public Vector4 uv,tangent;public Color color; }
    [Serializable] public sealed class WrapFixture
    {
        public string candidateSha256,source;public int domain,sourceTriangle,occurrence,material;
        public VertexFixture[] triangle;public Vector4 u,v,tangent;public Vector2 min,max;
        public bool repeatU,repeatV,sourceUv,sourceAtlasRepeat;public Pose pose;
        public float sourceZMin,sourceZMax,centerSlope,centerOffset,inputArea,smallestOutputArea;
        public Tiny[] output;
    }
    [Serializable] public sealed class Report { public string candidateSha256,scope,limitations;public Case[] cases; }
    public static void Run()
    {
        var cases=new List<Case>();
        try
        {
            var managed=Arg("-gameManaged");var candidate=Arg("-candidate");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{foreach(var folder in new[]{Path.GetDirectoryName(candidate),managed}){var path=Path.Combine(folder,new AssemblyName(e.Name).Name+".dll");if(File.Exists(path))return Assembly.LoadFrom(path);}return null;};
            using var sha=SHA256.Create();var digest=string.Concat(sha.ComputeHash(File.ReadAllBytes(candidate)).Select(b=>b.ToString("X2")));
            if(digest!=Arg("-expectedHash"))throw new Exception("Candidate digest differs.");
            var mod=Assembly.LoadFrom(candidate);Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
            var input=T("MovableSurfaceAtlas+Input");var surfaceType=input.GetField("Materials",I).FieldType.GetElementType();
            var database=surfaceType.Assembly;var globalType=database.GetType("Colossal.IO.AssetDatabase.AssetDatabase",true);
            var global=FormatterServices.GetUninitializedObject(globalType);var setField=globalType.GetField("m_Databases",I);var set=Activator.CreateInstance(setField.FieldType);
            var lookup=new Lookup(database.GetType("Colossal.IO.AssetDatabase.IAssetDatabaseInternal",true));
            set.GetType().GetMethod("Add").Invoke(set,new[]{lookup.GetTransparentProxy()});setField.SetValue(global,set);globalType.GetField("s_Instance",S).SetValue(null,global);
            object Surface(string file,string cid)
            {
                var surface=FormatterServices.GetUninitializedObject(surfaceType);
                using var reader=new BinaryReader(File.OpenRead(file));surfaceType.GetMethod("ReadHeader",I).Invoke(surface,new object[]{reader});
                var idProperty=surfaceType.GetProperty("id");var hashType=idProperty.PropertyType.GetField("guid").FieldType;
                var hash=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid});idProperty.SetValue(surface,New(idProperty.PropertyType,hash));lookup.Assets[cid]=surface;return surface;
            }
            var near=Surface(Arg("-nearSurface"),"6b973ac2133fa9bd8d897bc9eff3363f");
            var far=Surface(Arg("-farSurface"),"3530c13c82fb42d7bd090005884e4956");
            var repeat=Surface(Arg("-repeatSurface"),"56b5f1d96f319327c262af92b45d509e"); var finite=Surface(Arg("-finiteSurface"),"94255dc8a2d3036f149b6f2d095abc31");
            var callback=T("Lift04RoadAppearance").GetMethod("ForLod",S);var renderType=callback.GetParameters()[1].ParameterType;
            object Render(object surface)
            {
                var render=ScriptableObject.CreateInstance(renderType);var refs=renderType.GetField("m_SurfaceAssets",I);var type=refs.FieldType.GetElementType();
                var id=surfaceType.GetProperty("id").GetValue(surface);var hash=id.GetType().GetField("guid").GetValue(id);
                refs.SetValue(render,ArrayOf(type,new[]{New(type,hash)}));return render;
            }
            var nearRender=Render(near);var farRender=Render(far);
            var geometry=T("MovableRoadGeometry");var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");
            var bind=geometry.GetMethod("TryBind",S);var frame=geometry.GetMethods(S).Single(m=>m.Name=="InFrame"&&m.ReturnType==triangle.MakeArrayType());
            var physical=T("Lift04RoadPartitions").GetField("Carriageway",S).GetValue(null);
            Array Frame(Array a,Pose p)=>(Array)frame.Invoke(null,new object[]{a,p});
            Array Bind(Array a,object p,int count){var call=new object[]{a,p,count,1,null,null,false,null,null};if(!(bool)bind.Invoke(null,call))throw new Exception("TryBind: "+call[5]);return (Array)call[4];}
            Array Triangles(Array a)=>ArrayOf(triangle,a.Cast<object>().Select(x=>Get(x,"Geometry")));
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));Set(owner,"<StyleId>k__BackingField","Lift04");
            var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
            Vector3 Position(object v)=>(Vector3)Get(v,"Position");
            float Area(object t)=>Vector3.Cross(Position(Get(t,"B"))-Position(Get(t,"A")),Position(Get(t,"C"))-Position(Get(t,"A"))).magnitude*.5f;
            foreach(float delta in new[]{0f,2f,8f})foreach(int poseIndex in new[]{0,1,2})for(int lod=0;lod<3;lod++)
            {
                var row=new Case{lod=lod,pose=poseIndex,halfDelta=delta,minOutputArea=float.MaxValue};cases.Add(row);
                try
                {
                    var pose=poseIndex==0?Pose.identity:new Pose(new Vector3(3,2,-7),poseIndex==1?Quaternion.Euler(0,31,0):Quaternion.Euler(13,31,-9));
                    var rotation=Quaternion.Inverse(pose.rotation);var inverse=new Pose(rotation*-pose.position,rotation);var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
                    float half=(lod<2?4f:5f)+delta;
                    object V(float x,float z)=>New(vertex,new Vector3(x,9.71875f,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,7,9),new Color(.5f+x/1000,.5f+z/1000,.3f,.7f));
                    var source=ArrayOf(triangle,new[]{New(triangle,V(-half,-96),V(-half,96),V(half,96),0,13,101),New(triangle,V(-half,-96),V(half,96),V(half,-96),0,13,102)});
                    var motion=Bind(source,physical,3);row.physicalFaces=motion.Length;
                    var measured=lod<2?T("Lift04NearAppearance").GetMethod("Prepared",S).Invoke(null,new[]{(object)lod,near,repeat,finite,pose}):callback.Invoke(null,new[]{(object)lod,farRender,pose,delta});var patches=(Array)Get(measured,"Patches");
                    var road=Frame(Triangles(motion),inverse);var bound=Bind(road,Get(measured,"Carriageway"),patches.Length);row.boundFaces=bound.Length;
                    row.domains=bound.Cast<object>().Select(b=>(uint)Get(b,"Bone")).Distinct().Count();row.boundArea=bound.Cast<object>().Sum(b=>(double)Area(Get(b,"Geometry")));
                    var data=(lod<2?T("Lift04NearAppearance"):T("Lift04FarAppearance")).GetField("Data",S).GetValue(null);
                    var numbers=(float[])Get(data,"F32");var counts=(int[])Get(data,"I32");
                    var stations=new List<float>();
                    if(lod<2)for(int strip=0;strip<counts[2];strip++){int a=(lod*counts[2]+strip)*counts[3];stations.Add(numbers[a]);stations.Add(numbers[a+1]);}
                    else for(int cell=0;cell<counts[0];cell++){stations.Add(numbers[cell*counts[1]]);stations.Add(numbers[cell*counts[1]+1]);}
                    var boundPoints=bound.Cast<object>().SelectMany(b=>new[]{"A","B","C"}.Select(c=>matrix.MultiplyPoint3x4(Position(Get(Get(b,"Geometry"),c))))).ToArray();
                    row.maxStationDrift=stations.Max(z=>boundPoints.Min(p=>Mathf.Abs(p.z-z)));
                    foreach(var before in bound)
                    {
                        var baseline=new object[]{Frame(ArrayOf(triangle,new[]{Get(before,"Geometry")}),pose),physical,3,1,null,null,false,null,null};
                        if(!(bool)bind.Invoke(null,baseline))row.preComposeRebindFailures++;
                        else if(((Array)baseline[4]).Length!=1)row.preComposeRebindSplits++;
                    }
                    var sourceInput=New(input,false);Set(sourceInput,"Faces",bound);Set(sourceInput,"Materials",ArrayOf(surfaceType,new[]{near}));Set(sourceInput,"Native",lod<2?near:far);Set(sourceInput,"Patches",patches);Set(sourceInput,"SupportedTypes",Get(measured,"SupportedTypes"));
                    var guards=new List<Guard>();
                    if(lod<2)foreach(var b in bound)
                    {
                        int domain=(int)(uint)Get(b,"Bone");var patch=patches.GetValue(domain);
                        foreach(string corner in new[]{"A","B","C"})
                        {
                            var vertexValue=Get(Get(b,"Geometry"),corner);
                            try{T("MovableRoadProjection").GetMethod("Prepared",S).Invoke(null,new[]{vertexValue,patch});}
                            catch(Exception problem)
                            {
                                var pos=Position(vertexValue);var p4=new Vector4(pos.x,pos.y,pos.z,1);var u=(Vector4)Get(patch,"U");var v=(Vector4)Get(patch,"V");
                                guards.Add(new Guard{domain=domain,vertex=new VertexFixture{position=pos,normal=(Vector3)Get(vertexValue,"Normal"),uv=(Vector4)Get(vertexValue,"UV"),tangent=(Vector4)Get(vertexValue,"Tangent"),color=(Color)Get(vertexValue,"Color")},u=u,v=v,min=(Vector2)Get(patch,"Min"),max=(Vector2)Get(patch,"Max"),projectedU=Vector4.Dot(u,p4),projectedV=Vector4.Dot(v,p4),pose=pose,failure=problem.InnerException?.Message??problem.Message});
                            }
                        }
                    }
                    row.finiteViolations=guards.ToArray();
                    var call=new object[]{ArrayOf(input,new[]{sourceInput}),owner,paint,null};
                    var output=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",S).Invoke(null,call)).GetValue(0);var slots=(Array)((Array)call[3]).GetValue(0);
                    row.outputFaces=output.Length;row.outputArea=output.Cast<object>().Sum(t=>(double)Area(t));
                    if(false)
                    {
                        WrapFixture best=null;var wrap=T("MovableRoadAtlasWrap").GetMethod("Apply",S);
                        foreach(var b in bound)
                        {
                            var t=Get(b,"Geometry");if(delta==0&&Area(t)<1f)continue;
                            int domain=(int)(uint)Get(b,"Bone");var p=patches.GetValue(domain);var wrapped=(Array)wrap.Invoke(null,new[]{t,p});
                            if(wrapped.Length<=1)continue;
                            if(delta==8)
                            {
                                bool RebindFailed(object q){var args=new object[]{Frame(ArrayOf(triangle,new[]{q}),pose),physical,3,1,null,null,false,null,null};return !(bool)bind.Invoke(null,args);}
                                if(RebindFailed(t)||!wrapped.Cast<object>().Any(RebindFailed))continue;
                            }
                            float smallest=wrapped.Cast<object>().Min(Area);if(best!=null&&smallest>=best.smallestOutputArea)continue;
                            int at=(domain/2)*counts[3];
                            best=new WrapFixture{candidateSha256=digest,source="Actual Lift04 ForLod/Create + physical/appearance TryBind; Types_Lift04_NearAppearance schema2, strip=domain/2, side=domain%2. Pose is recorded below; half-width delta is "+delta+".",domain=domain,sourceTriangle=(int)Get(t,"SourceTriangle"),occurrence=(int)Get(t,"Occurrence"),material=(int)Get(t,"Material"),
                                triangle=new[]{"A","B","C"}.Select(c=>{var q=Get(t,c);return new VertexFixture{position=Position(q),normal=(Vector3)Get(q,"Normal"),uv=(Vector4)Get(q,"UV"),tangent=(Vector4)Get(q,"Tangent"),color=(Color)Get(q,"Color")};}).ToArray(),
                                u=(Vector4)Get(p,"U"),v=(Vector4)Get(p,"V"),min=(Vector2)Get(p,"Min"),max=(Vector2)Get(p,"Max"),tangent=(Vector4)Get(p,"Tangent"),repeatU=(bool)Get(p,"RepeatU"),repeatV=(bool)Get(p,"RepeatV"),sourceUv=(bool)Get(p,"SourceUv"),sourceAtlasRepeat=(bool)Get(p,"SourceAtlasRepeat"),pose=pose,
                                sourceZMin=numbers[at],sourceZMax=numbers[at+1],centerSlope=numbers[at+2],centerOffset=numbers[at+3],inputArea=Area(t),smallestOutputArea=smallest,
                                output=wrapped.Cast<object>().Select(q=>new Tiny{area=Area(q),sourceTriangle=(int)Get(q,"SourceTriangle"),objectPositions=new[]{"A","B","C"}.Select(c=>Position(Get(q,c))).ToArray(),uv=new[]{"A","B","C"}.Select(c=>(Vector4)Get(Get(q,c),"UV")).ToArray()}).ToArray()};
                        }
                        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Arg("-report")),delta==0?"minimal-wrap-repro.json":"wide-wrap-rebind-repro.json"),JsonUtility.ToJson(best,true));
                    }
                    row.smallestFaces=output.Cast<object>().OrderBy(Area).Take(8).Select(t=>new Tiny{area=Area(t),sourceTriangle=(int)Get(t,"SourceTriangle"),objectPositions=new[]{"A","B","C"}.Select(c=>matrix.MultiplyPoint3x4(Position(Get(t,c)))).ToArray(),uv=new[]{"A","B","C"}.Select(c=>(Vector4)Get(Get(t,c),"UV")).ToArray()}).ToArray();
                    if(delta==0&&lod<2)row.extraFacesAtOriginalWidth=output.Length-bound.Length;
                    foreach(var t in output)
                    {
                        row.minOutputArea=Mathf.Min(row.minOutputArea,Area(t));
                        var assigned=slots.GetValue((int)Get(t,"Material")); if(lod<2?(!ReferenceEquals(assigned,repeat)&&!ReferenceEquals(assigned,finite)):!ReferenceEquals(assigned,far))row.wrongSurface++;
                        if((int)Get(t,"Occurrence")!=13||!new[]{101,102}.Contains((int)Get(t,"SourceTriangle")))row.wrongProvenance++;
                        foreach(string corner in new[]{"A","B","C"})
                        {
                            var v=Get(t,corner);var p=matrix.MultiplyPoint3x4(Position(v));var uv=(Vector4)Get(v,"UV");var color=(Color)Get(v,"Color");var tangent=(Vector4)Get(v,"Tangent");
                            row.maxPlaneDrift=Mathf.Max(row.maxPlaneDrift,Mathf.Abs(p.y-9.71875f));
                            row.maxColorError=Mathf.Max(row.maxColorError,Mathf.Abs(color.r-(.5f+p.x/1000)),Mathf.Abs(color.g-(.5f+p.z/1000)),Mathf.Abs(color.b-.3f),Mathf.Abs(color.a-.7f));
                            row.maxTangentLengthError=Mathf.Max(row.maxTangentLengthError,Mathf.Abs(((Vector3)tangent).magnitude-1));
                            if(!float.IsFinite(uv.x)||!float.IsFinite(uv.y)||uv.z!=7||uv.w!=9)row.invalidUv++;
                        }
                        var rebindCall=new object[]{Frame(ArrayOf(triangle,new[]{t}),pose),physical,3,slots.Length,null,null,false,null,null};
                        if(!(bool)bind.Invoke(null,rebindCall))row.physicalRebindFailures++;
                        else if(((Array)rebindCall[4]).Length!=1)row.physicalRebindSplits++;
                    }
                }
                catch(Exception e){row.error=e.ToString();}
            }
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{candidateSha256=digest,cases=cases.ToArray(),
                scope="Actual candidate ForLod/Create, physical TryBind, appearance TryBind and Compose replay; 3 LODs x 3 widths x 3 poses. Original native descriptor ReadHeader and Surface lookup used.",
                limitations="Database identity lookup is an isolated proxy; no game database, selected-source classification, GPU renderer, saved-owner cold load or animation. Synthetic flat selected receiver with source provenance/colors, not prototype triangle input. Sliver counts and tiny geometric differences are reported, not silently accepted."},true));
            Debug.Log("Lift04 candidate replay recorded "+cases.Count+" cases.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
