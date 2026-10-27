using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using BridgeBuilder.Bridges;

// Draw02-only read-only native source and full-mip audit; no encoding or publication.
public static class Draw02RepeatControlAudit
{
    [Serializable] public class Input {public string recipe;public PrepareStructure.Source[] sources;public PrepareRepeat.Unit[] units;}
    [Serializable] public class SourceProof {public string cid,sha256,vtSurfaceCid,controlStackCid;public string[] decodedChannels;public bool authoredControl;public int controlWidth,controlHeight;}
    [Serializable] public class MipProof {public int mip,width,height,nonzeroTexels;public float maxAbsolute;}
    [Serializable] public class TextureProof {public string cid,path,sha256;public long fileBytes;public MipProof[] mips;public bool allTransparentZero;}
    [Serializable] public class UnitProof {public string key,sourceCid,surfaceCid,surfaceSha256,controlCid;public string[] channelCids;public bool eligible;public string reason;}
    [Serializable] public class Report {public string status;public SourceProof[] sources;public UnitProof[] units;public TextureProof[] controls;public TextureProof zero;public int candidates=0;public long bytesSaved=0;public bool noEncoding=true,noAssetChanges=true;}
    static void Need(bool ok,string error){if(!ok)throw new InvalidDataException(error);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string game=Arg("-gameData"),managed=Path.Combine(game,"Managed"),style=Arg("-styleRoot"),output=Arg("-candidateOutput");
        var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-repeatInventory")));Need(input.recipe==PrepareRepeat.Recipe,"Wrong recipe.");
        var archives=new List<ZipArchive>();var entries=new Dictionary<string,ZipArchiveEntry>();var module=PreparePaint.OpenCodec(game);
        ResolveEventHandler resolve=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try
        {
            foreach(string family in new[]{"Game","BridgesAndPorts"})foreach(string path in Directory.GetFiles(Path.Combine(game,"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT")))
            {
                var zip=ZipFile.OpenRead(path);archives.Add(zip);
                foreach(var e in zip.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var reader=new StreamReader(e.Open());string cid=reader.ReadToEnd().Trim();var payload=zip.GetEntry(e.FullName.Substring(0,e.FullName.Length-4));if(payload==null)continue;
                    if(entries.TryGetValue(cid,out var prior))Need(Bytes(prior).SequenceEqual(Bytes(payload)),"Conflicting source CID.");entries[cid]=payload;}
            }
            byte[] Read(string cid)=>entries.TryGetValue(cid,out var e)?Bytes(e):throw new FileNotFoundException("Missing source "+cid);
            var proofs=new List<SourceProof>();
            foreach(var group in input.units.GroupBy(u=>u.sourceCid))
            {
                var data=Read(group.Key);string sha=PreparePaint.Digest(data);Need(group.All(u=>u.sourceDigest==sha),"Source digest differs.");Need(data[6]==1,"Expected VT source.");
                var type=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                using var decoded=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{data,input.sources.Single(s=>s.cid==group.Key),(Func<string,byte[]>)Read},null);
                var textures=(Dictionary<string,Texture2D>)type.GetField("Textures",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(decoded);
                textures.TryGetValue("_ControlMask",out var control);
                proofs.Add(new SourceProof{cid=group.Key,sha256=sha,controlStackCid=PreparePaint.HashText(data,7+76+3*16),vtSurfaceCid=data[283]==255?PreparePaint.HashText(data,284):null,decodedChannels=textures.Keys.OrderBy(x=>x).ToArray(),authoredControl=control!=null,controlWidth=control!=null?control.width:0,controlHeight=control!=null?control.height:0});
            }
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var nativeType=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var resources=Directory.GetFiles(style,"*.Texture",SearchOption.AllDirectories).GroupBy(p=>File.ReadAllText(p+".cid").Trim()).ToDictionary(g=>g.Key,g=>g.First());
            var units=new List<UnitProof>();var controlIds=new HashSet<string>();
            var packaged=Directory.GetFiles(Path.Combine(style,"Repeat"),"Repeat.Surface",SearchOption.AllDirectories);Need(packaged.Length==input.units.Length,"Unaccounted repeat material.");
            foreach(var file in packaged)
            {
                string key=Path.GetFileName(Path.GetDirectoryName(file));var unit=input.units.Single(u=>u.key==key);var data=File.ReadAllBytes(file);
                using var reader=new BinaryReader(new MemoryStream(data));var asset=FormatterServices.GetUninitializedObject(nativeType);
                nativeType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});Need(!(bool)nativeType.GetProperty("isVTMaterial").GetValue(asset),"Repeat must be non-VT.");
                foreach(string method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
                Need(reader.ReadByte()==255,"Missing texture dictionary.");int count=reader.ReadInt32();var bindings=new Dictionary<string,string>();
                for(int i=0;i<count;i++){string name=reader.ReadString();Need(reader.ReadByte()==255,"Null binding.");string cid=PreparePaint.HashText(reader.ReadBytes(16),0);Need(resources.ContainsKey(cid),"Missing local dependency.");bindings.Add(name,cid);}
                binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader});Need(reader.BaseStream.Position==data.Length,"Unconsumed surface.");
                string control=bindings["_ControlMask"];controlIds.Add(control);
                bool authored=proofs.Single(s=>s.cid==unit.sourceCid).authoredControl;
                // This bridge is rejected at the source eligibility gate, independently of pixel content.
                Need(authored,"Unexpected newly eligible source: rerun candidate implementation review.");
                units.Add(new UnitProof{key=key,sourceCid=unit.sourceCid,surfaceCid=File.ReadAllText(file+".cid").Trim(),surfaceSha256=PreparePaint.Digest(data),controlCid=control,channelCids=new[]{bindings["_BaseColorMap"],bindings["_NormalMap"],bindings["_MaskMap"]},eligible=false,reason="Real prototype has authored _ControlMask; replacement is prohibited even if a cropped unit is zero."});
            }
            var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Draw02RepeatControlAudit.compute");int kernel=program.FindKernel("ReadMip");
            TextureProof Check(string cid,string path)
            {
                byte[] bytes=File.ReadAllBytes(path);
                var materialType=typeof(PrepareStructure).GetNestedType("MaterialSource",BindingFlags.NonPublic);
                int rawSize=(int)materialType.GetMethod("RawSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{bytes});
                using var stream=new MemoryStream(bytes);var texture=MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,rawSize));
                try
                {
                    var mips=new List<MipProof>();program.SetTexture(kernel,"Input",texture);
                    for(int mip=0;mip<texture.mipmapCount;mip++)
                    {
                        int w=Math.Max(1,texture.width>>mip),h=Math.Max(1,texture.height>>mip);using var buffer=new ComputeBuffer(w*h,16);
                        program.SetInt("Width",w);program.SetInt("Height",h);program.SetInt("Mip",mip);program.SetBuffer(kernel,"Pixels",buffer);program.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                        var values=new Vector4[w*h];buffer.GetData(values);int nonzero=0;float maximum=0;
                        foreach(var v in values){bool nz=false;for(int c=0;c<4;c++){Need(!float.IsNaN(v[c]),"NaN native control.");maximum=Mathf.Max(maximum,Mathf.Abs(v[c]));nz|=v[c]!=0;}if(nz)nonzero++;}
                        mips.Add(new MipProof{mip=mip,width=w,height=h,nonzeroTexels=nonzero,maxAbsolute=maximum});
                    }
                    return new TextureProof{cid=cid,path=path,sha256=PreparePaint.Digest(bytes),fileBytes=bytes.Length,mips=mips.ToArray(),allTransparentZero=mips.All(m=>m.nonzeroTexels==0)};
                }
                finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            string zeroPath=Path.Combine(style,"Road","ZeroControl.Texture");var zero=Check(File.ReadAllText(zeroPath+".cid").Trim(),zeroPath);Need(zero.allTransparentZero,"Local ZeroControl is not transparent zero.");
            var controls=controlIds.Select(cid=>Check(cid,resources[cid])).ToArray();
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(new Report{status="PASS_NO_ELIGIBLE_UNITS",sources=proofs.ToArray(),units=units.ToArray(),controls=controls,zero=zero},true));
            Debug.Log($"PASS Draw02 repeat control audit: units={units.Count}; controls={controls.Length}; authored sources={proofs.Count}; candidates=0; saved bytes=0; no encoding.");
        }
        finally{foreach(var a in archives)a.Dispose();AppDomain.CurrentDomain.AssemblyResolve-=resolve;PreparePaint.CloseCodec(module);}
    }
    static byte[] Bytes(ZipArchiveEntry e){using var s=e.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
}
