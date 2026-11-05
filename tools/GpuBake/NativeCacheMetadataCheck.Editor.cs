using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class NativeCacheMetadataCheck {
 public static void Run(){try{
 var a=Environment.GetCommandLineArgs();var root=a[Array.IndexOf(a,"-gameManaged")+1];
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(root,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var db=Assembly.LoadFrom(Path.Combine(root,"Colossal.IO.AssetDatabase.dll"));var factory=db.GetType("Colossal.IO.AssetDatabase.DefaultAssetFactory",true);
 var instance=factory.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);object[] args={".MidMips",null};
 var found=(bool)factory.GetMethod("GetAssetType").Invoke(instance,args);
 if(!found||((Type)args[1]).FullName!="Colossal.IO.AssetDatabase.MidMipCacheAsset")throw new Exception("Native factory cache type mismatch");
 var meta=Activator.CreateInstance(db.GetType("Colossal.IO.AssetDatabase.SourceMeta",true));var extension=(string)meta.GetType().GetField("extension").GetValue(meta);
 bool reproduced=false;try{extension.Equals(".MidMips",StringComparison.OrdinalIgnoreCase);}catch(NullReferenceException){reproduced=true;}
 if(!reproduced)throw new Exception("Native metadata default did not reproduce old query NRE");
 if(Array.IndexOf(a,"-cachePayload")>=0){
 var mod=Assembly.LoadFrom(a[Array.IndexOf(a,"-modAssembly")+1]);var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
 var persistence=mod.GetType("BridgeBuilder.Runtime.BridgeDependencyPersistence",true);var nameMethod=persistence.GetMethod("CacheFileName",flags);
 var core=Assembly.LoadFrom(Path.Combine(root,"Colossal.Core.dll"));var h=core.GetType("Colossal.Hash128",true);
 var entryType=db.GetType("Colossal.IO.AssetDatabase.FileSystemDataSource+EntryInfo",true);
 foreach(bool packaged in new[]{false,true}){
 var package=packaged?h.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"11111111111111111111111111111111"}):Activator.CreateInstance(h);
 var path=packaged?"C:/Cache/UK Pack.cok":"C:/Cache/StreamingData~/TS512_MB3.MidMips";
 var entry=Activator.CreateInstance(entryType,new object[]{"assetdb",3,path,packaged?"StreamingData~/TS512_MB3.MidMips":null,(Type)args[1],false,322056634L,0L,package});
 var fileName=(string)entryType.GetProperty("fileName").GetValue(entry);var ext=(string)entryType.GetProperty("extension").GetValue(entry);
 var result=(string)nameMethod.Invoke(null,new object[]{fileName,ext,path});if(result!="TS512_MB3.MidMips")throw new Exception("Native entry filename lost");
 if(packaged&&Path.GetFileName(path)==result)throw new Exception("Fixture does not reproduce container bug");}
 var payload=File.ReadAllBytes(a[Array.IndexOf(a,"-cachePayload")+1]);var refs=(string[])mod.GetType("BridgeBuilder.Runtime.BridgeMidMipReferences",true).GetMethod("Read",flags).Invoke(null,new object[]{payload});
 if(Array.IndexOf(refs,"d765f1348645cc68318b5ac29c61abde")>=0)throw new Exception("UK cache unexpectedly belongs to failure Surface");
 Debug.Log("PASS actual UK cache 41d4754f30d485a13d67772dc88f8d81: bytes="+payload.Length+" members="+refs.Length+" tile="+BitConverter.ToInt32(payload,0)+" levels="+BitConverter.ToInt32(payload,4)+"; requested Surface absent; real native EntryInfo packaged/filesystem filename corrected.");
 }
 Debug.Log("PASS installed native factory .MidMips -> MidMipCacheAsset; native default SourceMeta reproduces old global extension query NRE. Exact failed game CID unavailable because old catch discarded stack.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
