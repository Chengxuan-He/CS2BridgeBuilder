using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BridgeBuilder.Runtime;

/// <summary>Atomic UUID registry and creation journal. Unreadable input never authorizes a write.</summary>
internal static class BridgeRegistrationStore
{
    private const string LegacyHeader =
        "prefabName\tregistrationNameBase64\tupperDeckIdBase64\tlowerDeckIdBase64\tstyleIdBase64\tcreatedUtc";
    private const string Header = LegacyHeader + "\tstatus";
    private static readonly object Gate = new();
    private static readonly UTF8Encoding Utf8 = new(false, true);
    // The registration hook can run before ModHost.Initialize.
    internal static string FilePath => Path.Combine(UnityEngine.Application.persistentDataPath,
        "ModsData", "BridgeBuilder", "bridge-registry.tsv");
    private static readonly HashSet<string> Active = new(StringComparer.Ordinal);
    private static HashSet<string>? _blockedAtBoot;

    internal static bool CanRegister(string owner)
    {
        lock (Gate)
        {
            if (Active.Contains(owner)) return true;
            if (_blockedAtBoot == null)
            {
                if (!TryLoadUnsafe(out var entries)) return false;
                _blockedAtBoot = new HashSet<string>(entries.Where(e => e.Pending).Select(e => e.PrefabName), StringComparer.Ordinal);
            }
            return !_blockedAtBoot.Contains(owner);
        }
    }

    internal static void EndCreation(string owner)
    {
        lock (Gate)
        {
            Active.Remove(owner);
            _blockedAtBoot = null; // Reload all durable pending owners, including earlier failures.
        }
    }

    internal static void ResetSession()
    {
        lock (Gate) { Active.Clear(); _blockedAtBoot = null; }
    }

    internal static IReadOnlyList<BridgeRegistration> Load()
    {
        lock (Gate)
            return TryLoadUnsafe(out var entries)
                ? entries.Where(item => !item.Pending)
                    .OrderBy(item => item.RegistrationName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.PrefabName, StringComparer.Ordinal).ToList()
                : new List<BridgeRegistration>();
    }

    // Recovery distinguishes an empty registry from a failed read and includes pending owners.
    internal static bool TryLoad(out List<BridgeRegistration> entries)
    {
        lock (Gate) return TryLoadUnsafe(out entries);
    }

    internal static BridgeRegistration? Find(string? prefabName) =>
        Load().FirstOrDefault(item => string.Equals(item.PrefabName, prefabName, StringComparison.Ordinal));

    internal static bool Begin(BridgeRegistration registration)
    {
        if (!registration.Pending || !Valid(registration)) return false;
        lock (Gate)
        {
            if (!TryLoadUnsafe(out var entries) || entries.Any(e => e.PrefabName == registration.PrefabName))
                return false;
            entries.Add(registration);
            if (!SaveUnsafe(entries)) return false;
            Active.Add(registration.PrefabName);
            return true;
        }
    }

    internal static bool Record(BridgeRegistration registration)
    {
        if (!Valid(registration)) return false;
        lock (Gate)
        {
            if (!TryLoadUnsafe(out var entries)) return false;
            var index = entries.FindIndex(item => item.PrefabName == registration.PrefabName);
            if (index >= 0) entries[index] = registration;
            else entries.Add(registration);
            if (!SaveUnsafe(entries)) return false;
            if (!registration.Pending)
            {
                Active.Remove(registration.PrefabName);
                _blockedAtBoot?.Remove(registration.PrefabName);
            }
            return true;
        }
    }

    internal static bool Commit(string prefabName)
    {
        lock (Gate)
        {
            if (!Active.Contains(prefabName) || !TryLoadUnsafe(out var entries)) return false;
            var pending = entries.FirstOrDefault(e => e.PrefabName == prefabName && e.Pending);
            if (pending == null) return false;
            return Record(new BridgeRegistration(pending.PrefabName, pending.RegistrationName,
                pending.UpperDeckId, pending.LowerDeckId, pending.StyleId, pending.CreatedUtc));
        }
    }

    internal static bool Rename(string prefabName, string registrationName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName) || string.IsNullOrWhiteSpace(registrationName)) return false;
        lock (Gate)
        {
            if (!TryLoadUnsafe(out var entries)) return false;
            var entry = entries.FirstOrDefault(item => item.PrefabName == prefabName && !item.Pending);
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
            if (!TryLoadUnsafe(out var entries)) return false;
            return entries.RemoveAll(item => item.PrefabName == prefabName) > 0 && SaveUnsafe(entries);
        }
    }

    private static bool Valid(BridgeRegistration item) => BridgeRegistration.IsPrefabName(item.PrefabName)
        && !string.IsNullOrWhiteSpace(item.RegistrationName) && !string.IsNullOrWhiteSpace(item.UpperDeckId)
        && !string.IsNullOrWhiteSpace(item.StyleId) && !string.IsNullOrWhiteSpace(item.CreatedUtc)
        && item.CreatedUtc.IndexOfAny(new[] { '\t', '\r', '\n' }) < 0;

    private static bool TryLoadUnsafe(out List<BridgeRegistration> entries)
    {
        entries = new();
        try
        {
            // Exists propagates access errors, unlike File.Exists.
            if (!BridgeFileAccess.Exists(FilePath))
            {
                if (!BridgeFileAccess.Exists(FilePath + ".bak")) return true;
                return Invalid("primary file missing; backup retained for recovery");
            }
            var lines = File.ReadAllLines(FilePath, Utf8);
            if (lines.Length == 0 || (lines[0] != Header && lines[0] != LegacyHeader))
                return Invalid("missing or unsupported header");
            var columns = lines[0] == Header ? 7 : 6;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var parsed = new List<BridgeRegistration>();
            foreach (var line in lines.Skip(1))
            {
                var fields = line.Split('\t');
                if (fields.Length != columns || !seen.Add(fields[0])) return Invalid("invalid or duplicate row");
                if (columns == 7 && fields[6] != "pending" && fields[6] != "committed")
                    return Invalid("unknown creation status");
                var item = new BridgeRegistration(fields[0], Decode(fields[1]), Decode(fields[2]),
                    Decode(fields[3]), Decode(fields[4]), fields[5], columns == 7 && fields[6] == "pending");
                if (!Valid(item)) return Invalid("invalid registration");
                parsed.Add(item);
            }
            entries = parsed;
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not read bridge registry; mutations blocked, original and backup retained");
            return false;
        }
    }

    private static bool Invalid(string reason)
    {
        Mod.Log.Warn("Bridge registry is unreadable; mutations blocked: " + reason);
        return false;
    }

    private static bool SaveUnsafe(IEnumerable<BridgeRegistration> registrations)
    {
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, Utf8, 1024, leaveOpen: true))
                {
                    writer.WriteLine(Header);
                    foreach (var item in registrations.OrderBy(item => item.PrefabName, StringComparer.Ordinal))
                        writer.WriteLine(string.Join("\t", new[] { item.PrefabName, Encode(item.RegistrationName),
                            Encode(item.UpperDeckId), Encode(item.LowerDeckId ?? string.Empty), Encode(item.StyleId),
                            item.CreatedUtc, item.Pending ? "pending" : "committed" }));
                    writer.Flush();
                }
                stream.Flush(flushToDisk: true);
            }
            // Same-directory replacement is atomic; never delete the old file as a fallback.
            if (BridgeFileAccess.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
            else File.Move(temporary, FilePath);
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not commit bridge registry; previous record retained");
            return false;
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception exception) { Mod.Log.Warn(exception, "Could not remove registry temporary file"); }
        }
    }

    private static string Encode(string value) => Convert.ToBase64String(Utf8.GetBytes(value));
    private static string Decode(string value) => Utf8.GetString(Convert.FromBase64String(value));
}
