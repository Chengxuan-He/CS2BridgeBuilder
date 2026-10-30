using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Offline GPU sampling of real source and prepared complete mip chains.
public static class PedestrianHalfUnitCheck
{
    [Serializable] public sealed class Row { public string unit,channel; public int mip,cases; public float sourceLod,maximum,interiorMaximum,finiteEdgeMaximum,repeatSeamMaximum,normalDegrees; }
    [Serializable] public sealed class Report { public string scope;public Row[] rows; }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-manifest")));
        var products=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Arg("-products")));
        var codec=PreparePaint.OpenCodec(Arg("-gameData"));var rows=new List<Row>();
        try
        {
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/HalfUnitCheck.compute");int kernel=shader.FindKernel("Compare");
            foreach(var unit in manifest.units)
            foreach(var channel in unit.channels)
            {
                var product=products.units.Single(u=>u.id==unit.id).channels.Single(c=>c.name==channel.name);
                var source=Load(channel.path);var target=Load(Path.Combine(Arg("-output"),"Shared",product.cid+".Texture"));
                try
                {
                    source.wrapMode=TextureWrapMode.Clamp;source.filterMode=FilterMode.Trilinear;
                    target.wrapMode=TextureWrapMode.Repeat;target.filterMode=FilterMode.Trilinear;
                    shader.SetTexture(kernel,"Source",source);shader.SetTexture(kernel,"Candidate",target);
                    shader.SetInt("NormalChannel",channel.name=="_NormalMap"?1:0);
                    shader.SetVector("Region",new Vector4((float)unit.min[0],(float)unit.min[1],(float)(unit.max[0]-unit.min[0]),(float)(unit.max[1]-unit.min[1])));
                    double contentWidth=product.contentMax[0]-product.contentMin[0],contentHeight=product.contentMax[1]-product.contentMin[1];
                    shader.SetVector("Content",new Vector4((float)product.contentMin[0],(float)product.contentMin[1],(float)contentWidth,(float)contentHeight));
                    for(int mip=0;mip<target.mipmapCount;mip++)
                    {
                        var points=new List<Vector4>();
                        foreach(float x in new[]{.1f,.3f,.5f,.7f,.9f})foreach(float y in new[]{.1f,.3f,.5f,.7f,.9f})points.Add(new Vector4(x,y,mip,0));
                        foreach(float t in new[]{.1f,.3f,.5f,.7f,.9f})foreach(float offset in new[]{0f,.125f,.5f,1f,2f})
                        {
                            float dx=Mathf.Min(.1f,(float)(offset*(1<<mip)/(target.width*contentWidth))),dy=Mathf.Min(.1f,(float)(offset*(1<<mip)/(target.height*contentHeight)));
                            points.Add(new Vector4(dx,t,mip,1));points.Add(new Vector4(1-dx,t,mip,1));
                            points.Add(new Vector4(t,dy,mip,2));points.Add(new Vector4(t,1-dy,mip,2));
                        }
                        double footprint=Math.Max((unit.max[0]-unit.min[0])*source.width/(Math.Max(1,target.width>>mip)*contentWidth),
                            (unit.max[1]-unit.min[1])*source.height/(Math.Max(1,target.height>>mip)*contentHeight));
                        float sourceLod=Mathf.Clamp((float)(Math.Log(footprint)/Math.Log(2)),0,source.mipmapCount-1);
                        shader.SetFloat("SourceLod",sourceLod);
                        using var input=new ComputeBuffer(points.Count,16);using var output=new ComputeBuffer(points.Count,16);using var angles=new ComputeBuffer(points.Count,4);
                        input.SetData(points.ToArray());shader.SetBuffer(kernel,"Samples",input);shader.SetBuffer(kernel,"Differences",output);shader.SetBuffer(kernel,"Angles",angles);
                        shader.SetInt("Count",points.Count);shader.Dispatch(kernel,(points.Count+63)/64,1,1);
                        var values=new Vector4[points.Count];var normals=new float[points.Count];output.GetData(values);angles.GetData(normals);
                        var row=new Row{unit=unit.id,channel=channel.name,mip=mip,cases=points.Count,sourceLod=sourceLod};
                        for(int i=0;i<values.Length;i++)
                        {
                            float error=Mathf.Max(values[i].x,values[i].y,values[i].z,values[i].w);row.maximum=Mathf.Max(row.maximum,error);row.normalDegrees=Mathf.Max(row.normalDegrees,normals[i]);
                            if(points[i].w==0)row.interiorMaximum=Mathf.Max(row.interiorMaximum,error);
                            else if(points[i].w==1)row.finiteEdgeMaximum=Mathf.Max(row.finiteEdgeMaximum,error);
                            else row.repeatSeamMaximum=Mathf.Max(row.repeatSeamMaximum,error);
                        }
                        rows.Add(row);
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
            }
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{scope="Native GPU bilinear/trilinear source mip footprint and prepared half-unit comparisons; measurements do not assert visual acceptance.",rows=rows.ToArray()},true));
            Debug.Log("PASS half-unit GPU measurements: "+rows.Count+" channel/mip cases.");
        }
        finally{PreparePaint.CloseCodec(codec);}
    }
    static Texture2D Load(string path)
    {
        var bytes=File.ReadAllBytes(path);int w=BitConverter.ToUInt16(bytes,2),h=BitConverter.ToUInt16(bytes,4),mips=bytes[8];
        var format=(GraphicsFormat)bytes[9];int block=(int)GraphicsFormatUtility.GetBlockSize(format),bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format),size=0;
        for(int i=0;i<mips;i++){size+=((w+bw-1)/bw)*((h+bh-1)/bh)*block;w=Math.Max(1,w/2);h=Math.Max(1,h/2);}
        using var stream=new MemoryStream(bytes);return MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,size));
    }
}
