using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

// Native Sidewalk Piece 3.5 - NoTram uses BasculeBridgeBarrier01 Placeholder
// for LevelCrossing. Highway Shoulder 2 uses Traffic Sign Placeholder instead.
// Adapt the producer in the hidden mechanism node, retaining native node ownership.
internal static class MovableBarrierRules
{
    internal static void Apply(string owner, NetSectionInfo[] sections,
        ref PrefabBase[] created, ObjectPrefab barrier)
    {
        var owned = new List<PrefabBase>(created);
        var copies = new Dictionary<PrefabBase, PrefabBase>();
        foreach (var entry in sections)
            if (entry.m_RequireAll.Contains(NetPieceRequirements.FixedNodeSize)
                && entry.m_RequireAll.Contains(NetPieceRequirements.Node))
                entry.m_Section = Section(entry.m_Section);
        created = owned.ToArray();

        PrefabBase Writable(PrefabBase source)
        {
            if (owned.Contains(source)) return source;
            var copy = source.Clone(owner + " Barrier " + source.GetType().Name + " " + owned.Count);
            foreach (var component in copy.components) component.prefab = copy;
            owned.Add(copy);
            return copy;
        }

        NetSectionPrefab Section(NetSectionPrefab source)
        {
            if (copies.TryGetValue(source, out var known)) return (NetSectionPrefab)known;
            var changed = false;
            var pieces = source.m_Pieces?.Select(entry =>
            {
                var piece = Piece(entry.m_Piece);
                changed |= piece != entry.m_Piece;
                return new NetPieceInfo { m_Piece = piece, m_Offset = entry.m_Offset,
                    m_RequireAll = entry.m_RequireAll, m_RequireAny = entry.m_RequireAny,
                    m_RequireNone = entry.m_RequireNone };
            }).ToArray();
            var children = source.m_SubSections?.Select(entry =>
            {
                var section = Section(entry.m_Section);
                changed |= section != entry.m_Section;
                return new NetSubSectionInfo { m_Section = section,
                    m_RequireAll = entry.m_RequireAll, m_RequireAny = entry.m_RequireAny,
                    m_RequireNone = entry.m_RequireNone };
            }).ToArray();
            var result = changed ? (NetSectionPrefab)Writable(source) : source;
            if (changed) { result.m_Pieces = pieces; result.m_SubSections = children; }
            copies.Add(source, result);
            return result;
        }

        NetPiecePrefab Piece(NetPiecePrefab source)
        {
            if (copies.TryGetValue(source, out var known)) return (NetPiecePrefab)known;
            var result = source;
            if (source.TryGet<NetPieceObjects>(out var objects))
            {
                var rules = new List<NetPieceObjectInfo>();
                var changed = false;
                foreach (var rule in objects.m_PieceObjects)
                {
                    if (rule.m_Object.name != "Traffic Sign Placeholder"
                        || (rule.m_RequireNone ?? Array.Empty<NetPieceRequirements>()).Contains(NetPieceRequirements.LevelCrossing))
                    { rules.Add(rule); continue; }
                    // Keep ordinary signs outside crossings. Preserve all original placement,
                    // handedness and independent requirements; change only crossing identity.
                    var all = rule.m_RequireAll ?? Array.Empty<NetPieceRequirements>();
                    if (!all.Contains(NetPieceRequirements.LevelCrossing))
                        rules.Add(Copy(rule, rule.m_Object, all,
                            (rule.m_RequireNone ?? Array.Empty<NetPieceRequirements>()).Concat(new[] { NetPieceRequirements.LevelCrossing }).ToArray()));
                    rules.Add(Copy(rule, barrier, all.Concat(new[] { NetPieceRequirements.LevelCrossing }).Distinct().ToArray(), rule.m_RequireNone));
                    changed = true;
                }
                if (changed)
                {
                    result = (NetPiecePrefab)Writable(source);
                    result.GetComponent<NetPieceObjects>().m_PieceObjects = rules.ToArray();
                }
            }
            copies.Add(source, result);
            return result;
        }
    }

    private static NetPieceObjectInfo Copy(NetPieceObjectInfo rule, ObjectPrefab prefab,
        NetPieceRequirements[] all, NetPieceRequirements[]? none) => new NetPieceObjectInfo
    {
        m_Object = prefab, m_Position = rule.m_Position, m_Offset = rule.m_Offset,
        m_Rotation = rule.m_Rotation, m_RequireAll = all, m_RequireAny = rule.m_RequireAny,
        m_RequireNone = none ?? Array.Empty<NetPieceRequirements>(), m_Probability = rule.m_Probability, m_MinLength = rule.m_MinLength,
        m_CurveOffsetRange = rule.m_CurveOffsetRange, m_Spacing = rule.m_Spacing,
        m_UseCurveRotation = rule.m_UseCurveRotation, m_FlipWhenInverted = rule.m_FlipWhenInverted,
        m_EvenSpacing = rule.m_EvenSpacing, m_SpacingOverride = rule.m_SpacingOverride
    };
}

