using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BridgeBuilder.Bridges;

public static class Draw02NormalMipReadback
{
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var folder=root+"/artifacts/material-remap/Draw02/near-cells";
            var input=JsonUtility.FromJson<Draw02FiniteCellBake.Manifest>(File.ReadAllText(folder+"/bake-input.json"));
            var channel=input.channels.Single(c=>c.name=="_NormalMap");
            using var stream=File.OpenRead(channel.path);
            var source=MovableTextureSource.Read(stream,_=>throw new InvalidDataException("Expected retained uncompressed full native mip payload."));
            source.filterMode=FilterMode.Bilinear; source.wrapMode=TextureWrapMode.Clamp;
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/FiniteCell.compute");var kernel=shader.FindKernel("Extract");
            using var buffer=new ComputeBuffer(3,16);using var tracking=new ComputeBuffer(1,4);
            shader.SetBuffer(kernel,"Triangles",buffer);shader.SetBuffer(kernel,"Modified",tracking);
            shader.SetInt("AtlasOnly",1);shader.SetInt("EncodeSrgb",0);shader.SetInt("PaintRepair",0);shader.SetInt("Track",0);
            shader.SetTexture(kernel,"Source",source);
            Directory.CreateDirectory(folder+"/normal-source-mips");
            for(var mip=0;mip<source.mipmapCount;mip++)
            {
                var w=Math.Max(1,source.width>>mip);var h=Math.Max(1,source.height>>mip);
                var target=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};target.Create();
                shader.SetTexture(kernel,"Result",target);shader.SetInts("Extent",w,h);shader.SetInt("SourceMip",mip);
                shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                var read=AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32);read.WaitForCompletion();
                if(read.hasError)throw new IOException("Original normal mip readback failed.");
                File.WriteAllBytes(folder+"/normal-source-mips/"+mip+".rgba",read.GetData<byte>().ToArray());
                target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
            Debug.Log("PASS original normal BC7 native GPU decoding: "+source.mipmapCount+" complete source mip levels, no regeneration.");
            UnityEngine.Object.DestroyImmediate(source);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
