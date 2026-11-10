using System.Reflection;
using System.Text;
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;
using UnityEngine;
public static class Lift01Plans62Check { public static void Run(){try{var cmd=Environment.GetCommandLineArgs();var args=new[]{cmd[Array.IndexOf(cmd,"-candidate")+1],"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed","E:/CS2BridgeBuilder/artifacts/uv-density/actual-plans.bin","E:/CS2BridgeBuilder/artifacts/material-remap/Lift01/truss62/final-r4/native-plans.txt"};
const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(args[1],new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(args[0]);
T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F)!.GetValue(o)!;
var report=new List<object>();
using var r=new BinaryReader(File.OpenRead(args[2]));
if(r.ReadInt32()!=2)throw new Exception("Fixture version");int count=r.ReadInt32();
for(int ci=0;ci<count;ci++){
 string Text()=>Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
 string name=Text(),impl=Text();int family=r.ReadInt32(),lod=r.ReadInt32(),mesh=r.ReadInt32(),nv=r.ReadInt32(),ni=r.ReadInt32();
 var p=new Vector3[nv];for(int i=0;i<nv;i++)p[i]=new(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 for(int i=0;i<nv;i++){r.ReadSingle();r.ReadSingle();}var ix=new int[ni];for(int i=0;i<ni;i++)ix[i]=r.ReadInt32();
 if(!name.StartsWith("LiftBridge01")||impl=="Identity")continue;
 foreach(float delta in new[]{-1.25f,0,2,8,10.5f}){
 try{var sel=impl.Split('#');var m=mod.GetType("BridgeBuilder.Bridges."+sel[0])!.GetMethod(sel.Length>1?sel[1]:"Create",S)!;
 object Arg(ParameterInfo a)=>a.ParameterType==typeof(Vector3[])?(Vector3[])p.Clone():a.ParameterType==typeof(int[])?ix:a.ParameterType==typeof(int[][])?new[]{ix}:a.ParameterType==typeof(float)?delta:a.Name=="lod"?lod:a.Name=="mesh"?mesh:a.Name is "family" or "part"?family:a.Name=="footing"?family==1:a.Name=="column"?family==0:throw new Exception("Argument "+a.Name);
 var plan=m.Invoke(null,m.GetParameters().Select(Arg).ToArray())!;
 if(impl=="Lift01FarTowerCore")plan=mod.GetType("BridgeBuilder.Bridges.Lift01FarEndLips")!.GetMethod("Apply",S)!.Invoke(null,new[]{plan,(object)delta})!;
var mask=Get<bool[][]>(plan,"RemoveTriangles")[0];var fragments=Get<Array>(plan,"Fragments");var parents=fragments.Cast<object>().Select(f=>Get<int>(f,"Triangle")).ToArray();
 var resurrected=parents.Distinct().Count(i=>!mask[i]);
 var pos=Get<Vector3[]>(plan,"Positions");
 if(impl=="Lift01DetailedStructure") {var src=Get<Vector3[]>(plan,"SourcePositions");var edges=Get<Array>(plan,"AddedVertices");var copies=Get<int[]>(plan,"SourceCopies");using var fw=new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(args[3])!,"frame-witness-"+lod+"-"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+".bin")));foreach(var pair in Get<KeyValuePair<int,Matrix4x4>[]>(plan,"FrameTransforms")){int i=pair.Key;Vector3 original;if(i<src.Length)original=src[i];else if(i<src.Length+edges.Length){var edge=edges.GetValue(i-src.Length)!;original=Vector3.LerpUnclamped(src[Get<int>(edge,"A")],src[Get<int>(edge,"B")],Get<float>(edge,"Fraction"));}else original=src[copies[i-src.Length-edges.Length]];fw.Write(i);fw.Write(pair.Value.m00);fw.Write(original.x);fw.Write(original.y);fw.Write(original.z);fw.Write(pos[i].x);fw.Write(pos[i].y);fw.Write(pos[i].z);}}

 if(impl=="Lift01DetailedStructure") {using var w=new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(args[3])!,"positions-"+lod+"-"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+".bin")));foreach(var q in pos){w.Write(q.x);w.Write(q.y);w.Write(q.z);}}

 report.Add(new{name,impl,lod,mesh,delta,sourceFaces=ni/3,removed=mask.Count(x=>x),retained=mask.Count(x=>!x),fragments=fragments.Length,outputTriangles=mask.Count(x=>!x)+fragments.Cast<object>().Sum(f=>Get<int[]>(f,"Indices").Length/3),fragmentParents=parents.Distinct().Count(),fragmentParentIds=parents.Distinct().ToArray(),unremovedFragmentParents=resurrected,finite=pos.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),removedWithoutFragment=Enumerable.Range(0,mask.Length).Where(i=>mask[i]&&!parents.Contains(i)).ToArray()});
 }catch(Exception e){report.Add(new{name,impl,lod,mesh,delta,error=e.ToString()});}
 }
}
File.WriteAllLines(args[3],report.Select(o=>o.ToString()));Debug.Log("LIFT01_PLANS62_COMPLETE count="+report.Count);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}}
