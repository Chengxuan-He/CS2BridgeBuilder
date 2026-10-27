using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using BridgeBuilder.Bridges;

// Original native descriptor/key check only; no image generation or sampling probe.
public static class Draw03NativeBindingsCheck
{
 [Serializable] public class Source { public string cid,sha256,descriptorBase64;public int template; }
 [Serializable] public class Record { public string key;public int source;public bool triangle;public float[] bounds,a,b,c; }
 [Serializable] public class Table { public Source[] sources;public Record[] samplingRecords; }
 public static void Run() {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  var table=JsonUtility.FromJson<Table>(File.ReadAllText(Arg("-nativeBindings")));string managed=Arg("-gameManaged");
  ResolveEventHandler handler=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  AppDomain.CurrentDomain.AssemblyResolve+=handler;
  try {
   var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var st=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
   foreach(var s in table.sources) {
    byte[] bytes=Convert.FromBase64String(s.descriptorBase64);if(PreparePaint.Digest(bytes)!=s.sha256)throw new InvalidDataException("Original descriptor digest differs");
    var native=FormatterServices.GetUninitializedObject(st);using var reader=new BinaryReader(new MemoryStream(bytes));
    st.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(native,new object[]{reader});
    if((int)st.GetProperty("materialTemplateHash").GetValue(native)!=s.template)throw new InvalidDataException("Original template differs");
   }
   foreach(var r in table.samplingRecords) {
    Vector2 V(float[] v)=>new Vector2(v[0],v[1]);
    string key=MovableStructureKey.For(table.sources[r.source].sha256,r.triangle,new Vector4(r.bounds[0],r.bounds[1],r.bounds[2],r.bounds[3]),V(r.a),V(r.b),V(r.c));
    if(key!=r.key)throw new InvalidDataException("Native sampling table/runtime key mismatch");
   }
   Debug.Log("PASS Draw03 original bindings: "+table.sources.Length+" native descriptors; "+table.samplingRecords.Length+" exact production sampler keys; no texture writes.");
  } finally {AppDomain.CurrentDomain.AssemblyResolve-=handler;}
 }
}
