using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Base construction charge per native 8 m unit. Each selected archetype supplies its own
/// intercept; this is not a family-wide guessed surcharge. Height/upkeep and object charges
/// remain native. Only private pricing copies are edited, never shared road/pack pieces.
/// </summary>
internal sealed class BridgeEconomy
{
    private readonly Dictionary<NetSectionPrefab, NetSectionPrefab> _sections = new();
    private readonly Dictionary<NetPiecePrefab, NetPiecePrefab> _pieces = new();
    internal readonly List<PrefabCloneNode> Nodes = new();
    private string _prefix = string.Empty;

    internal bool Apply(NetGeometryPrefab root, BridgeStyleVariant variant,
        NetGeometryPrefab upper, NetGeometryPrefab? lower, ExportReport report)
    {
        using var composition = new BridgePreviewComposition();
        var prototypeDecks = new[] { variant.Donor }.Concat(
            variant.Donor.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?
                .Select(entry => entry.m_Prefab).OfType<NetGeometryPrefab>()
            ?? Enumerable.Empty<NetGeometryPrefab>()).ToArray();
        long prototypeBridge = 0, prototypeRoads = 0;
        foreach (var deck in prototypeDecks)
        {
            if (!ReadCost(composition, deck, true, out var bridgeCost) ||
                !ReadCost(composition, deck, false, out var roadCost)) return Fail(report, root);
            prototypeBridge += bridgeCost;
            prototypeRoads += roadCost;
        }
        if (!ReadCost(composition, upper, false, out var selected)) return Fail(report, root);
        long second = 0;
        if (lower != null)
        {
            if (!ReadCost(composition, lower, false, out second)) return Fail(report, root);
        }
        // Preserve the signed intercept. A negative total cannot be represented by the game's
        // uint cost; reject that combination rather than wrap or silently clamp the formula.
        if (!BridgeBasePrice.TryCalculate(prototypeBridge, prototypeRoads, selected, second,
            out var offset, out var price))
        {
            report.Failed(root.name, new InvalidOperationException(
                $"Base bridge price {price} is outside the native supported range; offset={offset}."));
            return false;
        }
        _prefix = root.name + "_Pricing";
        ZeroBaseCosts(root);
        foreach (var entry in root.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets
            ?? Array.Empty<AuxiliaryNetInfo>())
            if (entry.m_Prefab is NetGeometryPrefab deck) ZeroBaseCosts(deck);

        // One invisible, zero-width native piece owns both decks' base charge. It has no
        // geometry/lane/height/upkeep effects, and remains present in every composition state.
        var charge = PrefabBase.Create<NetPiecePrefab>(_prefix + "_Charge");
        charge.m_Layer = NetPieceLayer.Top;
        charge.m_Width = 0f;
        charge.m_Length = 64f;
        charge.m_HeightRange = new Colossal.Mathematics.Bounds1(0f, 0f);
        charge.m_NodeOffset = 0f;
        charge.AddComponent<PlaceableNetPiece>().m_ConstructionCost = (uint)price;
        Track(charge);
        var section = PrefabBase.Create<NetSectionPrefab>(_prefix + "_ChargeSection");
        section.m_SubSections = Array.Empty<NetSubSectionInfo>();
        section.m_Pieces = new[] { new NetPieceInfo { m_Piece = charge } };
        Track(section);
        var overhead = root.AddOrGetComponent<OverheadNetSections>();
        overhead.m_Sections = (overhead.m_Sections ?? Array.Empty<NetSectionInfo>())
            .Concat(new[] { new NetSectionInfo { m_Section = section } }).ToArray();
        report.Note($"{root.name}: base price/8m={price}; prototype='{variant.Name}', "
            + $"prototype bridge={prototypeBridge}, prototype road decks={prototypeRoads}, "
            + $"offset={offset}, selected road decks={selected}+{second}, multiplier=3.");
        return true;
    }

    private static bool Fail(ExportReport report, NetGeometryPrefab root)
    {
        report.Failed(root.name, new InvalidOperationException("Unable to resolve base-price composition."));
        return false;
    }

    private static bool ReadCost(BridgePreviewComposition composition, NetGeometryPrefab prefab,
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

    private void ZeroBaseCosts(NetGeometryPrefab net)
    {
        net.m_Sections = CopySections(net.m_Sections);
        if (net.TryGet<OverheadNetSections>(out var overhead))
            overhead.m_Sections = CopySections(overhead.m_Sections);
        if (net.TryGet<UndergroundNetSections>(out var underground))
            underground.m_Sections = CopySections(underground.m_Sections);
    }

    private NetSectionInfo[] CopySections(NetSectionInfo[] source) =>
        (source ?? Array.Empty<NetSectionInfo>()).Select(entry =>
        {
            var copy = CopyRecord(entry);
            copy.m_Section = Section(entry.m_Section);
            return copy;
        }).ToArray();

    private NetSectionPrefab Section(NetSectionPrefab source)
    {
        if (_sections.TryGetValue(source, out var known)) return known;
        var copy = CopyPrefab(source);
        _sections.Add(source, copy);
        copy.m_SubSections = (source.m_SubSections ?? Array.Empty<NetSubSectionInfo>()).Select(entry =>
        {
            var child = CopyRecord(entry);
            child.m_Section = Section(entry.m_Section);
            return child;
        }).ToArray();
        copy.m_Pieces = (source.m_Pieces ?? Array.Empty<NetPieceInfo>()).Select(entry =>
        {
            var piece = CopyRecord(entry);
            var original = entry.m_Piece;
            if (original.TryGet<PlaceableNetPiece>(out var cost) && cost.m_ConstructionCost != 0)
            {
                if (!_pieces.TryGetValue(original, out var priced))
                {
                    priced = CopyPrefab(original);
                    priced.GetComponent<PlaceableNetPiece>().m_ConstructionCost = 0;
                    _pieces.Add(original, priced);
                }
                piece.m_Piece = priced;
            }
            return piece;
        }).ToArray();
        return copy;
    }

    private T CopyPrefab<T>(T source) where T : PrefabBase
    {
        var copy = (T)ScriptableObject.CreateInstance(source.GetType());
        foreach (var field in SerializedFields.Of(source.GetType()))
            if (field.Name != nameof(PrefabBase.components) && field.Name != nameof(PrefabBase.isDirty))
                field.SetValue(copy, field.GetValue(source));
        copy.name = _prefix + "_" + Nodes.Count;
        copy.isDirty = true;
        foreach (var component in source.components)
        {
            if (component == null) continue;
            var target = copy.AddComponent(component.GetType());
            foreach (var field in SerializedFields.Of(component.GetType()))
                field.SetValue(target, field.GetValue(component));
            target.prefab = copy;
        }
        Track(copy);
        return copy;
    }

    private void Track(PrefabBase prefab) => Nodes.Add(new PrefabCloneNode(prefab, prefab, false, true, null));

    private static T CopyRecord<T>(T source) where T : new()
    {
        var copy = new T();
        foreach (var field in SerializedFields.Of(typeof(T))) field.SetValue(copy, field.GetValue(source));
        return copy;
    }
}
