using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;
public static class Lift01Guard65Check {
public static void Run(){try{
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 var args=Environment.GetCommandLineArgs();string candidate=args[Array.IndexOf(args,"-candidate")+1],managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var asm=Assembly.LoadFrom(candidate);var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var surfaceType=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var native=Activator.CreateInstance(surfaceType,true);var unit=Activator.CreateInstance(surfaceType,true);
 object Field(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 int passed=0,negative=0;
 for(int lod=0;lod<3;lod++){float zeroDelta=lod==2?-3.28533935546875f:-3f;foreach(float delta in new[]{0f,zeroDelta,-6f,float.NaN,float.PositiveInfinity}){
  var type=asm.GetType("BridgeBuilder.Bridges."+(lod==2?"Lift01FarAppearance":"Lift01HalfFloorAppearance"),true);var method=type.GetMethod("Prepared",F);object[] call=lod==2?new object[]{native,Pose.identity,delta}:new object[]{lod,native,unit,Pose.identity,delta};
  try{var result=method.Invoke(null,call);if(float.IsNaN(delta)||float.IsInfinity(delta))throw new Exception("Nonfinite input fabricated successful output.");var kinds=(Array)Field(result,"SupportedTypes");var patches=(Array)Field(result,"Patches");if(kinds.Length!=patches.Length)throw new Exception("Schema mismatch");if(delta==zeroDelta){if(kinds.Length!=1||Convert.ToInt32(kinds.GetValue(0))!=0)throw new Exception("Collapsed inverse must retain Original");foreach(var name in new[]{"Carriageway","Sidewalk"}){var partition=Field(result,name);if((bool)Field(partition,"IsGap"))throw new Exception("Collapsed material discarded geometry");}}passed++;Debug.Log("LIFT01_GUARD65_PASS lod="+lod+" delta="+delta+" domains="+kinds.Length);}
  catch(TargetInvocationException e){if((float.IsNaN(delta)||float.IsInfinity(delta))&&e.InnerException is ArgumentOutOfRangeException){negative++;continue;}throw;}
 }
 }
 Debug.Log("LIFT01_GUARD65_COMPLETE allowed="+passed+" actualErrorNegative="+negative);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
