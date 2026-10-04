using System.IO;
using UnityEngine;

namespace CS2Mods.Shared.Infrastructure;

internal static class ExportPaths
{
    internal static string DataDirectory => Path.Combine(Application.persistentDataPath, "ModsData", ModHost.Id);
    internal static string SettingsFile => Path.Combine(DataDirectory, "settings.ini");
    internal static string RequestFile => Path.Combine(DataDirectory, "export.request");
    internal static string ForceRequestFile => Path.Combine(DataDirectory, "force-export.request");
    internal static string StateFile => Path.Combine(DataDirectory, "export-state.tsv");
    internal static string ReportFile => Path.Combine(DataDirectory, "last-export-report.txt");

    /// <summary>
    /// Where measurements taken from the running game are written down.
    ///
    /// Separate from the report because it outlives it: the report describes one export and is
    /// overwritten by the next, while this is source data on its way into the code. Numbers that only
    /// exist in a log line get read once and retyped from memory, which is how a tower ended up
    /// recorded at another tower's width.
    /// </summary>
    internal static string MeasurementsFile => Path.Combine(DataDirectory, "tower-measurements.txt");

    internal static string IconsDirectory => Path.Combine(DataDirectory, "Icons");

    internal static void EnsureDataDirectory() => Directory.CreateDirectory(DataDirectory);
    internal static void EnsureIconsDirectory() => Directory.CreateDirectory(IconsDirectory);
}
