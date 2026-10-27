using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Offline evidence only: no custom shader or sampled image enters the mod.
public static class Lift04SharedSamplingAudit
{
    [Serializable] public sealed class Input { public PrepareStructure.Source[] sources; public PrepareStructure.Unit[] units; }
    [Serializable] public sealed class Result { public string key, sourceCid, channel, wrap; public bool triangle; public int mip, samples, different; public float maximumError; }
    [Serializable] public sealed class Report { public string status; public int representativeSamplers, bakedTextures; public Result[] results; }
    static byte[] Bytes(ZipArchiveEntry e) { using var s=e.Open(); using var b=new MemoryStream();s.CopyTo(b);return b.ToArray(); }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-samplingInventory")));
        var units=input.units.Where(u=>u.style=="Lift04").GroupBy(u=>(u.sourceDigest,u.triangle)).Select(g=>g.First()).ToArray();
        var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift04SharedSamplingProbe.compute");
        if(shader==null||units.Length!=4)throw new InvalidDataException("Missing Lift04 representatives or probe.");
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();
        var codec=PreparePaint.OpenCodec(Arg("-gameData"));var results=new List<Result>();
        try {
            foreach(string family in new[]{"Game","BridgesAndPorts"})foreach(string path in Directory.GetFiles(Path.Combine(Arg("-gameData"),"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))) {
                var zip=ZipFile.OpenRead(path);archives.Add(zip);
                foreach(var e in zip.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var reader=new StreamReader(e.Open());entries[reader.ReadToEnd().Trim()]=zip.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));}
            }
            byte[] Read(string cid)=>Bytes(entries[cid]);
            var points=new List<Vector2>();
            foreach(float x in new[]{-.01f,.01f,.25f,.49f,.51f,.75f,.99f,1.01f,1.25f})
                foreach(float y in new[]{.01f,.25f,.49f,.51f,.75f,.99f,1.01f})points.Add(new Vector2(x,y));
            using var coordinates=new ComputeBuffer(points.Count,8);coordinates.SetData(points);
            using var expected=new ComputeBuffer(points.Count,16);using var direct=new ComputeBuffer(points.Count,16);
            int kernel=shader.FindKernel("Probe");shader.SetInt("Count",points.Count);
            shader.SetBuffer(kernel,"Coordinates",coordinates);shader.SetBuffer(kernel,"Expected",expected);shader.SetBuffer(kernel,"Direct",direct);
            foreach(var group in units.GroupBy(u=>u.sourceDigest)) {
                var first=group.First();var bytes=Read(first.sourceCid);
                if(PreparePaint.Digest(bytes)!=first.sourceDigest)throw new InvalidDataException("Source digest changed.");
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{bytes,input.sources.Single(s=>s.cid==first.sourceCid),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
                foreach(var unit in group) {
                    shader.SetInt("Triangle",unit.triangle?1:0);
                    shader.SetVector("Bounds",new Vector4(unit.bounds[0],unit.bounds[1],unit.bounds[2],unit.bounds[3]));
                    shader.SetVector("A",new Vector4(unit.a[0],unit.a[1],0,0));shader.SetVector("B",new Vector4(unit.b[0],unit.b[1],0,0));shader.SetVector("C",new Vector4(unit.c[0],unit.c[1],0,0));
                    foreach(var pair in textures) {
                        var texture=pair.Value;texture.filterMode=FilterMode.Bilinear;shader.SetTexture(kernel,"Source",texture);shader.SetInt("Normal",pair.Key=="_NormalMap"?1:0);
                        foreach(var wrap in new[]{TextureWrapMode.Clamp,TextureWrapMode.Repeat,TextureWrapMode.Mirror}) {
                            texture.wrapMode=wrap;
                            for(int mip=0;mip<texture.mipmapCount;mip++) {
                                shader.SetInt("Mip",mip);shader.Dispatch(kernel,(points.Count+63)/64,1,1);
                                var a=new Vector4[points.Count];var b=new Vector4[points.Count];expected.GetData(a);direct.GetData(b);
                                int different=0;float maximum=0;
                                for(int i=0;i<a.Length;i++){var error=a[i]-b[i];float peak=Mathf.Max(Mathf.Abs(error.x),Mathf.Abs(error.y),Mathf.Abs(error.z),Mathf.Abs(error.w));maximum=Mathf.Max(maximum,peak);if(peak>1e-6f)different++;}
                                results.Add(new Result{key=unit.key,sourceCid=unit.sourceCid,channel=pair.Key,wrap=wrap.ToString(),triangle=unit.triangle,mip=mip,samples=points.Count,different=different,maximumError=maximum});
                            }
                        }
                    }
                }
            }
            if(results.All(r=>r.different==0))throw new InvalidDataException("Probe did not exercise a differing region.");
            File.WriteAllText(Arg("-samplingReport"),JsonUtility.ToJson(new Report{status="DIRECT_AFFINE_AND_FULL_TEXTURE_WRAP_NOT_EQUIVALENT",representativeSamplers=units.Length,bakedTextures=0,results=results.ToArray()},true));
            Debug.Log($"PASS Lift04 shared-source diagnostic: representatives={units.Length}; cases={results.Count}; differing={results.Count(r=>r.different>0)}; texture outputs=0. This excludes direct affine/full-texture wrap only; it does not prove all native representations impossible.");
        } finally {foreach(var archive in archives)archive.Dispose();PreparePaint.CloseCodec(codec);}
    }
}
