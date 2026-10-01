using System;
using System.IO;

namespace BridgeBuilder.Runtime;

// Keep logical paths unprefixed for ownership comparisons; use extended paths only at the IO boundary.
// File.Exists/Directory.Exists hide access errors, which must never authorize asset retirement.
internal static class BridgeFileAccess
{
    internal static string Logical(string path) => path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)
        ? @"\\" + path.Substring(8) : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path.Substring(4) : path;
    internal static string Native(string path)
    {
        var full = Path.GetFullPath(path);
        if (Path.DirectorySeparatorChar != '\\' || full.StartsWith(@"\\?\", StringComparison.Ordinal)) return full;
        return full.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + full.Substring(2) : @"\\?\" + full;
    }

    internal static bool Exists(string path)
    {
        try { File.GetAttributes(Native(path)); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }
    internal static FileAttributes Attributes(string path) => File.GetAttributes(Native(path));
    internal static string ReadText(string path) => File.ReadAllText(Native(path));
    internal static FileStream OpenRead(string path) => File.OpenRead(Native(path));
}
