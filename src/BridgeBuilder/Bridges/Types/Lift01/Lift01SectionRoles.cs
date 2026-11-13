using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
// Native Train/Highway/Alley Side sections contain only Side-layer pieces.
// Shoulders contain Surface (plus Bottom/Top) pieces. Resolve selected-network
// semantics through its native layer fields, including subsection wrappers.
internal static class Lift01SectionRoles
{
    internal static Lift01NetworkData.Role[] Resolve(NetSectionInfo[] occurrences, BridgeRoadContext context)
    {
        var roles = new Lift01NetworkData.Role[occurrences.Length];
        var cache = new Dictionary<NetSectionPrefab, (bool Any, bool OnlySide)>();
        var visiting = new HashSet<NetSectionPrefab>();
        for (var i = Numeric_Types_Lift01_Lift01SectionRoles_cs.Values.I32[0]; i < occurrences.Length; i++)
        {
            var occurrence = occurrences[i];
            // Inactive roots keep their ordinal and native conditions; do not inspect their alternatives.
            if (!context.AllowsLevel(occurrence.m_RequireAll, occurrence.m_RequireAny, occurrence.m_RequireNone))
                continue;
            var layers = Read(occurrence.m_Section);
            roles[i] = layers.Any && layers.OnlySide ? Lift01NetworkData.Role.Side : Lift01NetworkData.Role.Deck;
        }

        return roles;
        (bool Any, bool OnlySide) Read(NetSectionPrefab section)
        {
            if (cache.TryGetValue(section, out var known))
                return known;
            var any = false;
            var side = true;
            foreach (var entry in section.m_Pieces ?? Array.Empty<NetPieceInfo>())
            {
                if (!context.AllowsLevel(entry.m_RequireAll, entry.m_RequireAny, entry.m_RequireNone))
                    continue;
                any = true;
                side &= entry.m_Piece.m_Layer == NetPieceLayer.Side;
            }

            foreach (var child in section.m_SubSections ?? Array.Empty<NetSubSectionInfo>())
            {
                if (!context.AllowsLevel(child.m_RequireAll, child.m_RequireAny, child.m_RequireNone))
                    continue;
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

// Embedded numerical arrays: Types_Lift01_Lift01SectionRoles_cs
internal static class Numeric_Types_Lift01_Lift01SectionRoles_cs
{
    static Numeric_Types_Lift01_Lift01SectionRoles_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01SectionRoles_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01SectionRoles_cs"));
}
