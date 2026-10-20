using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift04SectionRoles
{
    internal static Lift04NetworkData.Role[] Resolve(NetSectionInfo[] occurrences)
    {
        var roles = new Lift04NetworkData.Role[occurrences.Length];
        for (var i = 0; i < occurrences.Length; i++)
        {
            var item = occurrences[i];
            if (item?.m_Section == null)
                throw new InvalidOperationException("Lift04 selected network contains a missing section.");
            // Only the recorded intersection-only median receives the one-branch exception.
            var intersectionOnly = (item.m_RequireAll ?? Array.Empty<NetPieceRequirements>())
                .Contains(NetPieceRequirements.Intersection) ||
                (item.m_RequireAny?.Length == 1 && item.m_RequireAny[0] == NetPieceRequirements.Intersection);
            roles[i] = item.m_Median && intersectionOnly
                ? Lift04NetworkData.Role.IntersectionMedian : Lift04NetworkData.Role.Span;
        }
        return roles;
    }
}
