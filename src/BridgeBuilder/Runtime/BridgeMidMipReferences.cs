using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// MidMips has reverse references: native streaming indexes this cache by Surface CID.
// These edges are not present in the serialized Surface texture dictionaries.
internal static class BridgeMidMipReferences
{
    internal static bool TryRequiredLevels(Stream stream,bool surface,out int result,out string? error)
    {
        result=0;error=null;string? failure=null;
        bool Fail(string message){failure=message;return false;}
        try
        {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (!surface)
        {
            if (reader.ReadUInt16() != 1) return Fail("Unsupported native VTTexture version.");
            byte marker = reader.ReadByte();
            if (marker == 255) reader.BaseStream.Position += 16;
            else if (marker != 0) return Fail("Invalid native VTTexture reference.");
            reader.BaseStream.Position += 8;
            int levels = reader.ReadUInt16();
            reader.BaseStream.Position += 5;
            // The native loader checks the count when the mip tail is present.
            result=reader.ReadInt32()>0?levels:0;return true;
        }
        // Native VTSurfaceDataHandler.Data: two fixed stacks, four layers each.
        var header = reader.ReadBytes(498);
        if (header.Length != 498) return Fail("Incomplete native VTSurface header.");
        int stacks = BitConverter.ToInt32(header, 490);
        if (BitConverter.ToUInt16(header, 0) != 1 || stacks < 0 || stacks > 2)
            return Fail("Unsupported native VTSurface header.");
        result = 0;
        for (int stack = 0; stack < stacks; stack++)
        {
        int layers = BitConverter.ToInt32(header, 26 + stack * 40 + 24);
        if (layers < 0 || layers > 4) return Fail("Invalid native VTSurface layer count.");
        for (int layer = 0; layer < layers; layer++)
        {
            int at = 106 + (stack * 4 + layer) * 48;
            if (header[at] == 0 || BitConverter.ToInt32(header, at + 16) <= 0) continue;
            int levels = BitConverter.ToInt32(header, at + 20);
            if (levels != 0 && result != 0 && levels != result)
                return Fail("Native VTSurface layers disagree on mid-mip levels.");
            result = Math.Max(result, levels);
        }
        }
        return true;
        }
        catch(Exception exception){return Fail("Native mip header read: "+exception);}
        finally{error=failure;if(error!=null)result=0;}
    }

    internal static bool TryRead(byte[] bytes,out string[] result,out string? error)
    {
        result=Array.Empty<string>();error=null;
        try{using var stream=new MemoryStream(bytes,false);return TryRead(stream,out result,out error);}
        catch(Exception exception){error="Native MidMips input: "+exception;return false;}
    }

    // Native tiled cache pixels follow the header. Membership checks need only
    // the selected cache metadata and the bounded stream length.
    internal static bool TryRead(Stream stream,out string[] result,out string? error)
    {
        result=Array.Empty<string>();error=null;string? failure=null;
        bool Fail(string message){failure=message;return false;}
        try
        {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        int tile = reader.ReadInt32(), levels = reader.ReadInt32(), group = reader.ReadInt32();
        int stacks = reader.ReadInt32(), groups = reader.ReadInt32(), entries = reader.ReadInt32();
        if (tile < 16 || tile > 16384 || (tile & (tile - 1)) != 0 || levels < 1 || levels > 8 ||
            group != 1 << (2 * levels) || stacks < 1 || stacks > 16 || groups < 0 || entries < 0)
            return Fail("Invalid native MidMips header.");
        var categories = new List<(int Count, int First, int Tiles, bool Disabled)>();
        bool Count(out int n) { n = reader.ReadInt32(); return (n >= 0 && n <= stream.Length / 4) || Fail("Invalid MidMips count."); }
        for (int stack = 0; stack < stacks; stack++)
        {
            if(!Count(out int count))return false;
            for (int i = 0; i < count; i++)
            {
                int width = reader.ReadInt32(), height = reader.ReadInt32();
                if(!Count(out int n)||!Count(out int first)||!Count(out int tiles))return false;
                bool disabled = reader.ReadBoolean(); reader.ReadInt32();
                if (width <= 0 || height <= 0) return Fail("Invalid MidMips dimensions.");
                categories.Add((n, first, tiles, disabled));
            }
        }
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indices = new List<int>();
        bool Hash(out string hash)
        {
            hash="";
            var value = reader.ReadBytes(16);
            if (value.Length != 16)return Fail("Incomplete native MidMips identity.");
            const string hex = "0123456789abcdef";
            hash=string.Concat(value.SelectMany(b => new[] { hex[b & 15], hex[b >> 4] }));return true;
        }
        foreach (var category in categories)
        {
            if (category.Disabled) continue;
            for (int i = 0; i < category.Count; i++)
            {
                if(!Hash(out var identity))return false;identities.Add(identity); int mask = reader.ReadInt32(); short ordinal = reader.ReadInt16();
                if (mask < 0 || mask > 15 || ordinal != i % group) return Fail("Invalid MidMips group entry.");
                if(!Count(out int duplicates))return false;
                for (int d = 0; d < duplicates; d++){if(!Hash(out var duplicate))return false;identities.Add(duplicate);}
                for (int level = 0; level < levels; level++)
                {
                    if(!Count(out int index))return false;int groupedMask = reader.ReadInt32();
                    if (groupedMask < 0 || groupedMask > 15 || (groupedMask & mask) != mask)
                        return Fail("Invalid MidMips layer mask.");
                    indices.Add(index + Enumerable.Range(0, 4).Count(bit => (groupedMask & (1 << bit)) != 0));
                }
            }
        }
        if(!Count(out int tileCount))return false; long alignment = reader.BaseStream.Position;
        if (reader.ReadInt64() != alignment || indices.Any(i => i > tileCount) ||
            categories.Any(c => !c.Disabled && (long)c.First + c.Tiles > tileCount))
            return Fail("Invalid MidMips tile offsets.");
        long tileBytes = checked((long)(tile + 16) * (tile + 16) * 5 / 4);
        if (reader.BaseStream.Position + tileBytes * tileCount != stream.Length)
            return Fail("Incomplete native MidMips payload.");
        result=identities.OrderBy(x => x, StringComparer.Ordinal).ToArray();return true;
        }
        catch(Exception exception){return Fail("Native MidMips membership read: "+exception);}
        finally{error=failure;if(error!=null)result=Array.Empty<string>();}
    }
}
