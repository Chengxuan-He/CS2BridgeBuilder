using System;
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
        // Do not create defaults before self-check has had a chance to migrate published settings.
        if (!File.Exists(ExportPaths.SettingsFile)) return new ExportSettings();
        var data = JsonConfigurationFile.Read<Data>(ExportPaths.SettingsFile);
        return new ExportSettings { QuietFrames = data.QuietFrames, MaxWaitFrames = data.MaxWaitFrames,
            NamePrefix = data.NamePrefix };
    }

    internal static void SaveCurrent(int quietFrames, int maxWaitFrames, string namePrefix)
    {
        ExportPaths.EnsureDataDirectory();
        JsonConfigurationFile.Write(ExportPaths.SettingsFile, new Data {
            QuietFrames = quietFrames, MaxWaitFrames = maxWaitFrames, NamePrefix = namePrefix });
    }

    internal sealed class Data
    {
        public int QuietFrames { get; set; } = 180;
        public int MaxWaitFrames { get; set; } = 3600;
        public string NamePrefix { get; set; } = ModHost.DefaultNamePrefix;
        public Data() { }
    }
}
