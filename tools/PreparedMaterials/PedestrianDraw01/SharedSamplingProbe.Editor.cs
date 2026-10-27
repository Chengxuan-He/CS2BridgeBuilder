using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Offline native-source GPU evidence only. This shader is never distributed.
public static class PedestrianSharedSamplingProbe
{
    [Serializable] public sealed class Row { public string sourceCid,channel; public int samplers,cases,differentSamples; public float maximumError; }
    [Serializable] public sealed class Report { public string status,meaning;public Row[] rows;public int encodedTextures=0; }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        var inventory=JsonUtility.FromJson<PrepareStructure.Inventory>(File.ReadAllText(Arg("-sourceInventory")));
        var units=inventory.units.Where(u=>u.style=="PedestrianDraw01").ToArray();
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try {
            foreach(string family in new[]{"Game","BridgesAndPorts"})
            foreach(string path in Directory.GetFiles(Path.Combine(Arg("-gameData"),"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))) {
                var archive=ZipFile.OpenRead(path);archives.Add(archive);
                foreach(var e in archive.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var r=new StreamReader(e.Open());entries[r.ReadToEnd().Trim()]=archive.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));}
            }
            byte[] Read(string identity){using var s=entries[identity].Open();using var bytes=new MemoryStream();s.CopyTo(bytes);return bytes.ToArray();}
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/PedestrianSharedSamplingProbe.compute");
            if(shader==null)throw new InvalidDataException("Missing diagnostic shader.");
            int kernel=shader.FindKernel("Compare");var rows=new List<Row>();
            foreach(var group in units.GroupBy(u=>u.sourceCid)) {
                var data=Read(group.Key);
                if(group.Any(u=>u.sourceDigest!=PreparePaint.Digest(data)))throw new InvalidDataException("Prototype descriptor changed.");
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var source=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{data,inventory.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
                var samples=new List<Vector4>();
                foreach(var u in group) {
                    Vector2 Map(Vector2 q)=>u.triangle?new Vector2(u.a[0]+q.x*(u.b[0]-u.a[0])+q.y*(u.c[0]-u.a[0]),u.a[1]+q.x*(u.b[1]-u.a[1])+q.y*(u.c[1]-u.a[1])):new Vector2(u.bounds[0]+q.x*(u.bounds[2]-u.bounds[0]),u.bounds[1]+q.y*(u.bounds[3]-u.bounds[1]));
                    foreach(var q in new[]{new Vector2(.2f,.3f),new Vector2(.8f,.7f),new Vector2(1.2f,.3f),new Vector2(-.2f,.3f),new Vector2(.3f,2.7f),new Vector2(.9999f,.0002f)}) {
                        var folded=new Vector2(q.x-Mathf.Floor(q.x),q.y-Mathf.Floor(q.y));
                        if(u.triangle&&folded.x+folded.y>1)folded=Vector2.one-folded;
                        var expected=Map(folded);var direct=Map(q);samples.Add(new Vector4(expected.x,expected.y,direct.x,direct.y));
                    }
                }
                using var input=new ComputeBuffer(samples.Count,16);input.SetData(samples.ToArray());using var output=new ComputeBuffer(samples.Count,16);
                shader.SetInt("Count",samples.Count);shader.SetBuffer(kernel,"Coordinates",input);shader.SetBuffer(kernel,"Differences",output);
                foreach(var channel in textures) {
                    shader.SetTexture(kernel,"Source",channel.Value);shader.Dispatch(kernel,(samples.Count+63)/64,1,1);
                    var differences=new Vector4[samples.Count];output.GetData(differences);
                    var row=new Row{sourceCid=group.Key,channel=channel.Key,samplers=group.Count(),cases=samples.Count};
                    foreach(var d in differences){float error=Mathf.Max(d.x,d.y,d.z,d.w);if(!float.IsFinite(error))throw new InvalidDataException("Nonfinite sample.");row.maximumError=Mathf.Max(row.maximumError,error);if(error>1f/255)row.differentSamples++;}
                    rows.Add(row);
                }
            }
            if(!rows.Any(r=>r.differentSamples>0))throw new InvalidDataException("Probe did not expose the regional-sampling distinction.");
            File.WriteAllText(Arg("-probeReport"),JsonUtility.ToJson(new Report{status="DIRECT_AFFINE_REPLACEMENT_REJECTED",meaning="Actual native VT source mip zero sampled through the GPU. Whole-atlas affine UVs do not implement the recorded region repeat/reflection. This does not test an implemented native regional sampler, filtered higher mips, mesh runtime reachability or in-game appearance.",rows=rows.ToArray()},true));
            Debug.Log("PASS shared-source capability probe: channels="+rows.Count+"; different="+rows.Sum(r=>r.differentSamples)+"; direct affine replacement rejected; encoded=0.");
        } finally {foreach(var archive in archives)archive.Dispose();PreparePaint.CloseCodec(module);}
    }
}
