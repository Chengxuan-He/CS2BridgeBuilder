using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using UnityEngine;using UnityEditor;
public static class Road134Check {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));
 Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 var geo=T("MovableRoadGeometry");var vertex=geo.GetNestedType("Vertex",F);var triangle=geo.GetNestedType("Triangle",F);var partition=geo.GetNestedType("Partition",F);
 object V(float x,float z)=>New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,0,0),Color.white);
 object Tri(float x)=>New(triangle,V(x,-1),V(1,1),V(-1,1),0,0,0);
 var triangles=Array.CreateInstance(triangle,1);triangles.SetValue(Tri(-1),0);
 object P()=>New(partition,Matrix4x4.identity,New(partition,0),New(partition,1));
 var skeletonType=game.GetType("Game.Prefabs.ProceduralAnimationProperties",true);var boneType=skeletonType.GetNestedType("BoneInfo");
 object Bones(){var sk=ScriptableObject.CreateInstance(skeletonType);var bs=Array.CreateInstance(boneType,2);for(int i=0;i<2;i++){var b=Activator.CreateInstance(boneType);boneType.GetField("name").SetValue(b,"bone"+i);boneType.GetField("scale").SetValue(b,Vector3.one);boneType.GetField("rotation").SetValue(b,Quaternion.identity);boneType.GetField("bindPose").SetValue(b,Matrix4x4.identity);boneType.GetField("parentId").SetValue(b,-1);bs.SetValue(b,i);}skeletonType.GetField("m_Bones").SetValue(sk,bs);return sk;}
 var skel=Bones();var p0=P();var bind=geo.GetMethod("TryBind",F);object[] ba={triangles,p0,2,1,null,null,false,null,null};Need((bool)bind.Invoke(null,ba),"bind "+ba[5]);var bound=(Array)ba[4];Need(bound.Length==3,"cross seam triangle count");
 var capture=New(T("MovableRoadReplayCapture"),"E:/CS2BridgeBuilder/work/lod134/fixture");
 var roadModel=T("MovableRoadModel");object[] ma={"test Material 0",bound,1,2,null,null};Need((bool)roadModel.GetMethod("TryBuild",F).Invoke(null,ma),"encode "+ma[5]);var model=ma[4];
 var cacheType=T("MovableRoadLodCache");var keyType=cacheType.GetNestedType("Key",F);var resultType=cacheType.GetNestedType("Result",F);var cache=New(cacheType);
 var surfaceType=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll")).GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
 var surfaces=Array.CreateInstance(surfaceType,1);var mappingType=T("MovableMaterialCompatibility").GetNestedType("Mapping",F);
 var dict=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(surfaceType,mappingType));
 object Key(Array ts,Pose pose,object tree,object sk)=>New(keyType,ts,pose,tree,null,null,surfaces,surfaces,dict,sk);
 var key=Key(triangles,Pose.identity,p0,skel);
 T("MovableRoadReplayCapture").GetMethod("Write",F).Invoke(capture,new object[]{0,"fixture",key,bound});
 Need(File.Exists("E:/CS2BridgeBuilder/work/lod134/fixture/fixture-lod0.bin"),"replay fixture absent");var models=Array.CreateInstance(model.GetType(),1);models.SetValue(model,0);var result=New(resultType,key,"test",models,surfaces,bound);cacheType.GetMethod("Add",F).Invoke(cache,new[]{result});
 object Find(object k)=>cacheType.GetMethod("Find",F).Invoke(cache,new[]{k});
 for(int i=0;i<3;i++)Need(Find(Key((Array)triangles.Clone(),Pose.identity,P(),Bones()))!=null,"equal LOD cache miss");
 var changed=(Array)triangles.Clone();changed.SetValue(Tri(-2),0);Need(Find(Key(changed,Pose.identity,P(),Bones()))==null,"different attributes reused");
 Need(Find(Key(triangles,new Pose(Vector3.right,Quaternion.identity),P(),Bones()))==null,"pose reused");
 Need(Find(Key(triangles,Pose.identity,New(partition,1),Bones()))==null,"partition reused");
 var otherSk=Bones();var bone=((Array)skeletonType.GetField("m_Bones").GetValue(otherSk)).GetValue(1);boneType.GetField("m_ConnectionID").SetValue(bone,99);Need(Find(Key(triangles,Pose.identity,P(),otherSk))!=null,"irrelevant bone metadata blocked reuse");
 skeletonType.GetField("m_Bones").SetValue(otherSk,Array.CreateInstance(boneType,3));Need(Find(Key(triangles,Pose.identity,P(),otherSk))==null,"different bone capacity reused");
 var named=(Array)resultType.GetMethod("NamedModels",F).Invoke(result,new object[]{"LOD1"});var clone=named.GetValue(0);
 var nativeA=(Mesh)model.GetType().GetMethod("ToUnityMesh").Invoke(model,new object[]{true});var nativeB=(Mesh)model.GetType().GetMethod("ToUnityMesh").Invoke(clone,new object[]{true});
 Need(nativeA.vertexCount==nativeB.vertexCount&&nativeA.triangles.SequenceEqual(nativeB.triangles),"publication differs");
 ((IDisposable)model).Dispose();Need(nativeA.vertices.SequenceEqual(nativeB.vertices),"published mesh lifetime borrowed source");UnityEngine.Object.DestroyImmediate(nativeA);UnityEngine.Object.DestroyImmediate(nativeB);
 Debug.Log("PASS actual DLL: equal LOD reuse, changed attributes/pose/partition/bone capacity miss; unused bone metadata preserved independently; seam clipping and model encoding; named shared model publication survives single owner disposal. "+mod.Location);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
