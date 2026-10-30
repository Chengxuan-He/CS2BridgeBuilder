using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;
public static class Draw02AtlasFilterProbe
{
    [Serializable] public sealed class Pair { public string name,source,derived,sourceColor,derivedColor; public float[] bounds,content; public bool normal; }
    [Serializable] public sealed class Manifest { public Pair[] pairs; }
    public static void Run()
    {
        var module=PreparePaint.OpenCodec("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data");
        try {
            var root=Environment.GetEnvironmentVariable("DRAW02_REPO")+"/artifacts/material-remap/Draw02/offline-atlas-units";
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(root+"/filter-manifest"+(Environment.GetEnvironmentVariable("DRAW02_FILTER_SUFFIX")??"")+".json"));
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/AtlasFilterProbe.compute");
            var kernel=shader.FindKernel("Compare");const int count=129;
            using var buffer=new ComputeBuffer(count*count,16);var errors=new Vector4[count*count];
            shader.SetBuffer(kernel,"Errors",buffer);shader.SetInt("Count",count);
            using var writer=new StreamWriter(root+"/native-filter-check"+(Environment.GetEnvironmentVariable("DRAW02_FILTER_SUFFIX")??"")+".tsv");
            writer.WriteLine("pair\tlod\tanisotropy\tmaxRGB\tmaxAlpha\tmaxNormalVector\talphaMismatch\tinteriorMaxNormalVector\tvisibleMaxNormalVector\tboundaryRGB\tboundaryAlpha\tboundaryNormal\tboundedInteriorRGB\tboundedInteriorAlpha\tboundedInteriorNormal\tboundedInteriorSamples");
            foreach(var pair in manifest.pairs) {
                var source=Read(pair.source);var derived=Read(pair.derived);var sourceColor=Read(pair.sourceColor);var derivedColor=Read(pair.derivedColor);
                try {
                    source.filterMode=derived.filterMode=FilterMode.Trilinear;
                    source.wrapMode=TextureWrapMode.Clamp;derived.wrapMode=TextureWrapMode.Repeat;
                    source.anisoLevel=derived.anisoLevel=16;
                    sourceColor.filterMode=derivedColor.filterMode=FilterMode.Trilinear;
                    sourceColor.wrapMode=TextureWrapMode.Clamp;derivedColor.wrapMode=TextureWrapMode.Repeat;
                    sourceColor.anisoLevel=derivedColor.anisoLevel=16;
                    shader.SetTexture(kernel,"Source",source);shader.SetTexture(kernel,"Derived",derived);
                    shader.SetTexture(kernel,"SourceColor",sourceColor);shader.SetTexture(kernel,"DerivedColor",derivedColor);
                    shader.SetVector("SourceBounds",new Vector4(pair.bounds[0],pair.bounds[1],pair.bounds[2],pair.bounds[3]));
                    shader.SetVector("ContentBounds",new Vector4(pair.content[0],pair.content[1],pair.content[2],pair.content[3]));
                    shader.SetInt("IsNormal",pair.normal?1:0);
                    foreach(var aniso in new[]{1f,4f,16f})for(var half=0;half<=24;half++) {
                        shader.SetFloat("Lod",half*.5f);shader.SetFloat("Anisotropy",aniso);
                        shader.Dispatch(kernel,(count+7)/8,(count+7)/8,1);buffer.GetData(errors);
                        float rgb=0,alpha=0,n=0,inner=0,visible=0,br=0,ba=0,bn=0,ir=0,ia=0,inn=0;int mismatch=0,interiorSamples=0;
                        for(var i=0;i<errors.Length;i++) {var e=errors[i];rgb=Math.Max(rgb,e.x);alpha=Math.Max(alpha,e.y);n=Math.Max(n,e.z);if(pair.normal)visible=Math.Max(visible,e.w);else mismatch+=(int)e.w;
                            var x=i%count;var y=i/count;if(x>2&&x<count-3&&y>2&&y<count-3)inner=Math.Max(inner,e.z);
                            if(y==0||y==count-1){br=Math.Max(br,e.x);ba=Math.Max(ba,e.y);bn=Math.Max(bn,e.z);}
                            // Conservative two-footprint exclusion in both source axes; empty coarse interiors stay explicit.
                            float qx=(float)x/(count-1),qy=(float)y/(count-1),pad=2*Mathf.Pow(2,half*.5f);
                            float sx=(pair.bounds[2]-pair.bounds[0])*4096,sy=(pair.bounds[3]-pair.bounds[1])*4096;
                            if(Math.Min(qx,1-qx)*sx>pad&&Math.Min(qy,1-qy)*sy>pad){ir=Math.Max(ir,e.x);ia=Math.Max(ia,e.y);inn=Math.Max(inn,e.z);interiorSamples++;}}
                        writer.WriteLine(FormattableString.Invariant($"{pair.name}\t{half*.5f}\t{aniso}\t{rgb:R}\t{alpha:R}\t{n:R}\t{mismatch}\t{inner:R}\t{visible:R}\t{br:R}\t{ba:R}\t{bn:R}\t{ir:R}\t{ia:R}\t{inn:R}\t{interiorSamples}"));
                    }
                } finally {UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(derived);UnityEngine.Object.DestroyImmediate(sourceColor);UnityEngine.Object.DestroyImmediate(derivedColor);}
            }
            writer.Flush();Debug.Log("PASS native filter comparison instrument completed; no quality threshold applied.");EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally {PreparePaint.CloseCodec(module);}
    }
    static Texture2D Read(string path) {
        var b=File.ReadAllBytes(path);var f=(UnityEngine.Experimental.Rendering.GraphicsFormat)b[9];
        int bw=(int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockWidth(f),bh=(int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockHeight(f),bs=(int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockSize(f),size=0;
        for(int m=0;m<b[8];m++)size+=((Math.Max(1,BitConverter.ToUInt16(b,2)>>m)+bw-1)/bw)*((Math.Max(1,BitConverter.ToUInt16(b,4)>>m)+bh-1)/bh)*bs;
        using var s=new MemoryStream(b);return MovableTextureSource.Read(s,data=>PreparePaint.DecodeRaw(data,size));
    }
}
