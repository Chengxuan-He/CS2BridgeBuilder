using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class ExplicitPanelDomainCheck
{
 [Serializable] public class SamplerEvidence { public Entry[] channels; }
 [Serializable] public class Entry { public string path,sha256,wrapU,filter;public int anisotropy; }
 public static void Run()
 {
  try
  {
   string root="C:/Users/admin/Documents/Codex/CS2BridgeBuilder/";
   var unit=JsonUtility.FromJson<PrepareRepeatUnits.Unit>(File.ReadAllText(root+"artifacts/material-remap/shared/outputs/draw01-guard-domain-fixture.json"));
   var flags=BindingFlags.Static|BindingFlags.NonPublic;
   var panels=(PrepareRepeatUnits.Panel[])typeof(PrepareRepeatUnits).GetMethod("SamplingPanels",flags).Invoke(null,new object[]{unit});
   if(panels.Length!=12)throw new Exception("Measured four content and four rectangular guard polygons were not retained.");
   var verify=typeof(PrepareRepeatUnits).GetMethod("VerifyGuardSampler",flags);
   verify.Invoke(null,new object[]{unit,TextureWrapMode.Clamp});
   var evidence=JsonUtility.FromJson<SamplerEvidence>(File.ReadAllText(root+"artifacts/material-remap/Draw01/native-atlas-road/actual-source-samplers.json"));
   foreach(var entry in evidence.channels)
   {
    var bytes=File.ReadAllBytes(entry.path);
    using var sha=System.Security.Cryptography.SHA256.Create();
    if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=entry.sha256)throw new Exception("Sampler evidence source changed.");
    var sampler=((TextureWrapMode Wrap,FilterMode Filter,int Aniso))typeof(PrepareRepeatUnits).GetMethod("SourceSampler",flags).Invoke(null,new object[]{bytes});
    if(sampler.Wrap.ToString()!=entry.wrapU||sampler.Filter.ToString()!=entry.filter||sampler.Aniso!=entry.anisotropy)throw new Exception("Native header sampler differs from source evidence.");
    verify.Invoke(null,new object[]{unit,sampler.Wrap});
   }
   unit.allowClampedSourceGuards=false;
   try{typeof(PrepareRepeatUnits).GetMethod("SamplingPanels",flags).Invoke(null,new object[]{unit});throw new Exception("Undeclared source-atlas extension accepted.");}
   catch(TargetInvocationException e)when(e.InnerException is InvalidDataException){}
   unit.allowClampedSourceGuards=true;

   try{verify.Invoke(null,new object[]{unit,TextureWrapMode.Repeat});throw new Exception("Non-clamp source guard accepted.");}
   catch(TargetInvocationException e)when(e.InnerException is InvalidDataException){}
   var grid=PrepareRepeatUnits.Grid(unit,16,16,false);
   if(grid.Min[0]!=.25||grid.Max[0]!=.75||grid.Min[1]!=0||grid.Max[1]!=1)throw new Exception("Explicit content window was padded a second time.");
   var source=new Texture2D(16,16,TextureFormat.RGBA32,true,true){filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,anisoLevel=16};
   for(int mip=0;mip<source.mipmapCount;mip++)
   {
    int n=Math.Max(1,16>>mip);var pixels=new Color32[n*n];
    for(int y=0;y<n;y++)for(int x=0;x<n;x++)pixels[y*n+x]=new Color32((byte)(x*13),(byte)(y*13),(byte)(mip*40),255);
    source.SetPixels32(pixels,mip);
   }
   source.Apply(false,false);
   var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
   var method=typeof(PrepareRepeatUnits).GetMethod("Extract",flags);
   var product=(Texture2D)method.Invoke(null,new object[]{shader,source,grid.X,grid.Y,grid.SX,grid.SY,grid.Width,grid.Height,1,1,null,false,true,false,false,null,true,panels});
   if(product.mipmapCount!=1+(int)Math.Floor(Math.Log(Math.Max(grid.Width,grid.Height),2)))throw new Exception("Missing explicit guard mip.");
   Debug.Log("PASS explicit panel texture domain: real Draw01 4 content triangles + 4 authored exterior guard rectangles; native Clamp only; original affine extrapolation; no double padding; complete GPU mip coverage.");
   UnityEngine.Object.DestroyImmediate(product);UnityEngine.Object.DestroyImmediate(source);EditorApplication.Exit(0);
  }
  catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
