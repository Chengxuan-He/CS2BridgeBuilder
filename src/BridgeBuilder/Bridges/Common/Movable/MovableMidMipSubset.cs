using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Bridges;

// Native BC7 blocks are relocated, never decoded, filtered or recompressed.
// Only the selected Surface's entries are retained; unrelated atlas members stay behind.
internal static class MovableMidMipSubset
{
    private sealed class Category
    {
        internal int Stack, Width, Height, Count, First, Tiles, Bias;
        internal bool Disabled;
    }
    private sealed class Entry
    {
        internal Category Category = null!;
        internal int Mask, Ordinal, OutputFirst;
        internal (int First, int Mask)[] Indices = null!;
    }

    internal static bool TryCopy(Stream source, string sourceCid, string targetCid, int tile, int levels, out byte[]? payload, out string? error)
    {
        payload=null;error=null;string? failure=null;
        bool Fail(string text){failure=text;return false;}
        try
        {
        if(!TryHash(sourceCid,out var from) || !TryHash(targetCid,out var to))return Fail("Invalid native Surface CID.");
        if (!source.CanSeek) return Fail("Native MidMips source must support bounded seeking.");
        using var reader = new BinaryReader(source, System.Text.Encoding.UTF8, true);
        int actualTile = reader.ReadInt32(), actualLevels = reader.ReadInt32(), group = reader.ReadInt32();
        int stacks = reader.ReadInt32(), groups = reader.ReadInt32(), entries = reader.ReadInt32();
        if (actualTile != tile || actualLevels != levels || tile < 16 || tile > 16384 ||
            (tile & (tile - 1)) != 0 || levels < 1 || levels > 8 ||
            (tile >> (levels + 3)) == 0 || group != 1 << (2 * levels) ||
            stacks < 1 || stacks > 16 || groups < 0 || entries < 0)
            return Fail("Unsupported native MidMips layout.");
        bool Count(out int n)
        {
            n=reader.ReadInt32();
            return (n>=0 && n<=source.Length/4) || Fail("Invalid native MidMips count.");
        }
        var categories = new List<Category>();
        for (int stack = 0; stack < stacks; stack++)
        {
            if(!Count(out var count))return false;
            for (int i = 0; i < count; i++)
            {
                var c=new Category {Stack=stack,Width=reader.ReadInt32(),Height=reader.ReadInt32()};
                if(!Count(out c.Count) || !Count(out c.First) || !Count(out c.Tiles))return false;
                c.Disabled=reader.ReadBoolean();c.Bias=reader.ReadInt32();
                if (c.Width <= 0 || c.Height <= 0) return Fail("Invalid native MidMips dimensions.");
                categories.Add(c);
            }
        }
        var selected = new List<Entry>();
        long largestTileEnd = 0;
        foreach (var c in categories)
        {
            if (c.Disabled) continue;
            for (int i = 0; i < c.Count; i++)
            {
                bool match = reader.ReadBytes(16).SequenceEqual(from);
                int mask=reader.ReadInt32(),ordinal=reader.ReadInt16();if(!Count(out var duplicates))return false;
                if (mask < 0 || mask > 15 || ordinal != i % group)
                    return Fail("Invalid native MidMips entry.");
                for (int d = 0; d < duplicates; d++) match |= reader.ReadBytes(16).SequenceEqual(from);
                var indices = new (int First, int Mask)[levels];
                for (int level = 0; level < levels; level++)
                {
                    if(!Count(out var first))return false;int groupedMask=reader.ReadInt32();
                    if (groupedMask < 0 || groupedMask > 15 || (groupedMask & mask) != mask)
                        return Fail("Invalid native MidMips mask.");
                    indices[level] = (first, groupedMask);
                    largestTileEnd = Math.Max(largestTileEnd, (long)first + Bits(groupedMask));
                }
                if (match) selected.Add(new Entry { Category = c, Mask = mask, Ordinal = ordinal, Indices = indices });
            }
        }
        if(!Count(out var tiles))return false;long alignment=source.Position;
        if (reader.ReadInt64() != alignment || largestTileEnd > tiles ||
            categories.Any(c => !c.Disabled && (long)c.First + c.Tiles > tiles))
            return Fail("Invalid native MidMips offsets.");
        long dataStart = source.Position;
        int mainBytes = checked((tile + 16) * (tile + 16)), tileBytes = checked(mainBytes * 5 / 4);
        if (dataStart + (long)tiles * tileBytes != source.Length)
            return Fail("Incomplete native MidMips source.");
        if (selected.Count == 0) return true; // Valid cache without this Surface membership.
        if (selected.GroupBy(e => e.Category.Stack).Any(g => g.Count() != 1) ||
            selected.Any(e => e.Category.Width != e.Category.Height || e.Mask == 0))
            return Fail("Ambiguous native Surface MidMips membership.");
        int outputTiles = selected.Sum(e => levels * Bits(e.Mask));
        if ((long)outputTiles * tileBytes > int.MaxValue)
            return Fail("Native MidMips payload exceeds addressable byte-array storage.");
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        writer.Write(tile); writer.Write(levels); writer.Write(group); writer.Write(stacks);
        writer.Write(selected.Count); writer.Write(selected.Count);
        int cursor = 0;
        for (int stack = 0; stack < stacks; stack++)
        {
            var matches = selected.Where(e => e.Category.Stack == stack).ToArray();
            writer.Write(matches.Length);
            foreach (var e in matches)
            {
                e.OutputFirst = cursor;
                int count = levels * Bits(e.Mask);
                writer.Write(e.Category.Width); writer.Write(e.Category.Height); writer.Write(1);
                writer.Write(cursor); writer.Write(count); writer.Write(false); writer.Write(e.Category.Bias);
                cursor += count;
            }
        }
        foreach (var e in selected)
        {
            writer.Write(to); writer.Write(e.Mask); writer.Write((short)0); writer.Write(0);
            for (int level = 0; level < levels; level++)
            { writer.Write(e.OutputFirst + level * Bits(e.Mask)); writer.Write(e.Mask); }
        }
        writer.Write(outputTiles); writer.Write(output.Position);
        foreach (var e in selected)
        for (int level = 0; level < levels; level++)
        for (int layer = 0; layer < 4; layer++)
        {
            if ((e.Mask & (1 << layer)) == 0) continue;
            var tilePayload = new byte[tileBytes];
            int index = e.Indices[level].First + Bits(e.Indices[level].Mask & ((1 << layer) - 1));
            int ordinal = e.Ordinal % (1 << (2 * (level + 1))), x = 0, y = 0;
            for (int bit = 0; bit <= level; bit++)
            { x |= ((ordinal >> (2 * bit)) & 1) << bit; y |= ((ordinal >> (2 * bit + 1)) & 1) << bit; }
            for (int half = 0; half < 2; half++)
            {
                int side = tile / 4 >> (level + 1 + half), pitch = (tile + 16) / 4 >> half;
                int pad = 2 >> half, start = half == 0 ? 0 : mainBytes;
                for (int row = 0; row < side; row++)
                {
                    int fromOffset = start + ((pad + y * side + row) * pitch + pad + x * side) * 16;
                    int toOffset = start + ((pad + row) * pitch + pad) * 16;
                    source.Position = dataStart + (long)index * tileBytes + fromOffset;
                    int remaining = side * 16, at = toOffset;
                    while (remaining > 0)
                    {
                        int read = source.Read(tilePayload, at, remaining);
                        if(read==0)return Fail("Incomplete native MidMips tile payload.");
                        at += read; remaining -= read;
                    }
                }
            }
            writer.Write(tilePayload);
        }
        payload=output.ToArray();return true;
        }
        catch(Exception exception){return Fail("Native MidMips read/write: "+exception);}
        finally{error=failure;if(error!=null)payload=null;}
    }

    private static int Bits(int mask) { int count = 0; for (; mask != 0; mask >>= 1) count += mask & 1; return count; }
    private static bool TryHash(string cid,out byte[] bytes)
    {
        bytes=Array.Empty<byte>();
        if (cid==null || cid.Length != 32 || cid.Any(c => !Uri.IsHexDigit(c))) return false;
        bytes = new byte[16];
        for (int i = 0; i < 16; i++) bytes[i] = Convert.ToByte(new string(new[] { cid[i * 2 + 1], cid[i * 2] }), 16);
        return true;
    }
}
