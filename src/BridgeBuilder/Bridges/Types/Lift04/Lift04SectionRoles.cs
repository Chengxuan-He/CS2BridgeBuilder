using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift04SectionRoles
{
    internal static Lift04NetworkData.Role[] Resolve(NetSectionInfo[] occurrences)
    {
        var roles = new Lift04NetworkData.Role[occurrences.Length];
        for (var i = Numeric_Types_Lift04_Lift04SectionRoles_cs.Values.I32[0]; i < occurrences.Length; i++)
        {
            var item = occurrences[i];
            if (item?.m_Section == null)
                throw new InvalidOperationException("Lift04 selected network contains a missing section.");
            // Only the recorded intersection-only median receives the one-branch exception.
            var intersectionOnly = (item.m_RequireAll ?? Array.Empty<NetPieceRequirements>()).Contains(NetPieceRequirements.Intersection) || (item.m_RequireAny?.Length == Numeric_Types_Lift04_Lift04SectionRoles_cs.Values.I32[1] && item.m_RequireAny[Numeric_Types_Lift04_Lift04SectionRoles_cs.Values.I32[2]] == NetPieceRequirements.Intersection);
            roles[i] = item.m_Median && intersectionOnly ? Lift04NetworkData.Role.IntersectionMedian : Lift04NetworkData.Role.Span;
        }

        return roles;
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04SectionRoles_cs
internal static class Numeric_Types_Lift04_Lift04SectionRoles_cs
{
    static Numeric_Types_Lift04_Lift04SectionRoles_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04SectionRoles_cs");
}
