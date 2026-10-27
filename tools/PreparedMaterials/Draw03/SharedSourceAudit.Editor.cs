using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Offline only: decode original channels once, retain per-use sampler metadata,
// and test hardware sampling. No material/texture encoding or production writes.
public static class Draw03SharedSourceAudit
{
 [Serializable] public class Channel { public string property,contentKey,rawSha256,format,filter,wrapU,wrapV; public int width,height,mips,aniso; public long rawBytes; public string[] mipSha256; }
 [Serializable] public class Source { public string cid,digest; public int template,uses,triangleUses; public Channel[] channels; }
 [Serializable] public class Sample { public string key,kind,channel; public int mip,comparisons; public float maxAffineError,maxLocalWrapDifference; }
 [Serializable] public class Report { public string status,scope; public Source[] sources; public Sample[] samples; public int encodedTextures; }
 static byte[] Bytes(ZipArchiveEntry e) { using var a=e.Open();using var b=new MemoryStream();a.CopyTo(b);return b.ToArray(); }
 static void Need(bool yes,string message){if(!yes)throw new InvalidDataException(message);}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  string game=Arg("-gameData"),output=Arg("-auditOutput");
  var inventory=JsonUtility.FromJson<PrepareStructure.Inventory>(File.ReadAllText(Arg("-structureInventory")));
  var units=inventory.units.Where(u=>u.style=="Draw03").ToArray();
  var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();
  var module=PreparePaint.OpenCodec(game);
  try {
   foreach(string family in new[]{"Game","BridgesAndPorts"})
   foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))) {
    var archive=ZipFile.OpenRead(path);archives.Add(archive);
    foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".cid"))) {
     using var reader=new StreamReader(entry.Open());string cid=reader.ReadToEnd().Trim();
     var payload=archive.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));if(payload==null)continue;
     if(entries.TryGetValue(cid,out var previous))Need(Bytes(previous).SequenceEqual(Bytes(payload)),"Native CID conflict");
     entries[cid]=payload;
    }
   }
   byte[] Read(string cid)=>Bytes(entries[cid]);
   var sources=new List<Source>();var tests=new List<Sample>();
   var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
   var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/SharedSourceProbe.compute");Need(shader!=null,"Missing offline probe");
   foreach(var group in units.GroupBy(u=>u.sourceCid)) {
    byte[] bytes=Read(group.Key);Need(group.All(u=>u.sourceDigest==PreparePaint.Digest(bytes)),"Source digest mismatch");
    var desc=inventory.sources.Single(s=>s.cid==group.Key);
    using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{bytes,desc,(Func<string,byte[]>)Read},null);
    var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
    var channels=new List<Channel>();
    foreach(var item in textures) {
     var t=item.Value;byte[] raw=t.GetRawTextureData<byte>().ToArray();string hash=PreparePaint.Digest(raw);
     string key=PreparePaint.Digest(System.Text.Encoding.UTF8.GetBytes(hash+"/"+t.width+"/"+t.height+"/"+t.mipmapCount+"/"+(int)t.graphicsFormat));
     channels.Add(new Channel{property=item.Key,contentKey=key,rawSha256=hash,format=t.graphicsFormat.ToString(),filter=t.filterMode.ToString(),wrapU=t.wrapModeU.ToString(),wrapV=t.wrapModeV.ToString(),aniso=t.anisoLevel,width=t.width,height=t.height,mips=t.mipmapCount,rawBytes=raw.Length,mipSha256=Enumerable.Range(0,t.mipmapCount).Select(m=>PreparePaint.Digest(t.GetPixelData<byte>(m).ToArray())).ToArray()});
     if(group.Key=="792a855e9d0c5c34d2c41d48b062139d") {
      foreach(bool triangle in new[]{false,true}) {
       var u=group.Where(x=>x.triangle==triangle).OrderByDescending(x=>triangle?Math.Abs((x.b[0]-x.a[0])*(x.c[1]-x.a[1])-(x.c[0]-x.a[0])*(x.b[1]-x.a[1])):(x.bounds[2]-x.bounds[0])*(x.bounds[3]-x.bounds[1])).First();
       tests.AddRange(Test(shader,t,u,item.Key));
      }
     }
    }
    sources.Add(new Source{cid=group.Key,digest=group.First().sourceDigest,template=desc.template,uses=group.Count(),triangleUses=group.Count(u=>u.triangle),channels=channels.ToArray()});
   }
   Need(tests.All(t=>t.maxAffineError<=0.005f),"Shared-source affine GPU precision failed");
   Need(tests.Any(t=>t.maxLocalWrapDifference>0.01f),"No observed local-wrap counterexample");
   Directory.CreateDirectory(Path.GetDirectoryName(output));
   File.WriteAllText(output,JsonUtility.ToJson(new Report{status="PASS_SOURCE_AUDIT_AND_CAPABILITY_COUNTEREXAMPLE",scope="Original native channel decoding and hardware sampling; offline probe shader, not game visual acceptance or a production shader replacement.",sources=sources.ToArray(),samples=tests.ToArray(),encodedTextures=0},true));
   Debug.Log("PASS Draw03 shared source audit: "+sources.Count+" sources; "+sources.Sum(s=>s.channels.Length)+" channel bindings; "+tests.Count+" channel/mip tests; no encoding.");
  } finally {foreach(var a in archives)a.Dispose();PreparePaint.CloseCodec(module);}
 }
 static IEnumerable<Sample> Test(ComputeShader shader,Texture2D texture,PrepareStructure.Unit u,string channel) {
  var q=new[]{new Vector2(.125f,.25f),new Vector2(.25f,.125f),new Vector2(.75f,.125f),new Vector2(.125f,.75f),new Vector2(1.125f,.25f),new Vector2(-.875f,.25f),new Vector2(.25f,1.125f),new Vector2(.875f,.75f),new Vector2(.9999f,.25f),new Vector2(1.0001f,.25f)};
  var coords=new Vector4[q.Length];var local=new Vector2[q.Length];for(int i=0;i<q.Length;i++) {
   Vector2 p=q[i],v=new Vector2(p.x-Mathf.Floor(p.x),p.y-Mathf.Floor(p.y));if(u.triangle&&v.x+v.y>1)v=Vector2.one-v;
   Vector2 Map(Vector2 x)=>u.triangle?new Vector2(u.a[0],u.a[1])+x.x*(new Vector2(u.b[0],u.b[1])-new Vector2(u.a[0],u.a[1]))+x.y*(new Vector2(u.c[0],u.c[1])-new Vector2(u.a[0],u.a[1])):new Vector2(u.bounds[0]+x.x*(u.bounds[2]-u.bounds[0]),u.bounds[1]+x.y*(u.bounds[3]-u.bounds[1]));
   var direct=Map(p);var wrapped=Map(v);coords[i]=new Vector4(direct.x,direct.y,wrapped.x,wrapped.y);local[i]=p;
  }
  using var localInput=new ComputeBuffer(q.Length,8);localInput.SetData(local);using var input=new ComputeBuffer(q.Length,16);using var output=new ComputeBuffer(q.Length*3,16);input.SetData(coords);
  int kernel=shader.FindKernel("Probe");shader.SetBuffer(kernel,"Coordinates",input);shader.SetBuffer(kernel,"Local",localInput);shader.SetVector("Origin",u.triangle?new Vector4(u.a[0],u.a[1]):new Vector4(u.bounds[0],u.bounds[1]));shader.SetVector("BasisX",u.triangle?new Vector4(u.b[0]-u.a[0],u.b[1]-u.a[1]):new Vector4(u.bounds[2]-u.bounds[0],0));shader.SetVector("BasisY",u.triangle?new Vector4(u.c[0]-u.a[0],u.c[1]-u.a[1]):new Vector4(0,u.bounds[3]-u.bounds[1]));shader.SetBuffer(kernel,"Samples",output);shader.SetTexture(kernel,"Source",texture);shader.SetInt("Count",q.Length);
  var results=new List<Sample>();
  for(int mip=0;mip<texture.mipmapCount;mip++) {
   shader.SetInt("Level",mip);shader.Dispatch(kernel,1,1,1);var values=new Vector4[q.Length*3];output.GetData(values);float same=0,diff=0;
   for(int i=0;i<q.Length;i++){same=Mathf.Max(same,Distance(values[i*3],values[i*3+1]));diff=Mathf.Max(diff,Distance(values[i*3],values[i*3+2]));}
   results.Add(new Sample{key=u.key,kind=u.triangle?"triangle-fold":"rectangle-repeat",channel=channel,mip=mip,comparisons=q.Length,maxAffineError=same,maxLocalWrapDifference=diff});
  }
  return results;
 }
 static float Distance(Vector4 a,Vector4 b)=>Mathf.Max(Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y),Mathf.Abs(a.z-b.z),Mathf.Abs(a.w-b.w));
}
