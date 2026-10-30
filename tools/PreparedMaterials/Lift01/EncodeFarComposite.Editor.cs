using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public static class Lift01EncodeFarComposite {
 public static void Run() {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];var root=Arg("-root");var input=Path.Combine(root,"artifacts/material-remap/Lift01/far-finite/role-safe-convex-probe");var output=Path.Combine(input,"native/Lift01");
  var codec=PreparePaint.OpenCodec(Arg("-gameData"));
  var methods=BindingFlags.Static|BindingFlags.NonPublic;var writer=typeof(PrepareRepeatUnits).GetMethod("Write",methods);var cidMethod=typeof(PrepareRepeatUnits).GetMethod("Cid",methods);
  string Store(string path,byte[] bytes){var cid=(string)cidMethod.Invoke(null,new object[]{bytes});writer.Invoke(null,new object[]{path,bytes,cid});return cid;}
  var bindings=new Dictionary<string,string>();
  try {
   foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap"}) {
    var texture=new Texture2D(512,512,TextureFormat.RGBA32,10,name!="_BaseColorMap"){filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,anisoLevel=16};
    try{texture.LoadRawTextureData(File.ReadAllBytes(Path.Combine(input,name+".rgba")));texture.Apply(false,false);var bytes=PreparePaint.Texture(texture);var cid=(string)cidMethod.Invoke(null,new object[]{bytes});Store(Path.Combine(output,"Shared",cid+".Texture"),bytes);bindings.Add(name,cid);}
    finally{UnityEngine.Object.DestroyImmediate(texture);}
   }
   var control="729893d593df5d52ba911bc1e7eab77b";var controlBytes=File.ReadAllBytes(Path.Combine(root,"assets/Materials/Lift01/Shared",control+".Texture"));writer.Invoke(null,new object[]{Path.Combine(output,"Shared",control+".Texture"),controlBytes,control});bindings.Add("_ControlMask",control);
   var original=File.ReadAllBytes(Path.Combine(input,"../Original.Surface"));var surface=(byte[])typeof(PrepareRepeatUnits).GetMethod("Surface",methods).Invoke(null,new object[]{original,bindings,1,1});var key=PreparePaint.Digest(surface);var identity=Store(Path.Combine(output,"RepeatUnits",key,"Repeat.Surface"),surface);
   File.WriteAllText(Path.Combine(input,"native-identities.txt"),"key="+key+"\nsurfaceCid="+identity+"\n"+string.Join("\n",bindings.Select(x=>x.Key+"="+x.Value)));
   Debug.Log("PASS native far local composite encoding; endpoint/quality acceptance remains incomplete.");
  }finally{PreparePaint.CloseCodec(codec);}
 }
}
