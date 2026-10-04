using Game.Prefabs;
using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2Mods.Shared.Export;

/// <summary>
/// Deletes previously exported assets. Like the writer it never touches <see cref="PrefabSystem"/>:
/// the prefab stays registered in the running world until the game is restarted, only the asset goes.
/// </summary>
internal sealed class PrefabAssetRemover
{
    private readonly PrefabSystem _prefabSystem;
    private readonly ExportSettings _settings;
    private readonly ExportReport _report;

    internal PrefabAssetRemover(PrefabSystem prefabSystem, ExportSettings settings, ExportReport report)
    {
        _prefabSystem = prefabSystem;
        _settings = settings;
        _report = report;
    }

    /// <summary>Returns the names whose exported asset was actually deleted.</summary>
    internal IReadOnlyList<string> Remove(
        IReadOnlyList<RoadBuilderRoad> roads,
        ExportStateStore state,
        bool removeUnusedDependencies)
    {
        var removed = new List<string>();
        foreach (var road in roads)
        {
            var exportName = ModHost.ExportNameOf(road);
            var prefab = FindExportedRoad(exportName);
            if (prefab == null)
            {
                _report.Skipped(exportName, $"no exported asset named '{exportName}' is loaded; restart the game and try again");
                continue;
            }

            try
            {
                prefab.asset!.Delete();
            }
            catch (Exception exception)
            {
                _report.Failed(exportName, exception);
                continue;
            }

            state.Remove(exportName);
            RoadBuilderIconExporter.Discard(exportName);
            _report.Removed(exportName);
            removed.Add(exportName);
        }

        if (removed.Count > 0 && removeUnusedDependencies) RemoveUnusedDependencies(state);
        return removed;
    }

    private void RemoveUnusedDependencies(ExportStateStore state)
    {
        var prefix = _settings.NamePrefix + "Dep_";
        var candidates = PrefabCatalog.GetAll(_prefabSystem)
            .Where(prefab => prefab.asset != null
                && !prefab.isReadOnly
                && !prefab.isBuiltin
                && (prefab.name ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal))
            .ToList();
        if (candidates.Count == 0) return;

        // Deleting a dependency is only safe once every surviving export can be inspected. A road that
        // was exported in this session is not loaded yet, so its dependencies would look unreferenced.
        var referenced = new HashSet<PrefabBase>(ReferenceEqualityComparer<PrefabBase>.Instance);
        foreach (var exportName in state.ExportNames())
        {
            var survivor = FindExportedRoad(exportName);
            if (survivor == null)
            {
                _report.Warning(
                    $"Kept all exported dependencies: the still exported road '{exportName}' is not loaded, "
                    + "so its dependencies cannot be told apart from unused ones. Restart the game and remove again.");
                return;
            }

            PrefabReferenceWalker.CollectInto(survivor, referenced);
        }

        foreach (var candidate in candidates)
        {
            if (referenced.Contains(candidate)) continue;
            try
            {
                candidate.asset!.Delete();
                _report.RemovedDependency(candidate.name);
            }
            catch (Exception exception)
            {
                _report.Warning($"Could not delete the unused dependency '{candidate.name}': {exception.Message}");
            }
        }
    }

    private RoadPrefab? FindExportedRoad(string exportName)
    {
        return PrefabCatalog.GetAll(_prefabSystem).OfType<RoadPrefab>().FirstOrDefault(prefab =>
            prefab.GetType() == typeof(RoadPrefab)
            && string.Equals(prefab.name, exportName, StringComparison.Ordinal)
            && prefab.asset != null
            && !prefab.isReadOnly);
    }
}
