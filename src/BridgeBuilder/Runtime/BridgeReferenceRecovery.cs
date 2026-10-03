using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BridgeBuilder.Runtime;

/// <summary>Repair exact serialized references at the title boundary, never by a similar asset name.</summary>
internal static class BridgeReferenceRecovery
{
    internal enum Result { Healthy, Deferred, Broken }
    // A deferred registration must not be mistaken for a deleted asset when loading a save.
    internal static readonly HashSet<string> DeferredOwners = new(StringComparer.Ordinal);

    internal static bool Supports(PrefabBase prefab) => prefab is NetGeometryPrefab
        || prefab is NetSectionPrefab || prefab is NetPiecePrefab || prefab is NetLanePrefab;

    internal static Result Repair(PrefabBase root, PrefabSystem prefabs, out string reason)
    {
        var visited = new HashSet<PrefabBase>(
            CS2Mods.Shared.Infrastructure.ReferenceEqualityComparer<PrefabBase>.Instance);
        return Visit(root, visited, prefabs, out reason);
    }

    private static Result Visit(PrefabBase prefab, HashSet<PrefabBase> visited,
        PrefabSystem prefabs, out string reason)
    {
        reason = string.Empty;
        if (!visited.Add(prefab)) return Result.Healthy;
        if (!Supports(prefab)) return Result.Healthy;
        var owned = !prefab.isBuiltin && !prefab.isReadOnly && BridgeLoadFailures.TryOwner(prefab.name, out _);
        var mutable = owned && !prefabs.TryGetEntity(prefab, out _);
        BridgeSerializedReferences? source = null;
        var read = false;
        var result = Result.Healthy;
        var reasonBuffer = string.Empty;
        var edits = new List<Action>();
        foreach (var slot in Slots(prefab))
        {
            var field = slot.Owner.GetType().GetField(slot.Field);
            if (field == null) { Merge(Result.Deferred, "Unrecognized native field: " + slot.Field); continue; }
            if (!slot.Array)
            {
                if (field.GetValue(slot.Owner) is PrefabBase single && single != null)
                { Merge(Visit(single, visited, prefabs, out var childReason), childReason); continue; }
                var singleResult = Resolve(null, slot, -1, field.FieldType, out var replacement);
                if (replacement != null)
                {
                    edits.Add(() => field.SetValue(slot.Owner, replacement));
                    Merge(Visit(replacement, visited, prefabs, out var childReason), childReason);
                }
                // A required scalar cannot be made safe by leaving null. Retire the affected bridge.
                Merge(singleResult, slot.Field + " missing required reference");
                continue;
            }
            var array = field.GetValue(slot.Owner) as Array;
            if (array == null)
            {
                if (!slot.Nullable) Merge(Resolve(null, slot, -1, null, out _), slot.Field + " null array");
                continue;
            }
            var kept = new List<object>();
            var changed = false;
            for (var i = 0; i < array.Length; i++)
            {
                var item = array.GetValue(i);
                var reference = item?.GetType().GetField(slot.Member);
                var target = reference?.GetValue(item) as PrefabBase;
                if (target != null)
                {
                    Merge(Visit(target, visited, prefabs, out var childReason), childReason);
                    kept.Add(item!); continue;
                }
                var status = Resolve(item, slot, i, reference?.FieldType, out var restored);
                if (restored != null && reference != null)
                {
                    // Clone descriptors: their arrays/entries can still be shared with another prefab.
                    var copy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(item, null)!;
                    reference.SetValue(copy, restored);
                    kept.Add(copy); changed = true;
                    Merge(Visit(restored, visited, prefabs, out var childReason), childReason);
                }
                else if (status == Result.Broken && slot.Prunable && mutable)
                {
                    changed = true;
                    var message = $"Removed unrecoverable null entry: {prefab.name}/{slot.Field}[{i}]. Original file retained.";
                    edits.Add(() => Mod.Log.Warn(message));
                }
                else
                {
                    kept.Add(item!);
                    Merge(status, slot.Field + "[" + i + "] could not be restored");
                }
            }
            if (changed)
            {
                var copy = Array.CreateInstance(field.FieldType.GetElementType()!, kept.Count);
                for (var i = 0; i < kept.Count; i++) copy.SetValue(kept[i], i);
                edits.Add(() => field.SetValue(slot.Owner, copy));
            }
        }
        // Do not shift array indices while another unresolved entry still needs its original
        // serialized position. Apply this prefab's changes as a unit, not piecemeal across retries.
        if (result == Result.Healthy) foreach (var edit in edits) edit();
        reason = reasonBuffer;
        return result;

        void Merge(Result status, string detail)
        {
            // An independently proven required null cannot be repaired by loading another asset.
            if (status == Result.Broken || status == Result.Deferred && result == Result.Healthy)
            { result = status; reasonBuffer = detail; }
        }

        Result Resolve(object? item, Slot slot, int index, Type? expected, out PrefabBase? restored)
        {
            restored = null;
            if (!owned) return Result.Deferred; // never inspect another mod's graph as owned damage
            if (!read)
            {
                read = true;
                var path = prefab.asset?.path;
                if (!string.IsNullOrEmpty(path) && IsOwnedPath(path!, prefab.name)
                    && BridgeSerializedReferences.TryRead(BridgeFileAccess.ReadText(path!), out var parsed)
                    && parsed.Name == prefab.name) source = parsed;
            }
            if (source == null) return Result.Deferred; // unreadable/unsupported serialization is not damage
            var component = ReferenceEquals(slot.Owner, prefab) ? null : slot.Owner.GetType().FullName;
            if (!source.Reference(component, slot.Field, index, slot.Member, out var identity)) return Result.Deferred;
            if (identity.Length == 0) return Result.Broken;
            if (!mutable) return Result.Deferred; // evidence may be read, registered graphs never mutated
            var status = Lookup(identity, out var candidate);
            if (status != Result.Healthy) return status;
            if (candidate == null || expected == null || !expected.IsInstanceOfType(candidate)) return Result.Deferred;
            restored = candidate;
            Mod.Log.Info($"Restored bridge reference by serialized identity: {prefab.name}/{slot.Field}[{index}] -> {identity}");
            return Result.Healthy;
        }
    }

    // Exact file identity and scope, not a substring match against arbitrary asset paths.
    private static bool IsOwnedPath(string path, string name)
    {
        var imported = Path.GetFullPath(Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData"));
        var full = Path.GetFullPath(BridgeFileAccess.Logical(path));
        var directory = Path.GetDirectoryName(full)!;
        return Path.GetDirectoryName(directory) == imported && Path.GetFileName(directory) == name
            && Path.GetFileName(full) == name + ".Prefab"
            && (BridgeFileAccess.Attributes(full) & FileAttributes.ReparsePoint) == 0
            && (BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) == 0;
    }

    private static Result Lookup(string identity, out PrefabBase? prefab)
    {
        prefab = null;
        if (identity.StartsWith("CID:", StringComparison.Ordinal))
        {
            if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(identity.Substring(4)), out PrefabAsset asset))
                return Result.Deferred; // mod/DLC may be unavailable; absence is not proof of permanent loss
            if (asset.database == AssetDatabase.user && !BridgeFileAccess.Exists(asset.path)) return Result.Broken;
            prefab = asset.GetInstance<PrefabBase>();
        }
        else if (identity.StartsWith("UnityGUID:", StringComparison.Ordinal))
        {
            if (AssetDatabase.global.resources.prefabsMap.TryGetObject(identity.Substring(10), out var value))
                prefab = value as PrefabBase;
        }
        return prefab != null ? Result.Healthy : Result.Deferred;
    }

    private sealed class Slot
    {
        internal object Owner = null!;
        internal string Field = "", Member = "";
        internal bool Array = true, Nullable, Prunable;
    }
    private static IEnumerable<Slot> Slots(PrefabBase p)
    {
        Slot A(object owner, string field, string member, bool nullable, bool prune = false)
            => new() { Owner = owner, Field = field, Member = member, Nullable = nullable, Prunable = prune };
        if (p is NetGeometryPrefab)
        {
            yield return A(p, "m_Sections", "m_Section", false);
            if (p.TryGet<OverheadNetSections>(out var o) && o.active) yield return A(o, "m_Sections", "m_Section", false);
            if (p.TryGet<UndergroundNetSections>(out var u) && u.active) yield return A(u, "m_Sections", "m_Section", false);
        }
        if (p is NetPrefab)
        {
            if (p.TryGet<AuxiliaryNets>(out var a) && a.active) yield return A(a, "m_AuxiliaryNets", "m_Prefab", false);
            if (p.TryGet<NetSubObjects>(out var o) && o.active) yield return A(o, "m_SubObjects", "m_Object", false);
        }
        if (p is NetSectionPrefab)
        {
            yield return A(p, "m_SubSections", "m_Section", true);
            yield return A(p, "m_Pieces", "m_Piece", true);
        }
        if (p is NetPiecePrefab)
        {
            if (p.TryGet<NetPieceLanes>(out var l) && l.active) yield return A(l, "m_Lanes", "m_Lane", true);
            if (p.TryGet<NetPieceObjects>(out var o) && o.active) yield return A(o, "m_PieceObjects", "m_Object", false);
            if (p.TryGet<NetPieceCrosswalk>(out var c) && c.active)
                yield return new Slot { Owner = c, Field = "m_Lane", Array = false };
        }
        if (p is NetLanePrefab)
        {
            if (p.TryGet<SecondaryLane>(out var s) && s.active)
            {
                yield return A(s, "m_LeftLanes", "m_Lane", true, true);
                yield return A(s, "m_RightLanes", "m_Lane", true, true);
                yield return A(s, "m_CrossingLanes", "m_Lane", true, true);
            }
            if (p.TryGet<AuxiliaryLanes>(out var a) && a.active) yield return A(a, "m_AuxiliaryLanes", "m_Lane", false, true);
        }
        if (p is NetLaneGeometryPrefab) yield return A(p, "m_Meshes", "m_Mesh", false, true);
    }
}
