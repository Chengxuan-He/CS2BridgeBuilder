using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Lift02SourceSeamLimit
{
 [Serializable] public class Row { public string channel;public int mip,aniso; public float epsilon,sourceTexelDistance;public float[] maxJump,meanJump;public float lowerBound;public float alphaMinLeft,alphaMinRight,alphaClassificationFlips; }
 [Serializable] public class Report { public string scope; public string sourceCid;public Row[] rows; }
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-manifest")));var unit=manifest.units.Single();
  string game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  var module=PreparePaint.OpenCodec(game);
  try
  {
   var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));var decode=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
   var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift02SourceSeamLimit.compute");int kernel=program.FindKernel("Check");var rows=new List<Row>();
   Texture.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
   foreach(var channel in unit.channels)
   {
    using var input=File.OpenRead(channel.path);var source=MovableTextureSource.Read(input,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
    try
    {
     program.SetTexture(kernel,"Source",source);program.SetVector("Region",new Vector4((float)unit.min[0],(float)unit.min[1],(float)(unit.max[0]-unit.min[0]),(float)(unit.max[1]-unit.min[1])));
     // Exact same physical derivatives as the 324x512 candidate comparison.
     program.SetVector("GridScale",new Vector4(322f/source.width,512f/source.height,0,0));program.SetVector("BaseExtent",new Vector4(324,512,0,0));
     using var buffer=new ComputeBuffer(1024,16);var data=new Vector4[1024];program.SetBuffer(kernel,"Results",buffer);
     foreach(int aniso in new[]{4,16})for(int mip=0;mip<source.mipmapCount;mip++)foreach(float epsilon in new[]{.01f,.001f,.0001f,.00001f,0f})
     {
      program.SetInt("Mip",mip);program.SetInt("Aniso",aniso);program.SetFloat("Epsilon",epsilon);program.SetInt("Mode",0);program.Dispatch(kernel,16,1,1);buffer.GetData(data);
      float[] max={data.Max(v=>v.x),data.Max(v=>v.y),data.Max(v=>v.z),data.Max(v=>v.w)};
      var row=new Row{channel=channel.name,mip=mip,aniso=aniso,epsilon=epsilon,sourceTexelDistance=epsilon*322,maxJump=max,meanJump=new[]{data.Average(v=>v.x),data.Average(v=>v.y),data.Average(v=>v.z),data.Average(v=>v.w)},lowerBound=max.Max()/2};
      program.SetInt("Mode",1);program.Dispatch(kernel,16,1,1);buffer.GetData(data);row.alphaMinLeft=data.Min(v=>v.x);row.alphaMinRight=data.Min(v=>v.y);row.alphaClassificationFlips=channel.name=="_BaseColorMap"?data.Sum(v=>v.z):0;rows.Add(row);
     }
    }finally{UnityEngine.Object.DestroyImmediate(source);}
   }
   File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{sourceCid=unit.sourceCid,scope="Original source paired left/right seam limits at identical finite V and physical SampleGrad. Epsilon 0 evaluates boundary limits; D/2 is a component-wise necessary bound for a continuous periodic sampler, not acceptance authorization. Raw PBR encoded components, not perceptual normal angles.",rows=rows.ToArray()},true));
   Debug.Log("PASS source seam limit measurement: "+rows.Count+" rows; no rebake.");
  }finally{PreparePaint.CloseCodec(module);}
 }
}
