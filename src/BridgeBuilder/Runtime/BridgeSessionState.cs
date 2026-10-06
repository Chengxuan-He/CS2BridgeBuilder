

namespace BridgeBuilder.Runtime;

/// <summary>Session bookkeeping for file migration; never inspects loaded assets.</summary>
internal static class BridgeSessionState
{
    internal static bool RestartRequired { get; private set; }
    internal static void RequireRestart() => RestartRequired = true;
    internal static void Stop() => RestartRequired = false;
    internal static bool TryOwner(string name, out string bridge)
    {
        return BridgeAssetInfo.TryFileOwner(name, out bridge);
    }
}
