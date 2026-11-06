using System;
using System.IO;

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
        RejectLink(root);
        RejectLink(imported);
        if(BridgeFileAccess.Exists(target)) RejectLink(target);
        return target;
    }

    internal static void RejectLink(string path)
    {
        if((File.GetAttributes(BridgeFileAccess.Native(path)) & FileAttributes.ReparsePoint)!=0)
            throw new IOException("Refused linked directory: "+path);
    }

    internal static void Clear(string root,string owner)
    {
        string directory=Match(root,owner);
        if(BridgeFileAccess.Exists(directory)) Directory.Delete(BridgeFileAccess.Native(directory),recursive:true);
    }
}
