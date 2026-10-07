using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Runtime;

internal static class BridgeNativeUnlock
{
    // Publication can finish while paused, before UnlockSystem updates the new group entities.
    // Evaluate their persisted AND/OR rules, using native progression state only at the leaves.
    internal static bool? Evaluate(PrefabBase root, PrefabSystem system, Unity.Entities.EntityManager manager)
    {
        var path = new HashSet<PrefabBase>();
        var budget = 4096;
        bool? Visit(PrefabBase prefab)
        {
            if (prefab == null || --budget < 0 || path.Count >= 64 || !path.Add(prefab)) return null;
            try
            {
                var gate = prefab.GetComponent<Unlockable>();
                if (gate == null)
                    return BridgeUnlockSnapshot.IsUnlocked(prefab.GetPrefabID().ToUrlSegment(), system, manager);
                // Our persisted groups explicitly enumerate every dependency. Other native gates
                // may have implicit requirements, so use their initialized native snapshot.
                if (!gate.m_IgnoreDependencies)
                    return BridgeUnlockSnapshot.Capture(prefab, system, manager, out var rule)
                        ? rule.Evaluate(id => BridgeUnlockSnapshot.IsUnlocked(id, system, manager)) : null;
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
                if (parts.Length < 2 || parts.Length > 3) { failure = "Invalid unlock identity"; return null; }
                var id = new PrefabID(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]),
                    parts.Length == 3 ? Colossal.Hash128.Parse(parts[2]) : default);
                if (!system.TryGetPrefab(id, out var leaf) || leaf == null || leaf is NetPrefab)
                { failure = "Unlock requirement unavailable: " + expression.Identity; return null; }
                return leaf;
            }
            if (expression.Kind != 0) { failure = "Unresolved legacy unlock rule"; return null; }
            // A native asset with no UI membership carries each nested AND/OR group.
            var group = ScriptableObject.CreateInstance<AssetPackPrefab>();
            group.name = root.name + " Unlock " + groups.Count;
            groups.Add(group);
            var all = new List<PrefabBase>(); var any = new List<PrefabBase>();
            for (var i = 0; i < expression.Children.Length; i++)
            {
                var child = ConvertRule(expression.Children[i]);
                if (child == null) return null;
                if ((expression.Flags[i] & 1) != 0) all.Add(child);
                if ((expression.Flags[i] & 2) != 0) any.Add(child);
            }
            var gate = group.AddComponent<Unlockable>();
            gate.m_IgnoreDependencies = true; gate.m_RequireAll = all.ToArray(); gate.m_RequireAny = any.ToArray();
            return group;
        }
        try
        {
            var requirement = ConvertRule(rule);
            if (requirement == null) { error = failure; return false; }
            foreach (var net in new[] { root }.Concat(includeAuxiliary ? root.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?
                .Select(a => a.m_Prefab).OfType<NetGeometryPrefab>() ?? Enumerable.Empty<NetGeometryPrefab>() : Enumerable.Empty<NetGeometryPrefab>()))
            {
                net.components.RemoveAll(c => c is UnlockableBase);
                var gate = net.AddComponent<Unlockable>();
                gate.m_IgnoreDependencies = true; gate.m_RequireAll = new[] { requirement };
                gate.m_RequireAny = Array.Empty<PrefabBase>();
            }
            created.AddRange(groups);
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }
}
