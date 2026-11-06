using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using UnityEditor;
using UnityEngine;

public static class MeshRetirementCheck
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static FieldInfo Field(Type type,string name)=>type.GetField(name,F)??throw new MissingFieldException(type.Name,name);
    static object Blank(Type t)=>FormatterServices.GetUninitializedObject(t);
    static void Set(object x,string n,object v)=>Field(x.GetType(),n).SetValue(x,v);
    static object NewList(Type t){var args=t.GetConstructors().Single(c=>c.GetParameters().Length==2).GetParameters();var allocator=Activator.CreateInstance(args[1].ParameterType);Set(allocator,"Index",(ushort)4);return Activator.CreateInstance(t,new object[]{4,allocator});}
    static void Add(object x,object v)=>x.GetType().GetMethod("Add",F).Invoke(x,new[]{v});
    static int Length(object x)=>(int)x.GetType().GetProperty("Length").GetValue(x);
    static object At(object x,int i)=>x.GetType().GetProperty("Item").GetValue(x,new object[]{i});
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    [InitializeOnLoadMethod]
    static void Resume()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BridgeRetirementFixture",false)){
                SessionState.SetBool("BridgeRetirementFixture",false); EditorApplication.delayCall+=Fixture;
            }
        };
    }
    public static void Run()
    {
        SessionState.SetBool("BridgeRetirementFixture",true);
        EditorApplication.EnterPlaymode();
    }
    static void Fixture()
    {
        try{
            var args=Environment.GetCommandLineArgs();
            string Arg(string key)=>args[Array.IndexOf(args,key)+1];
            var managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
            var mod=Assembly.LoadFrom(Arg("-candidate"));var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));
            var helper=mod.GetType("BridgeBuilder.Systems.BridgeMeshRetirement",true);
            var meshType=game.GetType("Game.Rendering.BatchMeshSystem",true);var meshes=Blank(meshType);
            var unload=Field(meshType,"m_UnloadingData");var queue=NewList(unload.FieldType);
            unload.SetValue(meshes,queue);
            var elem=unload.FieldType.GetGenericArguments()[0];
            var stateField=Field(meshType,"m_LoadingState");var states=NewList(stateField.FieldType);stateField.SetValue(meshes,states);
            var stateType=stateField.FieldType.GetGenericArguments()[0];
            var pf=Field(meshType,"m_BatchPriority");var priority=NewList(pf.FieldType);pf.SetValue(meshes,priority);
            for(int i=0;i<3;i++){Add(priority,-999);if(i!=2)Add(queue,Activator.CreateInstance(elem,new object[]{1,i}));Add(states,Enum.Parse(stateType,i==2?"Complete":"Obsolete"));}
            var bmField=Field(meshType,"m_BatchManagerSystem");var manager=Blank(bmField.FieldType);bmField.SetValue(meshes,manager);
            var managedField=Field(manager.GetType(),"m_ManagedBatches");var batches=Blank(managedField.FieldType);managedField.SetValue(manager,batches);
            var listField=Field(batches.GetType(),"m_Batches");var list=(IList)Activator.CreateInstance(listField.FieldType);listField.SetValue(batches,list);
            var custom=game.GetType("Game.Rendering.CustomBatch",true);var shared=custom.GetProperty("sharedMeshEntity");
            var meshEntity=Activator.CreateInstance(shared.PropertyType);Set(meshEntity,"Index",301);
            var mesh=new Mesh();Set(meshes,"m_DefaultObjectMesh",mesh);
            for(int i=0;i<3;i++){
                var batch=Blank(custom);custom.GetProperty("sourceType").SetValue(batch,Enum.Parse(custom.GetProperty("sourceType").PropertyType,"Object"));
                shared.SetValue(batch,meshEntity);custom.BaseType.GetProperty("mesh").SetValue(batch,mesh);list.Add(batch);
            }
            var dictField=Field(meshType,"m_MeshInfos");var infos=(IDictionary)Activator.CreateInstance(dictField.FieldType);dictField.SetValue(meshes,infos);
            var info=Activator.CreateInstance(dictField.FieldType.GetGenericArguments()[1]);Set(info,"m_BatchCount",3);Set(info,"m_GeneratedIndex",-1);Set(info,"m_SizeInMemory",64UL);
            infos.Add(meshEntity,info);meshType.GetProperty("memoryBudget").SetValue(meshes,ulong.MaxValue);meshType.GetProperty("totalSizeInMemory").SetValue(meshes,64UL);
            var method=helper.GetMethod("ReleaseBatch",F);
            var em=Activator.CreateInstance(method.GetParameters()[2].ParameterType);
            var destroying=new HashSet<UnityEngine.Object>();
            foreach(int i in new[]{0,2})method.Invoke(null,new object[]{meshes,manager,em,infos,list[i],i,destroying});
            Check(Length(queue)==1 && (int)Field(elem,"m_BatchIndex").GetValue(At(queue,0))==1,"foreign queue retained");
            Check(Length(states)==3&&Length(priority)==3&&At(states,0).ToString()=="None"&&At(states,1).ToString()=="Obsolete"&&At(states,2).ToString()=="None","direct obsolete and complete releases including final slot");
            Check((int)Field(info.GetType(),"m_BatchCount").GetValue(infos[meshEntity])==1,"native shared mesh reference accounting");
            Check(!(bool)meshType.GetProperty("forceMeshUnloading").GetValue(meshes) && (ulong)meshType.GetProperty("memoryBudget").GetValue(meshes)==ulong.MaxValue,"unchanged game budget");
            var drop=helper.GetMethod("DropUnclaimedRecord",F);var geo=Blank(drop.GetParameters()[4].ParameterType);
            var payload=infos[meshEntity];bool Drop()=>(bool)drop.Invoke(null,new object[]{meshes,infos,meshEntity,payload,geo});
            Set(geo,"m_InstanceRefCount",1);Check(!Drop(),"acquired geometry blocked");Set(geo,"m_InstanceRefCount",0);
            Set(payload,"m_GeneratedIndex",0);Check(!Drop(),"generated mesh blocked");Set(payload,"m_GeneratedIndex",-1);
            Check(Drop() && infos.Count==0 && (ulong)meshType.GetProperty("totalSizeInMemory").GetValue(meshes)==0UL,"orphan size accounted");
            ((IDisposable)priority).Dispose();((IDisposable)queue).Dispose();((IDisposable)states).Dispose();UnityEngine.Object.DestroyImmediate(mesh);
            var memory=mod.GetType("BridgeBuilder.Systems.BridgeOwnerMemory",true);
            var release=memory.GetMethod("Unload",F);var assetType=release.GetParameters()[0].ParameterType.GetGenericArguments()[0];
            var textureType=assetType.Assembly.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
            var textureAsset=Blank(textureType);var foreignAsset=Blank(textureType);
            var tex=new Texture2D(2,2);var foreign=new Texture2D(2,2);
            Set(textureAsset,"m_Instance",tex);Set(textureAsset,"m_ObjectInstanceRefCount",3);
            Set(foreignAsset,"m_Instance",foreign);Set(foreignAsset,"m_ObjectInstanceRefCount",1);
            var arr=Array.CreateInstance(assetType,1);arr.SetValue(textureAsset,0);
            release.Invoke(null,new object[]{arr});
            Check(ReferenceEquals(Field(textureType,"m_Instance").GetValue(textureAsset),null)&&
                textureType.GetProperty("state").GetValue(textureAsset).ToString()=="NotLoaded","actual native texture payload unloaded");
            Check(foreign!=null&&ReferenceEquals(Field(textureType,"m_Instance").GetValue(foreignAsset),foreign),"foreign texture retained");
            double until=EditorApplication.timeSinceStartup+10;
            void Finished(){
                if(tex!=null && EditorApplication.timeSinceStartup<until)return;
                EditorApplication.update-=Finished;
                if(tex!=null){Debug.LogError("Unity pending destruction unresolved");EditorApplication.Exit(1);return;}
                UnityEngine.Object.DestroyImmediate(foreign);
                Debug.Log("PASS direct native RemoveBatch under unlimited budget, Complete and Obsolete, last slot retained, shared count 3 -> 1, foreign queue preserved. Native texture unload cleared payload, destruction boundary observed; foreign texture retained.");
                EditorApplication.Exit(0);
            }
            EditorApplication.update+=Finished;
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
