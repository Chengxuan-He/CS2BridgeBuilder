using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Offline verification only. Reads the finished files using the installed game's
// readers and uploads the complete native mip payload without regenerating mips.
public static class RepeatNativeCheck
{
    [Serializable] public sealed class Band { public float start,end,repeatY; public float[] min,max; }
    [Serializable] public sealed class Unit
    {
        public string style,key,sourceDigest; public float[] min,max;
        public bool cutout,rows; public Band[] bands;
    }
    [Serializable] public sealed class Inventory { public string recipe; public Unit[] units; }
    [Serializable] public sealed class Result
    { public string style,key; public int sourceWidth,sourceHeight,width,height; }
    [Serializable] public sealed class Report { public string recipe,inventoryDigest; public Result[] results; }
    [DllImport("kernel32",CharSet=CharSet.Unicode)] private static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32")] private static extern bool FreeLibrary(IntPtr module);
    private static readonly string[] Channels={"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask"};

    static string RecipeKey(Unit unit)
    {
        using var stream=new MemoryStream();
        using var writer=new BinaryWriter(stream);
        writer.Write("BridgeBuilder/RoadRepeat/1");writer.Write(unit.sourceDigest.ToLowerInvariant());
        writer.Write(unit.min[0]);writer.Write(unit.min[1]);writer.Write(unit.max[0]);writer.Write(unit.max[1]);
        writer.Write(unit.cutout);writer.Write(unit.rows);writer.Write(unit.bands==null?-1:unit.bands.Length);
        if(unit.bands!=null)foreach(var band in unit.bands)
        {
            writer.Write(band.start);writer.Write(band.end);writer.Write(band.repeatY);
            writer.Write(band.min[0]);writer.Write(band.min[1]);writer.Write(band.max[0]);writer.Write(band.max[1]);
        }
        writer.Flush();return Digest(stream.ToArray());
    }

    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name) => args[Array.IndexOf(args,name)+1];
        string managed=Arg("-gameManaged"),root=Arg("-preparedOutput");
        var inventory=JsonUtility.FromJson<Inventory>(File.ReadAllText(Arg("-repeatInventory")));
        foreach(var unit in inventory.units)
        {
            if(unit.bands!=null && unit.bands.Length==0)unit.bands=null;
            string key=RecipeKey(unit);
            Require(string.IsNullOrEmpty(unit.key) || unit.key==key,"exact immutable repeat recipe key");
            unit.key=key;
        }
        var report=JsonUtility.FromJson<Report>(File.ReadAllText(Arg("-preparationReport")));
        Require(inventory.recipe=="BridgeBuilder/RoadRepeat/1","supported repeat recipe");
        Require(report.recipe==inventory.recipe && report.inventoryDigest==Digest(File.ReadAllBytes(Arg("-repeatInventory"))),"report belongs to the exact inventory bytes");
        var module=LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")));
        Require(module!=IntPtr.Zero,"native compression library");
        ResolveEventHandler resolve=(_,e)=>{
            string file=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
            return File.Exists(file)?Assembly.LoadFrom(file):null;
        };
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try
        {
            var database=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
            var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
            var surfaceType=database.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var textureType=database.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var hashReader=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
            var decompress=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true)
                .GetMethods().Single(m=>m.Name=="Decompress" && m.GetParameters()[0].ParameterType==typeof(byte[]));
            var zstd=Enum.Parse(decompress.GetParameters()[1].ParameterType,"ZSTD");
            long mipBytes=0; int textures=0,surfaces=0,sharedVariants=0;
            var sampled=new Dictionary<string,string[]>();
            var checkedTextures=new Dictionary<string,string>();
            foreach(var style in inventory.units.GroupBy(u=>u.style))
            {
                // A missing dependency must never fall through into another style.
                string styleRoot=Path.Combine(root,style.Key);
                var paths=new Dictionary<string,string>();
                foreach(var file in Directory.GetFiles(styleRoot,"*.Texture",SearchOption.AllDirectories))
                {
                    string cid=Cid(file);
                    if(paths.TryGetValue(cid,out var previous))
                        Require(File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(previous)),"same CID has identical bytes");
                    else paths.Add(cid,file);
                }
                foreach(var unit in style)
                {
                    var result=report.results.Single(r=>r.style==unit.style && r.key==unit.key);
                    var size=Dimensions(unit,result.sourceWidth,result.sourceHeight);
                    Require(result.width==size.x && result.height==size.y,"native source-density dimensions match inventory");
                    string file=Path.Combine(styleRoot,"Repeat",unit.key,"Repeat.Surface");
                    Cid(file);
                    var surface=FormatterServices.GetUninitializedObject(surfaceType);
                    using var reader=new BinaryReader(File.OpenRead(file));
                    surfaceType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(surface,new object[]{reader});
                    Require((int)surfaceType.GetProperty("materialTemplateHash").GetValue(surface)==(unit.cutout?14:0),"native repeat template");
                    Require(!(bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface),"non-VT repeat sampler");
                    var floats=(IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null,new object[]{reader});
                    Require(floats.Contains("_Snow") && (float)floats["_Snow"]==0,"native snow override");
                    foreach(var method in new[]{"ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})
                        binary.GetMethod(method).Invoke(null,new object[]{reader});
                    Require(reader.ReadByte()==255 && reader.ReadInt32()==4,"all four native sampled channels");
                    var bindings=new Dictionary<string,string>();
                    for(int i=0;i<4;i++)
                    {
                        string channel=reader.ReadString(); Require(reader.ReadByte()==255,"channel reference present");
                        bindings.Add(channel,hashReader.Invoke(null,new object[]{reader}).ToString());
                    }
                    Require(bindings.Keys.OrderBy(x=>x).SequenceEqual(Channels.OrderBy(x=>x)),"complete native channel semantics");
                    var keywords=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader})).Cast<string>();
                    Require(keywords.Contains("_TANGENTSPACE_OCTO"),"persistent packed tangent decoder");
                    Require(reader.BaseStream.Position==reader.BaseStream.Length,"complete native Surface consumed");
                    string recipe=unit.sourceDigest+"/"+JsonUtility.ToJson(new Sampling(unit));
                    var cids=Channels.Select(c=>bindings[c]).ToArray();
                    if(sampled.TryGetValue(recipe,out var same))
                    { Require(cids.SequenceEqual(same),"equivalent opaque/cutout variants share texture identities");sharedVariants++; }
                    else sampled.Add(recipe,cids);
                    foreach(var channel in Channels)
                    {
                        string cid=bindings[channel]; Require(paths.ContainsKey(cid),"style-local dependency complete: "+cid);
                        string textureFile=paths[cid];
                        var asset=FormatterServices.GetUninitializedObject(textureType);
                        using var tr=new BinaryReader(File.OpenRead(textureFile));
                        var version=textureType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{tr});
                        Require((ushort)version==5,"native Texture format version");
                        int w=(int)textureType.GetProperty("width").GetValue(asset),h=(int)textureType.GetProperty("height").GetValue(asset);
                        bool zero=channel=="_ControlMask" && cid=="729893d593df5d52ba911bc1e7eab77b";
                        Require(zero?w==1&&h==1:w==size.x&&h==size.y,"source-density texture size or verified constant control");
                        var format=(GraphicsFormat)textureType.GetProperty("format").GetValue(asset);
                        Require(format==(zero?GraphicsFormat.R8G8B8A8_UNorm:channel=="_BaseColorMap"?GraphicsFormat.RGBA_BC7_SRGB:GraphicsFormat.RGBA_BC7_UNorm),"channel color space and native encoding");
                        string digest=Digest(File.ReadAllBytes(textureFile));
                        if(zero)Require(digest=="d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4","exact verified transparent control payload");
                        if(checkedTextures.TryGetValue(cid,out var known))
                        { Require(known==digest,"same texture identity has identical bytes across styles");continue; }
                        checkedTextures.Add(cid,digest);
                        int compressed=tr.ReadInt32();
                        var bytes=tr.ReadBytes(checked((int)(tr.BaseStream.Length-tr.BaseStream.Position)));
                        if(compressed!=-1)
                        { Require(compressed==bytes.Length,"native compressed length");bytes=(byte[])decompress.Invoke(null,new[]{(object)bytes,zstd}); }
                        var flags=TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit;
                        if(Math.Max(w,h)>1)flags|=TextureCreationFlags.MipChain;
                        var texture=new Texture2D(w,h,format,flags);
                        try
                        {
                            int mips=1+(int)Math.Floor(Math.Log(Math.Max(w,h),2));
                            long expected=0;for(int mip=0;mip<mips;mip++)expected+=(long)GraphicsFormatUtility.ComputeMipmapSize(Math.Max(1,w>>mip),Math.Max(1,h>>mip),format);
                            Require(bytes.LongLength==expected,"complete mip footprint");
                            texture.LoadRawTextureData(bytes);texture.Apply(false,false);
                            Require(texture.mipmapCount==mips && texture.GetRawTextureData<byte>().ToArray().SequenceEqual(bytes),"all native mip bytes uploaded unchanged");
                            mipBytes+=bytes.Length;textures++;
                        }
                        finally { UnityEngine.Object.DestroyImmediate(texture); }
                    }
                    surfaces++;
                }
            }
            Debug.Log($"PASS native repeat readers: {surfaces} Surface descriptors; {textures} unique native textures; {sharedVariants} equivalent descriptor variants; {mipBytes} mip bytes uploaded unchanged; source-density dimensions; no game scene created.");
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve-=resolve;FreeLibrary(module); }
    }

    [Serializable] private sealed class Sampling
    {
        public float[] min,max;public bool rows;public Band[] bands;
        internal Sampling(Unit u){min=u.min;max=u.max;rows=u.rows;bands=u.bands;}
    }
    private static Vector2Int Dimensions(Unit unit,int sourceWidth,int sourceHeight)
    {
        Require(sourceWidth>0 && sourceHeight>0,"measured native source dimensions");
        float width=(unit.max[0]-unit.min[0])*sourceWidth,height=(unit.max[1]-unit.min[1])*sourceHeight;
        if(unit.bands!=null && unit.bands.Length>0)
        {
            width=height=0;float previous=0;
            foreach(var band in unit.bands)
            {
                Require(band.start==previous && band.end>band.start && band.end<=1 && band.repeatY>0,"contiguous measured bands");
                width=Mathf.Max(width,(band.max[0]-band.min[0])*sourceWidth/(unit.rows?1:band.end-band.start));
                height=Mathf.Max(height,(band.max[1]-band.min[1])*band.repeatY*sourceHeight/(unit.rows?band.end-band.start:1));
                previous=band.end;
            }
            Require(previous==1,"bands cover native repeat unit");
        }
        return new Vector2Int(4*Mathf.CeilToInt(width/4),4*Mathf.CeilToInt(height/4));
    }
    private static string Cid(string path)
    {
        string cid=File.ReadAllText(path+".cid").Trim();
        Require(cid.Length==32 && cid.Any(c=>c!='0') && cid.All(c=>"0123456789abcdef".Contains(c.ToString())),"valid native CID sidecar");return cid;
    }
    private static string Digest(byte[] bytes)
    {using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    private static void Require(bool ok,string reason){if(!ok)throw new InvalidDataException(reason);}
}
