using System;
using System.Reflection;
using Game.Prefabs;
using Game.Tools;

namespace BridgeBuilder.Runtime;

// Called only at confirmed owner retirement, while its prefabs are still registered.
internal static class BridgeToolRetirement
{
    // Native prefab's setter updates m_SelectedPrefab, but GetAvailableSnapMask reads
    // this separate raycast cache even when the tool is inactive. InitializeRaycast
    // normally clears/rebuilds it; do not run that whole raycast outside its native phase.
    private static readonly FieldInfo EffectivePrefab = typeof(NetToolSystem)
        .GetField("m_Prefab", BindingFlags.Instance | BindingFlags.NonPublic)!;

    internal static void Release(ToolSystem tools, NetToolSystem? netTool,
        Func<PrefabBase, bool> owns, Func<NetPrefab> replacement)
    {
        if (tools.activePrefab != null && owns(tools.activePrefab)) tools.ActivatePrefabTool(null);
        if (netTool == null) return;
        if ((netTool.prefab != null && owns(netTool.prefab))
            || (netTool.lane != null && owns(netTool.lane)))
            netTool.prefab = replacement();
        if (EffectivePrefab.GetValue(netTool) is PrefabBase effective && owns(effective))
            EffectivePrefab.SetValue(netTool, null);
    }
}
