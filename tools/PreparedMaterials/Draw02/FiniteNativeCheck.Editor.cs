using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BridgeBuilder.Bridges;

public static class Draw02FiniteNativeCheck
{
    [Serializable] public sealed class Bindings { public Unit[] units; }
    [Serializable] public sealed class Unit { public string name,key,cid;public Product[] channels; }
    [Serializable] public sealed class Product { public string channel,cid,path;public int width,height,mips; }
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO")+"/artifacts/material-remap/Draw02/near-cells";
            var managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var surface=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
            var decode=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));
            var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
            PreparePaint.OpenCodec(Path.GetDirectoryName(managed));
            var bindings=JsonUtility.FromJson<Bindings>(File.ReadAllText(root+"/verified-lod0-bindings.json"));
            int textures=0,mips=0;
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/FiniteCell.compute");var kernel=shader.FindKernel("Extract");
            using var dummy=new ComputeBuffer(3,16);using var changed=new ComputeBuffer(1,4);
            shader.SetBuffer(kernel,"Triangles",dummy);shader.SetBuffer(kernel,"Modified",changed);
            shader.SetInt("Track",0);shader.SetInt("PaintRepair",0);shader.SetInt("AtlasOnly",1);shader.SetInt("TriangleCount",0);shader.SetInt("EncodeSrgb",0);
            foreach(var unit in bindings.units)
            {
                using var reader=new BinaryReader(File.OpenRead(root+"/output/Draw02/RepeatUnits/"+unit.key+"/Repeat.Surface"));
                var asset=FormatterServices.GetUninitializedObject(surface);
                surface.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
                if((int)surface.GetProperty("materialTemplateHash").GetValue(asset)!=0 || (bool)surface.GetProperty("isVTMaterial").GetValue(asset))throw new Exception("Wrong native finite-unit descriptor.");
                foreach(var channel in unit.channels)
                {
                    using var stream=File.OpenRead(channel.path);
                    var texture=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
                    if(stream.Position!=stream.Length||texture.width!=channel.width||texture.height!=channel.height||texture.mipmapCount!=channel.mips)throw new Exception("Native dimensions/mips differ.");
                    for(int mip=0;mip<texture.mipmapCount;mip++)
                    {
                        int w=Math.Max(1,texture.width>>mip),h=Math.Max(1,texture.height>>mip);
                        var rt=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};rt.Create();
                        shader.SetTexture(kernel,"Source",texture);shader.SetTexture(kernel,"Result",rt);shader.SetInt("SourceMip",mip);shader.SetInts("Extent",w,h);
                        shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                        var read=AsyncGPUReadback.Request(rt,0,TextureFormat.RGBA32);read.WaitForCompletion();
                        if(read.hasError||read.GetData<byte>().Length!=w*h*4)throw new Exception("Native mip upload failed.");
                        rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                        mips++;
                    }
                    textures++;UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils",true);
            var methods=math.GetMethods().Where(m=>m.Name.Contains("Octahedral")).ToArray();
            foreach(var method in methods)Debug.Log("Tangent native API: "+method);
            var pack=methods.Single(m=>m.Name=="TangentToOctahedral");
            foreach(float x in new[]{-1f,1f})foreach(float w in new[]{-1f,1f})
            {
                var tangent=Activator.CreateInstance(pack.GetParameters()[0].ParameterType,new object[]{x,0f,0f,w});
                Debug.Log("Native tangent ("+x+",0,0,"+w+") = "+((uint)pack.Invoke(null,new[]{tangent})).ToString("x8"));
            }
            var result=$"PASS 4 LOD0 native Surface headers, {textures} complete texture uploads and {mips} original-derived mip levels. Independent runtime persistence and final appearance/size acceptance remain outstanding.";
            File.WriteAllText(root+"/native-format-check.txt",result);Debug.Log(result);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
