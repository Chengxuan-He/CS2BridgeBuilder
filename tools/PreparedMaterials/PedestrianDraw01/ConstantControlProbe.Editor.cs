using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Read-only native/GPU sampler validation; never invokes a texture encoder.
public static class PedestrianConstantControlProbe
{
    [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32")] static extern bool FreeLibrary(IntPtr module);
    [Serializable] public sealed class TextureInfo { public string path,sha256; public int width,height,mips,format,filter,wrap,aniso,rawBytes; }
    [Serializable] public sealed class Result { public string status,unity,gpu; public TextureInfo reference,candidate,transparent; public int casesPerRun,runs,sampledVectors,opaqueAlphaMismatches,transparentEquivalentSamples; public float maximumEquivalentError,maximumOpaqueError; public bool opaqueReplacementEquivalent=false; public bool noEncoding=true,noGameScene=true; }
    static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string managed=Arg("-gameManaged");var module=LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")));
        Require(module!=IntPtr.Zero,"Native pipeline unavailable.");
        ResolveEventHandler resolve=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        var owned=new List<Texture2D>();
        try {
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
            var compression=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true);
            var decode=compression.GetMethods().Single(m=>m.Name=="Decompress" && m.GetParameters()[0].ParameterType==typeof(byte[]));
            var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
            var type=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
            (Texture2D Texture,TextureInfo Info) Read(string path) {
                var payload=File.ReadAllBytes(path);var asset=FormatterServices.GetUninitializedObject(type);
                using var reader=new BinaryReader(new MemoryStream(payload));
                Require((ushort)type.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader})==5,"Native texture v5 required.");
                int width=(int)type.GetProperty("width").GetValue(asset),height=(int)type.GetProperty("height").GetValue(asset);
                var format=(GraphicsFormat)type.GetProperty("format").GetValue(asset);int stored=reader.ReadInt32();
                byte[] raw=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
                if(stored!=-1){Require(stored==raw.Length,"Compressed length mismatch.");raw=(byte[])decode.Invoke(null,new[]{(object)raw,zstd});}
                var flags=TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit;
                if(payload[8]>1)flags|=TextureCreationFlags.MipChain;
                var texture=new Texture2D(width,height,format,flags){filterMode=(FilterMode)payload[11],wrapMode=(TextureWrapMode)payload[12],anisoLevel=payload[13]};owned.Add(texture);
                texture.LoadRawTextureData(raw);texture.Apply(false,false);
                Require(texture.mipmapCount==payload[8],"Mip metadata differs.");
                Require(texture.GetRawTextureData<byte>().ToArray().SequenceEqual(raw),"Raw mip upload changed bytes.");
                using var sha=SHA256.Create();
                var info=new TextureInfo{path=path,sha256=BitConverter.ToString(sha.ComputeHash(payload)).Replace("-","").ToLowerInvariant(),width=width,height=height,mips=texture.mipmapCount,format=(int)format,filter=payload[11],wrap=payload[12],aniso=payload[13],rawBytes=raw.Length};
                return(texture,info);
            }
            var reference=Read(Arg("-referenceTexture"));var candidate=Read(Arg("-candidateTexture"));var transparent=Read(Arg("-transparentTexture"));
            Require(candidate.Info.width==1 && candidate.Info.height==1 && candidate.Info.mips==1 && candidate.Info.format==8,"Expected native 1x1 RGBA8 UNorm complete mip chain.");
            Require(candidate.Texture.GetRawTextureData<byte>().ToArray().SequenceEqual(new byte[]{0,0,0,255}),"Candidate must be opaque black.");
            var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ConstantControlProbe.compute");Require(program!=null,"Missing sampler shader.");int kernel=program.FindKernel("Sample");
            var cases=new List<Vector4>();
            foreach(float uv in new[]{-1000.125f,-2f,-1.001f,-.5f,-.0001f,0f,.0001f,.5f,.9999f,1f,1.0001f,2f,1000.125f})
            foreach(float lod in new[]{-4f,0f,.5f,1f,4.5f,9f,10f,16f})
            foreach(float gradient in new[]{.000001f,.001f,.01f,1f,16f})cases.Add(new Vector4(uv,1-uv,lod,gradient));
            using var input=new ComputeBuffer(cases.Count,16);input.SetData(cases.ToArray());using var output=new ComputeBuffer(cases.Count*4,16);
            program.SetInt("Count",cases.Count);program.SetBuffer(kernel,"Cases",input);program.SetBuffer(kernel,"Results",output);
            program.SetTexture(kernel,"Reference",reference.Texture);program.SetTexture(kernel,"Candidate",candidate.Texture);program.SetTexture(kernel,"UnityBlack",Texture2D.blackTexture);program.SetTexture(kernel,"Transparent",transparent.Texture);
            var result=new Result{status="PASS",unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceVersion,reference=reference.Info,candidate=candidate.Info,transparent=transparent.Info,casesPerRun=cases.Count};
            foreach(var wrap in new[]{TextureWrapMode.Repeat,TextureWrapMode.Clamp,TextureWrapMode.Mirror,TextureWrapMode.MirrorOnce})
            foreach(var filter in new[]{FilterMode.Point,FilterMode.Bilinear,FilterMode.Trilinear})
            foreach(int mode in new[]{0,1}) {
                foreach(var texture in new[]{reference.Texture,candidate.Texture,transparent.Texture}){texture.wrapMode=wrap;texture.filterMode=filter;}
                program.SetInt("Mode",mode);program.Dispatch(kernel,(cases.Count+63)/64,1,1);
                var values=new Vector4[cases.Count*4];output.GetData(values);result.runs++;
                for(int i=0;i<cases.Count;i++){
                    for(int t=0;t<4;t++)for(int channel=0;channel<4;channel++){
                        var expected=t==1?new Vector4(0,0,0,1):Vector4.zero;
                        float error=Mathf.Abs(values[i*4+t][channel]-expected[channel]);
                        Require(!float.IsNaN(error) && error<=.0000001f,"Unexpected constant sample at run "+result.runs+", case "+i+", texture "+t+", channel "+channel+": "+values[i*4+t]);
                        if(t!=1)result.maximumEquivalentError=Mathf.Max(result.maximumEquivalentError,Mathf.Abs(values[i*4+t][channel]-values[i*4][channel]));
                        else result.maximumOpaqueError=Mathf.Max(result.maximumOpaqueError,Mathf.Abs(values[i*4+t][channel]-values[i*4][channel]));
                    }
                    Require(values[i*4].w==0 && values[i*4+1].w==1 && values[i*4+2].w==0 && values[i*4+3].w==0,"Alpha distinction lost.");
                    result.opaqueAlphaMismatches++;result.transparentEquivalentSamples++;
                }
                result.sampledVectors+=values.Length;
            }
            File.WriteAllText(Arg("-probeReport"),JsonUtility.ToJson(result,true));
            Debug.Log($"PASS constant control native probe: runs={result.runs}; cases={result.casesPerRun}; vectors={result.sampledVectors}; equivalentError={result.maximumEquivalentError}; opaqueError={result.maximumOpaqueError}; opaque alpha mismatches={result.opaqueAlphaMismatches}; no encoding; no scene.");
        } finally {foreach(var texture in owned)UnityEngine.Object.DestroyImmediate(texture);AppDomain.CurrentDomain.AssemblyResolve-=resolve;FreeLibrary(module);}
    }
}
