using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class MaterialCompatibility130Check
{
 const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object o,string n)=>o.GetType().GetProperty(n,I)?.GetValue(o)??o.GetType().GetField(n,I).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 // Only fixture CID allocation is substituted: the game's Burst hash initializer
 // is incompatible with this Editor host. Every database IO and Surface method
 // remains native; production has no allocator injection or this proxy.
 sealed class FixtureDatabase:System.Runtime.Remoting.Proxies.RealProxy
 {
  readonly object database; int identity=1000;
  internal FixtureDatabase(Type type,object value):base(type){database=value;}
  public override System.Runtime.Remoting.Messaging.IMessage Invoke(System.Runtime.Remoting.Messaging.IMessage message)
  {
   var call=(System.Runtime.Remoting.Messaging.IMethodCallMessage)message;
   try { var arguments=call.Args;var method=(MethodInfo)call.MethodBase;
    if(method.Name=="AddAsset")for(int i=0;i<arguments.Length;i++)
     if(method.GetParameters()[i].ParameterType.FullName=="Colossal.Hash128")arguments[i]=method.GetParameters()[i].ParameterType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{(++identity).ToString("x32")});
    var value=method.Invoke(database,arguments);
    if(method.Name=="AddAsset" && value!=null)value.GetType().GetProperty("database",I).SetValue(value,GetTransparentProxy());
    return new System.Runtime.Remoting.Messaging.ReturnMessage(value,arguments,arguments.Length,call.LogicalCallContext,call);
   }catch(Exception e){return new System.Runtime.Remoting.Messaging.ReturnMessage(e is TargetInvocationException?e.InnerException:e,call);}
  }
 }
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));Type N(string n)=>native.GetType("Colossal.IO.AssetDatabase."+n,true);
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
 var root=Arg("-outputRoot");Directory.CreateDirectory(root);var dt=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
 var descriptor=Activator.CreateInstance(dt,I,null,new object[]{root},null);var dbType=N("AssetDatabase`1").MakeGenericType(dt);var db=dbType.GetMethod("GetInstance",S).Invoke(null,new[]{descriptor});
 object global;try{global=N("AssetDatabase").GetProperty("global",S).GetValue(null);}catch(TargetInvocationException){global=N("AssetDatabase").GetField("s_Instance",S).GetValue(null);}
 var resources=ScriptableObject.CreateInstance(N("AssetDatabaseResources"));var library=Activator.CreateInstance(N("MaterialLibrary"));var listField=N("MaterialLibrary").GetField("m_Materials",I);var list=(IList)Activator.CreateInstance(listField.FieldType);var mt=N("MaterialLibrary").GetNestedType("MaterialDescription");
 Material Template(int id,string shader,int queue=2225){var d=Activator.CreateInstance(mt);Set(d,"m_Hash",id);var st=mt.GetField("m_Stacks",I);st.SetValue(d,Array.CreateInstance(st.FieldType.GetElementType(),0));var material=new Material(Shader.Find(shader));Need(material.shader!=null,"fixture shader missing");material.name=id==0?"Default":id==13?"DefaultDoubleSided":id==14?"DefaultCutoutDoubleSided":"Fixture"+id;material.renderQueue=queue;Set(d,"m_Material",material);list.Add(d);return material;}
 var target=Template(0,"Hidden/BB129/Object");Template(11,"Unlit/Color");var sourceTemplate=Template(16,"Hidden/BB129/Curve");Template(17,"Unlit/Texture");Template(32,"Unlit/Texture");Template(881130,"Unlit/Texture");Template(30,"Hidden/BB129/Curve",3000);
 foreach(var id in new[]{1,13,14,35,36})Template(id,"Hidden/BB129/Object",id==1||id==14||id==35||id==36?2475:2225);
 var custom=Template(991129,"Hidden/BB129/Curve");sourceTemplate.SetFloat("_Snow",0);custom.SetFloat("_Snow",.25f);custom.SetFloat("_EmissiveIntensity",2.75f);custom.SetColor("_BaseColor",new Color(.2f,.4f,.6f,.8f));
 listField.SetValue(library,list);Set(resources,"m_MaterialLibrary",library);Set(global,"m_Resources",resources);
 var pathType=N("AssetDataPath");var pathCreate=pathType.GetMethods(S).Single(m=>m.Name=="Create"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].ParameterType==typeof(string));
 object PathFor(string name)=>pathCreate.Invoke(null,new object[]{"Fixture",name,Enum.Parse(pathCreate.GetParameters()[2].ParameterType,"None")});
 var add=dbType.GetMethods(I).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));var hashType=add.GetParameters()[2].ParameterType;
 var surfaceType=N("SurfaceAsset");int next=0;object Source(int template){var cid=(++next).ToString("x32");var sf=add.Invoke(db,new[]{PathFor("source"+next),(object)surfaceType,hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid})});var meta=sf.GetType().GetMethod("GetMeta",I).Invoke(sf,null);var path=(string)Get(meta,"path");Directory.CreateDirectory(Path.GetDirectoryName(path));
 // Native version-1 descriptor with real parser/writer, no synthetic Surface bypass.
 using(var w=new BinaryWriter(File.Create(path))){w.Write((ushort)1);w.Write(template);w.Write(false);w.Write((byte)255);w.Write(2);w.Write("_Smoothness");w.Write(.375f);w.Write("_TargetOnly");w.Write(99f);for(int i=0;i<4;i++){w.Write((byte)255);w.Write(0);}w.Write((byte)255);w.Write(1);w.Write("_TEST_APPEARANCE");}return sf;}
 // Seed a bounded prepared-control catalogue for all ten styles. This tests the
 // consumer contract, not native package bootstrap (the real package is checked separately).
 var neutral=Source(0);surfaceType.GetMethod("LoadProperties",I).Invoke(neutral,new object[]{true});
 var preparedType=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);preparedType.GetField("_root",S).SetValue(null,root);
 var catalogue=(IDictionary)preparedType.GetField("Surfaces",S).GetValue(null);
 foreach(var style in new[]{"Draw01","Draw02","Draw03","Lift01","Lift02","Lift03","Lift04","Lift05","PedestrianDraw01","PedestrianDraw02"})catalogue.Add(style+"/Road/NativeAsphalt.Surface",neutral);
 var preview=Activator.CreateInstance(T("PreviewGeometry"),true);Set(preview,"_nativeAssets",new FixtureDatabase(N("ILocalAssetDatabase"),db).GetTransparentProxy());
 var ownedType=typeof(System.Collections.Generic.List<>).MakeGenericType(N("AssetData"));object Owned()=>Activator.CreateInstance(ownedType);
 var geometryType=T("MovableMaterialConversion+GeometryInput");object Geometry(bool evaluated=true,int bones=2)=>Activator.CreateInstance(geometryType,I,null,new object[]{evaluated,true,bones},null);
 var run=T("MovableMaterialConversion").GetMethod("TryCreate",S);object[] Call(object source,object geometry=null)=>new object[]{source,geometry??Geometry(),"tmp11111111-2222-4333-8444-555555555555","Draw02",preview,Owned(),null,null};
 int count=0;
 foreach(int template in new[]{16,991129}){var sf=Source(template);var before=File.ReadAllBytes((string)Get(sf.GetType().GetMethod("GetMeta",I).Invoke(sf,null),"path")); var call=Call(sf);Need((bool)run.Invoke(null,call),"conversion failed: "+call[7]);var output=call[6];Need(!ReferenceEquals(sf,output)&&((IList)call[5]).Count==1,"must realize owned converted Surface");Need((int)Get(output,"materialTemplateHash")==0,"object target");surfaceType.GetMethod("LoadProperties",I).Invoke(output,new object[]{true});var floats=(IDictionary)Get(output,"floats");Need((float)floats["_Smoothness"]==.375f,"source override lost");Need(!floats.Contains("_TargetOnly"),"source-ignored property activated on target");Need((float)floats["_Snow"]==(template==16?0f:.25f),"template default lost");Need(((IEnumerable)Get(output,"keywords")).Cast<string>().Contains("_TEST_APPEARANCE"),"appearance keyword lost");surfaceType.GetMethod("UnloadProperties",I).Invoke(output,new object[]{false});var meta=output.GetType().GetMethod("GetMeta",I).Invoke(output,null);Need(File.Exists((string)Get(meta,"path")),"persistent payload missing");Need(before.SequenceEqual(File.ReadAllBytes((string)Get(sf.GetType().GetMethod("GetMeta",I).Invoke(sf,null),"path"))),"source changed");count++;}
 foreach(int template in new[]{17,30,32,881130}){var call=Call(Source(template));Need((bool)run.Invoke(null,call)&&call[6]!=null&&call[7]==null&&!ReferenceEquals(call[0],call[6])&&((IList)call[5]).Count==1,"valid incompatible source must produce processed material: "+call[7]);Need(new[]{13,14}.Contains((int)Get(call[6],"materialTemplateHash")),"fallback retained incompatible shader");count++;}
 foreach(var geometry in new[]{Geometry(false),Geometry(true,0)}){var call=Call(Source(16),geometry);Need(!(bool)run.Invoke(null,call)&&call[6]==null&&((IList)call[5]).Count==0,"invalid geometry accepted");count++;}
 var obj=Source(0);var direct=Call(obj);Need((bool)run.Invoke(null,direct)&&ReferenceEquals(obj,direct[6])&&((IList)direct[5]).Count==0,"native object reuse changed identity");count++;
 var missing=Call(null);Need(!(bool)run.Invoke(null,missing)&&missing[6]==null&&missing[7]!=null,"missing resource failed noexcept");count++;

 var preserved=Source(17);var nativeGeometry=geometryType.GetProperty("NativeNetwork",S).GetValue(null);
 var nativeCall=Call(preserved,nativeGeometry);Need((bool)run.Invoke(null,nativeCall)&&ReferenceEquals(preserved,nativeCall[6])&&((IList)nativeCall[5]).Count==0,"unchanged native graph source was converted or rejected");count++;
 // Failure after owned descriptor allocation must roll it back, not expose a
 // partial success or delete its source. Missing target appearance is not optional.
 var savedShader=target.shader;target.shader=Shader.Find("Hidden/BB129/Missing");
 var incomplete=Source(16);int filesBefore=Directory.GetFiles(root,"*.Surface",SearchOption.AllDirectories).Length;
 var rejected=Call(incomplete);Need((bool)run.Invoke(null,rejected)&&rejected[6]!=null&&rejected[7]==null&&((IList)rejected[5]).Count==1,"property mismatch must fall back after cleaning equivalent attempt: "+rejected[7]);
 Need(Directory.GetFiles(root,"*.Surface",SearchOption.AllDirectories).Length==filesBefore+1,"equivalent attempt leaked an extra payload");target.shader=savedShader;count++;
 // Real native colored-lane Texture payloads are bound without any pixel remake.
 foreach(var payload in new[]{Arg("-bikeTexture"),Arg("-busTexture")})
 {
  var tx=add.Invoke(db,new[]{PathFor("colorTexture"+next),(object)N("TextureAsset"),hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{(next+8000).ToString("x32")})});
  var txPath=(string)Get(tx.GetType().GetMethod("GetMeta",I).Invoke(tx,null),"path");Directory.CreateDirectory(Path.GetDirectoryName(txPath));File.Copy(payload,txPath,true);
  var colored=Source(32);surfaceType.GetMethod("LoadProperties",I).Invoke(colored,new object[]{true});
  var bindings=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string),N("TextureAsset")));bindings.Add("_BaseColorMap",tx);
  surfaceType.GetMethod("UpdateTextures",I).Invoke(colored,new object[]{bindings});surfaceType.GetMethod("UpdateKeywords",I).Invoke(colored,new object[]{new System.Collections.Generic.HashSet<string>{"_MATERIAL_AFFECTS_ALBEDO"}});surfaceType.GetMethod("SaveData",I).Invoke(colored,new object[]{true});
  var coloredCall=Call(colored);Need((bool)run.Invoke(null,coloredCall),"colored source conversion failed: "+coloredCall[7]);
  var texMap=(IDictionary)Get(coloredCall[6],"textures");Need(texMap.Contains("_BaseColorMap")&&Get(Get(texMap["_BaseColorMap"],"id"),"guid").Equals(Get(Get(tx,"id"),"guid")),"colored source texture identity lost");
  Need(File.ReadAllBytes(payload).SequenceEqual(File.ReadAllBytes(txPath)),"source color texture was resized/rewritten");Need((int)Get(coloredCall[6],"materialTemplateHash")==14,"color layer lacks native alpha object protocol");count++;
 }
 // A world-only source returns a coordinate plan, not a wrong-resolution
 // base-UV binding. Both road/lane consumers apply it before native mesh encoding.
 var world=Source(17);surfaceType.GetMethod("LoadProperties",I).Invoke(world,new object[]{true});
 var worldBindings=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string),N("TextureAsset")));
 var worldTexture=add.Invoke(db,new[]{PathFor("worldTexture"),(object)N("TextureAsset"),hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"a1111111222233334444555555555555"})});
 var worldTexturePath=(string)Get(worldTexture.GetType().GetMethod("GetMeta",I).Invoke(worldTexture,null),"path");Directory.CreateDirectory(Path.GetDirectoryName(worldTexturePath));File.Copy(Arg("-bikeTexture"),worldTexturePath,true);worldBindings.Add("_WorldspaceAlbedo",worldTexture);
 surfaceType.GetMethod("UpdateTextures",I).Invoke(world,new object[]{worldBindings});((IDictionary)Get(world,"floats"))["_WorldspaceUVScale"]=2f;surfaceType.GetMethod("SaveData",I).Invoke(world,new object[]{true});
 var worldCall=Call(world).Concat(new object[]{null}).ToArray();Need((bool)T("MovableMaterialConversion").GetMethod("TryCreateMapped",S).Invoke(null,worldCall),"world-only source failed: "+worldCall[7]);
 Need((bool)Get(worldCall[8],"Planar")&&(float)Get(worldCall[8],"Scale")==2,"world coordinate plan missing");count++;
 // The same no-albedo unknown shader works for every installed movable style.
 foreach(var style in new[]{"Draw01","Draw02","Draw03","Lift01","Lift02","Lift03","Lift04","Lift05","PedestrianDraw01","PedestrianDraw02"})
 {var call=Call(Source(881130));call[3]=style;Need((bool)run.Invoke(null,call)&&call[6]!=null&&call[7]==null,"neutral unknown material failed for "+style+": "+call[7]);count++;}
 // Real production clipping: a partial receiver footprint, two overlapping
 // segments, two native bones, and a gap. Area must not count the overlap twice.
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var partition=T("MovableRoadGeometry+Partition");var region=T("MovableNativeDecals+Region");
 object New(Type t,params object[] values)=>Activator.CreateInstance(t,I,null,values,null);
 object V(float x,float z)=>New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white);
 var receivers=Array.CreateInstance(triangle,2);receivers.SetValue(New(triangle,V(-1,-1),V(1,-1),V(1,1),0,0,0),0);receivers.SetValue(New(triangle,V(-1,-1),V(1,1),V(-1,1),0,0,1),1);
 var projectionSource=Source(32);var regions=Array.CreateInstance(region,2);
 regions.SetValue(New(region,new Vector3(-.25f,0,0),Quaternion.identity,new Vector3(.5f,1,1),false,projectionSource,new Vector4(0,0,1,1)),0);
 regions.SetValue(New(region,new Vector3(.25f,0,0),Quaternion.identity,new Vector3(.5f,1,1),false,projectionSource,new Vector4(0,0,1,1)),1);
 var slots=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(surfaceType,typeof(int)));slots.Add(projectionSource,0);
 var split=New(partition,Matrix4x4.identity,New(partition,0),New(partition,1));
 object[] projection={receivers,regions,slots,split,2,1,null,null};var project=T("MovableProjectedColor").GetMethod("TryBuild",S);
 Need((bool)project.Invoke(null,projection),"projected color assembly failed: "+projection[7]);
 double area=0;var boneSet=new System.Collections.Generic.HashSet<uint>();
 foreach(var face in (IEnumerable)projection[6]){boneSet.Add((uint)Get(face,"Bone"));var tri=Get(face,"Geometry");var a=(Vector3)Get(Get(tri,"A"),"Position");var b=(Vector3)Get(Get(tri,"B"),"Position");var c=(Vector3)Get(Get(tri,"C"),"Position");area+=Math.Abs((b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x))/2;foreach(var key in new[]{"A","B","C"}){var v=Get(tri,key);var position=(Vector3)Get(v,"Position");var uv=(Vector4)Get(v,"UV");Need(Math.Abs(position.y-.0005f)<1e-7&&Math.Abs(position.x)<=.75001f&&uv.x>=-.0001f&&uv.x<=1.0001f&&uv.y>=-.0001f&&uv.y<=1.0001f,"color footprint/UV/clearance");}}
 Need(Math.Abs(area-3)<1e-5&&boneSet.SetEquals(new uint[]{0,1}),"overlap duplicated area or bone assignment lost: "+area);count++;
 var gap=partition.GetField("Gap",S)?.GetValue(null)??partition.GetProperty("Gap",S).GetValue(null);projection[3]=New(partition,Matrix4x4.identity,New(partition,0),gap);
 Need((bool)project.Invoke(null,projection),"physical gap must be skipped");foreach(var face in (IEnumerable)projection[6])Need((uint)Get(face,"Bone")==0,"gap generated an unsupported bone");count++;
 var elevated=Array.CreateInstance(triangle,4);Array.Copy(receivers,elevated,2);
 object High(float x,float z)=>New(vertex,new Vector3(x,5,z),Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white);
 elevated.SetValue(New(triangle,High(-1,-1),High(1,-1),High(1,1),0,1,0),2);elevated.SetValue(New(triangle,High(-1,-1),High(1,1),High(-1,1),0,1,1),3);
 projection[0]=elevated;projection[3]=split;Need((bool)project.Invoke(null,projection),"height-separated receiver test failed");
 foreach(var face in (IEnumerable)projection[6])foreach(var key in new[]{"A","B","C"})Need(((Vector3)Get(Get(Get(face,"Geometry"),key),"Position")).y<1,"color projected onto unrelated elevated deck");count++;
 var mappedVertex=worldCall[8].GetType().GetMethod("Apply",I).Invoke(worldCall[8],new[]{V(3,5)});Need(((Vector4)Get(mappedVertex,"UV")).Equals(new Vector4(6,10,0,0)),"world mapping does not consume source coordinates");count++;
 // Real installed VT descriptor: exercise the same production full-header copy,
 // native property reader/writer and persisted reload without a fake VT reservation.
 var vtBytes=File.ReadAllBytes(Arg("-vtSurface"));var vt=Source(0);
 var write=T("MovableMaterialConversion").GetMethod("TryWriteDescriptor",S);
 object[] vtCopy={vtBytes,0,vt,null};Need((bool)write.Invoke(null,vtCopy),"VT descriptor copy failed: "+vtCopy[3]);
 surfaceType.GetMethod("LoadProperties",I).Invoke(vt,new object[]{true});Need((bool)Get(vt,"isVTMaterial"),"VT header lost");
 var vtTextures=(IDictionary)Get(vt,"textures");var ids=vtTextures.Keys.Cast<object>().ToDictionary(k=>(string)k,k=>vtTextures[k]==null?null:Get(vtTextures[k],"id").ToString());
 surfaceType.GetMethod("SaveData",I).Invoke(vt,new object[]{true});surfaceType.GetMethod("UnloadProperties",I).Invoke(vt,new object[]{true});
 surfaceType.GetMethod("LoadProperties",I).Invoke(vt,new object[]{true});var loaded=(IDictionary)Get(vt,"textures");
 Need(loaded.Count==ids.Count,"VT serialized layers changed");foreach(var k in ids.Keys)Need((loaded[k]==null?null:Get(loaded[k],"id").ToString())==ids[k],"VT source identity changed: "+k);
 var vtPath=(string)Get(vt.GetType().GetMethod("GetMeta",I).Invoke(vt,null),"path");Need(vtBytes.SequenceEqual(File.ReadAllBytes(vtPath)),"VT header/backing/descriptor roundtrip is not exact");count++;
 object[] badSchema={new byte[]{1,0},0,vt,null};Need(!(bool)write.Invoke(null,badSchema)&&badSchema[3]!=null,"truncated schema accepted");Need(vtBytes.SequenceEqual(File.ReadAllBytes(vtPath)),"invalid input changed existing payload");count++;
 Debug.Log("PASS "+count+" compatible material and projected receiver cases; actual native assemblies, fixture protocol shaders, no game visual claim.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
