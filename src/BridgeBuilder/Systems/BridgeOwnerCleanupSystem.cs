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

// Logical retirement only. Native users and physical files remain alive until shutdown.
public partial class BridgeOwnerCleanupSystem : GameSystemBase
{
    private BridgeInstanceRemoval? _mapRemoval;
    private DateTime _mapStarted;
    private readonly HashSet<Entity> _mapPrefabs = new();
    private sealed class Retirement
    {
        internal readonly DateTime Began = DateTime.UtcNow;
        internal readonly HashSet<Entity> Prefabs = new();
        internal BridgeInstanceRemoval? Map;
        internal bool Unregistered;
    }
    private readonly Dictionary<string,Retirement> _retirements = new(StringComparer.Ordinal);
    private string Root => UnityEngine.Application.persistentDataPath;

    protected override void OnCreate()
    {
        base.OnCreate();
        BridgeBulkRemoval.NativeReady = () => BridgePrefabRetirement.Ready(EntityManager,World.GetOrCreateSystemManaged<PrefabSystem>());
        BridgeBulkRemoval.PlacedOwners = () =>
        {
            var system = World.GetOrCreateSystemManaged<PrefabSystem>();
            return PrefabCatalog.GetAll(system).OfType<NetGeometryPrefab>()
                .Where(p=>!p.isBuiltin && BridgeAssetInfo.TryFileOwner(p.name,out _))
                .Select(p=>{ BridgeAssetInfo.TryFileOwner(p.name,out var owner); return owner; }).Distinct().ToArray();
        };
    }

    protected override void OnDestroy()
    {
        BridgeBulkRemoval.NativeReady = null;
        BridgeBulkRemoval.PlacedOwners = null;
        base.OnDestroy();
    }

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose,mode);
        _mapRemoval = null;
        _retirements.Clear();
        BridgeDeferredRemoval.Verified.Clear();
        BridgeShutdownCleanup.WorldChanging();
        BridgeDeferredRemoval.Resume();
    }

    protected override void OnUpdate()
    {
        if (!BridgeBulkRemoval.Update(FinishMapRemoval)) return;
        if(_retirements.Count!=0) foreach(var finished in _retirements.Keys.Where(o=>!BridgeDeferredRemoval.Pending(o)).ToArray())
            _retirements.Remove(finished);
        if(BridgeDeferredRemoval.ReadyOwners.Count==0) return;
        if(!BridgePrefabRetirement.Ready(EntityManager,World.GetOrCreateSystemManaged<PrefabSystem>())) return;
        foreach(var owner in BridgeDeferredRemoval.ReadyOwners.ToArray()) ProcessOwner(owner);
    }

    private void ProcessOwner(string owner)
    {
        bool silent = BridgeDeferredRemoval.IsSilent(owner);
        try
        {
            BridgeDeletionIntent.Verify(Root, owner);
            EntityManager.CompleteAllTrackedJobs();
            if(!_retirements.TryGetValue(owner,out var retirement))
            {
                retirement = new Retirement();
                _retirements.Add(owner,retirement);
                var system = World.GetOrCreateSystemManaged<PrefabSystem>();
                foreach(var prefab in PrefabCatalog.GetAll(system).Where(p=>OwnsPrefab(owner,p)))
                    if(system.TryGetEntity(prefab,out var entity)) retirement.Prefabs.Add(entity);
                var plan = BridgeInstanceRemoval.Collect(EntityManager,retirement.Prefabs);
                if(!plan.CanApply(EntityManager,out var error)) throw new IOException(error);
                retirement.Map = plan;
                var netTool = World.GetExistingSystemManaged<Game.Tools.NetToolSystem>();
                if(netTool != null && ((netTool.prefab != null && OwnsPrefab(owner,netTool.prefab))
                    || (netTool.lane != null && OwnsPrefab(owner,netTool.lane))))
                {
                    var replacement = PrefabCatalog.GetAll(system).OfType<RoadPrefab>()
                        .FirstOrDefault(p=>p.isBuiltin && system.TryGetEntity(p,out _));
                    if(replacement == null) throw new IOException("No registered built-in road can replace the retained tool selection.");
                    netTool.prefab = replacement;
                }
                var tools = World.GetOrCreateSystemManaged<Game.Tools.ToolSystem>();
                if(tools.activePrefab != null && OwnsPrefab(owner,tools.activePrefab)) tools.ActivatePrefabTool(null);
                if(plan.DeletedEntities.Contains(tools.selected)) tools.selected = Entity.Null;
                plan.Apply(EntityManager);
            }
            if(!retirement.Map!.IsComplete(EntityManager)
                || BridgeInstanceRemoval.HasPlacedReferences(EntityManager,retirement.Prefabs))
            {
                if(DateTime.UtcNow-retirement.Began>TimeSpan.FromMinutes(2))
                    throw new IOException("Map removal did not complete; durable intent and files retained.");
                return;
            }
            var prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
            if(!retirement.Unregistered)
            {
                if(!BridgePrefabRetirement.Ready(EntityManager,prefabs)) return;
                BridgeDeferredRemoval.SilentLog.Info("Unregistering initialized bridge prefabs; UUID="+owner+"; mode="+GameManager.instance.gameMode);
                // Native removal notifications own resource retirement. Do not unload shared
                // Surface/VT/geometry allocations or detach physical asset registrations here.
                foreach(var p in PrefabCatalog.GetAll(prefabs).Where(p=>OwnsPrefab(owner,p)).ToArray())
                                    {
                    if(p.TryGet<UIObject>(out var ui)) { ui.m_IsDebugObject=true; ui.m_Group=null; }
                    if(prefabs.TryGetEntity(p,out var e))
                    {
                        if(EntityManager.HasComponent<UIObjectData>(e))
                        {
                            var data=EntityManager.GetComponentData<UIObjectData>(e);
                            if(EntityManager.Exists(data.m_Group) && EntityManager.HasBuffer<UIGroupElement>(data.m_Group))
                            {
                                var entries=EntityManager.GetBuffer<UIGroupElement>(data.m_Group);
                                for(int i=entries.Length-1;i>=0;i--) if(entries[i].m_Prefab==e) entries.RemoveAt(i);
                            }
                            data.m_Group=Entity.Null; EntityManager.SetComponentData(e,data);
                        }
                        if(!prefabs.RemovePrefab(p)) throw new IOException("Native prefab removal failed: "+p.name);
                    }
                }
                retirement.Unregistered = true;
                return;
            }

            var result = BridgeDeferredRemoval.Result(owner);
            _retirements.Remove(owner);
            BridgeDeferredRemoval.Complete(owner,true);
            BridgeShutdownCleanup.LogicallyCompleted(owner);
            if(!silent)
            {
                Mod.Log.Info("Bridge logical deletion complete; files pending verified shutdown: "+owner);
                BridgeBulkRemoval.Completed(owner,true);
                BridgeRuntimeRequests.Complete("Deleted",result.Name,result.Count);
            }
            BridgeBuilder.UI.BridgeBuilderUISystem.RequestRefresh();
        }
        catch(Exception e)
        {
            _retirements.Remove(owner);
            BridgeDeferredRemoval.Complete(owner,false);
            if(!silent)
            {
                BridgeRuntimeRequests.Complete("DeleteIncomplete");
                BridgeBulkRemoval.Completed(owner,false);
            }
            (silent ? BridgeDeferredRemoval.SilentLog : Mod.Log).Error("Bridge logical deletion incomplete; UUID="+owner+"; stage=map-or-unregister; remainingCount=1; "+e);
        }
    }

    // One plan lets shared junctions see all retiring bridge prefabs before choosing a surviving road.
    private int FinishMapRemoval()
    {
        if(!BridgePrefabRetirement.Ready(EntityManager,World.GetOrCreateSystemManaged<PrefabSystem>())) return -1;
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

}
