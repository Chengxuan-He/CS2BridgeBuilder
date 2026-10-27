using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

// Offline read-only source audit and descriptor-only candidate conversion.
public static class Draw01RepeatControlAudit
{
    [Serializable] public sealed class Input {public string recipe;public PrepareStructure.Source[] sources;public PrepareRepeat.Unit[] units;}
    [Serializable] public sealed class SourceProof {public string cid,sha256,vtSurfaceCid,controlStackCid;public string[] decodedChannels;public bool authoredControl,vtControlLayer;}
    [Serializable] public sealed class MipProof {public int mip,width,height;public long texels;public uint[] nonzero;}
    [Serializable] public sealed class TextureProof {public string path,sha256;public int width,height,format,rawBytes;public MipProof[] mips;}
    [Serializable] public sealed class Binding {public string property,cid,path,sha256;}
    [Serializable] public sealed class Change {public string key,sourceCid,oldSurfaceCid,newSurfaceCid,oldSha256,newSha256;public int offset;public Binding[] before,after;public bool onlyControlHashChanged,otherChannelsPreserved;}
    [Serializable] public sealed class Rejection {public string key,sourceCid,controlCid,reason;public bool allMipsTransparentZero;}
    [Serializable] public sealed class Report {public string status,recipe;public SourceProof[] sources;public Change[] changes;public Rejection[] rejected;public TextureProof[] verifiedTextures;public int producedTextures=0,encodedTextures=0;}
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
            var proofs=new List<SourceProof>();var changes=new List<Change>();var rejected=new List<Rejection>();
            foreach(var group in input.units.GroupBy(u=>u.sourceCid)){
                var data=Read(group.Key);var sha=PreparePaint.Digest(data);Need(group.All(u=>u.sourceDigest==sha),"Source hash differs.");
                Need(data[6]==1,"Expected real prototype VT material.");
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{data,input.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
                string stack=PreparePaint.HashText(data,7+76+3*16),vt=data[283]==255?PreparePaint.HashText(data,284):null;
                bool vtControl=vt!=null && Read(vt)[106+3*48]!=0;
                var proof=new SourceProof{cid=group.Key,sha256=sha,controlStackCid=stack,vtSurfaceCid=vt,vtControlLayer=vtControl,decodedChannels=textures.Keys.OrderBy(x=>x).ToArray(),authoredControl=textures.ContainsKey("_ControlMask")};proofs.Add(proof);
                Debug.Log("Draw01 source control proof: "+JsonUtility.ToJson(proof));
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
            var textureProofs=new Dictionary<string,TextureProof>();
            TextureProof ProveZero(string path){
                if(textureProofs.TryGetValue(path,out var known))return known;
                var payload=File.ReadAllBytes(path);var nativeTextureType=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
                var asset=FormatterServices.GetUninitializedObject(nativeTextureType);using var reader=new BinaryReader(new MemoryStream(payload));
                Need((ushort)nativeTextureType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader})==5,"Expected native texture v5.");
                int w=(int)nativeTextureType.GetProperty("width").GetValue(asset),h=(int)nativeTextureType.GetProperty("height").GetValue(asset);
                var format=(GraphicsFormat)nativeTextureType.GetProperty("format").GetValue(asset);
                var flags=TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit;
                if(payload[8]>1)flags|=TextureCreationFlags.MipChain;
                var texture=new Texture2D(w,h,format,flags);
                try{
                    int rawSize=texture.GetRawTextureData<byte>().Length,compressed=reader.ReadInt32();
                    var raw=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
                    if(compressed>=0){Need(raw.Length==compressed,"Native compressed length mismatch.");raw=PreparePaint.DecodeRaw(raw,rawSize);}
                    Need(raw.Length==rawSize,"Mip payload size mismatch.");texture.LoadRawTextureData(raw);texture.Apply(false,false);
                    Need(texture.mipmapCount==payload[8] && texture.GetRawTextureData<byte>().ToArray().SequenceEqual(raw),"Native mip upload changed.");
                    var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Draw01ControlZero.compute");int kernel=program.FindKernel("CheckZero");
                    using var counter=new ComputeBuffer(4,4);var mips=new List<MipProof>();
                    for(int mip=0;mip<texture.mipmapCount;mip++){
                        int width=Math.Max(1,w>>mip),height=Math.Max(1,h>>mip);counter.SetData(new uint[4]);
                        program.SetTexture(kernel,"Source",texture);program.SetBuffer(kernel,"Nonzero",counter);
                        program.SetInt("Mip",mip);program.SetInts("Extent",width,height);program.Dispatch(kernel,(width+7)/8,(height+7)/8,1);
                        var nonzero=new uint[4];counter.GetData(nonzero);
                        mips.Add(new MipProof{mip=mip,width=width,height=height,texels=(long)width*height,nonzero=nonzero});
                    }
                    known=new TextureProof{path=path,sha256=PreparePaint.Digest(payload),width=w,height=h,format=(int)format,rawBytes=raw.Length,mips=mips.ToArray()};textureProofs.Add(path,known);return known;
                }finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            Need(ProveZero(zero).mips.All(m=>m.nonzero.All(v=>v==0)),"Road control is not transparent zero.");
            foreach(var file in packaged){string key=Path.GetFileName(Path.GetDirectoryName(file));var unit=input.units.Single(u=>u.key==key);
                Need(unit.style=="Draw01" && !unit.rows && (unit.bands==null||unit.bands.Length==0),"Only complete native Repeat crops are eligible.");
                byte[] old=File.ReadAllBytes(file);var before=Inspect(old,out int offset);Need(offset>=0,"No control binding.");
                var control=before.Single(b=>b.property=="_ControlMask");var checkedControl=ProveZero(control.path);
                bool allZero=checkedControl.mips.All(m=>m.nonzero.All(v=>v==0));var sourceProof=proofs.Single(s=>s.cid==unit.sourceCid);
                if(sourceProof.authoredControl || sourceProof.vtControlLayer || sourceProof.controlStackCid!=new string('0',32) || !allZero){
                    rejected.Add(new Rejection{key=key,sourceCid=unit.sourceCid,controlCid=control.cid,allMipsTransparentZero=allZero,
                        reason=sourceProof.authoredControl?"Real prototype VT has an authored control layer; replacement is prohibited.":"Control payload is not transparent zero in every mip."});continue;
                }
                byte[] candidate=(byte[])old.Clone();Array.Copy(PreparePaint.HashBytes(zeroCid),0,candidate,offset,16);
                var after=Inspect(candidate,out int newOffset);Need(newOffset==offset,"Binding offset changed.");
                Need(old.Take(offset).SequenceEqual(candidate.Take(offset)) && old.Skip(offset+16).SequenceEqual(candidate.Skip(offset+16)),"Non-control bytes changed.");
                foreach(var a in before.Where(b=>b.property!="_ControlMask")){var b=after.Single(x=>x.property==a.property);Need(a.cid==b.cid&&a.sha256==b.sha256,"Non-control channel changed.");}
                string digest=PreparePaint.Digest(candidate),cid=PreparePaint.Digest(Encoding.UTF8.GetBytes(input.recipe+"/"+digest)).Substring(0,32);
                string destination=Path.Combine(output,"Repeat",key,"Repeat.Surface");Need(!File.Exists(destination)&&!File.Exists(destination+".cid"),"Do not overwrite an existing candidate.");Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination,candidate);File.WriteAllText(destination+".cid",cid,new UTF8Encoding(false));
                Inspect(File.ReadAllBytes(destination),out int diskOffset);Need(diskOffset==offset,"Native disk readback differs.");
                changes.Add(new Change{key=key,sourceCid=unit.sourceCid,oldSurfaceCid=File.ReadAllText(file+".cid").Trim(),newSurfaceCid=cid,oldSha256=PreparePaint.Digest(old),newSha256=digest,offset=offset,before=before,after=after,onlyControlHashChanged=true,otherChannelsPreserved=true});
            }
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(new Report{status=rejected.Count>0?"REJECTED_INELIGIBLE_REPLACEMENTS":"PASS",recipe=input.recipe,sources=proofs.ToArray(),changes=changes.ToArray(),rejected=rejected.ToArray(),verifiedTextures=textureProofs.Values.ToArray()},true));
            Debug.Log($"PASS repeat control source audit: sources={proofs.Count}; candidateSurfaces={changes.Count}; rejected={rejected.Count}; texture outputs=0; encoding=0; non-control fields unchanged.");
        } finally {foreach(var a in archives)a.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
    }
    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
}
