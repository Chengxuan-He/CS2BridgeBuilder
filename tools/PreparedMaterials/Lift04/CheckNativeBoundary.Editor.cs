using System;
using System.IO;
using System.Linq;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;

// Small original-GPU counterexample: native atlas boundary versus the ideal
// opposite-edge blend imposed by a cropped repeating payload. No asset baking.
public static class CheckLift04NativeBoundary
{
    [Serializable] public sealed class Record { public string channel; public double maxDifference, meanDifference; public int samples; public double[] edgeMaximum, edgeMean; public Color[] original, repeated; }
    [Serializable] public sealed class Report { public string gpu, result; public Record[] records; }
    public static void Run()
    {
        string input=Environment.GetEnvironmentVariable("LIFT04_BOUNDARY_INPUT");
        string output=Environment.GetEnvironmentVariable("LIFT04_BOUNDARY_OUTPUT");
        var shader=Shader.Find("Hidden/Lift04NativeBoundary");
        if(shader==null) throw new Exception("Boundary shader missing.");
        var material=new Material(shader);
        var rows=new System.Collections.Generic.List<Record>();
        foreach(string path in Directory.GetFiles(input,"*.Texture"))
        {
            using var file=File.OpenRead(path);
            var texture=MovableTextureSource.Read(file,_=>throw new Exception("Unexpected compression."));
            var target=new RenderTexture(64,4,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
            var read=new Texture2D(64,4,TextureFormat.RGBAFloat,false,true);
            texture.filterMode=FilterMode.Bilinear;texture.anisoLevel=0;
            material.SetVector("_Bounds",new Vector4(.4697265625f,.8251953125f,.62548828125f,.9814453125f));
            Color[] Sample(int mode)
            {
                material.SetInt("_OutputMode",mode);Graphics.Blit(texture,target,material);
                var previous=RenderTexture.active;RenderTexture.active=target;
                read.ReadPixels(new Rect(0,0,64,4),0,0);read.Apply(false,false);RenderTexture.active=previous;
                return read.GetPixels();
            }
            var values=Sample(0).SelectMany(c=>new[]{(double)c.r,c.g,c.b,c.a}).ToArray();
            rows.Add(new Record{channel=Path.GetFileNameWithoutExtension(path),maxDifference=values.Max(),meanDifference=values.Average(),samples=values.Length,
                edgeMaximum=Enumerable.Range(0,4).Select(e=>values.Skip(e*256).Take(256).Max()).ToArray(),
                edgeMean=Enumerable.Range(0,4).Select(e=>values.Skip(e*256).Take(256).Average()).ToArray(),original=Sample(1),repeated=Sample(2)});
            UnityEngine.Object.DestroyImmediate(read);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);
        }
        UnityEngine.Object.DestroyImmediate(material);
        File.WriteAllText(output,JsonUtility.ToJson(new Report{gpu=SystemInfo.graphicsDeviceName,
            result="Original mip-zero atlas border versus ideal crop Repeat opposite-edge average; nonzero proves boundary footprints differ even before compression.",records=rows.ToArray()},true));
        Debug.Log("Lift04 original GPU boundary check complete.");
    }
}
