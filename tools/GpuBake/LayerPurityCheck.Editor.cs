using System;
using UnityEditor;
using UnityEngine;

public static class LayerPurityCheck
{
    public static void Run()
    {
        try
        {
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/BridgeBake.compute");
            var kernel=shader.FindKernel("ClassifyAsphaltTriangles");
            using(var input=new ComputeBuffer(3,16))
            using(var output=new ComputeBuffer(1,4))
            {
                input.SetData(new[]{new Vector4(.1f,.1f,0,0),new Vector4(.15f,.1f,0,0),new Vector4(.1f,.15f,0,0)});
                shader.SetBuffer(kernel,"AsphaltTriangles",input);shader.SetBuffer(kernel,"AsphaltOnly",output);
                shader.SetInt("AsphaltWrapU",1);shader.SetInt("AsphaltWrapV",1);
                for(int test=0;test<4;test++)
                {
                    var texture=new Texture2D(16,16,TextureFormat.RGBA32,true,true);
                    try
                    {
                        for(int mip=0;mip<texture.mipmapCount;mip++)
                        {
                            int size=Math.Max(1,16>>mip);
                            var pixels=new Color32[size*size];
                            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,255,255);
                            if(test==1 && mip==texture.mipmapCount-1)pixels[0].b=254;
                            if(test>=2 && mip==0)pixels[pixels.Length-1].b=254;
                            texture.SetPixels32(pixels,mip);
                        }
                        texture.Apply(false,false);
                        shader.SetTexture(kernel,"_MaskMap",texture);
                        shader.SetInt("AsphaltAnisotropy",test==3?16:1);
                        shader.Dispatch(kernel,1,1,1);
                        var actual=new uint[1];output.GetData(actual);
                        uint expected=test==0 || test==2?1u:0u;
                        if(actual[0]!=expected)throw new Exception("Layer purity case "+test+" expected "+expected+" got "+actual[0]);
                    }
                    finally {UnityEngine.Object.DestroyImmediate(texture);}
                }
            }
            Debug.Log("PASS layer purity: all-mip pure positive; authored coarse B254 rejected; distant mip0 negative control; anisotropic footprint B254 rejected.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
