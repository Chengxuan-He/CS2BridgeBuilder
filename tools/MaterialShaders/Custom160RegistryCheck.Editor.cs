using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEditor;
using UnityEngine;

public static class Custom160RegistryCheck
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    public static void Run()
    {
        try
        {
            string Arg(string name)=>Environment.GetCommandLineArgs().SkipWhile(x=>x!=name).Skip(1).First();
            string managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
            {var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
            var mod=Assembly.LoadFrom(Arg("-candidate"));
            var native=Assembly.Load("Colossal.IO.AssetDatabase");
            var libraryType=native.GetType("Colossal.IO.AssetDatabase.MaterialLibrary",true);
            var descriptionType=libraryType.GetNestedType("MaterialDescription",Flags);
            var library=Activator.CreateInstance(libraryType);
            var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(descriptionType));
            libraryType.GetField("m_Materials").SetValue(library,list);
            var nativeSourcePath=Environment.GetCommandLineArgs().SkipWhile(x=>x!="-sourceBundle").Skip(1).FirstOrDefault();
            var sourceBundle=string.IsNullOrEmpty(nativeSourcePath)?null:AssetBundle.LoadFromFile(nativeSourcePath);
            var sourceShader=sourceBundle==null?Shader.Find("Standard"):sourceBundle.LoadAllAssets<Shader>().Single();
            var source=new Material(sourceShader);
            source.SetFloat("_Metallic",.37f);source.renderQueue=2473;source.enableInstancing=true;
            var description=Activator.CreateInstance(descriptionType);
            descriptionType.GetField("m_Hash").SetValue(description,17);
            descriptionType.GetField("m_Material").SetValue(description,source);
            descriptionType.GetField("m_MipBiasOverride").SetValue(description,2);
            list.Add(description);
            var method=mod.GetType("BridgeBuilder.Runtime.BridgeCustomShaderRegistry",true).GetMethod("TryRegister",Flags);
            var bytes=File.ReadAllBytes(Arg("-bundle"));
            var compileAndRegister=mod.GetType("BridgeBuilder.Runtime.BridgeCustomShaderRegistry",true).GetMethod("TryRegisterPackage",Flags);
            object[] first={library,17,File.ReadAllBytes(Arg("-package")),null,null};
            var elapsed=System.Diagnostics.Stopwatch.StartNew();
            if(!(bool)compileAndRegister.Invoke(null,first))throw new Exception((string)first[4]);
            elapsed.Stop();Debug.Log("Generated package Mono compile + Unity registration ms="+elapsed.Elapsed.TotalMilliseconds);
            int id=(int)first[3];var created=list[1];
            string persistedIdentity;
            using(var hash=System.Security.Cryptography.SHA256.Create())
                persistedIdentity=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Arg("-package"))))+":17";
            uint stable=2166136261;foreach(char c in persistedIdentity)stable=unchecked((stable^c)*16777619);
            if(id!=((int)(stable&0x3fffffff)|0x40000000))throw new Exception("template identity depends on compiled container rather than persisted package");
            var material=(Material)descriptionType.GetField("m_Material").GetValue(created);
            if(list.Count!=2||material.shader==source.shader||Math.Abs(material.GetFloat("_Metallic")-.37f)>.00001f||
                material.renderQueue!=2473||!material.enableInstancing||source.shader!=sourceShader||
                (int)descriptionType.GetField("m_MipBiasOverride").GetValue(created)!=2)
                throw new Exception("registration changed source or lost shared material state");
            Debug.Log("Generated shader="+material.shader.name+" properties="+string.Join(",",Enumerable.Range(0,material.shader.GetPropertyCount()).Select(material.shader.GetPropertyName))+" hasBone="+material.HasProperty("colossal_BoneParameters"));
            if(!material.HasProperty("colossal_BoneParameters"))throw new Exception("native current-bone property was not declared");
            if(material.FindPass("DBufferMesh")>=0)
            {
                var preview=new Material(material);
                if(preview.HasProperty("_ZWrite"))preview.SetInt("_ZWrite",0);
                var configure=mod.GetType("BridgeBuilder.Runtime.BridgePreviewRenderResources",true).GetMethod("ConfigurePreviewDepth",Flags);
                configure.Invoke(null,new object[]{preview,false,false,null,false});
                if(preview.HasProperty("_ZWrite")&&preview.GetInt("_ZWrite")!=0)throw new Exception("mixed-render decal preview changed source depth writes");
                UnityEngine.Object.DestroyImmediate(preview);
                Debug.Log("PASS mixed-render DBuffer material retains depth writes without whole-render DecalProperties");
            }
            object[] repeat={library,17,File.ReadAllBytes(Arg("-package")),null,null};
            if(!(bool)compileAndRegister.Invoke(null,repeat)||(int)repeat[3]!=id||list.Count!=2)
                throw new Exception("registration not idempotent");
            object[] missing={library,999,bytes,null,null};
            if((bool)method.Invoke(null,missing)||(int)missing[3]!=-1||list.Count!=2||string.IsNullOrEmpty((string)missing[4]))
                throw new Exception("missing template published partial result");
            // Simulate a conflicting native registration without unloading active shader objects.
            list.Remove(created);var collision=Activator.CreateInstance(descriptionType);
            descriptionType.GetField("m_Hash").SetValue(collision,id);
            descriptionType.GetField("m_Material").SetValue(collision,source);list.Add(collision);
            object[] replaced={library,17,File.ReadAllBytes(Arg("-package")),null,null};
            if((bool)compileAndRegister.Invoke(null,replaced)||list.Count!=2||!ReferenceEquals(list[1],collision))
                throw new Exception("registration overwrote foreign template");
            var configured=mod.GetType("BridgeBuilder.Runtime.BridgeCustomShaderRegistry",true).GetMethod("TryRegisterConfiguredPackage",Flags);
            var restoredLibrary=Activator.CreateInstance(libraryType);var restoredList=(IList)Activator.CreateInstance(list.GetType());restoredList.Add(description);libraryType.GetField("m_Materials").SetValue(restoredLibrary,restoredList);
            int last=-1;
            foreach(int queue in new[]{2004,2017})
            {
                object[] arguments={library,17,File.ReadAllBytes(Arg("-package")),(int?)queue,null,null};
                if(!(bool)configured.Invoke(null,arguments))throw new Exception((string)arguments[5]);
                int current=(int)arguments[4];if(current==last)throw new Exception("priority variants share an identity");last=current;
                var entry=libraryType.GetMethods(Flags).Single(m=>m.Name=="GetMaterialDescription"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(int)).Invoke(library,new object[]{current});
                var value=(Material)descriptionType.GetField("m_Material").GetValue(entry);
                if(value.renderQueue!=queue || value.shader!=material.shader || !value.HasProperty("colossal_BoneParameters"))throw new Exception("priority variant or bone property lost");
                object[] reload={restoredLibrary,17,File.ReadAllBytes(Arg("-package")),(int?)queue,null,null};
                if(!(bool)configured.Invoke(null,reload)||(int)reload[4]!=current)throw new Exception("template identity changed after new-library restoration");
            }
            Debug.Log("PASS declared native bone property, shared program across exact queue2004/2017 variants, stable IDs in a new MaterialLibrary; original source unchanged.");
            FileAssetRoundTrip(mod,native,Arg("-package"),Arg("-outputRoot"));
            Debug.Log("PASS actual MaterialLibrary + generated native shader: registration preserves shared properties/queue/instancing/mip policy; repeated identity reused; missing source and replaced registration fail without publication. Synthetic source template; startup restoration is not tested.");
            EditorApplication.Exit(0);
        }
        catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
    }

    static void FileAssetRoundTrip(Assembly mod,Assembly native,string packagePath,string root)
    {
        Type N(string name)=>native.GetType("Colossal.IO.AssetDatabase."+name,true);
        Directory.CreateDirectory(root);
        var descriptorType=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
        var descriptor=Activator.CreateInstance(descriptorType,Flags,null,new object[]{root},null);
        var databaseType=N("AssetDatabase`1").MakeGenericType(descriptorType);
        var database=databaseType.GetMethod("GetInstance",Flags).Invoke(null,new[]{descriptor});
        var pathCreate=N("AssetDataPath").GetMethods(Flags).Single(m=>m.Name=="Create"&&m.GetParameters().Length==4);
        var path=pathCreate.Invoke(null,new[]{(object)"ImportedData/b11111111-2222-4333-8444-555555555555/MaterialPrograms",
            "Program.txt",true,Enum.Parse(pathCreate.GetParameters()[3].ParameterType,"None")});
        var add=databaseType.GetMethods(Flags).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));
        var hashType=add.GetParameters()[2].ParameterType;
        var cid=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"0000000000000000000000000000160160"});
        var asset=add.Invoke(database,new[]{path,(object)N("FileAsset"),cid});
        var envelope=mod.GetType("BridgeBuilder.Runtime.BridgeCustomShaderPackage",true);
        var bytes=File.ReadAllBytes(packagePath);
        using(var stream=(Stream)asset.GetType().GetMethod("GetWriteStream",Flags).Invoke(asset,null))
        {
            object[] arguments={stream,17,bytes,null};
            if(!(bool)envelope.GetMethod("TryWrite",Flags).Invoke(null,arguments))throw new Exception((string)arguments[3]);
        }
        using(var stream=(Stream)asset.GetType().GetMethod("GetReadStream",Flags).Invoke(asset,null))
        {
            object[] arguments={stream,null,null,null};
            if(!(bool)envelope.GetMethod("TryRead",Flags).Invoke(null,arguments)||(int)arguments[1]!=17||!bytes.SequenceEqual((byte[])arguments[2]))
                throw new Exception("native FileAsset program reload mismatch: "+arguments[3]);
        }
        using(var stream=(Stream)asset.GetType().GetMethod("GetWriteStream",Flags).Invoke(asset,null))
        {
            object[] arguments={stream,17,bytes,new[]{2017,2004},null};
            if(!(bool)envelope.GetMethod("TryWriteConfigured",Flags).Invoke(null,arguments))throw new Exception((string)arguments[4]);
        }
        using(var stream=(Stream)asset.GetType().GetMethod("GetReadStream",Flags).Invoke(asset,null))
        {
            object[] arguments={stream,null,null,null,null};
            if(!(bool)envelope.GetMethod("TryReadConfigured",Flags).Invoke(null,arguments)||!((int[])arguments[3]).SequenceEqual(new[]{2004,2017})||!bytes.SequenceEqual((byte[])arguments[2]))throw new Exception("queue variants were not restored with one native program payload");
        }
        string physical=(string)asset.GetType().GetProperty("path",Flags).GetValue(asset);
        if(!physical.EndsWith(".txt",StringComparison.OrdinalIgnoreCase)||!File.Exists(physical+".cid"))
            throw new Exception("program was not a native text asset with CID");
        Debug.Log("PASS native FileAsset program persistence with exact BBS1 roundtrip and CID; isolated fixture="+physical);
    }
}
