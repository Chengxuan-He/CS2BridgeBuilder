using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Offline authoring. Uses the same source decoder, pixel-footprint sizing, crop,
// channel mips and BC7 encoder as the runtime path being replaced.
public static class PrepareStructure
{
    [Serializable] public sealed class Channel { public int stack, layer; public string name; }
    [Serializable] public sealed class Source { public string cid; public int template; public Channel[] channels; }
    [Serializable] public sealed class Unit
    {
        public string style, sourceCid, sourceDigest, key;
        public bool triangle;
        public float[] bounds, a, b, c;
        internal MovableStructurePacking.Unit Sampler() => new MovableStructurePacking.Unit {
            Triangle=triangle, Bounds=new Vector4(bounds[0],bounds[1],bounds[2],bounds[3]),
            A=triangle?new Vector2(a[0],a[1]):default, B=triangle?new Vector2(b[0],b[1]):default,
            C=triangle?new Vector2(c[0],c[1]):default, Min=Vector2.zero, Max=Vector2.one };
    }
    [Serializable] public sealed class Inventory { public string recipe; public Unit[] units; public Source[] sources; }
    [Serializable] public sealed class Result { public string style, key, sourceCid, surfaceCid; public int width, height, channels; public long bytes; }
    [Serializable] public sealed class Report { public string recipe, inventoryDigest, pipeline; public int createdProducts,reusedProducts; public Result[] results; }

    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Argument(string name) => args[Array.IndexOf(args,name)+1];
        string game=Argument("-gameData"), inventoryPath=Argument("-structureInventory"), output=Argument("-preparedOutput");
        var inventory=JsonUtility.FromJson<Inventory>(File.ReadAllText(inventoryPath));
        if(inventory.recipe!=MovableStructureKey.Recipe)throw new InvalidDataException("Structural recipe version differs.");
        if(QualitySettings.activeColorSpace!=ColorSpace.Linear)throw new InvalidOperationException("Native material preparation requires Linear color space.");
        var selected=inventory.units.AsEnumerable();
        if(args.Contains("-style")) selected=selected.Where(u=>u.style==Argument("-style"));
        var units=selected.ToArray();
        if(units.Length==0)throw new InvalidDataException("No measured source units selected.");
        var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/StructureBake.compute");
        if(program==null)throw new FileNotFoundException("Structural preparation shader is missing.");
        int kernel=program.FindKernel("CropNativeAtlas");
        var archives=new List<ZipArchive>();
        var entries=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        var module=PreparePaint.OpenCodec(game);
        var results=new List<Result>();
        string pipeline=Argument("-pipelineKey")+"/"+Application.unityVersion+"/"+SystemInfo.graphicsDeviceVersion;
        var store=new PreparedBakeStore(Argument("-preparedStore"),inventory.recipe);
        var readOnly=args.Contains("-readOnlyStores")?Argument("-readOnlyStores").Split('|'):Array.Empty<string>();
        bool reuseOnly=args.Contains("-reuseOnly");
        PreparedBakeStore.Payload Product(string key,string extension,Func<byte[]> produce) =>
            store.GetEquivalent(key,key,extension,readOnly,()=> {
                if(reuseOnly)throw new InvalidOperationException("Prepared structural product is unavailable in reuse-only mode: "+key+extension);
                return produce();
            });
        try
        {
            foreach(string family in new[]{"Game","BridgesAndPorts"})
            foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok")
                .Where(p=>Path.GetFileName(p).StartsWith("Blob") || Path.GetFileName(p).StartsWith("VT")))
            {
                var archive=ZipFile.OpenRead(path);archives.Add(archive);
                foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".cid",StringComparison.Ordinal)))
                {
                    using var text=new StreamReader(entry.Open());string cid=text.ReadToEnd().Trim();
                    var payload=archive.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));
                    if(payload==null)continue;
                    if(entries.TryGetValue(cid,out var old) && !Bytes(old).SequenceEqual(Bytes(payload)))
                        throw new InvalidDataException("Conflicting native CID: "+cid);
                    entries[cid]=payload;
                }
            }
            byte[] Read(string cid) => entries.TryGetValue(cid,out var e)?Bytes(e):throw new FileNotFoundException("Native source missing: "+cid);
            foreach(var group in units.GroupBy(u=>u.sourceCid))
            {
                var bytes=Read(group.Key);string digest=PreparePaint.Digest(bytes);
                if(group.Any(u=>u.sourceDigest!=digest))throw new InvalidDataException("Structural source version changed: "+group.Key);
                var description=inventory.sources.Single(s=>s.cid==group.Key);
                using var source=new MaterialSource(bytes,description,Read);
                foreach(var u in group)
                {
                    var sampler=u.Sampler();
                    if(u.key!=MovableStructureKey.For(digest,sampler.Triangle,sampler.Bounds,sampler.A,sampler.B,sampler.C))
                        throw new InvalidDataException("Structural sampler key differs: "+u.key);
                    var page=MovableStructurePacking.Pack(new[]{sampler},source.Textures.Values.Select(t=>new Vector2Int(t.width,t.height)).ToArray(),SystemInfo.maxTextureSize).Single();
                    var cell=page.Cells.Single();
                    var files=new Dictionary<string,(string Cid,byte[] Data)>();
                    var bindings=new Dictionary<string,string>();
                    foreach(var input in source.Textures.OrderBy(p=>p.Key,StringComparer.Ordinal))
                    {
                        bool srgb=input.Key=="_BaseColorMap" || input.Key=="_EmissiveColorMap";
                        int mode=srgb?0:input.Key=="_NormalMap"?1:2;
                        string textureKey=PreparedBakeStore.Key(w=>{
                            w.Write(inventory.recipe);w.Write(pipeline);w.Write(source.TextureKeys[input.Key]);
                            w.Write(MovableStructureKey.For("",sampler.Triangle,sampler.Bounds,sampler.A,sampler.B,sampler.C));
                            w.Write(page.Width);w.Write(page.Height);w.Write(mode);w.Write(true);
                        });
                        var product=Product(textureKey,".Texture",()=>{
                        var target=new RenderTexture(page.Width,page.Height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};
                        try
                        {
                            target.Create();
                            program.SetTexture(kernel,"MipSource",input.Value);program.SetTexture(kernel,"AtlasColor",target);
                            program.SetInt("NativeCropSrgb",srgb?1:0);program.SetInt("NativeCropNormal",input.Key=="_NormalMap"?1:0);
                            program.SetInt("NativeCropTriangle",sampler.Triangle?1:0);
                            var b=sampler.Bounds;program.SetVector("NativeCrop",new Vector4(b.x,b.y,b.z-b.x,b.w-b.y));
                            program.SetVector("NativeCropA",sampler.A);program.SetVector("NativeCropB",sampler.B);program.SetVector("NativeCropC",sampler.C);
                            program.SetVector("NativeCropDestination",new Vector4(0,0,cell.W,cell.H));
                            program.SetVector("NativeCropOrigin",cell.SampleOrigin);program.SetVector("NativeCropStepX",cell.SampleX);program.SetVector("NativeCropStepY",cell.SampleY);
                            program.Dispatch(kernel,(cell.W+7)/8,(cell.H+7)/8,1);
                            using var channel=new MovableAtlasChannel(program,target,srgb?0:input.Key=="_NormalMap"?1:2,true);
                            channel.Read();return PreparePaint.Texture(channel.Texture);
                        }
                        finally { target.Release();UnityEngine.Object.DestroyImmediate(target); }
                        });
                        files.Add(input.Key+".Texture",(product.Cid,product.Bytes));
                        bindings.Add(input.Key,product.Cid);
                    }
                    string surfaceKey=PreparedBakeStore.Key(w=>{
                        w.Write(inventory.recipe);w.Write(pipeline);w.Write(digest);
                        foreach(var binding in bindings.OrderBy(p=>p.Key,StringComparer.Ordinal)){w.Write(binding.Key);w.Write(binding.Value);}
                    });
                    var surfaceProduct=Product(surfaceKey,".Surface",()=>source.Surface(bindings));
                    string surfaceCid=surfaceProduct.Cid;
                    files.Add("Structure.Surface",(surfaceCid,surfaceProduct.Bytes));
                    // Complete a unit in memory before creating its directory.
                    string folder=Path.Combine(output,u.style,"Structure",u.key);
                    Directory.CreateDirectory(folder);
                    foreach(var file in files)
                    {
                        var path=Path.Combine(folder,file.Key);
                        File.WriteAllBytes(path,file.Value.Data);File.WriteAllText(path+".cid",file.Value.Cid,new UTF8Encoding(false));
                        if(!File.ReadAllBytes(path).SequenceEqual(file.Value.Data))throw new IOException("Prepared write differs: "+path);
                    }
                    results.Add(new Result{style=u.style,key=u.key,sourceCid=u.sourceCid,surfaceCid=surfaceCid,width=page.Width,height=page.Height,channels=bindings.Count,bytes=files.Values.Sum(f=>(long)f.Data.Length+32)});
                }
                Debug.Log($"Prepared structural source {group.Key}: {group.Count()} units; completed {results.Count}/{units.Length}.");
            }
            File.WriteAllText(Argument("-preparationReport"),JsonUtility.ToJson(new Report{recipe=inventory.recipe,inventoryDigest=PreparePaint.Digest(File.ReadAllBytes(inventoryPath)),pipeline=pipeline,createdProducts=store.Created,reusedProducts=store.Reused,results=results.ToArray()},true));
            Debug.Log($"PASS prepared structures: {results.Count} units; created={store.Created}; reused={store.Reused}; {results.Sum(r=>r.bytes)} serialized bytes; source-sized Repeat; all native channels and mip levels.");
        }
        finally { foreach(var archive in archives)archive.Dispose();PreparePaint.CloseCodec(module); }
    }

    private static byte[] Bytes(ZipArchiveEntry entry)
    { using var stream=entry.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray(); }

    private sealed class MaterialSource : IDisposable
    {
        internal readonly Dictionary<string,Texture2D> Textures=new Dictionary<string,Texture2D>();
        internal readonly Dictionary<string,string> TextureKeys=new Dictionary<string,string>();
        private readonly HashSet<Texture2D> images=new HashSet<Texture2D>();
        private readonly byte[] maps,keywords;
        private readonly int template;
        internal MaterialSource(byte[] bytes,Source description,Func<string,byte[]> read)
        {
            try
            {
                using var r=new BinaryReader(new MemoryStream(bytes));
                if(r.ReadUInt16()!=1)throw new InvalidDataException("Unsupported native structural descriptor.");
                template=r.ReadInt32();if(template!=description.template)throw new InvalidDataException("Structural template differs.");
                bool vt=r.ReadBoolean();byte[] header=null;string surfaceCid=null;
                if(vt){header=r.ReadBytes(276);if(Present(r))surfaceCid=PreparePaint.HashText(r.ReadBytes(16),0);}
                int mapStart=(int)r.BaseStream.Position;
                foreach(int size in new[]{4,4,16,16})for(int n=Count(r);n>0;n--){r.ReadString();r.BaseStream.Position+=size;}
                maps=bytes.Skip(mapStart).Take((int)r.BaseStream.Position-mapStart).ToArray();
                var nativeBindings=new Dictionary<string,string>();
                for(int n=Count(r);n>0;n--){string name=r.ReadString();if(Present(r))nativeBindings.Add(name,PreparePaint.HashText(r.ReadBytes(16),0));}
                int keywordStart=(int)r.BaseStream.Position;
                for(int n=Count(r);n>0;n--)r.ReadString();
                if(r.BaseStream.Position!=bytes.Length)throw new InvalidDataException("Unconsumed structural descriptor bytes.");
                keywords=bytes.Skip(keywordStart).ToArray();
                if(vt)
                {
                    var handled=new HashSet<(int,int)>();
                    if(surfaceCid!=null)
                    {
                        using var input=new MemoryStream(read(surfaceCid));
                        var layers=MovableVtTextureSource.ReadSurface(input,PreparePaint.Decompress);
                        foreach(var image in layers.Values)images.Add(image);
                        foreach(var layer in layers)
                        {
                            var channel=description.channels.Single(c=>c.stack==layer.Key.Stack && c.layer==layer.Key.Layer);
                            Textures.Add(channel.name,layer.Value);handled.Add(layer.Key);
                        }
                    }
                    foreach(var channel in description.channels)
                    {
                        if(handled.Contains((channel.stack,channel.layer)))continue;
                        string cid=PreparePaint.HashText(header,76+channel.stack*136+channel.layer*16);
                        if(cid==new string('0',32))continue;
                        using var input=new MemoryStream(read(cid));
                        var texture=MovableVtTextureSource.ReadTexture(input,PreparePaint.Decompress);
                        images.Add(texture);Textures.Add(channel.name,texture);
                    }
                }
                foreach(var binding in nativeBindings)
                {
                    if(vt && description.channels.Any(c=>c.name==binding.Key))continue;
                    var payload=read(binding.Value);
                    int size=RawSize(payload);
                    using var input=new MemoryStream(payload);
                    var texture=MovableTextureSource.Read(input,b=>PreparePaint.DecodeRaw(b,size));
                    images.Add(texture);Textures.Add(binding.Key,texture);
                }
                if(Textures.Count==0)throw new InvalidDataException("No structural source texture channels.");
                // Identify the complete decoded input before any crop/encode. This
                // shares channels across different material descriptors as well as
                // style copies, while preserving sampling and format differences.
                foreach(var input in Textures)
                {
                    var texture=input.Value;
                    string raw=PreparePaint.Digest(texture.GetRawTextureData<byte>().ToArray());
                    TextureKeys.Add(input.Key,PreparedBakeStore.Key(w=>{
                        w.Write(raw);w.Write(texture.width);w.Write(texture.height);w.Write(texture.mipmapCount);
                        w.Write((int)texture.graphicsFormat);w.Write((int)texture.filterMode);
                        w.Write((int)texture.wrapModeU);w.Write((int)texture.wrapModeV);w.Write(texture.anisoLevel);
                    }));
                }
                // Do not invent template default textures for missing stack layers.
                // These remain native template defaults in the output descriptor.
            }
            catch { Dispose();throw; }
        }
        internal byte[] Surface(Dictionary<string,string> textures)
        {
            using var bytes=new MemoryStream();using var w=new BinaryWriter(bytes);
            w.Write((ushort)1);w.Write(template);w.Write(false);w.Write(maps);
            w.Write((byte)255);w.Write(textures.Count);
            foreach(var pair in textures){w.Write(pair.Key);w.Write((byte)255);w.Write(PreparePaint.HashBytes(pair.Value));}
            w.Write(keywords);return bytes.ToArray();
        }
        private static int RawSize(byte[] payload)
        {
            using var r=new BinaryReader(new MemoryStream(payload));int v=r.ReadUInt16();
            int width=r.ReadUInt16(),height=r.ReadUInt16();if(v>=4 && r.ReadUInt16()!=1)throw new InvalidDataException("Non-2D texture.");
            int mips=r.ReadByte();var format=v>=3?(GraphicsFormat)r.ReadByte():(GraphicsFormat)Enum.Parse(typeof(GraphicsFormat),r.ReadString());
            long size=0;for(int i=0;i<mips;i++)size+=(long)GraphicsFormatUtility.ComputeMipmapSize(Math.Max(1,width>>i),Math.Max(1,height>>i),format);
            return checked((int)size);
        }
        private static bool Present(BinaryReader r) => r.ReadByte() switch {0=>false,255=>true,_=>throw new InvalidDataException("Native dictionary marker.")};
        private static int Count(BinaryReader r) => Present(r)?r.ReadInt32():0;
        public void Dispose(){foreach(var image in images)UnityEngine.Object.DestroyImmediate(image);images.Clear();}
    }
}
