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
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(PrefabSystem).FullName, "m_Prefabs");

    internal static IEnumerable<PrefabBase> GetAll(PrefabSystem prefabSystem)
    {
        if (PrefabsField.GetValue(prefabSystem) is IEnumerable<PrefabBase> prefabs)
            return prefabs.Where(prefab => prefab != null);
        throw new InvalidOperationException("PrefabSystem.m_Prefabs has an unexpected type");
    }
}
