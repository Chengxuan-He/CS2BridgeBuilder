using System;
using System.Collections.Generic;

namespace BridgeBuilder.Runtime;

/// <summary>Runs once at the earliest mod callback, before owned prefab assetInfo.</summary>
internal static class BridgeStartupRecovery
{
    private static bool _started;
    private static bool _complete;
    private static bool _running;
    // Keep retired identities across city transitions: live prefab indices cannot be
    // removed safely, so a save can still resolve to their in-memory objects this session.
    internal static readonly HashSet<string> Retired = new(StringComparer.Ordinal);
    internal static void Start(bool finalAttempt = false)
    {
        if (_complete || _running || (_started && !finalAttempt)) return;
        _started = true; // deserialization can re-enter the component callback
        _running = true;
        try
        {
            if (!BridgeUnlockMigration.Run(UnityEngine.Application.persistentDataPath, true,
                out var changed, out var error))
            {
                // Access errors/unknown policies are not damage evidence.
                // The first callback may run while Odin still holds a read handle. Retry at
                // OnLoad, after import has closed that stream; do not poison a successful retry.
                if (finalAttempt) BridgeSessionState.RequireRestart();
                Mod.Log.Warn("Startup bridge migration deferred; files retained: " + error);
                return;
            }
            _complete = true;
            Mod.Log.Info($"Startup bridge recovery: migrated {changed} legacy unlock file(s) with backups. "
                + "Loaded prefab objects are never inspected or repaired.");
        }
        catch (Exception exception)
        {
            if (finalAttempt) BridgeSessionState.RequireRestart();
            Mod.Log.Warn("Startup bridge recovery unavailable; files retained: " + exception.Message);
        }
        finally { _running = false; }
    }

    internal static void Stop()
    {
        _started = false; _complete = false; _running = false; Retired.Clear();
        BridgeAssetCatalog.ResetSession();
    }
}
