using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Decode the serialized mip chain into a private texture. TextureAsset.Load(0)
// can still return an already loaded, biased instance; never bake that instance.
internal static class MovableTextureSource
{
    internal delegate bool DecompressBytes(byte[] input,out byte[] output,out string? error);
    internal static bool TryRead(Stream stream,Func<byte[],byte[]> decompress,out Texture2D result,out string? error)
        => TryRead(stream,(byte[] input,out byte[] output,out string? failure)=>
        {output=Array.Empty<byte>();failure=null;try{output=decompress(input);return true;}catch(Exception exception){failure="Native texture decompression: "+exception;return false;}},out result,out error);
    internal static bool TryRead(Stream stream, DecompressBytes decompress,out Texture2D result,out string? error)
    {
        result=null!;error=null;Texture2D? texture=null;string? failure=null;bool success=false;
        bool Fail(string message){failure=message;return false;}
        try
        {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var version = reader.ReadUInt16();
        if (version < 1 || version > 5)
            return Fail("Unsupported native texture version: " + version);
        int width = reader.ReadUInt16(), height = reader.ReadUInt16();
        int depth = version >= 4 ? reader.ReadUInt16() : 1;
        int mips = reader.ReadByte();
        var format = version >= 3 ? (GraphicsFormat)reader.ReadByte() :
            (Enum.TryParse<GraphicsFormat>(reader.ReadString(),out var parsed)?parsed:GraphicsFormat.None);
        if(format==GraphicsFormat.None)return Fail("Unsupported native texture graphics format.");
        var dimension = version >= 4 ? (TextureDimension)reader.ReadByte() : TextureDimension.Tex2D;
        var filter = version >= 2 ? (FilterMode)reader.ReadByte() : FilterMode.Trilinear;
        var wrap = version >= 2 ? (TextureWrapMode)reader.ReadByte() : TextureWrapMode.Repeat;
        int aniso = version >= 2 ? reader.ReadByte() : 16;
        if (dimension != TextureDimension.Tex2D || depth != 1)
            return Fail("Native movable texture sampling requires a 2D texture.");
        if (width == 0 || height == 0 || mips < 1 || mips > 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2)))
            return Fail("Invalid native texture dimensions or mip count.");
        // BC7/DXT uploads otherwise obey the global mip limit even though width
        // and height still report the serialized dimensions. Baking must sample
        // mip zero, independently of the player's rendering quality.
        var flags = TextureCreationFlags.DontInitializePixels | TextureCreationFlags.IgnoreMipmapLimit;
        if (mips > 1) flags |= TextureCreationFlags.MipChain;
        texture = new Texture2D(width, height, format, mips, flags);
        texture.filterMode=filter;texture.wrapMode=wrap;texture.anisoLevel=aniso;texture.hideFlags=HideFlags.DontSave;
            int size = texture.GetRawTextureData<byte>().Length;
            int compressed = version >= 5 ? reader.ReadInt32() : -1;
            if (compressed < -1) return Fail("Invalid native texture compression length.");
            int count = compressed == -1 ? size : compressed;
            var bytes = reader.ReadBytes(count);
            if (bytes.Length != count) return Fail("Incomplete native texture mip chain.");
            if (compressed != -1 && !decompress(bytes,out bytes,out var cause))return Fail(cause??"Native texture decompression failed.");
            if (bytes.Length != size) return Fail("Native texture mip payload has an incorrect size.");
            texture.LoadRawTextureData(bytes);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            result=texture;success=true;return true;
        }
        catch(Exception exception){return Fail("Native texture decode: "+exception);}
        finally
        {
            if(!success && texture!=null)try{UnityEngine.Object.Destroy(texture);}catch(Exception exception){failure=(failure??"Native texture failure")+"; cleanup: "+exception;}
            error=failure;if(!success)result=null!;
        }
    }
}
