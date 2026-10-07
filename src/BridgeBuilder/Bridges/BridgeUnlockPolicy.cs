using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal static class BridgeUnlockPolicy
{
    internal static bool Apply(NetGeometryPrefab root, BridgeStyleVariant variant, ExportReport report, List<PrefabBase> created)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var prefabs = world?.GetExistingSystemManaged<PrefabSystem>();
        if (prefabs == null || variant.Donor == null || ReferenceEquals(root, variant.Donor)
            || !BridgeUnlockSnapshot.Capture(variant.Donor, prefabs, world!.EntityManager, out var rule))
        {
            report.Failed(root.name, new InvalidOperationException("Original bridge unlock rules are not ready for an independent snapshot."));
            return false;
        }
        if (!BridgeNativeUnlock.Apply(root, rule, prefabs, created, out var error))
        { report.Failed(root.name, new InvalidOperationException(error)); return false; }
        return true;
    }

    internal static bool TryPrepareBuild(NetGeometryPrefab bridge, PrefabSystem prefabs,
        EntityManager manager, out bool locked)
    {
        locked = true;
        if (BridgeSessionState.RestartRequired) return false;
        try
        {
            manager.CompleteAllTrackedJobs();
            if (bridge.GetComponent<Unlockable>() != null)
            {
                if (!prefabs.TryGetEntity(bridge, out var native) || !manager.Exists(native)) return false;
                locked = manager.HasComponent<Locked>(native) && manager.IsComponentEnabled<Locked>(native);
                var available = !locked || Mod.Setting?.RemoveDevelopmentRestrictions == true
                    ? true : BridgeNativeUnlock.Evaluate(bridge, prefabs, manager);
                if (!available.HasValue) return false;
                locked = !available.Value;
                if (!locked)
                {
                    var networks = new List<Entity> { native };
                    foreach (var entry in bridge.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
                    {
                        if (entry?.m_Prefab == null || !prefabs.TryGetEntity(entry.m_Prefab, out var deck)
                            || !manager.Exists(deck)) return false;
                        networks.Add(deck);
                    }
                    foreach (var entity in networks)
                    {
                        if (!manager.HasComponent<Locked>(entity) || !manager.IsComponentEnabled<Locked>(entity)) continue;
                        manager.SetComponentEnabled<Locked>(entity, false);
                        var notification = manager.CreateEntity(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Unlock>());
                        manager.SetComponentData(notification, new Unlock(entity));
                    }
                }
                return true;
            }
            if (!BridgeUnlockSnapshot.Read(bridge, prefabs, manager, out var rule))
            { KeepLocked(bridge, prefabs, manager); return false; }
            var ready = rule.Evaluate(id => BridgeUnlockSnapshot.IsUnlocked(id, prefabs, manager));
            // Missing progression data keeps the gate locked; it is NOT a corrupt bridge.
            locked = Mod.Setting?.RemoveDevelopmentRestrictions != true && ready != true;
            var decks = new List<Entity>();
            if (!prefabs.TryGetEntity(bridge, out var root) || !manager.Exists(root)) return false;
            decks.Add(root);
            foreach (var entry in bridge.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
            {
                if (entry?.m_Prefab is not NetGeometryPrefab deck
                    || !BridgeUnlockSnapshot.Read(deck, prefabs, manager, out var lowerRule)
                    || lowerRule.Encode() != rule.Encode()
                    || !prefabs.TryGetEntity(deck, out var entity) || !manager.Exists(entity))
                { KeepLocked(bridge, prefabs, manager); return false; }
                decks.Add(entity);
            }
            foreach (var entity in decks)
            {
                if (!manager.HasComponent<Locked>(entity)) manager.AddComponent<Locked>(entity);
                if (manager.IsComponentEnabled<Locked>(entity) == locked) continue;
                manager.SetComponentEnabled<Locked>(entity, locked);
                if (locked) continue;
                var notification = manager.CreateEntity(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Unlock>());
                manager.SetComponentData(notification, new Unlock(entity));
            }
            return ready.HasValue || Mod.Setting?.RemoveDevelopmentRestrictions == true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn($"Bridge unlock evaluation deferred for '{bridge.name}': {exception.Message}");
            return false;
        }
    }

    private static void KeepLocked(NetGeometryPrefab bridge, PrefabSystem prefabs, EntityManager manager)
    {
        // A saved unlock flag must not bypass a rule whose progression assets are not ready yet.
        if (prefabs.TryGetEntity(bridge, out var entity) && manager.Exists(entity)
            && manager.HasComponent<Locked>(entity)) manager.SetComponentEnabled<Locked>(entity, true);
        foreach (var entry in bridge.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
        {
            if (entry?.m_Prefab is not NetGeometryPrefab deck
                || !(deck.name == bridge.name + "_Lower" || deck.name == bridge.name + "_Upper")) continue;
            if (prefabs.TryGetEntity(deck, out entity) && manager.Exists(entity)
                && manager.HasComponent<Locked>(entity)) manager.SetComponentEnabled<Locked>(entity, true);
        }
    }
}
