using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CS2Mods.Shared.Infrastructure;

/// <summary>
/// Remembers what has been exported, keyed by the exported asset's name. Road Builder's registration
/// id is deliberately absent: the record has to stay meaningful after Road Builder is removed, and a
/// road that comes back under a new id must still be recognised as already exported.
/// </summary>
internal sealed class ExportStateStore
{
    private const string Header = "exportName\tfingerprint\tlastExportUtc";
    private const string LegacyHeaderPrefix = "sourceId\t";

    private readonly Dictionary<string, ExportState> _states = new(StringComparer.Ordinal);

    internal static ExportStateStore Load()
    {
        ExportPaths.EnsureDataDirectory();
        var store = new ExportStateStore();
        if (!File.Exists(ExportPaths.StateFile)) return store;

        var lines = File.ReadAllLines(ExportPaths.StateFile);
        if (lines.Length == 0) return store;

        // The first format keyed rows by the Road Builder id and carried the asset name in column 3.
        // Re-key those rows; their fingerprints predate the current canonical form and will simply read
        // as "changed", which costs one overwrite and never produces a duplicate asset.
        var legacy = lines[0].StartsWith(LegacyHeaderPrefix, StringComparison.Ordinal);
        if (legacy) ModHost.Log.Info("Migrating the export state to name-based records.");

        foreach (var line in lines.Skip(1))
        {
            var fields = line.Split('\t');
            if (legacy)
            {
                if (fields.Length < 4 || fields[2].Length == 0) continue;
                store._states[fields[2]] = new ExportState(fields[1], fields[3]);
            }
            else
            {
                if (fields.Length < 3 || fields[0].Length == 0) continue;
                store._states[fields[0]] = new ExportState(fields[1], fields[2]);
            }
        }

        if (legacy) store.Save();
        return store;
    }

    /// <summary>True when this exact road content has already been exported under this name.</summary>
    internal bool IsUnchanged(string exportName, string fingerprint)
    {
        return _states.TryGetValue(exportName, out var state)
            && string.Equals(state.Fingerprint, fingerprint, StringComparison.Ordinal);
    }

    internal bool Contains(string exportName) => _states.ContainsKey(exportName);

    /// <summary>When this name was last exported, in local time. False when it was never exported.</summary>
    internal bool TryGetLastExport(string exportName, out DateTime lastExport)
    {
        lastExport = default;
        return _states.TryGetValue(exportName, out var state)
            && DateTime.TryParse(
                state.LastExportUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out lastExport);
    }

    internal IReadOnlyList<string> ExportNames() => _states.Keys.ToList();

    internal void Record(string exportName, string fingerprint)
    {
        _states[exportName] = new ExportState(fingerprint, DateTime.UtcNow.ToString("O"));
    }

    internal void Remove(string exportName) => _states.Remove(exportName);

    internal void Save()
    {
        var lines = new List<string> { Header };
        lines.AddRange(_states.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            Sanitize(pair.Key) + "\t"
            + Sanitize(pair.Value.Fingerprint) + "\t"
            + Sanitize(pair.Value.LastExportUtc)));
        File.WriteAllLines(ExportPaths.StateFile, lines);
    }

    private static string Sanitize(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private sealed class ExportState
    {
        internal ExportState(string fingerprint, string lastExportUtc)
        {
            Fingerprint = fingerprint;
            LastExportUtc = lastExportUtc;
        }

        internal string Fingerprint { get; }
        internal string LastExportUtc { get; }
    }
}
