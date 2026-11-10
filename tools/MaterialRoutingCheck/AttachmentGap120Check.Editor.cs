using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class AttachmentGap120Check {
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

 var method=T("MovableRoadObjects").GetMethod("MountBone",S);bool old=method.GetParameters().Length==4;
 var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var furniture=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.StaticObjectPrefab",true));furniture.name="Unlisted custom furniture";
 object Actual(Vector3 p,object tree)=>method.Invoke(null,old?new object[]{furniture,p,tree,17}:new object[]{p,tree,17});
 using((IDisposable)T("GenerationParameters").GetMethod("Begin",S).Invoke(null,null)){
 var ped=T("PedestrianDraw01RoadPartitions").GetProperty("Carriageway",S).GetValue(null);
 if(old){try{Actual(new Vector3(4,10,0),ped);throw new Exception("baseline must reject ordinary furniture in actual gap");}catch(TargetInvocationException e){Check(e.InnerException is InvalidOperationException,"unexpected baseline cause");}Debug.Log("PASS baseline actual PedestrianDraw01 measured central gap throws for unclassified furniture");EditorApplication.Exit(0);return;}
 Check(Actual(new Vector3(4,10,0),ped)==null,"central gap must omit ordinary furniture");
 var partition=T("MovableRoadGeometry+Partition");int checks=0,gaps=0,supported=0,seams=0;var names=new System.Collections.Generic.HashSet<string>();
 object Expected(Vector3 point,object tree){while(!(bool)Get(tree,"IsGap") && (int)Get(tree,"Bone")<0){var matrix=(Matrix4x4)Get(tree,"ObjectToSeam");tree=Get(tree,matrix.MultiplyPoint3x4(point).z<0?"Negative":"Positive");}return (bool)Get(tree,"IsGap")?null:Get(tree,"Bone");}
 void Verify(Vector3 point,object tree){var e=Expected(point,tree);Check(Equals(e,Actual(point,tree)),"mount partition mismatch "+point);checks++;if(e==null)gaps++;else supported++;}
 foreach(string style in new[]{"Draw01","Draw02","Draw03","Lift01","Lift02","Lift03","Lift04","Lift05","PedestrianDraw01","PedestrianDraw02"}){
 var type=T(style+"RoadPartitions");var trees=new System.Collections.Generic.List<object>();
 foreach(var property in type.GetProperties(S).Where(p=>p.PropertyType==partition))trees.Add(property.GetValue(null));
 foreach(var factory in type.GetMethods(S).Where(m=>m.IsAssembly && m.ReturnType==partition && m.GetParameters().Length==1 && m.GetParameters()[0].ParameterType==typeof(float)))foreach(float width in new[]{-2f,0f,4f})trees.Add(factory.Invoke(null,new object[]{width}));
 Check(trees.Count>0,"uncovered style "+style);names.Add(style);
 foreach(var tree in trees){foreach(float x in new[]{-4f,0f,4f})for(int z=-180;z<=180;z++)Verify(new Vector3(x,10,z*.5f),tree);
 var stack=new System.Collections.Generic.Stack<object>();stack.Push(tree);while(stack.Count>0){var node=stack.Pop();if((bool)Get(node,"IsGap")||(int)Get(node,"Bone")>=0)continue;var frame=(Matrix4x4)Get(node,"ObjectToSeam");var origin=frame.inverse.MultiplyPoint3x4(Vector3.zero);Verify(origin,tree);Verify(origin+Vector3.forward*.0001f,tree);Verify(origin-Vector3.forward*.0001f,tree);seams++;stack.Push(Get(node,"Negative"));stack.Push(Get(node,"Positive"));}}
 }
 // A legal fixed support must remain legal even outside the mechanism's usual length.
 Check((int)Actual(new Vector3(300,10,700),New(partition,0))==0,"fixed exterior support omitted");
 // Coordinate classification is per deck; a translated/reversed source frame reaches the same anchor.
 var anchor=new Vector3(4,10,0);foreach(float height in new[]{0f,10f})foreach(float yaw in new[]{0f,180f}){var frame=Matrix4x4.TRS(new Vector3(0,height,0),Quaternion.Euler(0,yaw,0),Vector3.one);var input=frame.inverse.MultiplyPoint3x4(anchor);Verify(frame.MultiplyPoint3x4(input),ped);}
 Check(gaps>0 && supported>0 && names.Count==10,"incomplete coverage");
 Debug.Log("PASS all "+names.Count+" actual style partition families; "+checks+" anchors, gaps="+gaps+", supported="+supported+", seams="+seams+"; fixed exterior support, independent opposite/deck frames, ordinary furniture skip. Shared mounting decisions are independent of render LOD; no game rendering claimed.");}
 EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
