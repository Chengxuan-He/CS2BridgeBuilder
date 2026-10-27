using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BridgeBuilder.Bridges;
using UnityEngine;

// Reads saved descriptors and uploads their existing complete mip data, without encoding.
public static class Draw02CompositeNativeCheck
{
 static void Need(bool value,string why){if(!value)throw new Exception(why);}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  string root=Arg("-materials"),managed=Arg("-gameManaged");
  var module=PreparePaint.OpenCodec(Path.GetDirectoryName(managed));
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  try
  {
   var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
   var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
   var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
   var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
   var hash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
   var type=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
   var maps=new List<Dictionary<string,string>>();var templates=new List<int>();
   foreach(var path in Directory.GetFiles(Path.Combine(root,"RepeatUnits"),"*.Surface",SearchOption.AllDirectories))
   {
    var asset=FormatterServices.GetUninitializedObject(type);using var reader=new BinaryReader(File.OpenRead(path));
    type.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
    int t=(int)type.GetProperty("materialTemplateHash").GetValue(asset);templates.Add(t);
    Need(!(bool)type.GetProperty("isVTMaterial").GetValue(asset),"Non-VT repeat");
    var floats=(IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null,new object[]{reader});
    Need(floats.Contains("_Snow")&&(float)floats["_Snow"]==0,"Snow override");
    foreach(var n in new[]{"ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(n).Invoke(null,new object[]{reader});
    Need(reader.ReadByte()==255,"Bindings");var map=new Dictionary<string,string>();
    for(int n=reader.ReadInt32();n>0;n--){string name=reader.ReadString();Need(reader.ReadByte()==255,"CID marker");map.Add(name,hash.Invoke(null,new object[]{reader}).ToString());}
    var keys=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader})).Cast<string>().ToArray();
    Need(keys.Contains("_TANGENTSPACE_OCTO"),"Packed normals");
    Need(keys.Contains("_ALPHATEST_ON")== (t==14)&&keys.Contains("_DOUBLESIDED_ON")==(t==14),"Caller cutout/opaque semantics");
    Need(reader.BaseStream.Position==reader.BaseStream.Length,"Complete descriptor");maps.Add(map);
   }
   Need(templates.OrderBy(x=>x).SequenceEqual(new[]{0,14}),"Two caller descriptors");
   Need(maps[0].Count==4&&maps[0].All(p=>maps[1][p.Key]==p.Value),"All four payloads shared");
   var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
   var decode=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));
   var mode=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
   foreach(var cid in maps[0].Values)
   {
    using var stream=File.OpenRead(Path.Combine(root,"Shared",cid+".Texture"));
    var tex=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,mode}));
    try {Need(stream.Position==stream.Length,"Complete texture");Need(tex.width==311&&tex.height==827&&tex.mipmapCount==10,"Full density/mip chain");
     Need(tex.wrapMode==TextureWrapMode.Repeat&&tex.filterMode==FilterMode.Trilinear,"Native repeat filtering");
     for(int mip=0;mip<10;mip++){var read=UnityEngine.Rendering.AsyncGPUReadback.Request(tex,mip,TextureFormat.RGBA32);read.WaitForCompletion();Need(!read.hasError&&read.GetData<byte>().Length==Math.Max(1,311>>mip)*Math.Max(1,827>>mip)*4,"Complete RGBA mip bytes");}}
    finally{UnityEngine.Object.DestroyImmediate(tex);}
   }
   Debug.Log("PASS Draw02 composite: native template 0/14, Snow=0; 4 shared 311x827 textures, 40 complete mip levels; no encoding.");
  }
  catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}
  finally{PreparePaint.CloseCodec(module);}
 }
}
