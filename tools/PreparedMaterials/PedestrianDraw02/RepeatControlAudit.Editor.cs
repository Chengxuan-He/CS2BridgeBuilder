using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

// Offline read-only source audit and descriptor-only candidate conversion.
public static class PedestrianDraw02RepeatControlAudit
{
    [Serializable] public sealed class Input {public string recipe;public PrepareStructure.Source[] sources;public PrepareRepeat.Unit[] units;}
    [Serializable] public sealed class SourceProof {public string cid,sha256,vtSurfaceCid,controlStackCid;public string[] decodedChannels;public bool authoredControl;}
    [Serializable] public sealed class Binding {public string property,cid,path,sha256;}
    [Serializable] public sealed class Change {public string key,sourceCid,oldSurfaceCid,newSurfaceCid,oldSha256,newSha256;public int offset;public Binding[] before,after;public bool onlyControlHashChanged,otherChannelsPreserved;}
    [Serializable] public sealed class Report {public string status,recipe;public SourceProof[] sources;public Change[] changes;public TextureProof[] controls;public int producedTextures=0,encodedTextures=0;}
    [Serializable] public sealed class MipProof {public int level,width,height,bytes;public string decodedSha256;public bool transparentZero;}
    [Serializable] public sealed class TextureProof {public string path,sha256,cid,format;public int width,height,mips,payloadBytes,fileBytes;public MipProof[] levels;public bool nativeHeaderRead,rawUploadUnchanged;}
    static readonly List<TextureProof> controlProofs=new List<TextureProof>();
    static void VerifyZero(string path,string managed,Assembly database)
    {
        if(controlProofs.Any(p=>p.path==path))return;
        var type=database.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
        using var reader=new BinaryReader(File.OpenRead(path));var asset=FormatterServices.GetUninitializedObject(type);
        var version=type.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});Need((ushort)version==5,"Expected native texture v5.");
        int width=(int)type.GetProperty("width").GetValue(asset),height=(int)type.GetProperty("height").GetValue(asset);
        var format=(UnityEngine.Experimental.Rendering.GraphicsFormat)type.GetProperty("format").GetValue(asset);
        int compressed=reader.ReadInt32();byte[] raw=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
        if(compressed!=-1){Need(compressed==raw.Length,"Compressed payload length differs.");var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));var decompress=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));raw=(byte[])decompress.Invoke(null,new object[]{raw,Enum.Parse(decompress.GetParameters()[1].ParameterType,"ZSTD")});}
        var flags=UnityEngine.Experimental.Rendering.TextureCreationFlags.DontInitializePixels|UnityEngine.Experimental.Rendering.TextureCreationFlags.IgnoreMipmapLimit;
        if(Math.Max(width,height)>1)flags|=UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain;
        var texture=new Texture2D(width,height,format,flags);
        try{
            texture.LoadRawTextureData(raw);texture.Apply(false,false);Need(texture.GetRawTextureData<byte>().ToArray().SequenceEqual(raw),"Native mip upload changed bytes.");
            Need(texture.mipmapCount==1+(int)Math.Floor(Math.Log(Math.Max(width,height),2)),"Incomplete mip chain.");var levels=new List<MipProof>();
            for(int mip=0;mip<texture.mipmapCount;mip++){
                int w=Math.Max(1,width>>mip),h=Math.Max(1,height>>mip);
                var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatControlReadback.compute");Need(shader!=null,"Readback shader missing.");int kernel=shader.FindKernel("ReadMip");
                using var buffer=new ComputeBuffer(w*h,16);shader.SetTexture(kernel,"Source",texture);shader.SetBuffer(kernel,"Pixels",buffer);shader.SetInt("Mip",mip);shader.SetInt("Width",w);shader.SetInt("Height",h);shader.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                var values=new float[w*h*4];buffer.GetData(values);Need(values.All(v=>v==0f),"Control mip is not transparent zero: "+path+" mip="+mip);
                byte[] pixels=new byte[values.Length*4];Buffer.BlockCopy(values,0,pixels,0,pixels.Length);
                levels.Add(new MipProof{level=mip,width=w,height=h,bytes=pixels.Length,decodedSha256=PreparePaint.Digest(pixels),transparentZero=true});
            }
            controlProofs.Add(new TextureProof{path=path,sha256=PreparePaint.Digest(File.ReadAllBytes(path)),cid=File.ReadAllText(path+".cid").Trim(),format=format.ToString(),width=width,height=height,mips=texture.mipmapCount,payloadBytes=raw.Length,fileBytes=(int)new FileInfo(path).Length,levels=levels.ToArray(),nativeHeaderRead=true,rawUploadUnchanged=true});
        }finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    static void Need(bool value,string error){if(!value)throw new InvalidDataException(error);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string game=Arg("-gameData"),managed=Path.Combine(game,"Managed"),style=Arg("-styleRoot"),output=Arg("-candidateOutput");
        var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-repeatInventory")));Need(input.recipe==PrepareRepeat.Recipe,"Wrong recipe.");
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();var module=PreparePaint.OpenCodec(game);
        ResolveEventHandler resolve=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try {
            foreach(string family in new[]{"Game","BridgesAndPorts"})foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))){
                var zip=ZipFile.OpenRead(path);archives.Add(zip);
                foreach(var e in zip.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var reader=new StreamReader(e.Open());string cid=reader.ReadToEnd().Trim();var payload=zip.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));if(payload==null)continue;
                    if(entries.TryGetValue(cid,out var prior))Need(Bytes(prior).SequenceEqual(Bytes(payload)),"Conflicting prototype CID.");entries[cid]=payload;}
            }
            byte[] Read(string cid)=>entries.TryGetValue(cid,out var e)?Bytes(e):throw new FileNotFoundException("Missing native source "+cid);
            var proofs=new List<SourceProof>();var changes=new List<Change>();
            foreach(var group in input.units.GroupBy(u=>u.sourceCid)){
                var data=Read(group.Key);var sha=PreparePaint.Digest(data);Need(group.All(u=>u.sourceDigest==sha),"Source hash differs.");
                Need(data[6]==1,"Expected real prototype VT material.");
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{data,input.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
                string stack=PreparePaint.HashText(data,7+76+3*16),vt=data[283]==255?PreparePaint.HashText(data,284):null;
                var proof=new SourceProof{cid=group.Key,sha256=sha,controlStackCid=stack,vtSurfaceCid=vt,decodedChannels=textures.Keys.OrderBy(x=>x).ToArray(),authoredControl=textures.ContainsKey("_ControlMask")};proofs.Add(proof);
                Need(!proof.authoredControl,"Authored control cannot be replaced.");
            }
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var nativeType=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var resources=Directory.GetFiles(style,"*.Texture",SearchOption.AllDirectories).GroupBy(p=>File.ReadAllText(p+".cid").Trim()).ToDictionary(g=>g.Key,g=>g.First());
            Binding[] Inspect(byte[] data,out int controlOffset){
                controlOffset=-1;using var reader=new BinaryReader(new MemoryStream(data));var asset=FormatterServices.GetUninitializedObject(nativeType);
                nativeType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
                Need(!(bool)nativeType.GetProperty("isVTMaterial").GetValue(asset),"Repeat surface must be non-VT.");
                foreach(string method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
                Need(reader.ReadByte()==255,"Missing texture dictionary.");int count=reader.ReadInt32();var list=new List<Binding>();
                for(int i=0;i<count;i++){string name=reader.ReadString();Need(reader.ReadByte()==255,"Null binding.");int offset=(int)reader.BaseStream.Position;string cid=PreparePaint.HashText(reader.ReadBytes(16),0);if(name=="_ControlMask")controlOffset=offset;
                    Need(resources.TryGetValue(cid,out var resource),"Missing local texture "+cid);list.Add(new Binding{property=name,cid=cid,path=resource,sha256=PreparePaint.Digest(File.ReadAllBytes(resource))});}
                binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader});Need(reader.BaseStream.Position==data.Length,"Unconsumed surface data.");return list.ToArray();
            }
            var packaged=Directory.GetFiles(Path.Combine(style,"Repeat"),"Repeat.Surface",SearchOption.AllDirectories);Need(packaged.Length==input.units.Length,"Unaccounted packaged Repeat material.");
            string zero=Path.Combine(style,"Road","ZeroControl.Texture"),zeroCid=File.ReadAllText(zero+".cid").Trim();
            VerifyZero(zero,managed,db);resources[zeroCid]=zero;
            foreach(var file in packaged){string key=Path.GetFileName(Path.GetDirectoryName(file));var unit=input.units.Single(u=>u.key==key);Need((unit.bands==null||unit.bands.Length==0)&&!unit.rows&&unit.max[0]>unit.min[0]&&unit.max[1]>unit.min[1],"Only complete unbanded native repeat units are audited here.");
                Need(proofs.Single(s=>s.cid==unit.sourceCid).authoredControl==false,"Source requires authored control.");
                byte[] old=File.ReadAllBytes(file);var before=Inspect(old,out int offset);Need(offset>=0,"No control binding.");
                var control=before.Single(b=>b.property=="_ControlMask");VerifyZero(control.path,managed,db);
                byte[] candidate=(byte[])old.Clone();Array.Copy(PreparePaint.HashBytes(zeroCid),0,candidate,offset,16);
                var after=Inspect(candidate,out int newOffset);Need(newOffset==offset,"Binding offset changed.");
                Need(old.Take(offset).SequenceEqual(candidate.Take(offset)) && old.Skip(offset+16).SequenceEqual(candidate.Skip(offset+16)),"Non-control bytes changed.");
                foreach(var a in before.Where(b=>b.property!="_ControlMask")){var b=after.Single(x=>x.property==a.property);Need(a.cid==b.cid&&a.sha256==b.sha256,"Non-control channel changed.");}
                string digest=PreparePaint.Digest(candidate),cid=PreparePaint.Digest(Encoding.UTF8.GetBytes(input.recipe+"/"+digest)).Substring(0,32);
                string destination=Path.Combine(output,"Repeat",key,"Repeat.Surface");Need(!File.Exists(destination)&&!File.Exists(destination+".cid"),"Do not overwrite an existing candidate.");Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination,candidate);File.WriteAllText(destination+".cid",cid,new UTF8Encoding(false));
                Need(File.ReadAllBytes(destination).SequenceEqual(candidate),"Candidate disk bytes differ.");Inspect(File.ReadAllBytes(destination),out int savedOffset);Need(savedOffset==offset&&File.ReadAllText(destination+".cid")==cid,"Candidate native readback differs.");
                changes.Add(new Change{key=key,sourceCid=unit.sourceCid,oldSurfaceCid=File.ReadAllText(file+".cid").Trim(),newSurfaceCid=cid,oldSha256=PreparePaint.Digest(old),newSha256=digest,offset=offset,before=before,after=after,onlyControlHashChanged=true,otherChannelsPreserved=true});
            }
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(new Report{status="PASS",recipe=input.recipe,sources=proofs.ToArray(),changes=changes.ToArray(),controls=controlProofs.ToArray()},true));
            Debug.Log($"PASS repeat control source audit: sources={proofs.Count}; candidateSurfaces={changes.Count}; authored controls=0; texture outputs=0; encoding=0; non-control fields unchanged.");
        } finally {foreach(var a in archives)a.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
    }
    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
}
