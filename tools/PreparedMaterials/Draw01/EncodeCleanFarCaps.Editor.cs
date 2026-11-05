using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Offline authoring only; each input level was composed from its authored source mip.
public static class Draw01EncodeCleanFarCaps
{
    public static void Descriptor()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        var bindings=new Dictionary<string,string> {
            ["_BaseColorMap"]=Arg("-color"), ["_NormalMap"]=Arg("-normal"), ["_MaskMap"]=Arg("-mask") };
        var method=typeof(Draw01PrepareFiniteEnds).GetMethod("Surface",BindingFlags.Static|BindingFlags.NonPublic);
        var bytes=(byte[])method.Invoke(null,new object[]{File.ReadAllBytes(Arg("-source")),bindings,1,1});
        File.WriteAllBytes(Arg("-output"),bytes);
        Debug.Log("PASS Draw01 finite cap native non-VT descriptor.");
    }

    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string output=Arg("-output");
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            for(int channel=0;channel<3;channel++)
            {
                var texture=new Texture2D(512,512,TextureFormat.RGBA32,true,channel!=0);
                try
                {
                    texture.LoadRawTextureData(File.ReadAllBytes(Path.Combine(output,$"far-{channel}.rgba")));
                    texture.Apply(false,false);
                    var encoded=NativeTextureEncoding.Bc7(texture);
                    try { File.WriteAllBytes(Path.Combine(output,$"far-{channel}.Texture"),PreparePaint.Texture(encoded)); }
                    finally { UnityEngine.Object.DestroyImmediate(encoded); }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
        finally { PreparePaint.CloseCodec(module); }
        Debug.Log("PASS Draw01 finite cap encoding: three channels, ten authored mips.");
    }
}
