using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BridgeBuilder.Settings;

internal static class BridgeRecoveryLocation
{
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
        return Runtime.BridgeFileAccess.WithinAssetStorage(path, root);
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
