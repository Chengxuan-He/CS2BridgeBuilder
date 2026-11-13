using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Bridges;

// Lossless native BC7 carrier conversion. No pixel decoding, resampling or mip generation.
internal static class NativeVtMipPyramid
{
    internal static Dictionary<(int Stack, int Layer), byte[]> Convert(byte[] surface, byte[]? midSubset, Func<byte[], byte[]> decompress)
    {
        int I(int at) => BitConverter.ToInt32(surface, at);
        if (surface.Length < 498 || BitConverter.ToUInt16(surface, 0) != 1 || I(22) != 0)
            throw new InvalidDataException("Unsupported native VT surface layout.");
        int tile = I(18), stacks = I(490), table = I(494);
        if (tile < 16 || (tile & (tile - 1)) != 0 || stacks < 1 || stacks > 2)
            throw new InvalidDataException("Invalid native VT stack dimensions.");
        var middle = midSubset == null ? new Dictionary<(int,int),byte[][]>() : ReadMid(midSubset, tile);
        var result = new Dictionary<(int,int), byte[]>();
        for (int stack = 0; stack < stacks; stack++)
        {
            int at = 26 + stack * 40, width = I(at), height = I(at + 4), count = I(at + 8);
            int high = checked(498 + I(at + 16)), start = I(at + 20), layers = I(at + 24);
            if (width != height || width < tile || width % tile != 0 || layers < 0 || layers > 4 || count < 1)
                throw new InvalidDataException("Unsupported native VT layer dimensions.");
            var offsets = new int[count];
            for (int i = 0; i < count; i++) offsets[i] = I(checked(table + i * 4));
            table = checked(table + count * 4);
            var channels = new List<(int Layer,int Format,int Offset,int Tail,int TailSize,int Mid)>();
            int stride = 0;
            for (int layer = 0; layer < layers; layer++)
            {
                int li = 106 + stack * 192 + layer * 48;
                if (surface[li] == 0) continue;
                int format = I(li + 4), size = I(li + 24), tail = I(li + 16), mids = I(li + 20);
                if ((format != 108 && format != 109) || BitConverter.ToUInt16(surface, li + 8) != 0 || size != Blocks(tile + 16))
                    throw new InvalidDataException("Unsupported native VT BC7 encoding.");
                channels.Add((layer, format, stride, high, tail, mids));
                high = checked(high + tail); stride = checked(stride + size * 5 / 4);
            }
            var chains = channels.ToDictionary(c => c.Layer, _ => new List<byte[]>());
            var cached = new Dictionary<int,byte[]>();
            int index = 0;
            for (int side = width; side >= tile; side >>= 1)
            {
                var mip = channels.ToDictionary(c => c.Layer, _ => new byte[Blocks(side)]);
                for (int y = 0; y < side / tile; y++)
                for (int x = 0; x < side / tile; x++)
                {
                    if (index >= count) throw new InvalidDataException("Missing native VT tile.");
                    int first = index == 0 ? 0 : offsets[index - 1], last = offsets[index++];
                    if (first < 0 || last <= first) throw new InvalidDataException("Invalid native VT tile offsets.");
                    byte[] bytes = decompress(Slice(surface, checked(start + first), last - first));
                    if (bytes.Length != stride) throw new InvalidDataException("Native VT tile size mismatch.");
                    foreach (var c in channels)
                    {
                        int pitch = (tile + 16) / 4 * 16;
                        for (int row = 0; row < tile / 4; row++)
                            Buffer.BlockCopy(bytes, c.Offset + (row + 2) * pitch + 32, mip[c.Layer],
                                ((y * tile / 4 + row) * (side / 4) + x * tile / 4) * 16, tile * 4);
                        if (side == tile)
                        {
                            var copy = new byte[Blocks(tile / 2)];
                            for (int row = 0; row < tile / 8; row++)
                                Buffer.BlockCopy(bytes, c.Offset + Blocks(tile + 16) + (row + 1) * (pitch / 2) + 16, copy, row * tile * 2, tile * 2);
                            cached[c.Layer] = copy;
                        }
                    }
                }
                foreach (var c in channels) chains[c.Layer].Add(mip[c.Layer]);
            }
            if (index != count) throw new InvalidDataException("Unexpected native VT tile count.");
            foreach (var c in channels)
            {
                var chain = chains[c.Layer];
                if (c.Mid > 0)
                {
                    if (!middle.TryGetValue((stack,c.Layer), out var mids) || mids.Length != c.Mid || !mids[0].SequenceEqual(cached[c.Layer]))
                        throw new InvalidDataException("Missing or conflicting authored native middle mips.");
                    chain.AddRange(mids);
                }
                int mipCount = 1 + (int)Math.Log(width, 2), tailOffset = c.Tail;
                while (chain.Count < mipCount)
                {
                    int size = Blocks(Math.Max(1, width >> chain.Count));
                    chain.Add(Slice(surface, tailOffset, size)); tailOffset = checked(tailOffset + size);
                }
                if (tailOffset != c.Tail + c.TailSize) throw new InvalidDataException("Native mip tail size mismatch.");
                using var output = new MemoryStream(); using var writer = new BinaryWriter(output);
                writer.Write((ushort)5); writer.Write((ushort)width); writer.Write((ushort)height); writer.Write((ushort)1);
                writer.Write((byte)mipCount); writer.Write((byte)c.Format); writer.Write((byte)2);
                writer.Write((byte)2); writer.Write((byte)1); writer.Write((byte)16); writer.Write(-1);
                foreach (var mip in chain) writer.Write(mip);
                result.Add((stack,c.Layer), output.ToArray());
            }
        }
        return result;
    }

    private static Dictionary<(int,int),byte[][]> ReadMid(byte[] bytes, int tile)
    {
        using var stream = new MemoryStream(bytes, false); using var r = new BinaryReader(stream);
        int actualTile=r.ReadInt32(),levels=r.ReadInt32(),group=r.ReadInt32(),stacks=r.ReadInt32();r.ReadInt32();r.ReadInt32();
        if(actualTile!=tile || levels<1 || levels>8 || group!=(1<<(levels*2)) || stacks<1 || stacks>2)
            throw new InvalidDataException("Invalid selected native middle mip header.");
        var categories=new List<(int Stack,int Count)>();
        for(int st=0;st<stacks;st++)
        {
            int n=r.ReadInt32(); if(n<0 || n>2)throw new InvalidDataException("Unexpected selected mip categories.");
            for(int i=0;i<n;i++)
            {r.ReadInt32();r.ReadInt32();int count=r.ReadInt32();r.ReadInt32();r.ReadInt32();bool disabled=r.ReadBoolean();r.ReadInt32();
                if(disabled || count!=1)throw new InvalidDataException("Expected normalized selected middle mip membership."); categories.Add((st,count));}
        }
        var entries=new List<(int Stack,int Mask,(int First,int Mask)[] Indices)>();
        foreach(var c in categories)
        {
            r.ReadBytes(16);int mask=r.ReadInt32();short ordinal=r.ReadInt16();int duplicates=r.ReadInt32();
            if(ordinal!=0 || duplicates!=0 || mask<1 || mask>15)throw new InvalidDataException("Invalid selected mip membership.");
            var indices=new (int,int)[levels];for(int i=0;i<levels;i++)indices[i]=(r.ReadInt32(),r.ReadInt32());
            entries.Add((c.Stack,mask,indices));
        }
        int countTiles=r.ReadInt32();long alignment=stream.Position;
        if(r.ReadInt64()!=alignment)throw new InvalidDataException("Invalid native mip alignment.");
        int start=checked((int)stream.Position),pitch=(tile+16)/4*16,tileBytes=Blocks(tile+16)*5/4;
        if(start+(long)countTiles*tileBytes!=bytes.Length)throw new InvalidDataException("Incomplete native mip payload.");
        var result=new Dictionary<(int,int),byte[][]>();
        foreach(var e in entries)for(int layer=0;layer<4;layer++)
        {
            if((e.Mask&(1<<layer))==0)continue;
            var mips=new byte[levels][];
            for(int level=0;level<levels;level++)
            {
                var ix=e.Indices[level];int tileIndex=ix.First+Bits(ix.Mask&((1<<layer)-1));
                int side=tile>>(level+1);mips[level]=new byte[Blocks(side)];
                for(int row=0;row<side/4;row++)
                    Buffer.BlockCopy(bytes,start+tileIndex*tileBytes+(row+2)*pitch+32,mips[level],row*side*4,side*4);
            }
            result.Add((e.Stack,layer),mips);
        }
        return result;
    }
    private static int Blocks(int size)=>checked(((size+3)/4)*((size+3)/4)*16);
    private static int Bits(int n){int result=0;for(;n!=0;n>>=1)result+=n&1;return result;}
    private static byte[] Slice(byte[] data,int start,int count)
    {if(start<0 || count<0 || start>(long)data.Length-count)throw new InvalidDataException("Native mip range is outside its source.");var result=new byte[count];Buffer.BlockCopy(data,start,result,0,count);return result;}
}
