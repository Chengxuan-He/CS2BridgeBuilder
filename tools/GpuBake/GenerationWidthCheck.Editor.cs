using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEditor;
public static class GenerationWidthCheck {
const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
static object Get(object o,string n)=>o.GetType().GetField(n,I)?.GetValue(o)??o.GetType().GetProperty(n,I).GetValue(o);
static object New(Type t,params object[] a)=>Activator.CreateInstance(t,I,null,a,null);
static Array Arr(Type t,IEnumerable<object> a){var x=a.ToArray();var r=Array.CreateInstance(t,x.Length);for(int i=0;i<x.Length;i++)r.SetValue(x[i],i);return r;}
static void Need(bool b,string m){if(!b)throw new Exception(m);}
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

public static void Run(){try{
var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];
var managed=Arg("-gameManaged");var root=Arg("-materials");
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
var input=T("MovableSurfaceAtlas+Input");var st=input.GetField("Materials",I).FieldType.GetElementType();
var dt=st.Assembly.GetType("Colossal.IO.AssetDatabase.AssetDatabase",true);var db=FormatterServices.GetUninitializedObject(dt);var df=dt.GetField("m_Databases",I);var ds=Activator.CreateInstance(df.FieldType);var lookup=new Lookup(st.Assembly.GetType("Colossal.IO.AssetDatabase.IAssetDatabaseInternal",true));ds.GetType().GetMethod("Add").Invoke(ds,new[]{lookup.GetTransparentProxy()});df.SetValue(db,ds);dt.GetField("s_Instance",S).SetValue(null,db);
object Surface(string p){var o=FormatterServices.GetUninitializedObject(st);using(var r=new BinaryReader(File.OpenRead(p)))st.GetMethod("ReadHeader",I).Invoke(o,new object[]{r});var cid=File.ReadAllText(p+".cid").Trim();var prop=st.GetProperty("id");var ht=prop.PropertyType.GetField("guid").FieldType;var hash=ht.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid});prop.SetValue(o,New(prop.PropertyType,hash));lookup.Assets[cid]=o;return o;}
var catalog=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);catalog.GetField("_root",S).SetValue(null,root);var cache=(IDictionary)catalog.GetField("Surfaces",S).GetValue(null);
foreach(var style in new[]{"Lift02","Lift03","Lift05","PedestrianDraw01","PedestrianDraw02"})foreach(var path in Directory.GetFiles(Path.Combine(root,style,"RepeatUnits"),"Repeat.Surface",SearchOption.AllDirectories)){var key=path.Substring(root.Length+1).Replace('\\','/');cache[key]=Surface(path);}
object Unit(string style,string key)=>cache[style+"/RepeatUnits/"+key+"/Repeat.Surface"];
var native=Surface(Path.Combine(root,"Lift02/Road/NativeAsphalt.Surface"));
var renderType=T("Lift05NativeRoadAppearance").GetMethod("ForLod",S).GetParameters()[1].ParameterType;
var render=ScriptableObject.CreateInstance(renderType);var sf=renderType.GetField("m_SurfaceAssets",I);var se=sf.FieldType.GetElementType();var id=st.GetProperty("id").GetValue(native);sf.SetValue(render,Arr(se,new[]{New(se,id.GetType().GetField("guid").GetValue(id))}));
var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var patch=T("MovableExistingRoadSurface+Patch");var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);var compose=T("MovableSurfaceAtlas").GetMethod("Compose",S);int levels=0,patchesChecked=0,outputVertices=0;
void Set(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
void CheckPatches(string name,Array patches){var faces=new List<object>();int i=0;foreach(var p in patches){foreach(string axis in new[]{"U","V"}){var v=(Vector4)Get(p,axis);for(int c=0;c<4;c++)Need(!float.IsNaN(v[c])&&!float.IsInfinity(v[c]),name+" nonfinite "+axis);}object V(float x,float z)=>New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(.3f,.4f,7,9),Color.white);var tri=New(triangle,V(0,0),V(.01f,0),V(0,.01f),0,i,i);faces.Add(New(bound,tri,(uint)i));i++;}
var value=New(input,false);Set(value,"Faces",Arr(bound,faces));Set(value,"Materials",Arr(st,new[]{native}));Set(value,"Native",native);Set(value,"Patches",patches);object[] call={Arr(input,new[]{value}),owner,paint,null};var result=(Array)((Array)compose.Invoke(null,call)).GetValue(0);Need(result.Length==patches.Length,name+" topology");foreach(var tri in result)foreach(string corner in new[]{"A","B","C"}){var v=Get(tri,corner);var uv=(Vector4)Get(v,"UV");Need(!float.IsNaN(uv.x)&&!float.IsInfinity(uv.x)&&!float.IsNaN(uv.y)&&!float.IsInfinity(uv.y),name+" invalid evaluated UV");Need(uv.z==7&&uv.w==9,name+" UVzw changed");outputVertices++;}levels++;patchesChecked+=patches.Length;Debug.Log("WIDTHPASS "+name+" patches="+patches.Length);}
void Level(string name,object value){if(name.StartsWith("ZERO_ORIGINAL"))Need(((Array)Get(value,"SupportedTypes")).Cast<object>().All(v=>Convert.ToInt32(v)==0),name+" did not retain Original");if(name.StartsWith("ZERO_FAR"))Need(((Array)Get(value,"SupportedTypes")).Cast<object>().Any(v=>Convert.ToInt32(v)==0),name+" missing Original zero domain");CheckPatches(name,(Array)Get(value,"Patches"));}
object Invoke(string type,string method,params object[] a)=>T(type).GetMethod(method,S).Invoke(null,a);
object Data(string type,string field="Data")=>T(type).GetField(field,S).GetValue(null);
var poses=new[]{new Pose(Vector3.zero,Quaternion.identity),new Pose(new Vector3(3,2,5),Quaternion.Euler(0,37,0))};
var l2Data=Data("Lift02BedAppearance");var l2f=(float[])Get(l2Data,"F32");var l2i=(int[])Get(l2Data,"I32");float l2w=l2f[l2i[2]+4];
foreach(var pose in poses)foreach(float delta in new[]{0f,-l2w,-l2w-1}){Level("Lift02 Prepared "+delta,Invoke("Lift02BedAppearance","Prepared",Unit("Lift02","1c6aac78ba66e66ebd3f5bb395a04d5d0f8223a6bf34f1edb74ef3b873929a48"),pose,delta));Level("Lift02 Far "+delta,Invoke("Lift02BedAppearance","Far",render,pose,delta));}
foreach(var pose in poses)foreach(float w in Enumerable.Range(0,l2i[0]).Select(i=>l2f[i*l2i[1]+2]).Distinct())Level("ZERO_FAR Lift02 "+(-w),Invoke("Lift02BedAppearance","Far",render,pose,-w));
var l3=T("Lift03SourceRoadMapping");var l3data=l3.GetNestedType("Data",I).GetField("Values",S).GetValue(null);var l3i=(int[])Get(l3data,"I32");var l3f=(float[])Get(l3data,"F32");var deck=T("Lift03RoadSampling+Deck");foreach(var pose in poses)foreach(int lod in new[]{0,1})foreach(int d in new[]{0,1}){int cb=(lod*2+d)*4;float w=-l3f[l3i[l3i[2+cb*2]]*35+2];foreach(float delta in new[]{0f,-w,-w-1})Level((delta==-w?"ZERO_ORIGINAL ":"")+"Lift03 "+lod+"/"+d+" "+delta,Invoke("Lift03SourceRoadMapping","Create",Enum.ToObject(deck,d),lod,native,pose,delta,Unit("Lift03",d==0?"9c5ba0c317f4d9ae207c5509a5e191a8c31c224c2305a43e1507bbf726488523":"32247dd719ab358e466505dd8f3fd0702a8f0db566be3e870bf3a7a523489e2c")));}
var l5data=mod.GetType("BridgeBuilder.Bridges.Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs",true).GetField("Values",S).GetValue(null);float l5w=(float)((double[])Get(l5data,"F64"))[1];var units=Arr(st,new[]{"84f30c1a451d65ae38673f25b4e8870b876e20e5977c0254a413fee964b68785","18d35ed1e221c7a1a1cad597b22896ce5146eb6050095b2b86fd3da7605cbd82","bc6c38c2c3b14e5bb5743dec2c2ccd05d02dfd30501dc5f08118b70412ccf2d7","40d7c50979f1502da28972cacf93d06805317986a0d1b1cc6b1faba7ada71284"}.Select(k=>Unit("Lift05",k)));
foreach(var pose in poses)foreach(int lod in new[]{0,1,2})foreach(float delta in new[]{0f,-l5w,-l5w-1})Level((delta==-l5w?"ZERO_ORIGINAL ":"")+"Lift05 "+lod+" "+delta,Invoke("Lift05NativeRoadAppearance","ForLod",lod,render,pose,delta,units));
var p1data=T("PedestrianDraw01PreparedRoadMapping").GetNestedType("Data",I).GetField("Values",S).GetValue(null);var p1f=(float[])Get(p1data,"F32");var p1d=new[]{0f,-p1f[0],-p1f[36],-p1f[0]-1};foreach(var pose in poses)foreach(float delta in p1d)CheckPatches("Ped01 "+delta,(Array)Invoke("PedestrianDraw01PreparedRoadMapping","Patches",pose,delta));
var p1far=T("PedestrianDraw01FarRoadMapping").GetNestedType("Data",I).GetField("Values",S).GetValue(null);var p1farf=(float[])Get(p1far,"F32");foreach(var pose in poses)foreach(float w in Enumerable.Range(0,8).Select(i=>p1farf[i*16+1]).Distinct())foreach(float delta in new[]{-w,-w-1})CheckPatches("Ped01 FAR_ZERO_REVERSE "+delta,(Array)Invoke("PedestrianDraw01FarRoadMapping","Patches",native,pose,delta));
var pd2=Data("PedestrianDraw02AtlasAppearance");var p2i=(int[])Get(pd2,"I32");var p2f=(float[])Get(pd2,"F32");int so=p2i[2]*20;var p2delta=new HashSet<float>{0f,-.5f,-2f,-4f,-8f};for(int n=0;n<p2i[1]-1;n++){float dw=p2f[so+(n+1)*2+1]-p2f[so+n*2+1];if(dw!=0f)p2delta.Add(-(p2f[so+(n+1)*2]-p2f[so+n*2])/dw);}
foreach(var pose in poses)foreach(float delta in p2delta){Level("Ped02 "+delta,Invoke("PedestrianDraw02AtlasAppearance","Prepared",native,Unit("PedestrianDraw02","32b19a6ad3c9751659a935b6b819bcdf6c9e8307b42714b56b500392c22c2ce1"),Unit("PedestrianDraw02","3bd3b389ccd3e506b219788ef22876c161e2d15582578ad50965312f2a99731d"),pose,delta));Invoke("PedestrianDraw02RoadPartitions","Create",delta);Invoke("PedestrianDraw02LocalAppearance","Physical",delta);}
Debug.Log("PASS GenerationWidthCheck actual DLL: levels="+levels+" patches="+patchesChecked+" ComposeVertices="+outputVertices+". Native descriptor header/identity lookup fixture; controlled triangles, not game/ECS/renderer validation.");EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
