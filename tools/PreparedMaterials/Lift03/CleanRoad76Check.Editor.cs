using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class Lift03CleanRoad76Check {
const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
static void Need(bool ok,string m){if(!ok)throw new Exception(m);}
public static void Run(){try{
var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];string managed=Path.Combine(Arg("-gameData"),"Managed");
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var a=Assembly.LoadFrom(Arg("-candidateDll"));var runtime=a.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);runtime.GetMethod("Initialize",F).Invoke(null,new object[]{Arg("-preparedRoot")});
var source=a.GetType("BridgeBuilder.Bridges.Lift03SourceRoadMapping",true).GetMethod("Create",F);var st=source.GetParameters()[2].ParameterType;var cache=(IDictionary)runtime.GetField("Surfaces",F).GetValue(null);
object Read(string key){var surface=FormatterServices.GetUninitializedObject(st);using(var r=new BinaryReader(File.OpenRead(Path.Combine(Arg("-preparedRoot"),"Materials/Lift03/RepeatUnits",key,"Repeat.Surface"))))st.GetMethod("ReadHeader",F).Invoke(surface,new object[]{r});cache.Add("Lift03/RepeatUnits/"+key+"/Repeat.Surface",surface);return surface;}
var background=Read("CleanRoadBackground");var wear=Read("CleanLaneWear");var native=FormatterServices.GetUninitializedObject(st);var layoutType=a.GetType("BridgeBuilder.Bridges.MovableSelectedRoadLayout",true);var apply=a.GetType("BridgeBuilder.Bridges.Lift03CleanRoadAppearance",true).GetMethod("Apply",F);var forLevel=a.GetType("BridgeBuilder.Bridges.Lift03RoadAppearance",true).GetMethod("ForLevel",F);var render=ScriptableObject.CreateInstance(forLevel.GetParameters()[2].ParameterType);var nativeSurfaces=Array.CreateInstance(st,1);nativeSurfaces.SetValue(native,0);render.GetType().GetProperty("surfaceAssets",F).SetValue(render,nativeSurfaces);int cases=0,checks=0;
foreach(int lod in new[]{0,1,2})foreach(float delta in new[]{0f,2f,-8f})foreach(bool transformed in new[]{false,true})foreach(float[] centers in new[]{new float[0],new[]{-3.5f,0f,3.5f},new[]{-1f,1f}}){
var pose=transformed?new Pose(new Vector3(3,1,-7),Quaternion.Euler(0,31,0)):Pose.identity;var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);var layout=FormatterServices.GetUninitializedObject(layoutType);layoutType.GetField("CarLaneCenters",F).SetValue(layout,centers);
var after=forLevel.Invoke(null,new[]{Enum.Parse(forLevel.GetParameters()[0].ParameterType,"Main"),(object)lod,render,pose,delta,layout});var patches=(Array)Get(after,"Patches");var types=(Array)Get(after,"SupportedTypes");
void Visit(object node){if((bool)Get(node,"IsGap"))return;int bone=(int)Get(node,"Bone");if(bone<0){Visit(Get(node,"Negative"));Visit(Get(node,"Positive"));return;}if(((int)types.GetValue(bone)&1)==0)return;var patch=patches.GetValue(bone);Need((bool)Get(patch,"PreparedUnit"),"Asphalt leaf still points at original atlas");var u=(Vector4)Get(patch,"U");var v=(Vector4)Get(patch,"V");var localU=matrix.inverse.transpose*u;var localV=matrix.inverse.transpose*v;bool worn=ReferenceEquals(Get(patch,"Surface"),wear);Need(worn||ReferenceEquals(Get(patch,"Surface"),background),"Unexpected material");Need(Math.Abs(localU.x-124f/(worn?424:128))<1e-5&&Math.Abs(localU.z)<1e-5,"Width changed source density");Need(Math.Abs(localV.z+124.359375f/992)<1e-5,"Longitudinal density");Need((bool)Get(patch,"RepeatV")&&(bool)Get(patch,"RepeatU")==!worn,"Native repeat axes");if(worn){float center=(3314.5f-3104-localU.w*424)/124;Need(centers.Any(x=>Math.Abs(center-x)<1e-4),"Wear not aligned actual lane");}checks++;}
Visit(Get(after,"Carriageway"));Visit(Get(after,"Sidewalk"));cases++;}
Debug.Log("PASS Lift03 clean appearance candidate: "+cases+" actual ForLevel all-LOD cases; "+checks+" reachable asphalt leaves; zero/positive/collapsed width, transformed pose, no/three/overlapping lanes; native descriptor identities and source gradients. Not city-render acceptance.");EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
