using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using System.Runtime.Serialization;using UnityEngine;using UnityEngine.Rendering;using UnityEditor;
public static class NativeMapping124Check {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr LoadLibraryW(string path);
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 Check(LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")))!=IntPtr.Zero,"native pipeline unavailable");
 var asm=Assembly.LoadFrom(Arg("-modAssembly"));var mapping=asm.GetType("BridgeBuilder.Bridges.MovableNetworkRoadMapping",true);var type=mapping.GetField("Surface",F).FieldType;
 object Surface(){var s=FormatterServices.GetUninitializedObject(type);using(var reader=new BinaryReader(File.OpenRead(Arg("-surfaceFile"))))type.GetMethod("ReadHeader",F).Invoke(s,new object[]{reader});return s;}
 var surface=Surface();var original=Surface();var mapFaces=new System.Collections.Generic.Dictionary<int,int[]>{{0,new[]{0}}};
 object Map(float density,float sign)=>Activator.CreateInstance(mapping,F,null,new object[]{surface,mapFaces,new Vector4(.5f,0,0,2),density,sign},null);
 foreach(var d in new[]{0f,-1f,float.NaN,float.PositiveInfinity})Check(mapping.GetField("Error",F).GetValue(Map(d,1))!=null,"invalid map result accepted");
 var mesh=new Mesh();mesh.vertices=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,0,2),new Vector3(1,0,2)};mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,1),4).ToArray();mesh.colors=Enumerable.Repeat(new Color(.2f,.3f,.4f,.5f),4).ToArray();mesh.SetUVs(0,Enumerable.Repeat(new Vector4(.1f,.2f,7,9),4).ToList());mesh.triangles=new[]{0,1,2,1,3,2};
 var method=mapping.GetMethod("TryAppendMesh",F);var p=method.GetParameters();var owned=(IList)Activator.CreateInstance(p[4].ParameterType);var batch=(IList)Activator.CreateInstance(p[5].ParameterType);var surfaces=(IList)Activator.CreateInstance(p[6].ParameterType);
 object[] call={"mapping-result-fixture",mesh,0,original,owned,batch,surfaces,null};Check((bool)method.Invoke(Map(.25f,1),call),(string)call[7]);Check(owned.Count==2&&batch.Count==2&&surfaces.Count==2,"positive split output differs");
 int triangles=0;foreach(var model in batch){var uploaded=(Mesh)model.GetType().GetMethod("ToUnityMesh",F).Invoke(model,new object[]{true});Check(uploaded.vertices.SequenceEqual(mesh.vertices),"positions changed");var uv=new System.Collections.Generic.List<Vector4>();uploaded.GetUVs(0,uv);Check(uv.All(x=>x.z==7&&x.w==9),"UV zw changed");triangles+=uploaded.triangles.Length/3;UnityEngine.Object.DestroyImmediate(uploaded);}Check(triangles==2,"topology changed");
 var saved=owned.Cast<object>().ToArray();object[] bad={"mapping-result-failure",mesh,0,original,owned,batch,surfaces,null};Check(!(bool)method.Invoke(Map(.25f,-1),bad)&&((string)bad[7]).Contains("unsupported packed tangent layout"),"post-model frame failure was not explicit");Check(owned.Count==2&&batch.Count==2&&surfaces.Count==2&&saved.SequenceEqual(owned.Cast<object>()),"failure did not restore prior list contents");
 foreach(var model in owned){var uploaded=(Mesh)model.GetType().GetMethod("ToUnityMesh",F).Invoke(model,new object[]{true});Check(uploaded.vertexCount==4,"rollback disposed an existing owned model");UnityEngine.Object.DestroyImmediate(uploaded);((IDisposable)model).Dispose();}UnityEngine.Object.DestroyImmediate(mesh);
 Debug.Log("PASS native mapping result: four invalid density results, real compiled TryAppendMesh/native model upload, exact positions/physical triangle count/UVzw, post-allocation frame failure rolls back new outputs and preserves two pre-existing live owned models. Synthetic geometry; no world rendering.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
