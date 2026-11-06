using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Runtime;

internal static class BridgeNativeRemoval
{
    internal static void Detach(ILocalAssetDatabase database,IEnumerable<AssetData> assets,string root,string owner)
    {
        var source=database.dataSource as FileSystemDataSource
            ?? throw new InvalidOperationException("Expected native file-system user asset database.");
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var remove=typeof(FileSystemDataSource).GetMethod("RemoveEntry",flags)
            ?? throw new MissingMethodException("FileSystemDataSource.RemoveEntry");
        var lookup=typeof(FileSystemDataSource).GetMethod("TryGetEntry",flags)
            ?? throw new MissingMethodException("FileSystemDataSource.TryGetEntry");
        var unwatch=typeof(FileSystemDataSource).GetMethod("RemoveFromFileSystemSnapshot",flags)
            ?? throw new MissingMethodException("FileSystemDataSource.RemoveFromFileSystemSnapshot");
        foreach(var asset in assets)
        {
            var id=asset.id.guid;
            object?[] args={id,null};
            if(!(bool)lookup.Invoke(source,args)!) continue;
            var entry=(FileSystemDataSource.EntryInfo)args[1]!;
            if(BridgeOwnedDirectoryRemoval.AssetPath(root,owner,entry.path)==null) continue;
            if(entry.isPackaged) throw new IOException("Refused packaged data-source registration: "+entry.path);
            unwatch.Invoke(source,new object[]{entry});
            remove.Invoke(source,new object[]{id});
            if(source.Contains(id) || database.TryGetAsset(id,out _))
                throw new IOException("Native CID detachment failed: "+id);
        }
    }
}
