using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

internal static class BridgeZipExport
{
    internal static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BridgeBuilder", "Exports");

    internal static string DefaultFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).TrimEnd(' ', '.');
        return (safe.Length == 0 ? "Bridge" : safe) + ".zip";
    }

    internal static bool IsValidFolderName(string folder) =>
        !(string.IsNullOrWhiteSpace(folder) || folder.Length > 255
            || folder == "." || folder == ".."
            || folder.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || folder.Contains("/") || folder.Contains("\\") || folder.EndsWith(" ") || folder.EndsWith(".")
            || Regex.IsMatch(folder, @"^(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)", RegexOptions.IgnoreCase));

    // File-only worker. No Unity/game calls and no writes before the player's confirmation.
    // Archive entries retain source-relative paths beneath one player-selected folder.
    internal static string Write(string source, string destination, string folder)
    {
        if (!IsValidFolderName(folder))
            throw new IOException("Choose a valid single top-level folder name.");
        source = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
        destination = Path.GetFullPath(destination);
        if (!destination.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new IOException("The destination must have a .zip extension.");
        if (destination.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The destination cannot be inside the bridge directory.");
        if (File.Exists(destination)) throw new IOException("The destination already exists. Choose another filename.");
        var files = Enumerate(source).ToArray();
        if (files.Length == 0) throw new IOException("The bridge directory is empty.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                foreach (var file in files)
                {
                    var relative = file.Substring(source.Length + 1).Replace('\\', '/');
                    var entry = zip.CreateEntry(folder + "/" + relative, CompressionLevel.Optimal);
                    using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var output = entry.Open();
                    input.CopyTo(output);
                }
            File.Move(temporary, destination); // Never replace an existing player file.
            return destination;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static IEnumerable<string> Enumerate(string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Linked bridge directories cannot be exported.");
        foreach (var file in Directory.GetFiles(directory))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked bridge files cannot be exported.");
            yield return file;
        }
        foreach (var child in Directory.GetDirectories(directory))
            foreach (var file in Enumerate(child)) yield return file;
    }
}
