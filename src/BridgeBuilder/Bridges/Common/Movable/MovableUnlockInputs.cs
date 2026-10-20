using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal static class MovableUnlockInputs
{
    internal static bool Apply(NetGeometryPrefab target, ExportReport report, List<PrefabBase> created,
        PrefabID[] dependencies, PrefabID[] requireAll, PrefabID[] requireAny, bool ignoreDependencies)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var prefabs = world?.GetExistingSystemManaged<PrefabSystem>();
        if (prefabs == null)
        {
            report.Failed(target.name, new InvalidOperationException("Native unlock resources are not initialized."));
            return false;
        }
        PrefabBase[] Resolve(PrefabID[] ids)
        {
            var result = new PrefabBase[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                if (!prefabs.TryGetPrefab(ids[i], out result[i]) || result[i] == null || !result[i].isBuiltin)
                    throw new InvalidOperationException("Recorded native unlock input unavailable: " + ids[i].ToUrlSegment());
            }
            return result;
        }
        try
        {
            if (!BridgeUnlockSnapshot.CaptureRequirements(
                ignoreDependencies ? Array.Empty<PrefabBase>() : Resolve(dependencies),
                Resolve(requireAll), Resolve(requireAny), ignoreDependencies,
                prefabs, world!.EntityManager, out var rule))
                throw new InvalidOperationException("Recorded native unlock inputs are not ready for an independent snapshot.");
            if (!BridgeNativeUnlock.Apply(target, rule, prefabs, created, out var error))
                throw new InvalidOperationException(error);
            return true;
        }
        catch (Exception exception)
        {
            report.Failed(target.name, exception);
            return false;
        }
    }
}
