using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
public static class Lift05RegionRetirementAudit
{
 const BindingFlags I=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,S=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
 static object Get(object o,string n)=>o.GetType().GetField(n,I)?.GetValue(o)??o.GetType().GetProperty(n,I).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
 [Serializable]public class Region{public string key;public double minU,minV,maxU,maxV;}
 [Serializable]public class Domain{public int id,axes;public int[] faces,matchingRegions;}
 [Serializable]public class Fragment{public int face;public int[] indices;}
 [Serializable]public class Case{public float delta;public int[] removed,preparedDomains,frameStretchDomains,densityFrameDomains;public Fragment[] fragments;}
 [Serializable]public class Lod{public int lod;public Domain[] domains;public Case[] cases;}
 [Serializable]public class Report{public string candidate;public Region[] regions;public Lod[] levels;}
 public static void Run()
 {
  try{
   var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];string managed=Arg("-gameManaged"),inputs=Arg("-inputs");
   AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
   var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
   var st=T("MovableStructureTiles").GetMethod("NativeSurface",I).ReturnType;var surface=FormatterServices.GetUninitializedObject(st);
   var regions=((IEnumerable)T("MovableRepeatUnits").GetMethod("Regions",S).Invoke(null,new object[]{"Lift05","40c466ebabd798b8bca9674788deefa14c10490f9f011f5377c8e48535bb984e"})).Cast<object>().ToArray();
   var unitType=T("MovableStructurePacking").GetNestedType("Unit",I);var levels=new List<Lod>();
   for(int lod=0;lod<2;lod++){
    Vector3[] p;Vector2[] uv;int[] ix;
    using(var r=new BinaryReader(File.OpenRead(Path.Combine(inputs,"lod"+lod+".bin")))){int n=r.ReadInt32(),ni=r.ReadInt32();p=new Vector3[n];uv=new Vector2[n];ix=new int[ni];for(int v=0;v<n;v++)p[v]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int v=0;v<ni;v++)ix[v]=r.ReadInt32();for(int v=0;v<n;v++)uv[v]=new Vector2(r.ReadSingle(),r.ReadSingle());}
    string render=lod==0?"LiftBridge05 Mesh":"LiftBridge05_LOD1 Mesh";
    var recipe=T("MovableUvDomains").GetMethod("For",S).Invoke(null,new object[]{render,0});var items=((IEnumerable)Get(recipe,"Items")).Cast<object>().ToArray();var domains=new List<Domain>();
    for(int d=0;d<items.Length;d++){
     var item=items[d];var unit=Activator.CreateInstance(unitType,true);Set(unit,"Triangle",Get(item,"Triangle"));Set(unit,"Bounds",Get(item,"Bounds"));Set(unit,"A",uv[(int)Get(item,"A")]);Set(unit,"B",uv[(int)Get(item,"B")]);Set(unit,"C",uv[(int)Get(item,"C")]);
     domains.Add(new Domain{id=d,axes=(int)T("Lift05UvSampling").GetMethod("Axes",S).Invoke(null,new object[]{render,0,d}),faces=((int[])Get(item,"_data")).Skip((int)Get(item,"_start")).Take((int)Get(item,"_count")).ToArray(),matchingRegions=Enumerable.Range(0,regions.Length).Where(n=>(bool)regions[n].GetType().GetMethod("ContainsDomain",I).Invoke(regions[n],new[]{unit})).ToArray()});
    }
    var cases=new List<Case>();foreach(float delta in new[]{0f,.25f,1f,3f,6f,12f,30f}){
     var plan=T("Lift05StructurePlan").GetMethod("Create",S).Invoke(null,new object[]{lod,p,ix,delta,surface,new Vector2(0,.25f),new Vector2(1,.75f)});
     var positions=(Vector3[])Get(plan,"Positions");var stretch=new List<int>();var density=new List<int>();var removedMask=((bool[][])Get(plan,"RemoveTriangles"))[0];var fragmentFaces=new HashSet<int>(((IEnumerable)Get(plan,"Fragments")).Cast<object>().Select(f=>(int)Get(f,"Triangle")));
     for(int domain=0;domain<items.Length;domain++){var item=items[domain];int a=(int)Get(item,"A"),b=(int)Get(item,"B"),c=(int)Get(item,"C");var frame=Activator.CreateInstance(T("MovableUvFrame"),I,null,new object[]{p[a],p[b],p[c],uv[a],uv[b],uv[c],positions[a],positions[b],positions[c]},null);if((bool)Get(frame,"HasStretch"))stretch.Add(domain);
      var fs=((int[])Get(item,"_data")).Skip((int)Get(item,"_start")).Take((int)Get(item,"_count"));var members=fs.Where(f=>!removedMask[f]&&!fragmentFaces.Contains(f)).SelectMany(f=>ix.Skip(f*3).Take(3)).Distinct().ToArray();
      if(members.Length>0&&((bool)Get(frame,"HasStretch")||members.Any(vertex=>((Vector2)frame.GetType().GetMethod("Map",I).Invoke(frame,new object[]{p[vertex],positions[vertex],uv[vertex]})-uv[vertex]).sqrMagnitude>1e-12f)))density.Add(domain);
     }
     var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];cases.Add(new Case{delta=delta,frameStretchDomains=stretch.ToArray(),densityFrameDomains=density.ToArray(),removed=Enumerable.Range(0,mask.Length).Where(f=>mask[f]).ToArray(),preparedDomains=((IEnumerable)Get(plan,"PreparedDomains")).Cast<object>().Select(d=>(int)Get(d,"Domain")).ToArray(),fragments=((IEnumerable)Get(plan,"Fragments")).Cast<object>().Select(f=>new Fragment{face=(int)Get(f,"Triangle"),indices=(int[])Get(f,"Indices")}).ToArray()});
    }
    levels.Add(new Lod{lod=lod,domains=domains.ToArray(),cases=cases.ToArray()});
   }
   using var sha=System.Security.Cryptography.SHA256.Create();var report=new Report{candidate=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(mod.Location))).Replace("-",""),regions=regions.Select(r=>new Region{key=(string)Get(r,"BindingKey"),minU=(double)Get(r,"CoverageMinU"),minV=(double)Get(r,"CoverageMinV"),maxU=(double)Get(r,"CoverageMaxU"),maxV=(double)Get(r,"CoverageMaxV")}).ToArray(),levels=levels.ToArray()};
   File.WriteAllText(Path.Combine(inputs,"actual-plan-region-audit.json"),JsonUtility.ToJson(report,true));Debug.Log("PASS Lift05 actual region coverage: 14 Plans, all native matching domains and fragment-first masks exported.");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
