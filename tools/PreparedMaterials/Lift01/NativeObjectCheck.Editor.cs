using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using BridgeBuilder.Bridges;

// Native reader and original complete mip upload validation, not game visual acceptance.
public static class Lift01NativeObjectCheck
{
    [Serializable] public sealed class Source { public string sourceCid,outputPath,outputCid; }
    [Serializable] public sealed class Input { public Source[] sources; }
    [Serializable] public sealed class TextureProof { public string cid,sha256,rawMipSha256,format,wrapU,wrapV,filter; public int width,height,mips,rawMipBytes,aniso; }
    [Serializable] public sealed class Report { public int surfaces,uniqueDescriptors,uniqueTextures,mipLevels; public bool nativeReaderVerified=true,nativeRenderingVerified=false; public TextureProof[] textures; }
    static void Need(bool value,string message) { if(!value)throw new InvalidDataException(message); }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string root=Arg("-candidateRoot"),game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
        var module=PreparePaint.OpenCodec(game);
        ResolveEventHandler resolve=(_,e)=>{string path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        var archives=new List<ZipArchive>();
        try
        {
            var input=JsonUtility.FromJson<Input>(File.ReadAllText(Path.Combine(root,"manifest.json")));
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var hashes=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
            var surfaceType=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            Dictionary<string,string> Read(byte[] bytes,int expectedTemplate)
            {
                using var reader=new BinaryReader(new MemoryStream(bytes));
                var surface=FormatterServices.GetUninitializedObject(surfaceType);
                surfaceType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(surface,new object[]{reader});
                Need((int)surfaceType.GetProperty("materialTemplateHash").GetValue(surface)==expectedTemplate,"Native template mismatch");
                Need(!(bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface),"Unexpected VT material");
                foreach(var method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})
                    Need(((IDictionary)binary.GetMethod(method).Invoke(null,new object[]{reader})).Count==0,"Unreviewed network parameters");
                Need(reader.ReadByte()==255,"Texture dictionary missing");var bindings=new Dictionary<string,string>();
                for(int n=reader.ReadInt32();n>0;n--){string key=reader.ReadString();Need(reader.ReadByte()==255,"Null binding");bindings.Add(key,hashes.Invoke(null,new object[]{reader}).ToString());}
                var keywords=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader})).Cast<string>().ToArray();
                Need(keywords.SequenceEqual(new[]{"_TANGENTSPACE_OCTO"}),"Packed tangent decoder must be retained");
                Need(reader.BaseStream.Position==reader.BaseStream.Length,"Unconsumed Surface bytes");return bindings;
            }
            var required=new HashSet<string>();var outputIds=new HashSet<string>();
            foreach(var source in input.sources)
            {
                var before=File.ReadAllBytes(Path.Combine(root,"originals",source.sourceCid+".Surface"));
                var after=File.ReadAllBytes(Path.Combine(root,source.outputPath));
                Need(before.Length==after.Length&&Enumerable.Range(0,before.Length).Where(i=>before[i]!=after[i]).SequenceEqual(new[]{2}),"Descriptor change exceeds template identifier");
                var original=Read(before,33);var converted=Read(after,0);
                Need(original.Count==3&&original.OrderBy(x=>x.Key).SequenceEqual(converted.OrderBy(x=>x.Key)),"Original texture CIDs differ");
                foreach(var cid in converted.Values)required.Add(cid);
                Need(File.ReadAllText(Path.Combine(root,source.outputPath)+".cid").Trim()==source.outputCid,"Descriptor CID sidecar mismatch");outputIds.Add(source.outputCid);
            }
            var entries=new Dictionary<string,ZipArchiveEntry>();
            foreach(string family in new[]{"Game","BridgesAndPorts"})
            foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"Blob*.cok"))
            {
                var z=ZipFile.OpenRead(path);archives.Add(z);
                foreach(var entry in z.Entries.Where(e=>e.FullName.EndsWith(".Texture.cid")))
                {using var reader=new StreamReader(entry.Open());string cid=reader.ReadToEnd().Trim();if(required.Contains(cid))entries[cid]=z.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));}
            }
            Need(entries.Count==required.Count,"Missing original built-in texture");
            var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
            var decode=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));
            var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
            var proofs=new List<TextureProof>();
            foreach(var pair in entries.OrderBy(x=>x.Key))
            {
                using var read=pair.Value.Open();using var bytes=new MemoryStream();read.CopyTo(bytes);var payload=bytes.ToArray();
                using var stream=new MemoryStream(payload);var texture=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
                try
                {
                    Need(stream.Position==stream.Length,"Incomplete native texture read");
                    Need(texture.mipmapCount==1+(int)Math.Floor(Math.Log(Math.Max(texture.width,texture.height),2)),"Incomplete mip chain");
                    var raw=texture.GetRawTextureData<byte>().ToArray();
                    proofs.Add(new TextureProof{cid=pair.Key,sha256=PreparePaint.Digest(payload),rawMipSha256=PreparePaint.Digest(raw),format=texture.graphicsFormat.ToString(),width=texture.width,height=texture.height,mips=texture.mipmapCount,rawMipBytes=raw.Length,filter=texture.filterMode.ToString(),wrapU=texture.wrapModeU.ToString(),wrapV=texture.wrapModeV.ToString(),aniso=texture.anisoLevel});
                }
                finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            var report=new Report{surfaces=input.sources.Length,uniqueDescriptors=outputIds.Count,uniqueTextures=proofs.Count,mipLevels=proofs.Sum(x=>x.mips),textures=proofs.ToArray()};
            File.WriteAllText(Path.Combine(root,"native-check.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"PASS Lift01 native object descriptors: sources={report.surfaces}; descriptors={report.uniqueDescriptors}; originalTextures={report.uniqueTextures}; mipLevels={report.mipLevels}; imageWrites=0; nativeRenderingVerified=false.");
        }
        finally{foreach(var z in archives)z.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
    }
}
