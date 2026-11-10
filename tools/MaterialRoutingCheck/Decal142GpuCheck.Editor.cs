using System;using System.IO;using UnityEngine;using UnityEditor;
public static class Decal142GpuCheck{
public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var output=Arg("-outputRoot");Directory.CreateDirectory(output);
 var bytes=File.ReadAllBytes(Arg("-sourceTexture"));int w=BitConverter.ToUInt16(bytes,2),h=BitConverter.ToUInt16(bytes,4),offset=11+bytes[7];var raw=new byte[bytes.Length-offset];Buffer.BlockCopy(bytes,offset,raw,0,raw.Length);
 var texture=new Texture2D(w,h,TextureFormat.BC7,true,false);texture.LoadRawTextureData(raw);texture.Apply(false,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
 var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Decal142.compute");var target=new RenderTexture(1024,512,0,RenderTextureFormat.ARGBFloat){enableRandomWrite=true};target.Create();var counts=new ComputeBuffer(3,4);int kernel=shader.FindKernel("Sample");shader.SetTexture(kernel,"Source",texture);shader.SetTexture(kernel,"Output",target);shader.SetBuffer(kernel,"Counts",counts);
 var log="GPU="+SystemInfo.graphicsDeviceName+";API="+SystemInfo.graphicsDeviceType+"; native source="+w+"x"+h+";mips="+texture.mipmapCount+"\n";
 foreach(int mip in new[]{0,4,8})foreach(int mode in new[]{0,1,2}){counts.SetData(new uint[3]);shader.SetFloat("Mip",mip);shader.SetInt("Mode",mode);shader.Dispatch(kernel,128,64,1);var values=new uint[3];counts.GetData(values);if(values[0]<values[1])throw new Exception("coverage subset");RenderTexture.active=target;var image=new Texture2D(1024,512,TextureFormat.RGBA32,false,true);image.ReadPixels(new Rect(0,0,1024,512),0,0);image.Apply();File.WriteAllBytes(output+"/mip"+mip+"-mode"+mode+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);log+="mip="+mip+" mode="+mode+" old="+values[0]+" native="+values[1]+" partial="+values[2]+"\n";}
 File.WriteAllText(output+"/result.txt",log);Debug.Log("PASS actual original BC7 GPU sampling; native near-cutoff coverage comparison, not full HDRP rendering\n"+log);counts.Dispose();target.Release();UnityEngine.Object.DestroyImmediate(texture);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
