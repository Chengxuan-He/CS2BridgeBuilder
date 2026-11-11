using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Diagnostic fragment only: proves actual native parameter/bone binding, not PBR.
public static class Custom160NativeVertexBindingCheck
{
    public static void Run()
    {
        AssetBundle bundle=null; Material material=null; Mesh mesh=null;
        ComputeBuffer globals=null,bones=null; RenderTexture target=null; Texture2D read=null;
        var textures=new List<Texture2D>();var extraTargets=new List<RenderTexture>();
        bool pbr=Environment.GetCommandLineArgs().Contains("-nativePbr");
        int exit=1;
        try
        {
            var file=Environment.GetCommandLineArgs().Contains("-runtimePackage")?"native-layered-runtime.bundle":pbr?"native-layered-all-dots.bundle":"native-layered-vertex-probe.bundle";
            bundle=AssetBundle.LoadFromFile("E:/CS2BridgeBuilder/work/custom160/"+file);
            if(bundle==null)throw new Exception("probe bundle load failed");
            material=new Material(bundle.LoadAllAssets<Shader>()[0]);
            material.EnableKeyword("_GPU_ANIMATION_PROCEDURAL"); material.EnableKeyword("DECALS_4RT");
            material.DisableKeyword("DOTS_INSTANCING_ON"); material.SetVector("colossal_BoneParameters",new Vector4(0,2,0,0));
            material.SetFloat("_CullMode",0); material.SetFloat("_CullModeForward",0);
            // This isolated probe has no preceding HDRP depth prepass.
            material.SetFloat("_ZTestGBuffer",(float)CompareFunction.Always);
            Texture2D mask=null;
            if(pbr)
            {
                Texture2D Channel(string name,Color color)
                {
                    var image=new Texture2D(2,2,TextureFormat.RGBAFloat,false,true);image.SetPixels(Enumerable.Repeat(color,4).ToArray());image.Apply();
                    material.SetTexture(name,image);textures.Add(image);return image;
                }
                Channel("_BaseColorMap",new Color(.8f,.1f,.05f,1));Channel("_WorldspaceAlbedo",new Color(.05f,.1f,.8f,1));
                Channel("_NormalMap",new Color(.5f,.5f,1,1));Channel("_WorldspaceNormalMap",new Color(.5f,.5f,1,1));
                mask=Channel("_MaskMap",new Color(0,1,0,.5f));Channel("_WorldspaceMaskMap",new Color(0,1,0,.5f));
                Channel("_ControlMask",Color.clear);Channel("_EmissiveColorMap",Color.clear);
                material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorldspaceUVScale",.4f);
                material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.5f);material.SetFloat("_AlphaCutoff",0);
            }
            mesh=new Mesh(); mesh.vertices=new[]{new Vector3(-.2f,-.2f,.5f),new Vector3(0,.2f,.5f),new Vector3(.2f,-.2f,.5f)};
            mesh.normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward};
            mesh.tangents=new[]{new Vector4(1,0,0,1),new Vector4(1,0,0,1),new Vector4(1,0,0,1)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.right}; mesh.uv2=mesh.uv; mesh.uv3=mesh.uv;
            mesh.triangles=new[]{0,1,2}; mesh.bindposes=new[]{Matrix4x4.identity,Matrix4x4.identity};
            mesh.boneWeights=new[]{new BoneWeight{boneIndex0=1,weight0=1},new BoneWeight{boneIndex0=1,weight0=1},new BoneWeight{boneIndex0=1,weight0=1}};
            var values=new Vector4[183]; values[20]=new Vector4(1,0,0,0); values[21]=new Vector4(0,1,0,0);
            values[22]=new Vector4(0,0,1,0); values[23]=new Vector4(0,0,0,1);
            globals=new ComputeBuffer(values.Length,16,ComputeBufferType.Constant); globals.SetData(values);
            Shader.SetGlobalConstantBuffer("ShaderVariablesGlobal",globals,0,values.Length*16);
            bones=new ComputeBuffer(32,4,ComputeBufferType.Raw); material.SetBuffer("_BoneTransforms",bones);
            target=new RenderTexture(128,128,24,RenderTextureFormat.ARGBFloat); target.Create();
            if(pbr)for(int i=0;i<3;i++){var rt=new RenderTexture(128,128,0,RenderTextureFormat.ARGBFloat);rt.Create();extraTargets.Add(rt);}
            read=new Texture2D(128,128,TextureFormat.RGBAFloat,false,true);
            float[] centers=new float[2]; int[] counts=new int[2];var colors=new Vector3[2];
            for(int frame=0;frame<2;frame++)
            {
                var matrices=new float[32]; for(int b=0;b<2;b++)for(int i=0;i<4;i++)matrices[b*16+i*5]=1;
                matrices[28]=frame*.5f; bones.SetData(matrices);
                if(pbr){mask.SetPixels(Enumerable.Repeat(new Color(0,1,frame,.5f),4).ToArray());mask.Apply();}
                RenderTexture.active=target; GL.Clear(true,true,Color.clear);
                if(pbr)
                {
                    foreach(var rt in extraTargets){RenderTexture.active=rt;GL.Clear(false,true,Color.clear);}
                    Graphics.SetRenderTarget(new[]{target.colorBuffer,extraTargets[0].colorBuffer,extraTargets[1].colorBuffer,extraTargets[2].colorBuffer},target.depthBuffer);
                }
                if(!material.SetPass(material.FindPass("GBuffer")))throw new Exception("GBuffer binding failed");
                Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
                RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();var pixels=read.GetPixels();
                for(int i=0;i<pixels.Length;i++)if(Math.Max(pixels[i].r,pixels[i].b)>.1f){counts[frame]++;centers[frame]+=i%128;colors[frame]+=new Vector3(pixels[i].r,pixels[i].g,pixels[i].b);}
                if(counts[frame]==0)throw new Exception("no native vertex probe pixels at pose "+frame);
                centers[frame]/=counts[frame];
                colors[frame]/=counts[frame];
            }
            if(Math.Abs((centers[1]-centers[0])-32)>1)throw new Exception("native bone metadata did not drive pose: "+string.Join(",",centers));
            if(pbr&&!(colors[0].x>colors[0].z&&colors[1].z>colors[1].x))throw new Exception("native PBR mask did not select two source layers: "+string.Join(",",colors));
            var text="PASS native non-DOTS parameter binding: bone1 translates visible vertices; centers="+string.Join(",",centers)+" coverage="+string.Join(",",counts)+" colors="+string.Join(",",colors)+(pbr?"; retained native GBuffer PS, synthetic two-layer textures; not game scene acceptance":"; diagnostic fragment, not native PBR acceptance");
            File.WriteAllText("E:/CS2BridgeBuilder/work/custom160/"+(pbr?"native-pbr-binding-result.txt":"native-vertex-binding-result.txt"),text); Debug.Log(text);exit=0;
        }
        catch(Exception exception){Debug.LogException(exception);}
        finally
        {
            RenderTexture.active=null;globals?.Dispose();bones?.Dispose();
            if(read!=null)UnityEngine.Object.DestroyImmediate(read);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            foreach(var texture in textures)UnityEngine.Object.DestroyImmediate(texture);
            foreach(var rt in extraTargets){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);if(material!=null)UnityEngine.Object.DestroyImmediate(material);bundle?.Unload(true);
            EditorApplication.Exit(exit);
        }
    }
}
