using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class Draw01NearUnitCheck {
 [Serializable] public class Row {public string unit,channel,sampling;public int sourceFilter,sourceWrap,sourceAniso,preparedFilter,preparedWrap,preparedAniso;public int mip,samples,alphaHalfCrossings;public double rms,max,boundaryMax,finiteEdgeMax,maximumNormalAngleDegrees;public double[] maximumComponentError=new double[4];}
 [Serializable] public class Report {public string status="MEASURED_NOT_ACCEPTED";public Row[] rows;public string limitation="Explicit bilinear mip and native texture sampler SampleGrad comparisons at isotropic/4x/16x footprints; not full native material rendering. Original painted edge retained.";}
 static Texture2D Load(string path){var b=File.ReadAllBytes(path);int size=(int)typeof(PrepareRepeatUnits).GetMethod("NativeRawSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{b});using var s=new MemoryStream(b);return BridgeBuilder.Bridges.MovableTextureSource.Read(s,x=>PreparePaint.DecodeRaw(x,size));}
 public static void Run(){
  string Arg(string key)=>Draw01AppearancePartitionCheck.Argument(key);
  var m=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-unitManifest")));var products=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Arg("-unitReport")));
  var codec=PreparePaint.OpenCodec(Arg("-gameData"));var result=new List<Row>();
  try{var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/NearUnitCheck.compute");var points=new List<Vector2>();
   foreach(float u in new[]{0f,.00001f,.1f,.25f,.5f,.75f,.9f,.94f,.95f,.96f,.97f,.98f,.99999f,1f})foreach(float v in new[]{0f,.00001f,.001f,.1f,.25f,.5f,.75f,.9f,.999f,.99999f,1f})points.Add(new Vector2(u,v));
   using var input=new ComputeBuffer(points.Count,8);input.SetData(points);using var output=new ComputeBuffer(points.Count*2,16);int kernel=shader.FindKernel("Check");shader.SetBuffer(kernel,"Points",input);shader.SetBuffer(kernel,"Samples",output);shader.SetInt("Count",points.Count);
   foreach(var unit in m.units){var product=products.units.Single(p=>p.id==unit.id);shader.SetVector("Region",new Vector4((float)unit.min[0],(float)unit.min[1],(float)(unit.max[0]-unit.min[0]),(float)(unit.max[1]-unit.min[1])));
    foreach(var channel in unit.channels){var original=Load(channel.path);var preparedChannel=product.channels.Single(c=>c.name==channel.name);var cid=preparedChannel.cid;var lo=preparedChannel.contentMin??new[]{.25,0};var hi=preparedChannel.contentMax??new[]{.75,1};shader.SetVector("PreparedRegion",new Vector4((float)lo[0],(float)lo[1],(float)(hi[0]-lo[0]),(float)(hi[1]-lo[1])));var prepared=Load(Path.Combine(Arg("-unitOutput"),"Draw01","Shared",cid+".Texture"));
     try{var sourceFilter=original.filterMode;var sourceWrap=original.wrapMode;var preparedFilter=prepared.filterMode;var preparedWrap=prepared.wrapMode;QualitySettings.anisotropicFiltering=AnisotropicFiltering.Enable;shader.SetTexture(kernel,"Original",original);shader.SetTexture(kernel,"Prepared",prepared);
      foreach(int mode in new[]{0,1,4,16})
      for(int mip=0;mip<prepared.mipmapCount;mip++){original.filterMode=mode==0?FilterMode.Bilinear:sourceFilter;original.wrapMode=mode==0?TextureWrapMode.Clamp:sourceWrap;prepared.filterMode=mode==0?FilterMode.Bilinear:preparedFilter;prepared.wrapMode=mode==0?TextureWrapMode.Repeat:preparedWrap;shader.SetInt("NativeGrad",mode==0?0:1);shader.SetFloat("FootprintRatio",mode);shader.SetInt("Mip",mip);shader.Dispatch(kernel,(points.Count+63)/64,1,1);var samples=new Vector4[points.Count*2];output.GetData(samples);if(unit.id=="asphalt-full-half"&&channel.name=="_BaseColorMap"&&mip==0){int at=points.FindIndex(p=>p.x==.96f&&p.y==.5f);Debug.Log("Native road paint probe "+samples[at*2]);}var row=new Row{unit=unit.id,channel=channel.name,mip=mip,samples=points.Count,sampling=mode==0?"ExplicitBilinear":"NativeSamplerGrad"+mode,sourceFilter=(int)sourceFilter,sourceWrap=(int)sourceWrap,sourceAniso=original.anisoLevel,preparedFilter=(int)preparedFilter,preparedWrap=(int)preparedWrap,preparedAniso=prepared.anisoLevel};double sum=0;
       for(int i=0;i<points.Count;i++)for(int c=0;c<4;c++){double e=Math.Abs(samples[2*i][c]-samples[2*i+1][c]);sum+=e*e;row.maximumComponentError[c]=Math.Max(row.maximumComponentError[c],e);row.max=Math.Max(row.max,e);if(points[i].y<=.001f||points[i].y>=.999f)row.boundaryMax=Math.Max(row.boundaryMax,e);if(points[i].x<=.00001f||points[i].x>=.99999f)row.finiteEdgeMax=Math.Max(row.finiteEdgeMax,e);}
       for(int i=0;i<points.Count;i++){var a=samples[i*2];var b=samples[i*2+1];if(channel.name=="_BaseColorMap"&&(a.w>=.5f)!=(b.w>=.5f))row.alphaHalfCrossings++;if(channel.name=="_NormalMap"){Vector3 Normal(Vector4 c){float x=c.x*c.w*2-1,y=c.y*2-1;return new Vector3(x,y,Mathf.Sqrt(Mathf.Max(0,1-x*x-y*y))).normalized;}row.maximumNormalAngleDegrees=Math.Max(row.maximumNormalAngleDegrees,Vector3.Angle(Normal(a),Normal(b)));}}
       row.rms=Math.Sqrt(sum/(points.Count*4));result.Add(row);
      }
     }finally{UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(prepared);}
    }
   }
   File.WriteAllText(Arg("-checkReport"),JsonUtility.ToJson(new Report{rows=result.ToArray()},true));Debug.Log("MEASURED Draw01 near units: "+result.Count+" channel/mip rows; boundary acceptance not assumed.");
  }finally{PreparePaint.CloseCodec(codec);}
 }
}
