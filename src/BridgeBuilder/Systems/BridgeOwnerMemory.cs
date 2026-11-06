using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BridgeBuilder.Systems;

internal static class BridgeOwnerMemory
{
    internal sealed class InUse : IOException
    {
        internal InUse(string reason) : base(reason) {}
    }

    internal sealed class Identity : IEqualityComparer<AssetData>
    {
        public bool Equals(AssetData? a,AssetData? b)=>ReferenceEquals(a,b);
        public int GetHashCode(AssetData a)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a);
    }
    private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private static object? Field(object target,string name)
    {
        for(var type=target.GetType();type!=null;type=type.BaseType)
            if(type.GetField(name,Fields|BindingFlags.DeclaredOnly) is FieldInfo field)return field.GetValue(target);
        throw new MissingFieldException(target.GetType().FullName,name);
    }

    internal static void JoinMeshReads(BatchMeshSystem meshes,HashSet<Entity> entities,Func<AssetData,bool> owned)
    {
        var jobs=(IDictionary)Field(meshes,"m_CachingMeshes")!;
        var complete=typeof(BatchMeshSystem).GetMethod("CompleteCaching",Fields,null,new[]{typeof(Entity)},null)
            ??throw new MissingMethodException("CompleteCaching(Entity)");
        foreach(Entity entity in jobs.Keys.Cast<Entity>().ToArray())
            if(entities.Contains(entity)||(Field(jobs[entity]!,"m_GeometryAsset") is AssetData asset && owned(asset)))
                complete.Invoke(meshes,new object[]{entity});
    }

    internal static bool CompleteHeaders(TextureStreamingSystem streaming,Func<string?,bool> owned)
    {
        var loaders=(IList)Field(streaming,"m_TexturesAsyncLoaders")!;
        bool done=true;
        for(int i=loaders.Count-1;i>=0;i--)
        {
            var loader=loaders[i]!;
            var headers=(IEnumerable)Field(loader,"m_Format0AsyncData")!;
            if(!headers.Cast<object>().Any(h=>owned(Field(h,"path") as string)))continue;
            if(Field(loader,"m_ReadHandles") is List<ReadHandle> reads)
                foreach(var read in reads)
                {
                    if(!read.IsValid())throw new IOException("Invalid native texture header read");
                    read.JobHandle.Complete();
                    if(read.Status!=ReadStatus.Complete)throw new IOException("Native texture header read failed: "+read.Status);
                }
            // CompleteIfReady also checks dependencies on other stacks and publishes ready tiles.
            if(((TexturesAsyncLoader)loader).CompleteIfReady(streaming))loaders.RemoveAt(i);else done=false;
        }
        return done;
    }

    internal static HashSet<Object> Instances(IEnumerable<AssetData> assets)
    {
        var result=new HashSet<Object>();
        foreach(var asset in assets)
        {
            if(asset is GeometryAsset)
            {
                if(Field(asset,"m_Instances") is Mesh[] meshes)foreach(var mesh in meshes)if(mesh!=null)result.Add(mesh);
            }
            else if(asset is SurfaceAsset || asset is TextureAsset || asset is PrefabAsset)
            {
                if(Field(asset,"m_Instance") is Object instance && instance!=null)result.Add(instance);
            }
        }
        return result;
    }

    // Inspect existing native registrations/materials, without loading any asset payload or traversing saved bridges.
    internal static void CheckConsumers(World world,HashSet<AssetData> assets,HashSet<Object> objects)
    {
        var ids=assets.Select(a=>a.id.guid).ToHashSet();
        bool Asset(AssetData? a)=>a!=null&&assets.Contains(a);
        bool Surface(SurfaceAsset? s)=>s!=null && (Asset(s)||(s.textures?.Values.Any(a=>Asset(a))??false));
        bool MaterialUses(Material? m)
        {
            if(m==null || objects.Count==0)return false;
            if(objects.Contains(m))return true;
            foreach(int id in m.GetTexturePropertyNameIDs())
                if(m.GetTexture(id) is Texture t && objects.Contains(t))return true;
            return false;
        }
        var prefabs=world.GetExistingSystemManaged<PrefabSystem>();
        if(prefabs!=null)
            foreach(var prefab in PrefabCatalog.GetAll(prefabs))
            {
                if(prefab==null)continue;
                if(objects.Contains(prefab))throw new InUse("A registered prefab still holds owner memory: "+prefab.name);
                if(!(prefab is RenderPrefab render))continue;
                var geometry=Field(render,"m_GeometryAsset") as AssetReference<GeometryAsset>;
                var surfaces=Field(render,"m_SurfaceAssets") as AssetReference<SurfaceAsset>[];
                if((geometry!=null && ids.Contains(geometry.guid) && Asset(render.geometryAsset)) ||
                    (surfaces?.Any(s=>s!=null && ids.Contains(s.guid) && Asset((SurfaceAsset)s))??false) ||
                    (Field(render,"m_MaterialsContainer") is Material[] mats && mats.Any(MaterialUses)))
                    throw new InUse("A surviving render prefab still uses owner memory: "+render.name);
            }
        var system=world.GetExistingSystemManaged<BatchManagerSystem>();
        if(system!=null)
        {
            var managed=system.GetManagedBatches();
            for(int i=0;i<managed.BatchCount;i++)
                if(managed.GetBatch(i) is CustomBatch batch && (Surface(batch.sourceSurface) ||
                    MaterialUses(batch.material)||MaterialUses(batch.sourceMaterial)||MaterialUses(batch.loadedMaterial)||
                    MaterialUses(batch.defaultMaterial)||(batch.mesh!=null && objects.Contains(batch.mesh))))
                    throw new InUse("A surviving render batch still uses owner memory: "+i);
        }
    }

    internal static void ReleaseGeometryRequests(World world,BatchMeshSystem meshes,HashSet<AssetData> assets)
    {
        var loading=(HashSet<GeometryAsset>)Field(meshes,"m_LoadingGeometries")!;
        var unload=(HashSet<GeometryAsset>)Field(meshes,"m_UnloadGeometryAssets")!;
        var loader=(GeometryAssetLoadingSystem)Field(meshes,"m_GeometryLoadingSystem")!;
        var queued=(Queue<GeometryAsset>)Field(loader,"m_ToBeLoaded")!;
        var headers=(List<GeometryAsset>)Field(loader,"m_LoadingHeader")!;
        var bodies=(List<GeometryAsset>)Field(loader,"m_LoadingBody")!;
        var affected=loading.Concat(unload).Concat(queued).Concat(headers).Concat(bodies).Where(assets.Contains).Distinct(new Identity()).Cast<GeometryAsset>().ToArray();
        if(affected.Length==0)return;
        CheckConsumers(world,assets,Instances(assets));
        int count=queued.Count;
        for(int i=0;i<count;i++){var g=queued.Dequeue();if(!assets.Contains(g))queued.Enqueue(g);}
        foreach(var g in affected)
        {
            // GeometryAsset.Loading.Dispose completes both read handles before disposing them.
            g.loading.Dispose();
            headers.RemoveAll(item=>ReferenceEquals(item,g));bodies.RemoveAll(item=>ReferenceEquals(item,g));
            if(loading.Any(item=>ReferenceEquals(item,g)) && loading.Remove(g))g.UnloadPartial();
            if(unload.Any(item=>ReferenceEquals(item,g)) && unload.Remove(g))g.UnloadPartial();
        }
    }

    internal static Action Prepare(World world,HashSet<AssetData> assets,HashSet<Object> destroying)
    {
        if(assets.Count==0)return ()=>{};
        foreach(var geometry in assets.OfType<GeometryAsset>())
            if((int)Field(geometry,"m_InstanceRefCount")!>0)throw new InUse("A native consumer still holds geometry: "+geometry.name);
        destroying.UnionWith(Instances(assets));
        CheckConsumers(world,assets,destroying);
        // Native material keys can retain retired textures after their final batch has gone.
        var batches=world.GetExistingSystemManaged<ManagedBatchSystem>();
        if(batches==null)return ()=>{};
        var materials=(Dictionary<ManagedBatchSystem.MaterialKey,Material>)Field(batches,"m_Materials")!;
        var ownedKeys=materials.Where(pair=>pair.Key.textures.Values.Any(v=>v is AssetData asset?assets.Contains(asset):
            v is Object o && destroying.Contains(o)) || (pair.Key.template!=null && destroying.Contains(pair.Key.template))).ToArray();
        foreach(var pair in ownedKeys)if(pair.Value!=null)destroying.Add(pair.Value);
        if(ownedKeys.Length!=0)CheckConsumers(world,assets,destroying);
        return ()=>{foreach(var pair in ownedKeys){materials.Remove(pair.Key);if(pair.Value!=null)Object.Destroy(pair.Value);}};
    }

    internal static void Unload(IEnumerable<AssetData> assets)
    {
        foreach(var asset in assets)
        {
            if(asset is GeometryAsset geometry)
            {
                geometry.loading.Dispose();
                geometry.UnloadPartial(true);
            }
            if(asset is SurfaceAsset surface)surface.UnloadProperties(true);
            asset.Unload(true);
            if(asset.state!=LoadState.Untracked && asset.state!=LoadState.NotLoaded ||
                asset is GeometryAsset g && g.data.IsValid)
                throw new IOException("Owner asset retained native payloads after unload: "+asset.name);
        }
    }
}
