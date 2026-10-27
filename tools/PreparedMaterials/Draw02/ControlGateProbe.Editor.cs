using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Readback-only GPU regression probe. No material baking or asset encoding.
public static class ControlGateProbe
{
    [Serializable] public class Comparison { public string name; public int width, height, differingBytes; public float maxError; }
    [Serializable] public class Report { public bool passed; public string unity, device, error; public List<Comparison> results = new List<Comparison>(); }
    static readonly string[] Inputs = { "_BaseColorMap", "_NormalMap", "_MaskMap", "_ControlMask" };
    static readonly string[] Outputs = { "AtlasColor", "AtlasNormal", "AtlasMask", "AtlasControl" };
    static readonly List<UnityEngine.Object> Objects = new List<UnityEngine.Object>();
    static T Keep<T>(T value) where T : UnityEngine.Object { Objects.Add(value); return value; }
    static string Argument(string name) { var a=Environment.GetCommandLineArgs(); return a[Array.IndexOf(a,name)+1]; }
    public static void Run()
    {
        var report = new Report { unity=Application.unityVersion, device=SystemInfo.graphicsDeviceVersion };
        int exit=1;
        try
        {
            foreach(var size in new[]{new Vector2Int(17,11),new Vector2Int(7,9),new Vector2Int(32,24)}) Check(size.x,size.y,report);
            report.passed=true; exit=0;
        }
        catch(Exception e) { report.error=e.ToString(); Debug.LogException(e); }
        finally
        {
            foreach(var o in Objects) if(o!=null) UnityEngine.Object.DestroyImmediate(o);
            File.WriteAllText(Argument("-gateReport"),JsonUtility.ToJson(report,true));
            EditorApplication.Exit(exit);
        }
    }
    static Texture2D Source(int channel)
    {
        var t=Keep(new Texture2D(13,7,TextureFormat.RGBA32,true,true));
        t.wrapMode=TextureWrapMode.Repeat; t.filterMode=FilterMode.Trilinear;
        var pixels=new Color[13*7];
        for(int y=0;y<7;y++) for(int x=0;x<13;x++)
        {
            // Authored control varies in all channels; normal stays valid under unpack().
            pixels[y*13+x]=channel==1 ? new Color(.45f+x*.005f,.45f+y*.009f,1,1)
                : new Color(((x*17+y*11+channel*31)%251)/255f,((x*7+y*29+channel*13)%239)/255f,((x*37+y*19+channel*23)%241)/255f,((x*23+y*31+83)%251)/255f);
        }
        t.SetPixels(pixels);t.Apply(true,false);return t;
    }
    static RenderTexture Target(int w,int h)
    {
        var t=Keep(new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true});t.Create();
        var previous=RenderTexture.active; RenderTexture.active=t;GL.Clear(false,true,new Color(.13f,.27f,.41f,.59f));RenderTexture.active=previous;return t;
    }
    static byte[] Read(RenderTexture t)
    {
        var previous=RenderTexture.active;RenderTexture.active=t;
        var image=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false,true);
        image.ReadPixels(new Rect(0,0,t.width,t.height),0,0);image.Apply(false,false);
        byte[] bytes=image.GetRawTextureData<byte>().ToArray();UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;return bytes;
    }
    static void Equal(string name,int w,int h,byte[] a,byte[] b,Report report)
    {
        if(a.Length!=b.Length)throw new Exception(name+": unequal lengths");
        int count=0,max=0;for(int i=0;i<a.Length;i++){int d=Math.Abs(a[i]-b[i]);if(d!=0)count++;max=Math.Max(max,d);}
        report.results.Add(new Comparison{name=name,width=w,height=h,differingBytes=count,maxError=max/255f});
        if(count!=0)throw new Exception(name+": differing bytes="+count);
    }
    static void Check(int w,int h,Report report)
    {
        var old=Keep(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ControlGateOld.compute")));
        var current=Keep(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ControlGateCurrent.compute")));
        var source=Enumerable.Range(0,4).Select(Source).ToArray();
        var oldTargets=Enumerable.Range(0,4).Select(_=>Target(w,h)).ToArray();
        var newTargets=Enumerable.Range(0,4).Select(_=>Target(w,h)).ToArray();
        using(var bands=new ComputeBuffer(2,16))
        {
            bands.SetData(new Vector4[2]);
            Bind(old,source,oldTargets,bands,w,h); Bind(current,source,newTargets,bands,w,h);
            Dispatch(old,w,h);Dispatch(current,w,h); // Intentionally never set SkipAtlasControl: test default zero.
            var baseline=oldTargets.Select(Read).ToArray();
            for(int c=0;c<4;c++)Equal("old-vs-default0/"+Outputs[c],w,h,baseline[c],Read(newTargets[c]),report);
            if(Enumerable.Range(0,w*h).Select(i=>BitConverter.ToUInt32(baseline[3],i*4)).Distinct().Count()<4)throw new Exception("Control baseline is not nonconstant.");
            var dummy=Target(1,1);var sentinel=Read(dummy);
            current.SetInt("SkipAtlasControl",1);current.SetTexture(current.FindKernel("SurfaceAtlas"),"AtlasControl",dummy);
            // Fresh targets prevent a failed dispatch from falsely passing by retaining previous pixels.
            for(int c=0;c<3;c++){newTargets[c]=Target(w,h);current.SetTexture(current.FindKernel("SurfaceAtlas"),Outputs[c],newTargets[c]);}
            Dispatch(current,w,h);
            for(int c=0;c<3;c++)Equal("skip1-vs-default0/"+Outputs[c],w,h,baseline[c],Read(newTargets[c]),report);
            Equal("skip1/dummy-sentinel",1,1,sentinel,Read(dummy),report);
            current.SetInt("SkipAtlasControl",0);
            for(int c=0;c<4;c++){newTargets[c]=Target(w,h);current.SetTexture(current.FindKernel("SurfaceAtlas"),Outputs[c],newTargets[c]);}
            Dispatch(current,w,h);
            for(int c=0;c<4;c++)Equal("same-program-1-to-0/"+Outputs[c],w,h,baseline[c],Read(newTargets[c]),report);
        }
    }
    static void Dispatch(ComputeShader shader,int w,int h){shader.Dispatch(shader.FindKernel("SurfaceAtlas"),(w+7)/8,(h+7)/8,1);}
    static void Bind(ComputeShader p,Texture2D[] source,RenderTexture[] targets,ComputeBuffer bands,int w,int h)
    {
        int k=p.FindKernel("SurfaceAtlas");
        for(int c=0;c<4;c++){p.SetTexture(k,Inputs[c],source[c]);p.SetTexture(k,Outputs[c],targets[c]);}
        p.SetInt("OriginalRoad",0);p.SetInt("ReplaceRoad",0);p.SetInt("RoadBandCount",0);p.SetInt("RoadBandRows",0);p.SetBuffer(k,"RoadBands",bands);
        for(int c=0;c<3;c++)p.SetTexture(k,"_Road"+Inputs[c],source[c]);
        foreach(var n in new[]{"_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"})p.SetTexture(k,n,Texture2D.whiteTexture);
        p.SetVector("ChartOrigin",Vector3.zero);p.SetVector("ChartU",Vector3.right);p.SetVector("ChartV",Vector3.forward);
        p.SetVector("PatchU",new Vector4(1,0,0,0));p.SetVector("PatchV",new Vector4(0,0,1,0));p.SetVector("PatchArea",new Vector4(.13f,.17f,.71f,.79f));
        p.SetVector("AtlasPixels",new Vector4(w,0,h,0));p.SetVector("AtlasRect",new Vector4(0,0,w,h));
    }
}
