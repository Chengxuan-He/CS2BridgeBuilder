using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Offline only. One source semantic unit, independent of all receiving triangles,
// roads, LODs and sampler choices. Each output level samples its authored source mip.
public static class PrepareRepeatUnits
{
    public const string Recipe="BridgeBuilder/RepeatUnits/2";
    [Serializable] public sealed class Channel { public string name,cid,sha256,path,encoding; }
    [Serializable] public sealed class Evidence { public string path,sha256; }
    [Serializable] public sealed class Band { public double start,end,repeatY=1;public double[] min,max; }
    [Serializable] public sealed class Unit
    {
        public string id,sourceCid,sourceDigest;
        public double[] min,max,physicalSize;
        public Evidence evidence;
        public Evidence descriptor;
        public Channel[] channels;
        public string repeatAxes;
        internal bool RepeatU=>string.IsNullOrEmpty(repeatAxes)||repeatAxes.Contains("U");
        internal bool RepeatV=>string.IsNullOrEmpty(repeatAxes)||repeatAxes.Contains("V");
        // An authored composite semantic unit, never receiver-derived strips.
        public Band[] bands;
        // Aliases are use metadata only and are never included in a pixel key.
        public string[] aliases;
    }
    [Serializable] public sealed class Manifest { public string recipe,style;public Unit[] units; }
    [Serializable] public sealed class Product
    {
        public string name,cid,key;public int width,height,mips;public long bytes;
        public double sourceSpanX,sourceSpanY,densityRatioX,densityRatioY;
    }
    [Serializable] public sealed class Result { public string id,key,surfaceCid;public int[] periods;public string[] aliases;public Product[] channels; }
    [Serializable] public sealed class Report { public string recipe,style;public int producedTextures,reusedTextures;public Result[] units; }
    static readonly string[] Names={"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"};
    static string Hash(Action<BinaryWriter> write)
    {using var s=new MemoryStream();using(var w=new BinaryWriter(s,Encoding.UTF8,true))write(w);return PreparePaint.Digest(s.ToArray());}
    static string Cid(byte[] bytes)
    {string digest=PreparePaint.Digest(bytes);return PreparePaint.HashText(Enumerable.Range(0,16).Select(i=>Convert.ToByte(digest.Substring(i*2,2),16)).ToArray(),0);}
    static void Write(string path,byte[] bytes,string cid)
    {
        if(File.Exists(path))
        {if(!File.ReadAllBytes(path).SequenceEqual(bytes)||File.ReadAllText(path+".cid").Trim()!=cid)throw new IOException("Conflicting unit output: "+path);return;}
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,bytes);File.WriteAllText(path+".cid",cid,new UTF8Encoding(false));
    }
    static string Resolve(string root,string path)=>Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(root,path));
    static byte[] Verified(string root,string path,string digest)
    {
        var bytes=File.ReadAllBytes(Resolve(root,path));
        if(PreparePaint.Digest(bytes)!=digest)throw new InvalidDataException("Source/evidence hash differs: "+path);
        return bytes;
    }
    static void Validate(Unit u,string root)
    {
        if(u.min?.Length!=2||u.max?.Length!=2||u.physicalSize?.Length!=2||u.channels==null||u.evidence==null||u.descriptor==null)
            throw new InvalidDataException("Incomplete genuine unit measurement: "+u.id);
        for(int i=0;i<2;i++)
            if(double.IsNaN(u.min[i])||double.IsInfinity(u.min[i])||double.IsNaN(u.max[i])||double.IsInfinity(u.max[i])||
               u.min[i]<0||u.max[i]>1||u.max[i]<=u.min[i]||double.IsNaN(u.physicalSize[i])||double.IsInfinity(u.physicalSize[i])||u.physicalSize[i]<=0)
                throw new InvalidDataException("Invalid genuine unit extent: "+u.id);
        Verified(root,u.evidence.path,u.evidence.sha256);
        if(u.channels.Select(c=>c.name).Distinct().Count()!=u.channels.Length||u.channels.Any(c=>!Names.Contains(c.name)))
            throw new InvalidDataException("Unsupported or duplicate channel.");
        foreach(string name in Names.Take(3))if(!u.channels.Any(c=>c.name==name))throw new InvalidDataException("Missing unit channel: "+name);
        if(u.channels.Any(c=>!string.IsNullOrEmpty(c.encoding)&&c.encoding!="BC7"))
            throw new InvalidDataException("Unsupported native unit encoding.");
        if(!string.IsNullOrEmpty(u.repeatAxes)&&u.repeatAxes!="UV"&&u.repeatAxes!="U"&&u.repeatAxes!="V")
            throw new InvalidDataException("Invalid measured repeat axes.");
        if(u.bands?.Length>0&&(!u.RepeatU||!u.RepeatV))throw new InvalidDataException("Composite bands require their verified two-axis mapping.");
        if(string.IsNullOrEmpty(u.sourceCid)||string.IsNullOrEmpty(u.sourceDigest))throw new InvalidDataException("Missing source provenance.");
        if(u.bands!=null&&u.bands.Length!=0)
        {
            double previous=0;
            foreach(var band in u.bands)
            {
                if(band.start!=previous||!double.IsFinite(band.end)||band.end<=band.start||band.end>1||
                   !double.IsFinite(band.repeatY)||band.repeatY<1||band.repeatY!=Math.Floor(band.repeatY)||
                   band.min?.Length!=2||band.max?.Length!=2)
                    throw new InvalidDataException("Invalid authored composite unit bands.");
                for(int i=0;i<2;i++)if(!double.IsFinite(band.min[i])||!double.IsFinite(band.max[i])||
                    band.min[i]<0||band.max[i]>1||band.min[i]>=band.max[i])throw new InvalidDataException("Invalid composite source region.");
                previous=band.end;
            }
            if(previous!=1)throw new InvalidDataException("Composite bands must cover one complete semantic unit.");
        }
    }
    static (double X,double Y) Span(Unit u,int width,int height)=>u.bands==null||u.bands.Length==0
        ?((u.max[0]-u.min[0])*width,(u.max[1]-u.min[1])*height)
        :(u.bands.Max(b=>(b.max[0]-b.min[0])*width/(b.end-b.start)),u.bands.Max(b=>(b.max[1]-b.min[1])*height*b.repeatY));
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string path=Arg("-unitManifest"),root=Path.GetDirectoryName(path),output=Arg("-unitOutput"),style=Arg("-style");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        if(manifest.recipe!=Recipe||manifest.style!=style||manifest.units==null||manifest.units.Length==0||Path.GetFileName(style)!=style)
            throw new InvalidDataException("Invalid unit manifest/style.");
        if(QualitySettings.activeColorSpace!=ColorSpace.Linear)throw new InvalidOperationException("Use Linear authoring color space.");
        var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
        var result=new Report{recipe=Recipe,style=style};var units=new List<Result>();
        var products=new Dictionary<string,(byte[] Bytes,Product Product)>();
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            foreach(var unit in manifest.units)
            {
                Validate(unit,root);var bindings=new Dictionary<string,string>();var channels=new List<Product>();
                int px=1,py=1;
                var inputs=unit.channels.ToDictionary(c=>c.name,c=>Verified(root,c.path,c.sha256));
                // One semantic unit, even when its measured edges fall between
                // source texels. Ceil chooses a sampling grid no coarser than the
                // source; it never changes the region or its physical period.
                // Requiring rational-grid phase equality caused huge repeated
                // images and was not a requirement of native Repeat sampling.
                foreach(var channel in unit.channels.OrderBy(c=>c.name,StringComparer.Ordinal))
                {
                    var sourceBytes=inputs[channel.name];
                    string key=Hash(w=>{w.Write(Recipe);w.Write(Arg("-unitPipeline"));w.Write(channel.sha256);
                        if(unit.bands==null||unit.bands.Length==0)
                        {w.Write(0);for(int i=0;i<2;i++){w.Write(unit.min[i]);w.Write(unit.max[i]);}}
                        else
                        {w.Write(unit.bands.Length);foreach(var b in unit.bands){w.Write(b.start);w.Write(b.end);w.Write(b.repeatY);for(int i=0;i<2;i++){w.Write(b.min[i]);w.Write(b.max[i]);}}}
                        w.Write(px);w.Write(py);w.Write(channel.encoding??"");w.Write(unit.RepeatU);w.Write(unit.RepeatV);});
                    if(!products.TryGetValue(key,out var entry))
                    {
                        string stored=Path.Combine(Arg("-unitStore"),key+".Texture");
                        if(File.Exists(stored))
                        {
                            var bytes=File.ReadAllBytes(stored);string cid=Cid(bytes);
                            if(File.ReadAllText(stored+".cid").Trim()!=cid)throw new InvalidDataException("Prepared unit product checksum differs.");
                            var p=new Product{name=channel.name,cid=cid,key=key,width=BitConverter.ToUInt16(bytes,2),height=BitConverter.ToUInt16(bytes,4),mips=bytes[8],bytes=bytes.Length};
                            entry=(bytes,p);products.Add(key,entry);result.reusedTextures++;
                        }
                        else
                        {
                        using var stream=new MemoryStream(sourceBytes);
                        var source=MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,NativeRawSize(sourceBytes)));
                        try
                        {
                            int complete=1+(int)Math.Floor(Math.Log(Math.Max(source.width,source.height),2));
                            if(source.mipmapCount!=complete)throw new InvalidDataException("Source must contain its complete authored mip chain: "+channel.path);
                            double x=unit.min[0]*source.width,y=unit.min[1]*source.height;
                            double sx=(unit.max[0]-unit.min[0])*source.width,sy=(unit.max[1]-unit.min[1])*source.height;
                            var span=Span(unit,source.width,source.height);
                            bool bc7=channel.encoding=="BC7";
                            int width=Pixel(span.X,bc7),height=Pixel(span.Y,bc7);
                            if(!unit.RepeatU)width=checked(width*2);
                            if(!unit.RepeatV)height=checked(height*2);
                            if(width>SystemInfo.maxTextureSize||height>SystemInfo.maxTextureSize)throw new InvalidDataException("Guarded unit exceeds native dimensions.");
                            var texture=Extract(program,source,x,y,sx,sy,width,height,px,py,unit.bands,unit.RepeatU,unit.RepeatV);
                            try
                            {
                                Texture2D encoded=null;
                                byte[] bytes;
                                try{if(bc7)encoded=NativeTextureEncoding.Bc7(texture);bytes=PreparePaint.Texture(encoded??texture);}
                                finally{if(encoded!=null)UnityEngine.Object.DestroyImmediate(encoded);}
                                string cid=Cid(bytes);
                                var p=new Product{name=channel.name,cid=cid,key=key,width=width,height=height,mips=texture.mipmapCount,bytes=bytes.Length};
                                entry=(bytes,p);products.Add(key,entry);Write(stored,bytes,cid);result.producedTextures++;
                            }
                            finally{UnityEngine.Object.DestroyImmediate(texture);}
                        }
                        finally{UnityEngine.Object.DestroyImmediate(source);}
                        }
                    }
                    else result.reusedTextures++;
                    Write(Path.Combine(output,style,"Shared",entry.Product.cid+".Texture"),entry.Bytes,entry.Product.cid);
                    bindings.Add(channel.name,entry.Product.cid);
                    var product=entry.Product;
                    var sourceSpan=Span(unit,BitConverter.ToUInt16(sourceBytes,2),BitConverter.ToUInt16(sourceBytes,4));
                    double spanX=sourceSpan.X,spanY=sourceSpan.Y;
                    channels.Add(new Product{name=channel.name,cid=product.cid,key=product.key,width=product.width,height=product.height,mips=product.mips,bytes=product.bytes,
                        sourceSpanX=spanX,sourceSpanY=spanY,densityRatioX=product.width/(unit.RepeatU?spanX:spanX*2),densityRatioY=product.height/(unit.RepeatV?spanY:spanY*2)});
                }
                if(!bindings.ContainsKey("_ControlMask"))
                {
                    const string cid="729893d593df5d52ba911bc1e7eab77b";
                    var bytes=Verified(Arg("-preparedMaterials"),Path.Combine(style,"Shared",cid+".Texture"),"d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4");
                    Write(Path.Combine(output,style,"Shared",cid+".Texture"),bytes,cid);bindings.Add("_ControlMask",cid);
                }
                var descriptor=Surface(Verified(root,unit.descriptor.path,unit.descriptor.sha256),bindings,px,py);string surfaceCid=Cid(descriptor);
                string unitKey=Hash(w=>{w.Write(Recipe);w.Write(surfaceCid);});
                Write(Path.Combine(output,style,"RepeatUnits",unitKey,"Repeat.Surface"),descriptor,surfaceCid);
                units.Add(new Result{id=unit.id,key=unitKey,surfaceCid=surfaceCid,periods=new[]{px,py},aliases=unit.aliases,channels=channels.ToArray()});
            }
            result.units=units.ToArray();File.WriteAllText(Arg("-unitReport"),JsonUtility.ToJson(result,true));
            Debug.Log($"PASS genuine repeat units: {units.Count}; produced textures={result.producedTextures}; shared uses={result.reusedTextures}; full source mips.");
        }
        finally{PreparePaint.CloseCodec(module);}
    }
    static int Pixel(double value,bool blockAligned=false)
    {
        int p=checked((int)Math.Ceiling(value));
        if(blockAligned)p=checked((p+3)/4*4);
        if(p<=0||p>SystemInfo.maxTextureSize)throw new InvalidDataException("One genuine unit exceeds native texture dimensions: "+value);
        return p;
    }
    static int NativeRawSize(byte[] bytes)
    {
        using var r=new BinaryReader(new MemoryStream(bytes));int version=r.ReadUInt16(),w=r.ReadUInt16(),h=r.ReadUInt16();
        if(version>=4)r.ReadUInt16();int mips=r.ReadByte();var format=version>=3?(GraphicsFormat)r.ReadByte():(GraphicsFormat)Enum.Parse(typeof(GraphicsFormat),r.ReadString());
        int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format),b=(int)GraphicsFormatUtility.GetBlockSize(format),size=0;
        for(int i=0;i<mips;i++){size=checked(size+((w+bw-1)/bw)*((h+bh-1)/bh)*b);w=Math.Max(1,w/2);h=Math.Max(1,h/2);}return size;
    }
    static Texture2D Extract(ComputeShader program,Texture2D source,double x,double y,double sx,double sy,int width,int height,int px,int py,Band[] bands,bool repeatU=true,bool repeatV=true)
    {
        string format=source.graphicsFormat.ToString();
        if(format.Contains("16")||format.Contains("32")||format.Contains("BC6H"))
            throw new InvalidDataException("This source requires a higher-precision unit output: "+format);
        bool srgb=GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
        var output=new Texture2D(width,height,TextureFormat.RGBA32,true,!srgb){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=source.anisoLevel};
        source.filterMode=FilterMode.Bilinear;source.wrapMode=TextureWrapMode.Clamp;
        try
        {
            int kernel=program.FindKernel("Extract");program.SetTexture(kernel,"Source",source);
            int count=bands?.Length??0;
            var bandData=new Vector4[Math.Max(2,count*2)];
            for(int i=0;i<count;i++)
            {
                var b=bands[i];bandData[i*2]=new Vector4((float)b.start,(float)b.end,(float)b.repeatY,0);
                bandData[i*2+1]=new Vector4((float)b.min[0],(float)b.min[1],(float)(b.max[0]-b.min[0]),(float)(b.max[1]-b.min[1]));
            }
            using var bandBuffer=new ComputeBuffer(bandData.Length,16);bandBuffer.SetData(bandData);
            program.SetBuffer(kernel,"Bands",bandBuffer);program.SetInt("BandCount",count);
            program.SetVector("Region",new Vector4((float)x/source.width,(float)y/source.height,(float)sx/source.width,(float)sy/source.height));
            program.SetVector("Periods",new Vector4(px,py,0,0));
            program.SetInts("RepeatAxes",repeatU?1:0,repeatV?1:0);
            program.SetInt("EncodeSrgb",srgb?1:0);
            for(int mip=0;mip<output.mipmapCount;mip++)
            {
                int w=Math.Max(1,width>>mip),h=Math.Max(1,height>>mip);
                var target=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};
                try
                {
                    target.Create();program.SetTexture(kernel,"Result",target);program.SetInts("Extent",w,h);program.SetInt("SourceMip",Math.Min(mip,source.mipmapCount-1));
                    program.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                    var read=AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32);read.WaitForCompletion();
                    if(read.hasError)throw new IOException("Repeat unit mip GPU readback failed.");
                    output.SetPixelData(read.GetData<byte>(),mip);
                }
                finally{target.Release();UnityEngine.Object.DestroyImmediate(target);}
            }
            output.Apply(false,false);return output;
        }
        catch{UnityEngine.Object.DestroyImmediate(output);throw;}
    }
    static byte[] Surface(byte[] original,Dictionary<string,string> bindings,int px,int py)
    {
        using var input=new MemoryStream(original);using var r=new BinaryReader(input);
        if(r.ReadUInt16()!=1)throw new InvalidDataException("Unsupported source descriptor version.");
        int template=r.ReadInt32();bool vt=r.ReadBoolean();
        if(template!=0&&template!=1&&template!=14)throw new InvalidDataException("Provide a native Default/Cutout object descriptor.");
        bool Present(){byte marker=r.ReadByte();if(marker!=0&&marker!=255)throw new InvalidDataException("Invalid descriptor marker.");return marker!=0;}
        int Count(){int count=Present()?r.ReadInt32():0;if(count<0)throw new InvalidDataException("Invalid descriptor count.");return count;}
        if(vt){if(r.ReadBytes(276).Length!=276)throw new EndOfStreamException();if(Present()&&r.ReadBytes(16).Length!=16)throw new EndOfStreamException();}
        int maps=checked((int)input.Position);
        foreach(int size in new[]{4,4,16})
        {
            int count=Count();
            for(int i=0;i<count;i++){r.ReadString();if(r.ReadBytes(size).Length!=size)throw new EndOfStreamException();}
        }
        int colorsStart=checked((int)input.Position);
        var colors=new Dictionary<string,Vector4>();
        for(int count=Count();count>0;count--)colors.Add(r.ReadString(),new Vector4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));
        if(px!=1||py!=1)
        {
            if(!colors.TryGetValue("_UV0Scale",out var scale))scale=new Vector4(1,1,0,0);
            colors.TryGetValue("_UV0Offset",out var offset);
            scale.x/=px;scale.y/=py;offset.x/=px;offset.y/=py;
            colors["_UV0Scale"]=scale;colors["_UV0Offset"]=offset;
        }
        int prefix=checked((int)input.Position);
        int textures=Count();var merged=new Dictionary<string,string>(bindings);
        for(int i=0;i<textures;i++)
        {
            string name=r.ReadString();if(!Present())continue;
            var cid=r.ReadBytes(16);if(cid.Length!=16)throw new EndOfStreamException();
            if(name=="_ControlMask"&&merged.TryGetValue(name,out var target)&&target=="729893d593df5d52ba911bc1e7eab77b"&&PreparePaint.HashText(cid,0)!=target)
                throw new InvalidDataException("An authored control map must be provided as a unit channel; transparent control cannot replace it.");
            if(!merged.ContainsKey(name))
            {
                if(vt&&Names.Contains(name))throw new InvalidDataException("Provide the authored VT channel or verified constant channel: "+name);
                merged.Add(name,PreparePaint.HashText(cid,0));
            }
        }
        int suffix=checked((int)input.Position);
        using var s=new MemoryStream();using var w=new BinaryWriter(s);
        w.Write((ushort)1);w.Write(template);w.Write(false);w.Write(original,maps,colorsStart-maps);
        w.Write((byte)255);w.Write(colors.Count);foreach(var p in colors){w.Write(p.Key);w.Write(p.Value.x);w.Write(p.Value.y);w.Write(p.Value.z);w.Write(p.Value.w);}
        w.Write((byte)255);w.Write(merged.Count);
        foreach(var p in merged.OrderBy(p=>p.Key,StringComparer.Ordinal)){w.Write(p.Key);w.Write((byte)255);w.Write(PreparePaint.HashBytes(p.Value));}
        w.Write(original,suffix,original.Length-suffix);return s.ToArray();
    }
}
