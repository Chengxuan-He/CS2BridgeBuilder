using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Lift01SharedSourceProbe {
 [Serializable] public sealed class Input { public Unit[] units; }
 [Serializable] public sealed class Unit { public string key; public bool triangle; public float[] bounds,a,b,c; }
 [Serializable] public sealed class Row { public string texture,key;public int mip,width,height,differentSamples;public float maxError; }
 [Serializable] public sealed class Report { public string status="PASS",meaning="Original texture bytes are shared; plain affine UV plus whole-texture Repeat is not equivalent to region-local repeat/fold."; public int nativeTextures,allMipBytes,samplers;public Row[] samples;public bool baked=false,productionShaderVerified=false; }
 static void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
 public static void Run(){
  var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];string folder=Arg("-sampleRoot"),game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
  var module=PreparePaint.OpenCodec(game);ResolveEventHandler resolve=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};AppDomain.CurrentDomain.AssemblyResolve+=resolve;
  try{
   var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var t=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
   var compression=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll")).GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true);
   var decode=compression.GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
   var units=JsonUtility.FromJson<Input>(File.ReadAllText(Path.Combine(folder,"samplers.json"))).units;
   var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift01SharedSourceProbe.compute");int kernel=program.FindKernel("Compare");var rows=new List<Row>();var report=new Report{samplers=units.Length};
   using var output=new ComputeBuffer(289,16);program.SetBuffer(kernel,"Results",output);
   foreach(var path in Directory.GetFiles(folder,"*.Texture")){
    var bytes=File.ReadAllBytes(path);using(var reader=new BinaryReader(new MemoryStream(bytes))){var asset=FormatterServices.GetUninitializedObject(t);t.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});}
    using var stream=new MemoryStream(bytes);var texture=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
    try{
     Need(stream.Position==stream.Length,"Unconsumed original texture.");report.nativeTextures++;report.allMipBytes+=texture.GetRawTextureData<byte>().Length;program.SetTexture(kernel,"Source",texture);program.SetInt("Normal",Path.GetFileName(path).StartsWith("c292")?1:0);
     foreach(var u in units){program.SetInt("Triangle",u.triangle?1:0);program.SetVector("A",new Vector4(u.a[0],u.a[1],0,0));program.SetVector("B",new Vector4(u.b[0],u.b[1],0,0));program.SetVector("C",new Vector4(u.c[0],u.c[1],0,0));program.SetVector("Bounds",new Vector4(u.bounds[0],u.bounds[1],u.bounds[2],u.bounds[3]));
      for(int mip=0;mip<texture.mipmapCount;mip++) {program.SetInt("Mip",mip);program.Dispatch(kernel,5,1,1);var values=new Vector4[289];output.GetData(values);Need(values.All(v=>!float.IsNaN(v.x)),"Invalid source sample.");rows.Add(new Row{texture=Path.GetFileName(path),key=u.key,mip=mip,width=Math.Max(1,texture.width>>mip),height=Math.Max(1,texture.height>>mip),differentSamples=values.Count(v=>v.x>1e-6f),maxError=values.Max(v=>v.x)});}
     }
    }finally{UnityEngine.Object.DestroyImmediate(texture);}
   }
   report.samples=rows.ToArray();Need(rows.Any(r=>r.differentSamples>0),"Representative did not expose affine mismatch.");File.WriteAllText(Arg("-probeReport"),JsonUtility.ToJson(report,true));Debug.Log($"PASS Lift01 shared source probe: nativeTextures={report.nativeTextures}; samplers={report.samplers}; mipCases={rows.Count}; mismatchingCases={rows.Count(r=>r.differentSamples>0)}; baked=0; production shader support unverified.");
  }finally{AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
 }
}
