using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;

// Offline source-unit authoring. Receiver dimensions and projection never enter
// the texture recipe. SurfaceAtlas, channel mip filtering and BC7 are production code.
public static class PrepareRepeat
{
    public const string Recipe="BridgeBuilder/RoadRepeat/1";
    static readonly string[] Names={"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask"};
    [Serializable] public sealed class Band { public float start,end,repeatY=1;public float[] min,max; }
    [Serializable] public sealed class Unit
    { public string style,sourceCid,sourceDigest,key;public float[] min,max;public bool cutout,rows;public Band[] bands; }
    [Serializable] public sealed class Inventory { public string recipe;public PrepareStructure.Source[] sources;public Unit[] units; }
    [Serializable] public sealed class Result
    { public string style,key,sourceCid,surfaceCid;public int width,height,sourceWidth,sourceHeight;public long bytes; }
    [Serializable] public sealed class Report
    { public string recipe,inventoryDigest,pipeline;public int createdProducts,reusedProducts,bakedTextures,borrowedControls;public Result[] results; }
    static string Key(Unit u,bool material)=>PreparedBakeStore.Key(w=>{
        w.Write(Recipe);if(material)w.Write(u.sourceDigest.ToLowerInvariant());
        w.Write(u.min[0]);w.Write(u.min[1]);w.Write(u.max[0]);w.Write(u.max[1]);
        if(material)w.Write(u.cutout);w.Write(u.rows);w.Write(u.bands==null?-1:u.bands.Length);
        if(u.bands!=null)foreach(var b in u.bands){w.Write(b.start);w.Write(b.end);w.Write(b.repeatY);w.Write(b.min[0]);w.Write(b.min[1]);w.Write(b.max[0]);w.Write(b.max[1]);}
    });
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string game=Arg("-gameData"),path=Arg("-repeatInventory"),output=Arg("-preparedOutput");
        var inv=JsonUtility.FromJson<Inventory>(File.ReadAllText(path));
        if(inv.recipe!=Recipe || inv.units==null || inv.units.Length==0)throw new InvalidDataException("Invalid repeat inventory.");
        // Unity JsonUtility reads JSON null arrays as empty arrays. Empty bands
        // cannot cover a unit; restore the no-band sentinel used by runtime keys.
        foreach(var unit in inv.units)if(unit.bands!=null && unit.bands.Length==0)unit.bands=null;
        if(QualitySettings.activeColorSpace!=ColorSpace.Linear)throw new InvalidOperationException("Repeat preparation requires Linear color space.");
        var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatBake.compute");
        int kernel=program.FindKernel("SurfaceAtlas");
        var store=new RepeatStore(Arg("-preparedStore"),args.Contains("-readOnlyStores")?Arg("-readOnlyStores").Split('|'):Array.Empty<string>(),args.Contains("-reuseOnly"));
        string pipeline=Arg("-pipelineKey")+"/"+Application.unityVersion+"/"+SystemInfo.graphicsDeviceVersion;
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        var results=new List<Result>();var module=PreparePaint.OpenCodec(game);
        int bakedTextures=0,borrowedControls=0;
        try
        {
            foreach(string family in new[]{"Game","BridgesAndPorts"})
            foreach(string p in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT")))
            {
                var archive=ZipFile.OpenRead(p);archives.Add(archive);
                foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".cid",StringComparison.Ordinal)))
                {
                    using var r=new StreamReader(entry.Open());string cid=r.ReadToEnd().Trim();
                    var payload=archive.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));if(payload==null)continue;
                    if(entries.TryGetValue(cid,out var old)&&!Bytes(old).SequenceEqual(Bytes(payload)))throw new InvalidDataException("Conflicting native CID: "+cid);
                    entries[cid]=payload;
                }
            }
            byte[] Read(string cid)=>entries.TryGetValue(cid,out var e)?Bytes(e):throw new FileNotFoundException("Native source missing: "+cid);
            foreach(var group in inv.units.GroupBy(u=>u.sourceCid))
            {
                var bytes=Read(group.Key);string digest=PreparePaint.Digest(bytes);
                if(group.Any(u=>u.sourceDigest!=digest))throw new InvalidDataException("Repeat source version changed: "+group.Key);
                // Reuse the existing authoring decoder without forking its VT/native logic.
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var source=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,
                    new object[]{bytes,inv.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
                var textureKeys=(Dictionary<string,string>)type.GetField("TextureKeys",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
                foreach(string name in Names.Take(3))if(!textures.ContainsKey(name))throw new InvalidDataException("Missing repeat channel: "+name);
                bool missingControl=!textures.ContainsKey(Names[3]);
                if(missingControl){textures.Add(Names[3],Texture2D.blackTexture);textureKeys.Add(Names[3],"Unity/blackTexture/opaque/v1");}
                int nw=Names.Max(n=>textures[n].width),nh=Names.Max(n=>textures[n].height);
                foreach(var u in group)
                {
                    string key=Key(u,true);if(!string.IsNullOrEmpty(u.key)&&u.key!=key)throw new InvalidDataException("Repeat key differs: "+u.key);
                    float rw=(u.max[0]-u.min[0])*nw,rh=(u.max[1]-u.min[1])*nh;
                    if(u.bands!=null)
                    {
                        rw=0;rh=0;float previous=0;
                        foreach(var b in u.bands){if(b.start!=previous||b.end<=b.start||b.end>1||b.max[0]<=b.min[0]||b.max[1]<=b.min[1]||float.IsNaN(b.repeatY)||float.IsInfinity(b.repeatY)||b.repeatY<=0)throw new InvalidDataException("Invalid contiguous repeat bands.");
                            rw=Mathf.Max(rw,(b.max[0]-b.min[0])*nw/(u.rows?1:b.end-b.start));rh=Mathf.Max(rh,(b.max[1]-b.min[1])*b.repeatY*nh/(u.rows?b.end-b.start:1));previous=b.end;}
                        if(previous!=1)throw new InvalidDataException("Repeat bands do not cover unit.");
                    }
                    int width=4*Mathf.CeilToInt(rw/4),height=4*Mathf.CeilToInt(rh/4);
                    if(width<=0||height<=0||width>SystemInfo.maxTextureSize||height>SystemInfo.maxTextureSize)throw new InvalidDataException("Repeat dimensions unsupported.");
                    var bindings=new Dictionary<string,string>();var products=new Dictionary<string,PreparedBakeStore.Payload>();
                    RenderTexture[] targets=null;
                    try
                    {
                        for(int channel=0;channel<Names.Length;channel++)
                        {
                            int index=channel;string name=Names[channel];
                            if(missingControl && index==3)
                            {
                                var zero=ZeroControl(Path.Combine(Arg("-preparedMaterials"),u.style,"Road","ZeroControl.Texture"));
                                bindings.Add(name,zero.Cid);products[name+".Texture"]=zero;borrowedControls++;continue;
                            }
                            string ProductKey(string sampler)=>PreparedBakeStore.Key(w=>{w.Write(Recipe);w.Write(pipeline);w.Write(textureKeys[name]);w.Write(sampler);w.Write(width);w.Write(height);w.Write(index);});
                            string textureKey=ProductKey(Key(u,false));
                            byte[] Produce(){
                                if(args.Contains("-reuseTexturesOnly"))throw new InvalidOperationException("Texture-only reuse has no existing product: "+textureKey);
                                bakedTextures++;
                                if(targets==null)targets=Render(program,kernel,u,textures,width,height,missingControl);
                                using var c=new MovableAtlasChannel(program,targets[index],index,true);c.Read();return PreparePaint.Texture(c.Texture);
                            }
                            var product=store.Get(textureKey,".Texture",Produce);
                            bindings.Add(name,product.Cid);products[name+".Texture"]=product;
                        }
                    }
                    finally{if(targets!=null)foreach(var t in targets){t.Release();UnityEngine.Object.DestroyImmediate(t);}}
                    string surfaceKey=PreparedBakeStore.Key(w=>{w.Write(Recipe);w.Write("NativeDefault/1");w.Write(u.cutout);foreach(var n in Names){w.Write(n);w.Write(bindings[n]);}});
                    var surface=store.Get(surfaceKey,".Surface",()=>Surface(u.cutout,bindings));
                    string folder=Path.Combine(output,u.style,"Repeat",key);Write(Path.Combine(folder,"Repeat.Surface"),surface);
                    foreach(var product in products.Values)Write(Path.Combine(output,u.style,"Shared",product.Cid+".Texture"),product);
                    results.Add(new Result{style=u.style,key=key,sourceCid=u.sourceCid,surfaceCid=surface.Cid,width=width,height=height,sourceWidth=nw,sourceHeight=nh,bytes=products.Values.Sum(p=>(long)p.Bytes.Length+32)+surface.Bytes.Length+32});
                    Debug.Log($"Prepared repeat {u.style}/{key}: {width}x{height}, native source {nw}x{nh}.");
                }
            }
            File.WriteAllText(Arg("-preparationReport"),JsonUtility.ToJson(new Report{recipe=Recipe,inventoryDigest=PreparePaint.Digest(File.ReadAllBytes(path)),pipeline=pipeline,createdProducts=store.Created,reusedProducts=store.Reused,bakedTextures=bakedTextures,borrowedControls=borrowedControls,results=results.ToArray()},true));
            Debug.Log($"PASS prepared repeats: {results.Count} units; created={store.Created}; reused={store.Reused}; bakedTextures={bakedTextures}; borrowedControls={borrowedControls}; four native channels, complete mip chains.");
        }
        finally{foreach(var a in archives)a.Dispose();PreparePaint.CloseCodec(module);}
    }
    static PreparedBakeStore.Payload ZeroControl(string path)
    {
        var bytes=File.ReadAllBytes(path);string cid=File.ReadAllText(path+".cid").Trim();
        // Reuse the already prepared, GPU-verified transparent 1x1 RGBA8 resource.
        // Never substitute opaque black or silently regenerate a missing source.
        if(cid!="729893d593df5d52ba911bc1e7eab77b" || PreparePaint.Digest(bytes)!="d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4")
            throw new InvalidDataException("Prepared transparent control differs: "+path);
        return new PreparedBakeStore.Payload(cid,bytes);
    }
    static RenderTexture[] Render(ComputeShader p,int kernel,Unit u,Dictionary<string,Texture2D> source,int width,int height,bool skipControl)
    {
        var targets=new RenderTexture[4];
        try
        {
            p.SetInt("SkipAtlasControl",skipControl?1:0);
            for(int i=0;i<4;i++){bool dummyControl=skipControl&&i==3;targets[i]=new RenderTexture(dummyControl?1:width,dummyControl?1:height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};targets[i].Create();p.SetTexture(kernel,new[]{"AtlasColor","AtlasNormal","AtlasMask","AtlasControl"}[i],targets[i]);p.SetTexture(kernel,Names[i],source[Names[i]]);}
            p.SetInt("OriginalRoad",0);p.SetInt("ReplaceRoad",0);p.SetInt("RoadBandCount",0);p.SetInt("RoadBandRows",0);
            using var dummy=new ComputeBuffer(2,16);dummy.SetData(new Vector4[2]);p.SetBuffer(kernel,"RoadBands",dummy);
            foreach(string n in Names.Take(3))p.SetTexture(kernel,"_Road"+n,source[n]);
            foreach(string n in new[]{"_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"})p.SetTexture(kernel,n,Texture2D.whiteTexture);
            p.SetVector("ChartOrigin",Vector3.zero);p.SetVector("ChartU",Vector3.right);p.SetVector("ChartV",Vector3.forward);
            var bands=u.bands??new[]{new Band{start=0,end=1,min=u.min,max=u.max,repeatY=1}};
            foreach(var b in bands)
            {
                int start=Mathf.RoundToInt(b.start*(u.rows?height:width)),end=Mathf.RoundToInt(b.end*(u.rows?height:width));
                if(end<=start){if(u.rows)continue;throw new InvalidDataException("Repeat band has no pixels.");}
                int w=u.rows?width:end-start,h=u.rows?end-start:height;
                p.SetVector("AtlasPixels",new Vector4(w,0,h,0));p.SetVector("AtlasRect",new Vector4(u.rows?0:start,u.rows?start:0,w,h));
                p.SetVector("PatchU",new Vector4(1,0,0,0));p.SetVector("PatchV",new Vector4(0,0,b.repeatY,0));
                p.SetVector("PatchArea",new Vector4(b.min[0],b.min[1],b.max[0]-b.min[0],b.max[1]-b.min[1]));
                p.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
            }
            return targets;
        }
        catch{foreach(var t in targets)if(t!=null){t.Release();UnityEngine.Object.DestroyImmediate(t);}throw;}
    }
    static byte[] Surface(bool cutout,Dictionary<string,string> bindings)
    {
        using var bytes=new MemoryStream();using var w=new BinaryWriter(bytes);
        w.Write((ushort)1);w.Write(cutout?14:0);w.Write(false);
        w.Write((byte)255);w.Write(1);w.Write("_Snow");w.Write(0f);
        for(int i=0;i<3;i++){w.Write((byte)255);w.Write(0);}
        w.Write((byte)255);w.Write(bindings.Count);foreach(var n in Names){w.Write(n);w.Write((byte)255);w.Write(PreparePaint.HashBytes(bindings[n]));}
        var keywords=cutout?new[]{"_ALPHATEST_ON","_DISABLE_SSR_TRANSPARENT","_DOUBLESIDED_ON","_TANGENTSPACE_OCTO"}:new[]{"_TANGENTSPACE_OCTO"};
        w.Write((byte)255);w.Write(keywords.Length);foreach(string k in keywords)w.Write(k);return bytes.ToArray();
    }
    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
    static void Write(string path,PreparedBakeStore.Payload p)
    {
        if(File.Exists(path)){if(!File.ReadAllBytes(path).SequenceEqual(p.Bytes)||File.ReadAllText(path+".cid")!=p.Cid)throw new IOException("Conflicting prepared output: "+path);return;}
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,p.Bytes);File.WriteAllText(path+".cid",p.Cid,new UTF8Encoding(false));
        if(!File.ReadAllBytes(path).SequenceEqual(p.Bytes))throw new IOException("Prepared write differs: "+path);
    }
    sealed class RepeatStore
    {
        readonly PreparedBakeStore writable;readonly string[] readOnly;readonly bool reuseOnly;public int Created=>writable.Created;public int Reused=>writable.Reused+borrowed;int borrowed;
        internal RepeatStore(string root,string[] readOnly,bool reuseOnly){writable=new PreparedBakeStore(root,Recipe);this.readOnly=readOnly;this.reuseOnly=reuseOnly;}
        byte[] Produce(string key,Func<byte[]> producer)
        {if(reuseOnly)throw new InvalidOperationException("Reuse-only preparation has no existing product: "+key);return producer();}
        internal PreparedBakeStore.Payload Get(string key,string extension,Func<byte[]> producer)
        {
            foreach(string root in readOnly){string path=Path.Combine(root,key+extension);if(!File.Exists(path))continue;var bytes=File.ReadAllBytes(path);if(File.ReadAllText(path+".sha256")!=PreparePaint.Digest(bytes))throw new InvalidDataException("Read-only product digest differs: "+path);borrowed++;return new PreparedBakeStore.Payload(PreparePaint.Digest(Encoding.UTF8.GetBytes(Recipe+"/"+PreparePaint.Digest(bytes))).Substring(0,32),bytes);}
            return writable.Get(key,extension,()=>Produce(key,producer));
        }
    }
}
