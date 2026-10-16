using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;
namespace BridgeBuilder.Bridges;
internal sealed partial class TowerFactory
{
    internal T ResolveOriginal<T>(string name) where T : PrefabBase
    {
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var prefab in _prefabs)
            if (prefab is T typed && string.Equals(typed.name, name, StringComparison.Ordinal) &&
                !BridgeAssetInfo.IsOtherBridgePrefab(typed.asset?.path, _bridgeName, imported)) return typed;
        throw new InvalidOperationException($"Original {typeof(T).Name} '{name}' is unavailable.");
    }
}
