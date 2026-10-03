using CS2Mods.Shared.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BridgeBuilder.Settings;

internal static class BridgeRecoveryLocation
{
    // Keep the existing root so recovery copies from earlier versions remain accessible.
    internal static string DefaultPath
    {
        get
        {
            var root = ExportPaths.DataDirectory;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return root.Replace('/', '\\').TrimEnd('\\') + "\\RemovedBridgeFiles";
            return root.Replace('\\', '/').TrimEnd('/') + "/RemovedBridgeFiles";
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
