using Colossal.Json;
using Game.Prefabs;
using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CS2Mods.Shared.Discovery;

/// <summary>Road Builder discovery uses registered prefab identifiers, independently of labels.</summary>
internal sealed class RoadDiscoveryResult
{
    internal RoadDiscoveryResult(IReadOnlyList<RoadBuilderRoad> roads) => Roads = roads;
    internal IReadOnlyList<RoadBuilderRoad> Roads { get; }
}

internal static class RoadBuilderDiscovery
{
    private const string RoadBuilderPrefabType = "RoadBuilder.Domain.Prefabs.RoadBuilderPrefab";
    private const string FingerprintVersion = "3";
    private static readonly Regex RoadIdPattern = new(
        @"r[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}-\d+",
        RegexOptions.Compiled);

    internal static RoadDiscoveryResult Find(PrefabSystem prefabSystem)
    {
        var roads = PrefabCatalog.GetAll(prefabSystem).OfType<RoadPrefab>()
            .Where(IsRoadBuilderRoad)
            .GroupBy(prefab => prefab.name, StringComparer.Ordinal)
            .Select(group => Describe(group.First()))
            .OrderBy(road => road.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(road => road.Id, StringComparer.Ordinal)
            .ToList();
        return new RoadDiscoveryResult(roads);
    }

    /// <summary>Cheap poll used while waiting for Road Builder.</summary>
    internal static int CountRoads(PrefabSystem prefabSystem) =>
        PrefabCatalog.GetAll(prefabSystem).OfType<RoadPrefab>().Count(IsRoadBuilderRoad);

    private static bool IsRoadBuilderRoad(RoadPrefab prefab)
    {
        var type = prefab.GetType();
        if (string.Equals(type.FullName, RoadBuilderPrefabType, StringComparison.Ordinal)) return true;

        var assemblyName = type.Assembly.GetName().Name;
        return string.Equals(assemblyName, "RoadBuilder", StringComparison.OrdinalIgnoreCase)
            && string.Equals(type.Name, "RoadBuilderPrefab", StringComparison.Ordinal);
    }

    private static RoadBuilderRoad Describe(RoadPrefab prefab)
    {
        var config = GetConfig(prefab);
        var registeredName = GetStringProperty(config, "Name").Trim();
        var name = registeredName.Length == 0 ? prefab.name : registeredName;

        var registeredIcon = prefab.components.OfType<UIObject>().FirstOrDefault()?.m_Icon ?? string.Empty;
        var (width, modules) = DescribeComposition(config);
        return new RoadBuilderRoad(
            prefab,
            name,
            registeredIcon,
            BuildFingerprint(prefab, config, name),
            width,
            ReadConfiguredSpeedLimit(config),
            modules);
    }

    /// <summary>Content fingerprint only; source identity is always RoadBuilderRoad.Id.</summary>
    private static string BuildFingerprint(RoadPrefab prefab, object? config, string name)
    {
        string canonical;
        try
        {
            canonical = config == null ? BuildFallbackDescription(prefab) : JSON.Dump(config);
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, $"Unable to serialize the Road Builder config of '{name}' for fingerprinting");
            canonical = BuildFallbackDescription(prefab);
        }

        canonical = RoadIdPattern.Replace(canonical, "<road-id>");
        return NameSanitizer.ShortHash($"ExporterIdentityVersion={FingerprintVersion}\nName={name}\n{canonical}", 32);
    }

    /// <summary>
    /// Reads the lane layout out of the Road Builder configuration: total width and one entry per lane
    /// group, in order across the road. Runs entirely through reflection, so this stays free of a
    /// compile time dependency on Road Builder. Consecutive identical lanes are folded into a count.
    /// </summary>
    private static (float Width, IReadOnlyList<string> Modules) DescribeComposition(object? config)
    {
        var modules = new List<string>();
        if (config?.GetType().GetProperty("Lanes", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(config) is not IEnumerable lanes)
        {
            return (0f, modules);
        }

        var width = 0f;
        string? previous = null;
        var repeats = 0;
        foreach (var lane in lanes)
        {
            if (lane == null) continue;
            var label = DescribeLane(lane, out var laneWidth);
            width += laneWidth;

            if (string.Equals(label, previous, StringComparison.Ordinal))
            {
                repeats++;
                continue;
            }

            if (previous != null) modules.Add(repeats > 1 ? $"{previous} x{repeats}" : previous);
            previous = label;
            repeats = 1;
        }

        if (previous != null) modules.Add(repeats > 1 ? $"{previous} x{repeats}" : previous);
        return (width, modules);
    }

    private static string DescribeLane(object lane, out float width)
    {
        width = 0f;
        var type = lane.GetType();
        var group = GetStringProperty(lane, "GroupPrefabName");
        var section = GetStringProperty(lane, "SectionPrefabName");
        var name = group.Length > 0 ? ShortenGroupName(group) : section.Length > 0 ? section : "Lane";

        if (type.GetProperty("GroupOptions", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(lane) is not IDictionary options)
        {
            return name;
        }

        var details = new List<string>();
        foreach (DictionaryEntry entry in options)
        {
            if (entry.Value is not string value || value.Length == 0) continue;
            var key = entry.Key as string ?? string.Empty;
            if (key.IndexOf("Width", StringComparison.OrdinalIgnoreCase) >= 0 && TryReadMetres(value, out var metres))
                width = metres;
            details.Add(value);
        }

        return details.Count == 0 ? name : $"{name} ({string.Join(", ", details)})";
    }

    /// <summary>"RoadBuilder.LaneGroups.SidewalkGroupPrefab" becomes "Sidewalk".</summary>
    private static string ShortenGroupName(string groupPrefabName)
    {
        var separator = groupPrefabName.LastIndexOf('.');
        var name = separator >= 0 ? groupPrefabName.Substring(separator + 1) : groupPrefabName;
        if (name.EndsWith("GroupPrefab", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "GroupPrefab".Length);
        else if (name.EndsWith("Prefab", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Prefab".Length);
        return name.Length == 0 ? groupPrefabName : name;
    }

    /// <summary>Road Builder stores widths as display strings such as "4m" or "1.5m".</summary>
    private static bool TryReadMetres(string value, out float metres)
    {
        var digits = value.TrimEnd('m', 'M', ' ');
        return float.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out metres);
    }


    /// <summary>
    /// The speed limit the player set in Road Builder, or 0 when it cannot be read.
    ///
    /// Read from the configuration rather than taken from the prefab because the configuration is
    /// what the player actually authored. Whether Road Builder also writes it onto the prefab field
    /// is not something this mod can rely on - it reaches Road Builder only through reflection, and
    /// a generated prefab that kept its template's speed would export a road that looks right and
    /// drives wrong. The two are compared at export time and any disagreement is reported.
    /// </summary>
    private static float ReadConfiguredSpeedLimit(object? config)
    {
        try
        {
            var value = config?.GetType()
                .GetProperty("SpeedLimit", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(config);
            return value is float speed && speed > 0f ? speed : 0f;
        }
        catch (Exception)
        {
            return 0f;
        }
    }

    private static object? GetConfig(RoadPrefab prefab)
    {
        return prefab.GetType()
            .GetProperty("Config", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(prefab);
    }

    private static string GetStringProperty(object? target, string propertyName)
    {
        return target?.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(target) as string ?? string.Empty;
    }

    /// <summary>Used when the config is unavailable. Deliberately omits prefab.name, which is the road id.</summary>
    private static string BuildFallbackDescription(RoadPrefab prefab)
    {
        var sectionNames = prefab.m_Sections == null
            ? string.Empty
            : string.Join("|", prefab.m_Sections.Select(item => item.m_Section?.name ?? "<null>"));
        return string.Join(
            ";",
            prefab.m_RoadType,
            prefab.m_SpeedLimit,
            prefab.m_MaxSlopeSteepness,
            prefab.m_TrafficLights,
            prefab.m_HighwayRules,
            sectionNames);
    }
}
