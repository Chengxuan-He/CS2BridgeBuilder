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
    internal static Texture2D Read(Stream stream, Func<byte[], byte[]> decompress)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var version = reader.ReadUInt16();
        if (version < 1 || version > 5)
            throw new NotSupportedException("Unsupported native texture version: " + version);
        int width = reader.ReadUInt16(), height = reader.ReadUInt16();
        int depth = version >= 4 ? reader.ReadUInt16() : 1;
        int mips = reader.ReadByte();
        var format = version >= 3 ? (GraphicsFormat)reader.ReadByte() :
            (GraphicsFormat)Enum.Parse(typeof(GraphicsFormat), reader.ReadString());
        var dimension = version >= 4 ? (TextureDimension)reader.ReadByte() : TextureDimension.Tex2D;
        var filter = version >= 2 ? (FilterMode)reader.ReadByte() : FilterMode.Trilinear;
        var wrap = version >= 2 ? (TextureWrapMode)reader.ReadByte() : TextureWrapMode.Repeat;
        int aniso = version >= 2 ? reader.ReadByte() : 16;
        if (dimension != TextureDimension.Tex2D || depth != 1)
            throw new NotSupportedException("Native movable texture sampling requires a 2D texture.");
        if (width == 0 || height == 0 || mips < 1 || mips > 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2)))
            throw new InvalidDataException("Invalid native texture dimensions or mip count.");
        // BC7/DXT uploads otherwise obey the global mip limit even though width
        // and height still report the serialized dimensions. Baking must sample
        // mip zero, independently of the player's rendering quality.
        var flags = TextureCreationFlags.DontInitializePixels | TextureCreationFlags.IgnoreMipmapLimit;
        if (mips > 1) flags |= TextureCreationFlags.MipChain;
        var texture = new Texture2D(width, height, format, mips, flags)
        { filterMode = filter, wrapMode = wrap, anisoLevel = aniso, hideFlags = HideFlags.DontSave };
        try
        {
            int size = texture.GetRawTextureData<byte>().Length;
            int compressed = version >= 5 ? reader.ReadInt32() : -1;
            if (compressed < -1) throw new InvalidDataException("Invalid native texture compression length.");
            int count = compressed == -1 ? size : compressed;
            var bytes = reader.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException("Incomplete native texture mip chain.");
            if (compressed != -1) bytes = decompress(bytes);
            if (bytes.Length != size) throw new InvalidDataException("Native texture mip payload has an incorrect size.");
            texture.LoadRawTextureData(bytes);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }
        catch { UnityEngine.Object.Destroy(texture); throw; }
    }
}
