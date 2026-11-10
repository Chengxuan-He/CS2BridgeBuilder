using System;
using System.IO;
using BridgeBuilder.Bridges;
using UnityEngine;
using UnityEngine.Rendering;

// Run in the existing Unity GPU harness with the production MovableTextureSource.
public static class TextureSourceCheck
{
    public static void Run()
    {
        var source = new Texture2D(16, 8, TextureFormat.RGBA32, 4, true);
        var read = new Texture2D(16, 8, TextureFormat.RGBA32, false, true);
        var target = new RenderTexture(16, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var oldTarget = RenderTexture.active;
        int oldLimit = QualitySettings.globalTextureMipmapLimit, samples = 0;
        try
        {
            for (int mip = 0; mip < source.mipmapCount; mip++)
            {
                int w = Math.Max(1, source.width >> mip), h = Math.Max(1, source.height >> mip);
                var pixels = new Color32[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = mip == 0 ? new Color32((byte)((x % 2) * 255), (byte)((y % 2) * 255), 73, 255) :
                        new Color32(13, (byte)(mip * 40), 211, 255);
                source.SetPixels32(pixels, mip);
            }
            source.Apply(false, false);
            var raw = source.GetRawTextureData<byte>().ToArray();
            foreach (int limit in new[] { 0, 2 })
            foreach (ushort version in new ushort[] { 1, 2, 3, 4, 5 })
            {
                QualitySettings.globalTextureMipmapLimit = limit;
                using var stream = new MemoryStream();
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(version); writer.Write((ushort)16); writer.Write((ushort)8);
                    if (version >= 4) writer.Write((ushort)1);
                    writer.Write((byte)source.mipmapCount);
                    if (version >= 3) writer.Write((byte)source.graphicsFormat); else writer.Write(source.graphicsFormat.ToString());
                    if (version >= 4) writer.Write((byte)TextureDimension.Tex2D);
                    if (version >= 2) { writer.Write((byte)FilterMode.Point); writer.Write((byte)TextureWrapMode.Repeat); writer.Write((byte)16); }
                    if (version >= 5) writer.Write(-1);
                    writer.Write(raw);
                }
                stream.Position = 0;
                var decoded = (MovableTextureSource.TryRead(stream, _ => throw new Exception("Unexpected compression."),out var parsedTexture1,out var textureReadError1)?parsedTexture1:throw new System.IO.InvalidDataException(textureReadError1));
                try
                {
                    if (decoded.width != 16 || decoded.height != 8 || decoded.mipmapCount != 4 || decoded.graphicsFormat != source.graphicsFormat)
                        throw new Exception("Native dimensions, format or partial mip chain changed.");
                    decoded.filterMode = FilterMode.Point;
                    Graphics.Blit(decoded, target);
                    RenderTexture.active = target;
                    read.ReadPixels(new Rect(0, 0, 16, 8), 0, 0); read.Apply();
                    var actual = read.GetPixels32(); var expected = source.GetPixels32(0);
                    for (int i = 0; i < actual.Length; i++)
                    {
                        if (!actual[i].Equals(expected[i])) throw new Exception($"Mip-zero pixel mismatch: version={version}, quality={limit}, pixel={i}, actual={actual[i]}, expected={expected[i]}");
                        samples++;
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(decoded); }
                var truncated = stream.ToArray(); Array.Resize(ref truncated, truncated.Length - 1);
                int before=Resources.FindObjectsOfTypeAll<Texture2D>().Length;
                using(var broken=new MemoryStream(truncated))
                    if(MovableTextureSource.TryRead(broken,_=>throw new Exception("decode fault"),out var bad,out var cause)||bad!=null||string.IsNullOrEmpty(cause))
                        throw new Exception("Truncated source did not return a clean failure.");
                if(Resources.FindObjectsOfTypeAll<Texture2D>().Length!=before)throw new Exception("Truncated source leaked a texture.");
                if(version==5)
                {
                    var packed=stream.ToArray();BitConverter.GetBytes(raw.Length).CopyTo(packed,14);
                    // The callback contract is independently checked; native ZSTD decompression is exercised by material integration tests.
                    using(var compressed=new MemoryStream(packed))
                    {
                        if(!MovableTextureSource.TryRead(compressed,_=>raw,out var ok,out var cause))throw new Exception(cause);
                        UnityEngine.Object.DestroyImmediate(ok);
                    }
                    foreach(var kind in new[]{0,1})
                    {
                        using var compressed=new MemoryStream(packed);
                        if(MovableTextureSource.TryRead(compressed,b=>kind==0?Array.Empty<byte>():throw new Exception("injected codec failure"),out var bad,out var cause)||bad!=null||string.IsNullOrEmpty(cause))
                            throw new Exception("Codec fault did not return a clean failure.");
                    }
                    if(Resources.FindObjectsOfTypeAll<Texture2D>().Length!=before)throw new Exception("Codec failure leaked a texture.");
                }
            }
            Debug.Log($"PASS native texture full-resolution reader: {samples} exact top-mip pixels; versions 1-5; quality limits 0/2; partial mip chain preserved; truncated sources and codec faults return failures without retained private textures.");
        }
        finally
        {
            QualitySettings.globalTextureMipmapLimit = oldLimit;
            RenderTexture.active = oldTarget;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(read);
        }
    }
}
