using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Bounded representative capability check. No texture output, baking or encoding.
public static class Draw02SharedSourceProbe
{
    [Serializable] public class Input {public PrepareStructure.Source[] sources;}
    [Serializable] public class Sample {public string channel;public float qx,qy,maxError;public bool inside;public Vector4 direct,folded;}
    [Serializable] public class Report {public string status,sourceCid,sourceSha256,key,unity,device;public int nativeTemplate,sourceWidth,sourceHeight,decodedMipCount;public string[] channels;public Sample[] samples;public bool completeNativeMipMigrationVerified=false,nativeSurfaceColdLoadVerified=false;public int newTexturePayloads=0;}
    static void Need(bool x,string s){if(!x)throw new InvalidDataException(s);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string game=Arg("-gameData"),managed=Path.Combine(game,"Managed");
        var unit=JsonUtility.FromJson<PrepareStructure.Unit>(File.ReadAllText(Arg("-representative")));
        var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-inventory")));
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();var codec=PreparePaint.OpenCodec(game);
        ResolveEventHandler resolve=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try
        {
            foreach(string family in new[]{"Game","BridgesAndPorts"})foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT")))
            {
                var archive=ZipFile.OpenRead(path);archives.Add(archive);
                foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".cid")))
                {using var reader=new StreamReader(entry.Open());string cid=reader.ReadToEnd().Trim();var payload=archive.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));if(payload!=null&&!entries.ContainsKey(cid))entries.Add(cid,payload);}
            }
            byte[] Read(string cid){using var s=entries[cid].Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
            byte[] data=Read(unit.sourceCid);Need(PreparePaint.Digest(data)==unit.sourceDigest,"Source digest differs.");
            // SurfaceAsset.ReadVTHeader requires the live game AssetDatabase service.
            // This isolated probe uses the already validated native VT decoder and
            // explicitly does not claim a cold-load SurfaceAsset check.
            Need(data[6]==1,"Expected original native VT material.");int template=BitConverter.ToInt32(data,2);
            var materialType=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
            using var decoded=(IDisposable)Activator.CreateInstance(materialType,BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{data,input.sources.Single(s=>s.cid==unit.sourceCid),(Func<string,byte[]>)Read},null);
            var textures=(Dictionary<string,Texture2D>)materialType.GetField("Textures",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(decoded);
            var q=new[]{new Vector2(.125f,.125f),new Vector2(.25f,.125f),new Vector2(.125f,.5f),new Vector2(.25f,.5f),new Vector2(.8f,.8f),new Vector2(1.2f,.2f),new Vector2(-.2f,.2f),new Vector2(2.125f,3.25f)};
            using var points=new ComputeBuffer(q.Length,8);points.SetData(q);using var results=new ComputeBuffer(q.Length*2,16);
            var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Draw02SharedSourceProbe.compute");int kernel=program.FindKernel("Probe");
            program.SetBuffer(kernel,"Coordinates",points);program.SetBuffer(kernel,"Results",results);program.SetInt("Count",q.Length);
            Vector4 Point(float[] x)=>new Vector4(x[0],x[1],0,0);
            program.SetVector("A",Point(unit.a));program.SetVector("B",Point(unit.b));program.SetVector("C",Point(unit.c));
            var rows=new List<Sample>();
            foreach(var channel in textures)
            {
                program.SetTexture(kernel,"Source",channel.Value);program.SetInt("Normal",channel.Key=="_NormalMap"?1:0);program.Dispatch(kernel,1,1,1);
                var values=new Vector4[q.Length*2];results.GetData(values);
                for(int i=0;i<q.Length;i++){float error=0;for(int c=0;c<4;c++)error=Mathf.Max(error,Mathf.Abs(values[2*i][c]-values[2*i+1][c]));bool inside=i<4;Need(!inside||error==0,"Interior direct reference differs.");rows.Add(new Sample{channel=channel.Key,qx=q[i].x,qy=q[i].y,inside=inside,direct=values[2*i],folded=values[2*i+1],maxError=error});}
            }
            Need(rows.Any(r=>!r.inside&&r.maxError>0),"Representative failed to expose missing repeat/fold semantics.");
            var report=new Report{status="PASS_INTERIOR_REFERENCE_UNSUPPORTED_AFFINE_REPEAT",sourceCid=unit.sourceCid,sourceSha256=unit.sourceDigest,key=unit.key,unity=Application.unityVersion,device=SystemInfo.graphicsDeviceVersion,nativeTemplate=template,sourceWidth=textures.First().Value.width,sourceHeight=textures.First().Value.height,decodedMipCount=textures.First().Value.mipmapCount,channels=textures.Keys.ToArray(),samples=rows.ToArray()};
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(report,true));Debug.Log("PASS Draw02 shared source representative: native VT mip0 decoded; interior exact; direct affine lacks periodic/fold semantics; no texture outputs; Surface cold-load not verified.");
        }
        finally{foreach(var a in archives)a.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(codec);}
    }
}
