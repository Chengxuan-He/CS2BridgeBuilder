using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Lift01FiniteWarpProbe
{
 [Serializable] public sealed class Output { public Vector4[] samples; }
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string root=args[Array.IndexOf(args,"-probeRoot")+1];
  var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Path.Combine(root,"private-manifest.json")));
  var unit=manifest.units.Single();var channel=unit.channels.Single(c=>c.name=="_MaskMap");
  using var stream=File.OpenRead(channel.path);var source=MovableTextureSource.Read(stream,b=>b);
  try {
   var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift01FiniteWarpProbe.compute");int k=shader.FindKernel("Inspect");
   var data=unit.finiteWarp;Vector4 Part(int offset,int count)=>new Vector4((float)data[offset],(float)data[offset+1],(float)data[offset+2],count==4?(float)data[offset+3]:0);
   shader.SetTexture(k,"Source",source);shader.SetInts("Extent",4,53);
   shader.SetVector("FiniteWarpShape",Part(0,4));shader.SetVector("FiniteWarpSeam",Part(4,3));
   shader.SetVector("FiniteWarpU0",Part(7,3));shader.SetVector("FiniteWarpV0",Part(10,3));
   shader.SetVector("FiniteWarpU1",Part(13,3));shader.SetVector("FiniteWarpV1",Part(16,3));
   var output=new Output{samples=new Vector4[4*53*2]};
   using var buffer=new ComputeBuffer(output.samples.Length,16);shader.SetBuffer(k,"Result",buffer);
   shader.Dispatch(k,1,7,1);buffer.GetData(output.samples);
   File.WriteAllText(Path.Combine(root,"gpu-warp-probe.json"),JsonUtility.ToJson(output,true));
   Debug.Log("PASS finite warp diagnostic readback; no production qualification.");
  }finally{UnityEngine.Object.DestroyImmediate(source);}
 }
}
