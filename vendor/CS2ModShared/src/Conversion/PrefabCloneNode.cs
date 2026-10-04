using Colossal.IO.AssetDatabase;
using Game.Prefabs;

namespace CS2Mods.Shared.Conversion;

internal sealed class PrefabCloneNode
{
    internal PrefabCloneNode(
        PrefabBase source,
        PrefabBase target,
        bool isRoot,
        bool needsSave,
        PrefabAsset? replacementAsset)
    {
        Source = source;
        Target = target;
        IsRoot = isRoot;
        NeedsSave = needsSave;
        ReplacementAsset = replacementAsset;
    }

    internal PrefabBase Source { get; }
    internal PrefabBase Target { get; }
    internal bool IsRoot { get; }
    internal bool NeedsSave { get; }
    internal PrefabAsset? ReplacementAsset { get; }
}
