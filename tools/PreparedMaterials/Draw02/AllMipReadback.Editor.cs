using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BridgeBuilder.Bridges;

public static class Draw02AllMipReadback
{
    public static void Run()
    {
        var module=PreparePaint.OpenCodec("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data");
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var folder=root+"/artifacts/material-remap/Draw02/near-cells";
            var manifest=Environment.GetEnvironmentVariable("DRAW02_READBACK_MANIFEST") ?? folder+"/bake-input.json";
            var output=Environment.GetEnvironmentVariable("DRAW02_READBACK_OUTPUT") ?? folder+"/all-source-mips";
            var input=JsonUtility.FromJson<Draw02FiniteCellBake.Manifest>(File.ReadAllText(manifest));
            foreach(var channel in input.channels) {
            using var stream=File.OpenRead(channel.path);
            var source=MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,RawSize(channel.path)));
            source.filterMode=FilterMode.Bilinear; source.wrapMode=TextureWrapMode.Clamp;
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/FiniteCell.compute");var kernel=shader.FindKernel("Extract");
            using var buffer=new ComputeBuffer(3,16);using var tracking=new ComputeBuffer(1,4);
            shader.SetBuffer(kernel,"Triangles",buffer);shader.SetBuffer(kernel,"Modified",tracking);
            shader.SetInt("AtlasOnly",1);shader.SetInt("EncodeSrgb",0);shader.SetInt("PaintRepair",0);shader.SetInt("Track",0);
            shader.SetTexture(kernel,"Source",source);
            Directory.CreateDirectory(output+"/"+channel.name);
            for(var mip=0;mip<source.mipmapCount;mip++)
            {
                var w=Math.Max(1,source.width>>mip);var h=Math.Max(1,source.height>>mip);
                var target=new RenderTexture(w,h,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear){enableRandomWrite=true};target.Create();
                shader.SetTexture(kernel,"Result",target);shader.SetInts("Extent",w,h);shader.SetInt("SourceMip",mip);
                shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                var read=AsyncGPUReadback.Request(target,0,TextureFormat.RGBAFloat);read.WaitForCompletion();
                if(read.hasError)throw new IOException("Original normal mip readback failed.");
                File.WriteAllBytes(output+"/"+channel.name+"/"+mip+".rgba32f",read.GetData<byte>().ToArray());
                target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
            Debug.Log("PASS original channel BC7 native GPU decoding: "+channel.name+" "+source.mipmapCount+" complete source mip levels, no regeneration.");
            UnityEngine.Object.DestroyImmediate(source); } EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally{PreparePaint.CloseCodec(module);}
    }
    static int RawSize(string path)
    {
        var bytes=File.ReadAllBytes(path);int width=BitConverter.ToUInt16(bytes,2),height=BitConverter.ToUInt16(bytes,4),size=0;
        var format=(UnityEngine.Experimental.Rendering.GraphicsFormat)bytes[9];
        int bw=(int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockWidth(format);
        int bh=(int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockHeight(format);
        int bs=(int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockSize(format);
        for(var m=0;m<bytes[8];m++)size+=((Math.Max(1,width>>m)+bw-1)/bw)*((Math.Max(1,height>>m)+bh-1)/bh)*bs;
        return size;
    }
}
