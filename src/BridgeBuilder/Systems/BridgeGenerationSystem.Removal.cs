using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using BridgeBuilder.UI;
using Colossal.Serialization.Entities;
using CS2Mods.Shared;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    internal bool IsRemoving => _pendingRemoval != null;

    internal void SuspendForCleanup()
    {
        ClearPreview();
        BridgePreviewState.Clear();
        World.GetOrCreateSystemManaged<ToolSystem>().ActivatePrefabTool(null);
    }

    private IReadOnlyList<string> RemoveByName(string exportName, ExportStateStore state, ExportReport report)
    {
        var removed = new List<string>();
        var loaded = PrefabCatalog.GetAll(_prefabSystem).Concat(BridgeLoadFailures.Prefabs()).Distinct().ToArray();
        var roots = RemovalRoots(exportName, loaded).ToArray();
        if (roots.Length == 0)
        {
            report.Skipped(exportName, "no exported asset with that name is loaded");
            return removed;
        }
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
        // Registered prefab/composition entities remain valid until world teardown. Do not call
        // RemovePrefab (which invalidates PrefabData indices) or unload their meshes here.
        var uuidOwner = BridgeRegistration.IsPrefabName(exportName);
        var sharedPrefix = _settings.NamePrefix + "Dep_";
        var deleted = BridgePrefabRemoval.Remove(roots, loaded,
            candidate => (candidate.name ?? string.Empty).StartsWith(sharedPrefix, StringComparison.Ordinal)
                || (uuidOwner && (candidate.name ?? string.Empty).Contains(exportName)),
            Mod.Setting?.RemoveUnusedDependencies ?? true, report);
        foreach (var root in roots)
        {
            if (!deleted.Contains(root)) continue;
            HideRemovedBridge(root);
            state.Remove(root.name);
            RoadBuilderIconExporter.Discard(root.name);
            report.Removed(root.name);
            removed.Add(root.name);
        }
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
