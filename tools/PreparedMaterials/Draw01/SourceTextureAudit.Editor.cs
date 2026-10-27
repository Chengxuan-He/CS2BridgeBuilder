using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Read original native source bytes and all levels exposed by the existing reader.
// That reader currently exposes mip zero only. No bake or re-encoding.
public static class Draw01SourceTextureAudit
{
    [Serializable] public class Input { public PrepareStructure.Source[] sources; }
    [Serializable] public class Image { public string property,key,rawSha256,format; public int width,height,mips,bytes; public int filter,wrapU,wrapV,aniso; }
    [Serializable] public class Result { public string sourceCid,sourceSha256;public Image[] channels;public int producedTextures=0; }
    static byte[] Bytes(ZipArchiveEntry entry) { using var s=entry.Open();using var m=new MemoryStream();s.CopyTo(m);return m.ToArray(); }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string game=Arg("-gameData"),cid="97291ab7f7938fc35cc670b15f2bca36";
        var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-inventory")));
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();
        var codec=PreparePaint.OpenCodec(game);
        try {
            foreach(var family in new[]{"Game","BridgesAndPorts"})
            foreach(var path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))) {
                var archive=ZipFile.OpenRead(path);archives.Add(archive);
                foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".cid"))) {
                    using var reader=new StreamReader(entry.Open());var id=reader.ReadToEnd().Trim();
                    var payload=archive.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));
                    if(payload!=null)entries[id]=payload;
                }
            }
            byte[] Read(string id)=>Bytes(entries[id]);
            var bytes=Read(cid);
            if(PreparePaint.Digest(bytes)!="c93b794bd592145931c03b5e4b6e7c247d7d7174f87ef0d1c6d844bf979af341")throw new InvalidDataException("Prototype differs.");
            var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
            using var material=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{bytes,input.sources.Single(s=>s.cid==cid),(Func<string,byte[]>)Read},null);
            var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(material);
            var keys=(Dictionary<string,string>)type.GetField("TextureKeys",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(material);
            var images=textures.Select(pair=> {
                var t=pair.Value;var raw=t.GetRawTextureData<byte>().ToArray();
                return new Image{property=pair.Key,key=keys[pair.Key],rawSha256=PreparePaint.Digest(raw),format=t.graphicsFormat.ToString(),width=t.width,height=t.height,mips=t.mipmapCount,bytes=raw.Length,filter=(int)t.filterMode,wrapU=(int)t.wrapModeU,wrapV=(int)t.wrapModeV,aniso=t.anisoLevel};
            }).ToArray();
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Result{sourceCid=cid,sourceSha256=PreparePaint.Digest(bytes),channels=images},true));
            Debug.Log("PASS Draw01 native shared source: "+images.Length+" channels, "+images.Sum(i=>i.mips)+" decoded mip levels (reader exposes mip zero only), no baked textures.");
        } finally {foreach(var archive in archives)archive.Dispose();PreparePaint.CloseCodec(codec);}
    }
}
