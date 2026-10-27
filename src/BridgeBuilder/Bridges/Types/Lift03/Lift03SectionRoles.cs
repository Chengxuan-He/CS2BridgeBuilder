using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift03SectionRoles
{
    internal static Lift03NetworkData.MainRole[] ResolveMain(NetSectionInfo[] sections)
    {
        var sides = SideSections(sections);
        var result = new Lift03NetworkData.MainRole[sections.Length];
        for (var i = Numeric_Types_Lift03_Lift03SectionRoles_cs.Values.I32[0]; i < result.Length; i++)
            result[i] = sides[i] ? Lift03NetworkData.MainRole.Side : Lift03NetworkData.MainRole.Deck;
        return result;
    }

    internal static Lift03NetworkData.AuxiliaryRole[] ResolveAuxiliary(NetSectionInfo[] sections)
    {
        var sides = SideSections(sections);
        var result = new Lift03NetworkData.AuxiliaryRole[sections.Length];
        for (var i = Numeric_Types_Lift03_Lift03SectionRoles_cs.Values.I32[1]; i < result.Length; i++)
            result[i] = sections[i].m_Median ? Lift03NetworkData.AuxiliaryRole.Median : sides[i] ? Lift03NetworkData.AuxiliaryRole.Side : Lift03NetworkData.AuxiliaryRole.TrackOrShoulder;
        return result;
    }

    // Both recorded Side sections contain only native Side-layer pieces. Shoulders,
    // lanes and medians also contain Surface/Bottom/Top. Names and widths are irrelevant.
    private static bool[] SideSections(NetSectionInfo[] sections)
    {
        var cache = new Dictionary<NetSectionPrefab, (bool Any, bool OnlySide)>();
        var visiting = new HashSet<NetSectionPrefab>();
        var result = new bool[sections.Length];
        for (var i = Numeric_Types_Lift03_Lift03SectionRoles_cs.Values.I32[2]; i < sections.Length; i++)
        {
            if (sections[i] == null)
                throw new InvalidOperationException("Lift03 selected network contains a missing section occurrence.");
            var layers = Read(sections[i].m_Section);
            result[i] = layers.Any && layers.OnlySide;
        }

        return result;
        (bool Any, bool OnlySide) Read(NetSectionPrefab section)
        {
            if (section == null)
                throw new InvalidOperationException("Lift03 selected network contains a missing section.");
            if (cache.TryGetValue(section, out var known))
                return known;
            if (!visiting.Add(section))
                throw new InvalidOperationException("Lift03 selected network contains cyclic subsections.");
            var any = false;
            var onlySide = true;
            foreach (var piece in section.m_Pieces ?? Array.Empty<NetPieceInfo>())
            {
                if (piece?.m_Piece == null)
                    throw new InvalidOperationException("Lift03 selected section contains a missing piece.");
                any = true;
                onlySide &= piece.m_Piece.m_Layer == NetPieceLayer.Side;
            }

            foreach (var child in section.m_SubSections ?? Array.Empty<NetSubSectionInfo>())
            {
                if (child == null)
                    throw new InvalidOperationException("Lift03 selected section contains a missing subsection.");
                var layers = Read(child.m_Section);
                any |= layers.Any;
                onlySide &= layers.OnlySide;
            }

            visiting.Remove(section);
            cache.Add(section, (any, onlySide));
            return (any, onlySide);
        }
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03SectionRoles_cs
internal static class Numeric_Types_Lift03_Lift03SectionRoles_cs
{
    static Numeric_Types_Lift03_Lift03SectionRoles_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03SectionRoles_cs");
}
