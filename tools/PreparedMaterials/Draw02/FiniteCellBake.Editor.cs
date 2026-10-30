using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using BridgeBuilder.Bridges;

public static class Draw02FiniteCellBake
{
    [Serializable] public sealed class Channel { public string name,path; }
    [Serializable] public sealed class Unit { public string id; public int width,height;public float[] physical,triangles; }
    [Serializable] public sealed class Manifest { public Channel[] channels;public Unit[] units; }
    [Serializable] public sealed class Product { public string unit,channel,cid,path;public int width,height,mips;public long bytes; }
    [Serializable] public sealed class Report { public Product[] products; }
    public static void Run()
    {
        try
        {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO");
            var output=root+"/artifacts/material-remap/Draw02/near-cells";
            var input=JsonUtility.FromJson<Manifest>(File.ReadAllText(output+"/bake-input.json"));
            PreparePaint.OpenCodec("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data");
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/FiniteCell.compute");
            int kernel=shader.FindKernel("Extract");
            var products=new System.Collections.Generic.List<Product>();
            using var noTracking=new ComputeBuffer(1,4);shader.SetBuffer(kernel,"Modified",noTracking);shader.SetInt("Track",0);
            long verifiedOutsideMask=0;
            foreach(var channel in input.channels)
            {
                var bytes=File.ReadAllBytes(channel.path);
                using var stream=new MemoryStream(bytes);
                var source=MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,RawSize(bytes)));
                source.filterMode=FilterMode.Bilinear; source.wrapMode=TextureWrapMode.Clamp;
                shader.SetTexture(kernel,"Source",source);
                shader.SetInt("EncodeSrgb",GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat)?1:0);
                shader.SetInt("PaintRepair",0);
                var target=new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};target.Create();
                shader.SetTexture(kernel,"Result",target);shader.SetInts("Extent",source.width,source.height);shader.SetInt("AtlasOnly",1);shader.SetInt("SourceMip",0);shader.SetInt("TriangleCount",0);
                using var dummy=new ComputeBuffer(3,16);shader.SetBuffer(kernel,"Triangles",dummy);
                shader.Dispatch(kernel,(source.width+7)/8,(source.height+7)/8,1);
                var read=AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32);read.WaitForCompletion();
                if(read.hasError)throw new IOException("Native atlas readback failed.");
                File.WriteAllBytes(output+"/"+channel.name+"-native.rgba",read.GetData<byte>().ToArray());
                target.Release();UnityEngine.Object.DestroyImmediate(target);
                foreach(var unit in input.units)
                {
                    var vertices=new Vector4[unit.triangles.Length/4];
                    for(int i=0;i<vertices.Length;i++) vertices[i]=new Vector4(unit.triangles[i*4],unit.triangles[i*4+1],unit.triangles[i*4+2],unit.triangles[i*4+3]);
                    using var triangles=new ComputeBuffer(vertices.Length,16);triangles.SetData(vertices);
                    shader.SetBuffer(kernel,"Triangles",triangles);shader.SetInt("TriangleCount",vertices.Length/3);shader.SetInt("AtlasOnly",0);
                    shader.SetVector("Physical",new Vector4(unit.physical[0],unit.physical[1],unit.physical[2],unit.physical[3]));
                    var texture=new Texture2D(unit.width,unit.height,TextureFormat.RGBA32,true,!GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat))
                        {wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=source.anisoLevel};
                    for(int mip=0;mip<texture.mipmapCount;mip++)
                    {
                        int w=Math.Max(1,unit.width>>mip),h=Math.Max(1,unit.height>>mip),sm=Math.Min(mip,source.mipmapCount-1);
                        var scale=1<<sm;
                        Vector4 Mask(int lo,int hi)=>new Vector4(Mathf.Floor((float)lo/scale)*scale/4096f,Mathf.Floor(2002f/scale)*scale/4096f,
                            Mathf.Ceil((float)hi/scale)*scale/4096f,Mathf.Ceil(2754f/scale)*scale/4096f);
                        var maskA=Mask(1344,1360);var maskB=Mask(1904,1924);
                        float Donor(float pixel)=>Mathf.Clamp((Mathf.Floor(pixel/scale)+.5f)*scale/4096f,0f,1f);
                        shader.SetVector("PaintRectA",maskA);shader.SetVector("PaintRectB",maskB);
                        shader.SetVector("DonorU",new Vector4(Donor(Math.Min(1640,1360+scale)),Donor(Math.Max(1640,1904-scale)),0f,0f));
                        shader.SetInt("PaintRepair",unit.id.EndsWith("fixed-road")&&channel.name!="_ControlMask"?1:0);
                        var rt=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};rt.Create();
                        shader.SetTexture(kernel,"Result",rt);shader.SetInts("Extent",w,h);shader.SetInt("SourceMip",sm);
                        shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                        var pixels=AsyncGPUReadback.Request(rt,0,TextureFormat.RGBA32);pixels.WaitForCompletion();
                        if(pixels.hasError)throw new IOException("Finite unit readback failed.");
                        var changed=pixels.GetData<byte>().ToArray();
                        texture.SetPixelData(changed,mip);
                        if(unit.id.EndsWith("fixed-road")&&channel.name!="_ControlMask")
                        {
                            using var maskBuffer=new ComputeBuffer(w*h,4);shader.SetBuffer(kernel,"Modified",maskBuffer);shader.SetInt("Track",1);
                            shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                            var mask=new uint[w*h];maskBuffer.GetData(mask);
                            shader.SetInt("PaintRepair",0);shader.SetInt("Track",0);shader.SetBuffer(kernel,"Modified",noTracking);
                            shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                            var before=AsyncGPUReadback.Request(rt,0,TextureFormat.RGBA32);before.WaitForCompletion();
                            if(before.hasError)throw new IOException("Paint isolation readback failed.");
                            var original=before.GetData<byte>();
                            for(int i=0;i<mask.Length;i++) if(mask[i]==0)
                            {for(int c=0;c<4;c++)if(original[i*4+c]!=changed[i*4+c])throw new InvalidDataException("Paint repair changed a texel outside its mask.");verifiedOutsideMask++;}
                        }
                        rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                    }
                    texture.Apply(false,false);
                    if(channel.name=="_BaseColorMap")File.WriteAllBytes(output+"/"+unit.id+"-candidate.png",texture.EncodeToPNG());
                    var encoded=NativeTextureEncoding.Bc7(texture);
                    var payload=PreparePaint.Texture(encoded);UnityEngine.Object.DestroyImmediate(encoded);
                    var digest=PreparePaint.Digest(payload);
                    var cid=PreparePaint.HashText(Enumerable.Range(0,16).Select(i=>Convert.ToByte(digest.Substring(i*2,2),16)).ToArray(),0);
                    var path=output+"/output/Draw02/Shared/"+cid+".Texture";Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path,payload);File.WriteAllText(path+".cid",cid);
                    products.Add(new Product{unit=unit.id,channel=channel.name,cid=cid,path=path,width=unit.width,height=unit.height,mips=texture.mipmapCount,bytes=payload.Length});
                    UnityEngine.Object.DestroyImmediate(texture);
                    Debug.Log("Finite cell authored: "+unit.id+" "+channel.name+" "+payload.Length);
                }
                UnityEngine.Object.DestroyImmediate(source);
            }
            File.WriteAllText(output+"/producer-report.json",JsonUtility.ToJson(new Report{products=products.ToArray()},true));
            File.WriteAllText(output+"/paint-isolation-check.txt","PASS: "+verifiedOutsideMask+" channel-texels outside the mip-specific paint masks are byte-identical to the unmodified source mapping; donor UV mapping is shared by BaseColor, Normal and Mask. This is localized composition, not recovery of an unknown underlying texture.");
            Debug.Log("PASS Draw02 original atlas channels sampled through native Unity texture upload; no mip regeneration.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static int RawSize(byte[] bytes)
    {
        using var r=new BinaryReader(new MemoryStream(bytes));int version=r.ReadUInt16(),w=r.ReadUInt16(),h=r.ReadUInt16();if(version>=4)r.ReadUInt16();int mips=r.ReadByte();var format=(GraphicsFormat)r.ReadByte();
        int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format),size=0;
        for(int i=0;i<mips;i++){size+=((w+bw-1)/bw)*((h+bh-1)/bh)*(int)GraphicsFormatUtility.GetBlockSize(format);w=Math.Max(1,w/2);h=Math.Max(1,h/2);}return size;
    }
}
