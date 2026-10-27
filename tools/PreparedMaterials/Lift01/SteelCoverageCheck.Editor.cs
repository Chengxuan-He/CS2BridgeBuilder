using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

// Diagnostic only: export original channel pixels for semantic coverage review.
// No source asset, material product, or authored mip is changed.
public static class Lift01SteelCoverageCheck
{
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  string root=Arg("-unitRoot"),game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
  var input=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Path.Combine(root,"inputs/manifest.json")));
  var module=PreparePaint.OpenCodec(game);
  ResolveEventHandler resolver=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  try
  {
   var type=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll")).GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true);
   var decode=type.GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));
   var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
   foreach(var c in input.units.First().channels)
   {
    using var stream=File.OpenRead(c.path);
    var texture=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
    var render=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
    var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,true);
    var old=RenderTexture.active;
    try
    {
     Graphics.Blit(texture,render);RenderTexture.active=render;
     pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply(false);
     File.WriteAllBytes(Path.Combine(root,c.name+"-original.png"),pixels.EncodeToPNG());
    }
    finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(render);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(texture);}
   }
   Debug.Log("PASS Lift01 original channel diagnostic export; no material products modified.");
  }
  finally{AppDomain.CurrentDomain.AssemblyResolve-=resolver;PreparePaint.CloseCodec(module);}
 }
}
