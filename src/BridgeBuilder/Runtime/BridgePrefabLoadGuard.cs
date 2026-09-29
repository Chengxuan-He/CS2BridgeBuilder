using BridgeBuilder.Systems;
using Game.Prefabs;
using HarmonyLib;
using System;

namespace BridgeBuilder.Runtime;

/// <summary>Reject malformed owned networks BEFORE native registration/dependency enumeration.</summary>
internal static class BridgePrefabLoadGuard
{
    private static Harmony? _harmony;

    internal static void Start()
    {
        try
        {
            _harmony = new Harmony("BridgeBuilder.PrefabLoadGuard");
            _harmony.Patch(AccessTools.Method(typeof(PrefabSystem), nameof(PrefabSystem.AddPrefab)),
                prefix: new HarmonyMethod(typeof(BridgePrefabLoadGuard), nameof(BeforeAdd)));
            _harmony.Patch(AccessTools.Method(typeof(PrefabSystem), nameof(PrefabSystem.UpdatePrefab)),
                prefix: new HarmonyMethod(typeof(BridgePrefabLoadGuard), nameof(BeforeUpdate)));
            Mod.Log.Info("Bridge prefab load guard installed (before native AddPrefab/UpdatePrefab).");
        }
        catch (Exception exception)
        {
            Mod.Log.Critical(exception, "Bridge prefab load guard could not be installed; damaged assets are not protected.");
        }
    }

    internal static void Stop()
    {
        _harmony?.UnpatchAll("BridgeBuilder.PrefabLoadGuard");
        _harmony = null;
    }

    private static bool BeforeAdd(PrefabBase prefab, ref bool __result)
    {
        if (Allow(prefab)) return true;
        __result = false;
        return false;
    }

    private static bool BeforeUpdate(PrefabBase prefab) => Allow(prefab);

    internal static bool Allow(PrefabBase prefab)
    {
        // Exact bridge/deck identity only. No interception of vanilla, Road Builder,
        // arbitrary UUID-suffixed pieces, or another mod's network registration.
        if (prefab is not NetGeometryPrefab net || net.isBuiltin || net.isReadOnly
            || !BridgeMissingAssetSystem.TryBridgeName(net.name, out _)) return true;
        try
        {
            if (!BridgeNetworkValidation.IsInvalid(net, out var reason)) return true;
            BridgeLoadFailures.Quarantine(net, reason);
            // No placeholder, array surgery, asset deletion, ECS destruction or index remapping.
            // Saved references resolve as obsolete and are retired by the normal PostTool cleanup.
            return false;
        }
        catch (Exception exception)
        {
            // Unknown inspection failures are not evidence authorizing deletion.
            Mod.Log.Warn($"Could not preflight '{net.name}': {exception.Message}");
            return true;
        }
    }
}
