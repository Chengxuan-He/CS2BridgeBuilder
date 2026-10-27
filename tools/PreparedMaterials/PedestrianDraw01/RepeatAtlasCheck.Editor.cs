using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Finite offline original-atlas comparison. Never ships as a runtime shader.
public static class PedestrianRepeatAtlasCheck
{
    [Serializable] public sealed class Row { public string unit,channel; public int mip,cases,edgeCases,different,edgeDifferent; public float maximum,edgeMaximum; }
    [Serializable] public sealed class Result { public string status,scope; public Row[] rows; }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs(); string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-manifest")));
        var report=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Arg("-products")));
        var codec=PreparePaint.OpenCodec(Arg("-gameData")); var rows=new List<Row>();
        try
        {
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/PedestrianRepeatAtlasCheck.compute");
            int kernel=shader.FindKernel("Compare");
            foreach(var unit in manifest.units)
            {
                var product=report.units.Single(x=>x.id==unit.id);
                foreach(var channel in unit.channels)
                {
                    var prepared=product.channels.Single(x=>x.name==channel.name);
                    var source=Load(channel.path); var candidate=Load(Path.Combine(Arg("-output"),"Shared",prepared.cid+".Texture"));
                    try
                    {
                        if(source.mipmapCount!=13||candidate.mipmapCount!=prepared.mips)throw new InvalidDataException("Mip chain mismatch.");
                        shader.SetTexture(kernel,"Source",source);shader.SetTexture(kernel,"Candidate",candidate);
                        shader.SetVector("Region",new Vector4((float)unit.min[0],(float)unit.min[1],(float)(unit.max[0]-unit.min[0]),(float)(unit.max[1]-unit.min[1])));
                        shader.SetVector("Periods",new Vector4(product.periods[0],product.periods[1],0,0));
                        for(int mip=0;mip<source.mipmapCount;mip++)
                        {
                            var samples=new List<Vector4>();
                            foreach(float x in new[]{.2f,.4f,.6f,.8f})foreach(float y in new[]{.2f,.4f,.6f,.8f})samples.Add(new Vector4(x,y,mip,0));
                            double sx=(unit.max[0]-unit.min[0])*source.width, sy=(unit.max[1]-unit.min[1])*source.height;
                            foreach(float t in new[]{.13f,.37f,.63f,.87f})foreach(float offset in new[]{0f,.125f,.5f,1f,2f})
                            {
                                float dx=(float)Math.Min(.1,offset*Math.Pow(2,mip)/sx),dy=(float)Math.Min(.1,offset*Math.Pow(2,mip)/sy);
                                samples.Add(new Vector4(dx,t,mip,1));samples.Add(new Vector4(1-dx,t,mip,1));
                                samples.Add(new Vector4(t,dy,mip,1));samples.Add(new Vector4(t,1-dy,mip,1));
                            }
                            using var input=new ComputeBuffer(samples.Count,16);using var output=new ComputeBuffer(samples.Count,16);
                            input.SetData(samples.ToArray());shader.SetBuffer(kernel,"Samples",input);shader.SetBuffer(kernel,"Differences",output);
                            shader.SetInt("Count",samples.Count);shader.Dispatch(kernel,(samples.Count+63)/64,1,1);
                            var errors=new Vector4[samples.Count];output.GetData(errors);
                            var row=new Row{unit=unit.id,channel=channel.name,mip=mip,cases=samples.Count};
                            for(int i=0;i<errors.Length;i++)
                            {
                                float e=Mathf.Max(errors[i].x,errors[i].y,errors[i].z,errors[i].w);
                                if(float.IsNaN(e)||float.IsInfinity(e))throw new InvalidDataException("Nonfinite sample.");
                                row.maximum=Mathf.Max(row.maximum,e);if(e>2f/255)row.different++;
                                if(samples[i].w!=0){row.edgeCases++;row.edgeMaximum=Mathf.Max(row.edgeMaximum,e);if(e>2f/255)row.edgeDifferent++;}
                            }
                            rows.Add(row);
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(candidate);}
                }
            }
            var result=new Result{status=rows.Any(r=>r.different>0)?"ORIGINAL_ATLAS_DIFFERENCES":"SAMPLES_EQUAL",scope="Original atlas versus generated unit; all 13 source mips, interior and boundary neighborhoods, linear GPU channels. Does not establish game visual acceptance or normal-vector angular equivalence.",rows=rows.ToArray()};
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(result,true));
            Debug.Log("PASS finite original-atlas measurement: "+result.status+"; rows="+rows.Count+"; differences="+rows.Sum(r=>r.different));
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally{PreparePaint.CloseCodec(codec);}
    }
    private static Texture2D Load(string path)
    {
        var bytes=File.ReadAllBytes(path);int w=BitConverter.ToUInt16(bytes,2),h=BitConverter.ToUInt16(bytes,4),size=0;
        var format=(GraphicsFormat)bytes[9];int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format),block=(int)GraphicsFormatUtility.GetBlockSize(format);
        for(int mip=0;mip<bytes[8];mip++){size+=((w+bw-1)/bw)*((h+bh-1)/bh)*block;w=Math.Max(1,w/2);h=Math.Max(1,h/2);}
        using var stream=new MemoryStream(bytes);return MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,size));
    }
}
