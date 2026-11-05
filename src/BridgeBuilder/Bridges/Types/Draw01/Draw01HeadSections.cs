using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
// A style-owned traversal for the native, stationary connection heads. The
// adapter must prove source face identity and native projection before returning
// a replacement piece. It owns that replacement's render/resource graph.
internal static class Draw01HeadSections
{
    internal static NetSectionInfo[] Apply(string owner, NetSectionInfo[] source, Func<string, NetSectionInfo, NetPieceInfo, NetPiecePrefab> adapt, List<PrefabBase> created)
    {
        var result = (NetSectionInfo[])source.Clone();
        for (int index = Numeric_Types_Draw01_Draw01HeadSections_cs.Values.I32[0]; index < source.Length; index++)
        {
            var root = source[index];
            // The fully hidden central node belongs to the mechanism. Ordinary
            // connecting roads remain selected-road originals.
            if (Array.IndexOf(root.m_RequireAll ?? Array.Empty<NetPieceRequirements>(), NetPieceRequirements.FixedNodeSize) < Numeric_Types_Draw01_Draw01HeadSections_cs.Values.I32[1] || Array.IndexOf(root.m_RequireNone ?? Array.Empty<NetPieceRequirements>(), NetPieceRequirements.Node) < Numeric_Types_Draw01_Draw01HeadSections_cs.Values.I32[2])
                continue;
            var visiting = new HashSet<NetSectionPrefab>();
            NetSectionPrefab Visit(NetSectionPrefab section, string path)
            {
                if (section == null || !visiting.Add(section))
                    throw new InvalidOperationException("Draw01 head has a missing or cyclic native section.");
                try
                {
                    bool changed = false;
                    var pieces = section.m_Pieces == null ? null : new NetPieceInfo[section.m_Pieces.Length];
                    for (int p = Numeric_Types_Draw01_Draw01HeadSections_cs.Values.I32[3]; pieces != null && p < pieces.Length; p++)
                    {
                        var original = section.m_Pieces![p];
                        if (original?.m_Piece == null)
                            throw new InvalidOperationException("Draw01 head has a missing native piece.");
                        var piece = adapt(path + "/pieces/" + p, root, original) ?? throw new InvalidOperationException("Draw01 head adapter returned no piece.");
                        changed |= piece != original.m_Piece;
                        pieces[p] = new NetPieceInfo
                        {
                            m_Piece = piece,
                            m_Offset = original.m_Offset,
                            m_RequireAll = original.m_RequireAll,
                            m_RequireAny = original.m_RequireAny,
                            m_RequireNone = original.m_RequireNone
                        };
                    }

                    var children = section.m_SubSections == null ? null : new NetSubSectionInfo[section.m_SubSections.Length];
                    for (int child = Numeric_Types_Draw01_Draw01HeadSections_cs.Values.I32[4]; children != null && child < children.Length; child++)
                    {
                        var original = section.m_SubSections![child];
                        if (original == null)
                            throw new InvalidOperationException("Draw01 head has a missing native subsection.");
                        var replacement = Visit(original.m_Section, path + "/subsections/" + child);
                        changed |= replacement != original.m_Section;
                        children[child] = new NetSubSectionInfo
                        {
                            m_Section = replacement,
                            m_RequireAll = original.m_RequireAll,
                            m_RequireAny = original.m_RequireAny,
                            m_RequireNone = original.m_RequireNone
                        };
                    }

                    if (!changed)
                        return section;
                    var clone = (NetSectionPrefab)section.Clone(owner + " Head Section " + created.Count);
                    created.Add(clone);
                    foreach (var component in clone.components)
                        component.prefab = clone;
                    clone.m_Pieces = pieces;
                    clone.m_SubSections = children;
                    clone.isDirty = true;
                    return clone;
                }
                finally
                {
                    visiting.Remove(section);
                }
            }

            var replacement = Visit(root.m_Section, "sections/" + index);
            if (replacement == root.m_Section)
                continue;
            result[index] = new NetSectionInfo
            {
                m_Section = replacement,
                m_RequireAll = root.m_RequireAll,
                m_RequireAny = root.m_RequireAny,
                m_RequireNone = root.m_RequireNone,
                m_HiddenLayers = root.m_HiddenLayers,
                m_Invert = root.m_Invert,
                m_Flip = root.m_Flip,
                m_Median = root.m_Median,
                m_Offset = root.m_Offset,
                m_HalfLength = root.m_HalfLength
            };
        }

        return result;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01HeadSections_cs
internal static class Numeric_Types_Draw01_Draw01HeadSections_cs
{
    static Numeric_Types_Draw01_Draw01HeadSections_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01HeadSections_cs");
}
