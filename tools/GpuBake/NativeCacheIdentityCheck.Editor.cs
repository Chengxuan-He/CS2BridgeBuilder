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
public static class NativeCacheIdentityCheck
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
            var args=Environment.GetCommandLineArgs();
            string Arg(string key)=>args[Array.IndexOf(args,key)+1];
            string managed=Arg("-gameManaged"),path=Arg("-cachePayload");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));
            var hashType=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll")).GetType("Colossal.Hash128",true);
            object Hash(string cid)=>hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid});
            var read=mod.GetType("BridgeBuilder.Runtime.BridgeMidMipReferences",true).GetMethod("TryRead",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Stream),typeof(string[]).MakeByRefType(),typeof(string).MakeByRefType()},null);
            object[] refs;
            using(var stream=File.OpenRead(path)){refs=new object[]{stream,null,null};Require((bool)read.Invoke(null,refs),"native cache header parses");}
            var members=(string[])refs[1];Require(members.Length>0,"real cache members exist");
            var files=new Files(T("ILocalAssetDatabase"));
            var asset=New(T("MidMipCacheAsset"));string cacheCid=File.ReadAllText(path+".cid").Trim();
            asset.GetType().GetProperty("id").SetValue(asset,New(T("Identifier"),Hash(cacheCid)));
            asset.GetType().GetProperty("database").SetValue(asset,files.GetTransparentProxy());files.Items.Add(cacheCid,(asset,path));
            var group=FormatterServices.GetUninitializedObject(T("VirtualTexturing.AtlasMaterialsGrouper"));Set(group,"m_MidMipCacheAsset",asset);
            var database=FormatterServices.GetUninitializedObject(T("VirtualTexturing.AtlasMaterialsDatabase"));
            var registrationsField=database.GetType().GetField("m_RegisteredTextures",All);
            var dictionaryType=registrationsField.FieldType.GetElementType();var infoType=dictionaryType.GetGenericArguments()[1];
            var registrations=Array.CreateInstance(dictionaryType,1);var map=(IDictionary)Activator.CreateInstance(dictionaryType);
            var infoConstructor=infoType.GetConstructors(All).Single(c=>c.GetParameters().Length==3);
            var info=infoConstructor.Invoke(infoConstructor.GetParameters().Select(p=>p.ParameterType.IsEnum?Enum.ToObject(p.ParameterType,0):Convert.ChangeType(0,p.ParameterType)).ToArray());
            foreach(string cid in members.Take(2))map.Add(Hash(cid),info);registrations.SetValue(map,0);registrationsField.SetValue(database,registrations);
            var tilesField=database.GetType().GetField("m_PreprocessedMidMipTilesInfo",All);
            var outer=(IDictionary)Activator.CreateInstance(tilesField.FieldType);var innerType=tilesField.FieldType.GetGenericArguments()[1];var inner=(IDictionary)Activator.CreateInstance(innerType);
            int index=(int)T("VirtualTexturing.PVTTileIndexer").GetMethod("GetUniversalTileIndex").Invoke(null,new object[]{0,0,1});
            inner.Add(index,New(T("VirtualTexturing.ObjectEntry"),0,0,group));outer.Add(0,inner);tilesField.SetValue(database,outer);
            var streaming=FormatterServices.GetUninitializedObject(T("VirtualTexturing.TextureStreamingSystem"));Set(streaming,"m_AtlasMaterialsDatabase",database);
            var lookup=mod.GetType("BridgeBuilder.Bridges.NativeSurfaceCaches",true).GetMethod("TryFindIdentity",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(string cid in members.Take(2))
            {
                object[] call={cid,streaming,null,null};Require((bool)lookup.Invoke(null,call),"lookup success: "+call[3]);
                Require(((IEnumerable)call[2]).Cast<object>().Single()==asset,"exact registered Surface identity resolves native cache without Surface handle");
            }
            object[] absent={new string('0',32),streaming,null,null};Require((bool)lookup.Invoke(null,absent)&&!((IEnumerable)absent[2]).Cast<object>().Any(),"unknown identity does not borrow adjacent cache");
            registrationsField.SetValue(database,null);object[] unready={members[0],streaming,null,null};Require(!(bool)lookup.Invoke(null,unready)&&unready[3]!=null,"unready native index explicitly reported");
            Debug.Log("PASS actual installed native dictionary/VTAtlassingInfo/ObjectEntry/grouper APIs + real cache payload; exact identities="+Math.Min(2,members.Length)+"; no Surface handle, no global enumeration; DLL="+mod.Location);
            UnityEditor.EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}
    }
}
