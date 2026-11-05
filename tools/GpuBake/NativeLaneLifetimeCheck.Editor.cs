using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Execute the installed engine's real cache reservation and VTTexture Load paths.
// Only the database file-access interface is supplied by the fixture; native
// readers, allocations, atlas indices and texture loading are not mocked.
public static class NativeLaneLifetimeCheck
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Assembly native;
    static Type T(string n) => native.GetType("Colossal.IO.AssetDatabase." + n, true);
    static object New(Type t, params object[] args) => Activator.CreateInstance(t, All, null, args, null);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(All)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(value, args);
    static object Get(object value, string name)
    {
        var field = value.GetType().GetField(name, All);
        return field != null ? field.GetValue(value) : value.GetType().GetProperty(name, All).GetValue(value);
    }
    static void Set(object value, string name, object data) => value.GetType().GetField(name, All).SetValue(value, data);
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    sealed class Files : RealProxy
    {
        public readonly Dictionary<string, (object Asset, string Path)> Items = new Dictionary<string, (object, string)>();
        public Files(Type type) : base(type) { }
        public override IMessage Invoke(IMessage request)
        {
            var call = (IMethodCallMessage)request;
            try
            {
                var args = call.Args;
                string Key(object id) => id.GetType().GetField("guid")?.GetValue(id).ToString() ?? id.ToString();
                string path = args.Length > 0 && Items.TryGetValue(Key(args[0]), out var found) ? found.Path : null;
                object result;
                switch (call.MethodName)
                {
                    case "GetReadStream": result = File.OpenRead(path); break;
                    case "GetName": result = Path.GetFileNameWithoutExtension(path); break;
                    case "IsPersistent": result = true; break;
                    case "GetAsyncReadDescriptor":
                        var type = ((MethodInfo)call.MethodBase).ReturnType;
                        result = Activator.CreateInstance(type);
                        type.GetField("path").SetValue(result, path);
                        type.GetField("offset").SetValue(result, 0L);
                        break;
                    case "TryGetAsset":
                        bool available = Items.TryGetValue(Key(args[0]), out var item);
                        args[1] = available ? item.Asset : null;
                        result = available; break;
                    default: throw new NotSupportedException("Fixture database call: " + call.MethodName);
                }
                return new ReturnMessage(result, args, args.Length, call.LogicalCallContext, call);
            }
            catch (Exception e) { return new ReturnMessage(e, call); }
        }
    }
    public static void Run()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key) => args[Array.IndexOf(args, key) + 1];
            string managed = Arg("-gameManaged"), fixtures = Arg("-fixtures"), materials = Arg("-materials");
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                string file = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(file) ? Assembly.LoadFrom(file) : null;
            };
            native = Assembly.LoadFrom(Path.Combine(managed, "Colossal.IO.AssetDatabase.dll"));
            // Original Texture IDs in VTTexture headers are provenance-only. The
            // native reader creates its usual dummy when no original payload exists.
            var globalType = T("AssetDatabase");
            var global = FormatterServices.GetUninitializedObject(globalType);
            var databaseSet = globalType.GetField("m_Databases", All);
            databaseSet.SetValue(global, Activator.CreateInstance(databaseSet.FieldType));
            globalType.GetField("s_Instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, global);
            var hashType = Assembly.LoadFrom(Path.Combine(managed, "Colossal.Core.dll")).GetType("Colossal.Hash128", true);
            object Hash(string cid) => hashType.GetMethod("Parse", new[] { typeof(string) }).Invoke(null, new object[] { cid });
            var files = new Files(T("ILocalAssetDatabase"));
            object proxy = files.GetTransparentProxy();
            object Asset(string path, string typeName)
            {
                string cid = File.ReadAllText(path + ".cid").Trim();
                if (files.Items.TryGetValue(cid, out var existing)) return existing.Asset;
                var asset = New(T(typeName));
                asset.GetType().GetProperty("id").SetValue(asset, New(T("Identifier"), Hash(cid)));
                asset.GetType().GetProperty("database").SetValue(asset, proxy);
                files.Items.Add(cid, (asset, path));
                return asset;
            }
            foreach (string path in Directory.GetFiles(fixtures, "*.VTTexture", SearchOption.AllDirectories)) Asset(path, "VTTextureAsset");
            var cacheFiles = Directory.GetFiles(materials, "*.MidMips", SearchOption.AllDirectories);
            var cacheType = T("MidMipCacheAsset");
            var caches = Array.CreateInstance(cacheType, cacheFiles.Length);
            for (int i = 0; i < cacheFiles.Length; i++) caches.SetValue(Asset(cacheFiles[i], "MidMipCacheAsset"), i);
            var configType = T("VirtualTexturing.VirtualTexturingConfig");
            var config = ScriptableObject.CreateInstance(configType);
            var stackType = T("VirtualTexturing.StackData");
            var stacks = Array.CreateInstance(stackType, 2);
            for (int i = 0; i < stacks.Length; i++)
            {
                var stack = New(stackType);
                Set(stack, "stackName", "Fixture" + i);
                Set(stack, "layerFormats", new[] { GraphicsFormat.RGBA_BC7_SRGB, GraphicsFormat.RGBA_BC7_UNorm, GraphicsFormat.RGBA_BC7_UNorm, GraphicsFormat.RGBA_BC7_UNorm });
                stacks.SetValue(stack, i);
            }
            Set(config, "stackDatas", stacks);
            var streamingType = T("VirtualTexturing.TextureStreamingSystem");
            var streaming = FormatterServices.GetUninitializedObject(streamingType);
            streamingType.GetField("s_VirtualTexturingConfig", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, config);
            var vt = New(T("VirtualTexturing.VTDatabase"), config, 3, null);
            Set(streaming, "m_VtDatabase", vt);
            Set(streaming, "m_MipBias", 3);
            var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(T("TexturesAsyncLoader"));
            Set(streaming, "m_TexturesAsyncLoaders", Activator.CreateInstance(listType));
            var atlas = New(T("VirtualTexturing.AtlasMaterialsDatabase"), 512, 3, caches);
            if (cacheFiles.Length > 0) Call(atlas, "PreRegisterToVT", streaming);
            int successes = 0, negatives = 0, channels = 0, empty = 0, streamed = 0;
            foreach (string path in Directory.GetFiles(materials, "*.Surface", SearchOption.AllDirectories))
            {
                if (!File.Exists(path + ".cid")) continue;
                var surface = Asset(path, "SurfaceAsset");
                using (var reader = new BinaryReader(File.OpenRead(path))) Call(surface, "ReadHeader", reader);
                if (!(bool)Get(surface, "isVTMaterial") || (bool)Get(surface, "hasVTSurfaceAsset")) continue;
                var guids = ((IEnumerable)Get(surface, "preProcessedTextureGuids")).Cast<object>().Where(h => (bool)Get(h, "isValid")).ToArray();
                if (guids.Length == 0) { empty++; continue; }
                if (args.Contains("-cacheOnly"))
                {
                    string cacheCid = File.ReadAllText(path + ".cid").Trim();
                    if (!(bool)Call(atlas, "IsHandlingMaterial", Hash(cacheCid))) continue;
                    Require((bool)Call(atlas, "SetPreReservedIndex", surface), "Cache membership missing");
                    var cached = Get(surface, "PreReservedAtlassingInfos");
                    Call(surface, "LoadProperties", true);
                    Require((bool)Call(surface, "UnloadProperties", false), "Cache fixture last unload failed");
                    Call(surface, "LoadProperties", true);
                    Require(ReferenceEquals(Get(surface, "PreReservedAtlassingInfos"), cached), "Native cache reservation lost");
                    foreach (var entry in (IEnumerable)cached)
                        if ((int)Get(entry, "indexInStack") >= 0)
                            Require(Call(streaming, "GetTextureParamBlock", entry) != null, "Cache batch parameters missing");
                    Call(surface, "UnloadProperties", false);
                    successes++;
                    Debug.Log("PASS positive-cache native reservation survives unload/reload " + cacheCid);
                    continue;
                }
                Require(guids.All(h => files.Items.ContainsKey(h.ToString())), "Incomplete original VT carrier fixture: " + path);
                string cid = File.ReadAllText(path + ".cid").Trim();
                bool handled = (bool)Call(atlas, "IsHandlingMaterial", Hash(cid));
                if (handled)
                {
                    Require((bool)Call(atlas, "SetPreReservedIndex", surface), "Missing native reservation: " + cid);
                    var badLoader = New(T("TexturesAsyncLoader"));
                    var handler = New(T("VTSurfaceDataHandler"));
                    bool failed = false;
                    try { Call(surface, "LoadTexturesVT", 0, streaming, handler, badLoader, 0); }
                    catch (TargetInvocationException e) { Debug.Log("Negative native load: " + e.InnerException); failed = e.InnerException.Message.Contains("Mid level mips have to match"); }
                    Require(failed, "Missing-cache negative did not reproduce native failure: " + cid);
                    ((IDisposable)handler).Dispose(); negatives++;
                }
                bool rejected = false;
                try { Call(surface, "LoadVTAsync", streaming, 3, 512, 0, false); }
                catch (TargetInvocationException e) { rejected = e.InnerException.Message.Contains("RegisterToVT first"); }
                Require(rejected, "Missing registration negative must fail"); negatives++;
                Require(Get(surface, "m_VTTextureParamBlock") == null, "Old route unexpectedly has parameters");
                Call(surface, "RegisterToVT", streaming);
                var info = ((Array)Get(surface, "m_VTAtlassingInfos")).GetValue(0);
                Require(Call(streaming, "GetTextureParamBlock", info) != null, "Native allocation has no parameters");
                for (int stack = 0; stack < (int)Get(surface, "stackCount"); stack++)
                {
                    var loader = New(T("TexturesAsyncLoader"));
                    using var handler = (IDisposable)New(T("VTSurfaceDataHandler"));
                    Call(surface, "LoadTexturesVT", stack, streaming, handler, loader, 0);
                    Call(loader, "StartStreaming");
                    if (Get(loader, "m_ReadHandles") is IEnumerable handles)
                    foreach (var handle in handles)
                    {
                        Call(Get(handle, "JobHandle"), "Complete");
                        Require(Get(handle, "Status").ToString() == "Complete", "Native asynchronous mip-tail read failed.");
                        Call(handle, "Dispose");
                    }
                    foreach (var read in (IEnumerable)Get(loader, "m_Format0AsyncData"))
                    {
                        var data = ((IEnumerable)Get(read, "data")).Cast<byte>().ToArray();
                        using var input = File.OpenRead((string)Get(read, "path"));
                        input.Position = (long)Get(read, "offset");
                        var expected = new byte[data.Length];
                        Require(input.Read(expected, 0, expected.Length) == expected.Length && data.SequenceEqual(expected), "Native mip-tail bytes changed.");
                        Call(streaming, "DoneLoading", Get(read, "textureGuid"));
                        streamed++;
                    }
                }
                                // Exercise native data reference teardown, not a model of it.
                Call(surface, "LoadProperties", true);
                var reservation = Get(surface, "VTAtlassingInfos");
                Require(reservation != null, "Reservation absent before unload");
                Require((bool)Call(surface, "UnloadProperties", false), "Last data reference did not unload");
                Require(Get(surface, "VTAtlassingInfos") == null, "Native last unload must reproduce cleared reservation");
                Call(surface, "LoadProperties", true);
                Require(Get(surface, "VTAtlassingInfos") == null && Get(surface, "PreReservedAtlassingInfos") == null,
                    "Old batch path unexpectedly retains its atlas binding");
                // Native cache reservations survive data unload through this public API.
                if (args.Contains("-bridgeDll"))
                {
                    // Invoke the actual candidate helper with the real engine objects.
                    // Restore the allocation cleared by the negative control first.
                    Set(surface, "m_VTAtlassingInfos", reservation);
                    var bridge = Assembly.LoadFrom(Arg("-bridgeDll"));
                    bridge.GetType("BridgeBuilder.Bridges.MovableLaneMidMips", true)
                        .GetMethod("InitializeZeroMid", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new[] { surface, streaming });
                }
                else Call(surface, "PreRegisterToVT", reservation);
                Call(surface, "UnloadProperties", false);
                Call(surface, "LoadProperties", true);
                var selected = Get(surface, "VTAtlassingInfos") ?? Get(surface, "PreReservedAtlassingInfos");
                Require(ReferenceEquals(selected, reservation), "Native batch reservation changed across unload/reload");
                foreach (var entry in (IEnumerable)selected)
                    if ((int)Get(entry, "indexInStack") >= 0)
                        Require(Call(streaming, "GetTextureParamBlock", entry) != null, "Restored batch has no parameter block");
                Call(surface, "UnloadProperties", false);
                for (int cycle = 0; cycle < 12; cycle++)
                {
                    Call(surface, "LoadProperties", true);
                    // The actual native shallow-init path must reuse the reservation,
                    // including its empty stack, without reallocating or reading pixels.
                    Call(surface, "LoadVTAsync", streaming, 3, 512, 0, true);
                    var rebound = (Array)Get(surface, "VTAtlassingInfos");
                    var expectedReservation = (Array)reservation;
                    for (int stack = 0; stack < rebound.Length; stack++)
                        Require(rebound.GetValue(stack).Equals(expectedReservation.GetValue(stack)), "Shallow init changed atlas identity");
                    Require((bool)Call(surface, "UnloadProperties", false), "Unbalanced data references");
                    Require(ReferenceEquals(Get(surface, "PreReservedAtlassingInfos"), reservation), "Repeated unload lost reservation");
                }
                Call(surface, "ClearVTAtlassingInfos");
                Require(Get(surface, "VTAtlassingInfos") == null && Get(surface, "PreReservedAtlassingInfos") == null,
                    "Native streaming reset must clear both reservation fields");
                Call(surface, "LoadProperties", true);
                Call(surface, "RegisterToVT", streaming);
                var refreshed = Get(surface, "VTAtlassingInfos");
                Call(surface, "PreRegisterToVT", refreshed);
                Call(surface, "UnloadProperties", false);
                Require(ReferenceEquals(Get(surface, "PreReservedAtlassingInfos"), refreshed) && !ReferenceEquals(refreshed, reservation),
                    "Refresh retained stale reservation");
                // Fresh object reads the saved descriptor, with no creation-time memory.
                var cold = New(T("SurfaceAsset"));
                cold.GetType().GetProperty("id").SetValue(cold, New(T("Identifier"), Hash(cid)));
                cold.GetType().GetProperty("database").SetValue(cold, proxy);
                Call(cold, "LoadHeader");
                Require(Get(cold, "PreReservedAtlassingInfos") == null && Get(cold, "VTAtlassingInfos") == null,
                    "New instance unexpectedly inherited runtime state");
                // Exact zero-cache Initialize/Refresh branch of native streaming.
                Call(cold, "ClearVTAtlassingInfos");
                Call(cold, "RegisterToVT", streaming);
                Call(cold, "LoadProperties", true);
                Require(Get(cold, "VTAtlassingInfos") != null, "Fresh native scan registration absent at batch load");
                foreach (var entry in (IEnumerable)Get(cold, "VTAtlassingInfos"))
                    if ((int)Get(entry, "indexInStack") >= 0)
                        Require(Call(streaming, "GetTextureParamBlock", entry) != null, "Cold batch parameters absent");
                Call(cold, "UnloadProperties", false);
                Debug.Log("PASS fresh serialized Surface native scan registration (not city rendering) " + cid);
                Debug.Log("PASS native properties lifetime: old atlas lost; retained native reservation restores batch selection " + cid);
                channels += guids.Length; successes++;
                Debug.Log("PASS native VT load " + cid + " levels=" + (handled ? 3 : 0));
            }
            Require(successes > 0 && (negatives > 0 || args.Contains("-cacheOnly")), "No native load coverage.");
            Call(vt, "Destroy");

            Debug.Log("PASS native lane VT reservation/channel-read (not renderer): surfaces=" + successes + "; channels=" + channels + "; missing-registration negatives=" + negatives + "; empty=" + empty + "; asynchronous byte-exact tails=" + streamed + "; caches=" + caches.Length);
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); }
    }
}
