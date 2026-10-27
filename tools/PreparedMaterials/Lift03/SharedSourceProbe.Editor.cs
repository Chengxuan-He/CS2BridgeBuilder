using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Diagnostic sampling only. No native asset encoder or output texture creation.
public static class Lift03SharedSourceProbe
{
    [Serializable] public sealed class Row {public string key,sourceCid,channel;public bool triangle;public float[] q,required,directAffine;public float maxError;}
    [Serializable] public sealed class Report {public string status,scope,unity,gpu;public int cases,mismatches;public Row[] results;}
    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        var inventory=JsonUtility.FromJson<PrepareStructure.Inventory>(File.ReadAllText(Arg("-structureInventory")));
        var selected=inventory.units.Where(u=>u.style=="Lift03").ToArray();
        var representative=new[]{
            selected.Single(u=>u.key=="014b89ccbffa91b27c3d1171ebf1abeb6fd62fc09214d33efbcb93d680236b21"),
            selected.Single(u=>u.key=="6de9805cfb94023fe7847fc44fe145368285e4ba81483c6968f85950610d8fa6"),
            selected.First(u=>u.sourceCid=="5402741b305d5f85e66a54863f585b84"&&u.triangle)};
        string game=Arg("-gameData");var module=PreparePaint.OpenCodec(game);var archives=new List<ZipArchive>();
        var entries=new Dictionary<string,ZipArchiveEntry>();var rows=new List<Row>();
        try {
            foreach(var family in new[]{"Game","BridgesAndPorts"})foreach(var path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))){
                var z=ZipFile.OpenRead(path);archives.Add(z);
                foreach(var e in z.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var r=new StreamReader(e.Open());var cid=r.ReadToEnd().Trim();var payload=z.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));if(payload!=null)entries[cid]=payload;}
            }
            byte[] Read(string cid)=>Bytes(entries[cid]);
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lift03SharedSourceProbe.compute");int kernel=shader.FindKernel("Sample");
            var cases=new[]{new Vector2(.2f,.2f),new Vector2(.8f,.8f),new Vector2(1.2f,.2f),new Vector2(-.2f,.3f),new Vector2(.9999f,.3f),new Vector2(1.0001f,.3f),new Vector2(.2f,2.4f),new Vector2(2.2f,2.8f)};
            using var input=new ComputeBuffer(cases.Length,8);input.SetData(cases);using var output=new ComputeBuffer(cases.Length*2,16);
            shader.SetBuffer(kernel,"Cases",input);shader.SetBuffer(kernel,"Results",output);shader.SetInt("Count",cases.Length);
            var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var u in representative){
                var bytes=Read(u.sourceCid);if(PreparePaint.Digest(bytes)!=u.sourceDigest)throw new InvalidDataException("Prototype source changed.");
                using var source=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{bytes,inventory.sources.Single(s=>s.cid==u.sourceCid),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",flags).GetValue(source);
                shader.SetInt("Triangle",u.triangle?1:0);shader.SetVector("Bounds",new Vector4(u.bounds[0],u.bounds[1],u.bounds[2],u.bounds[3]));
                Vector4 V(float[] v)=>v==null||v.Length<2?Vector4.zero:new Vector4(v[0],v[1],0,0);
                shader.SetVector("A",V(u.a));shader.SetVector("B",V(u.b));shader.SetVector("C",V(u.c));
                foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap"}){
                    var texture=textures[name];texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Bilinear;
                    shader.SetTexture(kernel,"Source",texture);shader.SetInt("Normal",name=="_NormalMap"?1:0);shader.Dispatch(kernel,1,1,1);
                    var values=new Vector4[cases.Length*2];output.GetData(values);
                    for(int i=0;i<cases.Length;i++){
                        float[] Array(Vector4 v)=>new[]{v.x,v.y,v.z,v.w};float error=0;
                        for(int c=0;c<4;c++)error=Mathf.Max(error,Mathf.Abs(values[i*2][c]-values[i*2+1][c]));
                        rows.Add(new Row{key=u.key,sourceCid=u.sourceCid,channel=name,triangle=u.triangle,q=new[]{cases[i].x,cases[i].y},required=Array(values[i*2]),directAffine=Array(values[i*2+1]),maxError=error});
                    }
                }
            }
            var report=new Report{status="PASS",scope="Native GPU texture sampling counterexample; mip 0 only; not game shader validation",unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceVersion,cases=rows.Count,mismatches=rows.Count(r=>r.maxError!=0),results=rows.ToArray()};
            if(report.mismatches==0)throw new InvalidOperationException("Representative samples did not demonstrate the capability gap.");
            File.WriteAllText(Arg("-probeReport"),JsonUtility.ToJson(report,true));Debug.Log("PASS Lift03 shared-source probe: "+report.cases+" samples, "+report.mismatches+" direct-affine mismatches; no texture output or encoding.");
        }finally{foreach(var z in archives)z.Dispose();PreparePaint.CloseCodec(module);}
    }
}
