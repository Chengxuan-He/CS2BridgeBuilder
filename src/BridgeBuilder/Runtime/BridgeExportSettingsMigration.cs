using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Runtime;

// Only self-check interprets the INI produced by published releases. Normal readers use JSON.
internal static class BridgeExportSettingsMigration
{
    internal static bool Run(out bool changed, out string error)
    {
        changed = false;
        error = "";
        try
        {
            if (File.Exists(ExportPaths.SettingsFile)) return true;
            var original = Path.Combine(ExportPaths.DataDirectory, "settings.ini");
            if (!File.Exists(original)) return true;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawLine in File.ReadAllLines(original))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                var separator = line.IndexOf('=');
                if (separator <= 0) continue;
                values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }
            var quiet = Integer(values, "QuietFrames", 180, 30, 3600);
            var wait = Integer(values, "MaxWaitFrames", 3600, quiet, 36000);
            var prefix = values.TryGetValue("NamePrefix", out var text) && !string.IsNullOrWhiteSpace(text)
                ? text.Trim() : ModHost.DefaultNamePrefix;
            ExportSettings.SaveCurrent(quiet, wait, prefix);
            // The original is never changed or removed; the current file makes subsequent runs no-ops.
            changed = true;
            return true;
        }
        catch (Exception exception) when (exception is not UnauthorizedAccessException)
        {
            error = "Export settings migration: " + exception.Message;
            return false;
        }
    }

    private static int Integer(IReadOnlyDictionary<string, string> values, string key,
        int fallback, int minimum, int maximum)
    {
        if (!values.TryGetValue(key, out var value)
            || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return fallback;
        return Math.Max(minimum, Math.Min(maximum, parsed));
    }
}
