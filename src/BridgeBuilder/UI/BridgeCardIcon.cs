using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.UI;

namespace BridgeBuilder.UI;

internal static class BridgeCardIcon
{
    private static readonly Dictionary<string, float> Ratios = new(StringComparer.Ordinal);

    internal static float Ratio(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri)) return 0;
        if (Ratios.TryGetValue(uri, out var cached)) return cached;
        try
        {
            using var stream = Open(uri);
            if (stream != null && BridgeIconDimensions.TryRead(stream, out var ratio))
            { Ratios.Add(uri, ratio); return ratio; }
        }
        catch (Exception exception)
        { Mod.Log.Warn($"Could not read card icon dimensions for '{uri}': {exception.Message}"); }
        // Unresolved metadata is not cached: a later catalogue refresh can resolve a loaded source.
        return 0;
    }

    private static Stream? Open(string text)
    {
        if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = text.IndexOf(',');
            if (comma < 0) return null;
            var content = text.Substring(comma + 1);
            return new MemoryStream(text.Substring(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase)
                ? Convert.FromBase64String(content) : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(content)));
        }
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            uri = new Uri("assetdb://gameui/" + text.TrimStart('/'));
        // Same registered-host resolution used by the shared Road Builder icon exporter.
        var handler = typeof(UISystem).GetProperty("resourceHandler", BindingFlags.Instance | BindingFlags.Public)
            !.GetValue(UIManager.defaultUISystem);
        if (uri.Scheme.Equals("assetdb", StringComparison.OrdinalIgnoreCase))
        {
            var databases = (IDictionary<string, List<(Uri, int)>>)typeof(DefaultResourceHandler).GetProperty("DatabaseHostLocationsMap", BindingFlags.Instance | BindingFlags.NonPublic)
                !.GetValue(handler)!;
            // Native Media/... URLs use gameui.uiHost (scheme assetdb), not a local coui host.
            // Preserve the native handler's priority order when resolving those aliases.
            if (databases.TryGetValue(uri.Host, out var roots))
            {
                foreach (var entry in roots)
                {
                    var candidate = new UriBuilder(uri) { Scheme = entry.Item1.Scheme, Host = entry.Item1.Host,
                        Path = entry.Item1.AbsolutePath + uri.AbsolutePath.TrimStart('/') };
                    if (AssetDatabase.global.TryGetAsset(candidate.Uri.AbsoluteUri, out AssetData source)) return source.GetReadStream();
                }
                return null;
            }
            return AssetDatabase.global.TryGetAsset(text, out AssetData asset) ? asset.GetReadStream() : null;
        }
        var hosts = (IDictionary<string, List<(string, int)>>)typeof(DefaultResourceHandler).GetField("m_HostLocationsMap", BindingFlags.Instance | BindingFlags.NonPublic)
            !.GetValue(handler)!;
        if (!hosts.TryGetValue(uri.Host, out var locations)) return null;
        var relative = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        foreach (var location in locations.OrderBy(item => item.Item2))
        {
            var root = Path.GetFullPath(location.Item1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(root, relative));
            if (candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                return File.OpenRead(candidate);
        }
        return null;
    }
}
