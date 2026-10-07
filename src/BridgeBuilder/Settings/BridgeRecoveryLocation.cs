using CS2Mods.Shared.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BridgeBuilder.Settings;

internal static class BridgeRecoveryLocation
{
    internal static string LegacyPath => System.IO.Path.Combine(ExportPaths.DataDirectory, "RemovedBridgeFiles");

    // User asset discovery recursively scans persistentDataPath, including ModsData.
    // Original-format backups must live OUTSIDE that tree.
    internal static string DefaultPath
    {
        get
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                UnityEngine.Application.persistentDataPath.TrimEnd('/', '\\'))!, "BridgeBuilder Backups");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return root.Replace('/', '\\');
            return root.Replace('\\', '/');
        }
    }
    internal static string Path => Mod.Setting?.RecoveryCopyLocation ?? DefaultPath;

    internal static bool TryNormalize(string value, out string path)
    {
        path = DefaultPath;
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            // Reject relative paths: their meaning would change with the game's working directory.
            if (!System.IO.Path.IsPathFullyQualified(value))
                return false;
            path = System.IO.Path.GetFullPath(value);
            if (string.Equals(path.TrimEnd('/', '\\'), System.IO.Path.GetFullPath(LegacyPath).TrimEnd('/', '\\'),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            { path = DefaultPath; return true; } // persisted default from older releases
            if (Within(path, UnityEngine.Application.persistentDataPath)) return false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                path = path.Replace('/', '\\');
            return !File.Exists(path);
        }
        catch (Exception exception)
        {
            Mod.Log.Warn("Invalid bridge recovery directory: " + exception.Message);
            return false;
        }
    }

    private static bool Within(string path, string root)
    {
        var full = System.IO.Path.GetFullPath(Runtime.BridgeFileAccess.Logical(path));
        root = System.IO.Path.GetFullPath(root).TrimEnd('/', '\\');
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return full.Equals(root, comparison)
            || full.StartsWith(root + System.IO.Path.DirectorySeparatorChar, comparison);
    }

    internal static void MigrateLegacy()
    {
        try
        {
            if (!Directory.Exists(LegacyPath)) return;
            // Same-volume rename of the entire backup tree; never overwrite existing copies.
            Directory.CreateDirectory(DefaultPath);
            var target = System.IO.Path.Combine(DefaultPath, "Legacy-" + Guid.NewGuid().ToString("N"));
            Directory.Move(LegacyPath, target);
            Mod.Log.Info("Moved legacy recovery copies outside game asset discovery: " + target);
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not relocate legacy recovery copies; assetInfo guard excludes them.");
        }
    }

    internal static void Open()
    {
        try
        {
            Directory.CreateDirectory(Path);
            Process.Start(new ProcessStartInfo { FileName = Path, UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Mod.Log.Warn("Could not open bridge recovery directory: " + exception.Message);
            Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("RecoveryOpenFailed"));
        }
    }
}
