using Colossal.UI;
using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CS2Mods.Shared.Export;

internal static class RoadBuilderIconExporter
{
    /// <summary>
    /// Derived from the hosting mod's id rather than fixed, because two mods built on this code both
    /// register a host location and the name is the key: a fixed name would have them overwrite each
    /// other's directory, and either one unregistering would take the other's icons down with it.
    /// For the road exporter this evaluates to the name it has always used, so assets exported before
    /// this became shared code keep resolving their thumbnails.
    /// </summary>
    internal static string IconHost => ModHost.Id.ToLowerInvariant() + "icons";
    private const string RoadBuilderThumbnailHost = "roadbuilderthumbnails";

    /// <summary>Above this an embedded thumbnail costs more than it is worth, so fall back to a file.</summary>
    private const long MaximumEmbeddedBytes = 512 * 1024;

    internal static void RegisterHost()
    {
        ExportPaths.EnsureIconsDirectory();
        UIManager.defaultUISystem.AddHostLocation(IconHost, ExportPaths.IconsDirectory, false);
    }

    internal static void UnregisterHost()
    {
        UIManager.defaultUISystem.RemoveHostLocation(IconHost);
    }

    /// <summary>
    /// Returns the icon URI the exported road should use.
    /// With <paramref name="embed"/> the thumbnail becomes a data URI, which makes the asset
    /// self-contained: it keeps working when shared or when this mod is gone. Otherwise the file is
    /// copied next to the mod's data and served from a host this mod registers, which is smaller but
    /// only resolves on this machine.
    /// </summary>
    internal static string Preserve(RoadBuilderRoad road, ExportReport report, bool embed)
    {
        var sourceIcon = road.RegisteredIcon;
        if (string.IsNullOrWhiteSpace(sourceIcon))
        {
            report.Warning($"Road '{road.Name}' has no registered Road Builder icon.");
            return string.Empty;
        }

        if (!Uri.TryCreate(sourceIcon, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, "coui", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, RoadBuilderThumbnailHost, StringComparison.OrdinalIgnoreCase))
        {
            return sourceIcon;
        }

        if (!TryResolveLocalFile(uri, out var sourceFile))
        {
            report.Warning($"Could not read the registered icon for '{road.Name}'. Keeping its original URI '{sourceIcon}'.");
            return sourceIcon;
        }

        if (embed)
        {
            try
            {
                return Embed(sourceFile);
            }
            catch (Exception exception)
            {
                report.Warning(
                    $"Could not embed the thumbnail of '{road.Name}': {exception.Message}. "
                    + "Falling back to a copy served by this mod.");
            }
        }

        try
        {
            ExportPaths.EnsureIconsDirectory();
            var extension = Path.GetExtension(sourceFile);
            if (string.IsNullOrEmpty(extension)) extension = ".svg";
            var fileName = IconBaseName(road.Name) + extension.ToLowerInvariant();
            var targetFile = Path.Combine(ExportPaths.IconsDirectory, fileName);
            File.Copy(sourceFile, targetFile, true);
            return $"coui://{IconHost}/{fileName}";
        }
        catch (Exception exception)
        {
            report.Warning($"Could not preserve the registered icon for '{road.Name}': {exception.Message}. Keeping its original URI.");
            return sourceIcon;
        }
    }

    /// <summary>Deletes the copied thumbnail of a road whose export was removed.</summary>
    internal static void Discard(string exportName)
    {
        if (!Directory.Exists(ExportPaths.IconsDirectory)) return;
        foreach (var file in Directory.GetFiles(ExportPaths.IconsDirectory, IconBaseName(exportName) + ".*"))
            File.Delete(file);
    }

    private static string Embed(string sourceFile)
    {
        var info = new FileInfo(sourceFile);
        if (info.Length > MaximumEmbeddedBytes)
            throw new InvalidOperationException($"the thumbnail is {info.Length / 1024} KB, above the {MaximumEmbeddedBytes / 1024} KB embedding limit");

        var bytes = File.ReadAllBytes(sourceFile);
        var extension = Path.GetExtension(sourceFile).ToLowerInvariant();
        var mediaType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "image/svg+xml",
        };

        // Road Builder writes its SVGs with a UTF-8 BOM; leading bytes before the XML declaration
        // upset strict parsers, and nothing needs them once the payload is base64.
        if (mediaType == "image/svg+xml" && bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            bytes = bytes.Skip(3).ToArray();

        return "data:" + mediaType + ";base64," + Convert.ToBase64String(bytes);
    }

    private static string IconBaseName(string exportName) =>
        NameSanitizer.ShortHash("RoadBuilderIcon:" + exportName, 32);

    private static bool TryResolveLocalFile(Uri uri, out string fullPath)
    {
        fullPath = string.Empty;
        try
        {
            var resourceHandler = typeof(UISystem)
                .GetProperty("resourceHandler", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(UIManager.defaultUISystem);
            var hostLocations = typeof(DefaultResourceHandler)
                .GetField("m_HostLocationsMap", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(resourceHandler) as IDictionary<string, List<(string, int)>>;
            if (hostLocations == null || !hostLocations.TryGetValue(uri.Host, out var locations)) return false;

            var relativePath = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            if (relativePath.Length == 0) return false;

            foreach (var location in locations.OrderBy(item => item.Item2))
            {
                var root = Path.GetFullPath(location.Item1)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
                if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate)) continue;
                fullPath = candidate;
                return true;
            }
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, $"Unable to resolve Road Builder icon URI '{uri}'");
        }

        return false;
    }
}
