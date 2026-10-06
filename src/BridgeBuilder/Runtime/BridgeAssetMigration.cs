using System.Collections.Generic;

namespace BridgeBuilder.Runtime;

/// <summary>Persist existing bridge dependencies without reading or changing bridge metadata.</summary>
internal static class BridgeAssetMigration
{
    internal static bool Run(string owner, IEnumerable<string> seeds,
        out bool changed, out string error)
    {
        var success = BridgeDependencyPersistence.Save(owner, seeds, out _, out error, out var writtenFiles);
        changed = writtenFiles != 0;
        return success;
    }
}
