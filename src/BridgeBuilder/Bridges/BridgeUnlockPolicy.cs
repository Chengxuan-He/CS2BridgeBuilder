using System;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class BridgeUnlockPolicy
{
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
