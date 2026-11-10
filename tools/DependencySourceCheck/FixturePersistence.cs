using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

// Explicit native edges supplied by each fixture, never reverse-discovered from its assets.
internal static class FixturePersistence
{
    internal static readonly Dictionary<string, AssetData[]> NativeEdges = new();
    internal static bool InspectSnapshots;
    internal static void Inspect(BridgeCreationDependencies context,string owner)
    {
        var root=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData",owner);
        var inventory=new BridgeInspectionCaches(()=>AssetDatabase.global.Assets.OfType<MidMipCacheAsset>(),
            ()=>Directory.Exists(root)?Directory.EnumerateFiles(root,"*.MidMips",SearchOption.AllDirectories):Array.Empty<string>());
        if(!inventory.Prepare(out var error))throw new InvalidOperationException(error);
        context.Inspect(inventory);
    }


    internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error)
        => Save(owner, seeds, out count, out error, out _);

    internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error, out int written)
    {
        var context = new BridgeCreationDependencies(asset =>
            NativeEdges.TryGetValue(asset.id.guid.ToString(), out var caches) ? caches : InspectSnapshots ? throw new InvalidOperationException("Native registration unavailable") : Array.Empty<AssetData>());
        if (InspectSnapshots) FixturePersistence.Inspect(context,owner);
        return Save(owner, seeds, context, out count, out error, out written);
    }

    internal static bool Save(string owner, IEnumerable<string> seeds, BridgeCreationDependencies context,
        out int count, out string error, out int written)
        => BridgeDependencyPersistence.Save(owner, seeds, context, out count, out error, out written);
}
