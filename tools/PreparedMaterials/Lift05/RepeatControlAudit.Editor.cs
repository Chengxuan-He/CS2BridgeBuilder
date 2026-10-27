using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Read-only native source/mip audit. Writes descriptor-only candidates outside
// assets; no rendering into new material products and no texture encoding.
public static class Lift05RepeatControlAudit
{
    [Serializable] public sealed class Input { public string recipe; public PrepareStructure.Source[] sources; public PrepareRepeat.Unit[] units; }
    [Serializable] public sealed class SourceProof { public string cid, sha256, vtSurfaceCid, controlStackCid; public string[] decodedChannels; public bool authoredControl; }
    [Serializable] public sealed class Binding { public string property, cid, path, sha256; }
    [Serializable] public sealed class MipProof { public int level,width,height; public long pixels; public bool transparentZero; }
    [Serializable] public sealed class TextureProof { public string cid,path,sha256,format; public int width,height; public long bytes,decodedPixels; public MipProof[] mips; }
    [Serializable] public sealed class Change { public string key,sourceCid,oldSurfaceCid,newSurfaceCid,oldSha256,newSha256; public int offset; public Binding[] before,after; public bool onlyControlHashChanged,otherChannelsPreserved,nativeDiskReadback; }
    [Serializable] public sealed class Report { public string status,recipe; public SourceProof[] sources; public TextureProof[] textures; public Change[] changes; public string[] removableControlCids,retainedControlCids; public long oldUniqueControlBytes,existingZeroBytes,potentialPayloadAndSidecarSaving; public bool readbackPositiveControl; public int producedTextures=0,encodedTextures=0; }
    static void Need(bool value,string message) { if(!value)throw new InvalidDataException(message); }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs(); string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string game=Arg("-gameData"),managed=Path.Combine(game,"Managed"),style=Arg("-styleRoot"),output=Arg("-candidateOutput");
        var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-repeatInventory")));
        Need(input.recipe==PrepareRepeat.Recipe,"Unsupported recipe.");
        var keyMethod=typeof(PrepareRepeat).GetMethod("Key",BindingFlags.NonPublic|BindingFlags.Static);
        Need(keyMethod!=null,"Missing authoring key implementation.");
        foreach(var unit in input.units) {
            if(unit.bands!=null&&unit.bands.Length==0)unit.bands=null;
            string computed=(string)keyMethod.Invoke(null,new object[]{unit,true});
            Need(string.IsNullOrEmpty(unit.key)||unit.key==computed,"Inventory recipe key differs.");
            unit.key=computed;
        }
        Need(input.units.All(u=>u.style=="Lift05" && (u.bands==null||u.bands.Length==0)),"Only complete Lift05 repeat units are eligible.");
        Need(!Directory.Exists(output),"Candidate output must be new."); Directory.CreateDirectory(output);
        var archives=new List<ZipArchive>(); var entries=new Dictionary<string,ZipArchiveEntry>(); var module=PreparePaint.OpenCodec(game);
        ResolveEventHandler resolve=(_,e)=>{ string path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null; };
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try
        {
            foreach(string family in new[]{"Game","BridgesAndPorts"})
            foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT")))
            {
                var archive=ZipFile.OpenRead(path);archives.Add(archive);
                foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".cid")))
                {
                    using var reader=new StreamReader(entry.Open());string cid=reader.ReadToEnd().Trim();
                    var payload=archive.GetEntry(entry.FullName.Substring(0,entry.FullName.Length-4));if(payload==null)continue;
                    if(entries.TryGetValue(cid,out var previous))Need(Bytes(previous).SequenceEqual(Bytes(payload)),"Conflicting native source CID.");
                    entries[cid]=payload;
                }
            }
            byte[] Read(string cid)=>entries.TryGetValue(cid,out var entry)?Bytes(entry):throw new FileNotFoundException("Missing native source "+cid);
            var sources=new List<SourceProof>();
            foreach(var group in input.units.GroupBy(u=>u.sourceCid))
            {
                byte[] bytes=Read(group.Key); string digest=PreparePaint.Digest(bytes);
                Need(group.All(u=>u.sourceDigest==digest),"Measured Lift05 source digest differs.");
                Need(bytes[6]==1,"Expected measured native VT material.");
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,
                    new object[]{bytes,input.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var channels=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
                sources.Add(new SourceProof{cid=group.Key,sha256=digest,controlStackCid=PreparePaint.HashText(bytes,7+76+3*16),
                    vtSurfaceCid=bytes[283]==255?PreparePaint.HashText(bytes,284):null,decodedChannels=channels.Keys.OrderBy(x=>x).ToArray(),authoredControl=channels.ContainsKey("_ControlMask")});
            }
            File.WriteAllText(Path.Combine(output,"source-audit.json"),JsonUtility.ToJson(new Report{status="SOURCE_CHECKED",recipe=input.recipe,sources=sources.ToArray()},true));
            Need(sources.All(s=>!s.authoredControl),"A real prototype has authored control; no replacement permitted.");

            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var nativeSurface=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var nativeTexture=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
            var decompress=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods()
                .Single(m=>m.Name=="Decompress" && m.GetParameters()[0].ParameterType==typeof(byte[]));
            object zstd=Enum.Parse(decompress.GetParameters()[1].ParameterType,"ZSTD");
            var resources=Directory.GetFiles(style,"*.Texture",SearchOption.AllDirectories)
                .GroupBy(p=>File.ReadAllText(p+".cid").Trim()).ToDictionary(g=>g.Key,g=>g.First());
            Binding[] Inspect(byte[] bytes,out int controlOffset)
            {
                controlOffset=-1;using var reader=new BinaryReader(new MemoryStream(bytes));var asset=FormatterServices.GetUninitializedObject(nativeSurface);
                nativeSurface.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
                Need(!(bool)nativeSurface.GetProperty("isVTMaterial").GetValue(asset),"Repeat Surface must be non-VT.");
                foreach(string method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
                Need(reader.ReadByte()==255,"Missing native texture dictionary.");int count=reader.ReadInt32();var result=new List<Binding>();
                for(int i=0;i<count;i++)
                {
                    string name=reader.ReadString();Need(reader.ReadByte()==255,"Null native texture binding.");int offset=(int)reader.BaseStream.Position;
                    string cid=PreparePaint.HashText(reader.ReadBytes(16),0);if(name=="_ControlMask")controlOffset=offset;
                    Need(resources.TryGetValue(cid,out var file),"Missing style-local texture "+cid);
                    result.Add(new Binding{property=name,cid=cid,path=file,sha256=PreparePaint.Digest(File.ReadAllBytes(file))});
                }
                binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader});Need(reader.BaseStream.Position==bytes.Length,"Unconsumed native Surface bytes.");
                Need(result.Select(b=>b.property).OrderBy(x=>x).SequenceEqual(new[]{"_BaseColorMap","_ControlMask","_MaskMap","_NormalMap"}),"Unexpected repeat channels.");
                return result.ToArray();
            }
            var proofs=new Dictionary<string,TextureProof>();
            TextureProof VerifyZero(string cid)
            {
                if(proofs.TryGetValue(cid,out var known))return known;
                string file=resources[cid];var asset=FormatterServices.GetUninitializedObject(nativeTexture);
                using var reader=new BinaryReader(File.OpenRead(file));var version=nativeTexture.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
                Need((ushort)version==5,"Unsupported native texture version.");
                int width=(int)nativeTexture.GetProperty("width").GetValue(asset),height=(int)nativeTexture.GetProperty("height").GetValue(asset);
                var format=(GraphicsFormat)nativeTexture.GetProperty("format").GetValue(asset);
                Need(!GraphicsFormatUtility.IsSRGBFormat(format),"Control must retain linear channel semantics.");
                int compressed=reader.ReadInt32();var payload=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
                if(compressed!=-1){Need(compressed==payload.Length,"Invalid native compression length.");payload=(byte[])decompress.Invoke(null,new[]{(object)payload,zstd});}
                int mipCount=1+(int)Math.Floor(Math.Log(Math.Max(width,height),2));long footprint=0;
                for(int mip=0;mip<mipCount;mip++)footprint+=GraphicsFormatUtility.ComputeMipmapSize(Math.Max(1,width>>mip),Math.Max(1,height>>mip),format);
                Need(footprint==payload.LongLength,"Incomplete native mip chain.");
                var flags=TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit;if(mipCount>1)flags|=TextureCreationFlags.MipChain;
                var texture=new Texture2D(width,height,format,flags){filterMode=FilterMode.Point};var mips=new List<MipProof>();long pixels=0;
                var shader=Shader.Find("Hidden/Lift05/ControlMipRead");Need(shader!=null&&shader.isSupported,"Offline explicit-mip read shader unavailable.");
                var sampler=new Material(shader);
                try
                {
                    // A known nonzero input detects an unbound sampler or broken
                    // readback path before accepting all-zero material channels.
                    var positive=RenderTexture.GetTemporary(1,1,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                    var knownPixel=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);var previousTarget=RenderTexture.active;
                    try
                    {
                        sampler.SetFloat("_Mip",0);Graphics.Blit(Texture2D.whiteTexture,positive,sampler);RenderTexture.active=positive;
                        knownPixel.ReadPixels(new Rect(0,0,1,1),0,0,false);knownPixel.Apply(false,false);
                        var values=knownPixel.GetRawTextureData<float>();Need(values.Length==4&&values.All(v=>v==1f),"Positive readback control failed.");
                    }
                    finally{RenderTexture.active=previousTarget;UnityEngine.Object.DestroyImmediate(knownPixel);RenderTexture.ReleaseTemporary(positive);}
                    texture.LoadRawTextureData(payload);texture.Apply(false,false);Need(texture.mipmapCount==mipCount,"Native mip count differs.");
                    Need(texture.GetRawTextureData<byte>().ToArray().SequenceEqual(payload),"Native mip bytes changed on upload.");
                    for(int mip=0;mip<mipCount;mip++)
                    {
                        int w=Math.Max(1,width>>mip),h=Math.Max(1,height>>mip);
                        var target=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                        var readback=new Texture2D(w,h,TextureFormat.RGBAFloat,false,true);var previous=RenderTexture.active;
                        try
                        {
                            sampler.SetFloat("_Mip",mip);Graphics.Blit(texture,target,sampler);
                            RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,w,h),0,0,false);readback.Apply(false,false);
                            var values=readback.GetRawTextureData<float>();for(int i=0;i<values.Length;i++)Need(values[i]==0f,"Nonzero control component at mip "+mip+", component "+i+": "+values[i]);
                            pixels+=(long)w*h;mips.Add(new MipProof{level=mip,width=w,height=h,pixels=(long)w*h,transparentZero=true});
                        }
                        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(readback);RenderTexture.ReleaseTemporary(target);}
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(sampler);UnityEngine.Object.DestroyImmediate(texture);}
                var proof=new TextureProof{cid=cid,path=file,sha256=PreparePaint.Digest(File.ReadAllBytes(file)),format=format.ToString(),width=width,height=height,bytes=new FileInfo(file).Length,decodedPixels=pixels,mips=mips.ToArray()};
                proofs.Add(cid,proof);return proof;
            }
            var packaged=Directory.GetFiles(Path.Combine(style,"Repeat"),"Repeat.Surface",SearchOption.AllDirectories);Need(packaged.Length==input.units.Length,"Unaccounted Lift05 Repeat Surface.");
            string zeroFile=Path.Combine(style,"Road","ZeroControl.Texture"),zeroCid=File.ReadAllText(zeroFile+".cid").Trim();
            byte[] zeroBytes=File.ReadAllBytes(zeroFile);
            foreach(string alias in Directory.GetFiles(style,"*.Texture",SearchOption.AllDirectories).Where(p=>File.ReadAllText(p+".cid").Trim()==zeroCid))
                Need(File.ReadAllBytes(alias).SequenceEqual(zeroBytes),"Conflicting style-local zero CID.");
            resources[zeroCid]=zeroFile;var zero=VerifyZero(zeroCid);
            var changes=new List<Change>();var candidates=new List<(string Path,byte[] Bytes,string Cid)>();var oldControls=new HashSet<string>();
            foreach(string file in packaged)
            {
                string key=Path.GetFileName(Path.GetDirectoryName(file));var unit=input.units.Single(u=>u.key==key);
                byte[] old=File.ReadAllBytes(file);var before=Inspect(old,out int offset);Need(offset>=0,"Missing control binding.");
                string oldCid=before.Single(b=>b.property=="_ControlMask").cid;VerifyZero(oldCid);oldControls.Add(oldCid);
                byte[] candidate=(byte[])old.Clone();Array.Copy(PreparePaint.HashBytes(zeroCid),0,candidate,offset,16);
                var after=Inspect(candidate,out int nextOffset);Need(nextOffset==offset,"Control field moved.");
                Need(old.Take(offset).SequenceEqual(candidate.Take(offset))&&old.Skip(offset+16).SequenceEqual(candidate.Skip(offset+16)),"Non-control Surface bytes changed.");
                foreach(var a in before.Where(b=>b.property!="_ControlMask")){var b=after.Single(x=>x.property==a.property);Need(a.cid==b.cid&&a.sha256==b.sha256,"Non-control texture changed.");}
                string digest=PreparePaint.Digest(candidate),cid=PreparePaint.Digest(Encoding.UTF8.GetBytes(input.recipe+"/"+digest)).Substring(0,32);
                candidates.Add((Path.Combine(output,"Repeat",key,"Repeat.Surface"),candidate,cid));
                changes.Add(new Change{key=key,sourceCid=unit.sourceCid,oldSurfaceCid=File.ReadAllText(file+".cid").Trim(),newSurfaceCid=cid,oldSha256=PreparePaint.Digest(old),newSha256=digest,offset=offset,before=before,after=after,onlyControlHashChanged=true,otherChannelsPreserved=true});
            }
            // Write only after all real-source, descriptor and mip proofs pass.
            foreach(var candidate in candidates)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(candidate.Path));File.WriteAllBytes(candidate.Path,candidate.Bytes);File.WriteAllText(candidate.Path+".cid",candidate.Cid,new UTF8Encoding(false));
                var bindings=Inspect(File.ReadAllBytes(candidate.Path),out _);Need(bindings.Single(b=>b.property=="_ControlMask").cid==zeroCid,"Native disk readback differs.");
                changes.Single(c=>c.newSurfaceCid==candidate.Cid).nativeDiskReadback=true;
            }
            var retained=new List<string>();var removable=new List<string>();long saving=0;
            foreach(string cid in oldControls.Where(c=>c!=zeroCid))
            {
                byte[] needle=PreparePaint.HashBytes(cid);bool used=Directory.GetFiles(style,"*.Surface",SearchOption.AllDirectories).Except(packaged)
                    .Any(path=>Contains(File.ReadAllBytes(path),needle));
                if(used)retained.Add(cid);else{removable.Add(cid);saving+=new FileInfo(resources[cid]).Length+new FileInfo(resources[cid]+".cid").Length;}
            }
            var report=new Report{status="PASS",recipe=input.recipe,sources=sources.ToArray(),textures=proofs.Values.ToArray(),changes=changes.ToArray(),
                removableControlCids=removable.ToArray(),retainedControlCids=retained.ToArray(),oldUniqueControlBytes=oldControls.Sum(c=>proofs[c].bytes),existingZeroBytes=zero.bytes,potentialPayloadAndSidecarSaving=saving,readbackPositiveControl=true};
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"PASS Lift05 repeat control audit: {sources.Count} real source(s), {changes.Count} descriptor candidates, {proofs.Count} native control textures, {proofs.Values.Sum(p=>p.mips.Length)} mips / {proofs.Values.Sum(p=>p.decodedPixels)} pixels all RGBA=0; removable bytes including CID={saving}; no texture output/encode.");
        }
        finally{foreach(var archive in archives)archive.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
    }
    static byte[] Bytes(ZipArchiveEntry entry){using var stream=entry.Open();using var memory=new MemoryStream();stream.CopyTo(memory);return memory.ToArray();}
    static bool Contains(byte[] bytes,byte[] needle){for(int i=0;i<=bytes.Length-needle.Length;i++){bool same=true;for(int j=0;j<needle.Length;j++)if(bytes[i+j]!=needle[j]){same=false;break;}if(same)return true;}return false;}
}
