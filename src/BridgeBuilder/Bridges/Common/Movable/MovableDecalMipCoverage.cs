using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BridgeBuilder.Bridges;

// Request-owned, chart-local derivatives. Original assets and geometry UVs are
// untouched; native UV0 scale/offset supplies the inverse integer-pixel crop.
internal static class MovableDecalMipCoverage
{
    private const long MaximumPixels = 4L * 1024 * 1024;

    internal static bool TryPrepare(SurfaceAsset surface, Vector4 chart, string owner,
        PreviewGeometry? preview, List<AssetData> assets, out string? error)
    {
        error=null;
        var timer=System.Diagnostics.Stopwatch.StartNew();long rawBytes=0;
        var loaded=new List<TextureAsset>();
        var channels=new Dictionary<string,(TextureAsset Asset,Texture2D Pixels,bool Constant)>();
        bool properties=false;
        try
        {
            surface.LoadProperties(true);properties=true;
            foreach(var pair in surface.textures)
            {
                if(pair.Value==null)continue;
                loaded.Add(pair.Value);
                if(pair.Value.Load() is not Texture2D pixels)
                {error="Projected chart requires a native 2D channel: "+pair.Key;return false;}
                bool constant=false;
                if(pixels.width<=4 && pixels.height<=4)
                {
                    var probe=Capture(pixels,0,0,pixels.width,pixels.height);
                    try{var values=probe.GetPixels32();constant=values.All(v=>v.Equals(values[0]));}
                    finally{UnityEngine.Object.DestroyImmediate(probe);}
                }
                channels.Add(pair.Key,(pair.Value,pixels,constant));
            }
            if(!channels.TryGetValue("_BaseColorMap",out var albedo) || albedo.Constant)return true;
            var template=surface.GetTemplateMaterial();
            Vector4 priorScale=surface.vectors.TryGetValue("_UV0Scale",out var s)?s:template.GetVector("_UV0Scale");
            Vector4 priorOffset=surface.vectors.TryGetValue("_UV0Offset",out var t)?t:template.GetVector("_UV0Offset");
            int grid=0;
            foreach(var value in channels.Values)if(!value.Constant)grid=Gcd(grid,value.Pixels.width);
            float a=chart.x*priorScale.x+priorOffset.x,b=chart.z*priorScale.x+priorOffset.x;
            float min=Math.Min(a,b),max=Math.Max(a,b);
            if(min<0||max>1||max<=min||grid==0)
            {Mod.Log.Warn("Projected coverage derivative retained source: chart is not a bounded horizontal interval.");return true;}
            int left=Math.Max(0,(int)Math.Floor(min*grid)-2);
            int right=Math.Min(grid,(int)Math.Ceiling(max*grid)+2);
            foreach(var value in channels.Values)
                if(!value.Constant && (long)(right-left)*(value.Pixels.width/grid)*value.Pixels.height>MaximumPixels)
                {Mod.Log.Warn("Projected coverage derivative retained source: chart exceeds bounded pixel capacity.");return true;}
            var output=new Dictionary<string,TextureAsset>();
            var database=preview?.NativeAssets??AssetDatabase.user;
            foreach(var pair in channels)
            {
                if(pair.Value.Constant){output.Add(pair.Key,pair.Value.Asset);continue;}
                var source=pair.Value.Pixels;
                int first=left*(source.width/grid),width=(right-left)*(source.width/grid);
                Texture2D? pixels=Capture(source,first,0,width,source.height);
                try
                {
                    // Native TextureAsset persists one wrap mode for all axes.
                    // Keep the source longitudinal repeat; chart guards isolate U.
                    pixels.wrapMode=source.wrapMode;
                    pixels.filterMode=source.filterMode;pixels.anisoLevel=source.anisoLevel;
                    if(pair.Key=="_BaseColorMap")PreserveCoverage(pixels);
                    rawBytes+=pixels.GetRawTextureData<byte>().Length;
                    var asset=database.AddAsset<TextureAsset>(BridgePrefabPersistence.ResourcePath(owner+" Chart "+pair.Key,"Appearance"));
                    assets.Add(asset);asset.SetData(pixels);pixels=null;
                    try{asset.Save(compress:preview==null);}finally{asset.Unload();}
                    output.Add(pair.Key,asset);
                }
                finally{if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
            }
            surface.UpdateTextures(output);
            surface.UpdateVectors(new Dictionary<string,Vector4>{
                ["_UV0Scale"]=new Vector4(priorScale.x*grid/(right-left),priorScale.y,priorScale.z,priorScale.w),
                ["_UV0Offset"]=new Vector4((priorOffset.x*grid-left)/(right-left),priorOffset.y,priorOffset.z,priorOffset.w)});
            if(!surface.Save(mipBias:0,force:true,saveTextures:false,vt:false))
            {error="Native chart Surface save returned false.";return false;}
            Mod.Log.Info($"Projected chart coverage: owner={owner}; sourceChart={chart}; crop={left}/{grid}..{right}/{grid}; channels={output.Count}; rawMipBytes={rawBytes}; preparationMs={timer.Elapsed.TotalMilliseconds:F3}; original longitudinal repeat retained; lower-mip alpha coverage approximated.");
            return true;
        }
        catch(Exception exception){error="Projected chart coverage preparation: "+exception;return false;}
        finally
        {
            foreach(var asset in loaded)try{asset.Unload();}catch(Exception exception){error=(error??"")+"; texture release: "+exception;}
            if(properties)try{surface.UnloadProperties();}catch(Exception exception){error=(error??"")+"; Surface release: "+exception;}
        }
    }

    private static int Gcd(int a,int b){while(b!=0){int c=a%b;a=b;b=c;}return a;}

    private static Texture2D Capture(Texture2D source,int left,int bottom,int width,int height)
    {
        bool srgb=GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
        var prior=RenderTexture.active;
        bool priorWrite=GL.sRGBWrite;
        var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,srgb?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
        Texture2D? pixels=null;
        try
        {
            GL.sRGBWrite=srgb;
            Graphics.Blit(source,target,new Vector2((float)width/source.width,(float)height/source.height),new Vector2((float)left/source.width,(float)bottom/source.height));
            RenderTexture.active=target;
            pixels=new Texture2D(width,height,TextureFormat.RGBA32,true,!srgb);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0,false);
            // Sample each original authored mip explicitly. Do not regenerate PBR
            // RGB/normal/mask mips from level zero while correcting alpha coverage.
            for(int mip=1;mip<pixels.mipmapCount;mip++)
            {
                int level=Math.Min(mip,source.mipmapCount-1);
                int sw=Math.Max(1,source.width>>level),sh=Math.Max(1,source.height>>level);
                int w=Math.Max(1,width>>mip),h=Math.Max(1,height>>mip);
                bool subBlock=GraphicsFormatUtility.IsCompressedFormat(source.graphicsFormat)&&(sw<4||sh<4);
                var sample=subBlock?new Texture2D(sw,sh,TextureFormat.RGBA32,false,!srgb):new Texture2D(sw,sh,source.graphicsFormat,TextureCreationFlags.None);
                var small=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32,srgb?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
                Texture2D? readback=null;
                try
                {
                    if(subBlock)
                    {
                        if(!Graphics.ConvertTexture(source,sample))throw new InvalidOperationException("Native final-mip texture conversion failed.");
                    }
                    else Graphics.CopyTexture(source,0,level,sample,0,0);
                    sample.filterMode=source.filterMode;sample.wrapMode=source.wrapMode;sample.anisoLevel=source.anisoLevel;
                    Graphics.Blit(sample,small,new Vector2((float)width/source.width,(float)height/source.height),new Vector2((float)left/source.width,(float)bottom/source.height));
                    RenderTexture.active=small;readback=new Texture2D(w,h,TextureFormat.RGBA32,false,!srgb);
                    readback.ReadPixels(new Rect(0,0,w,h),0,0,false);readback.Apply(false,false);
                    pixels.SetPixels32(readback.GetPixels32(),mip);
                }
                finally
                {if(readback!=null)UnityEngine.Object.DestroyImmediate(readback);RenderTexture.ReleaseTemporary(small);UnityEngine.Object.DestroyImmediate(sample);}
            }
            pixels.Apply(false,false);
            var result=pixels;pixels=null;return result;
        }
        finally
        {GL.sRGBWrite=priorWrite;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
    }

    private static void PreserveCoverage(Texture2D texture)
    {
        var first=texture.GetPixels32(0);
        double coverage=(double)first.Count(p=>p.a>=128)/first.Length;
        if(coverage==0)return;
        var alpha=first.Select(p=>p.a/255f).ToArray();int width=texture.width,height=texture.height;
        for(int mip=1;mip<texture.mipmapCount;mip++)
        {
            int w=Math.Max(1,width/2),h=Math.Max(1,height/2);var next=new float[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                // Exact source-area average for non-power-of-two cropped widths.
                double x0=(double)x*width/w,x1=(double)(x+1)*width/w,y0=(double)y*height/h,y1=(double)(y+1)*height/h,sum=0;
                for(int iy=(int)y0;iy<Math.Ceiling(y1);iy++)for(int ix=(int)x0;ix<Math.Ceiling(x1);ix++)
                    sum+=alpha[iy*width+ix]*(Math.Min(ix+1,x1)-Math.Max(ix,x0))*(Math.Min(iy+1,y1)-Math.Max(iy,y0));
                next[y*w+x]=(float)(sum/((x1-x0)*(y1-y0)));
            }
            var sorted=(float[])next.Clone();Array.Sort(sorted);
            int wanted=Math.Max(1,(int)Math.Round(coverage*next.Length));
            float threshold=sorted[Math.Max(0,sorted.Length-wanted)];
            if(threshold==0)threshold=sorted.FirstOrDefault(v=>v>0);
            float scale=threshold>0?.502f/threshold:1;
            var pixels=texture.GetPixels32(mip);
            for(int i=0;i<pixels.Length;i++)pixels[i].a=(byte)Mathf.Clamp(Mathf.RoundToInt(next[i]*scale*255),0,255);
            texture.SetPixels32(pixels,mip);alpha=next;width=w;height=h;
        }
        texture.Apply(false,false);
    }
}
