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

// Native prefab removal schedules work. A later main-loop update commits only after the target mesh,
// virtual-texture and file readers have drained, on the same main thread as save notifications.
public partial class BridgeOwnerCleanupSystem : GameSystemBase
{
    private BridgeInstanceRemoval? _mapRemoval;
    private DateTime _mapStarted;
    private readonly HashSet<Entity> _mapPrefabs = new();
    private sealed class Retirement
    {
        internal readonly DateTime Began = DateTime.UtcNow;
        internal readonly HashSet<Entity> Prefabs = new();
        internal readonly HashSet<AssetData> Assets = new(new BridgeOwnerMemory.Identity());
        internal string? Wait;
        internal readonly HashSet<UnityEngine.Object> Destroying = new();
        internal bool Unloaded;
        internal string MeshState = "";
    }
    private readonly Dictionary<string,Retirement> _retirements = new(StringComparer.Ordinal);
    private string Root => UnityEngine.Application.persistentDataPath;

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose,mode);
        _mapRemoval = null;
        _retirements.Clear();
    }

    protected override void OnUpdate()
    {
        if (!BridgeBulkRemoval.Update(FinishMapRemoval)) return;
        if(_retirements.Count!=0) foreach(var finished in _retirements.Keys.Where(o=>!BridgeDeferredRemoval.Pending(o)).ToArray())
            _retirements.Remove(finished);
        if(BridgeDeferredRemoval.ReadyOwners.Count==0) return;
        var game=GameManager.instance;
        if (game==null || game.isGameLoading || game.state!=GameManager.State.WorldReady
            || !BridgeAssetLoading.Ready || BridgeStartupAssetSystem.IsStartupInspection || BridgeDeferredRemoval.Saving) return;
        foreach(var owner in BridgeDeferredRemoval.ReadyOwners.ToArray()) ProcessOwner(owner);
    }

    private void ProcessOwner(string owner)
    {
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
                _retirements.Remove(owner); BridgeDeferredRemoval.Complete(owner,true); BridgeBulkRemoval.Completed(owner,true);
                return;
            }
            EntityManager.CompleteAllTrackedJobs();
            if(!_retirements.TryGetValue(owner,out var retirement))
            {
                _retirements.Add(owner,retirement=new Retirement());
                Mod.Log.Info("Beginning native bridge prefab retirement: " + owner);
                Begin(owner,retirement);
                if(retirement.Prefabs.Count!=0)return; // Native prefab consumers run after this main-loop phase.
            }
            var wait=WaitReason(owner,retirement);
            if(wait!=null)
            {
                if(retirement.Wait!=wait) Mod.Log.Info("Bridge retirement waiting: "+owner+"; "+wait);
                retirement.Wait=wait;
                if(DateTime.UtcNow-retirement.Began>TimeSpan.FromMinutes(2))
                    throw new IOException("Native retirement timed out: "+wait+". Its files were kept.");
                return;
            }
            if(!ReleaseMemory(owner,retirement))
            {
                if(DateTime.UtcNow-retirement.Began>TimeSpan.FromMinutes(2))throw new IOException("Native destruction of owner objects is unfinished.");
                return;
            }
            Commit(owner,retirement);
            _retirements.Remove(owner);
            var result=BridgeDeferredRemoval.Result(owner);
            BridgeDeferredRemoval.Complete(owner,true);
            Mod.Log.Info("Verified bridge removal in this session: "+owner);
            BridgeBulkRemoval.Completed(owner,true);
            Mod.ReloadActiveLocale();
            BridgeRuntimeRequests.Complete("Deleted",result.Name,result.Count);
            BridgeBuilder.UI.BridgeBuilderUISystem.RequestRefresh();
        }
        catch(BridgeOwnerMemory.InUse e) when(_retirements.TryGetValue(owner,out var pending) && DateTime.UtcNow-pending.Began<=TimeSpan.FromMinutes(2))
        {
            if(pending.Wait!=e.Message)Mod.Log.Info("Bridge retirement waiting: "+owner+"; "+e.Message);
            pending.Wait=e.Message;
        }
        catch(Exception e)
        {
            _retirements.Remove(owner);
            BridgeDeferredRemoval.Complete(owner,false);
            BridgeRuntimeRequests.Complete("DeleteIncomplete");
            BridgeBulkRemoval.Completed(owner,false);
            Mod.Log.Error("Bridge cleanup incomplete; retry "+owner+": "+e);
        }
    }

    // One plan lets shared junctions see all retiring bridge prefabs before choosing a surviving road.
    private int FinishMapRemoval()
    {
        try
        {
            EntityManager.CompleteAllTrackedJobs();
            if (_mapRemoval == null)
            {
                Mod.Log.Info("Beginning confirmed bridge map removal before the native prefab update.");
                var system = World.GetOrCreateSystemManaged<PrefabSystem>();
                _mapPrefabs.Clear();
                var tools = World.GetOrCreateSystemManaged<Game.Tools.ToolSystem>();
                bool stopPlacement = false;
                foreach (var p in PrefabCatalog.GetAll(system).OfType<NetGeometryPrefab>())
                {
                    if (p.isBuiltin || p.isReadOnly || !BridgeAssetInfo.TryFileOwner(p.name, out var owner)
                        || !BridgeBulkRemoval.IncludesMapOwner(owner) || (p.name != owner && p.name != Bridges.BridgeNaming.LowerDeckName(owner)
                            && p.name != Bridges.BridgeNaming.CarriedDeckName(owner, true))) continue;
                    if (system.TryGetEntity(p, out var entity)) _mapPrefabs.Add(entity);
                    if (p == tools.activePrefab) stopPlacement = true;
                }
                var plan = BridgeInstanceRemoval.Collect(EntityManager, _mapPrefabs);
                if (!plan.CanApply(EntityManager, out var error)) throw new IOException(error);
                if (stopPlacement) tools.ActivatePrefabTool(null);
                if (plan.DeletedEntities.Contains(tools.selected)) tools.selected = Entity.Null;
                _mapRemoval = plan;
                _mapStarted = DateTime.UtcNow;
                Mod.Log.Info($"Applying bridge map removal: {plan.DeletedEntities.Count} retiring entities, {plan.UpdatedEntities.Count} surviving updates.");
                plan.Apply(EntityManager);
            }
            if (!_mapRemoval.IsComplete(EntityManager))
            {
                if (DateTime.UtcNow - _mapStarted > TimeSpan.FromMinutes(2))
                    throw new IOException("The map still contains retiring bridge entities.");
                return -1;
            }
            if (BridgeInstanceRemoval.HasPlacedReferences(EntityManager, _mapPrefabs))
                throw new IOException("A bridge network still references a retiring map prefab.");
            int removed = _mapRemoval.DeletedEntities.Count;
            _mapRemoval = null;
            return removed;
        }
        catch { _mapRemoval = null; throw; }
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

    private void Begin(string owner,Retirement retirement)
    {
        BridgeDeferredRemoval.Prepare(owner);
        var system=World.GetOrCreateSystemManaged<PrefabSystem>();
        var owned=PrefabCatalog.GetAll(system).Where(p=>OwnsPrefab(owner,p)).Distinct().ToArray();
        foreach(var p in owned)
            if(system.TryGetEntity(p,out var entity)) retirement.Prefabs.Add(entity);
        if(BridgeInstanceRemoval.HasPlacedReferences(EntityManager,retirement.Prefabs))
            throw new IOException("Placed instances still reference the bridge.");
        foreach(var a in AssetDatabase.user.AllAssets().OfType<AssetData>())
            if(Owned(owner,a.path)) retirement.Assets.Add(a);
        retirement.Destroying.UnionWith(BridgeOwnerMemory.Instances(retirement.Assets));
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

    private string? WaitReason(string owner,Retirement retirement)
    {
        var meshes=World.GetExistingSystemManaged<BatchMeshSystem>();
        if(meshes!=null)
        {
            BridgeOwnerMemory.JoinMeshReads(meshes,retirement.Prefabs,a=>Owned(owner,a.path));

            var infos=(IDictionary)Field(meshes,"m_MeshInfos")!;
            var caching=(IDictionary)Field(meshes,"m_CachingMeshes")!;
            var targets=new HashSet<Entity>(retirement.Prefabs);
            // A retry may follow earlier successful prefab deregistration in this session.
            foreach(DictionaryEntry pair in infos)
                if(Field(pair.Value!,"m_Prefab") is RenderPrefab render && OwnsPrefab(owner,render))
                    targets.Add((Entity)pair.Key);
            var waiting=targets.Where(e=>infos.Contains(e)||caching.Contains(e)).ToArray();
            if(waiting.Length!=0 || ((IEnumerable<GeometryAsset>)Field(meshes,"m_LoadingGeometries")!).Any(a=>Owned(owner,a.path)))
            {
                retirement.MeshState=BridgeMeshRetirement.Advance(World,meshes,targets,infos,caching,owner,retirement.Destroying,out bool inUse);
                if(inUse)return "active or shared renderer: "+retirement.MeshState;
                foreach(var entity in waiting)
                {
                    if(caching.Contains(entity)) return "mesh cache job "+entity;
                    if(infos[entity] is object info)
                        return $"resident mesh {entity}, prefab={(Field(info,"m_Prefab") as RenderPrefab)?.name}, batchRefs={Field(info,"m_BatchCount")}, entityExists={EntityManager.Exists(entity)}, {retirement.MeshState}";
                }
            }
            BridgeOwnerMemory.ReleaseGeometryRequests(World,meshes,retirement.Assets);
            // A surviving composition can borrow the owner's geometry under another entity key.
            foreach(var entry in caching.Values)
                if(Field(entry!,"m_GeometryAsset") is AssetData a && Owned(owner,a.path)) return "borrowed geometry cache entry";
            foreach(var entry in infos.Values)
                if(Field(entry!,"m_Prefab") is RenderPrefab p && (Owned(owner,p.asset?.path)
                    || BridgeAssetInfo.MatchesOwner(p.name,owner))) return "registered render prefab";
            if(((IEnumerable<GeometryAsset>)Field(meshes,"m_UnloadGeometryAssets")!).Any(a=>Owned(owner,a.path))) return "pending geometry unload";
        }
        var streaming=World.GetExistingSystemManaged<TextureStreamingSystem>();
        // These are outstanding async header loaders. Tile read *counters* are lifetime statistics,
        // not fences. Commit joins the actual request handles before we unlink files.
        if(streaming!=null && !BridgeOwnerMemory.CompleteHeaders(streaming,path=>Owned(owner,path)))
            return "native VT header dependency";
        return null;
    }

    private bool ReleaseMemory(string owner,Retirement retirement)
    {
        if(retirement.Unloaded)
        {
            if(retirement.Assets.Any(a=>a.state!=LoadState.NotLoaded && a.state!=LoadState.Untracked || a is GeometryAsset g && g.data.IsValid))
                throw new IOException("Owner memory was reacquired before file removal.");
            return !retirement.Destroying.Any(o=>o!=null);
        }
        var releaseMaterials=BridgeOwnerMemory.Prepare(World,retirement.Assets,retirement.Destroying);
        var streaming=World.GetExistingSystemManaged<TextureStreamingSystem>();
        if (streaming != null)
        {
            var batches = World.GetExistingSystemManaged<ManagedBatchSystem>();
            if (batches != null) batches.GetVTRequestMaxPixels(out _, out _).Complete();
            BridgeVirtualTextureRemoval.Disconnect(streaming, p => Owned(owner,p), surface => surface.isPersistent && Owned(owner,surface.path));
        }
        releaseMaterials();
        BridgeOwnerMemory.Unload(retirement.Assets);
        retirement.Unloaded=true;
        return !retirement.Destroying.Any(o=>o!=null);
    }

    private void Commit(string owner,Retirement retirement)
    {
        string path=Path.Combine(Root,"ImportedData",owner);
        BridgeNativeRemoval.Detach(AssetDatabase.user,retirement.Assets,Root,owner);
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
