using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Uses the installed game's real binary readers. This verifies the native file
// formats and GPU texture upload, not Prefab publication or in-game appearance.
public static class Lift05PureRoadNativeCheck
{
    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32")] private static extern bool FreeLibrary(IntPtr module);
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string managed = args[Array.IndexOf(args, "-gameManaged") + 1];
        string folder = args[Array.IndexOf(args, "-preparedStructures") + 1];
        var plugin = LoadLibraryW(Path.GetFullPath(Path.Combine(managed, "../Plugins/x86_64/PipelinePlugin.dll")));
        if (plugin == IntPtr.Zero) throw new InvalidOperationException("Native compression library missing.");
        ResolveEventHandler resolve = (_, e) => {
            var path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        try
        {
            var database = Assembly.LoadFrom(Path.Combine(managed, "Colossal.IO.AssetDatabase.dll"));
            var core = Assembly.LoadFrom(Path.Combine(managed, "Colossal.Core.dll"));
            var io = Assembly.LoadFrom(Path.Combine(managed, "Colossal.IO.dll"));
            var pipeline = Assembly.LoadFrom(Path.Combine(managed, "Colossal.AssetPipeline.Native.dll"));
            var compression = pipeline.GetType("Colossal.AssetPipeline.Native.CompressionUtilities", true);
            var decompress = compression.GetMethods().Single(m => m.Name == "Decompress" && m.GetParameters()[0].ParameterType == typeof(byte[]));
            var zstd = Enum.Parse(decompress.GetParameters()[1].ParameterType, "ZSTD");
            var surfaceType = database.GetType("Colossal.IO.AssetDatabase.SurfaceAsset", true);
            var textureFiles = new System.Collections.Generic.List<string>();
            int structures = 0;
            if (args.Contains("-preparedStructures"))
            {
                var structuralRoot = args[Array.IndexOf(args, "-preparedStructures") + 1];
                var structuralTextures = Directory.GetFiles(structuralRoot, "*.Texture", SearchOption.AllDirectories);
                var identities = structuralTextures.Select(p => File.ReadAllText(p+".cid").Trim()).ToHashSet();
                foreach (var file in Directory.GetFiles(structuralRoot, "*.Surface", SearchOption.AllDirectories))
                {
                    var surface = FormatterServices.GetUninitializedObject(surfaceType);
                    using var reader = new BinaryReader(File.OpenRead(file));
                    surfaceType.GetMethod("ReadHeader", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, new object[] { reader });
                    Require(!(bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface), "structural Repeat uses native non-VT");
                    var binary = io.GetType("Colossal.IO.BinaryReaderExtensions", true);
                    foreach (var method in new[] { "ReadStringFloatDictionary", "ReadStringIntDictionary", "ReadStringVectorDictionary", "ReadStringColorDictionary" })
                        binary.GetMethod(method).Invoke(null, new object[] { reader });
                    Require(reader.ReadByte() == 255, "structural native texture map");
                    int count = reader.ReadInt32(); Require(count > 0, "structural texture channels present");
                    var hashReader = core.GetType("Colossal.IO.BinaryReaderExtensions", true).GetMethod("ReadHash");
                    for (int i=0;i<count;i++)
                    {
                        reader.ReadString(); Require(reader.ReadByte()==255, "structural channel identity present");
                        Require(identities.Contains(hashReader.Invoke(null,new object[]{reader}).ToString()), "structural dependency available");
                    }
                    binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader});
                    Require(reader.BaseStream.Position==reader.BaseStream.Length,"complete native structural descriptor");
                    structures++;
                }
                textureFiles.AddRange(structuralTextures);
            }
            var textureType = database.GetType("Colossal.IO.AssetDatabase.TextureAsset", true);
            long mipBytes = 0;
            foreach (var file in textureFiles)
            {
                var asset = FormatterServices.GetUninitializedObject(textureType);
                using var reader = new BinaryReader(File.OpenRead(file));
                var version = textureType.GetMethod("ReadHeader", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(asset, new object[] { reader });
                Require((ushort)version == 5, "native texture version");
                int w = (int)textureType.GetProperty("width").GetValue(asset), h = (int)textureType.GetProperty("height").GetValue(asset);
                var format = (GraphicsFormat)textureType.GetProperty("format").GetValue(asset);
                var compressed = reader.ReadInt32();
                var bytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                if (compressed != -1)
                {
                    Require(compressed == bytes.Length, "native compressed payload length");
                    bytes = (byte[])decompress.Invoke(null, new[] { (object)bytes, zstd });
                }
                var flags = TextureCreationFlags.DontInitializePixels | TextureCreationFlags.IgnoreMipmapLimit;
                if (Math.Max(w,h) > 1) flags |= TextureCreationFlags.MipChain;
                var texture = new Texture2D(w, h, format, flags);
                try
                {
                    texture.LoadRawTextureData(bytes); texture.Apply(false, false);
                    Require(texture.GetRawTextureData<byte>().ToArray().SequenceEqual(bytes), "native texture GPU upload keeps every mip byte");
                    Require(texture.mipmapCount == 1+(int)Math.Floor(Math.Log(Math.Max(w,h),2)), "native full mip chain");
                    mipBytes += bytes.Length;
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            Require(structures == 3 && textureFiles.Count == 10, "Expected pure road graph");
            Debug.Log($"PASS Lift05 pure road native: {structures} SurfaceAsset descriptors, {textureFiles.Count} TextureAsset payloads, {mipBytes} complete mip bytes uploaded unchanged.");
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolve; FreeLibrary(plugin); }
    }
    private static void Require(bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
}
