using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BridgeBuilder.Bridges;

// Decode the original mip-zero blocks into private generation-owned textures.
// Native stream, decompressor and upload faults are contained at this operation boundary;
// malformed schemas return explicit results rather than throwing from parser helpers.
internal static class MovableVtTextureSource
{
    internal static bool TryReadTexture(Stream stream, Func<byte[],byte[]> decompress,
        out Texture2D texture, out string? error)
    {
        texture=null!;error=null;
        var read=new ReadContext(stream);
        try
        {
            using var r=new BinaryReader(stream,System.Text.Encoding.UTF8,true);
            if(r.ReadUInt16()!=1) return read.Fail("Unsupported native VT texture version.");
            if(r.ReadByte()!=255) return read.Fail("Native VT original texture reference is absent.");
            if(!read.Exact(r,16,out _))return false;
            if(r.ReadUInt16()!=0)return read.Fail("Unsupported native VT texture preprocessing.");
            int tile=r.ReadUInt16(),width=r.ReadUInt16(),height=r.ReadUInt16();
            r.ReadUInt16();r.ReadByte();
            var format=(GraphicsFormat)r.ReadByte();var filter=(FilterMode)r.ReadByte();
            var wrap=(TextureWrapMode)r.ReadByte();int aniso=r.ReadByte(),tail=r.ReadInt32();
            if(tail<0)return read.Fail("Invalid native VT mip payload size.");
            if(!read.Seek(r,checked(stream.Position+tail)))return false;
            int count=r.ReadInt32();
            if(!read.TopTiles(width,height,tile,out var top))return false;
            if(count<top || count>(stream.Length-stream.Position)/4)return read.Fail("Incomplete native VT tile table.");
            var offsets=new int[count];for(int i=0;i<count;i++)offsets[i]=r.ReadInt32();
            long start=stream.Position;
            if(!read.ByteSize(width,height,format,out var size))return false;
            var raw=new byte[size];
            for(int i=0;i<top;i++)
            {
                int first=i==0?0:offsets[i-1],last=offsets[i];
                if(first<0 || last<=first)return read.Fail("Invalid native VT tile offsets.");
                if(!read.Seek(r,checked(start+first)) || !read.Exact(r,last-first,out var packed))return false;
                var bytes=decompress(packed);
                if(!read.CopyTile(bytes,0,raw,width,height,tile,format,i))return false;
            }
            return read.Upload(width,height,format,filter,wrap,aniso,raw,out texture);
        }
        catch(Exception exception){return read.Fail("Native VT texture read/upload: "+exception);}
        finally{error=read.Error;if(error!=null)texture=null!;}
    }

    internal static bool TryReadSurface(Stream stream,Func<byte[],byte[]> decompress,
        out Dictionary<(int Stack,int Layer),Texture2D> textures,out string? error)
    {
        textures=new();error=null;var made=new Dictionary<(int,int),Texture2D>();
        var read=new ReadContext(stream);bool complete=false;
        try
        {
            using var r=new BinaryReader(stream,System.Text.Encoding.UTF8,true);
            if(!read.Exact(r,498,out var header))return false;
            if(BitConverter.ToUInt16(header,0)!=1)return read.Fail("Unsupported native VT surface version.");
            int Int(int offset)=>BitConverter.ToInt32(header,offset);
            int stacks=Int(490),footer=Int(494),tile=Int(18);
            if(stacks<0 || stacks>2)return read.Fail("Invalid native VT stack count.");
            long table=footer;
            for(int stack=0;stack<stacks;stack++)
            {
                int si=26+stack*40,width=Int(si),height=Int(si+4),count=Int(si+8),start=Int(si+20),layers=Int(si+24);
                if(layers<0 || layers>4)return read.Fail("Invalid native VT layer count.");
                var formats=new GraphicsFormat[layers];var raw=new byte[layers][];var stride=new int[layers];int active=0;
                for(int layer=0;layer<layers;layer++)
                {
                    int li=106+stack*192+layer*48;if(header[li]==0)continue;
                    if(BitConverter.ToUInt16(header,li+8)!=0)return read.Fail("Unsupported native VT surface preprocessing.");
                    formats[layer]=(GraphicsFormat)Int(li+4);int bytes=Int(li+24);
                    if(!read.ByteSize(checked(tile+16),checked(tile+16),formats[layer],out var tileBytes))return false;
                    if(bytes!=tileBytes)return read.Fail("Native VT tile block layout mismatch.");
                    stride[layer]=checked(bytes+bytes/4);
                    if(!read.ByteSize(width,height,formats[layer],out var size))return false;
                    raw[layer]=new byte[size];active++;
                }
                if(active==0)continue;
                if(!read.TopTiles(width,height,tile,out var top))return false;
                if(count<top)return read.Fail("Incomplete native VT surface tiles.");
                if(!read.Seek(r,table))return false;
                var offsets=new int[top];for(int i=0;i<top;i++)offsets[i]=r.ReadInt32();
                table=checked(table+4L*count);
                for(int i=0;i<top;i++)
                {
                    int first=i==0?0:offsets[i-1],last=offsets[i];
                    if(first<0 || last<=first)return read.Fail("Invalid native VT surface tile offsets.");
                    if(!read.Seek(r,checked((long)start+first)) || !read.Exact(r,last-first,out var packed))return false;
                    var bytes=decompress(packed);int offset=0;
                    for(int layer=0;layer<layers;layer++)if(raw[layer]!=null)
                    {
                        if(!read.CopyTile(bytes,offset,raw[layer],width,height,tile,formats[layer],i))return false;
                        offset=checked(offset+stride[layer]);
                    }
                    if(offset!=bytes.Length)return read.Fail("Native VT surface layer payload mismatch.");
                }
                for(int layer=0;layer<layers;layer++)if(raw[layer]!=null)
                {
                    if(!read.Upload(width,height,formats[layer],FilterMode.Trilinear,TextureWrapMode.Clamp,16,raw[layer],out var texture))return false;
                    made.Add((stack,layer),texture);
                }
            }
            textures=made;complete=true;return true;
        }
        catch(Exception exception){return read.Fail("Native VT surface read/upload: "+exception);}
        finally
        {
            if(!complete)foreach(var texture in made.Values)read.Destroy(texture);
            error=read.Error;
        }
    }

    private sealed class ReadContext
    {
        private readonly Stream _stream;
        internal string? Error;
        internal ReadContext(Stream stream){_stream=stream;}
        internal bool Fail(string error){Error=Error==null?error:Error+"; "+error;return false;}
        internal bool TopTiles(int width,int height,int tile,out int count)
        {
            count=0;
            if(tile<1 || width<tile || height<tile || width%tile!=0 || height%tile!=0)
                return Fail("Invalid native VT top-level tile dimensions.");
            count=checked(width/tile*(height/tile));return true;
        }
        internal bool ByteSize(int width,int height,GraphicsFormat format,out int size)
        {
            size=0;int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format);
            int bytes=(int)GraphicsFormatUtility.GetBlockSize(format);
            if(width<=0 || height<=0 || bw<=0 || bh<=0 || bytes<=0)return Fail("Invalid native VT texture format or dimensions.");
            size=checked((width+bw-1)/bw*((height+bh-1)/bh)*bytes);return true;
        }
        internal bool CopyTile(byte[] source,int offset,byte[] target,int width,int height,int tile,GraphicsFormat format,int index)
        {
            int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format);
            int bytes=(int)GraphicsFormatUtility.GetBlockSize(format);
            if(bw<=0 || bh<=0 || bytes<=0 || tile%bw!=0 || tile%bh!=0 || 8%bw!=0 || 8%bh!=0)
                return Fail("Unsupported native VT block alignment.");
            int pitch=checked((tile+16)/bw*bytes),rows=tile/bh,columns=tile/bw,perRow=checked(width/bw*bytes);
            int x=index%(width/tile),y=index/(width/tile);
            for(int row=0;row<rows;row++)
            {
                int from=checked(offset+(row+8/bh)*pitch+8/bw*bytes),to=checked((y*rows+row)*perRow+x*columns*bytes),count=checked(columns*bytes);
                if(from<0 || to<0 || count>source.Length-from || count>target.Length-to)return Fail("Incomplete native VT tile pixels.");
                Buffer.BlockCopy(source,from,target,to,count);
            }
            return true;
        }
        internal bool Upload(int width,int height,GraphicsFormat format,FilterMode filter,TextureWrapMode wrap,int aniso,byte[] bytes,out Texture2D result)
        {
            result=null!;Texture2D? texture=null;
            try
            {
                texture=new Texture2D(width,height,format,TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit);
                texture.filterMode=filter;texture.wrapMode=wrap;texture.anisoLevel=aniso;texture.hideFlags=HideFlags.DontSave;
                texture.LoadRawTextureData(bytes);texture.Apply(false,true);result=texture;return true;
            }
            catch(Exception exception){if(texture!=null)Destroy(texture);return Fail("Native VT upload: "+exception);}
        }
        internal void Destroy(Texture2D texture)
        {try{UnityEngine.Object.Destroy(texture);}catch(Exception exception){Fail("Native VT cleanup: "+exception);}}
        internal bool Exact(BinaryReader reader,int count,out byte[] bytes)
        {
            bytes=Array.Empty<byte>();
            if(count<0 || count>_stream.Length-_stream.Position)return Fail("Incomplete native VT payload.");
            bytes=reader.ReadBytes(count);return bytes.Length==count || Fail("Incomplete native VT payload.");
        }
        internal bool Seek(BinaryReader reader,long position)
        {
            if(position<0 || position>_stream.Length)return Fail("Invalid native VT payload offset.");
            _stream.Position=position;return true;
        }
    }
}
