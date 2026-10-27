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
public static class PreparedNativeCheck
{
    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32")] private static extern bool FreeLibrary(IntPtr module);
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string managed = args[Array.IndexOf(args, "-gameManaged") + 1];
        string folder = args[Array.IndexOf(args, "-preparedPaint") + 1];
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
            foreach (var variant in new[] { "Paint", "Cable", "Road" })
            {
            var variantFolder = Path.Combine(Path.GetDirectoryName(folder), variant);
            bool paint = variant == "Paint";
            bool road = variant == "Road";
            var surface = FormatterServices.GetUninitializedObject(surfaceType);
            using (var reader = new BinaryReader(File.OpenRead(Path.Combine(variantFolder, paint ? "SkinnedPaint.Surface" : road ? "NativeAsphalt.Surface" : "SkinnedCable.Surface"))))
            {
                surfaceType.GetMethod("ReadHeader", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, new object[] { reader });
                Require((int)surfaceType.GetProperty("materialTemplateHash").GetValue(surface) == 0, "native Default template");
                Require(!(bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface), "native non-VT header");
                var binary = io.GetType("Colossal.IO.BinaryReaderExtensions", true);
                var floats = (IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null, new object[] { reader });
                Require((float)floats["_Snow"] == 0, "native snow parameter");
                if (!road) Require((float)floats["_Metallic"] == 0 && (float)floats["_Smoothness"] == (paint ? .35f : .5f), "native appearance parameters");
                if (paint) Require((float)floats["_CullMode"] == 2, "single-sided paint");
                foreach (var method in new[] { "ReadStringIntDictionary", "ReadStringVectorDictionary" })
                    Require(((IDictionary)binary.GetMethod(method).Invoke(null, new object[] { reader })).Count == 0, "native empty property map");
                var colors = (IDictionary)binary.GetMethod("ReadStringColorDictionary").Invoke(null, new object[] { reader });
                Require(colors.Count == (paint || road ? 0 : 1), "native colors");
                if (!paint && !road) Require(((Color)colors["_BaseColor"]).Equals(new Color(.018867909908294678f, .018867909908294678f, .018867909908294678f, 1)), "original cable color bits");
                Require(reader.ReadByte() == 255 && reader.ReadInt32() == (paint ? 2 : road ? 4 : 1), "native texture dictionary");
                var hashReader = core.GetType("Colossal.IO.BinaryReaderExtensions", true).GetMethod("ReadHash");
                if (road)
                foreach (var native in new[] { ("_BaseColorMap", "b00176216b0ce36579bce6fc675cfb77"),
                    ("_NormalMap", "9f446790360787756b0e5909db12e200"), ("_MaskMap", "375b871b66aaf675dacef141eb2a9810") })
                {
                    Require(reader.ReadString() == native.Item1 && reader.ReadByte() == 255, "native road texture property");
                    Require(hashReader.Invoke(null, new object[] { reader }).ToString() == native.Item2, "original road texture identity retained");
                }
                foreach (var item in paint ? new[] { ("_BaseColorMap", "SkinnedPaintColor.Texture"), ("_ControlMask", "SkinnedPaintControl.Texture") }
                    : new[] { ("_ControlMask", "ZeroControl.Texture") })
                {
                    Require(reader.ReadString() == item.Item1 && reader.ReadByte() == 255, "native texture property");
                    var hash = hashReader.Invoke(null, new object[] { reader });
                    Require(hash.ToString() == File.ReadAllText(Path.Combine(variantFolder, item.Item2 + ".cid")).Trim(), "native Hash128 reference identity");
                }
                var keywords = (IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null, new object[] { reader });
                Require(keywords.Cast<string>().OrderBy(x => x).SequenceEqual(paint ? new[] { "_DISABLE_DECALS", "_TANGENTSPACE_OCTO" } : new[] { "_TANGENTSPACE_OCTO" }), "native keywords");
                Require(reader.BaseStream.Position == reader.BaseStream.Length, "native descriptor consumed exactly");
            }
            }
            int lanes = 0;
            foreach (var file in Directory.GetFiles(Path.Combine(Path.GetDirectoryName(folder), "Lanes"), "*.Surface", SearchOption.AllDirectories))
            {
                var surface = FormatterServices.GetUninitializedObject(surfaceType);
                using var reader = new BinaryReader(File.OpenRead(file));
                surfaceType.GetMethod("ReadHeader", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, new object[] { reader });
                Require(new[] { 0, 1, 35 }.Contains((int)surfaceType.GetProperty("materialTemplateHash").GetValue(surface)), "native lane object template");
                Require((bool)surfaceType.GetProperty("isVTMaterial").GetValue(surface), "native lane VT header retained");
                var binary = io.GetType("Colossal.IO.BinaryReaderExtensions", true);
                var floats = (IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null, new object[] { reader });
                Require(floats.Contains("_Snow") && floats.Contains("_NormalScale"), "source lane appearance defaults persisted");
                foreach (var method in new[] { "ReadStringIntDictionary", "ReadStringVectorDictionary", "ReadStringColorDictionary" })
                    binary.GetMethod(method).Invoke(null, new object[] { reader });
                Require(reader.ReadByte() == 255, "native lane texture map");
                int textures = reader.ReadInt32();
                var hashReader = core.GetType("Colossal.IO.BinaryReaderExtensions", true).GetMethod("ReadHash");
                for (int i = 0; i < textures; i++)
                {
                    reader.ReadString(); byte marker = reader.ReadByte();
                    Require(marker == 0 || marker == 255, "native texture null marker");
                    if (marker == 255) hashReader.Invoke(null, new object[] { reader });
                }
                var keywords = (IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null, new object[] { reader });
                Require(keywords.Cast<string>().Contains("_TANGENTSPACE_OCTO"), "native lane skinning keyword");
                Require(reader.BaseStream.Position == reader.BaseStream.Length, "native lane descriptor consumed exactly");
                lanes++;
            }
            var textureFiles = Directory.GetFiles(Path.GetDirectoryName(folder), "*.Texture", SearchOption.AllDirectories).ToList();
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
            Debug.Log($"PASS native prepared readers: game SurfaceAsset/TextureAsset headers, Colossal property maps and Hash128; {lanes} native VT lane descriptors; {structures} structural descriptors; {mipBytes} mip bytes uploaded unchanged; no game scene created.");
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolve; FreeLibrary(plugin); }
    }
    private static void Require(bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
}
