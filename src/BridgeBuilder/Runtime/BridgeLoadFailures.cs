using Colossal.Logging;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>Observe native per-prefab failures; never mutate ECS from a logging callback.</summary>
internal static class BridgeLoadFailures
{
    private static readonly object Sync = new();
    private static readonly HashSet<PrefabBase> Failed = new(
        CS2Mods.Shared.Infrastructure.ReferenceEqualityComparer<PrefabBase>.Instance);
    private static readonly HashSet<PrefabBase> Quarantined = new(
        CS2Mods.Shared.Infrastructure.ReferenceEqualityComparer<PrefabBase>.Instance);
    private static readonly Regex Identity = new(
        @"(?:^|[ _-])(b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?=$|[ _-])");
    private static int _revision;
    internal static int Revision { get { lock (Sync) return _revision; } }

    internal static void Start()
    {
        UnityLogger.OnErrorOrHigher -= OnError;
        UnityLogger.OnErrorOrHigher += OnError;
    }

    internal static void Stop()
    {
        UnityLogger.OnErrorOrHigher -= OnError;
        lock (Sync) Quarantined.Clear();
        Clear();
    }

    internal static void Quarantine(PrefabBase prefab, string reason)
    {
        lock (Sync)
        {
            if (!Quarantined.Add(prefab)) return;
            _revision++;
        }
        Mod.Log.Warn($"Quarantined bridge before native prefab initialization: '{prefab.name}': {reason}");
    }

    internal static void Clear()
    {
        lock (Sync) { Failed.Clear(); _revision++; }
    }

    private static void OnError(ILog log, Level level, string message, Exception exception,
        UnityEngine.Object context)
    {
        // PrefabSystem / PrefabInitializeSystem catch these exceptions internally.
        // A finalizer on their public update method would never see them.
        if (exception == null || context is not PrefabBase prefab
            || !(message.StartsWith("Error when initializing prefab:", StringComparison.Ordinal)
                || message.StartsWith("Error when adding prefab:", StringComparison.Ordinal)
                || message.StartsWith("Error when updating prefab:", StringComparison.Ordinal))) return;
        lock (Sync) if (Failed.Add(prefab)) _revision++;
    }

    internal static PrefabBase[] Prefabs()
    {
        // Resolve Unity names and ownership only on the game thread. Never use the
        // message text or an arbitrary UUID in another mod's error as deletion authority.
        PrefabBase[] snapshot;
        // Registration happens before map preload too. Keep quarantined object identities
        // across Clear() so unregistered assets remain reachable for post-load revalidation.
        lock (Sync) snapshot = Failed.Concat(Quarantined).Distinct().ToArray();
        return snapshot.Where(p => p != null && !p.isBuiltin && !p.isReadOnly
            && TryOwner(p.name, out _)).ToArray();
    }

    internal static bool TryOwner(string name, out string bridge)
    {
        var match = Identity.Match(name ?? string.Empty);
        bridge = match.Success ? match.Groups[1].Value : string.Empty;
        return BridgeRegistration.IsPrefabName(bridge);
    }
}
