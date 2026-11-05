using System;
using System.IO;
using UnityEngine;

public static class Draw01EncodePartRepeats
{
    static string Arg(string n) { var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,n)+1]; }
    public static void Run()
    {
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            foreach(var part in new[]{("grating",492,768),("platform",512,768),("cap",640,768)})
            for(int c=0;c<4;c++)
            {
                var texture=new Texture2D(part.Item2,part.Item3,TextureFormat.RGBA32,true,c!=0)
                    {wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=16};
                try
                {
                    var path=Path.Combine(Arg("-output"),part.Item1,"channel-"+c);
                    texture.LoadRawTextureData(File.ReadAllBytes(path+".rgba"));texture.Apply(false,false);
                    var encoded=NativeTextureEncoding.Bc7(texture);
                    try {File.WriteAllBytes(path+".Texture",PreparePaint.Texture(encoded));}
                    finally {UnityEngine.Object.DestroyImmediate(encoded);}
                }
                finally {UnityEngine.Object.DestroyImmediate(texture);}
            }
        }
        finally {PreparePaint.CloseCodec(module);}
        Debug.Log("PASS Draw01 staged physical-part texture encoding; quality not asserted.");
    }
}
