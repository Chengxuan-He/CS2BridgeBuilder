using BridgeBuilder.Bridges;
using CS2Mods.Shared;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>Native pack membership, separate from the bridge prototype's DLC/mod dependencies.</summary>
internal static class BridgeAssetPack
{
    internal const string PrefabName = "BridgeBuilder Asset Pack";
    private const string Icon = "coui://bridgebuilderui/BridgeBuilderPack.svg";

    internal static AssetPackPrefab? Ensure(PrefabSystem prefabs)
    {
        try
        {
            var pack = PrefabCatalog.GetAll(prefabs).OfType<AssetPackPrefab>()
                .FirstOrDefault(item => item.name == PrefabName);
            if (pack != null)
            {
                var ui = pack.AddOrGetComponent<UIObject>();
                if (ui.m_Icon != Icon)
                {
                    // ImageSystem reads UIObject directly. Update the shared pack only;
                    // do not recreate or reinitialize any bridge/network entity.
                    ui.m_Icon = Icon;
                    if (pack.asset != null && !pack.isReadOnly) pack.asset.Save(false);
                }
                return pack;
            }
            pack = ScriptableObject.CreateInstance<AssetPackPrefab>();
            pack.name = PrefabName;
            pack.AddOrGetComponent<UIObject>().m_Icon = Icon;
            // Give the shared pack a persistent asset ID before any bridge serializes its reference.
            new PrefabAssetWriter().Save(new[] { new PrefabCloneNode(pack, pack, false, true, null) });
            prefabs.AddOrUpdatePrefab(pack);
            if (prefabs.TryGetEntity(pack, out _)) return pack;

        }
        catch (Exception)
        {
            // Generation diagnostics are silent; retain the external API exception boundary.
        }
        return null;
    }

    internal static void Assign(NetGeometryPrefab bridge, AssetPackPrefab pack)
    {
        // Deliberate metadata difference: generated networks belong to this pack, not the
        // selected road's pack. ContentPrerequisite and structural dependencies are untouched.
        bridge.AddOrGetComponent<AssetPackItem>().m_Packs = new[] { pack };
    }

    internal static void RefreshExisting(PrefabSystem prefabs, EntityManager entities, IEnumerable<string> roots)
    {
        var owned = new HashSet<string>(roots, StringComparer.Ordinal);
        if (owned.Count == 0) return;
        // Only exact persisted ownership records and the generator's exact carried-deck names.
        // Never classify ownership from a UUID prefix or walk into shared prototype references.
        foreach (var root in owned.ToArray())
        {
            owned.Add(BridgeNaming.CarriedDeckName(root, false));
            owned.Add(BridgeNaming.CarriedDeckName(root, true));
        }
        var bridges = PrefabCatalog.GetAll(prefabs).OfType<NetGeometryPrefab>()
            .Where(item => owned.Contains(item.name)).ToArray();
        if (bridges.Length == 0) return;
        var pack = Ensure(prefabs);
        if (pack == null || !prefabs.TryGetEntity(pack, out var packEntity)) return;
        foreach (var bridge in bridges)
        {
            try
            {
                if (bridge.isReadOnly || bridge.asset == null) continue;
                var item = bridge.AddOrGetComponent<AssetPackItem>();
                var previous = item.m_Packs;
                if (previous == null || previous.Length != 1 || previous[0] != pack)
                {
                    item.m_Packs = new[] { pack };
                    try { bridge.asset.Save(false); }
                    catch (Exception)
                    {
                        item.m_Packs = previous;

                        continue;
                    }
                }
                if (!prefabs.TryGetEntity(bridge, out var entity) || !entities.Exists(entity)) continue;
                // AssetPackItem.LateInitialize writes only this buffer. Do not reinitialize
                // live network prefabs, replace their entities, or disturb placed bridges.
                var buffer = entities.HasBuffer<AssetPackElement>(entity)
                    ? entities.GetBuffer<AssetPackElement>(entity)
                    : entities.AddBuffer<AssetPackElement>(entity);
                if (buffer.Length == 1 && buffer[0].m_Pack == packEntity) continue;
                buffer.Clear();
                buffer.Add(new AssetPackElement { m_Pack = packEntity });
            }
            catch (Exception)
            {
                // Generation diagnostics are silent; retain the external API exception boundary.
            }
        }
    }
}
