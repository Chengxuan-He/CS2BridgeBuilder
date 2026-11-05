using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class Draw01EncodeAsphaltRepeat
{
    static string Arg(string name) { var a=Environment.GetCommandLineArgs(); return a[Array.IndexOf(a,name)+1]; }
    public static void Run()
    {
        string output=Arg("-output");
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            for(int channel=0;channel<4;channel++)
            {
                var texture=new Texture2D(512,732,TextureFormat.RGBA32,true,channel!=0)
                    {wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=16};
                try
                {
                    texture.LoadRawTextureData(File.ReadAllBytes(Path.Combine(output,$"asphalt-{channel}.rgba")));
                    texture.Apply(false,false);
                    var encoded=NativeTextureEncoding.Bc7(texture);
                    try { File.WriteAllBytes(Path.Combine(output,$"asphalt-{channel}.Texture"),PreparePaint.Texture(encoded)); }
                    finally { UnityEngine.Object.DestroyImmediate(encoded); }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
        finally { PreparePaint.CloseCodec(module); }
        Debug.Log("PASS Draw01 authored asphalt repeat: four channels, complete periodic mip chain.");
    }
    public static void Descriptor()
    {
        var bindings=new Dictionary<string,string> {
            ["_BaseColorMap"]=Arg("-color"), ["_NormalMap"]=Arg("-normal"),
            ["_MaskMap"]=Arg("-mask"), ["_ControlMask"]=Arg("-control") };
        var method=typeof(Draw01PrepareFiniteEnds).GetMethod("Surface",BindingFlags.Static|BindingFlags.NonPublic);
        File.WriteAllBytes(Arg("-output"),(byte[])method.Invoke(null,new object[]{File.ReadAllBytes(Arg("-source")),bindings,1,1}));
        Debug.Log("PASS Draw01 native asphalt repeat descriptor.");
    }
}
