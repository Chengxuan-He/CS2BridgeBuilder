using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BridgeBuilder.Bridges;

// Rebuild the authoring container, from the innermost program array outwards.
// BBS1 v1/v2 seeds use UnityFS 8, serialized-file 22 type trees and literal LZ4.
// These are format requirements, not compiler-output capacity limits.
internal static class MovableShaderContainer
{
    internal sealed class Replacement
    {
        internal int Offset;
        internal byte[] Expected = Array.Empty<byte>(), Compiled = Array.Empty<byte>();
        internal bool Used;
    }
    private sealed class Cursor
    {
        internal readonly byte[] Data;
        internal int Position;
        internal bool Big;
        internal Cursor(byte[] data, bool big = false) { Data = data; Big = big; }
        internal void Need(int n) { if (n < 0 || Position < 0 || Position > Data.Length - n) throw new InvalidDataException("Truncated shader container."); }
        internal byte[] Bytes(int n) { Need(n); var b = Slice(Data, Position, n); Position += n; return b; }
        internal int Byte() { Need(1); return Data[Position++]; }
        internal int U16() { var b = Bytes(2); return Big ? b[0]*256+b[1] : b[1]*256+b[0]; }
        internal int I32() { var b = Bytes(4); if (Big) Array.Reverse(b); return BitConverter.ToInt32(b,0); }
        internal long I64() { var b = Bytes(8); if (Big) Array.Reverse(b); return BitConverter.ToInt64(b,0); }
        internal int Count() { int n=I32(); if(n<0) throw new InvalidDataException("Invalid container count."); return n; }
        internal string String() { int start=Position; while(Byte()!=0) {} return Encoding.UTF8.GetString(Data,start,Position-start-1); }
        internal void Align(int n=4) { int end=checked((Position+n-1)&~(n-1)); Need(end-Position); Position=end; }
    }
    private static byte[] Slice(byte[] b,int p,int n) { if(p<0||n<0||p>b.Length-n) throw new InvalidDataException("Container range is invalid."); var r=new byte[n]; Buffer.BlockCopy(b,p,r,0,n); return r; }
    private static void Put(byte[] b,int p,long value,int bytes=4,bool big=false)
    { for(int i=0;i<bytes;i++) b[p+(big?bytes-i-1:i)]=(byte)((ulong)value>>(8*i)); }
    private static void Write(BinaryWriter w,long value,int bytes=4,bool big=false)
    { var b=new byte[bytes]; Put(b,0,value,bytes,big); w.Write(b); }
    private static void Align(BinaryWriter w,int n=4) { while(w.BaseStream.Position%n!=0)w.Write((byte)0); }
    private static void Require(bool ok,string message) { if(!ok)throw new InvalidDataException(message); }

    internal static byte[] Rebuild(byte[] seed, IReadOnlyList<Replacement> replacements)
    {
        var sorted=replacements.OrderBy(r=>r.Offset).ToArray(); int last=0;
        foreach(var r in sorted)
        {
            r.Used=false;
            Require(r.Expected.Length>0 && r.Offset>=last && r.Offset<=seed.Length-r.Expected.Length,"Overlapping or invalid shader program slot.");
            Require(Slice(seed,r.Offset,r.Expected.Length).SequenceEqual(r.Expected),"Generated material program seed does not match its slot.");
            last=checked(r.Offset+r.Expected.Length);
        }
        var c=new Cursor(seed,true);
        Require(c.String()=="UnityFS" && c.I32()==8,"Unsupported shader UnityFS format.");
        c.String();c.String(); int sizeField=c.Position;
        Require(c.I64()==seed.Length,"Invalid UnityFS file length.");
        int infoSize=c.Count(), rawInfoSize=c.Count(), flags=c.I32();
        Require(flags==64 && infoSize==rawInfoSize,"Shader seed requires uncompressed leading UnityFS directory.");
        c.Align(16); int infoStart=c.Position; var info=c.Bytes(infoSize); int dataStart=c.Position;
        var d=new Cursor(info,true);d.Bytes(16);int blocks=d.Count(); long total=0;
        for(int i=0;i<blocks;i++) { int n=d.Count(); Require(d.Count()==n && d.U16()==64,"Shader seed block must be uncompressed streaming data."); total+=n; }
        Require(total==seed.Length-dataStart,"Invalid UnityFS block coverage.");
        int files=d.Count(); var entries=new List<(long Offset,int Size,int Flags,string Name)>();
        for(int i=0;i<files;i++) { long off=d.I64(), n=d.I64(); int f=d.I32();string name=d.String(); Require(off>=0&&n>=0&&off<=total-n&&n<=int.MaxValue,"Invalid UnityFS directory range."); entries.Add((off,(int)n,f,name)); }
        Require(d.Position==info.Length,"Unexpected UnityFS directory bytes.");
        using var payload=new MemoryStream(); using var pw=new BinaryWriter(payload);
        var lengths=new List<int>();
        foreach(var entry in entries)
        {
            int absolute=checked(dataStart+(int)entry.Offset);var bytes=Slice(seed,absolute,entry.Size);
            var slots=sorted.Where(r=>r.Offset>=absolute && r.Offset<absolute+entry.Size).ToArray();
            if(slots.Length!=0)bytes=RewriteFile(bytes,absolute,slots);
            lengths.Add(bytes.Length);pw.Write(bytes);
        }
        Require(sorted.All(r=>r.Used),"A declared shader slot is not a supported program record.");
        using var table=new MemoryStream();using var tw=new BinaryWriter(table);
        tw.Write(new byte[16]);Write(tw,1,big:true);Write(tw,payload.Length,big:true);Write(tw,payload.Length,big:true);Write(tw,64,2,true);Write(tw,files,big:true);
        long offset=0;
        for(int i=0;i<files;i++) { Write(tw,offset,8,true);Write(tw,lengths[i],8,true);Write(tw,entries[i].Flags,big:true);tw.Write(Encoding.UTF8.GetBytes(entries[i].Name));tw.Write((byte)0);offset+=lengths[i]; }
        using var result=new MemoryStream();using var rw=new BinaryWriter(result);
        rw.Write(Slice(seed,0,infoStart));rw.Write(table.ToArray());rw.Write(payload.ToArray());var output=result.ToArray();
        Put(output,sizeField,output.Length,8,true);Put(output,sizeField+8,table.Length,4,true);Put(output,sizeField+12,table.Length,4,true);
        return output;
    }

    private sealed class Node
    {
        internal int Level,Size,Flags,Meta;
        internal uint Name;
        internal readonly List<Node> Children=new();
    }
    private sealed class Type
    {
        internal int Class;
        internal Node Root=null!;
        internal byte[] Strings=Array.Empty<byte>();
        internal string Name(Node n) => (n.Name&0x80000000)!=0 ? "" : new Cursor(Strings){Position=checked((int)n.Name)}.String();
    }
    private static byte[] RewriteFile(byte[] original,int absolute,Replacement[] slots)
    {
        var c=new Cursor(original,true);c.Bytes(8);Require(c.I32()==22,"Unsupported shader serialized file version.");c.Bytes(4);
        Require(c.Byte()==0,"Shader serialized file must be little endian.");c.Bytes(3);int meta=c.Count();Require(c.I64()==original.Length,"Invalid serialized file length.");long data=c.I64();c.Bytes(8);
        Require(data>=c.Position && data<=original.Length && meta<=data,"Invalid serialized metadata bounds.");
        c.Big=false;c.String();c.I32();Require(c.Byte()==1,"Shader seed requires its native type tree.");
        int typesCount=c.Count();var types=new List<Type>();
        for(int t=0;t<typesCount;t++)
        {
            var type=new Type{Class=c.I32()};c.Byte();c.U16();if(type.Class==114)c.Bytes(16);c.Bytes(16);
            int count=c.Count(), strings=c.Count();var stack=new List<Node>();
            for(int i=0;i<count;i++)
            {
                c.U16();var n=new Node{Level=c.Byte(),Flags=c.Byte()};c.I32();n.Name=unchecked((uint)c.I32());n.Size=c.I32();c.I32();n.Meta=c.I32();c.I64();
                Require(n.Level<=stack.Count,"Invalid type-tree depth.");
                if(n.Level==0){Require(i==0,"Multiple type-tree roots.");type.Root=n;}
                else stack[n.Level-1].Children.Add(n);
                if(stack.Count>n.Level)stack.RemoveRange(n.Level,stack.Count-n.Level);stack.Add(n);
            }
            type.Strings=c.Bytes(strings);int dependencies=c.Count();c.Bytes(checked(dependencies*4));types.Add(type);
        }
        int objectCount=c.Count();var objects=new List<(int Field,int Start,int Size,int Type)>();
        for(int i=0;i<objectCount;i++)
        {
            c.Align();c.I64();int field=c.Position;long start=c.I64()+data;int size=c.Count(),type=c.I32();
            Require(start>=data&&start<=original.Length-size&&type>=0&&type<types.Count,"Invalid serialized object range.");
            objects.Add((field,(int)start,size,type));
        }
        Require(c.Position<=data,"Object table overlaps serialized data.");var header=Slice(original,0,(int)data);
        using var output=new MemoryStream();using var w=new BinaryWriter(output);w.Write(header);
        foreach(var obj in objects.OrderBy(o=>o.Start))
        {
            Align(w,8);int start=(int)output.Position;var bytes=Slice(original,obj.Start,obj.Size);
            var inside=slots.Where(r=>r.Offset>=absolute+obj.Start && r.Offset<absolute+obj.Start+obj.Size).ToArray();
            if(inside.Length!=0) { Require(types[obj.Type].Class==48,"Shader slot is outside Shader object."); bytes=RewriteShader(bytes,absolute+obj.Start,types[obj.Type],inside); }
            Put(header,obj.Field,start-data,8);Put(header,obj.Field+8,bytes.Length);w.Write(bytes);Align(w,8);
        }
        var result=output.ToArray();Put(header,24,result.Length,8,true);Buffer.BlockCopy(header,0,result,0,header.Length);return result;
    }
    private static void Skip(Cursor c,Node n)
    {
        if((n.Flags&1)!=0)
        {
            Require(n.Children.Count==2,"Invalid type-tree array.");int count=c.Count();var element=n.Children[1];
            if(element.Children.Count==0 && element.Size>0 && (element.Meta&0x4000)==0)c.Bytes(checked(count*element.Size));
            else for(int i=0;i<count;i++)Skip(c,element);
        }
        else if(n.Children.Count!=0)foreach(var child in n.Children)Skip(c,child);
        else { Require(n.Size>=0,"Unsupported variable type-tree leaf.");c.Bytes(n.Size); }
        if((n.Meta&0x4000)!=0)c.Align();
    }
    private static byte[] RewriteShader(byte[] original,int absolute,Type type,Replacement[] slots)
    {
        var fields=new Dictionary<string,(int Start,int End)>();var c=new Cursor(original);
        foreach(var n in type.Root.Children) { int start=c.Position;Skip(c,n);var name=type.Name(n);if(name.Length!=0)fields.Add(name,(start,c.Position)); }
        Require(c.Position==original.Length,"Shader type tree does not cover its object.");
        int Value(string name)
        { var range=fields[name];var r=new Cursor(original){Position=range.Start};Require(r.Count()==1&&r.Count()==1,"Unsupported shader platform/segment layout.");int v=r.Count();Require(r.Position==range.End,"Invalid shader segment table.");return v; }
        Require(Value("offsets")==0,"Invalid shader segment offset.");int compressed=Value("compressedLengths"),decompressed=Value("decompressedLengths");
        var field=fields["compressedBlob"];c.Position=field.Start;Require(c.Count()==compressed,"Shader blob length mismatch.");int blobStart=c.Position;
        Require(c.Byte()==0xf0,"Shader seed segment must be literal LZ4.");int length=15,b;
        do{b=c.Byte();length=checked(length+b);}while(b==255);
        int literalStart=c.Position;Require(length==decompressed && literalStart+length==blobStart+compressed,"Invalid literal LZ4 segment.");
        var raw=c.Bytes(length);var rebuilt=RewritePrograms(raw,checked(absolute+literalStart),slots);
        using var segment=new MemoryStream();using var sw=new BinaryWriter(segment);sw.Write((byte)0xf0);int remainder=rebuilt.Length-15;Require(remainder>=0,"Invalid shader program table.");
        while(remainder>=255){sw.Write((byte)255);remainder-=255;}sw.Write((byte)remainder);sw.Write(rebuilt);var blob=segment.ToArray();
        var changes=new Dictionary<int,(int End,byte[] Bytes)>();
        foreach(var name in new[]{"compressedLengths","decompressedLengths"}) { var r=fields[name];var bytes=Slice(original,r.Start,r.End-r.Start);Put(bytes,8,name=="compressedLengths"?blob.Length:rebuilt.Length);changes.Add(r.Start,(r.End,bytes)); }
        using var value=new MemoryStream();using var vw=new BinaryWriter(value);vw.Write(blob.Length);vw.Write(blob);Align(vw);changes.Add(field.Start,(field.End,value.ToArray()));
        using var output=new MemoryStream();using var w=new BinaryWriter(output);int pos=0;
        foreach(var change in changes.OrderBy(x=>x.Key)){w.Write(Slice(original,pos,change.Key-pos));w.Write(change.Value.Bytes);pos=change.Value.End;}w.Write(Slice(original,pos,original.Length-pos));return output.ToArray();
    }
    private static byte[] RewritePrograms(byte[] raw,int absolute,Replacement[] slots)
    {
        var c=new Cursor(raw);int count=c.Count();Require(count<=raw.Length/12,"Invalid shader program table count.");
        var entries=new List<byte[]>();int prior=checked(4+count*12);
        for(int i=0;i<count;i++)
        {
            int start=c.Count(),size=c.Count(),segment=c.I32();Require(segment==0&&start==prior&&start<=raw.Length-size,"Invalid shader program record.");prior=start+size;
            var bytes=Slice(raw,start,size);var inside=slots.Where(r=>r.Offset>=absolute+start&&r.Offset<absolute+start+size).ToArray();
            if(inside.Length!=0)
            {
                Require(inside.Length==1,"Multiple compiler payloads in one shader program record.");var r=inside[0];var p=new Cursor(bytes);
                Require(p.I32()==202012090,"Unsupported shader program record version.");p.Bytes(20);int keywords=p.Count();
                for(int k=0;k<keywords;k++){p.Bytes(p.Count());p.Align();}
                int sizeField=p.Position,programSize=p.Count(),programStart=p.Position;Require(programSize<=bytes.Length-programStart,"Invalid native program array.");
                int codeStart=r.Offset-absolute-start,codeEnd=checked(codeStart+r.Expected.Length),end=checked(programStart+programSize);
                Require(codeStart>=programStart&&codeEnd<=end,"Shader bytecode lies outside its native program array.");
                Require(r.Expected.Length>=32&&Encoding.ASCII.GetString(r.Expected,0,4)=="DXBC"&&BitConverter.ToInt32(r.Expected,24)==r.Expected.Length,"Invalid seed DXBC length.");
                using var output=new MemoryStream();using var w=new BinaryWriter(output);w.Write(Slice(bytes,0,sizeField));w.Write(checked(programSize+r.Compiled.Length-r.Expected.Length));
                w.Write(Slice(bytes,programStart,codeStart-programStart));w.Write(r.Compiled);w.Write(Slice(bytes,codeEnd,end-codeEnd));Align(w);int aligned=(end+3)&~3;w.Write(Slice(bytes,aligned,bytes.Length-aligned));bytes=output.ToArray();r.Used=true;
            }
            entries.Add(bytes);
        }
        Require(prior==raw.Length,"Unexpected trailing shader program bytes.");
        using var result=new MemoryStream();using var rw=new BinaryWriter(result);rw.Write(count);int offset=4+count*12;
        foreach(var entry in entries){rw.Write(offset);rw.Write(entry.Length);rw.Write(0);offset=checked(offset+entry.Length);}foreach(var entry in entries)rw.Write(entry);return result.ToArray();
    }
}
