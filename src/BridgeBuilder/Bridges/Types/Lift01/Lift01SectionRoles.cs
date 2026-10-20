using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

// Native Train/Highway/Alley Side sections contain only Side-layer pieces.
// Shoulders contain Surface (plus Bottom/Top) pieces. Resolve selected-network
// semantics through its native layer fields, including subsection wrappers.
internal static class Lift01SectionRoles
{
    internal static Lift01NetworkData.Role[] Resolve(NetSectionInfo[] occurrences)
    {
        var roles = new Lift01NetworkData.Role[occurrences.Length];
        var cache = new Dictionary<NetSectionPrefab, (bool Any, bool OnlySide)>();
        var visiting = new HashSet<NetSectionPrefab>();
        for (var i = 0; i < occurrences.Length; i++)
        {
            var layers = Read(occurrences[i].m_Section);
            roles[i] = layers.Any && layers.OnlySide
                ? Lift01NetworkData.Role.Side : Lift01NetworkData.Role.Deck;
        }
        return roles;

        (bool Any, bool OnlySide) Read(NetSectionPrefab section)
        {
            if (section == null)
                throw new InvalidOperationException("Lift01 selected network contains a missing section.");
            if (cache.TryGetValue(section, out var known)) return known;
            if (!visiting.Add(section))
                throw new InvalidOperationException("Lift01 selected network contains cyclic subsections.");
            var any = false;
            var side = true;
            foreach (var entry in section.m_Pieces ?? Array.Empty<NetPieceInfo>())
            {
                if (entry?.m_Piece == null)
                    throw new InvalidOperationException("Lift01 selected section contains a missing piece.");
                any = true;
                side &= entry.m_Piece.m_Layer == NetPieceLayer.Side;
            }
            foreach (var child in section.m_SubSections ?? Array.Empty<NetSubSectionInfo>())
            {
                var layers = Read(child.m_Section);
                any |= layers.Any;
                side &= layers.OnlySide;
            }
            visiting.Remove(section);
            cache.Add(section, (any, side));
            return (any, side);
        }
    }
}
