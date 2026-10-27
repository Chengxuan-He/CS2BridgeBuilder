using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BridgeBuilder.Bridges;
using UnityEngine;

// Offline authoring only. The output is native Surface v1 / Texture v5, not a
// custom runtime format. The game can load these assets without Bridge Builder.
public static class PreparePaint
{
    private const string SourceCid = "4b5500b1b5c270890d99149f7f62e51a";
    private const string Recipe = "BridgeBuilder/SkinnedPaint/1";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long Decode(IntPtr source, long size, IntPtr target, long capacity, int format);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate long Bound(long size, int format);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate long Encode(IntPtr source, long size, IntPtr target, long capacity, int format, int level);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32", CharSet = CharSet.Ansi)] private static extern IntPtr GetProcAddress(IntPtr module, string name);
    [DllImport("kernel32")] private static extern bool FreeLibrary(IntPtr module);
    private static Decode decode;
    private static Bound bound;
    private static Encode encode;

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string Argument(string name) => args[Array.IndexOf(args, name) + 1];
        string game = Argument("-gameData"), output = Argument("-preparedOutput");
        if (QualitySettings.activeColorSpace != ColorSpace.Linear)
            throw new InvalidOperationException("Prepare native paint in the game's linear color space.");
        var module = OpenCodec(game);
        var images = new List<Texture2D>();
        try
        {
            var source = ReadAsset(game, SourceCid, "Blob.cok");
            if (Digest(source) != "6bb8e926e34c3b39f8ad0cff6d8a893b5c8e305c0ace1370438d2614f799895f")
                throw new InvalidDataException("Native paint source version changed; review the recipe and sampling coordinates.");
            if (BitConverter.ToUInt16(source, 0) != 1 || source[6] != 1)
                throw new InvalidDataException("Expected the measured native VT paint surface.");
            // DefaultDecal's first stack/layer is _BaseColorMap (native material library).
            string textureCid = HashText(source, 83);
            Texture2D input;
            if (textureCid != new string('0', 32))
            {
                using var stream = new MemoryStream(ReadAsset(game, textureCid, "VT.cok"));
                input = MovableVtTextureSource.ReadTexture(stream, Decompress);
                images.Add(input);
            }
            else
            {
                if (source[283] != 255) throw new InvalidDataException("Paint has no native backing texture.");
                using var stream = new MemoryStream(ReadAsset(game, HashText(source, 284), "VT.cok"));
                var layers = MovableVtTextureSource.ReadSurface(stream, Decompress);
                images.AddRange(layers.Values);
                input = layers[(0, 0)];
            }
            if (input.width != 2048 || input.height != 2048)
                throw new InvalidDataException("Paint atlas changed; remeasure native paint sample coordinates.");
            var color = DecodeColor(input);
            images.Add(color);
            var sourceDigest = Digest(source);
            var files = new Dictionary<string, (string Cid, byte[] Bytes)>();
            string Add(string name, byte[] bytes)
            {
                var cid = Digest(Encoding.UTF8.GetBytes(Recipe + "/" + SourceCid + "/" + sourceDigest + "/" + Digest(bytes))).Substring(0, 32);
                files.Add(name, (cid, bytes));
                return cid;
            }
            var colorCid = Add("SkinnedPaintColor.Texture", Texture(color));
            var zero = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            images.Add(zero); zero.SetPixel(0, 0, Color.clear); zero.Apply(false, false);
            var controlCid = Add("SkinnedPaintControl.Texture", Texture(zero));
            Add("SkinnedPaint.Surface", Surface(colorCid, controlCid));
            var styles = new[] { "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" };
            foreach (var style in styles)
            foreach (var item in files)
            {
                var path = Path.Combine(output, style, "Paint", item.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, item.Value.Bytes);
                File.WriteAllText(path + ".cid", item.Value.Cid, new UTF8Encoding(false));
                if (!File.ReadAllBytes(path).SequenceEqual(item.Value.Bytes)) throw new IOException("Prepared payload differs: " + path);
            }
            Debug.Log($"PASS prepared paint: {styles.Length} styles; {color.width}x{color.height}; {color.mipmapCount} mips; native source {SourceCid}; SHA256={sourceDigest}; recipe={Recipe}; surface={files["SkinnedPaint.Surface"].Cid}; no runtime baking.");
        }
        finally { foreach (var image in images) UnityEngine.Object.DestroyImmediate(image); CloseCodec(module); }
    }

    private static byte[] ReadAsset(string game, string cid, string archiveName)
    {
        foreach (var family in new[] { "Game", "BridgesAndPorts" })
        {
            using var archive = ZipFile.OpenRead(Path.Combine(game, "Content", family, archiveName));
            foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".cid", StringComparison.Ordinal)))
            {
                using var sidecar = new StreamReader(entry.Open());
                if (sidecar.ReadToEnd().Trim() != cid) continue;
                using var payload = archive.GetEntry(entry.FullName.Substring(0, entry.FullName.Length - 4)).Open();
                using var bytes = new MemoryStream(); payload.CopyTo(bytes); return bytes.ToArray();
            }
        }
        throw new FileNotFoundException("Native source CID not found: " + cid);
    }

    private static Texture2D DecodeColor(Texture2D source)
    {
        var previous = RenderTexture.active; var srgb = GL.sRGBWrite;
        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D result = null;
        try
        {
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Graphics.Blit(source, target); RenderTexture.active = target;
            result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false)
                { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = source.anisoLevel };
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply(true, false);
            return result;
        }
        catch { if (result != null) UnityEngine.Object.DestroyImmediate(result); throw; }
        finally { RenderTexture.active = previous; GL.sRGBWrite = srgb; RenderTexture.ReleaseTemporary(target); }
    }

    internal static byte[] Texture(Texture2D texture)
    {
        using var bytes = new MemoryStream(); using var writer = new BinaryWriter(bytes);
        writer.Write((ushort)5); writer.Write((ushort)texture.width); writer.Write((ushort)texture.height); writer.Write((ushort)1);
        writer.Write((byte)texture.mipmapCount); writer.Write((byte)texture.graphicsFormat); writer.Write((byte)texture.dimension);
        writer.Write((byte)texture.filterMode); writer.Write((byte)texture.wrapMode); writer.Write((byte)texture.anisoLevel);
        var raw = texture.GetRawTextureData<byte>().ToArray();
        var compressed = Compress(raw);
        writer.Write(compressed.Length < raw.Length ? compressed.Length : -1);
        writer.Write(compressed.Length < raw.Length ? compressed : raw);
        return bytes.ToArray();
    }

    private static byte[] Compress(byte[] raw)
    {
        int capacity = checked((int)bound(raw.Length, 0));
        if (capacity <= 0) throw new InvalidDataException("Native compression bound failed.");
        var source = Marshal.AllocHGlobal(raw.Length); var target = Marshal.AllocHGlobal(capacity);
        var restored = Marshal.AllocHGlobal(raw.Length);
        try
        {
            Marshal.Copy(raw, 0, source, raw.Length);
            int size = checked((int)encode(source, raw.Length, target, capacity, 0, 11));
            if (size <= 0 || size > capacity || decode(target, size, restored, raw.Length, 0) != raw.Length)
                throw new InvalidDataException("Native lossless texture compression failed.");
            var check = new byte[raw.Length]; Marshal.Copy(restored, check, 0, check.Length);
            if (!raw.SequenceEqual(check)) throw new InvalidDataException("Native compression changed mip bytes.");
            var result = new byte[size]; Marshal.Copy(target, result, 0, size); return result;
        }
        finally { Marshal.FreeHGlobal(source); Marshal.FreeHGlobal(target); Marshal.FreeHGlobal(restored); }
    }

    private static byte[] Surface(string color, string control)
    {
        using var bytes = new MemoryStream(); using var writer = new BinaryWriter(bytes);
        writer.Write((ushort)1); writer.Write(0); writer.Write(false); // Default, non-VT.
        var floats = new Dictionary<string, float> { ["_SupportDecals"] = 0, ["_Snow"] = 0, ["_Metallic"] = 0,
            ["_Smoothness"] = .35f, ["_DoubleSidedEnable"] = 0, ["_CullMode"] = 2, ["_CullModeForward"] = 2 };
        writer.Write((byte)255); writer.Write(floats.Count);
        foreach (var item in floats) { writer.Write(item.Key); writer.Write(item.Value); }
        for (var i = 0; i < 3; i++) { writer.Write((byte)255); writer.Write(0); } // Ints, vectors, colors.
        writer.Write((byte)255); writer.Write(2);
        foreach (var item in new[] { ("_BaseColorMap", color), ("_ControlMask", control) })
        { writer.Write(item.Item1); writer.Write((byte)255); writer.Write(HashBytes(item.Item2)); }
        writer.Write((byte)255); writer.Write(2); writer.Write("_TANGENTSPACE_OCTO"); writer.Write("_DISABLE_DECALS");
        return bytes.ToArray();
    }
    internal static byte[] HashBytes(string cid) => Enumerable.Range(0, 16).Select(i =>
        (byte)(Convert.ToInt32(cid[i * 2].ToString(), 16) | Convert.ToInt32(cid[i * 2 + 1].ToString(), 16) << 4)).ToArray();
    internal static string HashText(byte[] bytes, int offset) => string.Concat(bytes.Skip(offset).Take(16).Select(b => (b & 15).ToString("x") + (b >> 4).ToString("x")));
    internal static string Digest(byte[] bytes) { using var sha = SHA256.Create(); return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2"))); }
    internal static IntPtr OpenCodec(string game)
    {
        var module = LoadLibraryW(Path.Combine(game, "Plugins/x86_64/PipelinePlugin.dll"));
        if (module == IntPtr.Zero) throw new InvalidOperationException("Native texture codec is unavailable.");
        try
        {
            decode = Marshal.GetDelegateForFunctionPointer<Decode>(GetProcAddress(module, "decompress_data"));
            bound = Marshal.GetDelegateForFunctionPointer<Bound>(GetProcAddress(module, "compress_calc_bound"));
            encode = Marshal.GetDelegateForFunctionPointer<Encode>(GetProcAddress(module, "compress_data"));
            return module;
        }
        catch { FreeLibrary(module); throw; }
    }
    internal static void CloseCodec(IntPtr module) => FreeLibrary(module);
    internal static byte[] DecodeRaw(byte[] bytes, int length)
    {
        var source = Marshal.AllocHGlobal(bytes.Length); var target = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.Copy(bytes, 0, source, bytes.Length);
            if (decode(source, bytes.Length, target, length, 0) != length) throw new InvalidDataException("Native texture decode failed.");
            var result = new byte[length]; Marshal.Copy(target, result, 0, length); return result;
        }
        finally { Marshal.FreeHGlobal(source); Marshal.FreeHGlobal(target); }
    }
    internal static byte[] Decompress(byte[] bytes)
    {
        if (BitConverter.ToInt32(bytes, 0) != int.MaxValue || BitConverter.ToInt32(bytes, 8) != bytes.Length - 12)
            throw new InvalidDataException("Invalid native ZSTD tile.");
        int length = BitConverter.ToInt32(bytes, 4);
        var source = Marshal.AllocHGlobal(bytes.Length - 12); var target = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.Copy(bytes, 12, source, bytes.Length - 12);
            if (decode(source, bytes.Length - 12, target, length, 0) != length) throw new InvalidDataException("Native tile decode failed.");
            var result = new byte[length]; Marshal.Copy(target, result, 0, length); return result;
        }
        finally { Marshal.FreeHGlobal(source); Marshal.FreeHGlobal(target); }
    }
}
