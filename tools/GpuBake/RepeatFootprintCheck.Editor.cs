using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RepeatFootprintCheck
{
    public static void Run()
    {
        try
        {
            var source=new Texture2D(512,512,TextureFormat.RGBA32,true,true)
                {filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,anisoLevel=16};
            for(int mip=0;mip<source.mipmapCount;mip++)
            {
                int n=Math.Max(1,512>>mip);var pixels=new Color32[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                    pixels[y*n+x]=new Color32((byte)((x*53+y*17)%256),(byte)(mip*25),(byte)((x*29+y*71)%256),255);
                source.SetPixels32(pixels,mip);
            }
            source.Apply(false,false);
            var extract=typeof(PrepareRepeatUnits).GetMethod("Extract",BindingFlags.Static|BindingFlags.NonPublic);
            var kernel=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
            Texture2D Bake(bool grad)=>(Texture2D)extract.Invoke(null,new object[]{kernel,source,0d,0d,322d,512d,324,512,1,1,null,true,false,true,false,null,grad,null});
            var actual=Bake(true);var scalar=Bake(false);
            var probe=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatFootprintProbe.compute");
            int k=probe.FindKernel("Probe");probe.SetTexture(k,"Source",source);
            int maxError=0,scalarDifference=0;long compared=0;
            for(int mip=0;mip<actual.mipmapCount;mip++)
            {
                int w=Math.Max(1,324>>mip),h=Math.Max(1,512>>mip);
                var target=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};target.Create();
                probe.SetTexture(k,"Result",target);probe.SetInts("Size",w,h);
                probe.SetVector("Bounds",new Vector4(0,0,322f/512,1));
                probe.SetVector("Gradient",new Vector4(322f/(512*w),0,0,1f/h));
                probe.Dispatch(k,(w+7)/8,(h+7)/8,1);
                var read=AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32);read.WaitForCompletion();
                if(read.hasError)throw new Exception("Independent footprint probe readback failed.");
                var reference=read.GetData<Color32>();var a=actual.GetPixels32(mip);var old=scalar.GetPixels32(mip);
                int mipError=0,mipDifference=0;
                for(int i=0;i<a.Length;i++)for(int c=0;c<4;c++)
                {
                    mipError=Math.Max(mipError,Math.Abs(a[i][c]-reference[i][c]));
                    mipDifference=Math.Max(mipDifference,Math.Abs(old[i][c]-reference[i][c]));compared++;
                }
                maxError=Math.Max(maxError,mipError);scalarDifference=Math.Max(scalarDifference,mipDifference);
                Debug.Log($"Footprint mip={mip}; size={w}x{h}; sourceTexels={322d/w:R},{512d/h:R}; gradError8={mipError}; scalarError8={mipDifference}");
                target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
            if(maxError>1)throw new Exception("Producer disagrees with independent native two-axis footprint sampling.");
            if(scalarDifference<=1)throw new Exception("NPOT scalar-LOD negative control did not distinguish the old producer.");
            Debug.Log($"PASS two-axis native footprint: {compared} channel samples; maxGradError8={maxError}; maxScalarError8={scalarDifference}; original aniso16 retained.");
            UnityEngine.Object.DestroyImmediate(actual);UnityEngine.Object.DestroyImmediate(scalar);UnityEngine.Object.DestroyImmediate(source);
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
