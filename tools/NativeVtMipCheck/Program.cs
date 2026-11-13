using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BridgeBuilder.Bridges;
unsafe class Program
{
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate long Decode(void* source,long count,void* target,long size,int codec);
 static void Main(string[] args)
 {
  var library=NativeLibrary.Load(args[1]);var decoder=Marshal.GetDelegateForFunctionPointer<Decode>(NativeLibrary.GetExport(library,"decompress_data"));
  byte[] Decompress(byte[] data){int size=BitConverter.ToInt32(data,4),count=BitConverter.ToInt32(data,8);if(BitConverter.ToInt32(data,0)!=int.MaxValue || count!=data.Length-12)throw new Exception("Compression header");var output=new byte[size];fixed(byte* s=data)fixed(byte* t=output)if(decoder(s+12,count,t,size,0)!=size)throw new Exception("Decode");return output;}
  using var zip=ZipFile.OpenRead(args[0]);byte[] Read(string name){using var s=zip.GetEntry(name)!.Open();using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
  var mid=Read(zip.Entries.Single(e=>e.FullName.EndsWith(".MidMips")).FullName);Directory.CreateDirectory(args[2]);int checks=0;
  foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".VTSurface")))
  {
   var bytes=Read(entry.FullName);string cid=string.Concat(bytes.Skip(2).Take(16).Select(b=>$"{b&15:x}{b>>4:x}"));
   using var stream=new MemoryStream(mid,false);
   if(!MovableMidMipSubset.TryCopy(stream,cid,cid,512,3,out var subset,out var error)||subset==null)throw new Exception(error??"Missing membership");
   var result=NativeVtMipPyramid.Convert(bytes,subset,Decompress);
   foreach(var pair in result){string name=Path.GetFileNameWithoutExtension(entry.FullName)+$"-{pair.Key.Stack}-{pair.Key.Layer}.Texture";File.WriteAllBytes(Path.Combine(args[2],name),pair.Value);Console.WriteLine($"{name} {pair.Value.Length} {Convert.ToHexString(SHA256.HashData(pair.Value))}");checks++;}
  }
  Console.WriteLine($"PASS {checks} native source complete BC7 mip pyramids; no filtering or recompression");
 }
}
