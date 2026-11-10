using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using System.Runtime.Serialization;using UnityEngine;using UnityEditor;
public static class RoadGpu136Cases {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run(){try{
 var a=Environment.GetCommandLineArgs();string Arg(string k)=>a[Array.IndexOf(a,k)+1];var managed=Arg("-gameManaged");AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);object New(Type t,params object[] xs)=>Activator.CreateInstance(t,F,null,xs,null);
 var geo=T("MovableRoadGeometry");var vt=geo.GetNestedType("Vertex",F);var tt=geo.GetNestedType("Triangle",F);var pt=geo.GetNestedType("Partition",F);var gpu=T("MovableRoadGpu").GetMethod("TryBuild",F);var cpu=geo.GetMethod("TryBind",F);
 object Vertex(float x,float z,bool uv4=false)=>New(vt,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,uv4?0.25f:0,uv4?0.75f:0),Color.white);
 object Tri(int mat=0,bool uv4=false,float x=-1)=>New(tt,Vertex(x,-1,uv4),Vertex(1,1,uv4),Vertex(-1,1,uv4),mat,0,0);
 Array Source(params object[] xs){var ar=Array.CreateInstance(tt,xs.Length);for(int i=0;i<xs.Length;i++)ar.SetValue(xs[i],i);return ar;}
 var gap=pt.GetField("Gap",F).GetValue(null);object Leaf(int b)=>New(pt,b);object Split(object l,object r)=>New(pt,Matrix4x4.identity,l,r);
 int cases=0;
 object[] Run(Array src,object tree,bool expectGpu=true,int slots=1,object oc=null,object fc=null){object[] g={src,tree,2,slots,null,null,null,true,oc,fc};Need((bool)gpu.Invoke(null,g),"GPU false "+g[6]);Need((g[5]!=null)==expectGpu,"unexpected backend");object[] c={src,tree,2,slots,null,null,true,oc,fc};Need((bool)cpu.Invoke(null,c),"CPU reference failed");var gb=(Array)g[4];var cb=(Array)c[4];Need(gb.Length==cb.Length,"count mismatch");for(int i=0;i<gb.Length;i++){var t=gb.GetValue(i);var refT=cb.GetValue(i);Need(t.Equals(refT),"synthetic exact attributes differ");}cases++;return g;}
 var seam=Split(Leaf(0),Leaf(1));var first=Run(Source(Tri()),seam);Run(Source(Tri(0,true)),seam);Run(Source(Tri(0,false,-0.00000001f)),seam);Run(Source(Tri()),gap);Run(Source(),seam);var oc=Array.CreateInstance(pt,1);oc.SetValue(Leaf(1),0);Run(Source(Tri()),seam,true,1,oc);
 var fc=(IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(ValueTuple<int,int>),pt));fc.Add((0,0),Leaf(0));Run(Source(Tri()),seam,true,1,oc,fc);
 var deep=Leaf(0);for(int i=0;i<25;i++)deep=Split(gap,deep);Run(Source(Tri()),deep,false);
 foreach(var bad in new[]{Source(Tri(1)),Source(Tri(0,false,float.NaN))}){object[] x={bad,seam,2,1,null,null,null,false,null,null};Need(!(bool)gpu.Invoke(null,x)&&x[5]==null,"invalid input accepted");cases++;}
 object[] invalidBone={Source(Tri()),Leaf(2),2,1,null,null,null,false,null,null};Need(!(bool)gpu.Invoke(null,invalidBone),"invalid bone accepted");cases++;
 var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var st=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var surfaces=Array.CreateInstance(st,2);surfaces.SetValue(FormatterServices.GetUninitializedObject(st),0);surfaces.SetValue(FormatterServices.GetUninitializedObject(st),1);
 var multi=Run(Source(Tri(0),Tri(1,true),Tri(0,false,-2)),Leaf(1),true,2);var bound=(Array)multi[4];var packed=(uint[])multi[5];var modelType=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.dll")).GetType("Colossal.AssetPipeline.Importers.ModelImporter+Model",true);
 for(int alias=0;alias<2;alias++){
  if(alias==1)surfaces.SetValue(surfaces.GetValue(0),1);var owned=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(modelType));object[] args={"group check",bound,surfaces,2,owned,null,null,packed};var models=(Array)T("MovableRoadAssets").GetMethod("BuildMaterialMeshes",F).Invoke(null,args);Need(models.Length==(alias==0?2:1),"canonical group count");
  var expectedCounts=alias==0?new[]{6,3}:new[]{9};for(int i=0;i<models.Length;i++){var m=models.GetValue(i);var mesh=(Mesh)modelType.GetMethod("ToUnityMesh").Invoke(m,new object[]{true});Need(mesh.vertexCount==expectedCounts[i],"group size");var uv=m.GetType().GetMethod("GetAttributeData").Invoke(m,new object[]{UnityEngine.Rendering.VertexAttribute.TexCoord0});Need((int)uv.GetType().GetProperty("dimension").GetValue(uv)==(alias==0&&i==0?2:4),"native UV2/UV4 dimension");var expectedIndices=alias==0?(i==0?new[]{0,2}:new[]{1}):new[]{0,1,2};var positions=mesh.vertices;for(int j=0;j<positions.Length;j++){int word=expectedIndices[j/3]*33+j%3*11;Need(BitConverter.SingleToInt32Bits(positions[j].x)==unchecked((int)packed[word]),"group packed order");}UnityEngine.Object.DestroyImmediate(mesh);}
  foreach(IDisposable m in owned)m.Dispose();cases++;
 }
 UnityEngine.Debug.Log("PASS production GPU cases="+cases+"; seam/gap/empty/UV2+4/occurrence/face precedence/depth CPU fallback/invalid attributes/material/bone/canonical grouping and original order.");EditorApplication.Exit(0);
 }catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}}
}
