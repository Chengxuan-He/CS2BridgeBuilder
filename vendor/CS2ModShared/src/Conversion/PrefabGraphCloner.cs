using Game.Prefabs;
using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CS2Mods.Shared.Conversion;

internal sealed class PrefabGraphCloner
{
    private readonly PrefabSystem _prefabSystem;
    private readonly ExportSettings _settings;
    private readonly ExportReport _report;
    private readonly bool _overwriteExisting;
    private readonly string? _dependencyOwner;
    private readonly Dictionary<PrefabBase, PrefabBase> _clones =
        new(ReferenceEqualityComparer<PrefabBase>.Instance);
    private readonly List<PrefabCloneNode> _nodes = new();
    private readonly HashSet<string> _reportedExternalDependencies = new(StringComparer.Ordinal);

    internal PrefabGraphCloner(
        PrefabSystem prefabSystem,
        ExportSettings settings,
        ExportReport report,
        bool overwriteExisting,
        string? dependencyOwner = null)
    {
        _prefabSystem = prefabSystem;
        _settings = settings;
        _report = report;
        _overwriteExisting = overwriteExisting;
        _dependencyOwner = dependencyOwner;
    }

    internal IReadOnlyList<PrefabCloneNode> Nodes => _nodes;

    internal int NodeCount => _nodes.Count;

    /// <summary>
    /// Drops every node added after <paramref name="nodeCount"/>. Used when one road fails halfway
    /// through, so its half built dependencies never reach the asset writer.
    /// </summary>
    internal void RollbackTo(int nodeCount)
    {
        for (var index = _nodes.Count - 1; index >= nodeCount; index--)
        {
            var node = _nodes[index];
            _clones.Remove(node.Source);
            if (node.NeedsSave && node.Target != null) Object.DestroyImmediate(node.Target);
            _nodes.RemoveAt(index);
        }
    }

    internal RoadPrefab CloneRoad(RoadPrefab source, string exportName, string registeredIcon)
    {
        var target = (RoadPrefab)CloneRuntimePrefab(source, exportName, true);
        if (ReferenceEquals(target, source)) return target;
        var uiObject = target.components.OfType<UIObject>().FirstOrDefault();
        if (uiObject != null) uiObject.m_Icon = registeredIcon;
        return target;
    }


    /// <summary>
    /// A clone of any net, under a name of its own.
    ///
    /// For nets that are not roads and are not the export's subject - a track hung under a bridge as
    /// its lower deck. It is cloned rather than referenced because the copy has to be changed, and the
    /// original is a registered prefab shared with everything else built from it: taking the pillars
    /// off a lower deck must not take them off every other track in the world.
    /// </summary>
    internal NetPrefab CloneNet(NetPrefab source, string exportName)
    {
        return (NetPrefab)CloneRuntimePrefab(source, exportName, true);
    }

    private PrefabBase CloneRuntimePrefab(PrefabBase source, string? forcedName = null, bool isRoot = false)
    {
        if (!isRoot && (source.isBuiltin || source.asset != null)) return source;

        // Roots deliberately skip the reuse lookup. A root is always asked for by name, so handing back
        // a root that was built under a different name would silently produce one asset where two were
        // requested - which is exactly what the bridge exporter needs when it clones a road a second
        // time to serve as the lower deck of a double deck bridge. Dependencies are still shared, so
        // the second root costs no duplicate assets.
        if (!isRoot && _clones.TryGetValue(source, out var known)) return known;

        // Road Builder also supplies TrackBuilderPrefab (TrackPrefab) and path
        // networks. Returning one of these roots as an external dependency lets
        // bridge composition mutate the city's source network. Project every RB
        // network onto its native base type, without loading/referencing the mod.
        var targetType = source.GetType();
        if (source is NetGeometryPrefab && IsRoadBuilderNetwork(source))
            while (targetType.Assembly != typeof(PrefabBase).Assembly && targetType.BaseType != null)
                targetType = targetType.BaseType;
        if (targetType.Assembly != typeof(PrefabBase).Assembly)
        {
            ReportExternalDependency(source);
            return source;
        }

        var targetName = forcedName ?? NameSanitizer.CreateDependencyName(
            _settings.NamePrefix,
            targetType.Name,
            source.name ?? "Unnamed");
        // Scope generated runtime dependencies before looking up existing assets; otherwise
        // separate bridges can reuse the same old unowned RBBridgeDep prefab.
        if (forcedName == null && !string.IsNullOrEmpty(_dependencyOwner))
        {
            var parts = targetName.Split(' ');
            parts[0] += "-" + _dependencyOwner;
            targetName = string.Join(" ", parts);
        }
        var existing = FindExisting(targetType, targetName);
        var needsSave = existing == null || _overwriteExisting;
        var replacementAsset = needsSave ? existing?.asset : null;
        var target = needsSave
            ? (PrefabBase)ScriptableObject.CreateInstance(targetType)
            : existing!;

        // Kept as the cycle guard for the subgraph, but never overwritten: if this source was already
        // cloned as an earlier root, that entry stays the one dependencies resolve to.
        if (!_clones.ContainsKey(source)) _clones[source] = target;
        _nodes.Add(new PrefabCloneNode(source, target, isRoot, needsSave, replacementAsset));
        if (!needsSave) return target;

        CopySerializedFields(source, target);
        target.name = targetName;
        target.isDirty = true;
        CloneComponents(source, target, isRoot);
        return target;
    }

    private PrefabBase? FindExisting(Type targetType, string name)
    {
        return PrefabCatalog.GetAll(_prefabSystem).FirstOrDefault(prefab =>
            prefab != null
            && prefab.GetType() == targetType
            && string.Equals(prefab.name, name, StringComparison.Ordinal)
            && prefab.asset != null
            && !prefab.isReadOnly);
    }

    private void CopySerializedFields(object source, object target)
    {
        foreach (var field in SerializedFields.Of(source.GetType()))
        {
            if (field.Name == nameof(PrefabBase.components)
                || field.Name == nameof(PrefabBase.isDirty)
                || !field.DeclaringType!.IsAssignableFrom(target.GetType()))
            {
                continue;
            }

            try
            {
                var value = field.GetValue(source);
                field.SetValue(target, CloneValue(value, field.FieldType));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Unable to copy serialized field {field.DeclaringType?.FullName}.{field.Name}",
                    exception);
            }
        }
    }

    private object? CloneValue(object? value, Type declaredType)
    {
        if (value == null) return null;
        if (value is PrefabBase prefab) return CloneRuntimePrefab(prefab);
        if (value is Object) return value;

        var runtimeType = value.GetType();
        if (runtimeType.IsPrimitive || runtimeType.IsEnum || runtimeType == typeof(string)
            || runtimeType == typeof(decimal) || runtimeType == typeof(DateTime)
            || runtimeType == typeof(Guid) || runtimeType == typeof(Type)) return value;

        if (value is Array sourceArray)
        {
            var elementType = runtimeType.GetElementType() ?? declaredType.GetElementType() ?? typeof(object);
            var targetArray = Array.CreateInstance(elementType, sourceArray.Length);
            for (var index = 0; index < sourceArray.Length; index++)
                targetArray.SetValue(CloneValue(sourceArray.GetValue(index), elementType), index);
            return targetArray;
        }

        if (value is IDictionary sourceDictionary)
        {
            if (CreateCollection(runtimeType) is not IDictionary targetDictionary) return value;
            foreach (DictionaryEntry entry in sourceDictionary)
                targetDictionary.Add(CloneValue(entry.Key, entry.Key?.GetType() ?? typeof(object)),
                    CloneValue(entry.Value, entry.Value?.GetType() ?? typeof(object)));
            return targetDictionary;
        }

        if (value is IList sourceList)
        {
            if (CreateCollection(runtimeType) is not IList targetList) return value;
            var elementType = runtimeType.IsGenericType ? runtimeType.GetGenericArguments()[0] : typeof(object);
            foreach (var item in sourceList) targetList.Add(CloneValue(item, elementType));
            return targetList;
        }

        if (!ShouldDeepClone(runtimeType)) return value;

        object clone;
        try
        {
            clone = runtimeType.IsValueType
                ? Activator.CreateInstance(runtimeType)!
                : FormatterServices.GetUninitializedObject(runtimeType);
        }
        catch
        {
            return value;
        }

        foreach (var field in SerializedFields.Of(runtimeType))
        {
            var fieldValue = field.GetValue(value);
            field.SetValue(clone, CloneValue(fieldValue, field.FieldType));
        }
        return clone;
    }

    private static object? CreateCollection(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            if (!type.IsGenericType) return null;
            var generic = type.GetGenericTypeDefinition();
            if (generic == typeof(IList<>) || generic == typeof(IEnumerable<>) || generic == typeof(ICollection<>))
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
            return null;
        }
    }

    private static bool ShouldDeepClone(Type type)
    {
        if (type.IsValueType) return true;
        var ns = type.Namespace ?? string.Empty;
        return ns.StartsWith("Game.", StringComparison.Ordinal)
            || ns.StartsWith("Unity.", StringComparison.Ordinal)
            || ns.StartsWith("UnityEngine.", StringComparison.Ordinal);
    }

    private void CloneComponents(PrefabBase source, PrefabBase target, bool isRoot)
    {
        foreach (var component in source.components)
        {
            if (component == null || ShouldStripComponent(component, isRoot)) continue;

            ComponentBase clone;
            try
            {
                clone = (ComponentBase)ScriptableObject.CreateInstance(component.GetType());
            }
            catch (Exception exception)
            {
                _report.Warning($"Skipped component {component.GetType().FullName}: {exception.Message}");
                continue;
            }

            CopySerializedFields(component, clone);
            clone.prefab = target;
            clone.name = component.GetType().Name;
            if (!isRoot && clone is UIObject dependencyUi) ClearRoadBuilderIcon(dependencyUi);
            target.components.Add(clone);
        }
    }

    /// <summary>
    /// Dependency thumbnails point at a UI host that Road Builder registers. Keeping the URI would
    /// leave the exported asset with a link that resolves to nothing once Road Builder is gone.
    /// The road's own icon is not touched here; the caller replaces it with a preserved copy.
    /// </summary>
    private static void ClearRoadBuilderIcon(UIObject uiObject)
    {
        var icon = uiObject.m_Icon;
        if (string.IsNullOrEmpty(icon)) return;
        if (icon.IndexOf("//roadbuilder", StringComparison.OrdinalIgnoreCase) >= 0) uiObject.m_Icon = string.Empty;
    }

    private static bool ShouldStripComponent(ComponentBase component, bool isRoot)
    {
        var type = component.GetType();
        if (string.Equals(type.Assembly.GetName().Name, "RoadBuilder", StringComparison.OrdinalIgnoreCase)
            || (type.Namespace?.StartsWith("RoadBuilder.", StringComparison.Ordinal) ?? false)) return true;
        if (isRoot && component is AssetPackItem) return true;
        if (component is not AssetPackItem packItem || packItem.m_Packs == null) return false;
        return packItem.m_Packs.Any(pack => pack != null
            && ((pack.name ?? string.Empty).Replace(" ", string.Empty)
                .IndexOf("RoadBuilder", StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private void ReportExternalDependency(PrefabBase source)
    {
        var key = source.GetType().FullName + ":" + source.name;
        if (!_reportedExternalDependencies.Add(key)) return;
        _report.Warning(
            $"Kept external runtime dependency {key}. The providing lane/asset mod remains required.");
    }

    private static bool IsRoadBuilderNetwork(PrefabBase source)
    {
        for (var type = source.GetType(); type != null; type = type.BaseType)
            if (string.Equals(type.Assembly.GetName().Name, "RoadBuilder", StringComparison.OrdinalIgnoreCase)
                || (type.Namespace?.StartsWith("RoadBuilder.", StringComparison.Ordinal) ?? false)) return true;
        return false;
    }
}
