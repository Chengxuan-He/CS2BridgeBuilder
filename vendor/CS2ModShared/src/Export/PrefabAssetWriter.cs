using Colossal.IO.AssetDatabase;
using Game.AssetPipeline;
using CS2Mods.Shared.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CS2Mods.Shared.Export;

internal sealed class PrefabAssetWriter
{
    internal static AssetDataPath PathFor(string name) => AssetImportPipeline.GetPath(
        null, name, name, DataType.Prefab, AssetDatabase.user, false);

    internal static string RelativePathFor(string name) =>
        PathFor(name).ToPath(new FileSystemDataSource.PathEscapePolicy()) + ".Prefab";
    private static readonly MethodInfo SetUninitializedInstanceMethod = typeof(PrefabAsset).GetMethod(
        "SetUninitializedInstance",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(PrefabAsset).FullName, "SetUninitializedInstance");

    /// <summary>
    /// Saves the cloned graph to disk and returns how many runtime dependencies were written. Registering
    /// them in the running world is the caller's job - the asset alone does not make a usable prefab.
    /// Every clone is registered in the asset database before anything is serialized: a reference is
    /// written as the target's asset id, so a dependency without an asset yet would serialize as null.
    /// Registering first makes the result independent of the order the graph happened to be built in.
    /// </summary>
    internal int Save(IReadOnlyList<PrefabCloneNode> nodes)
    {
        var pending = nodes.Where(node => node.NeedsSave).ToList();
        var assets = new List<PrefabAsset>(pending.Count);
        foreach (var node in pending) assets.Add(Register(node));
        // Write only the explicit owned plan. Native recursive Save would also rewrite shared donors.
        foreach (var asset in assets)
            asset.Save(null, Colossal.IO.AssetDatabase.ContentType.Text, new HashSet<PrefabAsset>(), false, true);
        return pending.Count(node => !node.IsRoot);
    }

    private static PrefabAsset Register(PrefabCloneNode node)
    {
        var prefab = node.Target;
        if (node.ReplacementAsset != null)
        {
            // An existing asset is matched by name across the whole user database, so a stray copy in an
            // unexpected folder would silently become the write target. Record where the data actually goes.
            if (node.IsRoot) ModHost.Log.Info($"Overwriting the existing asset of '{prefab.name}' at {node.ReplacementAsset.path}");
            SetUninitializedInstanceMethod.Invoke(node.ReplacementAsset, new object[] { prefab });
            return node.ReplacementAsset;
        }

        if (node.IsRoot && prefab.asset == null)
            ModHost.Log.Info($"Writing a new asset for '{prefab.name}'");

        if (prefab.asset == null)
        {
            var path = PathFor(prefab.name);
            return PrefabAssetExtensions.AddAsset(AssetDatabase.user, path, prefab);
        }

        if (prefab.isReadOnly)
            throw new InvalidOperationException($"Refusing to overwrite read-only prefab asset '{prefab.name}'");
        return prefab.asset;
    }
}
