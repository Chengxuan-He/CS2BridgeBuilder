using System;
using System.Linq;
using BridgeBuilder.Runtime;
using BridgeBuilder.Systems;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

/// <summary>Construction price is a property of the bridge, never an asset dependency.</summary>
internal sealed class BridgeEconomy
{
    internal bool Apply(NetGeometryPrefab root, BridgeStyleVariant variant,
        NetGeometryPrefab upper, NetGeometryPrefab? lower, ExportReport report,
        BridgePriceSystem prices)
    {
        using var composition = new BridgePreviewComposition();
        var prototypeDecks = new[] { variant.Donor }.Concat(
            variant.Donor.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?
                .Select(entry => entry.m_Prefab).OfType<NetGeometryPrefab>()
            ?? Enumerable.Empty<NetGeometryPrefab>()).ToArray();
        long prototypeBridge = 0, prototypeRoads = 0;
        foreach (var deck in prototypeDecks)
        {
            if (!ReadPrototypeCost(composition, deck, true, out var bridgeCost) ||
                !ReadPrototypeCost(composition, deck, false, out var roadCost)) return Fail(report, root);
            prototypeBridge += bridgeCost;
            prototypeRoads += roadCost;
        }
        // The initialized scalar is also read by PrefabUISystem.PlaceableNetCostBinder.
        // Do not reconstruct the selected road's base price from serialized piece objects.
        if (!prices.TryReadSource(upper, out var selected, out var elevated)) return Fail(report, root);
        long second = 0, secondElevated = 0;
        if (lower != null && !prices.TryReadSource(lower, out second, out secondElevated))
            return Fail(report, root);
        if (!BridgeBasePrice.TryCalculate(prototypeBridge, prototypeRoads, selected, second,
            elevated + secondElevated, out var offset, out var price)) return Fail(report, root);

        root.AddOrGetComponent<BridgeConstructionCost>().m_BaseConstructionCost = (uint)price;
        foreach (var entry in root.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets
            ?? Array.Empty<AuxiliaryNetInfo>())
            if (entry.m_Prefab is NetGeometryPrefab deck)
                deck.AddOrGetComponent<BridgeConstructionCost>().m_BaseConstructionCost = 0;
        report.Note($"{root.name}: base price/8m={price}; prototype='{variant.Name}', "
            + $"prototype bridge={prototypeBridge}, prototype road decks={prototypeRoads}, "
            + $"offset={offset}, native selected road decks={selected}+{second}, "
            + $"elevated floor={elevated + secondElevated}, multiplier=3; no pricing prefabs.");
        return true;
    }

    private static bool Fail(ExportReport report, NetGeometryPrefab root)
    {
        report.Failed(root.name, new InvalidOperationException(
            "Unable to resolve initialized road/elevated costs or represent the bridge price."));
        return false;
    }

    // Preserve the verified archetype offset calculation; never modify its dependencies.
    private static bool ReadPrototypeCost(BridgePreviewComposition composition, NetGeometryPrefab prefab,
        bool bridge, out long cost)
    {
        var flags = new CompositionFlags(bridge ? CompositionFlags.General.Elevated : 0, 0, 0);
        var pieces = composition.Compose(prefab, flags, out _, priceComposition: true,
            includeOverhead: bridge);
        cost = 0;
        if (pieces.Length == 0) return false;
        foreach (var piece in pieces)
            if (piece.Prefab.TryGet<PlaceableNetPiece>(out var placement))
                cost += placement.m_ConstructionCost;
        return true;
    }
}
