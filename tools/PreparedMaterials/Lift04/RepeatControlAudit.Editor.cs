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

// Offline read-only source audit and descriptor-only candidate conversion.
public static class Lift04RepeatControlAudit
{
    [Serializable] public sealed class Input {public string recipe;public PrepareStructure.Source[] sources;public PrepareRepeat.Unit[] units;}
    [Serializable] public sealed class SourceProof {public string cid,sha256,vtSurfaceCid,controlStackCid;public string[] decodedChannels;public bool authoredControl;}
    [Serializable] public sealed class Binding {public string property,cid,path,sha256;}
    [Serializable] public sealed class Change {public string key,sourceCid,oldSurfaceCid,newSurfaceCid,oldSha256,newSha256;public int offset;public Binding[] before,after;public bool onlyControlHashChanged,otherChannelsPreserved;}
    [Serializable] public sealed class MipProof {public int level,width,height;public string rgbaSha256;public int nonzeroComponents;public float[] minRgba,maxRgba;}
    [Serializable] public sealed class TextureProof {public string path,cid,sha256,format;public int width,height;public long fileBytes;public MipProof[] mips;}
    static readonly List<TextureProof> textureProofs=new List<TextureProof>();
    [Serializable] public sealed class Report {public TextureProof[] textures;public string status,recipe;public SourceProof[] sources;public Change[] changes;public int producedTextures=0,encodedTextures=0;}
    static void Need(bool value,string error){if(!value)throw new InvalidDataException(error);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string game=Arg("-gameData"),managed=Path.Combine(game,"Managed"),style=Arg("-styleRoot"),output=Arg("-candidateOutput");
        textureProofs.Clear();var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-repeatInventory")));Need(input.recipe==PrepareRepeat.Recipe,"Wrong recipe.");Need(input.units.All(u=>u.style=="Lift04" && (u.bands==null || u.bands.Length==0)),"Only complete Lift04 units without bands are eligible.");
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
            VerifyZero(zero,managed);
            foreach(var file in packaged){string key=Path.GetFileName(Path.GetDirectoryName(file));var unit=input.units.Single(u=>u.key==key);
                Need(proofs.Single(s=>s.cid==unit.sourceCid).authoredControl==false,"Source requires authored control.");
                byte[] old=File.ReadAllBytes(file);var before=Inspect(old,out int offset);Need(offset>=0,"No control binding.");
                var control=before.Single(b=>b.property=="_ControlMask");VerifyZero(control.path,managed);
                if(textureProofs.Any(t=>t.mips.Any(m=>m.nonzeroComponents!=0))){
                    File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(new Report{textures=textureProofs.ToArray(),status="INELIGIBLE_NONZERO_CONTROL",recipe=input.recipe,sources=proofs.ToArray(),changes=changes.ToArray()},true));
                    throw new InvalidDataException("A control mip is not transparent zero; candidate was not created.");
                }
                byte[] candidate=(byte[])old.Clone();Array.Copy(PreparePaint.HashBytes(zeroCid),0,candidate,offset,16);
                var after=Inspect(candidate,out int newOffset);Need(newOffset==offset,"Binding offset changed.");
                Need(old.Take(offset).SequenceEqual(candidate.Take(offset)) && old.Skip(offset+16).SequenceEqual(candidate.Skip(offset+16)),"Non-control bytes changed.");
                foreach(var a in before.Where(b=>b.property!="_ControlMask")){var b=after.Single(x=>x.property==a.property);Need(a.cid==b.cid&&a.sha256==b.sha256,"Non-control channel changed.");}
                string digest=PreparePaint.Digest(candidate),cid=PreparePaint.Digest(Encoding.UTF8.GetBytes(input.recipe+"/"+digest)).Substring(0,32);
                string destination=Path.Combine(output,"Repeat",key,"Repeat.Surface");Need(!File.Exists(destination)&&!File.Exists(destination+".cid"),"Do not overwrite an existing candidate.");Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination,candidate);File.WriteAllText(destination+".cid",cid,new UTF8Encoding(false));
                var persisted=Inspect(File.ReadAllBytes(destination),out int persistedOffset);Need(persistedOffset==offset && persisted.Single(b=>b.property=="_ControlMask").cid==zeroCid,"Candidate native readback mismatch.");
                changes.Add(new Change{key=key,sourceCid=unit.sourceCid,oldSurfaceCid=File.ReadAllText(file+".cid").Trim(),newSurfaceCid=cid,oldSha256=PreparePaint.Digest(old),newSha256=digest,offset=offset,before=before,after=after,onlyControlHashChanged=true,otherChannelsPreserved=true});
            }
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(new Report{textures=textureProofs.ToArray(),status="PASS",recipe=input.recipe,sources=proofs.ToArray(),changes=changes.ToArray()},true));
            Debug.Log($"PASS repeat control source audit: sources={proofs.Count}; candidateSurfaces={changes.Count}; authored controls=0; texture outputs=0; encoding=0; non-control fields unchanged.");
        } finally {foreach(var a in archives)a.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
    }

    static void VerifyZero(string path,string managed)
    {
        if(textureProofs.Any(p=>p.path==path))return;
        var database=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
        var native=database.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
        var asset=FormatterServices.GetUninitializedObject(native);
        using var reader=new BinaryReader(File.OpenRead(path));
        var version=native.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
        Need((ushort)version==5,"Expected current native TextureAsset format.");
        int width=(int)native.GetProperty("width").GetValue(asset),height=(int)native.GetProperty("height").GetValue(asset);
        var format=(GraphicsFormat)native.GetProperty("format").GetValue(asset);
        var flags=TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit;
        if(Math.Max(width,height)>1)flags|=TextureCreationFlags.MipChain;
        var texture=new Texture2D(width,height,format,flags){filterMode=FilterMode.Point};
        var shader=Shader.Find("Hidden/Lift04RepeatControlAudit");Need(shader!=null,"Missing mip audit shader.");
        var material=new Material(shader);var proofs=new List<MipProof>();
        try {
            int size=texture.GetRawTextureData<byte>().Length;
            int compressed=reader.ReadInt32();Need(compressed>=-1,"Invalid compression size.");
            byte[] bytes=reader.ReadBytes(compressed==-1?size:compressed);
            Need(reader.BaseStream.Position==reader.BaseStream.Length,"Unconsumed texture data.");
            if(compressed!=-1)bytes=PreparePaint.DecodeRaw(bytes,size);
            Need(bytes.Length==size,"Wrong native mip byte count.");
            texture.LoadRawTextureData(bytes);texture.Apply(false,false);
            Need(texture.GetRawTextureData<byte>().ToArray().SequenceEqual(bytes),"Native upload changed mip bytes.");
            for(int level=0;level<texture.mipmapCount;level++){
                int w=Math.Max(1,width>>level),h=Math.Max(1,height>>level);
                var target=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                var image=new Texture2D(w,h,TextureFormat.RGBAFloat,false,true);var prior=RenderTexture.active;
                try{
                    material.SetFloat("_Mip",level);Graphics.Blit(texture,target,material);RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply(false,false);
                    var rgba=image.GetRawTextureData<float>().ToArray();int nonzero=rgba.Count(x=>x!=0f);
                    float[] minima=Enumerable.Range(0,4).Select(c=>Enumerable.Range(0,w*h).Min(i=>rgba[i*4+c])).ToArray();float[] maxima=Enumerable.Range(0,4).Select(c=>Enumerable.Range(0,w*h).Max(i=>rgba[i*4+c])).ToArray();
                    proofs.Add(new MipProof{level=level,width=w,height=h,nonzeroComponents=nonzero,minRgba=minima,maxRgba=maxima,rgbaSha256=PreparePaint.Digest(image.GetRawTextureData<byte>().ToArray())});
                } finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(target);}
            }
            textureProofs.Add(new TextureProof{path=path,cid=File.ReadAllText(path+".cid").Trim(),sha256=PreparePaint.Digest(File.ReadAllBytes(path)),format=format.ToString(),width=width,height=height,fileBytes=new FileInfo(path).Length,mips=proofs.ToArray()});
        } finally{UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(texture);}
    }

    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
}
