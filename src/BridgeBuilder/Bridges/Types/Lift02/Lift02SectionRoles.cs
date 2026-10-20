using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift02SectionRoles
{
    internal static Lift02NetworkData.Role[] Resolve(NetSectionInfo[] occurrences)
    {
        var roles = new Lift02NetworkData.Role[occurrences.Length];
        for (var i = 0; i < occurrences.Length; i++)
        {
            var item = occurrences[i];
            if (item?.m_Section == null)
                throw new InvalidOperationException("Lift02 selected network contains a missing section.");
            // The native exception is the intersection-only median occurrence.
            // Ordinary medians, shoulders and side sections retain the three span branches.
            var intersectionOnly = (item.m_RequireAll ?? Array.Empty<NetPieceRequirements>())
                .Contains(NetPieceRequirements.Intersection) ||
                (item.m_RequireAny?.Length == 1 && item.m_RequireAny[0] == NetPieceRequirements.Intersection);
            roles[i] = item.m_Median && intersectionOnly
                ? Lift02NetworkData.Role.IntersectionMedian : Lift02NetworkData.Role.Span;
        }
        return roles;
    }
}
