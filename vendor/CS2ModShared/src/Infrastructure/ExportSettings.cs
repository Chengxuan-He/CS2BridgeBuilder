using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CS2Mods.Shared.Infrastructure;

/// <summary>
/// Advanced knobs that have no place in the options page. Everything the player operates while
/// exporting lives in <see cref="Settings.ExporterSetting"/> instead.
/// </summary>
internal sealed class ExportSettings
{
    internal int QuietFrames { get; private set; } = 180;
    internal int MaxWaitFrames { get; private set; } = 3600;
    internal string NamePrefix { get; private set; } = ModHost.DefaultNamePrefix;

    internal static ExportSettings Load()
    {
        ExportPaths.EnsureDataDirectory();
        var result = new ExportSettings();
        if (!File.Exists(ExportPaths.SettingsFile))
        {
            result.WriteDefaults();
            return result;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(ExportPaths.SettingsFile))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
            var separator = line.IndexOf('=');
            if (separator <= 0) continue;
            values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
        }

        result.QuietFrames = ReadInteger(values, nameof(QuietFrames), result.QuietFrames, 30, 3600);
        result.MaxWaitFrames = ReadInteger(values, nameof(MaxWaitFrames), result.MaxWaitFrames, result.QuietFrames, 36000);
        if (values.TryGetValue(nameof(NamePrefix), out var prefix) && !string.IsNullOrWhiteSpace(prefix))
            result.NamePrefix = prefix.Trim();
        return result;
    }

    private static int ReadInteger(IReadOnlyDictionary<string, string> values, string key, int fallback, int minimum, int maximum)
    {
        if (!values.TryGetValue(key, out var value)
            || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return fallback;
        return Math.Max(minimum, Math.Min(maximum, parsed));
    }

    private void WriteDefaults() => File.WriteAllText(
        ExportPaths.SettingsFile,
        "# " + ModHost.DisplayName + " advanced settings.\r\n"
        + "# Selection, export and removal live in Options -> Mods.\r\n"
        + "QuietFrames=180\r\nMaxWaitFrames=3600\r\nNamePrefix=" + ModHost.DefaultNamePrefix + "\r\n");
}
