using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CS2Mods.Shared.Infrastructure;

/// <summary>
/// Enumerates the fields Unity would serialize for a prefab, component or nested struct.
/// Shared by the cloner and the dependency walker so both agree on what a prefab "contains".
/// </summary>
internal static class SerializedFields
{
    internal static IEnumerable<FieldInfo> Of(Type type)
    {
        for (var current = type; current != null && current != typeof(Object); current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized) continue;
                if (field.IsPublic
                    || field.GetCustomAttribute<SerializeField>() != null
                    || field.GetCustomAttribute<SerializeReference>() != null) yield return field;
            }
        }
    }
}
