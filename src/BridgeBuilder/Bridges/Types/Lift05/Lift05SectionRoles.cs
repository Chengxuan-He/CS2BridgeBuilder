using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift05SectionRoles
{
    internal static Lift05NetworkData.Role[] Resolve(NetSectionInfo[] occurrences)
    {
        var roles = new Lift05NetworkData.Role[occurrences.Length];
        for (var i = 0; i < occurrences.Length; i++)
        {
            var item = occurrences[i];
            if (item?.m_Section == null)
                throw new InvalidOperationException("Lift05 selected network contains a missing section.");
            // The native exception is the intersection-only median occurrence.
            // Ordinary medians, shoulders and side sections retain the three span branches.
            var intersectionOnly = (item.m_RequireAll ?? Array.Empty<NetPieceRequirements>())
                .Contains(NetPieceRequirements.Intersection) ||
                (item.m_RequireAny?.Length == 1 && item.m_RequireAny[0] == NetPieceRequirements.Intersection);
            roles[i] = item.m_Median && intersectionOnly
                ? Lift05NetworkData.Role.IntersectionMedian : Lift05NetworkData.Role.Span;
        }
        return roles;
    }
}
