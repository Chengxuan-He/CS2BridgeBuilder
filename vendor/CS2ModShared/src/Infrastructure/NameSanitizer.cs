using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CS2Mods.Shared.Infrastructure;

internal static class NameSanitizer
{
    /// <summary>
    /// The exported prefab name doubles as a directory and file name, so the path budget is spent
    /// twice. Well below MAX_PATH once the user data folder is accounted for.
    /// </summary>
    private const int MaximumRoadNameLength = 96;

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    internal static string CreateDependencyName(string prefix, string typeName, string sourceName) =>
        CreateName(prefix + "Dep", typeName + "_" + sourceName, typeName + ":" + sourceName, 112);

    internal static string ShortHash(string value, int length = 10)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
        var hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        return hex.Substring(0, Math.Min(length, hex.Length));
    }

    /// <summary>
    /// Keeps the road's own name readable - non-ASCII is fine on every platform the game ships on -
    /// while removing everything Windows refuses in a path component. Returns an empty string when
    /// nothing usable is left, so the caller can fall back to a generated name.
    /// </summary>
    internal static string MakeFileSystemSafe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
        var builder = new StringBuilder(value!.Length);
        var previousUnderscore = false;
        foreach (var character in value.Trim())
        {
            if (invalid.Contains(character) || char.IsControl(character))
            {
                // Collapse runs so "A//B" does not become "A__B".
                if (!previousUnderscore && builder.Length > 0) builder.Append('_');
                previousUnderscore = true;
                continue;
            }

            builder.Append(character);
            previousUnderscore = false;
        }

        // Windows silently strips trailing dots and spaces from path components, which would make the
        // name on disk differ from the prefab name and break the lookup on the next run.
        var result = builder.ToString().Trim().TrimEnd('.', ' ', '_').TrimStart('_');
        if (result.Length > MaximumRoadNameLength)
            result = result.Substring(0, MaximumRoadNameLength).Trim().TrimEnd('.', ' ', '_');

        var withoutExtension = result.IndexOf('.') >= 0 ? result.Substring(0, result.IndexOf('.')) : result;
        if (ReservedDeviceNames.Contains(withoutExtension)) result += "_";
        return result;
    }

    private static string CreateName(string prefix, string humanPart, string stableIdentity, int maximumLength)
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
        var builder = new StringBuilder(humanPart.Length);
        var previousSeparator = false;
        foreach (var character in humanPart.Normalize(NormalizationForm.FormKC))
        {
            var separator = invalid.Contains(character) || char.IsWhiteSpace(character) || character == '-';
            if (separator)
            {
                if (!previousSeparator && builder.Length > 0) builder.Append('_');
                previousSeparator = true;
                continue;
            }
            builder.Append(character);
            previousSeparator = false;
        }

        var safeHumanPart = builder.ToString().Trim('_', '.', ' ');
        if (safeHumanPart.Length == 0) safeHumanPart = "Road";
        var suffix = "_" + ShortHash(stableIdentity);
        var safePrefix = new string(prefix.Where(c => !invalid.Contains(c) && !char.IsWhiteSpace(c)).ToArray());
        if (safePrefix.Length == 0) safePrefix = "RBExport";
        var available = Math.Max(8, maximumLength - safePrefix.Length - suffix.Length - 1);
        if (safeHumanPart.Length > available) safeHumanPart = safeHumanPart.Substring(0, available).TrimEnd('_', '.', ' ');
        return safePrefix + "_" + safeHumanPart + suffix;
    }
}
