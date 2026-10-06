using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;





using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;




using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;


using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{

    private IReadOnlyList<string> RemoveByName(string exportName, ExportStateStore state, ExportReport report)
    {
        var removed = new List<string>();
        var loaded = PrefabCatalog.GetAll(_prefabSystem).Distinct().ToArray();
        var roots = RemovalRoots(exportName, loaded).ToArray();
        var rootEntities = new HashSet<Entity>();
        foreach (var root in roots)
            if (_prefabSystem.TryGetEntity(root, out var entity)) rootEntities.Add(entity);
        // Also protects the settings-page remover and a shared junction that has not yet been
        // reassigned by the native network update. Never delete its backing asset prematurely.
        if (BridgeInstanceRemoval.HasPlacedReferences(EntityManager, rootEntities))
        {
            report.Warning($"Kept '{exportName}': a placed network entity still references it.");
            return removed;
        }
        var tools = World.GetOrCreateSystemManaged<ToolSystem>();
        if (roots.Contains(tools.activePrefab)) tools.ActivatePrefabTool(null);
        // Deactivating NetToolSystem does not clear its selected prefab. OnGamePreload
        // reads that selection even while inactive; replace it before removing registration.
        var netTool = World.GetExistingSystemManaged<NetToolSystem>();
        if (netTool != null && (BridgeAssetInfo.MatchesOwner(netTool.prefab?.name, exportName)
            || BridgeAssetInfo.MatchesOwner(netTool.lane?.name, exportName)))
        {
            var replacement = loaded.OfType<RoadPrefab>().FirstOrDefault(p => p.isBuiltin
                && !BridgeAssetInfo.MatchesOwner(p.name, exportName) && _prefabSystem.TryGetEntity(p, out _));
            if (replacement == null)
            {
                report.Warning("Cannot clear bridge tool selection: no registered built-in road is available.");
                return removed;
            }
            // The native setter cannot accept null: it directly looks up its dictionary key.
            netTool.prefab = replacement;
            Mod.Log.Info("Bridge deletion replaced retained NetTool selection with: " + replacement.name);
        }
        // Placed instances have finished native cleanup before unregistering their prefabs.
        var audit = BridgeDiskAudit.ForMemoryFailures(UnityEngine.Application.persistentDataPath,
            new Dictionary<string, string> { [exportName] = "User requested removal" });
        if (!audit.Complete) { report.Warning(audit.Error); return removed; }
        if (!audit.RetireFiles(new HashSet<string> { exportName }, BridgeRecoveryLocation.Path, out var moveError))
        { report.Warning(moveError); return removed; }
        BridgeStartupRecovery.Retired.Add(exportName);
        foreach (var root in roots) HideRemovedBridge(root);
        var unregisterFailed = false;
        foreach (var prefab in loaded.Where(p => BridgeAssetInfo.MatchesOwner(p.name, exportName)))
        {
            // Native removal updates swapped PrefabData indices and removes current/legacy IDs.
            // Keep Unity objects and meshes alive for pending native cleanup; do not Destroy them.
            if (!_prefabSystem.TryGetEntity(prefab, out _)) continue;
            if (!_prefabSystem.RemovePrefab(prefab))
            {
                unregisterFailed = true;
                report.Warning("Could not unregister removed bridge prefab: " + prefab.name);
                continue;
            }
            prefab.asset = null;
            Mod.Log.Info("Bridge prefab unregistered after file removal: " + prefab.name);
        }
        if (unregisterFailed) return removed;
        foreach (var root in roots)
        {
            state.Remove(root.name);
            RoadBuilderIconExporter.Discard(root.name);
            report.Removed(root.name);
            removed.Add(root.name);
        }
        if (!removed.Contains(exportName)) removed.Add(exportName);
        if (removed.Count > 0)
            World.GetOrCreateSystemManaged<BridgePublicationSystem>().RefreshMenus(report);
        return removed;
    }

    private static IEnumerable<PrefabBase> RemovalRoots(string exportName, IEnumerable<PrefabBase> loaded)
    {
        // The lower deck, when there is one, is a second asset next to the bridge and has to go too.
        foreach (var name in new[]
                 {
                     exportName,
                     BridgeNaming.LowerDeckName(exportName),
                     BridgeNaming.CarriedDeckName(exportName, above: true),
                 })
        {
            var prefab = loaded
                .OfType<NetGeometryPrefab>()
                .FirstOrDefault(candidate => candidate.asset != null
                    && !candidate.isReadOnly
                    && string.Equals(candidate.name, name, StringComparison.Ordinal));
            if (prefab == null)
            {
                continue;
            }

            yield return prefab;
        }

    }

    private void HideRemovedBridge(PrefabBase root)
    {
        if (root.TryGet<UIObject>(out var ui))
        {
            ui.m_IsDebugObject = true;
            ui.m_Group = null;
        }
        if (!_prefabSystem.TryGetEntity(root, out var entity)
            || !EntityManager.HasComponent<UIObjectData>(entity)) return;
        var data = EntityManager.GetComponentData<UIObjectData>(entity);
        if (EntityManager.Exists(data.m_Group) && EntityManager.HasBuffer<UIGroupElement>(data.m_Group))
        {
            var group = EntityManager.GetBuffer<UIGroupElement>(data.m_Group);
            for (var i = group.Length - 1; i >= 0; i--)
                if (group[i].m_Prefab == entity) group.RemoveAt(i);
        }
        data.m_Group = Entity.Null;
        EntityManager.SetComponentData(entity, data);
    }

}
