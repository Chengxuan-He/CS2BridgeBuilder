using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

internal static class BridgePresentationIcons
{
    private static readonly HashSet<string> Available = new(StringComparer.Ordinal);
    private static readonly Regex References = new("coui://bridgebuilder[^\"\\s\\\\]*", RegexOptions.IgnoreCase);

    // Called once at the same IO boundary that registers the packaged UI host.
    internal static void Register(string directory)
    {
        Available.Clear();
        foreach (var name in new[] { "BridgeNanpu.svg", "BridgeLupu.svg" })
            if (File.Exists(Path.Combine(directory, name)))
                Available.Add("coui://bridgebuilderui/" + name);
    }

    internal static void Clear() => Available.Clear();

    internal static bool Unavailable(string? icon) => string.IsNullOrWhiteSpace(icon)
        || (icon!.IndexOf("coui://bridgebuilder", StringComparison.OrdinalIgnoreCase) >= 0
            && !Available.Contains(icon));

    internal static bool HasUnavailableReference(string text)
    {
        foreach (Match match in References.Matches(text))
            if (Unavailable(match.Value)) return true;
        return false;
    }
}
