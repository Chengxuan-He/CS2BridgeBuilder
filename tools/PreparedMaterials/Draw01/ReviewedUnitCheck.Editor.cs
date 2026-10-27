using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using BridgeBuilder.Bridges;

// Read-only checks of persisted native resources. No texture encoding or mip generation.
public static class Draw01ReviewedUnitCheck
{
 [Serializable] public sealed class Level { public bool lossy;public string channel;public int mip,width,height,pixels;public float maxLinearError,maxRepeatSeamError,maxOriginalAtlasBoundaryDifference; }
 [Serializable] public sealed class Report { public string sourceCid,unitKey;public int textures,mips;public bool nativeHeaders,completeGraph,gameVisualAcceptance;public Level[] levels; }
 static void Need(bool yes,string why){if(!yes)throw new InvalidDataException(why);}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  string root=Arg("-unitRoot"),game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
  var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Path.Combine(root,"extracted-source/manifest.json")));
  var baked=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Path.Combine(root,"report.json")));
  string styleRoot=Path.Combine(root,"output/Draw01");
  var module=PreparePaint.OpenCodec(game);
  ResolveEventHandler resolver=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  try
  {
   var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
   var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
   var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
   var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
   var hashReader=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
   var surfaceType=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
   var textureType=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
   var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
   var decode=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));
   var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
   Texture2D Load(string path)
   {
    using(var header=new BinaryReader(File.OpenRead(path)))
    {
     var asset=FormatterServices.GetUninitializedObject(textureType);
     textureType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{header});
     Need((int)textureType.GetProperty("width").GetValue(asset)>0,"Native texture header");
    }
    using var stream=File.OpenRead(path);
    var texture=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
    Need(stream.Position==stream.Length,"Native full texture consumed");return texture;
   }
   var reports=new List<Report>();
   foreach(var unit in manifest.units)
   {
   var result=baked.units.Single(r=>r.id==unit.id);
   var maps=new Dictionary<string,string>();
   using(var reader=new BinaryReader(File.OpenRead(Path.Combine(styleRoot,"RepeatUnits",result.key,"Repeat.Surface"))))
   {
    var surface=FormatterServices.GetUninitializedObject(surfaceType);
    surfaceType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(surface,new object[]{reader});
    int expectedTemplate=BitConverter.ToInt32(File.ReadAllBytes(unit.descriptor.path),2);
    Need((int)surfaceType.GetProperty("materialTemplateHash").GetValue(surface)==expectedTemplate,"Native receiver template");
    Need(!(bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface),"Native non-VT repeat");
    foreach(string method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
    Need(reader.ReadByte()==255,"Native bindings");
    for(int n=reader.ReadInt32();n>0;n--){string key=reader.ReadString();Need(reader.ReadByte()==255,"Reference");maps.Add(key,hashReader.Invoke(null,new object[]{reader}).ToString());}
    var keywords=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader})).Cast<string>().ToArray();
    Need(keywords.Contains("_TANGENTSPACE_OCTO"),"Original packed normal interpretation");
    Need(reader.BaseStream.Position==reader.BaseStream.Length,"Native full descriptor consumed");
   }
   Need(maps.Count==4&&maps["_ControlMask"]==result.channels.Single(c=>c.name=="_ControlMask").cid,"Actual grating channels and authored control");
   foreach(var cid in maps.Values)Need(File.Exists(Path.Combine(styleRoot,"Shared",cid+".Texture")),"Owner complete graph");
   var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Draw01RepeatUnitCheck.compute");int kernel=program.FindKernel("Check");
   var levels=new List<Level>();
   foreach(var product in result.channels)
   {
    var channel=unit.channels.Single(c=>c.name==product.name);var source=Load(channel.path);
    var output=Load(Path.Combine(styleRoot,"Shared",product.cid+".Texture"));
    try
    {
     Need(source.width==4096&&source.height==4096&&source.mipmapCount==13,"Full authored source mip chain");
     double sx=(unit.max[0]-unit.min[0])*source.width,sy=(unit.max[1]-unit.min[1])*source.height;
     bool lossy=output.graphicsFormat.ToString().Contains("BC7");
     int align=lossy?4:1;
     Need(output.width==((int)Math.Ceiling(sx)+align-1)/align*align&&output.height==((int)Math.Ceiling(sy)+align-1)/align*align,"Exact bounds and native block-aligned single period");
     Need(output.width/sx>=1&&output.height/sy>=1,"No density reduction");
     Need(output.mipmapCount==1+(int)Math.Floor(Math.Log(Math.Max(output.width,output.height),2)),"Complete unit mip chain");
     Need(result.periods.SequenceEqual(new[]{1,1}),"Physical period unchanged");
     Need(output.wrapMode==TextureWrapMode.Repeat&&output.filterMode==FilterMode.Trilinear&&output.anisoLevel==source.anisoLevel,"Native repeat sampling settings");
     Need(GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat)==GraphicsFormatUtility.IsSRGBFormat(output.graphicsFormat),"Channel color space");
     source.filterMode=FilterMode.Bilinear;source.wrapMode=TextureWrapMode.Clamp;
     program.SetTexture(kernel,"Source",source);program.SetTexture(kernel,"Product",output);
     program.SetVector("Region",new Vector4((float)unit.min[0],(float)unit.min[1],(float)(unit.max[0]-unit.min[0]),(float)(unit.max[1]-unit.min[1])));
     for(int mip=0;mip<output.mipmapCount;mip++)
     {
      int w=Math.Max(1,output.width>>mip),h=Math.Max(1,output.height>>mip);var data=new Vector4[w*h];
      using var buffer=new ComputeBuffer(w*h,16);program.SetBuffer(kernel,"Results",buffer);program.SetInts("Extent",w,h);program.SetInt("Mip",mip);
      float Check(int mode){program.SetInt("Mode",mode);program.Dispatch(kernel,(w+7)/8,(h+7)/8,1);buffer.GetData(data);return data.Max(v=>Mathf.Max(Mathf.Max(v.x,v.y),Mathf.Max(v.z,v.w)));}
      var level=new Level{lossy=lossy,channel=channel.name,mip=mip,width=w,height=h,pixels=w*h,maxLinearError=Check(0),maxRepeatSeamError=Check(1),maxOriginalAtlasBoundaryDifference=Check(2)};
      levels.Add(level);
     }
    }
    finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(output);}
   }
   var report=new Report{sourceCid=unit.sourceCid,unitKey=result.key,textures=maps.Count,mips=levels.Count,nativeHeaders=true,completeGraph=true,gameVisualAcceptance=false,levels=levels.ToArray()};
   File.WriteAllText(Path.Combine(root,"native-check-"+unit.id+".json"),JsonUtility.ToJson(report,true));
   Need(levels.All(l=>l.lossy||l.maxLinearError<=0.005f),"Converted pixel error exceeds RGBA8 quantization tolerance; see native-check.json");
   Need(levels.All(l=>l.maxRepeatSeamError<=0.0001f),"Native Repeat periodicity differs; see native-check.json");
   reports.Add(report);
   }
   Debug.Log("PASS Draw01 reviewed mixed-encoding units (BC7 loss separately reviewed): "+reports.Count+" units; "+reports.Sum(r=>r.mips)+" channel/mip levels; native headers; original period and density floor; no game visual acceptance.");
  }
  finally{AppDomain.CurrentDomain.AssemblyResolve-=resolver;PreparePaint.CloseCodec(module);}
 }
}
