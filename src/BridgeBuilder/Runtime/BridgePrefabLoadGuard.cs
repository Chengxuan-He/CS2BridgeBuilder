using BridgeBuilder.Systems;
using Game.Prefabs;
using HarmonyLib;
using System;

namespace BridgeBuilder.Runtime;

/// <summary>Reject malformed owned networks BEFORE native registration/dependency enumeration.</summary>
internal static class BridgePrefabLoadGuard
{
    private static Harmony? _harmony;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void Start()
    {
        if (_harmony != null) return;
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
            BridgeLoadFailures.RequireRestart();
            // The title/save recovery system presents the localized recovery warning.
            // Retain the technical exception in the log without a second English popup.
            Mod.Log.Warn(exception, "Bridge prefab load guard could not be installed; damaged assets are not protected.");
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
        // A rejected root is not enough: native loading also registers its section/piece
        // assets independently. Scope those checks to Bridge Builder's exact UUID marker.
        if (prefab == null || !(prefab is NetGeometryPrefab
            || prefab is NetSectionPrefab || prefab is NetPiecePrefab)) return true;
        var verifiedOwned = false;
        try
        {
            var owned = prefab is NetGeometryPrefab
                ? BridgeMissingAssetSystem.TryBridgeName(prefab.name, out _)
                : BridgeLoadFailures.TryOwner(prefab.name, out _);
            // Reading asset metadata may itself fail. Never let that inspection error
            // escape this global registration hook, especially for unrelated prefabs.
            if (!owned || prefab.isBuiltin || prefab.isReadOnly) return true;
            verifiedOwned = true;
            if (BridgeLoadFailures.TryOwner(prefab.name, out var owner)
                && BridgeStartupRecovery.Retired.Contains(owner)) return false;
            if (prefab is NetGeometryPrefab bridge && !BridgeUnlockSnapshot.PrepareLegacy(bridge))
            {
                // Unresolved legacy unlock references are a startup/migration problem, not deletion
                // authority. Keep all files and don't register this partially deserialized network.
                BridgeLoadFailures.Quarantine(prefab, "Automatic unlock repair deferred; assets retained");
                return false;
            }
            if (!BridgeNetworkValidation.IsInvalid(prefab, out var reason)) return true;
            BridgeLoadFailures.Quarantine(prefab, reason);
            // No placeholder, array surgery, asset deletion, ECS destruction or index remapping.
            // Saved references resolve as obsolete and are retired by the normal PostTool cleanup.
            return false;
        }
        catch (Exception exception)
        {
            // Unknown inspection failures are not evidence authorizing deletion.
            Mod.Log.Warn($"Could not preflight '{prefab.name}': {exception.Message}");
            if (!verifiedOwned) return true;
            BridgeLoadFailures.RequireRestart();
            // An uninspectable owned network cannot safely enter native initialization. Retain its
            // files, disable automatic retirement and require recovery; never allow nulls downstream.
            return false;
        }
    }
}
