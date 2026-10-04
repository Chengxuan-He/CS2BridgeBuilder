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
    internal bool RemoveInvalidBridgeAfterLoad(string prefabName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName)) return false;
        _settings ??= ExportSettings.Load();
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        RemoveByName(prefabName, state, report);
        var remaining = RemovalRoots(prefabName, PrefabCatalog.GetAll(_prefabSystem)
            .Concat(BridgeLoadFailures.Prefabs())).Any();
        if (!remaining)
        {
            state.Remove(prefabName);
            state.Remove(BridgeNaming.LowerDeckName(prefabName));
            state.Remove(BridgeNaming.CarriedDeckName(prefabName, above: true));
            BridgeRegistrationStore.Remove(prefabName);
        }
        Finish(report, state, "Remove invalid bridge after load", showMessage: false);
        return !remaining;
    }

    internal bool CanRetireBridgeFiles(BridgeDiskAudit audit, ISet<string> names,
        out HashSet<PrefabBase> candidates)
    {
        var comparer = ReferenceEqualityComparer<PrefabBase>.Instance;
        var loaded = PrefabCatalog.GetAll(_prefabSystem).Concat(BridgeLoadFailures.Prefabs())
            .Where(p => p != null).Distinct(comparer).ToArray();
        candidates = new HashSet<PrefabBase>(loaded.Where(p => !p.isBuiltin && !p.isReadOnly
            && (audit.OwnsName(p.name, names)
                || p.asset != null && audit.OwnsPath(p.asset.path, names))), comparer);
        var candidateSet = candidates;
        foreach (var survivor in loaded.Where(p => p.asset != null && !p.isBuiltin
                     && !p.isReadOnly && !candidateSet.Contains(p)))
        {
            var references = new HashSet<PrefabBase>(comparer);
            PrefabReferenceWalker.CollectInto(survivor, references);
            if (!references.Overlaps(candidates)) continue;
            Mod.Log.Warn($"Kept invalid bridge files: surviving asset '{survivor.name}' still references their graph.");
            return false;
        }
        return true;
    }

    internal bool RetireInvalidBridgeFilesAtStartup(BridgeDiskAudit audit, ISet<string> names)
    {
        if (Game.SceneFlow.GameManager.instance == null
            || !BridgeStartupAssetSystem.IsStartupInspection
            || (Game.SceneFlow.GameManager.instance.gameMode & (GameMode.Game | GameMode.Editor)) != 0) return false;
        // A duplicated CID cannot safely be removed through AssetData.Delete(): the
        // database handle may identify the OTHER file. Retire exact validated files
        // together instead, including collision losers which never registered.
        EntityManager.CompleteAllTrackedJobs();
        if (!CanRetireBridgeFiles(audit, names, out var candidates)) return false;
        var entities = new HashSet<Entity>();
        foreach (var candidate in candidates)
            if (_prefabSystem.TryGetEntity(candidate, out var entity)) entities.Add(entity);
        if (BridgeInstanceRemoval.HasPlacedReferences(EntityManager, entities)) return false;
        var backup = Path.Combine(BridgeRecoveryLocation.Path,
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture));
        if (!audit.RetireFiles(names, backup, out var error))
        {
            Mod.Log.Warn("Bridge file retirement failed: " + error);
            return false;
        }
        BridgeStartupRecovery.Retired.UnionWith(names);
        // Keep all live objects, native indices, geometry and materials allocated.
        // Only hide deleted catalogue entries and detach stale disk handles.
        foreach (var candidate in candidates)
        {
            if (candidate is NetGeometryPrefab) HideRemovedBridge(candidate);
            candidate.asset = null;
        }
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        foreach (var name in names)
        {
            foreach (var deck in new[] { name, BridgeNaming.LowerDeckName(name),
                         BridgeNaming.CarriedDeckName(name, above: true) })
            {
                state.Remove(deck);
                RoadBuilderIconExporter.Discard(deck);
            }
            if (BridgeRegistrationStore.TryLoad(out var registrations)
                && registrations.Any(r => r.PrefabName == name) && !BridgeRegistrationStore.Remove(name))
            {
                Mod.Log.Warn($"Could not remove bridge registry entry '{name}'; recovery files: {backup}");
                // Disk retirement already succeeded. Do not suppress its recovery notification.
            }
            report.Removed(name);
        }
        World.GetOrCreateSystemManaged<BridgePublicationSystem>().RefreshMenus(report);
        Finish(report, state, "Retire invalid bridge files", showMessage: false);
        Mod.Log.Info($"Retired {names.Count} invalid bridge file group(s); recovery copies: {backup}");
        return true;
    }

}
