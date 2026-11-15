using System;
using System.Collections.Generic;

namespace BridgeBuilder.Runtime;

/// <summary>Session bookkeeping for completed self-check recovery.</summary>
internal static class BridgeStartupRecovery
{
    // Keep retired identities across city transitions: live prefab indices cannot be
    // removed safely, so a save can still resolve to their in-memory objects this session.
    internal static readonly HashSet<string> Retired = new(StringComparer.Ordinal);
    internal static void Stop()
    {
        Retired.Clear();
        BridgeAssetCatalog.ResetSession();
    }
}
