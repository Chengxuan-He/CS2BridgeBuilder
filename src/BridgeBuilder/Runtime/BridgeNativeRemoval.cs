using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Runtime;

internal static class BridgeNativeRemoval
{
    // Keep the existing AssetData/loaded Unity object identity. Only subsequent native reads
    // change location; no Removed event may unload a material used by another bridge.
    internal static void Redirect(AssetData asset, string target)
    {
        var source=asset.database.dataSource as FileSystemDataSource
            ?? throw new IOException("Shared asset has no native file source: "+asset.id);
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var entries=(System.Collections.Concurrent.ConcurrentDictionary<Colossal.Hash128,FileSystemDataSource.EntryInfo>)
            typeof(FileSystemDataSource).GetField("m_GuidToPhysicalData",flags)!.GetValue(source)!;
        if(!entries.TryGetValue(asset.id.guid,out var old) || old.isPackaged)
            throw new IOException("Shared asset registration is unavailable: "+asset.id);
        if(!BridgeFileAccess.Exists(target)) throw new IOException("Independent shared read source missing: "+target);
        var replacement=new FileSystemDataSource.EntryInfo(old.scheme,old.relativePathStart,target,null,
            old.type,old.isPriorityEntry,old.size,old.offset);
        var unwatch=typeof(FileSystemDataSource).GetMethod("RemoveFromFileSystemSnapshot",flags)!;
        var watch=typeof(FileSystemDataSource).GetMethod("AddToFileSystemSnapshot",flags)!;
        unwatch.Invoke(source,new object[]{old});
        if(!entries.TryUpdate(asset.id.guid,replacement,old))
        {watch.Invoke(source,new object[]{old});throw new IOException("Shared registration changed during redirect: "+asset.id);}
        watch.Invoke(source,new object[]{replacement});
        if(!asset.database.TryGetAsset(asset.id.guid,out var current) || !ReferenceEquals(asset,current))
            throw new IOException("Shared asset identity changed during redirect: "+asset.id);
    }
}
