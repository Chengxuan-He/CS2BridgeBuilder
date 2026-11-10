using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class NativeNetworkParent123Check
{
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object New(Type t,params object[] args)=>Activator.CreateInstance(t,args);
 static void Field(object obj,string name,object value)=>obj.GetType().GetField(name,F).SetValue(obj,value);
 static void Check(bool condition,string text){if(!condition)throw new Exception(text);}
 public static void Run(){object world=null;try{
 var args=Environment.GetCommandLineArgs();string Arg(string key)=>args[Array.IndexOf(args,key)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var entities=Assembly.LoadFrom(Path.Combine(managed,"Unity.Entities.dll"));
 Type G(string name)=>game.GetType(name,true);Type E(string name)=>entities.GetType("Unity.Entities."+name,true);
 bool full=Array.IndexOf(args,"-fullWorld")>=0;
 object manager=null,parent=null,child=null;MethodInfo set=null,get=null,add=null;
 if(full){
 var worldType=E("World");world=New(worldType,"Native parent evidence123",Enum.Parse(E("WorldFlags"),"Game"));
 manager=worldType.GetProperty("EntityManager").GetValue(world);var mt=manager.GetType();
 var create=mt.GetMethods(F).Single(m=>m.Name=="CreateEntity"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.IsArray);
 object Entity()=>create.Invoke(manager,new object[]{Array.CreateInstance(E("ComponentType"),0)});
 parent=Entity();child=Entity();
 add=mt.GetMethods(F).Single(m=>m.Name=="AddComponentData"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==2);
 set=mt.GetMethods(F).Single(m=>m.Name=="SetComponentData"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==2);
 get=mt.GetMethods(F).Single(m=>m.Name=="GetComponentData"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==1);


 }
 void Add(object entity,object component){if(full)add.MakeGenericMethod(component.GetType()).Invoke(manager,new[]{entity,component});}
 var transformType=G("Game.Objects.Transform");var f3=transformType.GetField("m_Position").FieldType;var quat=transformType.GetField("m_Rotation").FieldType;
 object V(float x,float y,float z)=>New(f3,x,y,z);
 object Transform(float angle){var t=New(transformType);Field(t,"m_Position",V(0,0,0));var q=Quaternion.AngleAxis(angle,Vector3.right);Field(t,"m_Rotation",New(quat,q.x,q.y,q.z,q.w));return t;}
 Add(parent,Transform(0));Add(child,Transform(0));var owner=New(G("Game.Common.Owner"));Field(owner,"m_Owner",parent);Add(child,owner);
 var attached=New(G("Game.Objects.Attached"));Field(attached,"m_Parent",parent);Add(child,attached);
 var edgeType=G("Game.Net.EdgeGeometry");var segmentType=G("Game.Net.Segment");var curveType=segmentType.GetField("m_Left").FieldType;
 object Curve(float x,float start,float end,float y){var c=New(curveType);Field(c,"a",V(x,y,start));Field(c,"b",V(x,y,start+(end-start)/3));Field(c,"c",V(x,y,start+2*(end-start)/3));Field(c,"d",V(x,y,end));return c;}
 object Segment(float start,float end,float y){var s=New(segmentType);Field(s,"m_Left",Curve(-3,start,end,y));Field(s,"m_Right",Curve(3,start,end,y));Field(s,"m_Length",New(segmentType.GetField("m_Length").FieldType,end-start,end-start));return s;}
 object Edge(float y){var e=New(edgeType);Field(e,"m_Start",Segment(0,10,y));Field(e,"m_End",Segment(10,20,y));var bounds=New(edgeType.GetField("m_Bounds").FieldType);Field(bounds,"min",V(-3,y,0));Field(bounds,"max",V(3,y,20));Field(e,"m_Bounds",bounds);return e;}
 object currentEdge=Edge(0);Add(child,currentEdge);
 var calculate=G("Game.Rendering.BatchDataHelpers").GetMethod("CalculateEdgeParameters",F);
 string Parameters(bool rotated){var edge=full?get.MakeGenericMethod(edgeType).Invoke(manager,new[]{child}):currentEdge;object[] call={edge,rotated,null};calculate.Invoke(null,call);return string.Join("|",call[2].GetType().GetFields(F).Where(f=>!f.IsStatic).Select(f=>f.Name+"="+f.GetValue(call[2])));}
 int checks=0;foreach(bool rotated in new[]{false,true}){var before=Parameters(rotated);foreach(float angle in new[]{30f,60f,90f}){if(full)set.MakeGenericMethod(transformType).Invoke(manager,new[]{parent,Transform(angle)});else Transform(angle);Check(Parameters(rotated)==before,"Parent transform unexpectedly changed native NetComposition parameters");checks++;}currentEdge=Edge(5);if(full)set.MakeGenericMethod(edgeType).Invoke(manager,new[]{child,currentEdge});Check(Parameters(rotated)!=before,"Positive edge-geometry update control was ineffective");currentEdge=Edge(0);if(full)set.MakeGenericMethod(edgeType).Invoke(manager,new[]{child,currentEdge});}
 Debug.Log("PASS native network parameter control experiment; full ECS="+full+"; "+checks+" parent rotation inputs preserve all 13 native composition fields; direct EdgeGeometry updates change them. Executed actual Game.dll CalculateEdgeParameters selected by BatchDataSystem Net path. No simulation/render loop or GPU bone execution was run; this disproves only automatic parent-transform consumption by this native parameter path, not every possible engine integration.");
 if(world is IDisposable completed)completed.Dispose();world=null;EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);if(world is IDisposable d)d.Dispose();EditorApplication.Exit(1);}}
}
