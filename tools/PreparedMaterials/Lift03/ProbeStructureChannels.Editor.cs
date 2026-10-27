using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Read-only decoder probe. It never dispatches a shader or writes native assets.
public static class Lift03StructureChannels
{
    [Serializable] public sealed class Channel { public string name, key, format; public int width, height, mips; public long rawBytes; }
    [Serializable] public sealed class Source { public string cid, entry, digest; public int units; public string[] declared, absent; public Channel[] decoded; }
    [Serializable] public sealed class Report { public string inventoryDigest, decoderDigest; public int units; public Source[] sources; }
    static byte[] Bytes(ZipArchiveEntry e) { using var s=e.Open();using var m=new MemoryStream();s.CopyTo(m);return m.ToArray(); }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        var path=Arg("-structureInventory");
        var inv=JsonUtility.FromJson<PrepareStructure.Inventory>(File.ReadAllText(path));
        var units=inv.units.Where(u=>u.style=="Lift03").ToArray();
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            foreach(var family in new[]{"Game","BridgesAndPorts"})
            foreach(var file in Directory.GetFiles(Path.Combine(Arg("-gameData"),"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT")))
            {
                var zip=ZipFile.OpenRead(file);archives.Add(zip);
                foreach(var e in zip.Entries.Where(e=>e.FullName.EndsWith(".cid",StringComparison.Ordinal)))
                { using var r=new StreamReader(e.Open());var cid=r.ReadToEnd().Trim();var payload=zip.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));if(payload!=null)entries[cid]=payload; }
            }
            byte[] Read(string cid)=>Bytes(entries[cid]);
            var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
            var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
            var result=new List<Source>();
            foreach(var group in units.GroupBy(u=>u.sourceCid))
            {
                var description=inv.sources.Single(s=>s.cid==group.Key);var bytes=Read(group.Key);var digest=PreparePaint.Digest(bytes);
                if(group.Any(u=>u.sourceDigest!=digest))throw new InvalidDataException("Source digest differs: "+group.Key);
                using var source=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{bytes,description,(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",flags).GetValue(source);
                var keys=(Dictionary<string,string>)type.GetField("TextureKeys",flags).GetValue(source);
                result.Add(new Source{cid=group.Key,entry=entries[group.Key].FullName,digest=digest,units=group.Count(),declared=description.channels.Select(c=>c.name).ToArray(),absent=description.channels.Select(c=>c.name).Except(textures.Keys).ToArray(),decoded=textures.OrderBy(p=>p.Key).Select(p=>new Channel{name=p.Key,key=keys[p.Key],format=p.Value.graphicsFormat.ToString(),width=p.Value.width,height=p.Value.height,mips=p.Value.mipmapCount,rawBytes=p.Value.GetRawTextureData<byte>().Length}).ToArray()});
                Debug.Log("Lift03 source decoded: "+group.Key+" "+string.Join(", ",result.Last().decoded.Select(c=>c.name+"="+c.width+"x"+c.height)));
            }
            var report=new Report{inventoryDigest=PreparePaint.Digest(File.ReadAllBytes(path)),decoderDigest=PreparePaint.Digest(File.ReadAllBytes("Assets/Editor/PrepareStructure.cs")),units=units.Length,sources=result.ToArray()};
            File.WriteAllText(Arg("-channelReport"),JsonUtility.ToJson(report,true));
            Debug.Log("PASS Lift03 channel decoder probe: "+units.Length+" units, "+result.Count+" sources. No bake dispatched.");
        }
        finally { foreach(var a in archives)a.Dispose();PreparePaint.CloseCodec(module); }
    }
}
