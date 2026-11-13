using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CS2Mods.Shared.Infrastructure;

internal static class PrefabCatalog
{
    private static readonly FieldInfo PrefabsField = typeof(PrefabSystem).GetField(
        "m_Prefabs",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    internal static IEnumerable<PrefabBase> GetAll(PrefabSystem prefabSystem)
    {
        return ((IEnumerable<PrefabBase>)PrefabsField.GetValue(prefabSystem)!).Where(prefab => prefab != null);
    }
}
