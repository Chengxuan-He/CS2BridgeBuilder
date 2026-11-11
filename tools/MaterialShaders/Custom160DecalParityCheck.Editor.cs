using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class Custom160DecalParityCheck
{
    public static void Run()
    {
        var owned=new List<UnityEngine.Object>();var buffers=new List<ComputeBuffer>();
        try
        {
            const int width=128;
            var camera=new Vector3(0,2,0);
            var vp=new Matrix4x4(new Vector4(1,0,0,0),new Vector4(0,0,1,0),new Vector4(0,1,0,0),new Vector4(0,0,2.5f,1));
            var globals=new Vector4[183];for(int i=0;i<4;i++){globals[i]=Matrix4x4.identity.GetColumn(i);globals[20+i]=vp.GetColumn(i);globals[28+i]=vp.inverse.GetColumn(i);}
            globals[44]=camera;globals[46]=new Vector4(width,width,1f/width,1f/width);
            void Constant(string name,Vector4[] values){var b=new ComputeBuffer(values.Length,16,ComputeBufferType.Constant);b.SetData(values);Shader.SetGlobalConstantBuffer(name,b,0,values.Length*16);buffers.Add(b);}
            Constant("ShaderVariablesGlobal",globals);Constant("$Globals",new[]{new Vector4(10000,0,0,0)});Constant("Infoviews",new Vector4[405]);
            Shader.SetGlobalVector("colossal_LodParameters",new Vector4(10000,0,0,0));
            var depth=new Texture2DArray(width,width,1,TextureFormat.RFloat,false,true);
            depth.SetPixels(Enumerable.Repeat(new Color(.5f,0,0,0),width*width).ToArray(),0);depth.Apply();owned.Add(depth);Shader.SetGlobalTexture("_CameraDepthTexture",depth);
            var decalPrepass=new Texture2DArray(width,width,1,TextureFormat.RGBAFloat,false,true);
            decalPrepass.SetPixels(Enumerable.Repeat(Color.white,width*width).ToArray(),0);decalPrepass.Apply();owned.Add(decalPrepass);Shader.SetGlobalTexture("_DecalPrepassTexture",decalPrepass);
            Texture2D Channel(Color value){var t=new Texture2D(4,4,TextureFormat.RGBAFloat,false,true);t.SetPixels(Enumerable.Repeat(value,16).ToArray());t.Apply();owned.Add(t);return t;}
            var albedo=Channel(new Color(.8f,.2f,.1f,.25f));var normal=Channel(new Color(.5f,.5f,1,1));var mask=Channel(new Color(.4f,.6f,.8f,.5f));
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
            var targets=new RenderTexture[4];for(int i=0;i<4;i++){targets[i]=new RenderTexture(width,width,i==0?24:0,RenderTextureFormat.ARGBFloat);targets[i].Create();owned.Add(targets[i]);}
            var read=new Texture2D(width,width,TextureFormat.RGBAFloat,false,true);owned.Add(read);
            var outputs=new List<Color[][]>();
            foreach(string name in new[]{"DecalOriginal","DecalTranscribed"})
            {
                var bundle=AssetBundle.LoadFromFile("E:/CS2BridgeBuilder/work/custom160/"+name+".bundle");
                if(bundle==null)throw new Exception("No decal bundle: "+name);
                var material=new Material(bundle.LoadAllAssets<Shader>().Single());
                try
                {
                    material.DisableKeyword("DOTS_INSTANCING_ON");material.DisableKeyword("ENABLE_VT");material.DisableKeyword("_TANGENTSPACE_OCTO");
                    foreach(var key in new[]{"DECALS_4RT","_MATERIAL_AFFECTS_ALBEDO","_MATERIAL_AFFECTS_NORMAL","_MATERIAL_AFFECTS_MASKMAP"})material.EnableKeyword(key);
                    material.SetTexture("_BaseColorMap",albedo);material.SetTexture("_NormalMap",normal);material.SetTexture("_MaskMap",mask);
                    material.SetColor("_BaseColor",Color.white);material.SetVector("colossal_MeshSize",Vector4.one);material.SetVector("colossal_TextureArea",new Vector4(0,0,1,1));
                    material.SetFloat("colossal_LodDistanceFactor",1);material.SetFloat("colossal_DecalLayerMask",255);
                    material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_NormalOpacity",1);material.SetFloat("_MetallicOpacity",1);
                    material.SetFloat("_NormalAlphaSource",0);material.SetFloat("_MetallicAlphaSource",0);
                    for(int i=0;i<4;i++)material.SetInt("_DecalColorMask"+i,i==3?3:15);
                    foreach(var rt in targets){RenderTexture.active=rt;GL.Clear(true,true,new Color(0,0,0,1),0);}
                    Graphics.SetRenderTarget(targets.Select(t=>t.colorBuffer).ToArray(),targets[0].depthBuffer);
                    if(!material.SetPass(material.FindPass("DBufferMesh")))throw new Exception("DBufferMesh unavailable: "+name);
                    Graphics.DrawMeshNow(mesh,Matrix4x4.Scale(Vector3.one*.8f));
                    var pixels=new Color[4][];
                    for(int i=0;i<4;i++){RenderTexture.active=targets[i];read.ReadPixels(new Rect(0,0,width,width),0,0);read.Apply();pixels[i]=read.GetPixels();}
                    int covered=pixels[0].Count(c=>c.r>.01f);Debug.Log(name+" coverage="+covered+" center="+pixels[0][width*64+64]);
                    if(covered<100)throw new Exception("No useful native decal pixels: "+name);
                    outputs.Add(pixels);
                }
                finally {UnityEngine.Object.DestroyImmediate(material);bundle.Unload(true);}
            }
            float maximum=0;int different=0;
            for(int target=0;target<4;target++)for(int i=0;i<width*width;i++)for(int c=0;c<4;c++)
            {float d=Math.Abs(outputs[0][target][i][c]-outputs[1][target][i][c]);maximum=Math.Max(maximum,d);if(d>1e-5f)different++;}
            if(different!=0)throw new Exception("Native DBuffer transcription mismatch: "+different+" maximum="+maximum);
            Debug.Log("PASS native original versus transcribed DBuffer PS: all four target pixels within 1e-5, max="+maximum+"; continuous source alpha .25, no cutoff; non-VT static projection only.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally {RenderTexture.active=null;foreach(var b in buffers)b.Dispose();foreach(var o in owned)UnityEngine.Object.DestroyImmediate(o);}
    }
}
