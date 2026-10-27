using System;
using System.IO;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;

// Exercise the production readback/mip/BC7 path and serialized payload reader.
// The test writes the documented native header, not a live game AssetDatabase.
public static class AtlasChannelCheck
{
    public static void Run()
    {
        var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/PackingProbe.compute");
        int oldLimit=QualitySettings.globalTextureMipmapLimit,checks=0,mips=0;
        try
        {
            foreach(var size in new[]{new Vector2Int(64,32),new Vector2Int(76,44),new Vector2Int(4,4)})
            foreach(int quality in new[]{0,2})
            foreach(int kind in new[]{0,1,2})
            {
                QualitySettings.globalTextureMipmapLimit=quality;
                var input=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false,true);
                var pixels=new Color32[size.x*size.y];
                for(int y=0;y<size.y;y++)for(int x=0;x<size.x;x++)
                {
                    byte v=(byte)((x+y)%2==0?0:255);
                    pixels[y*size.x+x]=kind==1?new Color32((byte)(v==0?64:191),128,255,255):new Color32(v,v,v,255);
                }
                input.SetPixels32(pixels);input.Apply(false,false);
                var source=new RenderTexture(size.x,size.y,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                source.Create();
                var previous=RenderTexture.active;
                Graphics.Blit(input,source);RenderTexture.active=previous;
                try
                {
                    using var channel=new MovableAtlasChannel(shader,source,kind,false);
                    channel.Read();
                    var texture=channel.Texture;
                    if(texture.width!=size.x || texture.height!=size.y)throw new Exception("Compressed dimensions changed.");
                    using var stream=new MemoryStream();
                    using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
                    {
                        writer.Write((ushort)5);writer.Write((ushort)size.x);writer.Write((ushort)size.y);writer.Write((ushort)1);
                        writer.Write((byte)texture.mipmapCount);writer.Write((byte)texture.graphicsFormat);
                        writer.Write((byte)UnityEngine.Rendering.TextureDimension.Tex2D);
                        writer.Write((byte)FilterMode.Point);writer.Write((byte)TextureWrapMode.Clamp);writer.Write((byte)8);writer.Write(-1);
                        writer.Write(texture.GetRawTextureData<byte>().ToArray());
                    }
                    stream.Position=0;
                    var decoded=MovableTextureSource.Read(stream,_=>throw new Exception("Unexpected compressed container."));
                    try
                    {
                        if(decoded.width!=size.x || decoded.height!=size.y || decoded.mipmapCount!=texture.mipmapCount)
                            throw new Exception("Serialized mip chain changed.");
                        Check(decoded,size.x,size.y,kind,true,ref checks);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(decoded);}
                    // Inspect every compressed mip independently through hardware BC7
                    // decoding, including partial blocks of rectangular NPOT pages.
                    for(int mip=1;mip<texture.mipmapCount;mip++)
                    {
                        int w=Math.Max(1,size.x>>mip),h=Math.Max(1,size.y>>mip);
                        // Standalone BC7 base levels must be block-aligned even
                        // though a valid parent texture has smaller/odd lower mips.
                        int decodedWidth=(w+3)/4*4,decodedHeight=(h+3)/4*4;
                        var one=new Texture2D(decodedWidth,decodedHeight,TextureFormat.BC7,false,kind!=0){filterMode=FilterMode.Point};
                        try
                        {
                            one.SetPixelData(texture.GetPixelData<byte>(mip),0);one.Apply(false,false);
                            Check(one,decodedWidth,decodedHeight,kind,false,ref checks);mips++;
                        }
                        finally{UnityEngine.Object.DestroyImmediate(one);}
                    }
                }
                finally{source.Release();UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(input);}
            }
            Debug.Log($"PASS native atlas channel: {checks} channel samples; {mips} lower mips; RGB/normal/mask; BC7 hardware decode; POT/NPOT pages; quality limits 0/2; serialized mip-zero resolution retained.");
        }
        finally{QualitySettings.globalTextureMipmapLimit=oldLimit;}
    }

    private static void Check(Texture2D texture,int width,int height,int kind,bool top,ref int checks)
    {
        var target=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var read=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
        var old=RenderTexture.active;
        try
        {
            Graphics.Blit(texture,target);RenderTexture.active=target;
            read.ReadPixels(new Rect(0,0,width,height),0,0);read.Apply(false,false);
            var actual=read.GetPixels32();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float value=(x+y)%2==0?0:255;
                // In a Gamma editor project hardware sampling does not decode sRGB;
                // the encoded average of black/white is 188, not byte-average 128.
                byte average=(byte)(kind==0 && QualitySettings.activeColorSpace==ColorSpace.Gamma?188:128);
                var expected=kind==1?new Color32((byte)(top?(value==0?64:191):128),128,255,255):
                    new Color32((byte)(top?value:average),(byte)(top?value:average),(byte)(top?value:average),255);
                var got=actual[y*width+x];
                for(int c=0;c<4;c++)
                {
                    if(Math.Abs(got[c]-expected[c])>3)
                        throw new Exception($"Native atlas channel mismatch: quality={QualitySettings.globalTextureMipmapLimit}; kind={kind}; top={top}; size={width}x{height}; pixel={x},{y}; actual={got}; expected={expected}.");
                    checks++;
                }
            }
        }
        finally{RenderTexture.active=old;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(read);}
    }
}
