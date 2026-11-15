using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Runtime;

internal static class BridgeCustomShaderRestoration
{
    // Main thread, before native material realization. Only the mod's current programs are loaded.
    internal static bool TryRestore(out int restored, out string? error)
    {
        restored = 0; error = null;
        var failures = new List<string>();
        try
        {
            if (!Bridges.MovableCustomShaderAsset.TryRegisterCurrent(out var initialError)) Add(initialError ?? "Current programs unavailable.");
            var seen = new HashSet<(int Source, int Queue)>();
            foreach (var asset in AssetDatabase.global.GetAssets(default(SearchFilter<SurfaceAsset>)).ToArray())
            {
                if (!BridgeAssetInfo.TryFileOwner(asset.path, out _)) continue;
                try
                {
                    using var input = asset.GetReadStream();
                    using var copy = new MemoryStream(); input.CopyTo(copy);
                    if (!BridgeCurrentProgramSurface.TryReadSurfaceIdentity(copy.ToArray(), out int template,
                        out var queue, out _, out var cause))
                    { Add(asset.path + "; " + cause); continue; }
                    if (!queue.HasValue || !BridgeNativeMaterialProgram.TryIdentify(template, queue.Value, out int source)) continue;
                    if (!seen.Add((source, queue.Value))) continue;
                    if (!Bridges.MovableCustomShaderAsset.TryPrepare(source, queue.Value, out _, out cause))
                    { Add(asset.path + "; " + cause); continue; }
                    restored++;
                }
                catch (Exception exception) when (exception is not UnauthorizedAccessException)
                { Add(asset.path + "; " + exception.Message); }
            }
        }
        catch (Exception exception) when (exception is not UnauthorizedAccessException)
        { Add(exception.Message); }
        error = failures.Count == 0 ? null : string.Join("\n", failures);
        return error == null;

        void Add(string cause) => failures.Add("Mod material registration: " + cause);
    }
}
