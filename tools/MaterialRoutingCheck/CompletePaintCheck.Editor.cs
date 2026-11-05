using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

public static class CompletePaintCheck
{
 [DllImport("kernel32", CharSet=CharSet.Unicode)] static extern IntPtr LoadLibraryW(string path);
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string key)=>args[Array.IndexOf(args,key)+1];
 var managed=Arg("-gameManaged");var root=Arg("-paint");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
 var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);var st=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var tt=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
 var surface=FormatterServices.GetUninitializedObject(st);var channels=new System.Collections.Generic.Dictionary<string,string>();
 using(var r=new BinaryReader(File.OpenRead(Path.Combine(root,"SkinnedPaint.Surface")))){
 st.GetMethod("ReadHeader",F).Invoke(surface,new object[]{r});Need(!(bool)st.GetProperty("isVTMaterial").GetValue(surface)&&(int)st.GetProperty("materialTemplateHash").GetValue(surface)==0,"Complete paint object template");
 var floats=(IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null,new object[]{r});
 Need((float)floats["_Metallic"]==0&&(float)floats["_Smoothness"]==.35f&&(float)floats["_CullMode"]==2&&(float)floats["_CullModeForward"]==2,"Original explicit paint appearance changed");
 foreach(var name in new[]{"ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(name).Invoke(null,new object[]{r});
 Need(r.ReadByte()==255&&r.ReadInt32()==4,"Four explicitly bound object channels");
 var hash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
 for(int i=0;i<4;i++){string name=r.ReadString();Need(r.ReadByte()==255,"Missing paint texture");channels.Add(name,hash.Invoke(null,new object[]{r}).ToString());}
 var keys=(IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{r});Need(keys.Cast<string>().Contains("_TANGENTSPACE_OCTO"),"Native packed tangent keyword");Need(r.BaseStream.Position==r.BaseStream.Length,"Trailing descriptor bytes");}
 Need(LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")))!=IntPtr.Zero,"Native compression library missing");var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll"));var compression=pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities",true);var dec=compression.GetMethods(BindingFlags.Static|BindingFlags.Public).First(m=>m.Name=="Decompress"&&m.GetParameters().Length==2&&m.GetParameters()[0].ParameterType==typeof(byte[]));var fmt=Enum.Parse(dec.GetParameters()[1].ParameterType,"Zstd",true);
 int totalMips=0;
 foreach(var pair in channels){
 var asset=FormatterServices.GetUninitializedObject(tt);using var r=new BinaryReader(File.OpenRead(Path.Combine(root,pair.Value+".Texture")));tt.GetMethod("ReadHeader",F).Invoke(asset,new object[]{r});
 int w=(int)tt.GetProperty("width").GetValue(asset),h=(int)tt.GetProperty("height").GetValue(asset),mips=(int)tt.GetProperty("mipsCount").GetValue(asset);var format=(GraphicsFormat)tt.GetProperty("format").GetValue(asset);
 int size=r.ReadInt32();var data=r.ReadBytes((int)(r.BaseStream.Length-r.BaseStream.Position));if(size!=-1)data=(byte[])dec.Invoke(null,new object[]{data,fmt});
 var texture=new Texture2D(w,h,format,TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit|(mips>1?TextureCreationFlags.MipChain:0));texture.LoadRawTextureData(data);texture.Apply(false,false);Need(texture.GetRawTextureData<byte>().ToArray().SequenceEqual(data),"Native texture upload changed bytes");
 if(pair.Key=="_BaseColorMap"){var expected=File.ReadAllBytes(Arg("-originalBase")).Skip(18).ToArray();Need(data.SequenceEqual(expected)&&mips==12&&w==2048&&h==2048,"Original BC7 full mip blocks changed");}
 else{var c=texture.GetPixel(0,0);var expected=pair.Key=="_NormalMap"?new Color(1,.5f,1,.5f):pair.Key=="_MaskMap"?Color.white:Color.clear;Need(c==expected,"Neutral channel native readback changed");if(pair.Key=="_NormalMap"){float x=c.r*c.a*2-1,y=c.g*2-1;Need(x==0&&y==0,"Normal is not exactly flat in native AG encoding");}}
 totalMips+=mips;UnityEngine.Object.DestroyImmediate(texture);
 }
 Debug.Log("PASS complete paint: native Surface/Texture readers; four explicit channels; original 12 mip BC7 blocks byte-identical after lossless decompression/native upload; exact half-float flat AG normal, white mask, zero control. Existing metallic/smoothness/culling retained. Not live game renderer acceptance. Total mips="+totalMips);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
