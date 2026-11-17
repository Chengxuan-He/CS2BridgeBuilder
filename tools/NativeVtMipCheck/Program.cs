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
  var identities=zip.Entries.Where(e=>e.FullName.EndsWith(".cid")).ToDictionary(e=>System.Text.Encoding.UTF8.GetString(Read(e.FullName)).Trim(),e=>e.FullName[..^4]);
  string Cid(byte[] data,int at)=>string.Concat(data.Skip(at).Take(16).Select(b=>$"{b&15:x}{b>>4:x}"));
  foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".Surface")))
  {
   var surface=Read(entry.FullName);if(surface[6]==0)continue;
   string cid=System.Text.Encoding.UTF8.GetString(Read(entry.FullName+".cid")).Trim();
   using var stream=new MemoryStream(mid,false);
   if(!MovableMidMipSubset.TryCopy(stream,cid,cid,512,3,out var subset,out var error)||subset==null)throw new Exception(error??"Missing membership");
   var result=new Dictionary<(int Stack,int Layer),byte[]>();
   if(surface[283]!=0)result=NativeVtMipPyramid.Convert(Read(identities[Cid(surface,284)]),subset,Decompress);
   else for(int stack=0;stack<BitConverter.ToInt32(surface,7);stack++)for(int layer=0;layer<4;layer++)
   {
    var key=Cid(surface,11+stack*136+72+layer*16);if(key==new string('0',32))continue;
    var source=Read(identities[key]);
    result.Add((stack,layer),NativeVtMipPyramid.ConvertTexture(source,subset,stack,layer,Decompress));
    // Authored mip tail must survive byte-for-byte, and missing required cache must fail.
    int tail=BitConverter.ToInt32(source,34);if(!result[(stack,layer)].TakeLast(tail).SequenceEqual(source.Skip(38).Take(tail)))throw new Exception("Mip tail changed");
    try{NativeVtMipPyramid.ConvertTexture(source,null,stack,layer,Decompress);throw new Exception("Missing cache accepted");}catch(InvalidDataException){}
   }
   int expected=0;
   if(surface[283]!=0)
   {
    var packed=Read(identities[Cid(surface,284)]);
    for(int st=0;st<BitConverter.ToInt32(packed,490);st++)for(int la=0;la<BitConverter.ToInt32(packed,26+st*40+24);la++)if(packed[106+(st*4+la)*48]!=0)expected++;
   }
   else for(int st=0;st<BitConverter.ToInt32(surface,7);st++)for(int la=0;la<4;la++)if(Cid(surface,11+st*136+72+la*16)!=new string('0',32))expected++;
   if(result.Count!=expected)throw new Exception("Missing source appearance channels");
   foreach(var pair in result){string name=Path.GetFileNameWithoutExtension(entry.FullName)+$"-{pair.Key.Stack}-{pair.Key.Layer}.Texture";File.WriteAllBytes(Path.Combine(args[2],name),pair.Value);Console.WriteLine($"{name} {pair.Value.Length} {Convert.ToHexString(SHA256.HashData(pair.Value))}");checks++;}
  }
  Console.WriteLine($"PASS {checks} native source complete BC7 mip pyramids; no filtering or recompression");
 }
}
