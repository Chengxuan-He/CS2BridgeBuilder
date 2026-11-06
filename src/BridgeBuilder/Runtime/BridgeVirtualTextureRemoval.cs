using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Unity.IO.LowLevel.Unsafe;

namespace BridgeBuilder.Runtime;

// Disconnect disk-backed routes without rebuilding native stacks or changing live atlas coordinates.
internal static class BridgeVirtualTextureRemoval
{
    internal static void Disconnect(TextureStreamingSystem streaming, Func<string?,bool> owned, Func<SurfaceAsset,bool> ownedSurface)
    {
        var atlas = Field(streaming, "m_AtlasMaterialsDatabase");
        var database = Field(streaming, "m_VtDatabase");
        var groups = atlas == null ? null : (List<AtlasMaterialsGrouper>)Field(atlas, "m_AtlasMaterialsGroupers")!;
        var retiring = groups?.Where(group => owned((string?)Field(group, "m_MidMipPath"))).ToHashSet()
            ?? new HashSet<AtlasMaterialsGrouper>();
        if (retiring.Count != 0 && retiring.Count == groups!.Count)
            throw new IOException("The last native atlas group still owns the atlas configuration.");

        var retired = new HashSet<Colossal.Hash128>();
        if(database is VTDatabase vt)
        {
            var paths=(IDictionary)Field(vt,"m_VTTexturePaths")!;
            foreach(DictionaryEntry entry in paths)
                if(owned((string)Field(entry.Value!,"path")!))retired.Add((Colossal.Hash128)entry.Key);
            if(retired.Count!=0)
            {
                foreach(var guid in retired)
                    if(!vt.IsDoneLoading(guid))throw new Systems.BridgeOwnerMemory.InUse("A native texture producer is still writing owner staging data.");
                foreach(var loader in (IEnumerable)Field(streaming,"m_TexturesAsyncLoaders")!)
                    foreach(string field in new[]{"m_Format0DependenciesData","m_Format0AsyncData"})
                        foreach(var item in (IEnumerable)Field(loader,field)!)
                            if(retired.Contains((Colossal.Hash128)Field(item,"textureGuid")!))throw new Systems.BridgeOwnerMemory.InUse("A native texture loader still requires owner staging data.");
                var surfaceSlots=(IList)Field(vt,"m_VTSurfaceTileInfo")!;
                var channels=(IList)Field(vt,"m_TextureGuidsPerIndex")!;
                for(int i=0;i<channels.Count;i++)
                    foreach(IDictionary layer in (IEnumerable)channels[i]!)
                        foreach(DictionaryEntry cell in layer)
                            if(retired.Contains((Colossal.Hash128)cell.Value!) && i<surfaceSlots.Count && surfaceSlots[i] is IDictionary routes &&
                                routes[cell.Key] is object route && !owned((string)Field(route,"path")!))
                                throw new Systems.BridgeOwnerMemory.InUse("Another native surface uses the owner's texture route.");
            }
        }

        // These requests own their buffers. Complete reads/decompression, leave disposal and
        // submission to the same native owner on its next update.
        foreach (var request in (IEnumerable)Field(streaming, "m_BusyRequests")!)
        {
            foreach (var read in (List<ReadHandle>)Field(request, "m_ReadHandles")!)
                if (read.IsValid() && read.Status == ReadStatus.InProgress) read.JobHandle.Complete();
            foreach (var read in (List<ReadAndDecompressHandle>)Field(request, "m_ReadAndDecompressionHandles")!)
                if (read.IsValid()) read.Complete();
        }
        foreach (string field in new[] { "m_VTAssetsToInit", "m_VTAssetsShallowInit" })
        {
            var queue = (Queue<SurfaceAsset>)Field(streaming, field)!;
            int count = queue.Count;
            for (int i = 0; i < count; i++)
            {
                var surface = queue.Dequeue();
                if (ReferenceEquals(surface, null) || !ownedSurface(surface)) queue.Enqueue(surface!);
            }
        }
        if (database != null)
        {
            // VTProceduralCPU tests GetVTSurfaceTileInfo and IsVTTextureData before disk reads.
            // Remove the owning path and its per-layer guard together; keep cached pixels intact.
            foreach (IDictionary slots in (IEnumerable)Field(database, "m_VTSurfaceTileInfo")!)
                RemoveWhere(slots, entry => owned((string)Field(entry.Value!, "path")!));
            var paths = (IDictionary)Field(database, "m_VTTexturePaths")!;
            if (retired.Count != 0)
            {
                foreach (IEnumerable layers in (IEnumerable)Field(database, "m_TextureGuidsPerIndex")!)
                    foreach (IDictionary slots in layers) RemoveWhere(slots, entry => retired.Contains((Colossal.Hash128)entry.Value!));
                var registry=(IDictionary)Field(database,"m_RegisteredTextures")!;
                var staging=(Dictionary<Colossal.Hash128,Unity.Collections.NativeArray<byte>>)Field(database,"m_TexturesData")!;
                var size=typeof(VTDatabase).GetField("m_DataSize",BindingFlags.Instance|BindingFlags.NonPublic)!;
                foreach(var key in retired)
                {
                    if(staging.TryGetValue(key,out var buffer))
                    {
                        size.SetValue(database,checked((int)size.GetValue(database)!-buffer.Length));
                        buffer.Dispose();
                        staging.Remove(key);
                    }
                    registry.Remove(key);
                    paths.Remove(key);
                }
            }
        }
        if (retiring.Count == 0) return;
        if (Field(atlas!, "m_PreprocessedMidMipTilesInfo") is IDictionary stacks)
            foreach (IDictionary entries in stacks.Values)
                RemoveWhere(entries, entry => retiring.Contains((AtlasMaterialsGrouper)Field(entry.Value!, "m_Grouper")!));
        var registered = (HashSet<AtlasMaterialsGrouper>)Field(atlas!, "m_RegisteredAtlasMaterialsGroupers")!;
        registered.ExceptWith(retiring);
        groups!.RemoveAll(retiring.Contains);
        foreach(var group in retiring)
            if(Field(group,"m_PreprocessedTiles") is IList tiles){group.Dispose();tiles.Clear();}
    }

    private static void RemoveWhere(IDictionary map, Func<DictionaryEntry,bool> match)
    {
        var keys = new List<object>();
        foreach (DictionaryEntry entry in map)
            if (match(entry)) keys.Add(entry.Key);
        foreach (var key in keys) map.Remove(key);
    }

    private static object? Field(object instance,string name)
    {
        var field = instance.GetType().GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            ?? throw new MissingFieldException(instance.GetType().FullName, name);
        return field.GetValue(instance);
    }
}
