using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class MaterialBoundary119Check {
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

 int checks=0;bool baseline=args.Contains("-baseline");
 var reportType=mod.GetType("CS2Mods.Shared.Infrastructure.ExportReport",true);var report=New(reportType,false);
 var systemType=mod.GetType("BridgeBuilder.Systems.BridgeGenerationSystem",true);var system=FormatterServices.GetUninitializedObject(systemType);
 var method=systemType.GetMethod("TryBuildBridge",F);var parameters=method.GetParameters();var call=new object[parameters.Length];
 for(int i=0;i<call.Length;i++)call[i]=parameters[i].ParameterType.IsValueType?Activator.CreateInstance(parameters[i].ParameterType):null;
 call[3]="b11111111-2222-4333-8444-555555555555";call[6]=report;
 bool threw=false;try{bool result=(bool)method.Invoke(system,call);Check(!result,"invalid setup reported success");}catch(TargetInvocationException){threw=true;}
 Check(threw==baseline,"setup containment regression");
 if(!baseline){Check((int)reportType.GetProperty("FailedRoads",F).GetValue(report)==1,"failed setup count");Check(((string)reportType.GetProperty("FailureDetails",F).GetValue(report)).Contains("generation setup/cleanup"),"missing setup diagnostic");}checks++;
 var create=T("MovableRoadAssets").GetMethod("TryCreate",S);var level=T("MovableRoadAssets+Level");
 foreach(bool countFailure in new[]{true,false}){
 var fault=Activator.CreateInstance(typeof(FaultList<>).MakeGenericType(level));fault.GetType().GetField("CountFailure").SetValue(fault,countFailure);
 object[] request={"b11111111-2222-4333-8444-555555555555","b11111111-2222-4333-8444-555555555555 Mechanism","Draw02",fault,null,null,null};
 threw=false;try{bool result=(bool)create.Invoke(null,request);Check(!result && ((Array)request[5]).Length==0,"partial graph published");Check(((string)request[6]).Contains("fixture native metadata unavailable"),"failure cause absent");}catch(TargetInvocationException){threw=true;}
 Check(threw==baseline,"graph boundary regression");checks++;
 }

 var snapshot=mod.GetType("BridgeBuilder.Runtime.BridgeMaterialFileSnapshot",true);
 var fromException=typeof(System.Threading.Tasks.Task).GetMethods().Single(m=>m.Name=="FromException" && m.IsGenericMethodDefinition);
 var faulted=fromException.MakeGenericMethod(snapshot).Invoke(null,new object[]{new IOException("fixture required source bytes unreadable")});
 Set(system,"_materialRead",faulted);Set(system,"_materialCancellation",new System.Threading.CancellationTokenSource());
 Set(system,"_materialReady",(Action)(()=>{throw new Exception("faulted source must not run ready");}));
 int failures=0;Set(system,"_materialFailed",(Action<Exception>)(e=>{failures++;Check(e is IOException,"source error lost");throw new InvalidOperationException("fixture failed UI/report callback");}));
 var update=systemType.GetMethod("UpdateMaterialPreparation",F);threw=false;
 try{Check((bool)update.Invoke(system,null),"completed operation not processed");}catch(TargetInvocationException){threw=true;}
 Check(threw==baseline && failures==1 && Get(system,"_materialRead")==null,"callback containment or stale task");
 Check(!(bool)update.Invoke(system,null),"failure callback repeated");checks++;
 Debug.Log("PASS "+checks+" real compiled generation/material entry-point cases; baseline="+baseline+"; malformed external setup returns failure and diagnostic with empty outputs on candidate. Native renderer success and arbitrary material support are not asserted.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public sealed class FaultList<T>:System.Collections.Generic.IReadOnlyList<T>{public bool CountFailure;public int Count=>CountFailure?throw new InvalidOperationException("fixture native metadata unavailable"):1;public T this[int index]=>throw new InvalidOperationException("fixture native metadata unavailable");public System.Collections.Generic.IEnumerator<T> GetEnumerator()=>throw new InvalidOperationException("fixture native metadata unavailable");IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
}
