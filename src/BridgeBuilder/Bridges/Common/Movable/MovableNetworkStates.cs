using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

// Native composition appearance belongs to the selected road, not its mechanism donor.
internal static class MovableNetworkStates
{
    private static NetPieceRequirements[] Motion(NetPieceRequirements[]? states) =>
        (states ?? Array.Empty<NetPieceRequirements>()).Where(state => state != NetPieceRequirements.Pavement
            && state != NetPieceRequirements.Gravel && state != NetPieceRequirements.Tiles).ToArray();
    private static NetPieceRequirements[] Copy(NetPieceRequirements[]? values) =>
        values == null ? Array.Empty<NetPieceRequirements>() : (NetPieceRequirements[])values.Clone();

    internal static NetEdgeStateInfo[] Append(NetEdgeStateInfo[]? selected, NetEdgeStateInfo[] mechanism)
    {
        var result = new List<NetEdgeStateInfo>(selected ?? Array.Empty<NetEdgeStateInfo>());
        foreach (var rule in mechanism)
        {
            var states = Motion(rule.m_SetState);
            if (states.Length == 0) continue;
            result.Add(new NetEdgeStateInfo { m_RequireAll = Copy(rule.m_RequireAll),
                m_RequireAny = Copy(rule.m_RequireAny), m_RequireNone = Copy(rule.m_RequireNone), m_SetState = states });
        }
        return result.ToArray();
    }
    internal static NetNodeStateInfo[] Append(NetNodeStateInfo[]? selected, NetNodeStateInfo[] mechanism)
    {
        var result = new List<NetNodeStateInfo>(selected ?? Array.Empty<NetNodeStateInfo>());
        foreach (var rule in mechanism)
        {
            var states = Motion(rule.m_SetState);
            if (states.Length == 0) continue;
            result.Add(new NetNodeStateInfo { m_RequireAll = Copy(rule.m_RequireAll),
                m_RequireAny = Copy(rule.m_RequireAny), m_RequireNone = Copy(rule.m_RequireNone),
                m_SetState = states, m_MatchType = rule.m_MatchType });
        }
        return result.ToArray();
    }
}
