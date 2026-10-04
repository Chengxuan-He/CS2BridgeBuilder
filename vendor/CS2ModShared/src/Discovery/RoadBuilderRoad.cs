using Game.Prefabs;
using System.Collections.Generic;

namespace CS2Mods.Shared.Discovery;

/// <summary>
/// A Road Builder road that can be exported.
/// Identity is <see cref="Name"/> - the sanitized registered name, which is also the name of the
/// asset on disk. Road Builder's registration id is deliberately not part of it: the exported asset
/// and the export state must stay meaningful once Road Builder is gone, and a re-issued id must not
/// make an already exported road look new.
/// </summary>
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

    /// <summary>Identity: shown in the UI and used as the exported asset's name.</summary>
    internal string Name { get; }

    internal string RegisteredIcon { get; }

    /// <summary>Content hash. Changes when the road changes, so a re-export overwrites.</summary>
    internal string Fingerprint { get; }
}
