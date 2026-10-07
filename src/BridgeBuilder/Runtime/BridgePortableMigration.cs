using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BridgeBuilder.Bridges;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Export;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Runtime;

internal static class BridgePortableMigration
{
    internal static bool Run(string owner, PrefabSystem system, string backup, out bool changed, out string error)
    {
        changed = false; error = "";
        var originals = new Dictionary<string, byte[]>();
        var newFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var instances = new Dictionary<PrefabAsset, PrefabBase>();
        var identities = new Dictionary<PrefabAsset, (string Cid, string Path, string Name, int Version)>();
        var nodes = new List<PrefabCloneNode>();
        var swap = typeof(PrefabAsset).GetMethod("SetUninitializedInstance", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            var assets = BridgeInspectionAssets.Read().Where(a => a.Owner == owner && a.Prefab != null).ToArray();
            var root = assets.Select(a => a.Prefab).OfType<NetGeometryPrefab>().FirstOrDefault(p => p.name == owner);
            if (root == null) { error = "Canonical bridge unavailable for independent migration"; return false; }
            var nativeReady = root.GetComponent<BridgeConstructionCost>() == null && root.GetComponent<Unlockable>() != null
                && root.TryGet<UIObject>(out var nativeUi) && nativeUi.name.StartsWith(BridgeAssetMetadata.Prefix, StringComparison.Ordinal);
            var icon = root.GetComponent<UIObject>()?.m_Icon;
            if (nativeReady && !string.IsNullOrEmpty(icon)) return true;
            var metadata = BridgeAssetCatalog.Find(owner) ?? new BridgeAssetInfo(owner, owner, "", null, "", "");
            if (string.IsNullOrEmpty(icon))
            {
                if (!BridgeStyleCatalog.Scanned)
                    BridgeStyleCatalog.Rebuild(system, assets.Select(a => a.Prefab!.name).ToArray());
                var variant = BridgeStyleCatalog.Find(metadata.StyleId)?.Select(NetWidth.RoadSurfaceOf(root),
                    forRoad: root is RoadPrefab, doubleDeck: root.GetComponent<AuxiliaryNets>() != null).Variant;
                icon = variant?.Donor.GetComponent<UIObject>()?.m_Icon;
                if (string.IsNullOrEmpty(icon))
                { error = "Original bridge icon unavailable; existing bridge retained"; return false; }
            }
            BridgeUnlockExpression rule = new();
            if (!nativeReady && root.GetComponent<ManualUnlockable>() != null)
            {
                if (!BridgeUnlockSnapshot.Read(root, system, system.EntityManager, out rule))
                { error = "Legacy unlock conditions unavailable"; return false; }
            }
            else if (!nativeReady && !BridgeUnlockSnapshot.Capture(root, system, system.EntityManager, out rule))
            { error = "Native unlock conditions unavailable"; return false; }
            NetGeometryPrefab CloneNet(NetGeometryPrefab source)
            {
                var copy = (NetGeometryPrefab)source.Clone(source.name);
                copy.asset = source.asset;
                instances[source.asset] = source;
                identities[source.asset] = (source.asset.id.guid.ToString(), source.asset.path, source.name, source.version);
                nodes.Add(new PrefabCloneNode(source, copy, source == root, true, source.asset));
                return copy;
            }
            var target = CloneNet(root);
            if (!nativeReady && target.TryGet<AuxiliaryNets>(out var auxiliary))
            {
                var copy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
                auxiliary.m_AuxiliaryNets = auxiliary.m_AuxiliaryNets.Select(a =>
                {
                    var entry = (AuxiliaryNetInfo)copy.Invoke(a, null)!;
                    if (a.m_Prefab is NetGeometryPrefab deck && deck.asset != null
                        && BridgeAssetInfo.MatchesOwner(deck.asset.path, owner)) entry.m_Prefab = CloneNet(deck);
                    return entry;
                }).ToArray();
                if (auxiliary.m_AuxiliaryNets.Any(a => !nodes.Any(n => ReferenceEquals(n.Target, a.m_Prefab))))
                { error = "Auxiliary network is not private to this bridge"; return false; }
            }
            var created = new List<PrefabBase>();
            // Legacy fixed-price metadata is discarded; retain the original native fee graph.
            if (!nativeReady && !BridgeNativeUnlock.Apply(target, rule, system, created, out error)) return false;
            nodes.AddRange(created.Select(p => new PrefabCloneNode(p, p, false, true, null)));
            foreach (var node in nodes)
            {
                node.Target.Remove<BridgeConstructionCost>();
                BridgeNativePresentation.Prepare(node.Target);
            }
            var pack = BridgeAssetPack.Ensure(system);
            if (pack == null) { error = "Native asset pack unavailable"; return false; }
            foreach (var net in nodes.Select(n => n.Target).OfType<NetGeometryPrefab>()) BridgeAssetPack.Assign(net, pack);
            target.AddOrGetComponent<UIObject>().name = BridgeAssetMetadata.Encode(metadata);
            target.GetComponent<UIObject>().m_Icon = icon!;
            // Back up every existing file that this transaction may replace, including CID sidecars.
            foreach (var node in nodes)
            {
                var assetPath = node.ReplacementAsset?.path ?? node.Target.asset?.path
                    ?? Path.Combine(Application.persistentDataPath, PrefabAssetWriter.RelativePathFor(node.Target.name));
                if (!BridgeAssetInfo.MatchesOwner(assetPath, owner)) { error = "Unowned migration target"; return false; }
                foreach (var path in new[] { assetPath, assetPath + ".cid" })
                {
                    if (!BridgeFileAccess.Exists(path)) { newFiles.Add(path); continue; }
                    if (originals.ContainsKey(path)) continue;
                    var bytes = File.ReadAllBytes(BridgeFileAccess.Native(path)); originals[path] = bytes;
                    var relative = path.Substring(Application.persistentDataPath.Length).TrimStart('\\', '/');
                    var saved = Path.Combine(backup, "Portable", relative);
                    Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(saved)!));
                    File.WriteAllBytes(BridgeFileAccess.Native(saved), bytes);
                }
            }
            new PrefabAssetWriter().Save(nodes);
            // Migration augments the existing asset. Root/deck identity and sidecar bytes are immutable.
            foreach (var pair in identities)
            {
                var expected = pair.Value;
                var saved = nodes.First(n => ReferenceEquals(n.ReplacementAsset, pair.Key)).Target;
                if (!ReferenceEquals(saved.asset, pair.Key) || pair.Key.id.guid.ToString() != expected.Cid
                    || pair.Key.path != expected.Path || saved.name != expected.Name || saved.version != expected.Version
                    || !BridgeSerializedReferences.TryRead(BridgeFileAccess.ReadText(expected.Path), out var document)
                    || document.Name != expected.Name
                    || (originals.TryGetValue(expected.Path + ".cid", out var cidBytes)
                        && !File.ReadAllBytes(BridgeFileAccess.Native(expected.Path + ".cid")).SequenceEqual(cidBytes)))
                { error = "Migration changed an existing bridge identity; restoring original files"; return false; }
            }
            foreach (var node in nodes)
            {
                var path = node.Target.asset?.path;
                if (path == null || !BridgeAssetInfo.MatchesOwner(path, owner)
                    || BridgeFileAccess.ReadText(path).Contains(", BridgeBuilder\""))
                { error = "Persisted bridge still contains a Bridge Builder component"; return false; }
            }
            if (!BridgeDependencyPersistence.Save(owner, nodes.Select(n => n.Target.asset.id.guid.ToString()), out _, out error)
                || !BridgeNativePresentation.Validate(owner, nodes.Select(n => n.Target), out error)
                || !BridgeNativePresentation.Save(metadata, out error)) return false;
            changed = true;
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
        finally
        {
            // Restore cached instances: this pass changes files, never publishes replacement live networks.
            foreach (var pair in instances)
                try { swap.Invoke(pair.Key, new object[] { pair.Value }); }
                catch (Exception exception) { Mod.Log.Critical("Could not restore cached bridge instance: " + exception); }
            if (!changed)
            {
                foreach (var pair in originals)
                    try { File.WriteAllBytes(BridgeFileAccess.Native(pair.Key), pair.Value); }
                    catch (Exception exception) { error += "; original restore failed: " + pair.Key + ": " + exception.Message; }
                // A failed migration must not leave orphan sections that next startup mistakes
                // for a damaged bridge. Only remove paths absent before this transaction.
                foreach (var path in newFiles)
                    try { File.Delete(BridgeFileAccess.Native(path)); }
                    catch (Exception exception) { error += "; new file rollback failed: " + path + ": " + exception.Message; }
                foreach (var directory in newFiles.Select(Path.GetDirectoryName).Distinct())
                    try
                    {
                        if (directory != null && Directory.Exists(BridgeFileAccess.Native(directory))
                            && !Directory.EnumerateFileSystemEntries(BridgeFileAccess.Native(directory)).Any())
                            Directory.Delete(BridgeFileAccess.Native(directory));
                    }
                    catch (Exception exception) { error += "; empty directory cleanup failed: " + exception.Message; }
            }
        }
    }
}
