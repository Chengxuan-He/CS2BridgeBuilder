using Game.Prefabs;
using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace CS2Mods.Shared.Export;

/// <summary>
/// Collects every prefab reachable from an exported road, using the same notion of "serialized field"
/// as the cloner. Only user assets are followed: a built-in prefab can never point back at an exported
/// dependency, and descending into one would walk most of the game's prefab graph.
/// </summary>
internal static class PrefabReferenceWalker
{
    internal static void CollectInto(PrefabBase root, HashSet<PrefabBase> found)
    {
        VisitPrefab(root, found, new HashSet<object>(ReferenceEqualityComparer<object>.Instance));
    }

    private static void VisitPrefab(PrefabBase prefab, HashSet<PrefabBase> found, HashSet<object> visited)
    {
        if (!found.Add(prefab)) return;
        if (prefab.isBuiltin || prefab.isReadOnly) return;

        VisitFields(prefab, found, visited);
        foreach (var component in prefab.components)
            if (component != null) VisitFields(component, found, visited);
    }

    private static void VisitFields(object target, HashSet<PrefabBase> found, HashSet<object> visited)
    {
        if (!visited.Add(target)) return;
        foreach (var field in SerializedFields.Of(target.GetType()))
        {
            if (field.Name == nameof(PrefabBase.components)) continue;
            object? value;
            try
            {
                value = field.GetValue(target);
            }
            catch (Exception)
            {
                continue;
            }

            VisitValue(value, found, visited);
        }
    }

    private static void VisitValue(object? value, HashSet<PrefabBase> found, HashSet<object> visited)
    {
        switch (value)
        {
            case null:
            case string:
                return;
            case PrefabBase prefab:
                VisitPrefab(prefab, found, visited);
                return;
            case Object:
                return;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    VisitValue(entry.Key, found, visited);
                    VisitValue(entry.Value, found, visited);
                }

                return;
            case IEnumerable enumerable:
                foreach (var item in enumerable) VisitValue(item, found, visited);
                return;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum) return;
        if (!ShouldDescend(type)) return;
        VisitFields(value, found, visited);
    }

    private static bool ShouldDescend(Type type)
    {
        if (type.IsValueType) return true;
        var ns = type.Namespace ?? string.Empty;
        return ns.StartsWith("Game.", StringComparison.Ordinal)
            || ns.StartsWith("Unity.", StringComparison.Ordinal)
            || ns.StartsWith("UnityEngine.", StringComparison.Ordinal);
    }
}
