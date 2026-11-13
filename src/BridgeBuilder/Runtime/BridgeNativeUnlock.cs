using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Runtime;

internal static class BridgeNativeUnlock
{
    // Owned explicit groups can be evaluated from their persisted AND/OR rules. Carried native
    // gates use initialized dependency buffers and the same current-state test as UnlockSystem.
    internal static bool? Evaluate(PrefabBase root, PrefabSystem system, Unity.Entities.EntityManager manager)
    {
        var path = new HashSet<PrefabBase>();
        bool? Visit(PrefabBase prefab)
        {
            if (!path.Add(prefab)) return null;
            try
            {
                var gate = prefab.GetComponent<Unlockable>();
                if (gate == null)
                    return BridgeUnlockSnapshot.IsUnlocked(prefab.GetPrefabID().ToUrlSegment(), system, manager);
                // Other native gates include implicit dependencies in their initialized buffer.
                // A current-state query is not a portable snapshot: expanding that graph rejects
                // Created/self/cyclic nodes which the native UnlockSystem simply keeps locked.
                if (!gate.m_IgnoreDependencies)
                    return EvaluateInitialized(prefab, system, manager);
                var all = true;
                var any = false;
                foreach (var child in gate.m_RequireAll ?? Array.Empty<PrefabBase>())
                {
                    var value = Visit(child);
                    if (!value.HasValue) return null;
                    all &= value.Value;
                }
                foreach (var child in gate.m_RequireAny ?? Array.Empty<PrefabBase>())
                {
                    var value = Visit(child);
                    if (!value.HasValue) return null;
                    any |= value.Value;
                }
                return all && (gate.m_RequireAny == null || gate.m_RequireAny.Length == 0 || any);
            }
            finally { path.Remove(prefab); }
        }
        return Visit(root);
    }

    private static bool? EvaluateInitialized(PrefabBase prefab, PrefabSystem system,
        Unity.Entities.EntityManager manager)
    {
        if (!system.TryGetEntity(prefab, out var entity) || !manager.Exists(entity)
            || !manager.HasBuffer<UnlockRequirement>(entity))
        {
            Mod.Log.Warn($"Bridge unlock requirements unavailable: '{prefab.name}' has no initialized native buffer.");
            return null;
        }
        var requirements = manager.GetBuffer<UnlockRequirement>(entity, true);
        var all = true;
        var any = false;
        var hasAny = false;
        for (var i = 0; i < requirements.Length; i++)
        {
            var requirement = requirements[i];
            if (!manager.Exists(requirement.m_Prefab))
            {
                Mod.Log.Warn($"Bridge unlock requirements unavailable: '{prefab.name}' requirement {i} has no live entity.");
                return null;
            }
            var unlocked = !manager.HasComponent<Locked>(requirement.m_Prefab)
                || !manager.IsComponentEnabled<Locked>(requirement.m_Prefab);
            if ((requirement.m_Flags & UnlockFlags.RequireAll) != 0) all &= unlocked;
            if ((requirement.m_Flags & UnlockFlags.RequireAny) != 0)
            { hasAny = true; any |= unlocked; }
        }
        // Mirrors Game.Prefabs.UnlockSystem.CheckUnlockRequirementsJob. No graph mutation,
        // guessed progression state, Created-marker bypass in snapshotting, or disk rewrite.
        return all && (!hasAny || any);
    }

    internal static bool Apply(NetGeometryPrefab root, BridgeUnlockExpression rule, PrefabSystem system,
        List<PrefabBase> created, out string error, bool includeAuxiliary = true)
    {
        error = "";
        var groups = new List<PrefabBase>();
        var failure = "";
        PrefabBase? ConvertRule(BridgeUnlockExpression expression)
        {
            if (expression.Kind == 1)
            {
                var parts = expression.Identity.Split('/');
                var id = new PrefabID(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]),
                    parts.Length == 3 ? Colossal.Hash128.Parse(parts[2]) : default);
                if (!system.TryGetPrefab(id, out var leaf) || leaf == null || leaf is NetPrefab)
                { failure = "Unlock requirement unavailable: " + expression.Identity; return null; }
                return leaf;
            }
            if (expression.Kind != 0) { failure = "Unresolved legacy unlock rule"; return null; }
            var all = new List<PrefabBase>(); var any = new List<PrefabBase>();
            for (var i = 0; i < expression.Children.Length; i++)
            {
                var child = ConvertRule(expression.Children[i]);
                if (child == null) return null;
                if ((expression.Flags[i] & 1) != 0) all.Add(child);
                if ((expression.Flags[i] & 2) != 0) any.Add(child);
            }
            // Equal native requirement sets share one owned node within this bridge.
            // Never share private nodes across independently persisted bridge owners.
            all = all.Distinct().ToList(); any = any.Distinct().ToList();
            foreach (var existing in groups)
            {
                var prior = existing.GetComponent<Unlockable>();
                if (new HashSet<PrefabBase>(prior.m_RequireAll).SetEquals(all)
                    && new HashSet<PrefabBase>(prior.m_RequireAny).SetEquals(any)) return existing;
            }
            var group = ScriptableObject.CreateInstance<AssetPackPrefab>();
            group.name = root.name + " Unlock " + groups.Count;
            groups.Add(group);
            var gate = group.AddComponent<Unlockable>();
            gate.m_IgnoreDependencies = true; gate.m_RequireAll = all.ToArray(); gate.m_RequireAny = any.ToArray();
            return group;
        }
        try
        {
            var requirement = ConvertRule(rule);
            if (requirement == null) { error = failure; return false; }
            var rootGroup = groups.Contains(requirement) ? requirement.GetComponent<Unlockable>() : null;
            var rootAll = rootGroup?.m_RequireAll ?? new[] { requirement };
            var rootAny = rootGroup?.m_RequireAny ?? Array.Empty<PrefabBase>();
            foreach (var net in new[] { root }.Concat(includeAuxiliary ? root.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?
                .Select(a => a.m_Prefab).OfType<NetGeometryPrefab>() ?? Enumerable.Empty<NetGeometryPrefab>() : Enumerable.Empty<NetGeometryPrefab>()))
            {
                net.components.RemoveAll(c => c is UnlockableBase);
                var gate = net.AddComponent<Unlockable>();
                gate.m_IgnoreDependencies = true; gate.m_RequireAll = rootAll.ToArray();
                gate.m_RequireAny = rootAny.ToArray();
            }
            // The outer AND/OR gate belongs on the network itself; no extra Prefab
            // is required to wrap the same native component.
            if (rootGroup != null)
            {
                groups.Remove(requirement);
                UnityEngine.Object.DestroyImmediate(rootGroup);
                UnityEngine.Object.DestroyImmediate(requirement);
            }
            created.AddRange(groups);
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }
}
