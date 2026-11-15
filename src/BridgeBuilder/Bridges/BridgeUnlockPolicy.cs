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
            return false; // Current generated bridges persist native Unlockable rules.

        }
        catch (Exception exception)
        {
            Mod.Log.Warn($"Bridge unlock evaluation deferred for '{bridge.name}': {exception.Message}");
            return false;
        }
    }

}
