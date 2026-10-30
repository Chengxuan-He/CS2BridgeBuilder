using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Draw01FarTextureCheck {
 [DllImport("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll",CallingConvention=CallingConvention.Cdecl)] static extern long decompress_data(IntPtr s,long n,IntPtr t,long c,int f);
 static byte[] Decode(byte[] b) { int n=BitConverter.ToInt32(b,4);var s=Marshal.AllocHGlobal(b.Length-12);var t=Marshal.AllocHGlobal(n);try {Marshal.Copy(b,12,s,b.Length-12);if(decompress_data(s,b.Length-12,t,n,0)!=n)throw new Exception("Decode");var r=new byte[n];Marshal.Copy(t,r,0,n);return r;}finally {Marshal.FreeHGlobal(s);Marshal.FreeHGlobal(t);} }
 static string Cid(byte[] b,int o)=>string.Concat(b.Skip(o).Take(16).Select(x=>(x&15).ToString("x")+(x>>4).ToString("x")));
 public static void Run() {
 var a=Environment.GetCommandLineArgs();string output=a[Array.IndexOf(a,"-output")+1];Directory.CreateDirectory(output);
 var zips=new List<ZipArchive>();var index=new Dictionary<string,ZipArchiveEntry>();
 try {
 foreach(var path in Directory.GetFiles("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts","*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))) {var z=ZipFile.OpenRead(path);zips.Add(z);foreach(var e in z.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var r=new StreamReader(e.Open());var entryId=r.ReadToEnd().Trim();var v=z.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));if(v!=null)index[entryId]=v;}}
 byte[] Read(string id){using var s=index[id].Open();using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
 var surface=Read("7271f977513970fff2c709a721970e75");
 using var hash=System.Security.Cryptography.SHA256.Create();var digest=string.Concat(hash.ComputeHash(surface).Select(v=>v.ToString("x2")));
 if(digest!="eb8ccf270ed33f4a8b7b59b004d1e19b16a1f37ab27eb9c432cb877ac6c3f814")throw new Exception("Native far Surface differs from measured prototype.");
 var id=Cid(surface,83);Texture2D tex;
 if(id!=new string('0',32)){using var s=new MemoryStream(Read(id));tex=MovableVtTextureSource.ReadTexture(s,Decode);}else{using var s=new MemoryStream(Read(Cid(surface,284)));tex=MovableVtTextureSource.ReadSurface(s,Decode)[(0,0)];}
 var rt=new RenderTexture(tex.width,tex.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);Graphics.Blit(tex,rt);var old=RenderTexture.active;RenderTexture.active=rt;var result=new Texture2D(tex.width,tex.height,TextureFormat.RGBA32,false,true);result.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);result.Apply();RenderTexture.active=old;File.WriteAllBytes(Path.Combine(output,"far-basecolor.png"),result.EncodeToPNG());Debug.Log("PASS native far image "+tex.width+" x "+tex.height);
 }finally{foreach(var z in zips)z.Dispose();}
 }
}
