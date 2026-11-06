using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BridgeBuilder.Runtime;
using CS2Mods.Shared.Infrastructure;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Unity.Entities;

namespace BridgeBuilder.Systems;

// Native prefab removal schedules work. A later UI update commits only after the target mesh,
// virtual-texture and file readers have drained, on the same main thread as save notifications.
public partial class BridgeOwnerCleanupSystem : GameSystemBase
{
    private string? _owner;
    private DateTime _began;
    private readonly HashSet<Entity> _prefabs = new();
    private readonly HashSet<AssetData> _assets = new();
    private string Root => UnityEngine.Application.persistentDataPath;

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose,mode);
        Reset();
    }

    private void Reset()
    {
        _owner=null;
        _prefabs.Clear();
        _assets.Clear();
    }

    protected override void OnUpdate()
    {
        var game=GameManager.instance;
        if (game==null || game.isGameLoading || game.state!=GameManager.State.WorldReady
            || !BridgeAssetLoading.Ready || BridgeStartupAssetSystem.IsStartupInspection || BridgeDeferredRemoval.Saving)
        {
            _began=DateTime.UtcNow;
            return;
        }
        var owner=_owner ?? BridgeDeferredRemoval.ReadyOwners.FirstOrDefault();
        if(owner==null) return;
        try
        {
            if(BridgeLegacyRemoval.IsDetached(owner,out var detached))
            {
                // Version 95 detached this tree before the new game registered assets.
                // The replacement live UUID, if any, is outside this ticket.
                BridgeRemovalFiles.RejectLink(detached);
                Directory.Delete(BridgeFileAccess.Native(detached),true);
                if(BridgeFileAccess.Exists(detached)) throw new IOException("Legacy detached directory remains.");
                BridgeLegacyRemoval.Finish(owner);
                Reset(); BridgeDeferredRemoval.Complete(owner,true); BridgeBulkRemoval.Completed(owner,true);
                return;
            }
            EntityManager.CompleteAllTrackedJobs();
            if(_owner==null)
            {
                _owner=owner;
                _began=DateTime.UtcNow;
                Begin(owner);
                return; // Allow the game's render and prefab phases to consume Deleted notifications.
            }
            if(!Drained(owner))
            {
                if(DateTime.UtcNow-_began>TimeSpan.FromMinutes(2))
                    throw new IOException("The renderer still references this bridge. Its files were kept.");
                return;
            }
            Commit(owner);
            Reset();
            var result=BridgeDeferredRemoval.Result(owner);
            BridgeDeferredRemoval.Complete(owner,true);
            BridgeBulkRemoval.Completed(owner,true);
            Mod.ReloadActiveLocale();
            BridgeRuntimeRequests.Complete("Deleted",result.Name,result.Count);
            Mod.Log.Info("Verified bridge removal in this session: "+owner);
            BridgeBuilder.UI.BridgeBuilderUISystem.RequestRefresh();
        }
        catch(Exception e)
        {
            Reset();
            BridgeDeferredRemoval.Complete(owner,false);
            BridgeRuntimeRequests.Complete("DeleteIncomplete");
            BridgeBulkRemoval.Completed(owner,false);
            Mod.Log.Error("Bridge cleanup incomplete; retry "+owner+": "+e);
        }
    }

    private bool Owned(string owner, string? path) => BridgeOwnedDirectoryRemoval.AssetPath(Root,owner,path)!=null;

    private bool OwnsPrefab(string owner,PrefabBase prefab)
    {
        if(prefab==null || prefab.isBuiltin) return false;
        var path=prefab.asset?.path;
        if(!string.IsNullOrEmpty(path) && BridgeAssetInfo.TryFileOwner(path,out var registeredOwner))
            return string.Equals(owner,registeredOwner,StringComparison.OrdinalIgnoreCase) && Owned(owner,path);
        return BridgeAssetInfo.MatchesOwner(prefab.name,owner);
    }

    private void Begin(string owner)
    {
        BridgeDeferredRemoval.Prepare(owner);
        var system=World.GetOrCreateSystemManaged<PrefabSystem>();
        var owned=PrefabCatalog.GetAll(system).Where(p=>OwnsPrefab(owner,p)).Distinct().ToArray();
        foreach(var p in owned)
            if(system.TryGetEntity(p,out var entity)) _prefabs.Add(entity);
        if(BridgeInstanceRemoval.HasPlacedReferences(EntityManager,_prefabs))
            throw new IOException("Placed instances still reference the bridge.");
        foreach(var a in AssetDatabase.user.AllAssets().OfType<AssetData>())
            if(Owned(owner,a.path)) _assets.Add(a);
        foreach(var p in owned)
            if(system.TryGetEntity(p,out _) && !system.RemovePrefab(p))
                throw new IOException("The native prefab system did not unregister "+p.name);
    }

    private static object? Field(object target,string name)
    {
        var field=target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance)
            ?? throw new MissingFieldException(target.GetType().FullName,name);
        return field.GetValue(target);
    }

    private bool Drained(string owner)
    {
        var meshes=World.GetExistingSystemManaged<BatchMeshSystem>();
        if(meshes!=null)
        {
            meshes.CompleteCaching();
            var infos=(IDictionary)Field(meshes,"m_MeshInfos")!;
            var caching=(IDictionary)Field(meshes,"m_CachingMeshes")!;
            if(_prefabs.Any(e=>infos.Contains(e)||caching.Contains(e))) return false;
            // A surviving composition can borrow the owner's geometry under another entity key.
            foreach(var entry in caching.Values)
                if(Field(entry!,"m_GeometryAsset") is AssetData a && Owned(owner,a.path)) return false;
            foreach(var entry in infos.Values)
                if(Field(entry!,"m_Prefab") is RenderPrefab p && (Owned(owner,p.asset?.path)
                    || BridgeAssetInfo.MatchesOwner(p.name,owner))) return false;
            if(((IEnumerable<GeometryAsset>)Field(meshes,"m_UnloadGeometryAssets")!).Any(a=>Owned(owner,a.path))) return false;
        }
        var streaming=World.GetExistingSystemManaged<TextureStreamingSystem>();
        // These are outstanding async header loaders. Tile read *counters* are lifetime statistics,
        // not fences. ReloadVT below joins the actual request handles before we unlink files.
        if(streaming!=null && ((ICollection)Field(streaming,"m_TexturesAsyncLoaders")!).Count!=0) return false;
        return true;
    }

    private void Commit(string owner)
    {
        string path=Path.Combine(Root,"ImportedData",owner);
        var streaming=World.GetExistingSystemManaged<TextureStreamingSystem>();
        bool resetVT=_assets.Any(a=>a is SurfaceAsset || a is MidMipCacheAsset || a is TextureAsset);
        if(streaming!=null)
        {
            var atlas=Field(streaming,"m_AtlasMaterialsDatabase");
            if(atlas!=null)
            {
                var groups=(List<AtlasMaterialsGrouper>)Field(atlas,"m_AtlasMaterialsGroupers")!;
                if(groups.Count!=0 && groups.All(g=>g.asset!=null && Owned(owner,g.asset.path)))
                    throw new IOException("Refusing to remove the last native VT atlas group.");
                resetVT|=groups.RemoveAll(g=>g.asset!=null && Owned(owner,g.asset.path))>0;
            }
        }
        // The native data source's watcher-removal path detaches its CID first and synchronously
        // notifies LocalAssetDatabase, which drops its name, URI and type indices. The original
        // payload remains readable throughout the native VT reset.
        BridgeNativeRemoval.Detach(AssetDatabase.user,_assets,Root,owner);
        if(resetVT && streaming!=null && Field(streaming,"m_VtDatabase")!=null)
            World.GetOrCreateSystemManaged<ManagedBatchSystem>().ReloadVT();
        void Verify()
        {
            if(AssetDatabase.user.AllAssets().OfType<AssetData>().Any(a=>Owned(owner,a.path)))
                throw new IOException("The user asset index still contains owner paths.");
            var system=World.GetOrCreateSystemManaged<PrefabSystem>();
            if(PrefabCatalog.GetAll(system).Any(p=>OwnsPrefab(owner,p)))
                throw new IOException("The prefab index still contains owner paths.");
        }
        Verify();
        BridgeRemovalFiles.Clear(Root,owner);
        if(BridgeFileAccess.Exists(path)) throw new IOException("The bridge directory still exists: "+owner);
        Verify();
        BridgeLegacyRemoval.Finish(owner);
    }
}
