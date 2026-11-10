using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BridgeBuilder.Runtime;

/// <summary>Reads native SurfaceAsset dependency fields without loading or rewriting the asset.</summary>
internal static class BridgeSurfaceReferences
{
    // Layout from game 1.6.2f1 SurfaceAsset, SurfaceAssetVTHeader, PerStackData and the
    // Colossal binary reader extensions. Hash128 prints least-significant nibbles first;
    // neither Guid.ToString nor ordinary byte hexadecimal produces its CID representation.
    internal static bool TryRead(byte[] bytes, Func<int, string, bool> inVirtualStack, out string[] references, out string? error)
    {
        references=Array.Empty<string>();error=null;
        string? failure=null;
        bool Fail(string message){failure??=message;return false;}
        try
        {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var version = reader.ReadUInt16();
        if (version > 11) return Fail("Unsupported native surface version " + version);
        var template = reader.ReadInt32();
        var virtualTextures = reader.ReadBoolean();
        if (virtualTextures)
        {
            var stacks = reader.ReadInt32();
            if (stacks < 0 || stacks > 2) return Fail("Invalid native surface VT stack count.");
            for (var stack = 0; stack < 2; stack++)
            {
                Skip(reader, 8); // Unbiased stack dimensions.
                Skip(reader, 64); // Original texture identities, not payload dependencies in VT mode.
                for (var layer = 0; layer < 4; layer++)
                {
                    var cid = ReadHash(reader);
                    if (stack < stacks) Add(cid);
                }
            }
            if (version != 1 || Present(reader)) Add(ReadHash(reader));
        }
        if (version == 1)
        {
            Dictionary(4); // Floats.
            Dictionary(4); // Ints.
            Dictionary(16); // Vectors.
            Dictionary(16); // Colors.
            if (Present(reader))
                for (var n = Count(reader); n > 0; n--)
                {
                    var property = reader.ReadString();
                    if (Present(reader)) Texture(property, ReadHash(reader));
                }
            Strings(); // Keywords may contain CID-looking text; they are not references.
        }
        else
        {
            // Legacy ShaderPropertyType records, terminated by 255 (native ReadPropertiesLegacy).
            if (stream.Position < stream.Length)
                while (true)
                {
                    var type = reader.ReadByte();
                    if (type == 255) break;
                    var property = reader.ReadString();
                    switch (type)
                    {
                        case 0: case 1: Skip(reader, 16); break; // Color, Vector.
                        case 2: case 3: Skip(reader, 4); break; // Float, Range.
                        case 4: Texture(property, ReadHash(reader)); break;
                        default: return Fail("Unknown legacy surface property type " + type);
                    }
                }
            if (version >= 11) Strings();
        }
        if(failure!=null)return false;
        if (stream.Position != stream.Length) return Fail("Unexpected trailing native surface data.");
        references = new string[result.Count];
        result.CopyTo(references);
        return true;

        bool Present(BinaryReader input)
        {
            byte value=input.ReadByte();
            if(value==0)return false;
            if(value==255)return true;
            Fail("Invalid native surface null marker.");return false;
        }
        int Count(BinaryReader input)
        {
            int count=input.ReadInt32();
            if(count<0 || count>input.BaseStream.Length-input.BaseStream.Position)
            {Fail("Invalid native surface collection count.");return 0;}
            return count;
        }
        void Skip(BinaryReader input,int count)
        {
            if(count<0 || count>input.BaseStream.Length-input.BaseStream.Position)
            {Fail("Truncated native surface data.");input.BaseStream.Position=input.BaseStream.Length;return;}
            input.BaseStream.Position+=count;
        }
        void Add(string cid) { if (cid != "00000000000000000000000000000000") result.Add(cid); }
        void Texture(string property, string cid)
        {
            // SurfaceAsset.IsHandledByVirtualTexturing bypasses these original bindings.
            // Their data lives in VTSurface/VTTexture payloads, which are traversed above.
            // Non-stack textures are still ordinary dependencies even on a VT surface.
            if (!virtualTextures || !inVirtualStack(template, property)) Add(cid);
        }
        void Dictionary(int size)
        {
            if (!Present(reader)) return;
            for (var n = Count(reader); n > 0; n--) { reader.ReadString(); Skip(reader, size); }
        }
        void Strings()
        {
            if (!Present(reader)) return;
            for (var n = Count(reader); n > 0; n--) reader.ReadString();
        }
        }
        catch(Exception exception){failure??="Native surface read: "+exception.Message;return false;}
        finally{if(failure!=null){references=Array.Empty<string>();error=failure;}}
    }

    internal static string ReadHash(BinaryReader reader)
    {
        const string digits = "0123456789abcdef";
        var chars = new char[32];
        for (var i = 0; i < 16; i++)
        {
            var value = reader.ReadByte();
            chars[i * 2] = digits[value & 15];
            chars[i * 2 + 1] = digits[value >> 4];
        }
        return new string(chars);
    }
}
