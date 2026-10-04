using Colossal.Logging;
using CS2Mods.Shared.Discovery;
using System;

namespace CS2Mods.Shared;

/// <summary>
/// The one thing the shared code needs from whichever mod is hosting it: an identity, a log and a
/// way to ask the options page to rebuild itself.
/// Deliberately a static shim rather than an injected service. The shared code is compiled straight
/// into each mod assembly, so there is exactly one host per assembly and no chance of two competing
/// registrations; passing a context object through every call site would buy nothing.
/// </summary>
internal static class ModHost
{
    private static ILog? _log;

    /// <summary>The hosting mod's id. Names its data directory and its report file.</summary>
    internal static string Id { get; private set; } = "CS2Mod";

    /// <summary>The hosting mod's name as the player sees it. Used in generated files, not in the UI.</summary>
    internal static string DisplayName { get; private set; } = "CS2 Mod";

    /// <summary>
    /// Never null. Falls back to a logger of its own so that shared code called before
    /// <see cref="Initialize"/> - or from a unit test - still logs instead of throwing.
    /// </summary>
    internal static ILog Log => _log ??= LogManager.GetLogger(Id);

    /// <summary>Set by the host to <c>Mod.RebuildOptionsPage</c>. Null until the page exists.</summary>
    internal static Action? PageRebuilder { get; set; }


    /// <summary>
    /// What an exported asset is called, given the road it came from. Defaults to the road's own
    /// name. The bridge exporter overrides it so that a road and the bridge made from that road can
    /// both exist in the asset database, and so that the same road can be exported in more than one
    /// bridge style.
    /// </summary>
    internal static Func<RoadBuilderRoad, string>? ExportNamePolicy { get; set; }

    internal static string ExportNameOf(RoadBuilderRoad road) =>
        ExportNamePolicy?.Invoke(road) ?? road.Name;

    /// <summary>Prefix for the generated dependency assets. Keeps two mods' dependencies apart.</summary>
    internal static string DefaultNamePrefix { get; set; } = "RBExport";

    internal static void Initialize(string id, string displayName, ILog log)
    {
        Id = id;
        DisplayName = displayName;
        _log = log;
    }

    internal static void RebuildOptionsPage()
    {
        try
        {
            PageRebuilder?.Invoke();
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not rebuild the options page");
        }
    }
}
