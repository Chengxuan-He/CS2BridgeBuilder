using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Lift03AtlasInspect {
 public static void Run(){
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  var module=PreparePaint.OpenCodec(Arg("-gameData"));
  try{ using var stream=File.OpenRead(Arg("-texture"));var source=MovableTextureSource.Read(stream,PreparePaint.Decompress);
   var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
   Graphics.Blit(source,rt);var previous=RenderTexture.active;RenderTexture.active=rt;
   var pixels=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,false);pixels.ReadPixels(new Rect(0,0,source.width,source.height),0,0);pixels.Apply(false,false);
   File.WriteAllBytes(Arg("-png"),pixels.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(source);
  }finally{PreparePaint.CloseCodec(module);}
 }
}
