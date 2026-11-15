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
            string managed=Arg("-gameManaged"); AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
            var mod=Assembly.LoadFrom(Arg("-candidate"));
            var native=Assembly.Load("Colossal.IO.AssetDatabase");
            var libraryType=native.GetType("Colossal.IO.AssetDatabase.MaterialLibrary",true);
            var descriptionType=libraryType.GetNestedType("MaterialDescription",Flags);
            object NewLibrary()
            {
                var result=Activator.CreateInstance(libraryType);
                libraryType.GetField("m_Materials").SetValue(result,Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(descriptionType)));
                return result;
            }
            IList Rows(object library)=>(IList)libraryType.GetField("m_Materials").GetValue(library);
            var registry=mod.GetType("BridgeBuilder.Runtime.BridgeCustomShaderRegistry",true);
            var register=registry.GetMethod("TryRegisterPackage",Flags);
            var configure=mod.GetType("BridgeBuilder.Runtime.BridgeNativeMaterialProgram",true).GetMethod("Configure",Flags);
            var library=NewLibrary();var restored=NewLibrary();
            foreach(var name in new[]{"WorldLayeredPbr","BoneDecal"})
            {
                int source=name=="BoneDecal"?18:17;
                var bytes=File.ReadAllBytes(Path.Combine(Arg("-programDir"),name+".bundle"));
                foreach(int queue in new[]{2000,2007})
                {
                    var configured=(byte[])configure.Invoke(null,new object[]{bytes,source,(int?)queue});
                    string identity=(string)mod.GetType("BridgeBuilder.Runtime.BridgeNativeMaterialProgram",true).GetMethod("LogicalIdentity",Flags).Invoke(null,new object[]{source,queue});
                    object[] first={library,configured,identity,null,null};
                    if(!(bool)register.Invoke(null,first))throw new Exception((string)first[4]);
                    int id=(int)first[3];var row=Rows(library)[Rows(library).Count-1];
                    var material=(Material)descriptionType.GetField("m_Material").GetValue(row);
                    if(material==null||material.shader==null||material.renderQueue!=queue||!material.HasProperty("colossal_BoneParameters"))throw new Exception("Native graph lost material/shader/queue/bone field");
                    int count=Rows(library).Count;
                    object[] repeat={library,configured,identity,null,null};
                    if(!(bool)register.Invoke(null,repeat)||(int)repeat[3]!=id||Rows(library).Count!=count)throw new Exception("Registration not idempotent");
                    object[] cold={restored,configured,identity,null,null};
                    if(!(bool)register.Invoke(null,cold)||(int)cold[3]!=id||!ReferenceEquals(Rows(restored)[count-1],row))throw new Exception("Second library did not reuse immutable native description");
                    var foreign=NewLibrary();var collision=Activator.CreateInstance(descriptionType);descriptionType.GetField("m_Hash").SetValue(collision,id);Rows(foreign).Add(collision);
                    object[] rejected={foreign,configured,identity,null,null};
                    if((bool)register.Invoke(null,rejected)||Rows(foreign).Count!=1||!ReferenceEquals(Rows(foreign)[0],collision))throw new Exception("Foreign template overwritten");
                    Debug.Log("PASS production native graph "+name+" queue="+queue+" template="+id+" shader="+material.shader.name);
                }
            }
            Debug.Log("PASS simultaneous production native graphs, stable queue variants, idempotence, independent library publication, collision rollback; no owner program files");
            EditorApplication.Exit(0);
        }
        catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
    }

    public static void SurfaceRoundTrip()
    {
      try {
        string Arg(string name)=>Environment.GetCommandLineArgs().SkipWhile(x=>x!=name).Skip(1).First();
        string managed=Arg("-gameManaged");AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        var mod=Assembly.LoadFrom(Arg("-candidate"));var native=Assembly.Load("Colossal.IO.AssetDatabase");Type N(string name)=>native.GetType("Colossal.IO.AssetDatabase."+name,true);
        string root=Arg("-outputRoot");Directory.CreateDirectory(root);
        var descriptorType=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
        var descriptor=Activator.CreateInstance(descriptorType,Flags,null,new object[]{root},null);
        var databaseType=N("AssetDatabase`1").MakeGenericType(descriptorType);var database=databaseType.GetMethod("GetInstance",Flags).Invoke(null,new[]{descriptor});
        var pathCreate=N("AssetDataPath").GetMethods(Flags).Single(m=>m.Name=="Create"&&m.GetParameters().Length==4);
        var path=pathCreate.Invoke(null,new[]{(object)"Fixture","Current.Surface",true,Enum.Parse(pathCreate.GetParameters()[3].ParameterType,"None")});
        var add=databaseType.GetMethods(Flags).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));
        var hashType=add.GetParameters()[2].ParameterType;var cid=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"0000000000000000000000000000161116"});
        var asset=add.Invoke(database,new[]{path,(object)N("SurfaceAsset"),cid});var t=asset.GetType();
        var template=mod.GetType("BridgeBuilder.Runtime.BridgeNativeMaterialProgram",true).GetMethod("TemplateId",Flags).Invoke(null,new object[]{18,(int?)2007});
        t.GetMethod("SetMaterialHash",Flags).Invoke(asset,new[]{template});
        t.GetMethod("UpdateInts",Flags).Invoke(asset,new object[]{new System.Collections.Generic.Dictionary<string,int>{{"BridgeBuilder_RenderQueue",2007},{"ForeignMetadata",123}}});
        t.GetMethod("SaveData",Flags).Invoke(asset,new object[]{true});
        t.GetMethod("UnloadProperties",Flags).Invoke(asset,new object[]{true});
        t.GetMethod("LoadProperties",Flags).Invoke(asset,new object[]{false});
        object[] query={"BridgeBuilder_RenderQueue",0};
        if(!(bool)t.GetMethod("TryGetIntProperty",Flags).Invoke(asset,query)||(int)query[1]!=2007)throw new Exception("Native Surface queue not retained");
        using(var input=(Stream)t.GetMethod("GetReadStream",Flags).Invoke(asset,null))using(var bytes=new MemoryStream()){
          input.CopyTo(bytes);object[] args={bytes.ToArray(),0,null,null,null};
          if(!(bool)mod.GetType("BridgeBuilder.Runtime.BridgeCurrentProgramSurface",true).GetMethod("TryReadSurfaceIdentity",Flags).Invoke(null,args)||(int)args[1]!=(int)template||(int)args[2]!=2007)throw new Exception("Current identity parser differs from native serialized Surface");
        }
        Debug.Log("PASS actual native Surface SaveData/UnloadProperties/LoadProperties retained integer queue and stable template; no Material realization or shader file");EditorApplication.Exit(0);
      }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }

    // A fresh process reuses the same packaged-program test, never an owner package.
    public static void Cold() => Run();
}
