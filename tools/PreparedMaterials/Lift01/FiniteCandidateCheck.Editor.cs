using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Lift01FiniteCandidateCheck
{
 [Serializable] public sealed class Check {public string channel;public int mip,width,height;public float pixelError,filteredFiniteDomainError;}
 [Serializable] public sealed class Output {public bool productionQualified;public Check[] checks;public string sourceSampler;}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string root=args[Array.IndexOf(args,"-probeRoot")+1];
  string game=args[Array.IndexOf(args,"-gameData")+1];var codec=PreparePaint.OpenCodec(game);
  try {
   var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Path.Combine(root,"private-manifest.json")));
   var report=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Path.Combine(root,"report-v2.json")));
   var unit=manifest.units.Single();var result=report.units.Single();var checks=new List<Check>();
   var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift01FiniteCandidateCheck.compute");int k=shader.FindKernel("Inspect");
   var data=unit.finiteWarp;Vector4 Part(int offset,int count)=>new Vector4((float)data[offset],(float)data[offset+1],(float)data[offset+2],count==4?(float)data[offset+3]:0);
   shader.SetVector("FiniteWarpShape",Part(0,4));shader.SetVector("FiniteWarpSeam",Part(4,3));
   shader.SetVector("FiniteWarpU0",Part(7,3));shader.SetVector("FiniteWarpV0",Part(10,3));
   shader.SetVector("FiniteWarpU1",Part(13,3));shader.SetVector("FiniteWarpV1",Part(16,3));
   foreach(var product in result.channels) {
    var channel=unit.channels.Single(c=>c.name==product.name);
    using var sourceStream=File.OpenRead(channel.path);var source=MovableTextureSource.Read(sourceStream,b=>b);
    using var targetStream=File.OpenRead(Path.Combine(root,"candidate-v2/Lift01/Shared",product.cid+".Texture"));
    int rawSize=Enumerable.Range(0,product.mips).Sum(m=>Math.Max(1,product.width>>m)*Math.Max(1,product.height>>m)*4);
    var target=MovableTextureSource.Read(targetStream,b=>PreparePaint.DecodeRaw(b,rawSize));
    try {
     if(target.wrapMode!=TextureWrapMode.Clamp||target.anisoLevel!=source.anisoLevel||target.filterMode!=source.filterMode)throw new InvalidDataException("Original sampling settings differ.");
     shader.SetTexture(k,"Source",source);shader.SetTexture(k,"Product",target);
     shader.SetVector("ContentBounds",new Vector4((float)product.contentMin[0],(float)product.contentMin[1],(float)product.contentMax[0],(float)product.contentMax[1]));
     for(int mip=0;mip<target.mipmapCount;mip++) {
      int w=Math.Max(1,target.width>>mip),h=Math.Max(1,target.height>>mip);shader.SetInts("Extent",w,h);shader.SetInt("OutputMip",mip);
      float Measure(int mode) {
       int dw=mode==0?w:257,dh=mode==0?h:257;var values=new Vector4[dw*dh];
       using var buffer=new ComputeBuffer(values.Length,16);shader.SetBuffer(k,"Result",buffer);shader.SetInts("DispatchExtent",dw,dh);shader.SetInt("Mode",mode);
       shader.Dispatch(k,(dw+7)/8,(dh+7)/8,1);buffer.GetData(values);
       return values.Max(v=>Mathf.Max(Mathf.Max(v.x,v.y),Mathf.Max(v.z,v.w)));
      }
      checks.Add(new Check{channel=channel.name,mip=mip,width=w,height=h,pixelError=Measure(0),filteredFiniteDomainError=Measure(1)});
     }
    }finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
   }
   File.WriteAllText(Path.Combine(root,"native-sampling-v2.json"),JsonUtility.ToJson(new Output{productionQualified=false,checks=checks.ToArray(),sourceSampler="Original native aniso/filter/wrap retained; no forced bilinear sampler."},true));
   Debug.Log("PASS finite candidate measurements recorded; acceptance remains pending.");
  }finally{PreparePaint.CloseCodec(codec);}
 }
}
