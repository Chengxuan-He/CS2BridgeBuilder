using BridgeBuilder.Bridges;

using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>Native pack membership, separate from the bridge prototype's DLC/mod dependencies.</summary>
internal static class BridgeAssetPack
{
    // New identity avoids changing same-CID snapshots of the historical custom-host pack.
    internal const string PrefabName = "BridgeBuilder Native Asset Pack";
    private static string? _icon;

    internal static AssetPackPrefab? Ensure(PrefabSystem prefabs)
    {
        try
        {
            if (_icon == null)
            {
                // Mods can be loaded from bytes, leaving Assembly.Location empty.
                // Embed the source, then persist the complete icon rather than a mod URL.
                using var resource = typeof(Mod).Assembly.GetManifestResourceStream("BridgeBuilderPack.svg");
                if (resource == null)
                {
                    Mod.Log.Error("Embedded BridgeBuilderPack.svg is missing from the mod assembly.");
                    return null;
                }
                using var bytes = new MemoryStream();
                resource.CopyTo(bytes);
                _icon = "data:image/svg+xml;base64," + Convert.ToBase64String(bytes.ToArray());
            }
            var icon = _icon;
            var pack = PrefabCatalog.GetAll(prefabs).OfType<AssetPackPrefab>()
                .FirstOrDefault(item => item.name == PrefabName);
            if (pack != null)
            {
                var ui = pack.AddOrGetComponent<UIObject>();
                if (!BridgePackIcon.Persist(pack, icon, out var error))
                {
                    Mod.Log.Error("Could not persist the native Bridge Builder pack icon: " + error);
                    return null;
                }
                if (ui.m_Icon != icon)
                {
                    // ImageSystem reads UIObject directly. Update the shared pack only;
                    // do not recreate or reinitialize any bridge/network entity.
                    ui.m_Icon = icon;
                }
                return pack;
            }
            pack = ScriptableObject.CreateInstance<AssetPackPrefab>();
            pack.name = PrefabName;
            pack.AddOrGetComponent<UIObject>().m_Icon = icon;
            // Give the shared pack a persistent asset ID before any bridge serializes its reference.
            new PrefabAssetWriter().Save(new[] { new PrefabCloneNode(pack, pack, false, true, null) });
            prefabs.AddOrUpdatePrefab(pack);
            if (prefabs.TryGetEntity(pack, out _)) return pack;

        }
        catch (Exception exception)
        {
            Mod.Log.Error(exception, "Could not persist the native Bridge Builder pack icon.");
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
        // A cached legacy root must never overwrite its newly migrated file before restart.
        if (BridgeSessionState.RestartRequired) return;
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
                if (bridge.isReadOnly || bridge.asset == null
                    || !owned.Any(id => BridgeAssetInfo.MatchesOwner(bridge.asset.path, id))) continue;
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
