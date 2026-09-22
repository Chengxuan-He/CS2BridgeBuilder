using System;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal static class BridgeUnlockPolicy
{
    // New prefabs are published during PrefabUpdate, but native UnlockSystem runs in MainLoop.
    // A just-created bridge can therefore retain its initial Locked marker after its prototype
    // has already unlocked (especially while paused). Resolve our exact one-prototype policy
    // before activating, without updating a game system recursively or bypassing a locked donor.
    internal static bool TryPrepareBuild(NetGeometryPrefab bridge, PrefabSystem prefabs,
        EntityManager manager, out bool locked)
    {
        locked = true;
        var rule = bridge.GetComponent<Unlockable>();
        if (rule == null || !rule.m_IgnoreDependencies || rule.m_RequireAll?.Length != 1
            || (rule.m_RequireAny?.Length ?? 0) != 0) return false;
        var prototype = rule.m_RequireAll[0];
        if (prototype == null || !prefabs.TryGetEntity(prototype, out var prototypeEntity)
            || !manager.Exists(prototypeEntity)) return false;
        manager.CompleteAllTrackedJobs();
        // A user-selected runtime override, not a change to the saved prototype requirements.
        // Re-evaluate in both directions so disabling it restores the original restriction.
        locked = Mod.Setting?.RemoveDevelopmentRestrictions != true
            && manager.HasComponent<Locked>(prototypeEntity)
            && manager.IsComponentEnabled<Locked>(prototypeEntity);

        // Resolve the entire owned network before changing anything. Other/shared dependencies
        // are never unlocked; Apply() has installed this exact rule on both generated decks.
        var decks = new System.Collections.Generic.List<Entity>();
        if (!prefabs.TryGetEntity(bridge, out var root) || !manager.Exists(root)) return false;
        decks.Add(root);
        foreach (var entry in bridge.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets
            ?? Array.Empty<AuxiliaryNetInfo>())
        {
            if (entry?.m_Prefab is not NetGeometryPrefab deck) return false;
            var deckRule = deck.GetComponent<Unlockable>();
            if (deckRule == null || !deckRule.m_IgnoreDependencies
                || deckRule.m_RequireAll?.Length != 1
                || !ReferenceEquals(deckRule.m_RequireAll[0], prototype)
                || (deckRule.m_RequireAny?.Length ?? 0) != 0
                || !prefabs.TryGetEntity(deck, out var entity) || !manager.Exists(entity)) return false;
            decks.Add(entity);
        }
        foreach (var entity in decks)
        {
            if (!manager.HasComponent<Locked>(entity))
            {
                if (!locked) continue;
                manager.AddComponent<Locked>(entity);
            }
            if (manager.IsComponentEnabled<Locked>(entity) == locked) continue;
            manager.SetComponentEnabled<Locked>(entity, locked);
            if (locked) continue;
            // Mirror native UnlockPrefab's notification so menus/dependents also see the change.
            var notification = manager.CreateEntity(ComponentType.ReadWrite<Game.Common.Event>(),
                ComponentType.ReadWrite<Unlock>());
            manager.SetComponentData(notification, new Unlock(entity));
        }
        return true;
    }

    internal static bool Apply(NetGeometryPrefab root, BridgeStyleVariant variant, ExportReport report)
    {
        if (variant.Donor == null || ReferenceEquals(root, variant.Donor))
        {
            report.Failed(root.name, new InvalidOperationException("Bridge unlock prototype is unavailable."));
            return false;
        }
        Configure(root, variant.Donor);
        foreach (var entry in root.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets
            ?? Array.Empty<AuxiliaryNetInfo>())
            if (entry.m_Prefab is NetGeometryPrefab deck) Configure(deck, variant.Donor);
        return true;
    }

    private static void Configure(NetGeometryPrefab prefab, PrefabBase prototype)
    {
        // Removing Unlockable alone invokes DefaultLateInitialize, which would re-import the
        // selected road/track and dependency locks. Depend on the original bridge itself:
        // the native unlock graph then preserves its AND/OR, manual, milestone, development
        // and indirect requirements without flattening them or copying current unlocked state.
        // This includes GrandBridgeNode through Grand Bridge, with no style-name exception.
        prefab.components.RemoveAll(component => component is UnlockableBase);
        var unlock = prefab.AddComponent<Unlockable>();
        unlock.m_IgnoreDependencies = true;
        unlock.m_RequireAll = new[] { prototype };
        unlock.m_RequireAny = Array.Empty<PrefabBase>();
    }
}
