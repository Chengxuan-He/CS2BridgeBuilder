using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Stores bridges created by the runtime UI. The file is keyed only by the immutable prefab name;
/// registration/display names are payload and are intentionally allowed to be duplicated.
/// </summary>
internal static class BridgeRegistrationStore
{
    private const string Header =
        "prefabName\tregistrationNameBase64\tupperDeckIdBase64\tlowerDeckIdBase64\tstyleIdBase64\tcreatedUtc";
    private static readonly object Gate = new();

    internal static string FilePath => Path.Combine(ExportPaths.DataDirectory, "bridge-registry.tsv");

    internal static IReadOnlyList<BridgeRegistration> Load()
    {
        lock (Gate)
        {
            return LoadUnsafe()
                .OrderBy(item => item.RegistrationName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.PrefabName, StringComparer.Ordinal)
                .ToList();
        }
    }

    internal static BridgeRegistration? Find(string? prefabName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName)) return null;
        lock (Gate)
        {
            return LoadUnsafe().FirstOrDefault(item =>
                string.Equals(item.PrefabName, prefabName, StringComparison.Ordinal));
        }
    }

    internal static bool Record(BridgeRegistration registration)
    {
        if (!BridgeRegistration.IsPrefabName(registration.PrefabName)) return false;
        lock (Gate)
        {
            var entries = LoadUnsafe();
            var index = entries.FindIndex(item =>
                string.Equals(item.PrefabName, registration.PrefabName, StringComparison.Ordinal));
            if (index >= 0) entries[index] = registration;
            else entries.Add(registration);
            return SaveUnsafe(entries);
        }
    }

    internal static bool Rename(string prefabName, string registrationName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName) || string.IsNullOrWhiteSpace(registrationName))
            return false;

        lock (Gate)
        {
            var entries = LoadUnsafe();
            var entry = entries.FirstOrDefault(item =>
                string.Equals(item.PrefabName, prefabName, StringComparison.Ordinal));
            if (entry == null) return false;
            entry.RegistrationName = registrationName.Trim();
            return SaveUnsafe(entries);
        }
    }

    internal static bool Remove(string prefabName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName)) return false;
        lock (Gate)
        {
            var entries = LoadUnsafe();
            var removed = entries.RemoveAll(item =>
                string.Equals(item.PrefabName, prefabName, StringComparison.Ordinal));
            return removed > 0 && SaveUnsafe(entries);
        }
    }

    private static List<BridgeRegistration> LoadUnsafe()
    {
        var entries = new List<BridgeRegistration>();
        try
        {
            ExportPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return entries;
            var lines = File.ReadAllLines(FilePath);
            foreach (var line in lines.Skip(1))
            {
                var fields = line.Split('\t');
                if (fields.Length < 6 || !BridgeRegistration.IsPrefabName(fields[0])) continue;
                var name = Decode(fields[1]);
                var upper = Decode(fields[2]);
                var lower = Decode(fields[3]);
                var style = Decode(fields[4]);
                if (string.IsNullOrWhiteSpace(name)
                    || string.IsNullOrWhiteSpace(upper)
                    || string.IsNullOrWhiteSpace(style))
                    continue;

                entries.Add(new BridgeRegistration(
                    fields[0], name, upper, lower.Length == 0 ? null : lower, style, fields[5]));
            }
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not read the runtime bridge registry");
        }

        return entries;
    }

    private static bool SaveUnsafe(IEnumerable<BridgeRegistration> registrations)
    {
        try
        {
            ExportPaths.EnsureDataDirectory();
            var lines = new List<string> { Header };
            lines.AddRange(registrations
                .OrderBy(item => item.PrefabName, StringComparer.Ordinal)
                .Select(item => string.Join("\t", new[]
                {
                    item.PrefabName,
                    Encode(item.RegistrationName),
                    Encode(item.UpperDeckId),
                    Encode(item.LowerDeckId ?? string.Empty),
                    Encode(item.StyleId),
                    item.CreatedUtc.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '),
                })));
            File.WriteAllLines(FilePath, lines);
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not save the runtime bridge registry");
            return false;
        }
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
