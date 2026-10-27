using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BridgeBuilder.Bridges;
using UnityEngine;

public static class VtSourceCheck
{
    [DllImport("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll",CallingConvention=CallingConvention.Cdecl)]
    private static extern long decompress_data(IntPtr source,long length,IntPtr target,long capacity,int format);
    private static byte[] Decompress(byte[] input)
    {
        if(input.Length<12 || BitConverter.ToInt32(input,0)!=int.MaxValue || BitConverter.ToInt32(input,8)!=input.Length-12)
            throw new Exception("Invalid native ZSTD marker.");
        int size=BitConverter.ToInt32(input,4);
        var source=Marshal.AllocHGlobal(input.Length-12);var target=Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(input,12,source,input.Length-12);
            if(decompress_data(source,input.Length-12,target,size,0)!=size)throw new Exception("Native ZSTD decode failed.");
            var result=new byte[size];Marshal.Copy(target,result,0,size);return result;
        }
        finally{Marshal.FreeHGlobal(source);Marshal.FreeHGlobal(target);}
    }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string directory=args[Array.IndexOf(args,"-vtFixtures")+1];
        int old=QualitySettings.globalTextureMipmapLimit,files=0,channels=0,samples=0;
        try
        {
            CheckTileLayout();
            foreach(string file in Directory.GetFiles(directory).Where(f=>f.EndsWith(".VTTexture") || f.EndsWith(".VTSurface")))
            {
                var reference=new Dictionary<(int,int),Color32[]>();
                foreach(int quality in new[]{0,2})
                {
                    QualitySettings.globalTextureMipmapLimit=quality;
                    using var stream=File.OpenRead(file);
                    var textures=file.EndsWith(".VTTexture")?
                        new Dictionary<(int,int),Texture2D>{{(0,0),MovableVtTextureSource.ReadTexture(stream,Decompress)}}:
                        MovableVtTextureSource.ReadSurface(stream,Decompress);
                    try
                    {
                        foreach(var item in textures)
                        {
                            if(item.Value.activeMipmapLimit!=0 || item.Value.mipmapCount!=1)throw new Exception("VT source upload is quality-biased.");
                            var pixels=Sample(item.Value);
                            if(quality==0)reference.Add(item.Key,pixels);
                            else
                            {
                                if(!pixels.SequenceEqual(reference[item.Key]))throw new Exception("VT mip-zero pixels changed with quality: "+file);
                                channels++;samples+=pixels.Length*4;
                            }
                        }
                    }
                    finally{foreach(var texture in textures.Values)UnityEngine.Object.DestroyImmediate(texture);}
                }
                files++;
            }
            if(files==0)throw new Exception("No native VT fixtures.");
            Debug.Log($"PASS native VT full-resolution source: {files} original files; {channels} texture channels; {samples} exact channel comparisons; quality 0/2; native tile decompression; mip-zero residency.");
        }
        finally{QualitySettings.globalTextureMipmapLimit=old;}
    }
    private static void CheckTileLayout()
    {
        // A rectangular 4-by-2 tile layout with unique BC7 block colors. This
        // detects tile transposition, border mistakes and layer-offset errors;
        // quality-invariance alone would not detect a consistently wrong map.
        const int width=64,height=32,tile=16,block=16,pitch=8*block;
        var source=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
        var colors=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            colors[y*width+x]=new Color32((byte)(x/4*16),(byte)(y/4*32),64,255);
        source.SetPixels32(colors);source.Apply(false,false);
        var target=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var previous=RenderTexture.active;
        try
        {
            Graphics.Blit(source,target);RenderTexture.active=previous;
            var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/PackingProbe.compute");
            using var channel=new MovableAtlasChannel(shader,target,2,false);
            channel.Read();channel.Texture.ignoreMipmapLimit=true;channel.Texture.Apply(false,false);
            var expected=Sample(channel.Texture);
            var raw=channel.Texture.GetPixelData<byte>(0).ToArray();
            var tiles=new List<byte[]>();
            for(int ty=0;ty<2;ty++)for(int tx=0;tx<4;tx++)
            {
                var bytes=new byte[1280]; // 32x32 padded tile plus its 16x16 mip.
                for(int by=0;by<8;by++)for(int bx=0;bx<8;bx++)
                {
                    int x=Math.Max(0,Math.Min(15,tx*4+bx-2)),y=Math.Max(0,Math.Min(7,ty*4+by-2));
                    Buffer.BlockCopy(raw,(y*16+x)*block,bytes,by*pitch+bx*block,block);
                }
                tiles.Add(bytes);
            }
            foreach(bool surface in new[]{false,true})
            foreach(int quality in new[]{0,2})
            {
                QualitySettings.globalTextureMipmapLimit=quality;
                using var stream=new MemoryStream();
                using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
                {
                    if(surface)
                    {
                        var header=new byte[498];
                        void Int(int at,int value)=>Buffer.BlockCopy(BitConverter.GetBytes(value),0,header,at,4);
                        header[0]=1;Int(18,tile);Int(26,width);Int(30,height);Int(34,8);Int(46,498);Int(50,3);
                        // Two owned layers with an external layer between them.
                        foreach(int li in new[]{106,202}){header[li]=1;Int(li+4,(int)channel.Texture.graphicsFormat);Int(li+24,1024);}
                        Int(490,1);Int(494,498+tiles.Sum(t=>t.Length)*2);writer.Write(header);
                        foreach(var bytes in tiles){writer.Write(bytes);writer.Write(bytes);}
                        for(int i=1;i<=8;i++)writer.Write(i*2560);
                    }
                    else
                    {
                        writer.Write((ushort)1);writer.Write((byte)255);writer.Write(new byte[16]);writer.Write((ushort)0);
                        writer.Write((ushort)tile);writer.Write((ushort)width);writer.Write((ushort)height);writer.Write((ushort)0);
                        writer.Write((byte)1);writer.Write((byte)channel.Texture.graphicsFormat);
                        writer.Write((byte)FilterMode.Bilinear);writer.Write((byte)TextureWrapMode.Clamp);writer.Write((byte)8);
                        writer.Write(0);writer.Write(8);for(int i=1;i<=8;i++)writer.Write(i*1280);
                        foreach(var bytes in tiles)writer.Write(bytes);
                    }
                }
                stream.Position=0;
                var images=surface?MovableVtTextureSource.ReadSurface(stream,b=>b):
                    new Dictionary<(int,int),Texture2D>{{(0,0),MovableVtTextureSource.ReadTexture(stream,b=>b)}};
                try
                {
                    if(images.Count!=(surface?2:1))throw new Exception("VT owned layer count changed.");
                    foreach(var texture in images.Values)
                        if(!Sample(texture).SequenceEqual(expected))throw new Exception("VT top-level tile placement or pixels changed.");
                }
                finally{foreach(var image in images.Values)UnityEngine.Object.DestroyImmediate(image);}
            }
            Debug.Log("PASS native VT tile layout: rectangular 4x2 tiles; unique block colors; tile borders; interleaved external/owned layers; both containers; quality 0/2.");
        }
        finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(source);}
    }
    private static Color32[] Sample(Texture2D source)
    {
        var target=new RenderTexture(128,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var read=new Texture2D(128,128,TextureFormat.RGBA32,false,true);var old=RenderTexture.active;
        try
        {
            Graphics.Blit(source,target);RenderTexture.active=target;
            read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply(false,false);return read.GetPixels32();
        }
        finally{RenderTexture.active=old;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(read);}
    }
}
