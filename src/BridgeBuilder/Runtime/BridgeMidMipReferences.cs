using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// MidMips has reverse references: native streaming indexes this cache by Surface CID.
// These edges are not present in the serialized Surface texture dictionaries.
internal static class BridgeMidMipReferences
{
    internal static int RequiredLevels(Stream stream, bool surface)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (!surface)
        {
            if (reader.ReadUInt16() != 1) throw new InvalidDataException("Unsupported native VTTexture version.");
            byte marker = reader.ReadByte();
            if (marker == 255) reader.BaseStream.Position += 16;
            else if (marker != 0) throw new InvalidDataException("Invalid native VTTexture reference.");
            reader.BaseStream.Position += 8;
            int levels = reader.ReadUInt16();
            reader.BaseStream.Position += 5;
            // The native loader checks the count when the mip tail is present.
            return reader.ReadInt32() > 0 ? levels : 0;
        }
        // Native VTSurfaceDataHandler.Data: two fixed stacks, four layers each.
        var header = reader.ReadBytes(498);
        if (header.Length != 498) throw new EndOfStreamException();
        int stacks = BitConverter.ToInt32(header, 490);
        if (BitConverter.ToUInt16(header, 0) != 1 || stacks < 0 || stacks > 2)
            throw new InvalidDataException("Unsupported native VTSurface header.");
        int result = 0;
        for (int stack = 0; stack < stacks; stack++)
        {
        int layers = BitConverter.ToInt32(header, 26 + stack * 40 + 24);
        if (layers < 0 || layers > 4) throw new InvalidDataException("Invalid native VTSurface layer count.");
        for (int layer = 0; layer < layers; layer++)
        {
            int at = 106 + (stack * 4 + layer) * 48;
            if (header[at] == 0 || BitConverter.ToInt32(header, at + 16) <= 0) continue;
            int levels = BitConverter.ToInt32(header, at + 20);
            if (levels != 0 && result != 0 && levels != result)
                throw new InvalidDataException("Native VTSurface layers disagree on mid-mip levels.");
            result = Math.Max(result, levels);
        }
        }
        return result;
    }

    internal static string[] Read(byte[] bytes)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes, false));
        int tile = reader.ReadInt32(), levels = reader.ReadInt32(), group = reader.ReadInt32();
        int stacks = reader.ReadInt32(), groups = reader.ReadInt32(), entries = reader.ReadInt32();
        if (tile < 16 || tile > 16384 || (tile & (tile - 1)) != 0 || levels < 1 || levels > 8 ||
            group != 1 << (2 * levels) || stacks < 1 || stacks > 16 || groups < 0 || entries < 0)
            throw new InvalidDataException("Invalid native MidMips header.");
        var categories = new List<(int Count, int First, int Tiles, bool Disabled)>();
        int Count() { int n = reader.ReadInt32(); if (n < 0 || n > bytes.Length / 4) throw new InvalidDataException("Invalid MidMips count."); return n; }
        for (int stack = 0; stack < stacks; stack++)
        {
            int count = Count();
            for (int i = 0; i < count; i++)
            {
                int width = reader.ReadInt32(), height = reader.ReadInt32();
                int n = Count(), first = Count(), tiles = Count();
                bool disabled = reader.ReadBoolean(); reader.ReadInt32();
                if (width <= 0 || height <= 0) throw new InvalidDataException("Invalid MidMips dimensions.");
                categories.Add((n, first, tiles, disabled));
            }
        }
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indices = new List<int>();
        string Hash()
        {
            var value = reader.ReadBytes(16);
            if (value.Length != 16) throw new EndOfStreamException();
            const string hex = "0123456789abcdef";
            return string.Concat(value.SelectMany(b => new[] { hex[b & 15], hex[b >> 4] }));
        }
        foreach (var category in categories)
        {
            if (category.Disabled) continue;
            for (int i = 0; i < category.Count; i++)
            {
                identities.Add(Hash()); int mask = reader.ReadInt32(); short ordinal = reader.ReadInt16();
                if (mask < 0 || mask > 15 || ordinal != i % group) throw new InvalidDataException("Invalid MidMips group entry.");
                int duplicates = Count();
                for (int d = 0; d < duplicates; d++) identities.Add(Hash());
                for (int level = 0; level < levels; level++)
                {
                    int index = Count(), groupedMask = reader.ReadInt32();
                    if (groupedMask < 0 || groupedMask > 15 || (groupedMask & mask) != mask)
                        throw new InvalidDataException("Invalid MidMips layer mask.");
                    indices.Add(index + Enumerable.Range(0, 4).Count(bit => (groupedMask & (1 << bit)) != 0));
                }
            }
        }
        int tileCount = Count(); long alignment = reader.BaseStream.Position;
        if (reader.ReadInt64() != alignment || indices.Any(i => i > tileCount) ||
            categories.Any(c => !c.Disabled && (long)c.First + c.Tiles > tileCount))
            throw new InvalidDataException("Invalid MidMips tile offsets.");
        long tileBytes = checked((long)(tile + 16) * (tile + 16) * 5 / 4);
        if (reader.BaseStream.Position + tileBytes * tileCount != bytes.LongLength)
            throw new InvalidDataException("Incomplete native MidMips payload.");
        return identities.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }
}
