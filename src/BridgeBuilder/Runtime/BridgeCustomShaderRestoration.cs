using System;
using System.IO;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Runtime;

internal static class BridgeCustomShaderRestoration
{
    // Caller must run on the main thread before native mesh/material realization.
    // This restores registration; it does not mutate an already cached fallback.
    internal static bool TryRestore(out int restored, out string? error)
    {
        restored = 0; error = null;
        try
        {
            Bridges.MovableCustomShaderAsset.TryRegisterCurrent(out error);
            foreach (var asset in AssetDatabase.global.GetAssets(default(SearchFilter<FileAsset>)))
            {
                string path = asset.path.Replace('\\', '/');
                if (!BridgeAssetInfo.TryFileOwner(path, out _) ||
                    !string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "MaterialPrograms", StringComparison.OrdinalIgnoreCase) ||
                    !path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                using var input = asset.GetReadStream();
                bool complete = BridgeCustomShaderPackage.TryReadConfigured(input, out var sourceTemplate, out var package, out var queues, out var cause);
                if (complete)
                {
                    var library = AssetDatabase.global.resources.materialLibrary;
                    complete = BridgeCustomShaderRegistry.TryRegisterPackage(library, sourceTemplate, package, out _, out cause);
                    foreach (int queue in queues)
                        if (!BridgeCustomShaderRegistry.TryRegisterConfiguredPackage(library, sourceTemplate, package, queue, out _, out cause))
                        { complete = false; break; }
                }
                if (!complete)
                {
                    error = (error == null ? "" : error + "\n") + "Generated material restore: " + path + "; " + cause;
                    continue;
                }
                restored++;
                }
                catch (Exception exception)
                { error = (error == null ? "" : error + "\n") + "Generated material restore: " + path + "; " + exception.Message; }
            }
            return error == null;
        }
        catch (Exception exception)
        { error = (error == null ? "" : error + "\n") + "Generated material restoration failed: " + exception.Message; return false; }
    }
}
