using Game.Prefabs;
using System;
using System.Collections.Generic;

namespace BridgeBuilder.Runtime;

/// <summary>Check required native network references without editing the loaded asset graph.</summary>
internal static class BridgeNetworkValidation
{
    internal static bool IsInvalid(PrefabBase prefab, out string reason)
        => IsInvalid(prefab, new HashSet<PrefabBase>(
            CS2Mods.Shared.Infrastructure.ReferenceEqualityComparer<PrefabBase>.Instance), out reason);

    private static bool IsInvalid(PrefabBase prefab, HashSet<PrefabBase> visited, out string reason)
    {
        reason = string.Empty;
        if (!visited.Add(prefab)) return false;
        // Matches NetInitializeSystem's direct GetEntity calls, not every nullable prefab
        // field. Aggregate/style/pathfind and many other references are optional natively.
        if (prefab is NetGeometryPrefab net)
        {
            if (BadReferences(net.m_Sections, false, false, s => s.m_Section,
                "m_Sections", visited, out reason)) return true;
            if (net.TryGet<OverheadNetSections>(out var overhead) && overhead.active
                && BadReferences(overhead.m_Sections, false, true, s => s.m_Section,
                    "OverheadNetSections.m_Sections", visited, out reason)) return true;
            if (net.TryGet<UndergroundNetSections>(out var underground) && underground.active
                && BadReferences(underground.m_Sections, false, true, s => s.m_Section,
                    "UndergroundNetSections.m_Sections", visited, out reason)) return true;
        }
        if (prefab is NetPrefab)
        {
            if (prefab.TryGet<NetSubObjects>(out var objects) && objects.active
                && BadReferences(objects.m_SubObjects, false, true, s => s.m_Object,
                    "NetSubObjects.m_SubObjects", visited, out reason)) return true;
            if (prefab.TryGet<AuxiliaryNets>(out var auxiliary) && auxiliary.active
                && BadReferences(auxiliary.m_AuxiliaryNets, false, true, s => s.m_Prefab,
                    "AuxiliaryNets", visited, out reason)) return true;
        }
        if (prefab is NetSectionPrefab section)
        {
            // Native code explicitly accepts absent/empty section and piece arrays.
            if (BadReferences(section.m_SubSections, true, true, s => s.m_Section,
                "m_SubSections", visited, out reason)) return true;
            if (BadReferences(section.m_Pieces, true, true, s => s.m_Piece,
                "m_Pieces", visited, out reason)) return true;
        }
        if (prefab is NetPiecePrefab piece)
        {
            if (piece.TryGet<NetPieceLanes>(out var lanes) && lanes.active
                && BadReferences(lanes.m_Lanes, true, true, s => s.m_Lane,
                    "NetPieceLanes.m_Lanes", visited, out reason)) return true;
            if (piece.TryGet<NetPieceObjects>(out var objects) && objects.active
                && BadReferences(objects.m_PieceObjects, false, true, s => s.m_Object,
                    "NetPieceObjects.m_PieceObjects", visited, out reason)) return true;
            if (piece.TryGet<NetPieceCrosswalk>(out var crosswalk) && crosswalk.active
                && crosswalk.m_Lane == null)
            {
                reason = "NetPieceCrosswalk.m_Lane is null";
                return true;
            }
        }
        if (prefab is NetLanePrefab lane)
        {
            if (lane.TryGet<SecondaryLane>(out var secondary) && secondary.active
                && (BadReferences(secondary.m_LeftLanes, true, true, s => s.m_Lane,
                        "SecondaryLane.m_LeftLanes", visited, out reason)
                    || BadReferences(secondary.m_RightLanes, true, true, s => s.m_Lane,
                        "SecondaryLane.m_RightLanes", visited, out reason)
                    || BadReferences(secondary.m_CrossingLanes, true, true, s => s.m_Lane,
                        "SecondaryLane.m_CrossingLanes", visited, out reason))) return true;
            if (lane.TryGet<AuxiliaryLanes>(out var auxiliary) && auxiliary.active
                && BadReferences(auxiliary.m_AuxiliaryLanes, false, true, s => s.m_Lane,
                    "AuxiliaryLanes.m_AuxiliaryLanes", visited, out reason)) return true;
        }
        if (prefab is NetLaneGeometryPrefab geometry
            && BadReferences(geometry.m_Meshes, false, true, s => s.m_Mesh,
                "NetLaneGeometryPrefab.m_Meshes", visited, out reason)) return true;
        return false;
    }

    private static bool BadReferences<T>(T[] items, bool allowNull, bool allowEmpty,
        Func<T, PrefabBase> reference, string path, HashSet<PrefabBase> visited, out string reason)
    {
        reason = string.Empty;
        if (items == null)
        {
            if (allowNull) return false;
            reason = path + " is null";
            return true;
        }
        if (!allowEmpty && items.Length == 0)
        {
            reason = path + " is empty";
            return true;
        }
        for (var i = 0; i < items.Length; i++)
        {
            var child = items[i] is null ? null : reference(items[i]);
            if (child == null)
            {
                reason = $"{path}[{i}] has a null required prefab reference";
                return true;
            }
            if (IsInvalid(child, visited, out reason))
            {
                reason = $"{path}[{i}] ({child.name}): {reason}";
                return true;
            }
        }
        return false;
    }
}
