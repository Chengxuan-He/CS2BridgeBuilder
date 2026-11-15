using System;
using System.Linq;
namespace BridgeBuilder.Runtime;
// Historical external snapshots are recognized only by inspection, never as private generated files.
internal static class BridgeInspectionPaths
{
    internal static bool IsDependencyPath(string path, string owner) =>
        BridgeAssetInfo.IsDependencyPath(path, owner) || path.Split('\\', '/')
            .Any(part => part.Equals(owner + "_Dependencies", StringComparison.OrdinalIgnoreCase));
}
