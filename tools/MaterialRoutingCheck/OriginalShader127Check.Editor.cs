using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class OriginalShader127Check {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static Array Arr(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
 public static void Run(){try{
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
var descriptor=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
 var root=Arg("-outputRoot");Directory.CreateDirectory(root);var d=Activator.CreateInstance(descriptor,F,null,new object[]{root},null);var dbType=D("AssetDatabase`1").MakeGenericType(descriptor);var local=dbType.GetMethod("GetInstance",S).Invoke(null,new[]{d});
 object global;try{global=D("AssetDatabase").GetProperty("global",S).GetValue(null);}catch(TargetInvocationException){global=D("AssetDatabase").GetField("s_Instance",S).GetValue(null);Check(global!=null,"global bootstrap missing");}var resources=ScriptableObject.CreateInstance(D("AssetDatabaseResources"));var library=Activator.CreateInstance(D("MaterialLibrary"));var descriptions=D("MaterialLibrary").GetField("m_Materials",F);var list=(IList)Activator.CreateInstance(descriptions.FieldType);var descType=D("MaterialLibrary").GetNestedType("MaterialDescription");var md=Activator.CreateInstance(descType);descType.GetField("m_Hash",F).SetValue(md,0);var stacks=descType.GetField("m_Stacks",F);stacks.SetValue(md,Array.CreateInstance(stacks.FieldType.GetElementType(),0));var named=new Material(Shader.Find("Standard"));named.name="Default";descType.GetField("m_Material",F).SetValue(md,named);list.Add(md);
 void Template(int id, Shader shader) { var m=Activator.CreateInstance(descType); descType.GetField("m_Hash",F).SetValue(m,id);stacks.SetValue(m,Array.CreateInstance(stacks.FieldType.GetElementType(),0));descType.GetField("m_Material",F).SetValue(m,new Material(shader));list.Add(m); }
 Template(11,Shader.Find("Unlit/Color")); Template(17,Shader.Find("Unlit/Texture")); Template(991123,named.shader);
 descriptions.SetValue(library,list);resources.GetType().GetField("m_MaterialLibrary",F).SetValue(resources,library);D("AssetDatabase").GetField("m_Resources",F).SetValue(global,resources);


 var native=Surface(Arg("-sourceSurface"),"40dd255c63eb8f65f96c5333d7d28afb",true);
 var targetPath=Arg("-surfaceFile");var original=Surface(targetPath,File.ReadAllText(targetPath+".cid").Trim(),false);
 foreach(var sf in new[]{native,original})foreach(var tx in ((IDictionary)Get(sf,"m_Textures")).Values)if(tx!=null)tx.GetType().GetProperty("database").SetValue(tx,local);
 var lane=T("MovableLaneSurface").GetMethod("TryCreate",S);Check(lane!=null,"result API missing");
 var assetType=D("AssetData");object Owned()=>Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(assetType));
 int cases=0;
 foreach(string identity in new[]{"11111111232233334444555555555555","aaaaaaaa222233334444555555555555"}) {
 original.GetType().GetProperty("id").SetValue(original,New(D("Identifier"),Hash(identity)));
 var owned=Owned();object[] call={original,"tmp123","Draw02",null,owned,null,null};
 Check((bool)lane.Invoke(null,call) && ReferenceEquals(original,call[5]) && call[6]==null && ((IList)owned).Count==0,"unknown object identity must reuse complete source: "+call[6]);cases++;
 native.GetType().GetProperty("id").SetValue(native,New(D("Identifier"),Hash(identity)));
 call=new object[]{native,"tmp123","Draw02",null,Owned(),null,null};
 Check((bool)lane.Invoke(null,call) && ReferenceEquals(native,call[5]) && call[6]==null && ((IList)call[4]).Count==0,"original network descriptor replaced");cases++;
 }
 // A third-party template identity using the proven native shader protocol is
 // accepted without a template-ID/CID entry or descriptor reconstruction.
 var templateField=original.GetType().GetFields(F).Single(f=>f.Name=="m_MaterialTemplateHash");
 templateField.SetValue(original,991123);
 object[] custom={original,"tmp123","Draw02",null,Owned(),null,null};
 Check((bool)lane.Invoke(null,custom) && ReferenceEquals(original,custom[5]),"custom same-shader template must reuse exact descriptor");cases++;
 foreach(var keyword in new[]{"_GPU_ANIMATION_NORMAL","_GPU_ANIMATION_SHAPE","COLOSSAL_GEOMETRY_TILING","COLOSSAL_GEOMETRY_DILATED"}) {
 var keywords=(System.Collections.Generic.HashSet<string>)Get(original,"m_Keywords");keywords.Add(keyword);
 object[] incompatible={original,"tmp123","Draw02",null,Owned(),null,null};
 Check((bool)lane.Invoke(null,incompatible) && ReferenceEquals(original,incompatible[5]) && incompatible[6]==null && ((IList)incompatible[4]).Count==0 && keywords.Contains(keyword),"original keyword modified by experimental binding");
 keywords.Remove(keyword);cases++;
 }
 Check((int)Get(original,"m_InstanceRefCount")==1,"protocol inspection leaked native property leases");
 // Exact descriptor containers and references must survive all accepted source
 // identities and shader families. This is not a shader rendering assertion.
 var fields=new[]{"m_Floats","m_Ints","m_Vectors","m_Colors","m_Textures","m_Keywords"};
 var before=fields.Select(f=>Get(native,f)).ToArray();var nativeTemplate=native.GetType().GetField("m_MaterialTemplateHash",F);
 Template(881127,Shader.Find("Unlit/Texture"));descriptions.SetValue(library,list);
 foreach(int tid in new[]{17,881127}){nativeTemplate.SetValue(native,tid);object[] reuse={native,"tmp123","Draw02",null,Owned(),null,null};Check((bool)lane.Invoke(null,reuse)&&ReferenceEquals(native,reuse[5]),"arbitrary template did not reuse source");for(int j=0;j<fields.Length;j++)Check(ReferenceEquals(before[j],Get(native,fields[j])),"source property container replaced");Check((int)Get(native,"m_MaterialTemplateHash")==tid,"template replaced");cases++;}
 nativeTemplate.SetValue(native,17);
 var sourceTextures=(IDictionary)Get(original,"m_Textures");sourceTextures.Add("_OptionalUnboundFixture",null);
 object[] optionalCall={original,"tmp123","Draw02",null,Owned(),null,null};Check((bool)lane.Invoke(null,optionalCall)&&ReferenceEquals(original,optionalCall[5])&&sourceTextures.Contains("_OptionalUnboundFixture"),"unbound optional shader default rejected");cases++;sourceTextures.Remove("_OptionalUnboundFixture");
 sourceTextures.Add("_MissingFixture",FormatterServices.GetUninitializedObject(D("TextureAsset")));
 object[] dummyCall={original,"tmp123","Draw02",null,Owned(),null,null};Check(!(bool)lane.Invoke(null,dummyCall)&&dummyCall[5]==null&&dummyCall[6]!=null,"unresolved explicit texture accepted");cases++;sourceTextures.Remove("_MissingFixture");
 var vtOriginal=Surface(Arg("-vtSurface"),"11111111444433332222555555555555",false);
 Check((bool)Get(vtOriginal,"m_IsVTMaterial"),"fixture must be actual native VT header");
 var vtProvenance=(IDictionary)Get(vtOriginal,"m_Textures");vtProvenance.Add("_ProvenanceOnlyFixture",FormatterServices.GetUninitializedObject(D("TextureAsset")));
 object[] vtCall={vtOriginal,"tmp123","Draw02",null,Owned(),null,null};Check((bool)lane.Invoke(null,vtCall)&&ReferenceEquals(vtOriginal,vtCall[5])&&(int)Get(vtOriginal,"m_InstanceRefCount")==1,"VT provenance rejected or source lease disturbed");cases++;vtProvenance.Remove("_ProvenanceOnlyFixture");


 object[] roadSetup={"tmp123","Draw02",native,null,Owned(),null,null,null,null};
 Check((bool)T("MovableRoadSurface").GetMethod("TryCreate",S).Invoke(null,roadSetup)&&ReferenceEquals(native,roadSetup[5])&&((IList)roadSetup[4]).Count==0&&roadSetup[8]==null,"road setup substituted shader or allocated Default surface: "+roadSetup[8]);cases++;
 object[] missingSource={null,"tmp123","Draw02",null,Owned(),null,null};Check(!(bool)lane.Invoke(null,missingSource)&&missingSource[5]==null&&missingSource[6]!=null,"missing source accepted");cases++;

 // Preserve complete original descriptors and every vertex attribute through the
 // actual shared selected-road Compose entry, independently for three LOD inputs.
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var input=T("MovableSurfaceAtlas+Input");
 var owner=New(T("MovableRoadMaterial"),"tmp123","Draw02",null);
 var inputs=Array.CreateInstance(input,3);var faces=new object[3];
 for(int lod=0;lod<3;lod++){
 object V(float x,float z)=>New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,.25f,.75f),new Color(.1f,.2f,.3f,.4f));
 var face=New(triangle,V(0,0),V(1,0),V(0,1),0,lod,17);faces[lod]=face;
 var item=New(input,true);Set(item,"Faces",Arr(bound,New(bound,face,0x80000000u+(uint)lod)));Set(item,"Materials",Arr(D("SurfaceAsset"),native));Set(item,"Native",original);inputs.SetValue(item,lod);
 }
 var regions=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
 object[] composeCall={inputs,owner,regions,null,null};
 var composed=(Array)T("MovableSurfaceAtlas").GetMethod("Compose",S).Invoke(null,composeCall);
 Check(composed!=null && composed.Length==3,"original composition failed");
 for(int lod=0;lod<3;lod++){
 var output=(Array)composed.GetValue(lod);Check(output.Length==1,"original face count changed");
 var face=output.GetValue(0);foreach(var corner in new[]{"A","B","C"})Check(Get(face,corner).Equals(Get(faces[lod],corner)),"original vertex attribute changed");
 Check(ReferenceEquals(((Array)((Array)composeCall[3]).GetValue(lod)).GetValue(0),native),"original network layer replaced");cases++;
 }
 ((IDisposable)owner).Dispose();
 var pathType=D("AssetDataPath");var pathFactory=pathType.GetMethods(S).Single(m=>m.Name=="Create" && m.GetParameters().Length==3 && m.GetParameters()[0].ParameterType==typeof(string) && m.GetParameters()[1].ParameterType==typeof(string));
 var path=pathFactory.Invoke(null,new object[]{"BridgeBuilder","Malformed123",Enum.Parse(pathFactory.GetParameters()[2].ParameterType,"None")});
 var add=dbType.GetMethods(F).Single(m=>m.Name=="AddAsset" && !m.IsGenericMethod && m.GetParameters().Length==3 && m.GetParameters()[1].ParameterType==typeof(Type));
 var broken=add.Invoke(local,new[]{path,(object)D("SurfaceAsset"),Hash("bbbbbbbb222233334444555555555555")});
 var meta=broken.GetType().GetMethod("GetMeta",F).Invoke(broken,null);var actual=(string)meta.GetType().GetField("path",F)?.GetValue(meta)??(string)meta.GetType().GetProperty("path",F).GetValue(meta);
 Directory.CreateDirectory(Path.GetDirectoryName(actual));File.WriteAllBytes(actual,new byte[]{1});
 object[] failed={broken,"tmp123","Draw02",null,Owned(),null,null};
 Check(!(bool)lane.Invoke(null,failed) && failed[5]==null && !string.IsNullOrEmpty((string)failed[6]),"actual native short stream must return failure");cases++;
 object[] cacheCall={native,null,3,null,null,"tmp123"};
 Check(!(bool)T("NativeSurfaceCaches").GetMethod("TryFind",S).Invoke(null,cacheCall)
  && !((IEnumerable)cacheCall[3]).Cast<object>().Any() && ((string)cacheCall[4]).Contains("tmp123"),"unavailable native streaming must return diagnosed failure, never empty success");cases++;
 object[] otherCall={FormatterServices.GetUninitializedObject(D("TextureAsset")),null,null};
 var requirements=T("MovableLaneMidMips").GetMethods(S).Single(m=>m.Name=="TryRequiredCaches"&&m.GetParameters().Length==3);
 Check((bool)requirements.Invoke(null,otherCall) && !((IEnumerable)otherCall[1]).Cast<object>().Any() && otherCall[2]==null,"non-Surface dependency must require no reverse Surface edge");cases++;
 object[] readerCall={broken,null,null};
 Check(!(bool)T("MovableSurfaceReader").GetMethod("TryRead",S).Invoke(null,readerCall) && readerCall[1]==null && readerCall[2]!=null,"malformed source decoder must return explicit failure");cases++;
 var route=T("MovableRoadRoute").GetMethod("TryRead",S);
 foreach(var asset in new[]{original,native}){object[] routeCall={asset,null,null};Check((bool)route.Invoke(null,routeCall) && routeCall[2]==null,"valid arbitrary source classification failed");cases++;}
 object[] brokenRoute={broken,null,null};Check(!(bool)route.Invoke(null,brokenRoute) && brokenRoute[2]!=null,"broken classification escaped result");cases++;
 var atlas=T("MovableSurfaceAtlas");
 var invalidOwner=New(T("MovableRoadMaterial"),"tmp123","Draw02",null);
 var classified=FormatterServices.GetUninitializedObject(T("MovableSidewalkFaces"));Set(classified,"<Error>k__BackingField","injected classification read failure");
 Set(inputs.GetValue(0),"Sidewalks",classified);object[] invalidCompose={inputs,invalidOwner,regions,null,null};
 Check(atlas.GetMethod("Compose",S).Invoke(null,invalidCompose)==null && ((Array)invalidCompose[3]).Length==0 && (string)T("MovableRoadMaterial").GetProperty("Failure",F).GetValue(invalidOwner)!=null,"classification failure became successful geometry or partial material output");cases++;((IDisposable)invalidOwner).Dispose();
 var decoder=T("MovableVtTextureSource");
 foreach(var bytes in new[]{new byte[1],new byte[]{2,0,255}}){foreach(var method in new[]{"TryReadTexture","TryReadSurface"}){
 using(var stream=new MemoryStream(bytes)){object[] decode={stream,(Func<byte[],byte[]>)(b=>b),null,null};Check(!(bool)decoder.GetMethod(method,S).Invoke(null,decode)&&decode[3]!=null,"invalid VT decode escaped result");cases++;}
 }}
 using(var raw=new MemoryStream()){
 using(var writer=new BinaryWriter(raw,System.Text.Encoding.UTF8,true)){
 writer.Write((ushort)1);writer.Write((byte)255);writer.Write(new byte[16]);writer.Write((ushort)0);writer.Write((ushort)16);writer.Write((ushort)16);writer.Write((ushort)16);writer.Write((ushort)0);writer.Write((byte)1);writer.Write((byte)UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);writer.Write((byte)FilterMode.Bilinear);writer.Write((byte)TextureWrapMode.Clamp);writer.Write((byte)8);writer.Write(0);writer.Write(1);writer.Write(5120);writer.Write(Enumerable.Repeat((byte)42,5120).ToArray());}
 raw.Position=0;object[] decode={raw,(Func<byte[],byte[]>)(b=>b),null,null};Check((bool)decoder.GetMethod("TryReadTexture",S).Invoke(null,decode)&&decode[3]==null,"compiled RGBA VT upload failed");var texture=(Texture2D)decode[2];Check(texture.width==16&&texture.height==16&&texture.mipmapCount==1,"decoded dimensions changed");UnityEngine.Object.DestroyImmediate(texture);cases++;
 }
 // Register an isolated, real native descriptor with no external payloads through the actual catalogue.
 var catalog=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);
 var installation=Path.Combine(root,"catalogue");
 var originalBytes=File.ReadAllBytes(targetPath);int prefix,tail;
 using(var reader=new BinaryReader(new MemoryStream(originalBytes))){var dummy=FormatterServices.GetUninitializedObject(D("SurfaceAsset"));dummy.GetType().GetMethod("ReadHeader",F).Invoke(dummy,new object[]{reader});
 foreach(var method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
 prefix=(int)reader.BaseStream.Position;Check(reader.ReadByte()==255,"fixture texture dictionary");for(int n=reader.ReadInt32();n>0;n--){reader.ReadString();var marker=reader.ReadByte();if(marker==255)reader.ReadBytes(16);else Check(marker==0,"fixture texture identity");}tail=(int)reader.BaseStream.Position;}
 byte[] payload;
 using(var stream=new MemoryStream()){stream.Write(originalBytes,0,prefix);using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write((byte)255);writer.Write(0);}stream.Write(originalBytes,tail,originalBytes.Length-tail);payload=stream.ToArray();}
 void WritePrepared(string style,string cid,byte[] bytes){var folder=Path.Combine(installation,"Materials",style,"Road");Directory.CreateDirectory(folder);var path=Path.Combine(folder,"NativeAsphalt.Surface");File.WriteAllBytes(path,bytes);File.WriteAllText(path+".cid",cid);}
 WritePrepared("Fixture",new string('1',32),payload);
 catalog.GetMethod("Initialize",S).Invoke(null,new object[]{installation});
 object Road(string style)=>catalog.GetMethod("Road",S).Invoke(null,new object[]{style});
 var preparedResult=Road("Fixture");Check(Get(preparedResult,"Error")==null && Get(preparedResult,"Value")!=null,"catalogue actual registration failed: "+Get(preparedResult,"Error"));cases++;
 var cached=Road("Fixture");Check(ReferenceEquals(Get(preparedResult,"Value"),Get(cached,"Value")),"catalogue cache identity differs");cases++;
 var absent=Road("Missing");Check(Get(absent,"Value")==null && (string)Get(absent,"Error")!=null,"missing prepared source escaped/returned partial asset");cases++;
 var invalidStyle=Road("../Fixture");Check(Get(invalidStyle,"Value")==null && (string)Get(invalidStyle,"Error")!=null,"invalid style escaped result");cases++;
 var altered=(byte[])payload.Clone();BitConverter.GetBytes(991123).CopyTo(altered,2);WritePrepared("Conflict",new string('1',32),altered);
 var conflict=Road("Conflict");Check(Get(conflict,"Value")==null && ((string)Get(conflict,"Error")).Contains("different payloads"),"conflicting prepared CID was registered");cases++;
 WritePrepared("Retry",new string('2',32),payload);catalog.GetField("_pendingCacheRefresh",S).SetValue(null,true);
 var pending=Road("Retry");Check(Get(pending,"Value")==null && (string)Get(pending,"Error")!=null && (bool)catalog.GetField("_pendingCacheRefresh",S).GetValue(null),"pending VT refresh failure was lost");cases++;
 catalog.GetField("_pendingCacheRefresh",S).SetValue(null,false);var retried=Road("Retry");Check(Get(retried,"Error")==null && Get(retried,"Value")!=null,"registered immutable input could not be retried");cases++;
 var failedSurface=New(T("MovableExistingRoadSurface+SurfaceResult"),null,"fixture prepared failure");
 var patchType=T("MovableExistingRoadSurface+Patch");var constructor=patchType.GetConstructors(F).Single(c=>c.GetParameters().Length==15 && c.GetParameters()[4].ParameterType==failedSurface.GetType());
 var patchArgs=constructor.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Activator.CreateInstance(p.ParameterType)).ToArray();patchArgs[0]=Vector2.zero;patchArgs[1]=Vector2.one;patchArgs[2]=Vector4.one;patchArgs[3]=Vector4.one;patchArgs[4]=failedSurface;patchArgs[9]=true;
 var failedPatch=constructor.Invoke(patchArgs);Check((string)Get(failedPatch,"Error")=="fixture prepared failure","catalogue error lost before Patch consumer");cases++;
 object[] shutdown={null};Check((bool)catalog.GetMethod("TryStop",S).Invoke(null,shutdown),"catalogue shutdown failed: "+shutdown[0]);cases++;

 var refsType=mod.GetType("BridgeBuilder.Runtime.BridgeSurfaceReferences",true);var read=refsType.GetMethod("TryRead",S);
 var referencePayload=File.ReadAllBytes(Arg("-surfaceFile"));Func<int,string,bool> notVirtual=(t,p)=>false;
 object[] parse={referencePayload,notVirtual,null,null};Check((bool)read.Invoke(null,parse)&&parse[3]==null,"native source reference referencePayload rejected");cases++;
 for(int cut=0;cut<referencePayload.Length;cut++){
   object[] truncated={referencePayload.Take(cut).ToArray(),notVirtual,null,null};
   Check(!(bool)read.Invoke(null,truncated)&&((string[])truncated[2]).Length==0&&!string.IsNullOrEmpty((string)truncated[3]),"truncated native descriptor exposed partial references at "+cut);cases++;
 }
 var frameType=T("MovableUvFrame");var frame=New(frameType,Vector3.zero,Vector3.zero,Vector3.zero,Vector2.zero,Vector2.right,Vector2.up,Vector3.zero,Vector3.right,Vector3.up);
 Check(!string.IsNullOrEmpty((string)Get(frame,"Error")),"invalid frame did not return Error");cases++;
 object[] polar={0d,0d,0d,0d,0d,0d,0d,0d,null};Check(!(bool)frameType.GetMethod("TryPolarRotation",S).Invoke(null,polar)&&polar[8]!=null,"invalid polar frame accepted");cases++;
 object[] tangent={Vector3.up,Vector4.zero,Vector4.zero,null,null};Check(!(bool)T("MovableRoadAtlasWrap").GetMethod("TryProjectedTangent",S).Invoke(null,tangent)&&tangent[4]!=null,"singular projected tangent accepted");cases++;
 var tiles=New(T("MovableStructureTiles"),"Draw01",true);
 object[] preserve={"DrawBridge01Bottom300cm Mesh",0,original,false,null};
 Check((bool)tiles.GetType().GetMethod("TryPreserveNativeNetwork",F).Invoke(tiles,preserve)&&!(bool)preserve[3]&&preserve[4]==null,"unknown source must not gain a measured prototype gate");cases++;
 original.GetType().GetProperty("id").SetValue(original,New(D("Identifier"),Hash("16b28f9bc6abc54fd73acbdfa5f14978")));
 preserve=new object[]{"DrawBridge01Bottom300cm Mesh",0,original,false,null};
 Check(!(bool)tiles.GetType().GetMethod("TryPreserveNativeNetwork",F).Invoke(tiles,preserve)&&!(bool)preserve[3]&&preserve[4]!=null,"changed native prototype header lacks failure result");cases++;

 var contextType=T("MovableNetworkContext");var context=FormatterServices.GetUninitializedObject(contextType);
 Set(context,"_generationOwner","result126-fixture");var edgeField=contextType.GetField("Edges",F);Set(context,"Edges",Array.CreateInstance(edgeField.FieldType.GetElementType(),1));
 var compile=contextType.GetMethod("CompileSurface",F);var cp=compile.GetParameters();var origin=Activator.CreateInstance(cp[2].ParameterType);
 foreach(var combination in new[]{new[]{-1,0},new[]{0,999},new[]{0,0}}){
 var call=new object[]{combination[0],Enum.ToObject(cp[1].ParameterType,combination[1]),origin,Enum.ToObject(cp[3].ParameterType,0),true,2};
 var sourceFailure=compile.Invoke(context,call);Check(((string)Get(sourceFailure,"Error")).Contains("owner=result126-fixture")&&((Array)Get(sourceFailure,"Triangles")).Length==0&&((Array)Get(sourceFailure,"Materials")).Length==0,"source failure escaped or published partial geometry");cases++;}
 var roadCompile=contextType.GetMethod("CompileRoad",F);var rp=roadCompile.GetParameters();var contributionType=rp[0].ParameterType.GetGenericArguments()[0];
 foreach(var entries in new[]{Array.CreateInstance(contributionType,0),Arr(contributionType,Activator.CreateInstance(contributionType,new object[]{-1,Enum.ToObject(cp[1].ParameterType,0)}))}){
 var sourceFailure=roadCompile.Invoke(context,new object[]{entries,Array.CreateInstance(rp[1].ParameterType.GetGenericArguments()[0],0),origin,Enum.ToObject(rp[3].ParameterType,0),true,1});Check(((string)Get(sourceFailure,"Error")).Contains("LOD=1")&&((Array)Get(sourceFailure,"Triangles")).Length==0,"closed road failure result lost");cases++;}
 var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward,new Vector3(1,0,1)};mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,-1),4).ToArray();mesh.SetUVs(0,Enumerable.Range(0,4).Select(i=>new Vector4(i,.25f,7,9)).ToList());mesh.subMeshCount=2;mesh.SetIndices(new[]{0,1,2},MeshTopology.Triangles,0);mesh.SetIndices(new[]{1,3,2},MeshTopology.Triangles,1);
 var copy=T("MovableNativeRoadOriginal").GetMethod("TryCopy",S);
 foreach(int slot in new[]{0,1}){object[] copyCall={"result126",mesh,slot,null,null};Check((bool)copy.Invoke(null,copyCall)&&copyCall[4]==null,"native original copy sourceFailure");var model=copyCall[3];var uploaded=(Mesh)model.GetType().GetMethod("ToUnityMesh",F).Invoke(model,new object[]{true});try{var expected=new System.Collections.Generic.List<Vector4>();var copiedUv=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(0,expected);uploaded.GetUVs(0,copiedUv);Check(uploaded.vertices.SequenceEqual(mesh.vertices)&&uploaded.triangles.SequenceEqual(mesh.GetIndices(slot))&&uploaded.normals.SequenceEqual(mesh.normals)&&uploaded.tangents.SequenceEqual(mesh.tangents)&&copiedUv.SequenceEqual(expected),"original native attributes changed");}finally{((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(uploaded);}cases++;}
 object[] invalidCopy={"result126",mesh,999,null,null};Check(!(bool)copy.Invoke(null,invalidCopy)&&invalidCopy[3]==null&&invalidCopy[4]!=null,"post-attribute allocation failure did not roll back");cases++;UnityEngine.Object.DestroyImmediate(mesh);
 Debug.Log("PASS native126 source extraction failure results (invalid input and missing ECS world), empty/invalid contribution propagation, two native original submeshes exact P/N/T/UV4/indices, post-allocation failure. Full city ECS successful extraction not asserted.");

 Debug.Log("PASS "+cases+" compiled material results and original-shader descriptor identity: network template17, unknown same-network-shader template, object source and additional keywords unchanged; no owned replacement assets; original source UV4/color/positions across three LODs; missing source diagnosed. No rendered network buffers, motion or cold-load visual acceptance asserted.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
