using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class Lift03SharedDeck77Check {
const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
static void Need(bool ok,string m){if(!ok)throw new Exception(m);}
public static void Run(){try{
var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];string managed=Path.Combine(Arg("-gameData"),"Managed");
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var a=Assembly.LoadFrom(Arg("-candidateDll"));var runtime=a.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);runtime.GetMethod("Initialize",F).Invoke(null,new object[]{Arg("-preparedRoot")});
var source=a.GetType("BridgeBuilder.Bridges.Lift03SourceRoadMapping",true).GetMethod("Create",F);var st=source.GetParameters()[2].ParameterType;var cache=(IDictionary)runtime.GetField("Surfaces",F).GetValue(null);
object Read(string key){var surface=FormatterServices.GetUninitializedObject(st);using(var r=new BinaryReader(File.OpenRead(Path.Combine(Arg("-preparedRoot"),"Materials/Lift03/RepeatUnits",key,"Repeat.Surface"))))st.GetMethod("ReadHeader",F).Invoke(surface,new object[]{r});cache.Add("Lift03/RepeatUnits/"+key+"/Repeat.Surface",surface);return surface;}
var background=Read("CleanRoadBackground");var wear=Read("CleanLaneWear");var ballast=Read("906eeb04251378f9e20b74cc9dc1a3d0f689e383892d58517a3853e6b803adff");
var native=FormatterServices.GetUninitializedObject(st);var layoutType=a.GetType("BridgeBuilder.Bridges.MovableSelectedRoadLayout",true);var owner=a.GetType("BridgeBuilder.Bridges.Lift03RoadAppearance",true);var select=owner.GetMethod("ForReceiver",F);var fallback=owner.GetMethod("ForLevel",F);var category=select.GetParameters()[4].ParameterType;
var render=ScriptableObject.CreateInstance(select.GetParameters()[1].ParameterType);var nativeSurfaces=Array.CreateInstance(st,1);nativeSurfaces.SetValue(native,0);render.GetType().GetProperty("surfaceAssets",F).SetValue(render,nativeSurfaces);int cases=0,checks=0;
foreach(string deck in new[]{"Main","Auxiliary"})foreach(int lod in new[]{0,1,2})foreach(bool opposite in new[]{false,true})foreach(float[] centers in new[]{new float[0],new[]{-3.5f,0f,3.5f},new[]{-1f,1f}}){
var pose=new Pose(new Vector3(0,deck=="Main"?20:10,0),Quaternion.Euler(0,opposite?180:0,0));var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);var layout=FormatterServices.GetUninitializedObject(layoutType);layoutType.GetField("CarLaneCenters",F).SetValue(layout,centers);
foreach(float delta in new[]{0f,2f,-5f,-8f,-16f}){var original=fallback.Invoke(null,new[]{Enum.Parse(fallback.GetParameters()[0].ParameterType,deck),(object)lod,render,pose,delta,layout});Need(((Array)Get(original,"SupportedTypes")).Cast<object>().All(t=>(int)t==0),"Unknown receiver inherited deck material");}
foreach(int kind in new[]{0,1,2,4,5,8,16,17,32,64}){
var after=select.Invoke(null,new[]{(object)lod,render,pose,layout,Enum.ToObject(category,kind)});
bool recognized=kind==1||kind==5||kind==16;Need((after!=null)==recognized,"Receiver family selection");if(after==null){cases++;continue;}
var patches=(Array)Get(after,"Patches");var types=(Array)Get(after,"SupportedTypes");int wornLeaves=0;
void Visit(object node){Need(!(bool)Get(node,"IsGap"),"Material route deleted geometry");int bone=(int)Get(node,"Bone");if(bone<0){Visit(Get(node,"Negative"));Visit(Get(node,"Positive"));return;}var patch=patches.GetValue(bone);Need((bool)Get(patch,"PreparedUnit"),"Raw embedded-marking atlas remains reachable");var u=matrix.inverse.transpose*(Vector4)Get(patch,"U");var v=matrix.inverse.transpose*(Vector4)Get(patch,"V");var surface=Get(patch,"Surface");
if(kind==16){Need(ReferenceEquals(surface,ballast),"Ballast identity depends on deck");Need(Math.Abs(Math.Abs(u.x)*1240-249)<1e-3&&Math.Abs(Math.Abs(v.z)*992-249)<1e-3,"Ballast physical density");Need((bool)Get(patch,"RepeatU")&&(bool)Get(patch,"RepeatV"),"Ballast axis mode");Need((int)types.GetValue(bone)==16,"Ballast family contaminated");}
else{bool worn=ReferenceEquals(surface,wear);Need(worn||ReferenceEquals(surface,background),"Road identity depends on deck");Need(Math.Abs(u.x-124f/(worn?424:128))<1e-5&&Math.Abs(u.z)<1e-5,"Road transverse density");Need(Math.Abs(v.z+124.359375f/992)<1e-5,"Road longitudinal density");if(worn){float center=(3314.5f-3104-u.w*424)/124;Need(centers.Any(x=>Math.Abs(center-x)<1e-4),"Wear center mismatch");wornLeaves++;}}
checks++;}
Visit(Get(after,"Carriageway"));
if(kind==16)Visit(Get(after,"Sidewalk"));
else{
var walk=Get(after,"Sidewalk");var index=(int)Get(walk,"Bone");
Need(!(bool)Get(walk,"IsGap") && index>=0 && (int)types.GetValue(index)==0,"Walking asphalt inherited carriageway paving or lost geometry");
Need((wornLeaves>0)==(centers.Length>0),"No-lane wear semantics");
}
cases++;}
}
Debug.Log("PASS Lift03 shared deck receiver families: "+cases+" cases; "+checks+" reachable leaf checks; all 3 LODs, both deck heights and directions, source identities/PBR periods, actual lane centers/no-lane controls, unknown/mixed Original; independent motion retained. Not live-city motion or renderer acceptance.");EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
