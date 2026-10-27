using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

// Offline evidence only: decode each original source once; never create material payloads.
public static class Lift02SourceSamplingAudit
{
    [Serializable] public sealed class Channel {
        public string name,contentSha256,textureIdentity; public int width,height,format,mips,rawBytes;
        public string[] mipSha256;
    }
    [Serializable] public sealed class Probe {
        public string key,channel;public int mip;public float[] maximumDifferenceByCoordinate;
    }
    [Serializable] public sealed class Source {
        public string cid,descriptorSha256;public int samplerCount,triangleCount,rectangleCount;public Channel[] channels;
    }
    [Serializable] public sealed class Report {
        public string status,inventorySha256,scope;public Source[] sources;public Probe[] probes;
        public int textureOutputs,encodedTextures;public float[] coordinates;
    }
    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
    static void Need(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string game=Arg("-gameData"),inventoryPath=Arg("-structureInventory"),output=Arg("-sourceReport");
        var inv=JsonUtility.FromJson<PrepareStructure.Inventory>(File.ReadAllText(inventoryPath));
        var units=inv.units.Where(u=>u.style=="Lift02").ToArray();Need(units.Length>0,"No Lift02 source samplers.");
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();
        var sources=new List<Source>();var probes=new List<Probe>();var module=PreparePaint.OpenCodec(game);
        var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift02SourceSamplingProbe.compute");
        Need(shader!=null,"Missing offline source probe.");int kernel=shader.FindKernel("Probe");
        var coordinates=new[]{new Vector2(.25f,.25f),new Vector2(.75f,.75f),new Vector2(1.25f,.25f),new Vector2(-.25f,.25f),new Vector2(.001f,.5f),new Vector2(.999f,.5f)};
        try {
            foreach(string family in new[]{"Game","BridgesAndPorts"})
            foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))){
                var zip=ZipFile.OpenRead(path);archives.Add(zip);
                foreach(var e in zip.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var reader=new StreamReader(e.Open());string cid=reader.ReadToEnd().Trim();var payload=zip.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));if(payload==null)continue;
                    if(entries.TryGetValue(cid,out var prior))Need(Bytes(prior).SequenceEqual(Bytes(payload)),"Conflicting native source CID.");entries[cid]=payload;}
            }
            byte[] Read(string cid)=>entries.TryGetValue(cid,out var e)?Bytes(e):throw new FileNotFoundException(cid);
            var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
            using var inputBuffer=new ComputeBuffer(coordinates.Length,8);inputBuffer.SetData(coordinates);
            using var outputBuffer=new ComputeBuffer(coordinates.Length*2,16);
            shader.SetBuffer(kernel,"Coordinates",inputBuffer);shader.SetBuffer(kernel,"Samples",outputBuffer);
            foreach(var group in units.GroupBy(u=>u.sourceCid)){
                byte[] descriptor=Read(group.Key);string digest=PreparePaint.Digest(descriptor);
                Need(group.All(u=>u.sourceDigest==digest),"Source descriptor digest differs.");
                using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,
                    new object[]{descriptor,inv.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
                var channels=new List<Channel>();
                var representative=group.Where(u=>u.triangle).OrderByDescending(u=>Math.Abs((u.b[0]-u.a[0])*(u.c[1]-u.a[1])-(u.b[1]-u.a[1])*(u.c[0]-u.a[0]))).FirstOrDefault();
                foreach(var pair in textures.OrderBy(p=>p.Key)){
                    var texture=pair.Value;var raw=texture.GetRawTextureData<byte>().ToArray();
                    var mipHashes=new List<string>();for(int m=0;m<texture.mipmapCount;m++)mipHashes.Add(PreparePaint.Digest(texture.GetPixelData<byte>(m).ToArray()));
                    string content=PreparePaint.Digest(raw);
                    string identity=PreparePaint.Digest(System.Text.Encoding.UTF8.GetBytes(pair.Key+"/"+texture.graphicsFormat+"/"+texture.width+"/"+texture.height+"/"+texture.mipmapCount+"/"+content));
                    channels.Add(new Channel{name=pair.Key,contentSha256=content,textureIdentity=identity,width=texture.width,height=texture.height,format=(int)texture.graphicsFormat,mips=texture.mipmapCount,rawBytes=raw.Length,mipSha256=mipHashes.ToArray()});
                    if(representative==null)continue;
                    Vector4 V(float[] p)=>new Vector4(p[0],p[1],0,0);
                    shader.SetVector("A",V(representative.a));shader.SetVector("B",V(representative.b));shader.SetVector("C",V(representative.c));
                    shader.SetTexture(kernel,"Source",texture);shader.SetInt("IsNormal",pair.Key=="_NormalMap"?1:0);
                    for(int mip=0;mip<texture.mipmapCount;mip++){
                        shader.SetInt("Mip",mip);shader.Dispatch(kernel,1,1,1);var samples=new Vector4[coordinates.Length*2];outputBuffer.GetData(samples);
                        var differences=new float[coordinates.Length];for(int n=0;n<differences.Length;n++)for(int c=0;c<4;c++)differences[n]=Math.Max(differences[n],Math.Abs(samples[n*2][c]-samples[n*2+1][c]));
                        probes.Add(new Probe{key=representative.key,channel=pair.Key,mip=mip,maximumDifferenceByCoordinate=differences});
                    }
                }
                sources.Add(new Source{cid=group.Key,descriptorSha256=digest,samplerCount=group.Count(),triangleCount=group.Count(u=>u.triangle),rectangleCount=group.Count(u=>!u.triangle),channels=channels.ToArray()});
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonUtility.ToJson(new Report{status="PASS source decode; native affine replacement remains unsupported where probe differs",inventorySha256=PreparePaint.Digest(File.ReadAllBytes(inventoryPath)),scope="Original native source textures and every mip; no crop, texture encode or payload generation",sources=sources.ToArray(),probes=probes.ToArray(),coordinates=coordinates.SelectMany(p=>new[]{p.x,p.y}).ToArray()},true));
            Debug.Log($"PASS Lift02 source sampling audit: {sources.Count} descriptors, {sources.Sum(s=>s.channels.Length)} decoded channels, {probes.Count} mip probes; texture outputs=0; encoded textures=0.");
        } finally {foreach(var a in archives)a.Dispose();PreparePaint.CloseCodec(module);}
    }
}
