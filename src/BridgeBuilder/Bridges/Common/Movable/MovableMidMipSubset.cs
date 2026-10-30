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

    internal static byte[]? Copy(Stream source, string sourceCid, string targetCid, int tile, int levels)
    {
        byte[] from = Hash(sourceCid), to = Hash(targetCid);
        if (!source.CanSeek) throw new InvalidDataException("Native MidMips source must support bounded seeking.");
        using var reader = new BinaryReader(source, System.Text.Encoding.UTF8, true);
        int actualTile = reader.ReadInt32(), actualLevels = reader.ReadInt32(), group = reader.ReadInt32();
        int stacks = reader.ReadInt32(), groups = reader.ReadInt32(), entries = reader.ReadInt32();
        if (actualTile != tile || actualLevels != levels || tile < 16 || tile > 16384 ||
            (tile & (tile - 1)) != 0 || levels < 1 || levels > 8 ||
            (tile >> (levels + 3)) == 0 || group != 1 << (2 * levels) ||
            stacks < 1 || stacks > 16 || groups < 0 || entries < 0)
            throw new InvalidDataException("Unsupported native MidMips layout.");
        int Count()
        {
            int n = reader.ReadInt32();
            if (n < 0 || n > source.Length / 4) throw new InvalidDataException("Invalid native MidMips count.");
            return n;
        }
        var categories = new List<Category>();
        for (int stack = 0; stack < stacks; stack++)
        {
            int count = Count();
            for (int i = 0; i < count; i++)
            {
                var c = new Category { Stack = stack, Width = reader.ReadInt32(), Height = reader.ReadInt32(),
                    Count = Count(), First = Count(), Tiles = Count(), Disabled = reader.ReadBoolean(), Bias = reader.ReadInt32() };
                if (c.Width <= 0 || c.Height <= 0) throw new InvalidDataException("Invalid native MidMips dimensions.");
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
                int mask = reader.ReadInt32(), ordinal = reader.ReadInt16(), duplicates = Count();
                if (mask < 0 || mask > 15 || ordinal != i % group)
                    throw new InvalidDataException("Invalid native MidMips entry.");
                for (int d = 0; d < duplicates; d++) match |= reader.ReadBytes(16).SequenceEqual(from);
                var indices = new (int First, int Mask)[levels];
                for (int level = 0; level < levels; level++)
                {
                    int first = Count(), groupedMask = reader.ReadInt32();
                    if (groupedMask < 0 || groupedMask > 15 || (groupedMask & mask) != mask)
                        throw new InvalidDataException("Invalid native MidMips mask.");
                    indices[level] = (first, groupedMask);
                    largestTileEnd = Math.Max(largestTileEnd, (long)first + Bits(groupedMask));
                }
                if (match) selected.Add(new Entry { Category = c, Mask = mask, Ordinal = ordinal, Indices = indices });
            }
        }
        int tiles = Count(); long alignment = source.Position;
        if (reader.ReadInt64() != alignment || largestTileEnd > tiles ||
            categories.Any(c => !c.Disabled && (long)c.First + c.Tiles > tiles))
            throw new InvalidDataException("Invalid native MidMips offsets.");
        long dataStart = source.Position;
        int mainBytes = checked((tile + 16) * (tile + 16)), tileBytes = checked(mainBytes * 5 / 4);
        if (dataStart + (long)tiles * tileBytes != source.Length)
            throw new InvalidDataException("Incomplete native MidMips source.");
        if (selected.Count == 0) return null;
        if (selected.GroupBy(e => e.Category.Stack).Any(g => g.Count() != 1) ||
            selected.Any(e => e.Category.Width != e.Category.Height || e.Mask == 0))
            throw new InvalidDataException("Ambiguous native Surface MidMips membership.");
        int outputTiles = selected.Sum(e => levels * Bits(e.Mask));
        if ((long)outputTiles * tileBytes > 10_000_000)
            throw new InvalidDataException("Selected native MidMips alone exceed the owner material limit.");
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
            var payload = new byte[tileBytes];
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
                        int read = source.Read(payload, at, remaining);
                        if (read == 0) throw new EndOfStreamException();
                        at += read; remaining -= read;
                    }
                }
            }
            writer.Write(payload);
        }
        return output.ToArray();
    }

    private static int Bits(int mask) { int count = 0; for (; mask != 0; mask >>= 1) count += mask & 1; return count; }
    private static byte[] Hash(string cid)
    {
        if (cid.Length != 32 || cid.Any(c => !Uri.IsHexDigit(c))) throw new InvalidDataException("Invalid native Surface CID.");
        var bytes = new byte[16];
        for (int i = 0; i < 16; i++) bytes[i] = Convert.ToByte(new string(new[] { cid[i * 2 + 1], cid[i * 2] }), 16);
        return bytes;
    }
}
