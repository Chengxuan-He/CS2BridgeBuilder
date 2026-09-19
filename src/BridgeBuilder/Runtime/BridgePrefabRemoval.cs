using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Runtime;

/// <summary>
/// An explicit delete operation, never a startup/per-frame cleanup. Replacements point TO their
/// placeholder, so walking only outwards from a road is not a complete bridge dependency graph.
/// </summary>
internal static class BridgePrefabRemoval
{
    internal static IReadOnlyList<PrefabBase> Remove(
        IReadOnlyCollection<PrefabBase> roots,
        IEnumerable<PrefabBase> loaded,
        Func<PrefabBase, bool> isOwnedDependency,
        bool removeDependencies,
        ExportReport report)
    {
        var comparer = ReferenceEqualityComparer<PrefabBase>.Instance;
        var deleted = new List<PrefabBase>();
        try
        {
            var rootSet = new HashSet<PrefabBase>(roots, comparer);
            var all = loaded.Concat(roots).Where(Writable).Distinct(comparer).ToArray();
            var graph = new Dictionary<PrefabBase, HashSet<PrefabBase>>(comparer);
            HashSet<PrefabBase> References(PrefabBase prefab)
            {
                if (graph.TryGetValue(prefab, out var known)) return known;
                var found = new HashSet<PrefabBase>(comparer);
                PrefabReferenceWalker.CollectInto(prefab, found);
                found.Remove(prefab);
                graph.Add(prefab, found);
                return found;
            }

            var candidates = new HashSet<PrefabBase>(roots, comparer);
            void Include(PrefabBase prefab)
            {
                candidates.Add(prefab);
                foreach (var dependency in References(prefab))
                    if (Writable(dependency) && isOwnedDependency(dependency))
                        candidates.Add(dependency);
            }

            if (removeDependencies)
            {
                foreach (var root in roots) Include(root);
                // Include each owned replacement and its meshes/LODs, not just the placeholder.
                // A replacement for another placeholder as well is shared, not ours to remove.
                bool changed;
                do
                {
                    changed = false;
                    foreach (var prefab in all)
                    {
                        if (candidates.Contains(prefab) || !isOwnedDependency(prefab)
                            || !prefab.TryGet<SpawnableObject>(out var spawnable)
                            || spawnable.m_Placeholders == null
                            || spawnable.m_Placeholders.Length == 0
                            || !spawnable.m_Placeholders.All(p => p != null && candidates.Contains(p)))
                            continue;
                        Include(prefab);
                        changed = true;
                    }
                } while (changed);
            }

            // Protect references from ALL surviving user assets, not only export-state roads.
            // Include unknown/unregistered objects and replacements belonging to another bridge.
            var protectedAssets = new HashSet<PrefabBase>(comparer);
            foreach (var survivor in all.Where(p => !candidates.Contains(p)))
                protectedAssets.UnionWith(References(survivor));
            // A surviving road referencing a placeholder still needs the objects which can
            // replace it, even though it has no outward field naming those replacements.
            bool protectedMore;
            do
            {
                protectedMore = false;
                foreach (var candidate in candidates)
                {
                    if (protectedAssets.Contains(candidate)
                        || !candidate.TryGet<SpawnableObject>(out var spawnable)
                        || spawnable.m_Placeholders == null
                        || !spawnable.m_Placeholders.Any(p => p != null && protectedAssets.Contains(p)))
                        continue;
                    protectedAssets.Add(candidate);
                    protectedAssets.UnionWith(References(candidate));
                    protectedMore = true;
                }
            } while (protectedMore);
            foreach (var root in roots)
            {
                if (!protectedAssets.Contains(root)) continue;
                report.Warning($"Kept bridge '{root.name}': another saved prefab still references it.");
                return deleted;
            }
            candidates.ExceptWith(protectedAssets);

            // Delete referrers BEFORE their dependencies. If a Delete fails, don't release its
            // outgoing references: its placeholder and meshes must still exist on next startup.
            // Transitive references are intentional; they also protect the entire failure subtree.
            var incoming = candidates.ToDictionary(p => p, _ => 0, comparer);
            foreach (var candidate in candidates)
                foreach (var dependency in References(candidate))
                    if (incoming.ContainsKey(dependency)) incoming[dependency]++;
            var ready = new Queue<PrefabBase>(candidates
                .Where(p => incoming[p] == 0).OrderByDescending(p => rootSet.Contains(p)));
            while (ready.Count > 0)
            {
                var candidate = ready.Dequeue();
                var asset = candidate.asset!;
                try
                {
                    // AssetDatabase.DeleteAsset removes SourceMeta synchronously but does NOT
                    // unregister this live prefab. Leave the prefab/mesh alive, without a stale
                    // disk handle: isReadOnly -> isPackaged -> asset.GetMeta otherwise keeps
                    // querying an entry that no longer exists (also from delete-event listeners).
                    candidate.asset = null;
                    asset.Delete();
                }
                catch (Exception exception)
                {
                    candidate.asset = asset;
                    report.Warning($"Could not delete '{candidate.name}'; its dependencies were retained: "
                        + exception.Message);
                    // A road which could not be removed still needs its reverse-linked replacements.
                    if (rootSet.Contains(candidate)) break;
                    continue;
                }
                deleted.Add(candidate);
                if (!rootSet.Contains(candidate)) report.RemovedDependency(candidate.name);
                foreach (var dependency in References(candidate))
                    if (incoming.ContainsKey(dependency) && --incoming[dependency] == 0)
                        ready.Enqueue(dependency);
            }
            if (deleted.Count < candidates.Count)
                report.Warning($"Kept {candidates.Count - deleted.Count} bridge prefab(s) because "
                    + "a deletion failed or their references form a cycle; no dependency was cut away.");
        }
        catch (Exception exception)
        {
            report.Warning("Bridge deletion stopped while inspecting asset references: " + exception.Message);
        }
        // Geometry stays allocated while the old render prefabs are registered in the live world.
        // No geometry, mod cache, source asset or unrelated bridge is swept by this operation.
        return deleted;
    }

    private static bool Writable(PrefabBase prefab) =>
        prefab != null && prefab.asset != null && !prefab.isReadOnly && !prefab.isBuiltin;
}
