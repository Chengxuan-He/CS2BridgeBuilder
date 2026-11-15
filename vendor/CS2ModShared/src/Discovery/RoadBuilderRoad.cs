using Game.Prefabs;
using System.Collections.Generic;

namespace CS2Mods.Shared.Discovery;

/// <summary>A Road Builder source with separate registered identity and display label.</summary>
internal sealed class RoadBuilderRoad
{
    internal RoadBuilderRoad(
        RoadPrefab prefab,
        string name,
        string registeredIcon,
        string fingerprint,
        float width,
        float configuredSpeedLimit,
        IReadOnlyList<string> modules)
    {
        Prefab = prefab;
        Name = name;
        RegisteredIcon = registeredIcon;
        Fingerprint = fingerprint;
        Width = width;
        ConfiguredSpeedLimit = configuredSpeedLimit;
        Modules = modules;
    }

    /// <summary>Total of the lane widths in the Road Builder configuration, in metres. Zero if unknown.</summary>
    internal float Width { get; }

    /// <summary>
    /// The speed limit as set in Road Builder, in the same unit the prefab field uses. Zero when the
    /// configuration could not be read, which means "do not override whatever the prefab says".
    /// </summary>
    internal float ConfiguredSpeedLimit { get; }

    /// <summary>One line per lane group, in order across the road. Empty when the configuration is unreadable.</summary>
    internal IReadOnlyList<string> Modules { get; }

    internal RoadPrefab Prefab { get; }

    /// <summary>Registered prefab identifier; independent of the editable label.</summary>
    internal string Id => Prefab.name;

    /// <summary>Display label only.</summary>
    internal string Name { get; }

    internal string RegisteredIcon { get; }

    /// <summary>Content hash. Changes when the road changes, so a re-export overwrites.</summary>
    internal string Fingerprint { get; }
}
