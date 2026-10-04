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

/// <summary>What a scan found, including what it had to leave out and why.</summary>
internal sealed class RoadDiscoveryResult
{
    internal RoadDiscoveryResult(IReadOnlyList<RoadBuilderRoad> roads, int skippedBroken, int skippedNameConflicts)
    {
        Roads = roads;
        SkippedBroken = skippedBroken;
        SkippedNameConflicts = skippedNameConflicts;
    }

    internal IReadOnlyList<RoadBuilderRoad> Roads { get; }
    internal int SkippedBroken { get; }
    internal int SkippedNameConflicts { get; }
}

internal static class RoadBuilderDiscovery
{
    private const string RoadBuilderPrefabType = "RoadBuilder.Domain.Prefabs.RoadBuilderPrefab";

    /// <summary>Bump when the canonical form changes, so old fingerprints are not trusted.</summary>
    private const string FingerprintVersion = "3";

    /// <summary>Matches a Road Builder road id such as r03776e2c-574e-...-76561199197854251.</summary>
    private static readonly Regex RoadIdPattern = new(
        @"r[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}-\d+",
        RegexOptions.Compiled);

    private static string _lastWarnings = string.Empty;

    /// <summary>
    /// Lists the roads that can be exported.
    /// A road's name is its identity, so a name that is not unambiguously free is a hard stop rather
    /// than something to work around: appending a discriminator would either put Road Builder's id back
    /// into the asset name, or tie the name to the content and rename the asset on every edit.
    /// </summary>
    internal static RoadDiscoveryResult Find(PrefabSystem prefabSystem, ICollection<string> ownedExportNames)
    {
        var candidates = PrefabCatalog.GetAll(prefabSystem).OfType<RoadPrefab>().Where(IsRoadBuilderRoad).ToList();

        // A scan runs whenever the options page is on screen, so the same complaints would otherwise
        // be logged once a second. Collect them and only write them out when the set actually changes.
        var warnings = new List<string>();
        var usable = new List<RoadBuilderRoad>(candidates.Count);
        var broken = 0;
        var unnameable = 0;
        foreach (var prefab in candidates)
        {
            if (HasBrokenConfiguration(prefab))
            {
                broken++;
                warnings.Add(
                    $"Skipping '{prefab.name}': Road Builder has no configuration for it, so the prefab is "
                    + "incomplete. Road Builder logs this as 'NULL CONFIG WHILE GENERATING'.");
                continue;
            }

            var road = Describe(prefab, warnings);
            if (road == null)
            {
                unnameable++;
                continue;
            }

            usable.Add(road);
        }

        var taken = new HashSet<string>(TakenNames(prefabSystem, ownedExportNames), StringComparer.Ordinal);
        var byName = usable.GroupBy(road => road.Name, StringComparer.Ordinal).ToList();

        var roads = new List<RoadBuilderRoad>(usable.Count);
        var conflicts = unnameable;
        foreach (var group in byName)
        {
            if (group.Count() > 1)
            {
                conflicts += group.Count();
                warnings.Add(
                    $"Skipping {group.Count()} roads all registered as '{group.Key}'. Their exported assets "
                    + "would be indistinguishable; rename them in Road Builder first.");
                continue;
            }

            if (taken.Contains(group.Key))
            {
                conflicts++;
                warnings.Add(
                    $"Skipping '{group.Key}': another road prefab already uses that name, and exporting would "
                    + "shadow it. Rename the road in Road Builder first.");
                continue;
            }

            roads.Add(group.First());
        }

        roads.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
        FlushWarnings(warnings);
        return new RoadDiscoveryResult(roads, broken, conflicts);
    }

    /// <summary>Writes the scan's complaints only when they differ from the previous scan's.</summary>
    private static void FlushWarnings(List<string> warnings)
    {
        var signature = string.Join("\n", warnings);
        if (string.Equals(signature, _lastWarnings, StringComparison.Ordinal)) return;
        _lastWarnings = signature;
        foreach (var warning in warnings) ModHost.Log.Warn(warning);
    }

    /// <summary>Cheap poll used while waiting for Road Builder: no naming, hashing or sorting.</summary>
    internal static int CountRoads(PrefabSystem prefabSystem) =>
        PrefabCatalog.GetAll(prefabSystem).OfType<RoadPrefab>().Count(IsRoadBuilderRoad);

    /// <summary>
    /// Names that already belong to some other road prefab - vanilla, another mod, or a user asset this
    /// exporter did not write. Names this exporter produced are excluded: a road keeps its own name.
    /// </summary>
    private static IEnumerable<string> TakenNames(PrefabSystem prefabSystem, ICollection<string> ownedExportNames)
    {
        return PrefabCatalog.GetAll(prefabSystem)
            .OfType<RoadPrefab>()
            .Where(prefab => !IsRoadBuilderRoad(prefab))
            .Select(prefab => prefab.name)
            .Where(name => !string.IsNullOrEmpty(name) && !ownedExportNames.Contains(name));
    }

    /// <summary>
    /// True when Road Builder exposes a configuration for this road but it is null, which is how a
    /// road whose generation failed looks. Such a prefab is half built - Road Builder's own thumbnail
    /// step throws on it - and exporting it would write out a broken asset. A missing property means a
    /// different Road Builder version instead, which the fingerprint fallback already covers.
    /// </summary>
    private static bool HasBrokenConfiguration(RoadPrefab prefab)
    {
        var property = prefab.GetType().GetProperty("Config", BindingFlags.Instance | BindingFlags.Public);
        return property != null && property.GetValue(prefab) == null;
    }

    private static bool IsRoadBuilderRoad(RoadPrefab prefab)
    {
        var type = prefab.GetType();
        if (string.Equals(type.FullName, RoadBuilderPrefabType, StringComparison.Ordinal)) return true;

        var assemblyName = type.Assembly.GetName().Name;
        return string.Equals(assemblyName, "RoadBuilder", StringComparison.OrdinalIgnoreCase)
            && string.Equals(type.Name, "RoadBuilderPrefab", StringComparison.Ordinal);
    }

    /// <summary>Null when the road has no usable name, which under name-as-identity means it cannot be exported.</summary>
    private static RoadBuilderRoad? Describe(RoadPrefab prefab, List<string> warnings)
    {
        var diagnosticId = string.IsNullOrWhiteSpace(prefab.name) ? "unnamed-road" : prefab.name;
        var config = GetConfig(prefab);
        var registeredName = GetStringProperty(config, "Name").Trim();
        var name = NameSanitizer.MakeFileSystemSafe(registeredName);
        if (name.Length == 0)
        {
            // No name means no identity, and a generated one would have to come from the Road Builder id.
            warnings.Add(
                $"Skipping Road Builder prefab '{diagnosticId}': it has no usable registered name. "
                + "Give it a name in Road Builder to make it exportable.");
            return null;
        }

        if (!string.Equals(registeredName, name, StringComparison.Ordinal))
            warnings.Add($"The registered name '{registeredName}' cannot be used as a file name. Exporting it as '{name}'.");

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

    /// <summary>
    /// Hashes what the road *is*, never which id Road Builder happens to have given it. Road ids are
    /// blanked out of the serialized config, so re-importing a road under a fresh id still counts as
    /// the same road and a repeated export stays a no-op.
    /// </summary>
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
