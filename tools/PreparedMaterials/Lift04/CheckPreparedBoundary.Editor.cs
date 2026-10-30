using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;
public static class Lift04PreparedBoundary {
 [Serializable] public class Config {public Item[] items;}
 [Serializable] public class Item {public string name,source,prepared;public float[] min,max;}
 [Serializable] public class Row {public string channel;public int mip;public Color[] original,repeated;}
 [Serializable] public class Report {public string scope;public Row[] records;}
 public static void Run(){
  string Arg(string n){var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,n)+1];}
  var config=JsonUtility.FromJson<Config>(File.ReadAllText(Arg("-boundaryConfig")));
  var module=PreparePaint.OpenCodec(Arg("-gameData"));var rows=new System.Collections.Generic.List<Row>();
  var shader=Shader.Find("Hidden/Lift04PreparedBoundary");var mat=new Material(shader);
  Texture2D Load(string p){var bytes=File.ReadAllBytes(p);int size=(int)typeof(PrepareRepeatUnits).GetMethod("NativeRawSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{bytes});using var s=new MemoryStream(bytes);return MovableTextureSource.Read(s,b=>PreparePaint.DecodeRaw(b,size));}
  try {foreach(var item in config.items){var source=Load(item.source);var prepared=Load(item.prepared);
   mat.SetTexture("_Prepared",prepared);mat.SetVector("_Bounds",new Vector4(.4697265625f,.8251953125f,.62548828125f,.9814453125f));mat.SetVector("_Content",new Vector4(item.min[0],item.min[1],item.max[0],item.max[1]));
   var target=new RenderTexture(64,4,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);var read=new Texture2D(64,4,TextureFormat.RGBAFloat,false,true);
   Color[] Sample(int mode){mat.SetInt("_Mode",mode);Graphics.Blit(source,target,mat);var old=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,64,4),0,0);read.Apply(false,false);RenderTexture.active=old;return read.GetPixels();}
   for(int mip=0;mip<source.mipmapCount;mip++){mat.SetFloat("_Mip",mip);rows.Add(new Row{channel=item.name,mip=mip,original=Sample(0),repeated=Sample(1)});}
   UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(prepared);UnityEngine.Object.DestroyImmediate(read);target.Release();UnityEngine.Object.DestroyImmediate(target);
  } File.WriteAllText(Arg("-boundaryReport"),JsonUtility.ToJson(new Report{scope="Actual native atlas versus finished offline unit, 64 points on each boundary at every source mip. Explicit isotropic LOD; anisotropic and renderer/cold-load acceptance not covered.",records=rows.ToArray()},true));}
  finally{PreparePaint.CloseCodec(module);}
 }
}
