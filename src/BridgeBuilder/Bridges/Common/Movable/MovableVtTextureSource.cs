using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BridgeBuilder.Bridges;

// Generation samples mip zero explicitly. Read its original compressed blocks
// before uploading; native debug loaders discard CPU data after a quality-biased
// upload, so their returned textures cannot safely be repaired or cloned later.
internal static class MovableVtTextureSource
{
    internal static Texture2D ReadTexture(Stream stream,Func<byte[],byte[]> decompress)
    {
        using var reader=new BinaryReader(stream,System.Text.Encoding.UTF8,true);
        if(reader.ReadUInt16()!=1)throw new NotSupportedException("Unsupported native VT texture version.");
        if(reader.ReadByte()!=255)throw new InvalidDataException("Native VT original texture reference is absent.");
        Exact(reader,16); // Original TextureAsset CID, not a generation dependency.
        if(reader.ReadUInt16()!=0)throw new NotSupportedException("Unsupported native VT texture preprocessing.");
        int tile=reader.ReadUInt16(),width=reader.ReadUInt16(),height=reader.ReadUInt16();
        reader.ReadUInt16();reader.ReadByte(); // Mid-mip and complete mip counts.
        var format=(GraphicsFormat)reader.ReadByte();
        var filter=(FilterMode)reader.ReadByte();var wrap=(TextureWrapMode)reader.ReadByte();
        int aniso=reader.ReadByte(),tail=reader.ReadInt32();
        if(tail<0)throw new InvalidDataException("Invalid native VT mip payload size.");
        Seek(reader,checked(reader.BaseStream.Position+tail));
        int count=reader.ReadInt32(),top=TopTiles(width,height,tile);
        if(count<top || count>(reader.BaseStream.Length-reader.BaseStream.Position)/4)
            throw new InvalidDataException("Incomplete native VT tile table.");
        var offsets=new int[count];for(int i=0;i<count;i++)offsets[i]=reader.ReadInt32();
        long start=reader.BaseStream.Position;
        var raw=new byte[ByteSize(width,height,format)];
        for(int i=0;i<top;i++)
        {
            int first=i==0?0:offsets[i-1],last=offsets[i];
            if(first<0 || last<=first)throw new InvalidDataException("Invalid native VT tile offsets.");
            Seek(reader,checked(start+first));
            var bytes=decompress(Exact(reader,last-first));
            CopyTile(bytes,0,raw,width,height,tile,format,i);
        }
        return Upload(width,height,format,filter,wrap,aniso,raw);
    }

    internal static Dictionary<(int Stack,int Layer),Texture2D> ReadSurface(Stream stream,Func<byte[],byte[]> decompress)
    {
        using var reader=new BinaryReader(stream,System.Text.Encoding.UTF8,true);
        var header=Exact(reader,498); // Native VTSurface Data version 1, packed layout.
        if(BitConverter.ToUInt16(header,0)!=1)throw new NotSupportedException("Unsupported native VT surface version.");
        int Int(int offset)=>BitConverter.ToInt32(header,offset);
        int stacks=Int(490),footer=Int(494),tile=Int(18);
        if(stacks<0 || stacks>2)throw new InvalidDataException("Invalid native VT stack count.");
        var result=new Dictionary<(int,int),Texture2D>();
        try
        {
            long table=footer;
            for(int stack=0;stack<stacks;stack++)
            {
                int si=26+stack*40,width=Int(si),height=Int(si+4),count=Int(si+8),start=Int(si+20),layers=Int(si+24);
                if(layers<0 || layers>4)throw new InvalidDataException("Invalid native VT layer count.");
                var formats=new GraphicsFormat[layers];var raw=new byte[layers][];
                var stride=new int[layers];int active=0;
                for(int layer=0;layer<layers;layer++)
                {
                    int li=106+stack*192+layer*48;
                    if(header[li]==0)continue;
                    if(BitConverter.ToUInt16(header,li+8)!=0)
                        throw new NotSupportedException("Unsupported native VT surface preprocessing.");
                    formats[layer]=(GraphicsFormat)Int(li+4);
                    int bytes=Int(li+24);
                    if(bytes!=ByteSize(checked(tile+16),checked(tile+16),formats[layer]))
                        throw new InvalidDataException("Native VT tile block layout mismatch.");
                    stride[layer]=checked(bytes+bytes/4);
                    raw[layer]=new byte[ByteSize(width,height,formats[layer])];active++;
                }
                if(active==0)continue;
                int top=TopTiles(width,height,tile);
                if(count<top)throw new InvalidDataException("Incomplete native VT surface tiles.");
                Seek(reader,table);
                var offsets=new int[top];for(int i=0;i<top;i++)offsets[i]=reader.ReadInt32();
                table=checked(table+4L*count);
                for(int i=0;i<top;i++)
                {
                    int first=i==0?0:offsets[i-1],last=offsets[i];
                    if(first<0 || last<=first)throw new InvalidDataException("Invalid native VT surface tile offsets.");
                    Seek(reader,checked((long)start+first));
                    var bytes=decompress(Exact(reader,last-first));int offset=0;
                    for(int layer=0;layer<layers;layer++)if(raw[layer]!=null)
                    {
                        CopyTile(bytes,offset,raw[layer],width,height,tile,formats[layer],i);
                        offset=checked(offset+stride[layer]);
                    }
                    if(offset!=bytes.Length)throw new InvalidDataException("Native VT surface layer payload mismatch.");
                }
                for(int layer=0;layer<layers;layer++)if(raw[layer]!=null)
                    result.Add((stack,layer),Upload(width,height,formats[layer],FilterMode.Trilinear,TextureWrapMode.Clamp,16,raw[layer]));
            }
            return result;
        }
        catch
        {
            foreach(var texture in result.Values)UnityEngine.Object.Destroy(texture);
            throw;
        }
    }

    private static int TopTiles(int width,int height,int tile)
    {
        if(tile<1 || width<tile || height<tile || width%tile!=0 || height%tile!=0)
            throw new InvalidDataException("Invalid native VT top-level tile dimensions.");
        return checked(width/tile*(height/tile));
    }
    private static int ByteSize(int width,int height,GraphicsFormat format)
    {
        int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format);
        int bytes=(int)GraphicsFormatUtility.GetBlockSize(format);
        if(width<=0 || height<=0 || bw<=0 || bh<=0 || bytes<=0)
            throw new InvalidDataException("Invalid native VT texture format or dimensions.");
        return checked((width+bw-1)/bw*((height+bh-1)/bh)*bytes);
    }
    private static void CopyTile(byte[] source,int offset,byte[] target,int width,int height,int tile,GraphicsFormat format,int index)
    {
        int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format);
        int bytes=(int)GraphicsFormatUtility.GetBlockSize(format);
        if(tile%bw!=0 || tile%bh!=0 || 8%bw!=0 || 8%bh!=0)
            throw new InvalidDataException("Unsupported native VT block alignment.");
        int pitch=(tile+16)/bw*bytes,rows=tile/bh,columns=tile/bw,perRow=width/bw*bytes;
        int x=index%(width/tile),y=index/(width/tile);
        for(int row=0;row<rows;row++)
        {
            int from=checked(offset+(row+8/bh)*pitch+8/bw*bytes);
            int to=checked((y*rows+row)*perRow+x*columns*bytes);
            Buffer.BlockCopy(source,from,target,to,columns*bytes);
        }
    }
    private static Texture2D Upload(int width,int height,GraphicsFormat format,FilterMode filter,TextureWrapMode wrap,int aniso,byte[] bytes)
    {
        var texture=new Texture2D(width,height,format,TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit)
            {filterMode=filter,wrapMode=wrap,anisoLevel=aniso,hideFlags=HideFlags.DontSave};
        try{texture.LoadRawTextureData(bytes);texture.Apply(false,true);return texture;}
        catch{UnityEngine.Object.Destroy(texture);throw;}
    }
    private static byte[] Exact(BinaryReader reader,int count)
    {
        if(count<0 || count>reader.BaseStream.Length-reader.BaseStream.Position)throw new EndOfStreamException("Incomplete native VT payload.");
        var bytes=reader.ReadBytes(count);
        if(bytes.Length!=count)throw new EndOfStreamException("Incomplete native VT payload.");
        return bytes;
    }
    private static void Seek(BinaryReader reader,long position)
    {
        if(position<0 || position>reader.BaseStream.Length)throw new EndOfStreamException("Invalid native VT payload offset.");
        reader.BaseStream.Position=position;
    }
}
