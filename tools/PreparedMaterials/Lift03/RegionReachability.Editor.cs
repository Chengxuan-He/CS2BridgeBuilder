using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public static class Lift03RegionReachability
{
 [Serializable] public sealed class Row { public string type; public int lod; public int[] fragments; public bool embeddedMatchesCurrent; }
 [Serializable] public sealed class Report { public string assembly; public Row[] rows; }
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];
  string dll=Arg("-candidateDll"),managed=Path.Combine(Arg("-gameData"),"Managed");
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  var a=Assembly.LoadFrom(dll);var rows=new List<Row>();const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance;
  foreach(string name in new[]{"Frames","EndBeams","JoinedShells","TallSupports","SlopedSupports"})
  {
   string resource="Types_Lift03_Lift03"+name+"_g_cs";
   var embedded=a.GetManifestResourceNames().Single(x=>x.EndsWith(resource+".bin"));
   byte[] bytes;using(var stream=a.GetManifestResourceStream(embedded))using(var b=new MemoryStream()){stream.CopyTo(b);bytes=b.ToArray();}
   string repo=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Arg("-fixture")),"../../.."));
   bool matches=bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(repo,"assets/HardcodedNumbers/Lift03",resource+".bin")));
   if(!matches)throw new InvalidDataException("Candidate structural resource differs: "+resource);
   var t=a.GetType("BridgeBuilder.Bridges.Lift03"+name,true);
   for(int lod=0;lod<2;lod++)
   {
    var fragments=(IEnumerable)t.GetField("Fragments"+lod,flags).GetValue(null);
    var faces=new List<int>();foreach(var f in fragments)faces.Add((int)f.GetType().GetField("Triangle",flags).GetValue(f));
    rows.Add(new Row{type=name,lod=lod,fragments=faces.ToArray(),embeddedMatchesCurrent=matches});
   }
  }
  File.WriteAllText(Path.Combine(Path.GetDirectoryName(Arg("-fixture")),"structural-fragment-membership.json"),JsonUtility.ToJson(new Report{assembly=dll,rows=rows.ToArray()},true));
 }
}
