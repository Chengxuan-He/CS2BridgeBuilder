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
            var metadata = BridgeAssetCatalog.Find(owner) ?? new BridgeAssetInfo(owner, owner, "", null, "", "");
            // Dependency snapshots are byte-identical external assets, not conversion targets.
            var privateAssets = assets.Where(a => !BridgeFileAccess.Logical(a.Path)
                .Split('\\', '/').Contains(owner + "_Dependencies")).ToArray();
            var networks = privateAssets.Select(a => a.Prefab).OfType<NetGeometryPrefab>().Distinct().ToArray();
            var pack = BridgeAssetPack.Ensure(system);
            if (pack == null) { error = "Native asset pack unavailable"; return false; }
            foreach (var source in networks.OrderBy(p => p == root ? 0 : 1))
            {
                var ui = source.GetComponent<UIObject>();
                var gate = source.GetComponent<Unlockable>();
                var needsUnlock = gate == null || !gate.m_IgnoreDependencies;
                var needsIcon = source == root && (string.IsNullOrWhiteSpace(ui?.m_Icon)
                    || ui!.m_Icon.IndexOf("coui://bridgebuilder", StringComparison.OrdinalIgnoreCase) >= 0);
                var needsMetadata = source == root && ui?.name != BridgeAssetMetadata.Encode(metadata);
                var packs = source.GetComponent<AssetPackItem>()?.m_Packs;
                var needsPack = packs == null || packs.Length != 1 || packs[0] != pack;
                var needsCleanup = source.components.Any(c => c is BridgeConstructionCost || c is ManualUnlockable
                    || (c != null && PrefabGraphCloner.ShouldStripComponent(c, false)));
                if (!needsUnlock && !needsIcon && !needsMetadata && !needsPack && !needsCleanup) continue;
                if (source.asset == null || !BridgeAssetInfo.MatchesOwner(source.asset.path, owner))
                { error = "Unowned migration target: " + source.name; return false; }
                var target = (NetGeometryPrefab)source.Clone(source.name);
                target.asset = source.asset;
                instances[source.asset] = source;
                identities[source.asset] = (source.asset.id.guid.ToString(), source.asset.path, source.name, source.version);
                nodes.Add(new PrefabCloneNode(source, target, source == root, true, source.asset));
                if (needsUnlock)
                {
                    // Each network has its own missing fields. Never rebuild a valid sibling's rule.
                    BridgeUnlockExpression rule;
                    var read = source.GetComponent<ManualUnlockable>() != null
                        ? BridgeUnlockSnapshot.Read(source, system, system.EntityManager, out rule)
                        : BridgeUnlockSnapshot.Capture(source, system, system.EntityManager, out rule);
                    if (!read) { error = "Unlock conditions unavailable: " + source.name; return false; }
                    var created = new List<PrefabBase>();
                    if (!BridgeNativeUnlock.Apply(target, rule, system, created, out error, includeAuxiliary: false)) return false;
                    nodes.AddRange(created.Select(p => new PrefabCloneNode(p, p, false, true, null)));
                }
                else target.Remove<ManualUnlockable>();
                if (needsIcon)
                {
                    if (!BridgeStyleCatalog.Scanned)
                        BridgeStyleCatalog.Rebuild(system, assets.Select(a => a.Prefab!.name).ToArray());
                    var variant = BridgeStyleCatalog.Find(metadata.StyleId)?.Select(NetWidth.RoadSurfaceOf(root),
                        forRoad: root is RoadPrefab, doubleDeck: root.GetComponent<AuxiliaryNets>() != null).Variant;
                    var icon = variant?.Donor.GetComponent<UIObject>()?.m_Icon;
                    if (string.IsNullOrWhiteSpace(icon) || icon.IndexOf("coui://bridgebuilder", StringComparison.OrdinalIgnoreCase) >= 0)
                    { error = "Original bridge icon unavailable; existing bridge retained"; return false; }
                    target.AddOrGetComponent<UIObject>().m_Icon = icon;
                }
                if (needsMetadata) target.AddOrGetComponent<UIObject>().name = BridgeAssetMetadata.Encode(metadata);
                if (needsPack) BridgeAssetPack.Assign(target, pack);
            }
            // Private non-network components can also come from a different historical version.
            foreach (var source in privateAssets.Select(a => a.Prefab!).Distinct().Where(p => p is not NetGeometryPrefab))
            {
                if (!source.components.Any(c => c is BridgeConstructionCost
                    || (c != null && PrefabGraphCloner.ShouldStripComponent(c, false)))) continue;
                if (source.asset == null || !BridgeAssetInfo.MatchesOwner(source.asset.path, owner))
                { error = "Unowned migration target: " + source.name; return false; }
                var target = source.Clone(source.name);
                target.asset = source.asset;
                instances[source.asset] = source;
                identities[source.asset] = (source.asset.id.guid.ToString(), source.asset.path, source.name, source.version);
                nodes.Add(new PrefabCloneNode(source, target, false, true, source.asset));
            }
            foreach (var node in nodes)
            {
                node.Target.Remove<BridgeConstructionCost>();
                BridgeNativePresentation.Prepare(node.Target);
            }
            // Complete every independent check before taking a no-Prefab-write path.
            if (nodes.Count == 0)
            {
                if (!BridgeNativePresentation.Validate(owner, assets.Select(a => a.Prefab!), out error)) return false;
                return BridgeNativePresentation.EnsureDescriptions(metadata, out changed, out error);
            }
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
                    var relative = BridgeFileAccess.Logical(path).Substring(BridgeFileAccess.Logical(Application.persistentDataPath).Length).TrimStart('\\', '/');
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
                || !BridgeNativePresentation.Validate(owner, assets.Select(a => a.Prefab!).Concat(nodes.Select(n => n.Target)), out error)
                || !BridgeNativePresentation.EnsureDescriptions(metadata, out _, out error)) return false;
            changed = true;
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
        finally
        {
            // Restore cached instances: this pass changes files, never publishes replacement live networks.
            foreach (var pair in instances)
                try { swap.Invoke(pair.Key, new object[] { pair.Value }); }
                catch (Exception exception) { Mod.Log.Error("Could not restore cached bridge instance: " + exception); }
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
