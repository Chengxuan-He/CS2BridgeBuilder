using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CS2Mods.Shared.Infrastructure;

internal sealed class ExportReport
{
    private readonly List<string> _entries = new();
    private readonly bool _logIssues;

    // Hosts can keep a file report without surfacing generation diagnostics in-game.
    internal ExportReport(bool logIssues = true) => _logIssues = logIssues;

    internal int ExportedRoads { get; private set; }
    internal int SkippedRoads { get; private set; }
    internal int FailedRoads { get; private set; }
    internal int RemovedRoads { get; private set; }
    internal int RemovedDependencies { get; private set; }
    internal int SavedDependencies { get; set; }

    internal void Exported(string name)
    {
        ExportedRoads++;
        _entries.Add($"[OK] {name}");
    }

    internal void Skipped(string name, string reason)
    {
        SkippedRoads++;
        _entries.Add($"[SKIP] {name}: {reason}");
    }

    /// <summary>
    /// Retain failures and their counter in the report. Hosts may opt out of game-log output.
    /// </summary>
    internal void Failed(string name, Exception exception)
    {
        FailedRoads++;
        _entries.Add($"[FAIL] {name}: {exception.GetType().Name}: {exception.Message}");
        if (_logIssues) ModHost.Log.Error(exception, $"Export failed for '{name}'");
    }

    internal void Removed(string name)
    {
        RemovedRoads++;
        _entries.Add($"[DEL] {name}");
    }

    internal void RemovedDependency(string name)
    {
        RemovedDependencies++;
        _entries.Add($"[DEL-DEP] {name}");
    }

    internal void Warning(string message)
    {
        _entries.Add("[WARN] " + message);
        if (_logIssues) ModHost.Log.Warn(message);
    }

    /// <summary>
    /// Record a defect without changing failure counters. Logging follows the host's policy.
    /// </summary>
    internal void Defect(string message)
    {
        _entries.Add("[ERROR] " + message);
        if (_logIssues) ModHost.Log.Error(message);
    }

    /// <summary>A fact about the result, not a problem. Carries no counter.</summary>
    internal void Note(string message) => _entries.Add("[INFO] " + message);

    internal void Save(string mode, string operation)
    {
        ExportPaths.EnsureDataDirectory();
        var builder = new StringBuilder();
        builder.AppendLine(ModHost.Id + " report");
        builder.AppendLine("UTC: " + DateTime.UtcNow.ToString("O"));
        builder.AppendLine("Mode: " + mode);
        builder.AppendLine("Operation: " + operation);
        builder.AppendLine($"Roads exported: {ExportedRoads}");
        builder.AppendLine($"Roads skipped: {SkippedRoads}");
        builder.AppendLine($"Roads failed: {FailedRoads}");
        builder.AppendLine($"Roads removed: {RemovedRoads}");
        builder.AppendLine($"Runtime dependencies saved: {SavedDependencies}");
        builder.AppendLine($"Runtime dependencies removed: {RemovedDependencies}");
        builder.AppendLine();
        foreach (var entry in _entries) builder.AppendLine(entry);
        File.WriteAllText(ExportPaths.ReportFile, builder.ToString());
    }
}
