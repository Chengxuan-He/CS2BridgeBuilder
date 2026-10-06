using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>Prove missing runtime slots against their original CID; never edit or reload live objects.</summary>
internal static class BridgeLoadedCidRecovery
{
    internal enum Result { Recoverable, Broken, Incomplete }
    internal static Result Inspect(PrefabBase root, out HashSet<string> copies, out string reason, LoadedIndex? lookup = null)
    {
        lookup ??= new LoadedIndex();
        var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<PrefabBase>(CS2Mods.Shared.Infrastructure.ReferenceEqualityComparer<PrefabBase>.Instance);
        var outcome = Visit(root, out reason);
        copies = needed;
        return outcome;
        Result Visit(PrefabBase prefab, out string detail)
        {
            detail = "";
            if (!visited.Add(prefab)) return Result.Recoverable;
            BridgeSerializedReferences? source = null;
            foreach (var slot in Slots(prefab))
            {
                var field = slot.Owner.GetType().GetField(slot.Field);
                if (field == null) { detail = "Unknown required field: " + slot.Field; return Result.Incomplete; }
                if (!slot.Array)
                {
                    var result = Target(field.GetValue(slot.Owner) as PrefabBase, -1, field.FieldType, out detail);
                    if (result != Result.Recoverable) return result;
                    continue;
                }
                var array = field.GetValue(slot.Owner) as Array;
                if (array == null)
                {
                    if (slot.Nullable) continue;
                    detail = prefab.name + "/" + slot.Field + ": null array has no recoverable CID";
                    return Result.Broken;
                }
                if (prefab is NetGeometryPrefab && ReferenceEquals(slot.Owner, prefab) && slot.Field == "m_Sections" && array.Length == 0)
                { detail = prefab.name + ": empty required sections"; return Result.Broken; }
                for (var index = 0; index < array.Length; index++)
                {
                    var item = array.GetValue(index);
                    var member = item?.GetType().GetField(slot.Member);
                    if (member == null) { detail = "Missing required descriptor: " + slot.Field; return Result.Broken; }
                    var result = Target(member.GetValue(item) as PrefabBase, index, member.FieldType, out detail);
                    if (result != Result.Recoverable) return result;
                }
                Result Target(PrefabBase? child, int index, Type expected, out string why)
                {
                    why = "";
                    if (child != null) return Visit(child, out why);
                    if (source == null)
                    {
                        if (prefab.asset == null) { why = "Serialized source unavailable: " + prefab.name; return Result.Incomplete; }
                        using var stream = prefab.asset.GetReadStream();
                        using var reader = new StreamReader(stream);
                        if (!BridgeSerializedReferences.TryRead(reader.ReadToEnd(), out source))
                        { why = "Unsupported serialized source: " + prefab.name; return Result.Incomplete; }
                    }
                    var component = ReferenceEquals(slot.Owner, prefab) ? null : slot.Owner.GetType().FullName;
                    if (!source.Reference(component, slot.Field, index, slot.Member, out var identity))
                    { why = "Original slot unavailable: " + prefab.name + "/" + slot.Field; return Result.Incomplete; }
                    if (!Regex.IsMatch(identity, "^CID:[a-fA-F0-9]{32}$"))
                    { why = "Null reference has no recoverable CID: " + prefab.name + "/" + slot.Field; return Result.Broken; }
                    var cid = identity.Substring(4);
                    // Same-CID snapshots may coexist; accept an already-loaded instance, never Load().
                    var loaded = lookup.Find(cid, expected);
                    if (loaded == null) { why = "CID asset not loaded: " + cid; return Result.Broken; }
                    needed.Add(cid);
                    return Visit(loaded, out why);
                }
            }
            return Result.Recoverable;
        }
    }

    // Per-inspection lazy index: O(all assets + missing references), not a full scan per null.
    internal sealed class LoadedIndex
    {
        private Dictionary<string, List<PrefabBase>>? _loaded;
        internal PrefabBase? Find(string cid, Type expected)
        {
            if (_loaded == null)
            {
                _loaded = new(StringComparer.OrdinalIgnoreCase);
                foreach (var asset in AssetDatabase.global.GetAssets<PrefabAsset>())
                {
                    var instance = asset.GetInstance<PrefabBase>();
                    if (instance == null) continue;
                    var id = asset.id.guid.ToString();
                    if (!_loaded.TryGetValue(id, out var instances)) _loaded[id] = instances = new();
                    instances.Add(instance);
                }
            }
            return _loaded.TryGetValue(cid, out var matches) ? matches.FirstOrDefault(expected.IsInstanceOfType) : null;
        }
    }

    private sealed class Slot
    {
        internal object Owner = null!;
        internal string Field = "", Member = "";
        internal bool Array = true, Nullable;
    }
    private static IEnumerable<Slot> Slots(PrefabBase p)
    {
        Slot A(object owner, string field, string member, bool nullable)
            => new() { Owner = owner, Field = field, Member = member, Nullable = nullable };
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
                yield return A(s, "m_LeftLanes", "m_Lane", true);
                yield return A(s, "m_RightLanes", "m_Lane", true);
                yield return A(s, "m_CrossingLanes", "m_Lane", true);
            }
            if (p.TryGet<AuxiliaryLanes>(out var a) && a.active) yield return A(a, "m_AuxiliaryLanes", "m_Lane", false);
        }
        if (p is NetLaneGeometryPrefab) yield return A(p, "m_Meshes", "m_Mesh", false);
    }
}
