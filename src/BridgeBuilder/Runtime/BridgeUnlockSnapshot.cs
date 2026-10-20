using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Runtime;

internal static class BridgeUnlockSnapshot
{
    // For reconstructed archetypes: callers supply the measured native dependency
    // list as well as explicit rules. Do not substitute the selected road's graph.
    // Resource dependencies remain initialized native Prefabs; the original bridge
    // network itself need not exist. Mirrors Unlockable.LateInitialize filtering.
    internal static bool CaptureRequirements(IEnumerable<PrefabBase> dependencies,
        IEnumerable<PrefabBase> requireAll, IEnumerable<PrefabBase> requireAny,
        bool ignoreDependencies, PrefabSystem prefabs, EntityManager manager,
        out BridgeUnlockExpression rule)
    {
        rule = new();
        var children = new List<BridgeUnlockExpression>();
        var flags = new List<byte>();
        var budget = 4096;
        var path = new HashSet<Entity>();
        bool Append(IEnumerable<PrefabBase> inputs, byte flag)
        {
            foreach (var source in inputs)
            {
                if (source == null) return false;
                if (!prefabs.IsUnlockable(source)) continue;
                if (!prefabs.TryGetEntity(source, out var entity) ||
                    !Capture(entity, prefabs, manager, path, ref budget, out var child)) return false;
                children.Add(child);
                flags.Add(flag);
            }
            return true;
        }
        if ((!ignoreDependencies && !Append(dependencies, 1)) ||
            !Append(requireAll, 1) || !Append(requireAny, 2)) return false;
        rule.Children = children.ToArray();
        rule.Flags = flags.ToArray();
        return true;
    }

    internal static bool Capture(PrefabBase source, PrefabSystem prefabs, EntityManager manager,
        out BridgeUnlockExpression rule)
    {
        rule = new();
        if (source == null || !prefabs.TryGetEntity(source, out var entity)) return false;
        var budget = 4096;
        return Capture(entity, prefabs, manager, new HashSet<Entity>(), ref budget, out rule);
    }

    private static bool Capture(Entity entity, PrefabSystem prefabs, EntityManager manager,
        HashSet<Entity> path, ref int budget, out BridgeUnlockExpression rule)
    {
        rule = new();
        if (--budget < 0 || path.Count >= 64 || !manager.Exists(entity)
            || !prefabs.TryGetPrefab<PrefabBase>(entity, out var source) || source == null
            || manager.HasComponent<Game.Common.Created>(entity)) return false;
        if (!manager.HasComponent<Locked>(entity)) return true;
        if (!manager.HasBuffer<UnlockRequirement>(entity)) return false;
        var requirements = manager.GetBuffer<UnlockRequirement>(entity, true);
        // Native manual/progression prefabs use a self-edge and are changed by their own systems.
        // Preserve those leaves by VALUE identity, never by their current unlocked state.
        for (var i = 0; i < requirements.Length; i++)
            if (requirements[i].m_Prefab == entity)
            {
                // Do not silently reintroduce a network dependency for an unsupported manual donor.
                if (source is NetPrefab) return false;
                rule.Kind = 1;
                rule.Identity = source.GetPrefabID().ToUrlSegment();
                return true;
            }
        if (!path.Add(entity)) return false;
        rule.Children = new BridgeUnlockExpression[requirements.Length];
        rule.Flags = new byte[requirements.Length];
        for (var i = 0; i < requirements.Length; i++)
        {
            var edge = requirements[i];
            var flags = (uint)edge.m_Flags;
            if (flags < 1 || flags > 3) { path.Remove(entity); return false; }
            rule.Flags[i] = (byte)flags;
            if (!Capture(edge.m_Prefab, prefabs, manager, path, ref budget, out rule.Children[i]))
            { path.Remove(entity); return false; }
        }
        path.Remove(entity);
        return true;
    }

    internal static bool? IsUnlocked(string identity, PrefabSystem prefabs, EntityManager manager)
    {
        var parts = identity.Split('/');
        if (parts.Length < 2 || parts.Length > 3) return null;
        var id = new PrefabID(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]),
            parts.Length == 3 ? Colossal.Hash128.Parse(parts[2]) : default);
        if (!prefabs.TryGetPrefab(id, out var source) || source == null || source is NetPrefab
            || !prefabs.TryGetEntity(source, out var entity) || !manager.Exists(entity)
            || manager.HasComponent<Game.Common.Created>(entity)) return null;
        return !manager.HasComponent<Locked>(entity) || !manager.IsComponentEnabled<Locked>(entity);
    }

    internal static bool Read(NetGeometryPrefab bridge, PrefabSystem prefabs, EntityManager manager,
        out BridgeUnlockExpression rule)
    {
        rule = new();
        var gate = bridge.GetComponent<ManualUnlockable>();
        if (gate == null || !BridgeUnlockExpression.TryDecode(gate.name, out rule)) return false;
        if (rule.Kind != 2) return true;
        if (bridge.isBuiltin || bridge.isReadOnly) return false;
        // Resolve only registered, initialized instances. Never call PrefabAsset.Load or GetEntity(null).
        PrefabBase? prototype = null;
        if (rule.Identity.StartsWith("CID:", StringComparison.Ordinal))
        {
            if (AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(rule.Identity.Substring(4)),
                out PrefabAsset asset)) prototype = asset.GetInstance<PrefabBase>();
        }
        else if (rule.Identity.StartsWith("UnityGUID:", StringComparison.Ordinal)
            && AssetDatabase.global.resources.prefabsMap.TryGetObject(rule.Identity.Substring(10), out var value))
            prototype = value as PrefabBase;
        else if (rule.Identity.StartsWith("ID:", StringComparison.Ordinal))
        {
            var parts = rule.Identity.Substring(3).Split('/');
            if (parts.Length < 2 || parts.Length > 3) return false;
            prefabs.TryGetPrefab(new PrefabID(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]),
                parts.Length == 3 ? Colossal.Hash128.Parse(parts[2]) : default), out prototype);
        }
        if (prototype == null || !Capture(prototype, prefabs, manager, out var snapshot)) return false;
        rule = snapshot;
        return true;
    }
}
