using System.Collections.Generic;
using System;
using System.IO;
using Colossal.IO.AssetDatabase;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Export;
using Game.AssetPipeline;

namespace BridgeBuilder.Runtime;

// Group owned native resources without changing prefab names, CIDs or references.
// The same path function is used by creation and file-only layout migration.
internal static class BridgePrefabPersistence
{
    internal static AssetDataPath PathFor(string name)
    {
        if (BridgeAssetInfo.TryFileOwner(name, out var owner))
            return AssetDataPath.Create(BridgeAssetInfo.OwnedDirectory(owner, "Prefabs"),
                name.Equals(owner, StringComparison.OrdinalIgnoreCase) ? "Bridge" : BridgeAssetInfo.FileStem(name, owner), EscapeStrategy.None);
        return AssetImportPipeline.GetPath(null, name, name, DataType.Prefab, AssetDatabase.user, false);
    }

    internal static AssetDataPath ResourcePath(string name, string category) =>
        BridgeAssetInfo.TryFileOwner(name, out var owner)
            ? AssetDataPath.Create(BridgeAssetInfo.OwnedDirectory(owner, category),
                BridgeAssetInfo.FileStem(name, owner), EscapeStrategy.None)
            : AssetDataPath.Create("BridgeBuilder", BridgeAssetInfo.FileStem(name, ""), EscapeStrategy.None);

    internal static string RelativePathFor(string name) =>
        PathFor(name).ToPath(new FileSystemDataSource.PathEscapePolicy()) + ".Prefab";

    // Only freshly generated resources, before publication. Native MoveTo targets another
    // database and deletes the source registration; it must not be used within one database.
    internal static void RelocateGenerated(AssetData asset, string owner, string category)
    {
        var database = asset.database;
        var original = asset.GetMeta();
        var oldId = asset.id;
        bool midMips = original.extension.Equals(".MidMips", StringComparison.OrdinalIgnoreCase);
        var folder = BridgeAssetInfo.OwnedDirectory(owner, category);
        // Native caches share a tag basename; distinguish them by unchanged asset identity.
        if (midMips) folder = Path.Combine(folder, oldId.guid.ToString());
        var path = AssetDataPath.Create(folder,
            midMips ? Path.GetFileNameWithoutExtension(original.path) : BridgeAssetInfo.FileStem(asset.name, owner), EscapeStrategy.None);
        var target = Path.Combine(database.rootPath,
            path.ToPath(new FileSystemDataSource.PathEscapePolicy()) + original.extension);
        if (Path.GetFullPath(original.path).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) return;
        if (BridgeFileAccess.Exists(target) || BridgeFileAccess.Exists(target + ".cid"))
            throw new IOException("Generated resource destination already exists: " + target);
        Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(target)!));
        File.Copy(BridgeFileAccess.Native(original.path), BridgeFileAccess.Native(target), false);
        try
        {
            File.Copy(BridgeFileAccess.Native(original.path + ".cid"), BridgeFileAccess.Native(target + ".cid"), false);
            database.dataSource.DeleteEntryFromDatabase(oldId);
            if (database.dataSource.Contains(oldId)) throw new IOException("Could not detach generated resource: " + original.path);
            asset.id = database.dataSource.AddEntryFromDatabase(path, asset.GetType(), oldId);
            database.dataSource.SetTimestamp(asset.id, original.creationTime, original.lastAccessTime, original.lastWriteTime);
        }
        catch
        {
            // Preserve the only remaining payload if registration failed after detachment.
            if (!database.dataSource.Contains(oldId))
            {
                File.Copy(BridgeFileAccess.Native(target), BridgeFileAccess.Native(original.path), true);
                File.Copy(BridgeFileAccess.Native(target + ".cid"), BridgeFileAccess.Native(original.path + ".cid"), true);
                asset.id = database.dataSource.AddEntryFromDatabase(
                    AssetDataPath.Create(original.subPath, original.fileName, EscapeStrategy.None), asset.GetType(), oldId);
            }
            throw;
        }
    }

    internal static int Save(IReadOnlyList<PrefabCloneNode> nodes)
    {
        foreach (var node in nodes)
            if (node.NeedsSave && node.ReplacementAsset == null && node.Target.asset == null)
                PrefabAssetExtensions.AddAsset(AssetDatabase.user, PathFor(node.Target.name), node.Target);
        // Existing assets retain identity/path until the backed-up file-only migration.
        return new PrefabAssetWriter().Save(nodes);
    }
}
