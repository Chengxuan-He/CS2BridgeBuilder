using System;
using System.IO;
using CS2Mods.Shared.Infrastructure;
namespace BridgeBuilder.Runtime;
internal static class BridgeInspectionRecoveryCopies
{
    private static string LegacyPath => Path.Combine(ExportPaths.DataDirectory, "RemovedBridgeFiles");
    internal static void Run()
    {
        try
        {
            if (!Directory.Exists(LegacyPath)) return;
            // Move the actual backup payload across mappings/volumes; never overwrite existing copies.
            Directory.CreateDirectory(Settings.BridgeRecoveryLocation.DefaultPath);
            var target = System.IO.Path.Combine(Settings.BridgeRecoveryLocation.DefaultPath, "Legacy-" + Guid.NewGuid().ToString("N"));
            BridgeFileAccess.MovePayload(LegacyPath, target);
            Mod.Log.Info("Moved legacy recovery copies outside game asset discovery: " + target);
        }
        catch (Exception exception) when (exception is not UnauthorizedAccessException)
        {
            Mod.Log.Warn(exception, "Could not relocate legacy recovery copies; assetInfo guard excludes them.");
        }
    }

}
