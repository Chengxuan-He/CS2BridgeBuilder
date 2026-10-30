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
public static class Lift02BedUnitV3Check
{
 [Serializable] public sealed class Level { public string channel;public int mip,width,height,pixels;public float maxLinearError,maxRepeatSeamError,maxOriginalAtlasBoundaryDifference,maxFiniteGuardWrapDifference,maxAnisoOffGridError,maxAnisoSeamError,maxNormalAngleDegrees,codecPixelError,unencodedMappingError,codecFilteredError,unencodedNormalDegrees,codecNormalDegrees,unencodedSeamError,codecSeamError,sourceAlphaMin,productAlphaMin,alphaClassificationMismatches,interiorMappingError,interiorNormalDegrees;public bool alphaClassificationApplicable;public float[] offGridComponents,seamComponents; }
 [Serializable] public sealed class Report { public string sourceCid,unitKey;public int textures,mips;public bool nativeHeaders,completeGraph,gameVisualAcceptance,pixelCentersWithinTolerance,atlasBoundariesWithinTolerance;public Level[] levels; }
 [Serializable] public sealed class Bucket { public string channel; public int mip,aniso,bin,samples; public float minU,maxU; public float[] maxError,meanError; }
 [Serializable] public sealed class Distribution { public string scope; public Bucket[] buckets; }
 static void Need(bool yes,string why){if(!yes)throw new InvalidDataException(why);}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  string root=Arg("-unitRoot"),game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
  var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-manifest")));
  var baked=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Path.Combine(root,"report.json")));
  var unit=manifest.units.Single();var result=baked.units.Single();string styleRoot=Path.Combine(root,"Lift02");
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
   var maps=new Dictionary<string,string>();
   using(var reader=new BinaryReader(File.OpenRead(Path.Combine(styleRoot,"RepeatUnits",result.key,"Repeat.Surface"))))
   {
    var surface=FormatterServices.GetUninitializedObject(surfaceType);
    surfaceType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(surface,new object[]{reader});
    Need((int)surfaceType.GetProperty("materialTemplateHash").GetValue(surface)==1,"Original single-sided cutout template");
    Need(!(bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface),"Native non-VT repeat");
    foreach(string method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
    Need(reader.ReadByte()==255,"Native bindings");
    for(int n=reader.ReadInt32();n>0;n--){string key=reader.ReadString();Need(reader.ReadByte()==255,"Reference");maps.Add(key,hashReader.Invoke(null,new object[]{reader}).ToString());}
    var keywords=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader})).Cast<string>().ToArray();
    Need(keywords.Contains("_TANGENTSPACE_OCTO"),"Original packed normal interpretation");
    Need(reader.BaseStream.Position==reader.BaseStream.Length,"Native full descriptor consumed");
   }
   Need(maps.Count==4&&maps["_ControlMask"]=="729893d593df5d52ba911bc1e7eab77b","Actual floor channels and transparent zero");
   foreach(var cid in maps.Values)Need(File.Exists(Path.Combine(styleRoot,"Shared",cid+".Texture")),"Owner complete graph");
   var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift02BedUnitV3Check.compute");int kernel=program.FindKernel("Check");
   var levels=new List<Level>();var buckets=new List<Bucket>();
   foreach(var product in result.channels)
   {
    var channel=unit.channels.Single(c=>c.name==product.name);var source=Load(channel.path);
    var output=Load(Path.Combine(styleRoot,"Shared",product.cid+".Texture"));
    // Reconstruct the exact pre-BC7 candidate in memory once. Comparing this to
    // the persisted product at identical UV/LOD isolates codec error without
    // creating another candidate or replacing the original GPU reference.
    var extract=typeof(PrepareRepeatUnits).GetMethod("Extract",BindingFlags.Static|BindingFlags.NonPublic);
    var unencoded=(Texture2D)extract.Invoke(null,new object[]{AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute"),source,
      product.sourceOriginPixels[0],product.sourceOriginPixels[1],product.sourceRegionPixels[0],product.sourceRegionPixels[1],product.width,product.height,1,1,null,true,false,true,channel.name=="_NormalMap",null,unit.sourceGradientSampling});
    try
    {
     Need(source.width==4096&&source.height==4096&&source.mipmapCount==13,"Full authored source mip chain");
     Need(output.width==324&&output.height==512&&output.mipmapCount==10,"Original unit density and complete mip chain");
     Need(output.wrapMode==TextureWrapMode.Repeat&&output.filterMode==FilterMode.Trilinear&&output.anisoLevel==source.anisoLevel,"Native repeat sampling settings");
     Need(GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat)==GraphicsFormatUtility.IsSRGBFormat(output.graphicsFormat),"Channel color space");
     Texture.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
     program.SetTexture(kernel,"Source",source);program.SetTexture(kernel,"Product",output);program.SetTexture(kernel,"Unencoded",unencoded);
     program.SetVector("Region",new Vector4((float)unit.min[0],(float)unit.min[1],(float)(unit.max[0]-unit.min[0]),(float)(unit.max[1]-unit.min[1])));
     program.SetVector("Periods",new Vector4(result.periods[0],result.periods[1],0,0));
     program.SetVector("Content",new Vector4((float)product.contentMin[0],(float)product.contentMin[1],(float)product.contentMax[0],(float)product.contentMax[1]));
     program.SetVector("GridRegion",new Vector4((float)product.sourceOriginPixels[0]/source.width,(float)product.sourceOriginPixels[1]/source.height,(float)product.sourceRegionPixels[0]/source.width,(float)product.sourceRegionPixels[1]/source.height));
     program.SetVector("BaseExtent",new Vector4(output.width,output.height,0,0)); program.SetInt("NormalChannel",channel.name=="_NormalMap"?1:0);
     for(int mip=0;mip<output.mipmapCount;mip++)
     {
      int w=Math.Max(1,output.width>>mip),h=Math.Max(1,output.height>>mip);var data=new Vector4[w*h];
      using var buffer=new ComputeBuffer(w*h,16);program.SetBuffer(kernel,"Results",buffer);program.SetInts("Extent",w,h);program.SetInt("Mip",mip);program.SetFloat("SourceLod",(float)product.sourceLodByMip[mip]);
      float Check(int mode){program.SetInt("Mode",mode);program.Dispatch(kernel,(w+7)/8,(h+7)/8,1);buffer.GetData(data);return data.Max(v=>Mathf.Max(Mathf.Max(v.x,v.y),Mathf.Max(v.z,v.w)));}
      var level=new Level{channel=channel.name,mip=mip,width=w,height=h,pixels=w*h,maxLinearError=Check(0),maxRepeatSeamError=Check(1),maxOriginalAtlasBoundaryDifference=Check(2),maxFiniteGuardWrapDifference=Check(3),maxAnisoOffGridError=Check(4),maxAnisoSeamError=Check(5),maxNormalAngleDegrees=Check(6)};
      Need(level.maxFiniteGuardWrapDifference<=.000001f,"Finite V guard permits opposite-edge wrapping.");
      level.codecPixelError=Check(7);level.unencodedMappingError=Check(8);level.codecFilteredError=Check(9);
      level.unencodedNormalDegrees=Check(10);level.codecNormalDegrees=Check(11);
      level.unencodedSeamError=Check(12);level.codecSeamError=Check(13);
      Check(4);level.offGridComponents=new[]{data.Max(v=>v.x),data.Max(v=>v.y),data.Max(v=>v.z),data.Max(v=>v.w)};
      Check(5);level.seamComponents=new[]{data.Max(v=>v.x),data.Max(v=>v.y),data.Max(v=>v.z),data.Max(v=>v.w)};
      Check(14);level.sourceAlphaMin=data.Min(v=>v.x);level.productAlphaMin=data.Min(v=>v.y);level.alphaClassificationMismatches=data.Sum(v=>v.z);
      level.alphaClassificationApplicable=channel.name=="_BaseColorMap";
      if(!level.alphaClassificationApplicable)level.alphaClassificationMismatches=0;
      level.interiorMappingError=Check(15);level.interiorNormalDegrees=Check(16);
      levels.Add(level);
      foreach(int mode in new[]{17,18})
      {
       // Fixed sampling grid resolves each period even where the output mip has one texel.
       const int bw=1024,bh=32;var samples=new Vector4[bw*bh];
       using var grid=new ComputeBuffer(samples.Length,16);program.SetBuffer(kernel,"Results",grid);program.SetInts("Extent",bw,bh);program.SetInt("Mode",mode);
       program.Dispatch(kernel,bw/8,bh/8,1);grid.GetData(samples);
       for(int bin=0;bin<32;bin++)
       {
        var maximum=Vector4.zero;var sum=Vector4.zero;int count=0;
        for(int y=0;y<bh;y++)for(int x=bin*32;x<(bin+1)*32;x++){var error=samples[y*bw+x];maximum=Vector4.Max(maximum,error);sum+=error;count++;}
        buckets.Add(new Bucket{channel=channel.name,mip=mip,aniso=mode==18?16:4,bin=bin,samples=count,minU=bin/32f,maxU=(bin+1)/32f,maxError=new[]{maximum.x,maximum.y,maximum.z,maximum.w},meanError=new[]{sum.x/count,sum.y/count,sum.z/count,sum.w/count}});
       }
       program.SetBuffer(kernel,"Results",buffer);program.SetInts("Extent",w,h);
      }
     }
    }
    finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(unencoded);}
   }
   File.WriteAllText(Path.Combine(root,"bed-v3-filter-distribution.json"),JsonUtility.ToJson(new Distribution{scope="Original GPU SampleGrad versus existing encoded candidate; 32 texture-domain bins, fixed 1024x32 grid per channel/mip/anisotropy. No screen coverage or acceptance inferred.",buckets=buckets.ToArray()},true));
   var report=new Report{pixelCentersWithinTolerance=levels.All(l=>l.maxLinearError<=0.005f),atlasBoundariesWithinTolerance=levels.All(l=>l.maxOriginalAtlasBoundaryDifference<=0.005f),sourceCid=unit.sourceCid,unitKey=result.key,textures=maps.Count,mips=levels.Count,nativeHeaders=true,completeGraph=true,gameVisualAcceptance=false,levels=levels.ToArray()};
   File.WriteAllText(Path.Combine(root,"bed-v3-native-check.json"),JsonUtility.ToJson(report,true));
   Debug.Log("MEASURED Lift02 repeat candidate: native headers/graph and 30 source-channel/mip levels; pixel and original atlas boundary differences recorded; no acceptance inferred from periodicity.");
  }
  finally{AppDomain.CurrentDomain.AssemblyResolve-=resolver;PreparePaint.CloseCodec(module);}
 }
}
