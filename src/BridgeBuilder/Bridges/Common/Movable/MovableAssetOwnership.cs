using System;
using BridgeBuilder.Runtime;

namespace BridgeBuilder.Bridges;

// Permanent assets and disposable preview recipes deliberately have different identities.
// Keep temporary identities out of BridgeAssetInfo and the permanent persistence boundary.
internal static class MovableAssetOwnership
{
    internal static bool Matches(string? name, string owner, PreviewGeometry? preview)
    {
        if (preview == null) return BridgeAssetInfo.MatchesOwner(name, owner);
        return owner.StartsWith("tmp", StringComparison.Ordinal)
            && Guid.TryParseExact(owner.Substring(3), "D", out _)
            && name != null && name.IndexOf(owner, StringComparison.Ordinal) >= 0;
    }
}
