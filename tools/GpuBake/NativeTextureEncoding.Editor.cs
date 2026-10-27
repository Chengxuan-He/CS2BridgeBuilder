using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Offline native encoding only. Every level comes from the authored mip input;
// Unity's whole-chain compressor corrupts some partial-block mip edges.
internal static class NativeTextureEncoding
{
    internal static Texture2D Bc7(Texture2D source)
    {
        if(source.width%4!=0||source.height%4!=0)
            throw new InvalidDataException("BC7 requires a block-aligned sampling grid.");
        bool srgb=GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
        if(source.graphicsFormat!=(srgb?GraphicsFormat.R8G8B8A8_SRGB:GraphicsFormat.R8G8B8A8_UNorm))
            throw new InvalidDataException("BC7 conversion expects the verified RGBA8 unit.");
        var raw=source.GetRawTextureData<byte>().ToArray();
        using var encoded=new MemoryStream();int offset=0;
        for(int mip=0;mip<source.mipmapCount;mip++)
        {
            int width=Math.Max(1,source.width>>mip),height=Math.Max(1,source.height>>mip);
            int blockWidth=(width+3)/4*4,blockHeight=(height+3)/4*4;
            var pixels=new byte[blockWidth*blockHeight*4];
            for(int y=0;y<blockHeight;y++)for(int x=0;x<blockWidth;x++)for(int c=0;c<4;c++)
                pixels[(y*blockWidth+x)*4+c]=raw[offset+(Math.Min(y,height-1)*width+Math.Min(x,width-1))*4+c];
            var level=new Texture2D(blockWidth,blockHeight,TextureFormat.RGBA32,false,!srgb);
            try
            {
                level.LoadRawTextureData(pixels);level.Apply(false,false);
                EditorUtility.CompressTexture(level,TextureFormat.BC7,TextureCompressionQuality.Best);
                if(level.format!=TextureFormat.BC7)throw new InvalidDataException("Native BC7 encoding failed.");
                var bytes=level.GetRawTextureData<byte>().ToArray();
                if(bytes.Length!=blockWidth*blockHeight)throw new InvalidDataException("Incomplete BC7 mip blocks.");
                encoded.Write(bytes);
            }
            finally{UnityEngine.Object.DestroyImmediate(level);}
            offset+=width*height*4;
        }
        if(offset!=raw.Length)throw new InvalidDataException("Incomplete source mip accounting.");
        var result=new Texture2D(source.width,source.height,
            srgb?GraphicsFormat.RGBA_BC7_SRGB:GraphicsFormat.RGBA_BC7_UNorm,
            source.mipmapCount,TextureCreationFlags.MipChain|TextureCreationFlags.IgnoreMipmapLimit)
            {wrapMode=source.wrapMode,filterMode=source.filterMode,anisoLevel=source.anisoLevel};
        try{result.LoadRawTextureData(encoded.ToArray());result.Apply(false,false);return result;}
        catch{UnityEngine.Object.DestroyImmediate(result);throw;}
    }
}
