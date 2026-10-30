using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Read-only source sampling for the proposed donor overlay; no material authoring.
public static class Draw01GratingBoundaryCheck
{
    [Serializable] public class Row { public string channel,mode; public int mip; public Vector4[] original,donor; }
    [Serializable] public class Report { public string status="EVIDENCE_ONLY_NOT_IMPLEMENTED"; public Row[] rows; }
    public static void Run()
    {
        string Arg(string key)=>Draw01AppearancePartitionCheck.Argument(key);
        var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-unitManifest")));
        var codec=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.Enable;
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/NearUnitCheck.compute");
            int kernel=shader.FindKernel("Check");
            var points=new List<Vector2>();
            foreach(float u in new[]{0f,.5f,1f})foreach(float v in new[]{0f,.25f,.5f,.75f,1f})points.Add(new Vector2(u,v));
            using var input=new ComputeBuffer(points.Count,8);input.SetData(points);
            using var output=new ComputeBuffer(points.Count*2,16);
            shader.SetBuffer(kernel,"Points",input);shader.SetBuffer(kernel,"Samples",output);shader.SetInt("Count",points.Count);
            shader.SetVector("Region",new Vector4(985f/4096,2708f/4096,692f/4096,0));
            shader.SetVector("PreparedRegion",new Vector4(985f/4096,3622f/4096,692f/4096,0));
            var rows=new List<Row>();
            foreach(var channel in manifest.units[0].channels)
            {
                byte[] bytes=File.ReadAllBytes(channel.path);
                int size=(int)typeof(PrepareRepeatUnits).GetMethod("NativeRawSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{bytes});
                using var stream=new MemoryStream(bytes);
                var texture=BridgeBuilder.Bridges.MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,size));
                try
                {
                    shader.SetTexture(kernel,"Original",texture);shader.SetTexture(kernel,"Prepared",texture);
                    foreach(int mode in new[]{0})for(int mip=0;mip<texture.mipmapCount;mip++)
                    {
                        shader.SetInt("NativeGrad",mode==0?0:1);shader.SetFloat("FootprintRatio",mode);shader.SetInt("Mip",mip);
                        shader.Dispatch(kernel,1,1,1);var samples=new Vector4[points.Count*2];output.GetData(samples);
                        rows.Add(new Row{channel=channel.name,mode=mode==0?"SampleLevel":"NativeGrad"+mode,mip=mip,
                            original=Enumerable.Range(0,points.Count).Select(i=>samples[2*i]).ToArray(),
                            donor=Enumerable.Range(0,points.Count).Select(i=>samples[2*i+1]).ToArray()});
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{rows=rows.ToArray()},true));
            Debug.Log("MEASURED original grating opposite-boundary samples: "+rows.Count);
        }
        finally{PreparePaint.CloseCodec(codec);}
        EditorApplication.Exit(0);
    }
}
