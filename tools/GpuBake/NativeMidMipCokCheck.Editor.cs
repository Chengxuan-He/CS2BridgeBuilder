using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using UnityEngine;

// Verify the actual native packaged-file stream and discovery tag, then execute
// the production bounded subset copier on original game COK entries.
public static class NativeMidMipCokCheck
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    sealed class SourceDatabase : RealProxy
    {
        public Func<Stream> Open;
        public string Name;
        public SourceDatabase(Type type) : base(type) { }
        public override IMessage Invoke(IMessage request)
        {
            var call = (IMethodCallMessage)request;
            try
            {
                object value = call.MethodName == "GetReadStream" ? (object)Open() :
                    call.MethodName == "GetName" ? Name : throw new NotSupportedException(call.MethodName);
                return new ReturnMessage(value, null, 0, call.LogicalCallContext, call);
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
            string managed = Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                string file = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(file) ? Assembly.LoadFrom(file) : null;
            };
            var native = Assembly.LoadFrom(Path.Combine(managed, "Colossal.IO.AssetDatabase.dll"));
            Type T(string name) => native.GetType("Colossal.IO.AssetDatabase." + name, true);
            var hashType = Assembly.LoadFrom(Path.Combine(managed, "Colossal.Core.dll")).GetType("Colossal.Hash128", true);
            object Hash(string cid) => hashType.GetMethod("Parse", new[] { typeof(string) }).Invoke(null, new object[] { cid });
            var sourceType = T("FileSystemDataSource");
            var source = FormatterServices.GetUninitializedObject(sourceType);
            var entryType = sourceType.GetNestedType("EntryInfo");
            var read = sourceType.GetMethod("GetReadStream", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { entryType }, null);
            var mod = Assembly.LoadFrom(Arg("-modAssembly"));
            var subset = mod.GetType("BridgeBuilder.Bridges.MovableMidMipSubset", true).GetMethod("Copy", BindingFlags.Static | BindingFlags.NonPublic);
            int count = 0;
            foreach (string line in File.ReadAllLines(Arg("-cokCases")))
            {
                var row = line.Split('\t');
                var entry = Activator.CreateInstance(entryType, new object[] { "native-cok-fixture", 0, row[0], "TS512_MB3.MidMips", T("MidMipCacheAsset"), false, long.Parse(row[2]), long.Parse(row[1]), Hash(File.ReadAllText(row[0] + ".cid").Trim()) });
                var proxy = new SourceDatabase(T("ILocalAssetDatabase"))
                {
                    Open = () => (Stream)read.Invoke(source, new[] { entry }),
                    Name = (string)entryType.GetProperty("fileName").GetValue(entry)
                };
                var cache = Activator.CreateInstance(T("MidMipCacheAsset"));
                cache.GetType().GetProperty("database").SetValue(cache, proxy.GetTransparentProxy());
                cache.GetType().GetProperty("id").SetValue(cache, Activator.CreateInstance(T("Identifier"), Hash(row[6])));
                cache.GetType().GetMethod("PostCreate").Invoke(cache, null);
                var tags = ((System.Collections.IEnumerable)cache.GetType().GetProperty("tags").GetValue(cache)).Cast<string>().ToArray();
                if (tags.Length != 1 || tags[0] != "TS512_MB3") throw new Exception("Native cache discovery tag changed.");
                using var stream = (Stream)cache.GetType().GetMethod("GetReadStream").Invoke(cache, null);
                if (!stream.CanSeek || stream.Length != long.Parse(row[2])) throw new Exception("Native COK stream is not correctly bounded/seekable.");
                var result = (byte[])subset.Invoke(null, new object[] { stream, row[3], row[4], 512, 3 });
                if (!result.SequenceEqual(File.ReadAllBytes(row[5]))) throw new Exception("Native COK subset differs from independently recovered BC7 blocks.");
                count++;
            }
            if (count == 0) throw new Exception("No native COK fixtures.");
            Debug.Log("PASS native COK MidMips source: cases=" + count + "; seekable bounded native SubStream; actual PostCreate tag; production subsets byte-exact.");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); }
    }
}
