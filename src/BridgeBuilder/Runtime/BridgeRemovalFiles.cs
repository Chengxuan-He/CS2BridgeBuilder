using System;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

internal static class BridgeRemovalFiles
{
    internal static string Match(string root,string owner)
    {
        if(!BridgeAssetInfo.IsPrefabName(owner)) throw new InvalidDataException("Invalid bridge UUID.");
        string imported=Path.GetFullPath(Path.Combine(root,"ImportedData"));
        string target=Path.GetFullPath(Path.Combine(imported,owner));
        if(!string.Equals(Path.GetDirectoryName(target),imported,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Bridge directory must be an immediate child of ImportedData.");
        return target;
    }

    internal static bool IsDirectoryLink(string path)
        => (File.GetAttributes(BridgeFileAccess.Native(path)) & FileAttributes.ReparsePoint) != 0;

    internal static void Clear(string root,string owner)
    {
        string directory=Match(root,owner);
        if (!BridgeFileAccess.Exists(directory)) return;
        RequireExclusive(root, directory, owner);
        BridgeFileAccess.DeletePayload(directory);
    }
    // Logical UUID ownership authorizes mapped storage, except a proven overlap with another owner.
    internal static void RequireExclusive(string root, string source, string owner)
    {
        string imported = Path.Combine(root, "ImportedData");
        if (!Directory.Exists(BridgeFileAccess.Native(imported))) return;
        var scopes = Directory.Exists(BridgeFileAccess.Native(source))
            ? BridgeFileAccess.Directories(source).Where(p => p.Equals(source, BridgeFileAccess.PathComparison) || IsDirectoryLink(p)).ToArray() : new[] { source };
        foreach (string other in Directory.GetDirectories(BridgeFileAccess.Native(imported)))
        {
            string id = Path.GetFileName(other);
            if (id.Equals(owner, StringComparison.OrdinalIgnoreCase) || !BridgeAssetInfo.IsPrefabName(id)) continue;
            foreach (string boundary in BridgeFileAccess.MappedBoundaries(BridgeFileAccess.Logical(other)))
                foreach (string scope in scopes)
                    if (BridgeFileAccess.WithinPhysical(scope, boundary) || BridgeFileAccess.WithinPhysical(boundary, scope))
                        throw new IOException("Mapped payload overlaps another bridge owner: " + id);
        }
    }

}
