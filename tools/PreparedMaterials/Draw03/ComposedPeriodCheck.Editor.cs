using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using BridgeBuilder.Bridges;

// Offline native readers and raw uploads; no scene, encoding or bridge creation.
public static class Draw03ComposedPeriodCheck
{
    [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32")] static extern bool FreeLibrary(IntPtr module);
    static void Need(bool ok,string reason){if(!ok)throw new InvalidDataException(reason);}
    public static void Run()
    {
        IntPtr module=IntPtr.Zero;
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
            string game=Arg("-gameData"),root=Arg("-materials"),managed=Path.Combine(game,"Managed");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            module=LoadLibraryW(Path.Combine(game,"Plugins/x86_64/PipelinePlugin.dll"));Need(module!=IntPtr.Zero,"Native codec");
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var readHash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
            var st=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var tt=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
            var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));
            var decode=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true).GetMethods().Single(m=>m.Name=="Decompress"&&m.GetParameters()[0].ParameterType==typeof(byte[]));
            var zstd=Enum.Parse(decode.GetParameters()[1].ParameterType,"ZSTD");
            var seen=new HashSet<string>();int surfaces=0,textures=0,mips=0;long bytes=0;
            foreach(string path in Directory.GetFiles(root,"*.Surface",SearchOption.AllDirectories))
            {
                using var reader=new BinaryReader(File.OpenRead(path));var asset=FormatterServices.GetUninitializedObject(st);
                st.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
                Need(new[]{0,1}.Contains((int)st.GetProperty("materialTemplateHash").GetValue(asset))&&!(bool)st.GetProperty("isVTMaterial").GetValue(asset),"Native Default descriptor");
                foreach(string method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(method).Invoke(null,new object[]{reader});
                Need(reader.ReadByte()==255,"Native texture map");int count=reader.ReadInt32();Need(count==4,"Four aligned channels");
                for(int i=0;i<count;i++)
                {
                    string property=reader.ReadString();Need(reader.ReadByte()==255,"Native texture reference");string cid=readHash.Invoke(null,new object[]{reader}).ToString();
                    string texturePath=Path.Combine(root,"Shared",cid+".Texture");Need(File.Exists(texturePath),"Complete style graph");
                    if(!seen.Add(cid))continue;
                    using(var header=new BinaryReader(File.OpenRead(texturePath))){var textureAsset=FormatterServices.GetUninitializedObject(tt);tt.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(textureAsset,new object[]{header});Need((int)tt.GetProperty("width").GetValue(textureAsset)>0,"Native texture header");}
                    using var stream=File.OpenRead(texturePath);
                    var texture=MovableTextureSource.Read(stream,b=>(byte[])decode.Invoke(null,new[]{(object)b,zstd}));
                    try
                    {
                        Need(stream.Position==stream.Length,"Complete texture consumed");
                        Need(texture.width==1||texture.width==968||texture.width==512,"Original period or finite atlas resolution");
                        Need(texture.mipmapCount==1+(int)Math.Floor(Math.Log(Math.Max(texture.width,texture.height),2)),"Complete mip chain");
                        Need(texture.wrapMode==(texture.width==512?TextureWrapMode.Clamp:TextureWrapMode.Repeat)&&texture.filterMode==FilterMode.Trilinear,"Native sampler");
                        Need(GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat)==(property=="_BaseColorMap"),"Channel color space");
                        bytes+=texture.GetRawTextureData<byte>().Length;textures++;mips+=texture.mipmapCount;
                    }
                    finally{UnityEngine.Object.DestroyImmediate(texture);}
                }
                var keywords=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader})).Cast<string>();Need(keywords.Contains("_TANGENTSPACE_OCTO"),"Packed normal interpretation");Need(reader.BaseStream.Position==reader.BaseStream.Length,"Complete Surface consumed");surfaces++;
            }
            Need(surfaces==5&&textures==16,"Expected four near units and finite far atlas");
            Debug.Log($"PASS Draw03 composed periods: {surfaces} native Surface descriptors; {textures} native Texture headers and uploads; {mips} complete mip levels; {bytes} raw RGBA bytes; exact 968 texel U; finite V guards; complete native graph. Shader filtering and game acceptance remain separate.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally{if(module!=IntPtr.Zero)FreeLibrary(module);}
    }
}
