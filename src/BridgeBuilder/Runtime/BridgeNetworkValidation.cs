using Game.Prefabs;
using System.Collections.Generic;

namespace BridgeBuilder.Runtime;

/// <summary>Validate the arrays native AddSections/AuxiliaryNets dereference, without mutating them.</summary>
internal static class BridgeNetworkValidation
{
    internal static bool IsInvalid(NetGeometryPrefab net, out string reason)
        => IsInvalid(net, new HashSet<NetGeometryPrefab>(
            CS2Mods.Shared.Infrastructure.ReferenceEqualityComparer<NetGeometryPrefab>.Instance), out reason);

    private static bool IsInvalid(NetGeometryPrefab net, HashSet<NetGeometryPrefab> visited, out string reason)
    {
        reason = string.Empty;
        if (!visited.Add(net)) return false;
        if (BadSections(net.m_Sections, false, "m_Sections", out reason)) return true;
        if (net.TryGet<OverheadNetSections>(out var overhead)
            && BadSections(overhead.m_Sections, true, "OverheadNetSections.m_Sections", out reason)) return true;
        if (net.TryGet<UndergroundNetSections>(out var underground)
            && BadSections(underground.m_Sections, true, "UndergroundNetSections.m_Sections", out reason)) return true;
        if (!net.TryGet<AuxiliaryNets>(out var auxiliary)) return false;
        if (auxiliary.m_AuxiliaryNets == null)
        {
            reason = "AuxiliaryNets.m_AuxiliaryNets is null";
            return true;
        }
        for (var i = 0; i < auxiliary.m_AuxiliaryNets.Length; i++)
        {
            var item = auxiliary.m_AuxiliaryNets[i];
            if (item == null || item.m_Prefab == null)
            {
                reason = $"AuxiliaryNets[{i}].m_Prefab is null";
                return true;
            }
            // Read the full carried network graph, including cycles. Never modify a shared donor.
            if (item.m_Prefab is NetGeometryPrefab child && IsInvalid(child, visited, out reason))
            {
                reason = $"AuxiliaryNets[{i}] ({child.name}): {reason}";
                return true;
            }
        }
        return false;
    }

    private static bool BadSections(NetSectionInfo[] sections, bool allowEmpty, string path, out string reason)
    {
        reason = string.Empty;
        if (sections == null || (!allowEmpty && sections.Length == 0))
        {
            reason = path + (sections == null ? " is null" : " is empty");
            return true;
        }
        for (var i = 0; i < sections.Length; i++)
            if (sections[i] == null || sections[i].m_Section == null)
            {
                reason = $"{path}[{i}].m_Section is null";
                return true;
            }
        return false;
    }
}
