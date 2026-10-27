using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

// Read-only native GPU sampling. No texture bake, serialization or mip generation.
public static class Draw01StructureAliasProbe
{
    [Serializable] public class Input { public Pair[] pairs; }
    [Serializable] public class Pair { public string oldKey,keepKey; public float[] matrix; public Channel[] channels; }
    [Serializable] public class Channel { public string name,oldPath,keepPath; public float minimumStretch,maximumStretch; }
    [Serializable] public class Result { public string oldKey,keepKey,channel,mode; public int oldWidth,oldHeight,keepWidth,keepHeight; public float oldMip,keepMip,interiorMax,seamMax,rms,maxNormalAngle; public float[] worstPoint,oldValue,keepValue; }
    [Serializable] public class Output { public string gpu,api; public int pairs; public List<Result> samples=new List<Result>(); }
    static string Arg(string name) {var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,name)+1];}
    static Texture2D Load(string path)
    {
        using var r=new BinaryReader(File.OpenRead(path));
        if(r.ReadUInt16()!=5)throw new InvalidDataException("Expected native v5");
        int w=r.ReadUInt16(),h=r.ReadUInt16(),depth=r.ReadUInt16(),mips=r.ReadByte();
        var format=(GraphicsFormat)r.ReadByte();int dimension=r.ReadByte();
        var filter=(FilterMode)r.ReadByte();var wrap=(TextureWrapMode)r.ReadByte();int aniso=r.ReadByte();
        if(depth!=1||dimension!=2||wrap!=TextureWrapMode.Repeat)throw new InvalidDataException("Expected repeating native 2D");
        var t=new Texture2D(w,h,format,mips,TextureCreationFlags.DontInitializePixels|TextureCreationFlags.IgnoreMipmapLimit|TextureCreationFlags.MipChain);
        int size=t.GetRawTextureData<byte>().Length,compressed=r.ReadInt32();
        var bytes=r.ReadBytes(compressed<0?size:compressed);
        if(compressed>=0)bytes=PreparePaint.DecodeRaw(bytes,size);
        if(bytes.Length!=size||r.BaseStream.Position!=r.BaseStream.Length)throw new InvalidDataException("Incomplete native mip chain");
        t.LoadRawTextureData(bytes);t.Apply(false,true);t.wrapMode=wrap;t.filterMode=filter;t.anisoLevel=aniso;
        return t;
    }
    static Vector3 Normal(Vector4 c) {var x=c.x*c.w*2-1;var y=c.y*2-1;return new Vector3(x,y,Mathf.Sqrt(1-Mathf.Min(1,x*x+y*y))).normalized;}
    static float[] V(Vector4 v)=>new[]{v.x,v.y,v.z,v.w};
    public static void Run()
    {
        var codec=PreparePaint.OpenCodec(Arg("-gameData"));
        try {
            var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("-aliasInput")));
            var output=new Output{gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceVersion,pairs=input.pairs.Length};
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/StructureAliasProbe.compute");int kernel=shader.FindKernel("Compare");
            foreach(var pair in input.pairs) foreach(var channel in pair.channels) {
                var old=Load(channel.oldPath);var kept=Load(channel.keepPath);
                try {
                    var points=new List<Vector2>();var rng=new System.Random(407);
                    for(int i=0;i<512;i++)points.Add(new Vector2((float)(rng.NextDouble()*4-2),(float)(rng.NextDouble()*4-2)));
                    int interiors=points.Count;
                    float step=1f/Math.Max(Math.Max(old.width,old.height),Math.Max(kept.width,kept.height));
                    for(int i=0;i<=64;i++)foreach(float d in new[]{0f,-step*.5f,step*.5f,-step*.01f,step*.01f}) {
                        float a=i/64f;points.Add(new Vector2(d,a));points.Add(new Vector2(a,d));points.Add(new Vector2(a,1-a+d));
                    }
                    using var pp=new ComputeBuffer(points.Count,8);pp.SetData(points);
                    using var values=new ComputeBuffer(points.Count*2,16);var read=new Vector4[points.Count*2];
                    shader.SetBuffer(kernel,"Points",pp);shader.SetBuffer(kernel,"Values",values);shader.SetInt("Count",points.Count);
                    shader.SetTexture(kernel,"Old",old);shader.SetTexture(kernel,"Kept",kept);
                    shader.SetVector("RowU",new Vector4(pair.matrix[0],pair.matrix[1],pair.matrix[2],0));
                    shader.SetVector("RowV",new Vector4(pair.matrix[3],pair.matrix[4],pair.matrix[5],0));
                    foreach(string mode in new[]{"same-index","footprint-adjusted","self-control"})
                    for(int level=0;level<=2*(old.mipmapCount-1);level++) {
                        bool self=mode=="self-control";
                        shader.SetTexture(kernel,"Kept",self?old:kept);
                        shader.SetVector("RowU",self?new Vector4(1,0,0,0):new Vector4(pair.matrix[0],pair.matrix[1],pair.matrix[2],0));
                        shader.SetVector("RowV",self?new Vector4(0,1,0,0):new Vector4(pair.matrix[3],pair.matrix[4],pair.matrix[5],0));
                        float oldMip=level*.5f,keepMip=mode!="footprint-adjusted"?oldMip:Mathf.Max(0,oldMip+Mathf.Log(channel.maximumStretch,2));
                        shader.SetFloat("OldMip",oldMip);shader.SetFloat("KeptMip",keepMip);shader.Dispatch(kernel,(points.Count+63)/64,1,1);values.GetData(read);
                        var result=new Result{oldKey=pair.oldKey,keepKey=pair.keepKey,channel=channel.name,mode=mode,oldWidth=old.width,oldHeight=old.height,keepWidth=kept.width,keepHeight=kept.height,oldMip=oldMip,keepMip=keepMip};
                        double sum=0;float worst=-1;
                        for(int i=0;i<points.Count;i++) {
                            var a=read[i*2];var b=read[i*2+1];var d=a-b;
                            if(float.IsNaN(d.sqrMagnitude))throw new InvalidDataException("Non-finite GPU output");
                            float error=Mathf.Max(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z),Mathf.Abs(d.w));sum+=d.sqrMagnitude;
                            if(i<interiors)result.interiorMax=Mathf.Max(result.interiorMax,error);else result.seamMax=Mathf.Max(result.seamMax,error);
                            if(channel.name=="_NormalMap")result.maxNormalAngle=Mathf.Max(result.maxNormalAngle,Vector3.Angle(Normal(a),Normal(b)));
                            if(error>worst){worst=error;result.worstPoint=new[]{points[i].x,points[i].y};result.oldValue=V(a);result.keepValue=V(b);}
                        }
                        result.rms=(float)Math.Sqrt(sum/(4*points.Count));output.samples.Add(result);
                    }
                } finally {UnityEngine.Object.DestroyImmediate(old);UnityEngine.Object.DestroyImmediate(kept);}
                Debug.Log("Alias sampled "+pair.oldKey+" "+channel.name);
            }
            File.WriteAllText(Arg("-aliasOutput"),JsonUtility.ToJson(output,true));Debug.Log("PASS Draw01 alias native sampling: "+output.samples.Count+" channel/mip comparisons");
        } finally {PreparePaint.CloseCodec(codec);}
    }
}
