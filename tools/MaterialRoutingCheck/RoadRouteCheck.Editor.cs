using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class RoadRouteCheck {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static Array Arr(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
 static readonly System.Collections.Generic.Dictionary<int,object[]> Captured = new();
 static int captureCalls, captureTriangles, sourceDomainChecks;
 static void Capture<T>(int lod, System.Collections.Generic.IReadOnlyList<T> values) {
  Check(!Captured.ContainsKey(lod), "capture repeated a LOD"); Captured.Add(lod,values.Cast<object>().ToArray());
 }
 static object InvokeCompose(MethodInfo compose, object[] call) {
  if(compose.GetParameters().Length==4)return compose.Invoke(null,call);
  object[] plain={call[0],call[1],call[2],null,null};
  var before=(Array)compose.Invoke(null,plain);
  var callbackType=compose.GetParameters()[4].ParameterType;
  var bindingType=callbackType.GenericTypeArguments[1].GenericTypeArguments[0];
  var callback=Delegate.CreateDelegate(callbackType,typeof(RoadRouteCheck).GetMethod("Capture",S).MakeGenericMethod(bindingType));
  Captured.Clear(); object[] observed={call[0],call[1],call[2],null,callback};
  var after=(Array)compose.Invoke(null,observed); Check(after.Length==before.Length,"capture LOD count");
  Check(Captured.Count==after.Length,"capture omitted empty/original LOD");
  for(int lod=0;lod<after.Length;lod++){
   var a=(Array)before.GetValue(lod);var b=(Array)after.GetValue(lod);var bindings=Captured[lod];
   Check(a.Length==b.Length&&bindings.Length==b.Length,"capture output triangle count");
   var sa=(Array)((Array)plain[3]).GetValue(lod);var sb=(Array)((Array)observed[3]).GetValue(lod);
   Check(sa.Length==sb.Length,"capture material slots");
   for(int slot=0;slot<sa.Length;slot++)Check(ReferenceEquals(sa.GetValue(slot),sb.GetValue(slot)),"capture changed material identity");
   for(int i=0;i<a.Length;i++){
    Check(a.GetValue(i).Equals(b.GetValue(i)),"capture changed vertex attributes/topology/provenance");
    var binding=bindings[i];var triangle=b.GetValue(i);
    Check(Get(binding,"Receiver").Equals(triangle),"capture receiver mismatch");
    Check(ReferenceEquals(Get(binding,"Surface"),sb.GetValue((int)Get(triangle,"Material"))),"capture surface slot mismatch");
    Check(Get(binding,"SourceSurface")!=null,"capture lost original source");
    var mode=Get(binding,"ProjectionKind").ToString();
    if(mode=="Corresponding")Check((float)Get(binding,"CompatibleScale")==.4f,"capture compatible density lost");
    if(mode=="Prepared")Check((bool)Get(Get(binding,"Patch"),"PreparedUnit"),"capture prepared semantics lost");
    if(mode=="Original")Check(Get(binding,"Route").ToString()=="Original" || (Get(binding,"Route").ToString()=="Corresponding" && (bool)Get(Get(binding,"SourceIdentity"),"TiledPaving")),"capture retained route mislabeled");
    var domainField=binding.GetType().GetField("SourceDomain",F);
    if(domainField!=null){
     var domain=domainField.GetValue(binding);
     bool eligible=mode=="Original"&&(bool)Get(binding,"MeasuredBase")&&Get(binding,"ReceiverType").ToString()=="Sidewalk";
     Check((domain!=null)==eligible,"cached source domain eligibility");
     if(domain!=null){
      Check(Get(binding,"SourceIdentity")!=null,"missing cached source identity");
      Check((float)Get(domain,"MinU")==.32763671875f&&(float)Get(domain,"MaxU")==.6650390625f,"source band bounds");
      var contains=domain.GetType().GetMethod("Contains",F);
      foreach(var uv in new[]{new Vector2(.6f,-3),new Vector2(.6650390625f,8)})Check((bool)contains.Invoke(domain,new object[]{uv}),"full source domain clipped to receiver");
      foreach(var uv in new[]{new Vector2(.28f,0),new Vector2(.666f,0),new Vector2(.5f,float.NaN)})Check(!(bool)contains.Invoke(domain,new object[]{uv}),"source domain overreach");
      Check(!(bool)domain.GetType().GetProperty("RepeatU",F).GetValue(domain)&&(bool)domain.GetType().GetProperty("RepeatV",F).GetValue(domain),"source sampler modes");
      sourceDomainChecks++;
     }
    }
    captureTriangles++;
   }
  }
  captureCalls++;call[3]=observed[3];return after;
 }
 [Serializable] public class BindingDumpRoot { public string scope, candidate, sourceDescriptor, sourceFixtures; public BindingDump[] bindings; }
 [Serializable] public class BindingDump { public string scenario, route, receiverType, projection, sourceSurface, surface, sourcePbr, outputPbr; public bool measuredBase, selectedInput; public uint domain; public float compatibleScale; public float[] sourceFrame,U,V; public Case receiver, sourceReceiver; public PatchDump patch, inputPatch; public string inputSupport; }
 [Serializable] public class PatchDump { public float[] min,max,finiteMin,finiteMax,U,V,tangent,sourceTangent; public string surface; public bool project,repeat,sourceUv,sourceAtlasRepeat,preparedUnit,repeatU,repeatV,projectTangent,bandRows; }
 [Serializable] public class Root {public Case[] cases;}
 [Serializable] public class Case {public string style,supported;public int occurrence,sourceTriangle;public Corner[] vertices;}
 [Serializable] public class Corner {public float[] P,N,T,UV,Color;}
 public static void Run(){try {
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
 var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll")); Type D(string n)=>db.GetType("Colossal.IO.AssetDatabase."+n,true);
 var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
 var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);var readHash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
 var hash=core.GetType("Colossal.Hash128",true);object Hash(string s)=>hash.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{s});
 // Read real native serialized descriptors, with identity-only TextureAsset handles.
 // No shader rendering or game database registration is asserted by this fixture.
 object Surface(string path,string cid,bool builtin){
 var o=FormatterServices.GetUninitializedObject(D("SurfaceAsset"));
 o.GetType().GetProperty("id").SetValue(o,New(D("Identifier"),Hash(cid)));
 if(builtin){var generic=D("AssetDatabase`1");var game=D("Game");var fixtureDb=FormatterServices.GetUninitializedObject(generic.MakeGenericType(game));var providerType=D("FileSystemDataSource");var fixtureProvider=New(providerType,"route-fixture",Path.Combine(Path.GetTempPath(),"BridgeBuilder-RouteFixture"),null,0L);Set(fixtureDb,"m_DataSource",fixtureProvider);o.GetType().GetProperty("database").SetValue(o,fixtureDb);}
 using(var r=new BinaryReader(File.OpenRead(path))){o.GetType().GetMethod("ReadHeader",F).Invoke(o,new object[]{r});
 foreach(var pair in new[]{("m_Floats","ReadStringFloatDictionary"),("m_Ints","ReadStringIntDictionary"),("m_Vectors","ReadStringVectorDictionary"),("m_Colors","ReadStringColorDictionary")})Set(o,pair.Item1,binary.GetMethod(pair.Item2).Invoke(null,new object[]{r}));
 Check(r.ReadByte()==255,"native texture map missing");var dict=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string),D("TextureAsset")));
 for(int n=r.ReadInt32();n>0;n--){string k=r.ReadString();byte present=r.ReadByte();object texture=null;if(present==255){texture=FormatterServices.GetUninitializedObject(D("TextureAsset"));texture.GetType().GetProperty("id").SetValue(texture,New(D("Identifier"),readHash.Invoke(null,new object[]{r})));}else Check(present==0,"texture marker");dict.Add(k,texture);}
 Set(o,"m_Textures",dict);Set(o,"m_Keywords",binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{r}));Check(r.BaseStream.Position==r.BaseStream.Length,"native descriptor trailing data");}
 Set(o,"m_InstanceRefCount",1);return o;}
 var original=Surface(Arg("-sourceSurface"),"1d7dad9f64c9fe55a809f1080c39fb92",true);
 var targetPath=Arg("-surfaceFile");var target=Surface(targetPath,File.ReadAllText(targetPath+".cid").Trim(),false);
 Check((int)target.GetType().GetProperty("materialTemplateHash").GetValue(target)==0,"target not Default");
 Check(((IEnumerable)Get(target,"m_Keywords")).Cast<string>().Contains("_TANGENTSPACE_OCTO"),"packed tangent keyword absent");
 var vectors=(IDictionary)Get(target,"m_Vectors");foreach(DictionaryEntry entry in vectors)Debug.Log("TARGET vector "+entry.Key+"="+entry.Value);
 var floats=(IDictionary)Get(original,"m_Floats");Debug.Log("SOURCE descriptor has native default scale; actual game candidate21 captured default .4"); floats["_WorldspaceUVScale"]=.4f;
 var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));Set(owner,"<Asset>k__BackingField",target);Set(owner,"<StyleId>k__BackingField","Lift01");
 var binding=T("MovableCompatibleRoad").GetMethod("Read",S).Invoke(null,new[]{original,owner});Check(binding!=null,"actual descriptor family did not resolve");
 var kind=T("MovableRoadAppearance+Type");var asphalt=Enum.Parse(kind,"Asphalt");var none=Enum.Parse(kind,"None");
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var input=T("MovableSurfaceAtlas+Input");var patch=T("MovableExistingRoadSurface+Patch");
 Vector4 V4(float[] v)=>new Vector4(v[0],v[1],v[2],v[3]);
 object V(Corner c)=>New(vertex,new Vector3(c.P[0],c.P[1],c.P[2]),new Vector3(c.N[0],c.N[1],c.N[2]),V4(c.T),V4(c.UV),new Color(c.Color[0],c.Color[1],c.Color[2],c.Color[3]));
 var dumps=new System.Collections.Generic.List<BindingDump>();
 string Cid(object asset)=>asset==null?null:Get(asset.GetType().GetProperty("id").GetValue(asset),"guid").ToString();
 float[] Floats(Vector4 v)=>new[]{v.x,v.y,v.z,v.w};
 float[] Matrix(Matrix4x4 m)=>Enumerable.Range(0,16).Select(i=>m[i/4,i%4]).ToArray();
 Case TriangleDump(object tri){Corner CornerDump(object corner){var p=(Vector3)Get(corner,"Position");var n=(Vector3)Get(corner,"Normal");var color=(Color)Get(corner,"Color");return new Corner{P=new[]{p.x,p.y,p.z},N=new[]{n.x,n.y,n.z},T=Floats((Vector4)Get(corner,"Tangent")),UV=Floats((Vector4)Get(corner,"UV")),Color=new[]{color.r,color.g,color.b,color.a}};}
 return new Case{occurrence=(int)Get(tri,"Occurrence"),sourceTriangle=(int)Get(tri,"SourceTriangle"),vertices=new[]{CornerDump(Get(tri,"A")),CornerDump(Get(tri,"B")),CornerDump(Get(tri,"C"))}};}
 PatchDump PatchDumpOf(object p){float[] V2(string n){var v=(Vector2)Get(p,n);return new[]{v.x,v.y};}bool B(string n)=>(bool)Get(p,n);return new PatchDump{min=V2("Min"),max=V2("Max"),finiteMin=V2("FiniteSamplingMin"),finiteMax=V2("FiniteSamplingMax"),U=Floats((Vector4)Get(p,"U")),V=Floats((Vector4)Get(p,"V")),surface=Cid(Get(p,"Surface")),project=B("Project"),repeat=B("Repeat"),sourceUv=B("SourceUv"),sourceAtlasRepeat=B("SourceAtlasRepeat"),preparedUnit=B("PreparedUnit"),repeatU=B("RepeatU"),repeatV=B("RepeatV"),projectTangent=B("ProjectTangent"),bandRows=B("BandRows"),tangent=Get(p,"Tangent") is Vector4 t?Floats(t):null,sourceTangent=Get(p,"SourceTangent") is Matrix4x4 m?Matrix(m):null};}
 string Pbr(object asset){var textures=Get(asset,"m_Textures") as IDictionary;return textures==null?"not loaded":string.Join(";",textures.Keys.Cast<object>().Select(x=>x+"="+Cid(textures[x])).OrderBy(x=>x));}
 void Dump(string scenario,object inputPatch,string support){if(Array.IndexOf(args,"-captureOutput")<0)return;foreach(var b in Captured[0])dumps.Add(new BindingDump{scenario=scenario,route=Get(b,"Route").ToString(),receiverType=Get(b,"ReceiverType").ToString(),projection=Get(b,"ProjectionKind").ToString(),sourceSurface=Cid(Get(b,"SourceSurface")),surface=Cid(Get(b,"Surface")),sourcePbr=Pbr(Get(b,"SourceSurface")),outputPbr=Pbr(Get(b,"Surface")),measuredBase=(bool)Get(b,"MeasuredBase"),selectedInput=(bool)Get(b,"SelectedInput"),domain=(uint)Get(b,"AppearanceDomain"),compatibleScale=(float)Get(b,"CompatibleScale"),sourceFrame=Matrix((Matrix4x4)Get(b,"SourceFrame")),U=Floats((Vector4)Get(b,"U")),V=Floats((Vector4)Get(b,"V")),receiver=TriangleDump(Get(b,"Receiver")),sourceReceiver=TriangleDump(Get(b,"SourceReceiver")),patch=PatchDumpOf(Get(b,"Patch")),inputPatch=PatchDumpOf(inputPatch),inputSupport=support});}
 var cases=JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-fixtures"))).cases;int count=0;
 var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils");
 var pack=math.GetMethod("TangentToOctahedral",S);var f4=pack.GetParameters()[0].ParameterType;
 object Pack(Vector4 t)=>pack.Invoke(null,new[]{Activator.CreateInstance(f4,new object[]{t.x,t.y,t.z,t.w})});
 var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);var compose=T("MovableSurfaceAtlas").GetMethod("Compose",S);
 var projection=T("MovableCompatibleRoad").GetMethod("Project",S);var accepts=binding.GetType().GetMethod("Accepts",F);
 foreach(var c in cases)foreach(float angle in new[]{0f,31f,143f}){
 var vertices=c.vertices.Select(V).ToArray();var face=New(triangle,vertices[0],vertices[1],vertices[2],0,c.occurrence,c.sourceTriangle);
 Check((bool)accepts.Invoke(binding,new[]{face,Enum.Parse(kind,c.supported)}),"actual receiver rejected "+c.style);
 Check(!(bool)accepts.Invoke(binding,new[]{face,asphalt}),"supported prototype stolen");
 var frame=Matrix4x4.TRS(new Vector3(17,-4,31),Quaternion.Euler(0,angle,0),Vector3.one);
 var value=New(input,true);Set(value,"Faces",Arr(bound,New(bound,face,0u)));Set(value,"Materials",Arr(D("SurfaceAsset"),original));Set(value,"Patches",Array.CreateInstance(patch,1));Set(value,"SupportedTypes",Arr(kind,Enum.Parse(kind,c.supported)));Set(value,"SourceFrame",frame);
 object[] call={Arr(input,value),owner,paint,null};var result=(Array)InvokeCompose(compose,call);var faces=(Array)result.GetValue(0);Check(faces.Length==1,"replacement cut or removed face");var actual=faces.GetValue(0);var materials=(Array)((Array)call[3]).GetValue(0);Check(ReferenceEquals(materials.GetValue((int)Get(actual,"Material")),target),"wrong output material slot");
 Check(Get(actual,"Occurrence").Equals(Get(face,"Occurrence"))&&Get(actual,"SourceTriangle").Equals(Get(face,"SourceTriangle")),"bone rebind provenance lost");
 int i=0;foreach(string corner in new[]{"A","B","C"}){var a=vertices[i++];var b=Get(actual,corner);foreach(string attr in new[]{"Position","Normal","Color"})Check(Get(a,attr).Equals(Get(b,attr)),"changed "+attr);
 var p=(Vector3)Get(a,"Position");var projected=frame.MultiplyPoint3x4(p);var uv=(Vector4)Get(b,"UV");var old=(Vector4)Get(a,"UV");Check(Math.Abs(uv.x-projected.x*.4f)<.00002f&&Math.Abs(uv.y-projected.z*.4f)<.00002f,"density/phase mismatch");Check(uv.z==old.z&&uv.w==old.w,"UVzw lost");
 var expected=projection.Invoke(null,new object[]{a,frame,.4f});Check(Get(expected,"Tangent").Equals(Get(b,"Tangent")),"tangent mismatch");
 var tangent=(Vector4)Get(b,"Tangent");var normal=(Vector3)Get(b,"Normal");var u=(Vector3)frame.GetRow(0);var v=(Vector3)frame.GetRow(2);
 var determinant=Vector3.Dot(u,Vector3.Cross(v,normal));
 if(determinant!=0){Check(Math.Abs(Vector3.Dot(normal,(Vector3)tangent))<.00001f,"T not orthogonal to authored N");Check(Vector3.Dot(u,(Vector3)tangent)>0,"T opposite U");Check(tangent.w==Math.Sign(determinant),"handedness mismatch");}
 Check(Pack(tangent).Equals(Pack((Vector4)Get(expected,"Tangent"))),"native packed tangent differs");count++;}
 }
 foreach(var n in new[]{Vector3.up,Vector3.down,new Vector3(0,1,1).normalized,Vector3.right}) {
 var authored=new Vector4(0,0,1,1);var a=New(vertex,new Vector3(3,4,5),n,authored,new Vector4(0,0,7,9),Color.gray);
 var b=projection.Invoke(null,new object[]{a,Matrix4x4.identity,.4f});var t=(Vector4)Get(b,"Tangent");
 Check(Get(b,"Normal").Equals(n),"replacement changed normal");if(n==Vector3.right)Check(t==authored,"singular XZ discarded authored T");else Check(Math.Abs(Vector3.Dot(n,(Vector3)t))<.00001f,"tilted/down normal frame incorrect");Pack(t);
 }
 // Gate negatives cannot become asphalt just because they use a network template.
 var sample=cases[0];var corners=sample.vertices.Select(V).ToArray();var badUv=V4(sample.vertices[0].UV);badUv.x=.5f;var oldCorner=corners[0];corners[0]=New(vertex,Get(oldCorner,"Position"),Get(oldCorner,"Normal"),Get(oldCorner,"Tangent"),badUv,Get(oldCorner,"Color"));var badFace=New(triangle,corners[0],corners[1],corners[2],0,0,0);Check(!(bool)accepts.Invoke(binding,new[]{badFace,none}),"curb/partial footprint admitted");
 var sourceTextures=(IDictionary)Get(original,"m_Textures");var saved=sourceTextures["_WorldspaceNormalMap"];sourceTextures["_WorldspaceNormalMap"]=sourceTextures["_NormalMap"];Check(T("MovableCompatibleRoad").GetMethod("Read",S).Invoke(null,new[]{original,owner})==null,"different PBR family admitted");sourceTextures["_WorldspaceNormalMap"]=saved;
 foreach(string style in cases.Select(c=>c.style).Distinct()) {
 var path=Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(targetPath).FullName).FullName).FullName,style,"Road","NativeAsphalt.Surface");
 var objectSurface=Surface(path,File.ReadAllText(path+".cid").Trim(),false);Set(owner,"<Asset>k__BackingField",objectSurface);
 Check(T("MovableCompatibleRoad").GetMethod("Read",S).Invoke(null,new[]{original,owner})!=null,"target graph family mismatch "+style);
 }
 // Pure route negatives: source family alone cannot grant a prototype patch.
 var route=T("MovableRoadRoute");var sourceType=T("MovableRoadRoute+Source");
 var routeSource=New(sourceType,"1d7dad9f64c9fe55a809f1080c39fb92",true,"9a6506aaede935752b5c4c0362c95f6e","a142c18b42577de52815d1afd054660b","37ceea0e8c2bd24599e237236e6ae338",asphalt);
 var select=route.GetMethod("Select",S);
 if(args.Contains("-curbFixtures")) {
 var curbKind=Enum.Parse(kind,"Curb");int tested=0;
 foreach(var measured in JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-curbFixtures"))).cases){
 var cv=measured.vertices.Select(V).ToArray();var cf=New(triangle,cv[0],cv[1],cv[2],0,measured.occurrence,measured.sourceTriangle);
 Check(sourceType.GetMethod("Receiver",F).Invoke(routeSource,new[]{cf}).Equals(curbKind),"actual curb footprint classification");
 Check(select.Invoke(null,new[]{routeSource,cf,curbKind,(object)true,false}).ToString()=="Prototype","corresponding measured curb part not selected");
 foreach(var unmatched in new[]{none,Enum.Parse(kind,"Sidewalk"),asphalt}) Check(select.Invoke(null,new[]{routeSource,cf,unmatched,(object)true,false}).ToString()=="Original","curb escaped actual target part support");
 tested++;
 }
 Debug.Log("PASS actual saved curb footprint routing: "+tested+" faces across all LODs; explicit Curb target positive; None/Sidewalk/Asphalt targets Original. Position/UV actual; normal/tangent/color controlled, not renderer acceptance.");
 }
 var good=cases[0].vertices.Select(V).ToArray();var goodFace=New(triangle,good[0],good[1],good[2],0,0,0);
 Check(select.Invoke(null,new[]{routeSource,goodFace,asphalt,(object)true,true}).ToString()=="Prototype","prototype priority");
 Check(select.Invoke(null,new[]{routeSource,badFace,asphalt,(object)true,true}).ToString()=="Original","partial domain wrongly matched");
 Check(select.Invoke(null,new[]{routeSource,goodFace,asphalt,(object)false,false}).ToString()=="Original","missing map not Original");
 // Unknown original faces short-circuit Compose before any GPU/period access.
 var unchanged=New(input,true);Set(unchanged,"Faces",Arr(bound,New(bound,badFace,99u)));Set(unchanged,"Materials",Arr(D("SurfaceAsset"),original));
 object[] originalCall={Arr(input,unchanged),owner,paint,null};var preserved=(Array)((Array)InvokeCompose(compose,originalCall)).GetValue(0);
 Check(preserved.Length==1,"unknown original topology changed");foreach(string c in new[]{"A","B","C"})Check(Get(preserved.GetValue(0),c).Equals(Get(badFace,c)),"unknown source attributes changed");
 Check(ReferenceEquals(((Array)((Array)originalCall[3]).GetValue(0)).GetValue(0),original),"unknown source identity changed");
 var baseTarget=Surface(Arg("-baseSurface"),"11111111111111111111111111111111",false);
 var baseCache=Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(D("SurfaceAsset"),D("SurfaceAsset")));
 ((IDictionary)baseCache).Add(original,baseTarget);Set(owner,"_originalBase",baseCache);
 object C(Vector3 p,Vector4 uv)=>New(vertex,p,Vector3.up,new Vector4(1,0,0,1),uv,Color.white);
 var curb=New(triangle,C(new Vector3(-10.6000061f,.0000162124634f,-78.75f),new Vector4(.281738281f,.3515625f,7,9)),C(new Vector3(-10.6000061f,.0000162124634f,-78.15039f),new Vector4(.281738281f,.3703003f,7,9)),C(new Vector3(-10.5893459f,.0000162124634f,-78.15039f),new Vector4(.282519f,.3703003f,7,9)),0,1,12);
 Check((bool)sourceType.GetMethod("IsMeasuredBase",F).Invoke(routeSource,new[]{curb}),"actual domain19 base not recognized");
 var curbInput=New(input,true);Set(curbInput,"Faces",Arr(bound,New(bound,curb,19u)));Set(curbInput,"Materials",Arr(D("SurfaceAsset"),original));
 object[] curbCall={Arr(input,curbInput),owner,paint,null};var curbResult=(Array)((Array)InvokeCompose(compose,curbCall)).GetValue(0);var curbOut=curbResult.GetValue(0);
 foreach(string c in new[]{"A","B","C"})Check(Get(curbOut,c).Equals(Get(curb,c)),"base route altered source attrs");
 Check(ReferenceEquals(((Array)((Array)curbCall[3]).GetValue(0)).GetValue((int)Get(curbOut,"Material")),baseTarget),"base route material identity");
 var originalTextures=(IDictionary)Get(original,"m_Textures");var baseTextures=(IDictionary)Get(baseTarget,"m_Textures");foreach(string k in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})Check(originalTextures[k].GetType().GetProperty("id").GetValue(originalTextures[k]).Equals(baseTextures[k].GetType().GetProperty("id").GetValue(baseTextures[k])),"base PBR changed");
 Debug.Log("PASS reported domain19 source-base Compose: real PBR descriptor read, all vertex attrs/UV4/provenance preserved; material realization injected, native runtime save and game rendering not asserted.");
 var nonselected=New(input,false);Set(nonselected,"Faces",Arr(bound,New(bound,badFace,99u)));Set(nonselected,"Materials",Arr(D("SurfaceAsset"),original));object[] nc={Arr(input,nonselected),owner,paint,null};Check(((Array)((Array)InvokeCompose(compose,nc)).GetValue(0)).Length==1,"prototype missing map threw"); Debug.Log("PASS total route: prototype priority, missing optional map, unknown domain99/empty maps and partial footprint Original; actual Compose bypass without GPU/period execution.");
 // A real sidewalk's base-atlas class differs from its asphalt worldspace layer.
 // This exercises the entire selected-input Compose route, including its second gate.
 if(Array.IndexOf(args,"-sidewalkFixtures")>=0){
 var paving=Enum.Parse(kind,"Sidewalk");var prototypePath=Arg("-prototypeSurface");
 var prototype=Surface(prototypePath,File.ReadAllText(prototypePath+".cid").Trim(),false);
 Set(owner,"<AsphaltTexture>k__BackingField",sourceTextures["_WorldspaceAlbedo"]);
 var constructor=patch.GetConstructors(F).Single(c=>c.GetParameters().Length==15);
 var pa=constructor.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:null).ToArray();
 pa[0]=Vector2.zero;pa[1]=Vector2.one;pa[2]=new Vector4(.125f,0,0,0);pa[3]=new Vector4(0,0,.25f,0);pa[4]=prototype;pa[9]=true;pa[10]=true;pa[11]=true;
 var prototypePatch=constructor.Invoke(pa);int checkedFaces=0;
 // Controlled collapsed geometry with the real selected-road descriptor: the
 // established prototype correspondence must survive without a plane or GPU bake.
 foreach(bool sourceUv in new[]{false,true}) foreach(bool point in new[]{false,true}) {
 var cv=new[]{C(Vector3.zero,new Vector4(.1f,.2f,7,9)),C(point?Vector3.zero:Vector3.right,new Vector4(.1f,.3f,7,9)),C(point?Vector3.zero:Vector3.right*2,new Vector4(.1f,.4f,7,9))};
 var cf=New(triangle,cv[0],cv[1],cv[2],0,2,75);
 var cp=prototypePatch;if(sourceUv){var sp=(object[])pa.Clone();sp[4]=null;sp[8]=true;sp[9]=false;cp=constructor.Invoke(sp);}
 var ci=New(input,true);Set(ci,"Faces",Arr(bound,New(bound,cf,0u)));Set(ci,"Materials",Arr(D("SurfaceAsset"),original));Set(ci,"Patches",Arr(patch,cp));Set(ci,"SupportedTypes",Arr(kind,asphalt));Set(ci,"Native",prototype);
 if(sourceUv && input.GetField("DomainNatives",F)!=null){Set(ci,"Native",target);Set(ci,"DomainNatives",Arr(D("SurfaceAsset"),prototype));}
 object[] cc={Arr(input,ci),owner,paint,null};var cr=(Array)((Array)InvokeCompose(compose,cc)).GetValue(0);
 Check(cr.Length==1,"collapsed matched receiver was removed");var ct=cr.GetValue(0);
 Check(ReferenceEquals(((Array)((Array)cc[3]).GetValue(0)).GetValue((int)Get(ct,"Material")),prototype),"collapsed prototype reverted to network material");
 foreach(string corner in new[]{"A","B","C"})foreach(string attr in new[]{"Position","Normal","Color"})Check(Get(Get(cf,corner),attr).Equals(Get(Get(ct,corner),attr)),"collapsed correspondence changed "+attr);
 Check(Get(ct,"Occurrence").Equals(2)&&Get(ct,"SourceTriangle").Equals(75),"collapsed provenance lost");
 // Include unused native-network and null slots. Only the active object slot
 // reaches the native model, without modifying positions, topology or bones.
 var sparseTriangle=New(triangle,Get(ct,"A"),Get(ct,"B"),Get(ct,"C"),2,2,75);
 var materialMeshes=T("MovableRoadAssets").GetMethod("BuildMaterialMeshes",S);
 var modelType=materialMeshes.ReturnType.GetElementType();
 var owned=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(modelType));
 object[] meshCall={"route75",Arr(bound,New(bound,sparseTriangle,1u)),Arr(D("SurfaceAsset"),original,null,prototype),2,owned,null};
 try {
 var models=(Array)materialMeshes.Invoke(null,meshCall);Check(models.Length==1,"unused slot emitted a mesh");
 Check(((Array)meshCall[5]).Length==1&&ReferenceEquals(((Array)meshCall[5]).GetValue(0),prototype),"unused network slot leaked to render graph");
 var model=models.GetValue(0);var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();
 var mesh=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
 try {Check(mesh.vertexCount==3&&mesh.GetIndices(0).Length==3,"collapsed native geometry removed");var p=mesh.vertices;for(int i=0;i<3;i++)Check(p[i].Equals(Get(cv[i],"Position")),"native collapsed position changed");}
 finally {UnityEngine.Object.DestroyImmediate(mesh);}
 } finally {foreach(var model in owned)((IDisposable)model).Dispose();}
 }
 Debug.Log("PASS collapsed line/point selected-road prototype Compose: real native descriptor, matched object surface, topology/position/normal/color/provenance retained; no GPU bake.");
 Debug.Log("PASS per-domain Native for null-Surface SourceUv: actual selected source uses its family's prototype, not the different Input.Native fallback.");
 foreach(var measured in JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-sidewalkFixtures"))).cases){
 var vs=measured.vertices.Select(V).ToArray();var face=New(triangle,vs[0],vs[1],vs[2],0,measured.occurrence,measured.sourceTriangle);
 Check(sourceType.GetMethod("Receiver",F).Invoke(routeSource,new[]{face}).Equals(paving),"measured native sidewalk not classified");
 Check(select.Invoke(null,new[]{routeSource,face,paving,(object)true,false}).ToString()=="Prototype","sidewalk did not select prototype");
 Check(select.Invoke(null,new[]{routeSource,face,asphalt,(object)true,false}).ToString()=="Original","sidewalk stolen by asphalt prototype");
 var si=New(input,true);Set(si,"Faces",Arr(bound,New(bound,face,0u)));Set(si,"Materials",Arr(D("SurfaceAsset"),original));Set(si,"Patches",Arr(patch,prototypePatch));Set(si,"SupportedTypes",Arr(kind,paving));Set(si,"Native",prototype);
 object[] sc={Arr(input,si),owner,paint,null};var result=(Array)((Array)InvokeCompose(compose,sc)).GetValue(0);Check(result.Length==1,"sidewalk topology changed");var actual=result.GetValue(0);
 Dump("controlled-Sidewalk-prototype-repeat",prototypePatch,"Sidewalk");
 Check(ReferenceEquals(((Array)((Array)sc[3]).GetValue(0)).GetValue((int)Get(actual,"Material")),prototype),"asphalt world layer vetoed measured paving prototype");
 foreach(string c in new[]{"A","B","C"}){var before=Get(face,c);var after=Get(actual,c);foreach(string attr in new[]{"Position","Normal","Color"})Check(Get(before,attr).Equals(Get(after,attr)),"sidewalk changed "+attr);var uv=(Vector4)Get(after,"UV");var old=(Vector4)Get(before,"UV");Check(uv.z==old.z&&uv.w==old.w,"sidewalk lost UVzw");}
 if(checkedFaces==0)foreach(var noMatchType in new[]{none,asphalt}){
 Set(si,"SupportedTypes",Arr(kind,noMatchType));object[] missing={Arr(input,si),owner,paint,null};var retained=(Array)((Array)InvokeCompose(compose,missing)).GetValue(0);
 Check(retained.Length==1,"unmatched target part removed/cut a face");var retainedFace=retained.GetValue(0);
 Check(ReferenceEquals(((Array)((Array)missing[3]).GetValue(0)).GetValue((int)Get(retainedFace,"Material")),baseTarget),"valid patch with missing/wrong target correspondence stole paving");
 foreach(string c in new[]{"A","B","C"})Check(Get(retainedFace,c).Equals(Get(face,c)),"unmatched target changed original source attrs");
 }
 checkedFaces++;
 }
 if(Array.IndexOf(args,"-captureOutput")>=0){
 string bedPath=Arg("-lift02Bed");var bed=Surface(bedPath,File.ReadAllText(bedPath+".cid").Trim(),false);
 var appearance=T("Lift02BedAppearance").GetMethod("Prepared",S).Invoke(null,new object[]{bed,Pose.identity,10.25f});
 var actualPatches=(Array)Get(appearance,"Patches");var actualSupport=(Array)Get(appearance,"SupportedTypes");
 Set(owner,"<StyleId>k__BackingField","Lift02");
 foreach(var measured in JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-sidewalkFixtures"))).cases)for(int domain=0;domain<actualPatches.Length;domain++){
 var vs=measured.vertices.Select(V).ToArray();var face=New(triangle,vs[0],vs[1],vs[2],0,measured.occurrence,measured.sourceTriangle);
 var li=New(input,true);Set(li,"Faces",Arr(bound,New(bound,face,(uint)domain)));Set(li,"Materials",Arr(D("SurfaceAsset"),original));Set(li,"Patches",actualPatches);Set(li,"SupportedTypes",actualSupport);Set(li,"Native",bed);
 object[] lc={Arr(input,li),owner,paint,null};InvokeCompose(compose,lc);
 Check(Get(Captured[0][0],"Route").ToString()=="Original","Lift02 unexpectedly claimed prototype sidewalk");
 Dump("Lift02-production-Prepared-domains-controlled-assignment",actualPatches.GetValue(domain),actualSupport.GetValue(domain).ToString());
 }
 var export=new BindingDumpRoot{candidate=Arg("-modAssembly"),sourceDescriptor=Arg("-sourceSurface"),sourceFixtures=Arg("-sidewalkFixtures"),scope="64 saved native source P/UV triangles; N/T/color and UVzw controlled. First scenario uses a controlled Sidewalk prototype patch. Lift02 scenario invokes actual Lift02BedAppearance.Prepared at identity pose, halfDelta10.25 and tests each of its three actual domains independently; domain assignment is controlled, not game ECS or complete elevated-road generation. Original is selected by actual source/target routing, never forced. Texture handles identify native descriptor bindings; no GPU render.",bindings=dumps.ToArray()};
 File.WriteAllText(Arg("-captureOutput"),JsonUtility.ToJson(export,true));Debug.Log("PASS exported sidewalk captures "+dumps.Count+" to "+Arg("-captureOutput"));
 Set(owner,"<StyleId>k__BackingField","Lift01");
 }
 Debug.Log("PASS actual native sidewalk top fixtures selected-input Compose: "+checkedFaces+" faces; exact source six-channel identity; prototype material slot, no GPU/period bake, raw attributes and topology retained. Header handles only; game appearance remains manual.");
 }
 // A measured object source must not bypass a matching target part merely because it is already skinned-compatible.
 {
 var oc=patch.GetConstructors(F).Single(c=>c.GetParameters().Length==15);var oa=oc.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:null).ToArray();
 oa[0]=Vector2.zero;oa[1]=Vector2.one;oa[2]=new Vector4(.17f,0,0,.31f);oa[3]=new Vector4(0,0,.23f,.41f);oa[4]=target;oa[9]=true;oa[10]=true;oa[11]=true;
 var op=oc.Invoke(oa);var of=New(triangle,C(new Vector3(0,0,0),new Vector4(.2f,.3f,7,9)),C(new Vector3(0,0,1),new Vector4(.2f,.5f,7,9)),C(new Vector3(1,0,0),new Vector4(.4f,.3f,7,9)),0,4,7);
 var oi=New(input,true);Set(oi,"Faces",Arr(bound,New(bound,of,0u)));Set(oi,"Materials",Arr(D("SurfaceAsset"),target));Set(oi,"Patches",Arr(patch,op));Set(oi,"SupportedTypes",Arr(kind,asphalt));Set(oi,"Native",target);
 object[] known={Arr(input,oi),owner,paint,null};var mapped=((Array)((Array)InvokeCompose(compose,known)).GetValue(0)).GetValue(0);
 Check(((Vector4)Get(Get(mapped,"A"),"UV")).x==.31f,"known object bypassed prototype mapping");
 if(compose.GetParameters().Length>4){
 var finiteArgs=(object[])oa.Clone();finiteArgs[10]=false;finiteArgs[11]=true;
 var finitePatch=oc.Invoke(finiteArgs);Set(oi,"Patches",Arr(patch,finitePatch));
 object[] finiteCall={Arr(input,oi),owner,paint,null};InvokeCompose(compose,finiteCall);
 var capturedPatch=Get(Captured[0][0],"Patch");
 Check(!(bool)Get(capturedPatch,"RepeatU")&&(bool)Get(capturedPatch,"RepeatV"),"capture changed finite/repeat axes");
 Check(Get(capturedPatch,"FiniteSamplingMin").Equals(Vector2.zero)&&Get(capturedPatch,"FiniteSamplingMax").Equals(Vector2.one),"capture lost finite guard");
 Set(oi,"Patches",Arr(patch,op));
 var empty=New(input,false);object[] emptyCall={Arr(input,empty),owner,paint,null};InvokeCompose(compose,emptyCall);
 Check(Captured.Count==1&&Captured[0].Length==0,"empty LOD capture absent");
 Debug.Log("PASS capture finite U/repeating V and finite sampling bounds; empty LOD callback retained.");
 }
 var unknown=Surface(Arg("-surfaceFile"),"11111111111111111111111111111111",false);Set(oi,"Materials",Arr(D("SurfaceAsset"),unknown));object[] un={Arr(input,oi),owner,paint,null};var retained=((Array)((Array)InvokeCompose(compose,un)).GetValue(0)).GetValue(0);
 foreach(string c in new[]{"A","B","C"})Check(Get(retained,c).Equals(Get(of,c)),"unknown object overwritten");
 Check(ReferenceEquals(((Array)((Array)un[3]).GetValue(0)).GetValue((int)Get(retained,"Material")),unknown),"unknown object identity replaced");
 Debug.Log("PASS measured native object Asphalt routes to its matching prototype patch; unknown same-template/PBR object remains exact Original. Controlled triangle and native serialized descriptor, not a Pathway object claim.");
 }
 if(Array.IndexOf(args,"-ballastSource")>=0){
 var gravel=Surface(Arg("-ballastSource"),"a3522ead5e61e02569f0884fb4607eec",true);
 ((IDictionary)Get(gravel,"m_Floats"))["_WorldspaceUVScale"]=.4f;
 string ballastPath=Arg("-ballastObject");var ballastObject=Surface(ballastPath,File.ReadAllText(ballastPath+".cid").Trim(),false);
 var catalog=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);
 catalog.GetField("_root",S).SetValue(null,Path.GetDirectoryName(ballastPath));
 ((IDictionary)catalog.GetField("Surfaces",S).GetValue(null))["Lift02/NativeSources/3aaa84602734568f9f0909f01c8527f0/Object.Surface"]=ballastObject;
 Set(owner,"<StyleId>k__BackingField","Lift02");var ballast=Enum.Parse(kind,"Ballast");
 var gb=T("MovableCompatibleRoad").GetMethod("Read",S).Invoke(null,new[]{gravel,owner});Check(gb!=null,"measured gravel object unavailable");
 var gt=(IDictionary)Get(gravel,"m_Textures");var bt=(IDictionary)Get(ballastObject,"m_Textures");
 foreach(var pair in new[]{("_WorldspaceAlbedo","_BaseColorMap"),("_WorldspaceNormalMap","_NormalMap"),("_WorldspaceMaskMap","_MaskMap")})Check(gt[pair.Item1].GetType().GetProperty("id").GetValue(gt[pair.Item1]).Equals(bt[pair.Item2].GetType().GetProperty("id").GetValue(bt[pair.Item2])),"Ballast PBR borrowed another category");
 int gc=0;foreach(float angle in new[]{0f,31f,143f})foreach(float length in new[]{4f,212f,1000f}){
 var f=New(triangle,C(new Vector3(-1.5f,0,0),new Vector4(.25f,0,7,9)),C(new Vector3(1.5f,0,0),new Vector4(.75f,0,7,9)),C(new Vector3(-1.5f,0,length),new Vector4(.25f,1,7,9)),0,5,17);
 Check((bool)accepts.Invoke(gb,new[]{f,none}),"Ballast no-prototype rejection");Check(!(bool)accepts.Invoke(gb,new[]{f,ballast}),"Ballast prototype priority stolen");
 var frame=Matrix4x4.TRS(new Vector3(17,-4,31),Quaternion.Euler(0,angle,0),Vector3.one);var gi=New(input,true);Set(gi,"Faces",Arr(bound,New(bound,f,0u)));Set(gi,"Materials",Arr(D("SurfaceAsset"),gravel));Set(gi,"SupportedTypes",Arr(kind,none));Set(gi,"SourceFrame",frame);
 object[] call={Arr(input,gi),owner,paint,null};var fs=(Array)((Array)InvokeCompose(compose,call)).GetValue(0);Check(fs.Length==1,"Ballast span bake/cut");var output=fs.GetValue(0);Check(ReferenceEquals(((Array)((Array)call[3]).GetValue(0)).GetValue((int)Get(output,"Material")),ballastObject),"Ballast output material slot");
 foreach(string corner in new[]{"A","B","C"}){var a=Get(f,corner);var b=Get(output,corner);foreach(string attr in new[]{"Position","Normal","Color"})Check(Get(a,attr).Equals(Get(b,attr)),"Ballast source attribute changed");var expect=projection.Invoke(null,new[]{a,(object)frame,.4f});Check(Get(b,"UV").Equals(Get(expect,"UV"))&&Get(b,"Tangent").Equals(Get(expect,"Tangent")),"Ballast physical density/frame mismatch");Pack((Vector4)Get(b,"Tangent"));gc++;}
 }
 Debug.Log("PASS corresponding Ballast: real native six-channel source and serialized object descriptor; 9 controlled pose/span inputs, "+gc+" vertices through Compose and native tangent codec, no new pixels/cuts. Controlled geometry, not actual selected-road GameDB generation.");
 }
 Check((int)Get(original,"m_InstanceRefCount")==1&&(int)Get(target,"m_InstanceRefCount")==1,"properties reference lease leaked");
 Debug.Log("PASS compatible road: actual 8 logged triangles x3 source poses, "+count+" vertices; real source/target descriptor readers, Compose direct material slots and provenance, density/phase/UVzw/attributes; supported and partial/family negative gates; no GPU/texture writer. Not actual game rendering.");Debug.Log("PASS optional appearance capture: calls="+captureCalls+" triangles="+captureTriangles+"; sourceDomainChecks="+sourceDomainChecks+"; null/capture results identical, per-output routes/surfaces/provenance retained.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
