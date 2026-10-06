using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>Session bookkeeping for file migration; never inspects loaded assets.</summary>
internal static class BridgeSessionState
{
    private static readonly Regex Identity = new(
        @"(?:^|[ _-])(b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?=$|[ _-])");
    internal static bool RestartRequired { get; private set; }
    internal static void RequireRestart() => RestartRequired = true;
    internal static void Stop() => RestartRequired = false;
    internal static bool TryOwner(string name, out string bridge)
    {
        var match = Identity.Match(name ?? string.Empty);
        bridge = match.Success ? match.Groups[1].Value : string.Empty;
        return BridgeAssetInfo.IsPrefabName(bridge);
    }
}
