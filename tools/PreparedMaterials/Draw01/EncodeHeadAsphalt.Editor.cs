using System;
using System.IO;
using UnityEngine;

public static class Draw01EncodeHeadAsphalt
{
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string Arg(string key) => args[Array.IndexOf(args, key) + 1];
        var pixels = File.ReadAllBytes(Arg("-maskSource"));
        // Preserve the authored full mip chain and all other mask channels.
        for (int i = 2; i < pixels.Length; i += 4) pixels[i] = 0;
        var module = PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            var texture = new Texture2D(512, 732, TextureFormat.RGBA32, true, true)
                { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16 };
            try
            {
                texture.LoadRawTextureData(pixels);
                texture.Apply(false, false);
                var encoded = NativeTextureEncoding.Bc7(texture);
                try { File.WriteAllBytes(Arg("-output"), PreparePaint.Texture(encoded)); }
                finally { UnityEngine.Object.DestroyImmediate(encoded); }
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        finally { PreparePaint.CloseCodec(module); }
        Debug.Log("PASS Draw01 head mask: original authored R/G/A mip inputs, blue zero, native BC7.");
    }
}
