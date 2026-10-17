using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Applies measured network branches without replacing the selected road's sections or lanes.
/// Conditions live on native NetSectionInfo entries, not one Prefab per occurrence/branch.
/// Independent OR groups retain a shared subsection only when they cannot be flattened.
/// </summary>
internal static class MovableSectionConversion
{
    internal sealed class Branch
    {
        internal readonly NetPieceRequirements[] All, Any, None;
        internal readonly NetPieceLayerMask HiddenLayers;
        internal readonly bool? HalfLength;

        internal Branch(NetPieceRequirements[] all, NetPieceRequirements[] any,
            NetPieceRequirements[] none, NetPieceLayerMask hiddenLayers, bool? halfLength)
        {
            All = (NetPieceRequirements[])all.Clone();
            Any = (NetPieceRequirements[])any.Clone();
            None = (NetPieceRequirements[])none.Clone();
            HiddenLayers = hiddenLayers;
            HalfLength = halfLength;
        }
    }

    internal readonly struct Occurrence
    {
        internal readonly int SourceIndex, BranchIndex;
        internal readonly NetSectionInfo Section;
        internal Occurrence(int source, int branch, NetSectionInfo section)
        { SourceIndex = source; BranchIndex = branch; Section = section; }
    }

    // A caller supplies rules per occurrence, not per prefab ID. The same section can appear
    // multiple times, with different lateral roles and different mechanism branches.
    internal static bool TryConvert(string owner, NetSectionInfo[] source,
        IReadOnlyList<Branch[]> rules, out NetSectionInfo[] sections,
        out PrefabBase[] created, out Occurrence[] occurrences, out string? error)
    {
        sections = Array.Empty<NetSectionInfo>();
        created = Array.Empty<PrefabBase>();
        occurrences = Array.Empty<Occurrence>();
        error = null;
        if (string.IsNullOrWhiteSpace(owner) || source == null || rules == null || rules.Count != source.Length)
        { error = "Movable section conversion requires an owner and rules for every source occurrence."; return false; }

        // Validate everything before allocating Unity objects. Missing rules are not a request
        // to silently retain a stationary section inside the moving span.
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i]?.m_Section == null || rules[i] == null || rules[i].Length == 0)
            { error = $"Section occurrence {i} has no source section or measured branch rules."; return false; }
            for (var j = 0; j < rules[i].Length; j++)
            {
                var rule = rules[i][j];
                if (rule == null || Contradicts(rule.All, rule.None))
                { error = $"Section occurrence {i}, branch {j} has invalid requirements."; return false; }
            }
        }

        var result = new List<NetSectionInfo>();
        var wrappers = new List<PrefabBase>();
        var movingObjects = new MovableSectionObjects(owner, wrappers);
        var conjunctions = new Dictionary<(NetSectionPrefab Section, string All, string Any, string None), NetSectionPrefab>();
        var trace = new List<Occurrence>();
        try
        {
            for (var i = 0; i < source.Length; i++)
            {
                var original = source[i];
                for (var j = 0; j < rules[i].Length; j++)
                {
                    var rule = rules[i][j];
                    var section = IsMovingNode(rule)
                        ? movingObjects.WithoutMovingObjects(original.m_Section) : original.m_Section;
                    var all = Combine(original.m_RequireAll, rule.All);
                    var none = Combine(original.m_RequireNone, rule.None);
                    var any = Copy(original.m_RequireAny);
                    if (any.Length == 0) any = Copy(rule.Any);
                    else if (rule.Any.Length != 0)
                    {
                        // Native requirement arrays encode one OR group. Never union two
                        // independent groups or emit overlapping entries (duplicate lanes).
                        // All audited B&P branch rules have empty Any: they use no wrapper.
                        var key = (section, Key(rule.All), Key(rule.Any), Key(rule.None));
                        if (!conjunctions.TryGetValue(key, out var wrapper))
                        {
                            wrapper = ScriptableObject.CreateInstance<NetSectionPrefab>();
                            wrappers.Add(wrapper);
                            wrapper.name = owner + " Condition Section " + conjunctions.Count;
                            wrapper.m_Pieces = Array.Empty<NetPieceInfo>();
                            wrapper.m_SubSections = new[] { new NetSubSectionInfo
                            {
                                m_Section = section, m_RequireAll = Copy(rule.All),
                                m_RequireAny = Copy(rule.Any), m_RequireNone = Copy(rule.None)
                            } };
                            conjunctions.Add(key, wrapper);
                        }
                        section = wrapper;
                        all = Copy(original.m_RequireAll);
                        none = Copy(original.m_RequireNone);
                    }
                    var entry = new NetSectionInfo
                    {
                        m_Section = section,
                        m_RequireAll = all, m_RequireAny = any, m_RequireNone = none,
                        m_HiddenLayers = original.m_HiddenLayers | rule.HiddenLayers,
                        m_Invert = original.m_Invert, m_Flip = original.m_Flip,
                        m_Median = original.m_Median, m_Offset = original.m_Offset,
                        m_HalfLength = rule.HalfLength ?? original.m_HalfLength
                    };
                    result.Add(entry);
                    trace.Add(new Occurrence(i, j, entry));
                }
            }
            sections = result.ToArray(); created = wrappers.ToArray(); occurrences = trace.ToArray();
            Mod.Log.Info($"{owner}: movable network entries={sections.Length}; unique root sections={sections.Select(entry => entry.m_Section).Distinct().Count()}; condition wrappers={conjunctions.Count}; modified network prefabs={created.Length-conjunctions.Count}.");
            return true;
        }
        catch (Exception exception)
        {
            foreach (var wrapper in wrappers)
            {
                foreach (var component in wrapper.components) UnityEngine.Object.DestroyImmediate(component);
                UnityEngine.Object.DestroyImmediate(wrapper);
            }
            error = "Movable section conversion failed: " + exception.Message;
            return false;
        }
    }

    // Only the measured fully hidden fixed-size node is replaced by the animated object.
    // Bottom-only hiding on connection edges must retain their stationary objects.
    private static bool IsMovingNode(Branch rule)
        => rule.HiddenLayers == (NetPieceLayerMask)(-1)
            && Array.IndexOf(rule.All, NetPieceRequirements.Node) >= 0
            && Array.IndexOf(rule.All, NetPieceRequirements.FixedNodeSize) >= 0;

    private static NetPieceRequirements[] Copy(NetPieceRequirements[]? values)
        => values == null ? Array.Empty<NetPieceRequirements>() : (NetPieceRequirements[])values.Clone();

    private static NetPieceRequirements[] Combine(NetPieceRequirements[]? first, NetPieceRequirements[] second)
        => (first ?? Array.Empty<NetPieceRequirements>()).Concat(second).Distinct().ToArray();

    private static string Key(NetPieceRequirements[] values)
        => string.Join(",", values.Distinct().OrderBy(value => value));

    private static bool Contradicts(NetPieceRequirements[] all, NetPieceRequirements[] none)
    {
        foreach (var required in all)
            if (Array.IndexOf(none, required) >= 0) return true;
        return false;
    }
}
