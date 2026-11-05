using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class CompatibleRoadCheck {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static Array Arr(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
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
 if(builtin){var generic=D("AssetDatabase`1");var game=D("Game");o.GetType().GetProperty("database").SetValue(o,FormatterServices.GetUninitializedObject(generic.MakeGenericType(game)));}
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
 object[] call={Arr(input,value),owner,paint,null};var result=(Array)compose.Invoke(null,call);var faces=(Array)result.GetValue(0);Check(faces.Length==1,"replacement cut or removed face");var actual=faces.GetValue(0);var materials=(Array)((Array)call[3]).GetValue(0);Check(ReferenceEquals(materials.GetValue((int)Get(actual,"Material")),target),"wrong output material slot");
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
 Check((int)Get(original,"m_InstanceRefCount")==1&&(int)Get(target,"m_InstanceRefCount")==1,"properties reference lease leaked");
 Debug.Log("PASS compatible road: actual 8 logged triangles x3 source poses, "+count+" vertices; real source/target descriptor readers, Compose direct material slots and provenance, density/phase/UVzw/attributes; supported and partial/family negative gates; no GPU/texture writer. Not actual game rendering.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
